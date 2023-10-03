
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


        private System.Windows.Threading.DispatcherTimer timer;

        WaveOut waveOut = null;

        private int currentPlayingIndex = -1;

        private TimeSpan timerInterval;
        private bool isPaused = false;

        public class ItemInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Length { get; set; }
            public string Size { get; set; }
            public AwcStream Stream { get; set; }

            public ItemInfo(string name, string type, string length, string size, AwcStream stream)
            {
                Name = name;
                Type = type;
                Length = length;
                Size = size;
                Stream = stream;
            }
        }

        private ItemInfo currentItem = null;

        public AudioEditingWindow()
        {
            //Loaded += delegate (object sender, RoutedEventArgs e) { this.Owner.Hide(); };
            //Closed += delegate (object? sender, EventArgs e) { this.Owner.Close(); };
            waveOut = new WaveOut();
            waveOut.PlaybackStopped += waveOut_PlaybackStopped;
            InitializeComponent();
            timer = new System.Windows.Threading.DispatcherTimer(DispatcherPriority.Render); //smoother slide
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = TimeSpan.FromMilliseconds(10); // update the slider every 10 milliseconds so it has like a smooth slide
            TimeSpan timerInterval = timer.Interval;
            VolumeResetButton.IsEnabled = false;
            VolumeSlider.IsEnabled = false;
        }
        private void waveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (waveOut.PlaybackState == PlaybackState.Stopped)
            {
                //basically at this point the playback has stopped so we want to reset the slider for it to look nice 😋
                this.Dispatcher.Invoke(() =>
                {
                    slider.Value = slider.Minimum;
                    if (StreamList.SelectedItems.Count == 1)
                    {
                        currentItem = StreamList.SelectedItems[0] as ItemInfo;
                        TimeSpan durationTime = TimeSpan.FromSeconds(currentItem.Stream.Length);
                        DurationLabel.Content = $"00:00 / {durationTime.ToString(@"mm\:ss")}"; //change 00:00 to the actual length of the audio 
                    }
                    /*PlayButton.Content = "▶";
                    PlayButton.Click += new RoutedEventHandler(PlayButton_Click);*/
                });
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
                        Title = $"AudioEditingWindow - {System.IO.Path.GetFileName(fbd.FileName)}";
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

                    StreamList.Items.Add(new ItemInfo(name, audio.Type, audio.LengthStr, TextUtil.GetBytesReadable(audio.ByteLength), audio));
                }

                foreach (var audio in nameStreams)
                {
                    var stereo = (audio.ChannelStreams?.Length == 2);
                    if ((audio.StreamBlocks != null) && (!stereo)) continue;//don't display multichannel source audios
                    var name = audio.Name + $" (0x{audio.Hash})";
                    if (stereo) continue; // name = "(Stereo Playback)";

                    StreamList.Items.Add(new ItemInfo(name, audio.Type, audio.LengthStr, TextUtil.GetBytesReadable(audio.ByteLength), audio));
                }

                SaveButton.IsEnabled = true;
                RenameButton.IsEnabled = true;
                ReplaceButton.IsEnabled = true;
                //DeleteButton.IsEnabled = true;
                //MoreOptionsButton.IsEnabled = true;

                if (Awc.MultiChannelFlag)
                {
                    StreamList.IsEnabled = false;
                }
                else
                {
                    StreamList.IsEnabled = true;
                    StreamList.SelectedIndex = 0;
                }
            }
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            ReplaceAudioWindow window = new ReplaceAudioWindow(Awc.MultiChannelFlag);

            bool? result = window.ShowDialog();

            if (result == true)
            {
                if (Awc.MultiChannelFlag)
                {
                    if (Awc?.Streams != null)
                    {
                        Awc?.ReplaceAudioStream(null, (uint)(window.SampleCount / 2), (uint)window.SampleRate, window.PcmData, window.CodecType);
                    }

                    RefreshList();
                    return;
                }

                for (int i = 0; i < StreamList.SelectedItems.Count; i++)
                {
                    if (Awc?.Streams != null)
                    {
                        Awc?.ReplaceAudioStream((StreamList.SelectedItems[i] as ItemInfo).Stream.Hash, (uint)window.SampleCount, (uint)window.SampleRate, window.PcmData, window.CodecType);
                    }
                }
            }

            RefreshList();
        }

        private void Play()
        {
            if (isPaused)
            {
                waveOut.Play();
                isPaused = false;
                return;
            }

            /*PlayButton.Content = "⏸";
            PlayButton.Click += new RoutedEventHandler(PauseButton_Click);*/

            Stop();

            if (StreamList.SelectedItems.Count == 1)
            {
                var item = StreamList.SelectedItems[0] as ItemInfo;
                var audio = item.Stream;
                currentItem = item;
                IWaveProvider provider = new RawSourceWaveStream(new MemoryStream(audio.GetRawData()), new WaveFormat(audio.SamplesPerSecond, 16, 1));

                waveOut.Init(provider);
                waveOut.Play();
                timer.Start();
                currentPlayingIndex = StreamList.SelectedIndex;
                double totalDuration = audio.Length; //this is how we make sure that the bar length is equal to the duration so that it properly goes from start to finish
                slider.Maximum = totalDuration;
                slider.Value = 0;
            }
        }
        //might update in the future but its really not needed I don't think.
        private void Pause()
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
                isPaused = true;
                PlayButton.Content = "▶";
                PlayButton.Click += new RoutedEventHandler(PlayButton_Click);
            }
        }

        private void Stop()
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = new WaveOut();
            }
            timer.Stop();
        }

        private void PlayNext()
        {
            Stop();
            if (currentPlayingIndex < StreamList.Items.Count - 1)
            {
                currentPlayingIndex++;
                StreamList.SelectedIndex = currentPlayingIndex;
                Play();
            }
        }

        private void PlayLast()
        {
            Stop();
            if (currentPlayingIndex > 0)
            {
                currentPlayingIndex--;
                StreamList.SelectedIndex = currentPlayingIndex;
                Play();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {

                double currentPosition = waveOut.GetPosition() / (double)waveOut.OutputWaveFormat.AverageBytesPerSecond;
                slider.Value = currentPosition;

                TimeSpan durationTime = TimeSpan.FromSeconds(currentItem.Stream.Length);
                TimeSpan currentTime = TimeSpan.FromSeconds(currentPosition);
                DurationLabel.Content = $"{currentTime.ToString(@"mm\:ss")} / {durationTime.ToString(@"mm\:ss")}"; //change 00:00 to the actual length of the audio 
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
        }

        private void StreamList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (waveOut.PlaybackState != PlaybackState.Playing)
            {
                waveOut.Volume = .5f;
                VolumeSlider.Value = 50;
                if (StreamList.SelectedItems.Count == 1)
                {
                    currentItem = StreamList.SelectedItems[0] as ItemInfo;
                    TimeSpan durationTime = TimeSpan.FromSeconds(currentItem.Stream.Length);
                    DurationLabel.Content = $"00:00 / {durationTime.ToString(@"mm\:ss")}"; //change 00:00 to the actual length of the audio 
                }
            }
        }

        //reset button for volume slider but button looks shit so for now it's disabled
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            waveOut.Volume = .5f;
            VolumeSlider.Value = 50;
        }

        /*private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            FindDialog findDialog = new FindDialog(AwcXmlTextBox);
            findDialog.ShowDialog();
        }*/
    }
}
