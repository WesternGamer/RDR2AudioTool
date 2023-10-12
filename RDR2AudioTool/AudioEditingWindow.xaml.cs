
using CodeWalker;
using CodeWalker.GameFiles;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;


namespace RDR2AudioTool
{




    /// <summary>
    /// Interaction logic for AudioEditingWindow.xaml
    /// </summary>
    /// 

    public partial class AudioEditingWindow : Window
    {
        AwcFile? Awc = null;

        private RoutedEventHandler? playHandler = null;

        private RoutedEventHandler? pauseHandler = null;

        private System.Windows.Threading.DispatcherTimer timer;
        private GridViewColumn lastSortedColumn = new GridViewColumn();
        private ListSortDirection lastSortDirection = ListSortDirection.Ascending;
        private bool autoPlayEnabled = false;
        private bool autoPlayLoopEnabled = false;

        private WaveOut? waveOut = null;

        private RawSourceWaveStream? sourceWaveStream = null;

        private int currentPlayingIndex = 0;

        private TimeSpan timerInterval;
        private bool isPaused = false;

        public class ItemInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Length { get; set; }
            public string Size { get; set; }
            public AwcStream Stream { get; set; }

            public ItemInfo(AwcStream stream)
            {
                Name = stream.Name;
                Type = stream.TypeString;
                Length = stream.LengthStr;
                Size = TextUtil.GetBytesReadable(stream.ByteLength);
                Stream = stream;
            }
        }

        public class StereoItemInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Length { get; set; }
            public string Size { get; set; }
            public AwcStream StreamLeft { get; set; }
            public AwcStream StreamRight { get; set; }

            public StereoItemInfo(AwcStream streamLeft, AwcStream streamRight)
            {
                Name = streamLeft.Name.Substring(0, streamLeft.Name.Length - 5);
                Type = streamLeft.TypeString;
                Length = streamLeft.LengthStr;
                Size = TextUtil.GetBytesReadable(streamLeft.ByteLength + streamRight.ByteLength);
                StreamLeft = streamLeft;
                StreamRight = streamRight;
            }
        }

        public AudioEditingWindow()
        {
            waveOut = new WaveOut();
            InitializeComponent();
            timer = new System.Windows.Threading.DispatcherTimer(DispatcherPriority.Render); //smoother slide
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = TimeSpan.FromMilliseconds(10); // update the slider every 10 milliseconds so it has like a smooth slide
            TimeSpan timerInterval = timer.Interval;
            playHandler = new RoutedEventHandler(PlayButton_Click);
            pauseHandler = new RoutedEventHandler(PauseButton_Click);
            VolumeResetButton.IsEnabled = false;
            VolumeSlider.IsEnabled = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            isPaused = false;
            waveOut.Stop();
            waveOut.Dispose();
            base.OnClosed(e);
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (GridViewColumnHeader)sender;
            string tag = column.Tag as string;

            if (lastSortedColumn != null && lastSortedColumn.Header != column)
            {
                lastSortedColumn.HeaderTemplate = null;
            }

            ListSortDirection direction = ListSortDirection.Ascending;
            if (column != lastSortedColumn.Header)
            {
                direction = ListSortDirection.Ascending;
                column.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
            }
            else
            {
                direction = (lastSortDirection == ListSortDirection.Ascending) ? ListSortDirection.Descending : ListSortDirection.Ascending;
                column.Column.HeaderTemplate = (direction == ListSortDirection.Ascending) ? Resources["HeaderTemplateArrowUp"] as DataTemplate : Resources["HeaderTemplateArrowDown"] as DataTemplate; //this works for now, would like for arrow to be above/below column name, but thats for the future.
            }

            lastSortedColumn = column.Column;
            lastSortDirection = direction;
            SortListView(tag, direction);
        }

        private void SortListView(string tag, ListSortDirection direction)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(StreamList.Items); //we use StreamList.Items because in RefreshList we set StreamList.ItemsSource to null so passing that in to this will do nothing!!

            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(tag, direction));
                view.Refresh();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new Microsoft.Win32.OpenFileDialog();

            fbd.Filter = "Audio Wave Container (.awc)|*.awc";

            if (fbd.ShowDialog() == true && !string.IsNullOrWhiteSpace(fbd.FileName))
            {

                using (var stream = new FileStream(fbd.FileName, FileMode.Open))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);

                        Awc = new AwcFile();
                        Awc.Load(memoryStream.ToArray(), System.IO.Path.GetFileName(fbd.FileName));
                        Title = $"RDR2 Audio Tool - {System.IO.Path.GetFileName(fbd.FileName)}";
                        VolumeResetButton.IsEnabled = true;
                        VolumeSlider.IsEnabled = true;

                        RefreshList();
                    }
                }

            }

            if (Awc == null)
            {
                SaveButton.IsEnabled = false;
                RenameButton.IsEnabled = false;
                ReplaceButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
                MoreOptionsButton.IsEnabled = false;
                slider.IsEnabled = false;
                PlayLastButton.IsEnabled = false;
                PlayButton.IsEnabled = false;
                PlayNextButton.IsEnabled = false;
                StreamList.IsEnabled = false;
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();

            dialog.FileName = Awc?.Name;

            var dialogResult = dialog.ShowDialog();



            if (dialogResult == true)
            {
                if ((bool)(Awc?.MultiChannelFlag))
                {
                    Awc?.MultiChannelSource?.CompactMultiChannelSources(Awc?.Streams);
                }


                Awc?.BuildPeakChunks();

                Awc?.BuildChunkIndices();

                Awc?.BuildStreamInfos();

                Awc?.BuildStreamDict();

                File.WriteAllBytes(dialog.FileName, Awc.Save());
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (Awc.MultiChannelFlag)
            {
                string name = null;

                if (Awc.Streams[1].Name.EndsWith("_right"))
                {
                    name = Awc.Streams[1].Name.Substring(0, Awc.Streams[1].Name.Length - 6);
                }
                else
                {
                    name = "";
                }

                RenameWindow w = new RenameWindow(name);

                bool? r = w.ShowDialog();

                if (r == true)
                {
                    Awc.Streams[1].Name = w.String + "_right";
                    Awc.Streams[2].Name = w.String + "_left";
                }

                RefreshList();

                return;
            }

            RenameWindow window = new RenameWindow((StreamList.SelectedItem as ItemInfo).Stream.Name);

            bool? result = window.ShowDialog();

            if (result == true)
            {
                if (Awc?.Streams != null)
                {
                    for (int i = 0; i < Awc.Streams.Length; i++)
                    {
                        if (Awc.Streams[i].Name == (StreamList.SelectedItem as ItemInfo).Stream.Name)
                        {
                            Awc.Streams[i].Name = window.String;
                            RefreshList();
                            break;
                        }
                    }
                }
            }
        }


        private void RefreshList()
        {
            StreamList.ItemsSource = null;
            StreamList.Items.Clear();

            if (Awc?.Streams != null)
            {
                List<AwcStream> hashStreams = new List<AwcStream>();
                List<AwcStream> nameStreams = new List<AwcStream>();
                List<AwcStream> filteredStreams = new List<AwcStream>();


                var strlist = Awc.Streams.ToList();
                foreach (var audio in strlist)
                {
                    if (audio.Name.StartsWith("0x"))
                    {
                        hashStreams.Add(audio);
                    }
                    else
                    {
                        nameStreams.Add(audio);
                    }
                }

                hashStreams.Sort((a, b) => a.Hash.Hash.CompareTo(b.Hash.Hash));
                nameStreams.Sort(new AlphanumericComparer());

                foreach (var audio in hashStreams)
                {
                    var stereo = (audio.ChannelStreams?.Length == 2);
                    if ((audio.StreamBlocks != null) && (!stereo)) continue;//don't display multichannel source audios
                    var name = audio.Name;
                    if (stereo) continue; // name = "(Stereo Playback)";

                    StreamList.Items.Add(new ItemInfo(audio));
                }

                foreach (var audio in nameStreams)
                {
                    var stereo = (audio.ChannelStreams?.Length == 2);
                    if ((audio.StreamBlocks != null) && (!stereo)) continue;//don't display multichannel source audios
                    var name = audio.Name + $" (0x{audio.Hash})";
                    if (stereo) continue; // name = "(Stereo Playback)";

                    filteredStreams.Add(audio);
                }

                if (Awc.MultiChannelFlag)
                {
                    while (filteredStreams.Count > 0)
                    {
                        List<AwcStream> objectsToRemove = filteredStreams
                        .GroupBy(obj => GetBaseName(obj.Name)) // Group by the base name
                        .Where(group => group.Count() > 1)     // Filter groups with more than one item
                        .SelectMany(group => group.Skip(0))    // Select all but the first item in each group
                        .ToList();

                        if (objectsToRemove.Count != 0)
                        {
                            StreamList.Items.Add(new StereoItemInfo(objectsToRemove[0], objectsToRemove[1]));

                            filteredStreams.Remove(objectsToRemove[0]);
                            filteredStreams.Remove(objectsToRemove[1]);
                        }
                        else if (filteredStreams.Count > 0)
                        {
                            foreach (var audio in filteredStreams)
                            {
                                StreamList.Items.Add(new ItemInfo(audio));
                            }

                            break;
                        }
                        
                    }
                }
                else
                {
                    foreach (var audio in filteredStreams)
                    {
                        StreamList.Items.Add(new ItemInfo(audio));
                    }
                }

                
                SaveButton.IsEnabled = true;
                RenameButton.IsEnabled = true;
                ReplaceButton.IsEnabled = true;
                AutoPlayBox.IsEnabled = true;
                slider.IsEnabled = true;
                PlayLastButton.IsEnabled = true;
                PlayButton.IsEnabled = true;
                PlayNextButton.IsEnabled = true;
                StreamList.IsEnabled = true;

                StreamList.SelectedIndex = 0;
            }
        }

        private string GetBaseName(string fullName)
        {
            int lastUnderscoreIndex = fullName.LastIndexOf('_');
            if (lastUnderscoreIndex >= 0)
            {
                return fullName.Substring(0, lastUnderscoreIndex);
            }
            return fullName;
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            ReplaceAudioWindow window = new ReplaceAudioWindow(Awc.MultiChannelFlag);

            bool? result = window.ShowDialog();

            if (result == true)
            {
                if (Awc.MultiChannelFlag)
                {
                    for (int i = 0; i < StreamList.SelectedItems.Count; i++)
                    {
                        if (Awc?.Streams != null)
                        {
                            if ((StreamList.SelectedItems[i] as StereoItemInfo) != null)
                            {
                                byte[] pcmdata = null;

                                pcmdata = window.PcmData;

                                if (!window.StereoInput)
                                {
                                    pcmdata = MonoToStereo(pcmdata);
                                }

                                Awc?.ReplaceAudioStreamStereo((StreamList.SelectedItems[i] as StereoItemInfo).StreamLeft.Hash, (StreamList.SelectedItems[i] as StereoItemInfo).StreamRight.Hash, (uint)(window.SampleCount / 2), (uint)window.SampleRate, window.PcmData, window.CodecType);
                            }
                            else
                            {
                                byte[] pcmdata = null;

                                pcmdata = window.PcmData;

                                if (window.StereoInput)
                                {
                                    pcmdata = MixStereoToMono(pcmdata);
                                }

                                Awc?.ReplaceAudioStreamSingle((StreamList.SelectedItems[i] as ItemInfo).Stream.Hash, (uint)window.SampleCount / 2, (uint)window.SampleRate, pcmdata, window.CodecType);
                            }

                        }
                    }

                    RefreshList();
                    return;
                }

                for (int i = 0; i < StreamList.SelectedItems.Count; i++)
                {
                    if (Awc?.Streams != null)
                    {
                        byte[] pcmdata = null;

                        pcmdata = window.PcmData;

                        if (window.StereoInput)
                        {
                            pcmdata = MixStereoToMono(pcmdata);
                        }

                        Awc?.ReplaceAudioStreamSingle((StreamList.SelectedItems[i] as ItemInfo).Stream.Hash, (uint)window.SampleCount, (uint)window.SampleRate, pcmdata, window.CodecType);
                    }
                }
            }

            RefreshList();
        }

        private void Play()
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                return;
            }

            PlayButton.Content = "⏸";
            PlayButton.Click -= playHandler;
            PlayButton.Click += pauseHandler;

            if (isPaused)
            {
                waveOut.Play();
                isPaused = false;
                return;
            }

            Stop();

            if (StreamList.SelectedItems.Count == 1)
            {
                double lengthSeconds = 0;
                var item = StreamList.Items[currentPlayingIndex];
                if (item.GetType() == typeof(StereoItemInfo))
                {
                    StereoItemInfo aud = (StereoItemInfo)item;
                    lengthSeconds = aud.StreamLeft.Length;
                    byte[] leftPcm = aud.StreamLeft.GetPcmData();
                    byte[] rightPcm = aud.StreamRight.GetPcmData();

                    byte[] stereoPcm = CombineLeftAndRightChannel(leftPcm, rightPcm);

                    sourceWaveStream = new RawSourceWaveStream(new MemoryStream(stereoPcm), new WaveFormat(aud.StreamLeft.SamplesPerSecond, 16, 2));
                }
                else
                {
                    var audio = (item as ItemInfo).Stream;
                    lengthSeconds = audio.Length;

                    sourceWaveStream = new RawSourceWaveStream(new MemoryStream(audio.GetPcmData()), new WaveFormat(audio.SamplesPerSecond, 16, 1));
                }

                waveOut.Init(sourceWaveStream);
                waveOut.Play();
                timer.Start();

                double totalDuration = lengthSeconds; //this is how we make sure that the bar length is equal to the duration so that it properly goes from start to finish
                slider.Maximum = totalDuration;
                slider.Value = 0;
            }
        }

        private byte[] CombineLeftAndRightChannel(byte[] left, byte[] right)
        {
            byte[] output = new byte[left.Length + right.Length];
            int outputIndex = 0;
            for (int n = 0; n < left.Length; n += 2)
            {
                output[outputIndex++] = left[n];
                output[outputIndex++] = left[n + 1];
                output[outputIndex++] = right[n];
                output[outputIndex++] = right[n + 1];
            }
            return output;
        }

        //might update in the future but its really not needed I don't think.
        private void Pause()
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
                isPaused = true;
                PlayButton.Content = "⏵";
                PlayButton.Click -= pauseHandler;
                PlayButton.Click += playHandler;
            }
        }

        private void Stop()
        {
            isPaused = false;
            waveOut.Stop();
                waveOut.Dispose();
            waveOut = new WaveOut();
            timer.Stop();
            PlayButton.Content = "⏵";
        }

        private void PlayNext()
        {
            if (currentPlayingIndex < StreamList.Items.Count - 1)
            {
                Stop();
                currentPlayingIndex++;
                StreamList.SelectedIndex = currentPlayingIndex;
                Play();
            }
            else
            {
                StreamList.SelectedIndex = 0;
                currentPlayingIndex = 0;
                Play();
            }
        }

        private void PlayLast()
        {
            if (currentPlayingIndex > 0)
            {
                Stop();
                currentPlayingIndex--;
                StreamList.SelectedIndex = currentPlayingIndex;
                Play();
            }
            else if (currentPlayingIndex == 0)
            {
                currentPlayingIndex = StreamList.Items.Count - 1;
                StreamList.SelectedIndex = StreamList.Items.Count - 1;
                Play();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {

                double currentPosition = sourceWaveStream.Position / (double)sourceWaveStream.WaveFormat.AverageBytesPerSecond;
                slider.Value = currentPosition;

                TimeSpan currentTime = TimeSpan.FromSeconds(currentPosition);
                DurationLabel.Content = currentTime.ToString(@"mm\:ss") + " / " + TimeSpan.FromSeconds(sourceWaveStream.Length / sourceWaveStream.WaveFormat.AverageBytesPerSecond).ToString(@"mm\:ss");
            }
            else
            {
                if (!isPaused && sourceWaveStream.Length == sourceWaveStream.Position)
                {
                    //basically at this point the playback has stopped so we want to reset the slider for it to look nice 😋
                    this.Dispatcher.Invoke(() =>
                    {
                        if (autoPlayEnabled)
                        {
                            object item = null;
                            if ((currentPlayingIndex) == (StreamList.Items.Count - 1))
                            {
                                if (autoPlayLoopEnabled)
                                {
                                    currentPlayingIndex = 0;
                                    item = StreamList.Items[currentPlayingIndex];
                                    StreamList.SelectedItem = item;
                                }
                            }
                            else if ((currentPlayingIndex) < (StreamList.Items.Count - 1))
                            {
                                currentPlayingIndex += 1;

                                item = StreamList.Items[currentPlayingIndex];
                                StreamList.SelectedItem = item;


                            }

                            if (item != null)
                            {
                                
                                //currentItem = item;
                                
                                Stop();
                                Play();
                            }
                        }

                        else
                        {
                            slider.Value = slider.Minimum;
                            DurationLabel.Content = "00:00 / " + TimeSpan.FromSeconds(sourceWaveStream.Length / sourceWaveStream.WaveFormat.AverageBytesPerSecond).ToString(@"mm\:ss");
                            PlayButton.Content = "▶";
                            PlayButton.Click -= pauseHandler;
                            PlayButton.Click += playHandler;
                        }
                        
                    });
                }
            }
        }

        private void MoreOptionsButton_Copy_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void PlayLastButton_Click(object sender, RoutedEventArgs e)
        {
            PlayLast();
        }

        private void PlayNextButton_Click(object sender, RoutedEventArgs e)
        {
            PlayNext();
        }
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (slider.IsFocused || slider.IsMouseDirectlyOver || slider.IsMouseOver || slider.IsKeyboardFocused || slider.IsKeyboardFocusWithin)
            {
                sourceWaveStream.SetPosition(slider.Value);
            }
        }

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                if (tabControl.SelectedItem == AwcPlayerTab) //check which tab is currently selected, more specifically if it's the player tab.  We do this to refresh the list when they switch back
                {
                    RefreshList();
                }
                else if (tabControl.SelectedItem == AwcXmlTab)
                {
                    AwcXmlTextBox.Text = AwcXml.GetXml(Awc);
                }
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Volume = (float)(VolumeSlider.Value / 100);

            }
            if (VolumeLabel != null)
            {
                if (VolumeLabel.Content != null)
                {
                    VolumeLabel.Content = VolumeSlider.Value.ToString();
                }
            }
        }

        private void StreamList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (waveOut.PlaybackState != PlaybackState.Playing)
            {
                if (StreamList.SelectedItems.Count == 1 && StreamList.Items[currentPlayingIndex] != null)
                {
                  

                    if (StreamList.Items[currentPlayingIndex] as ItemInfo is not null) 
                    {
                        TimeSpan durationTime = TimeSpan.FromSeconds((StreamList.Items[currentPlayingIndex] as ItemInfo).Stream.Length);
                        DurationLabel.Content = $"00:00 / {durationTime.ToString(@"mm\:ss")}"; //change 00:00 to the actual length of the audio 
                    }
                    else if (StreamList.Items[currentPlayingIndex] is StereoItemInfo) 
                    {
                        TimeSpan durationTime = TimeSpan.FromSeconds((StreamList.Items[currentPlayingIndex] as StereoItemInfo).StreamLeft.Length);
                        DurationLabel.Content = $"00:00 / {durationTime.ToString(@"mm\:ss")}"; //change 00:00 to the actual length of the audio 
                    }
                    
                }
            }
        }

        //reset button for volume slider but button looks shit so for now it's disabled
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            waveOut.Volume = .5f;
            VolumeSlider.Value = 50;
        }

        private void autoPlay_Checked(object sender, RoutedEventArgs e)
        {
            autoPlayEnabled = true;
            LoopAutoPlay.IsEnabled = true;
        }

        private void autoPlay_Unchecked(object sender, RoutedEventArgs e)
        {
            autoPlayEnabled = false;
            autoPlayLoopEnabled = false;
            LoopAutoPlay.IsEnabled = false;
        }

        private void loopAutoPlay_Checked(object sender, RoutedEventArgs e)
        {
            autoPlayLoopEnabled = true;
        }

        private void loopAutoPlay_Unchecked(object sender, RoutedEventArgs e)
        {
            autoPlayLoopEnabled = false;
        }

        private byte[] MonoToStereo(byte[] input)
        {
            byte[] output = new byte[input.Length * 2];
            int outputIndex = 0;
            for (int n = 0; n < input.Length; n += 2)
            {
                output[outputIndex++] = input[n];
                output[outputIndex++] = input[n + 1];
                output[outputIndex++] = input[n];
                output[outputIndex++] = input[n + 1];
            }
            return output;
        }

        private byte[] MixStereoToMono(byte[] input)
        {
            byte[] output = new byte[input.Length / 2];
            int outputIndex = 0;
            for (int n = 0; n < input.Length; n += 4)
            {
                int leftChannel = BitConverter.ToInt16(input, n);
                int rightChannel = BitConverter.ToInt16(input, n + 2);
                int mixed = (leftChannel + rightChannel) / 2;
                byte[] outSample = BitConverter.GetBytes((short)mixed);

                output[outputIndex++] = outSample[0];
                output[outputIndex++] = outSample[1];
            }
            return output;
        }
    }
}