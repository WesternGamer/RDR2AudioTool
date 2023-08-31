
using CodeWalker;
using CodeWalker.GameFiles;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;


namespace RDR2AudioTool
{
    



    /// <summary>
    /// Interaction logic for AudioEditingWindow.xaml
    /// </summary>
    public partial class AudioEditingWindow : Window
    {
        AwcFile? Awc = null;

        WaveOut waveOut = null;

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

        public AudioEditingWindow()
        {
            Loaded += delegate (object sender, RoutedEventArgs e) { this.Owner.Hide(); };
            Closed += delegate (object? sender, EventArgs e) { this.Owner.Close(); };
            waveOut = new WaveOut();
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new Microsoft.Win32.OpenFileDialog();

            if (fbd.ShowDialog() == true && !string.IsNullOrWhiteSpace(fbd.FileName))
            {

                using (var stream = new FileStream(fbd.FileName, FileMode.Open))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);

                        Awc = new AwcFile();
                        Awc.Load(memoryStream.ToArray(), System.IO.Path.GetFileName(fbd.FileName));

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
                var strlist = Awc.Streams.ToList();
                foreach (var audio in strlist)
                {
                    var stereo = (audio.ChannelStreams?.Length == 2);
                    if ((audio.StreamBlocks != null) && (!stereo)) continue;//don't display multichannel source audios
                    var name = audio.Name;
                    if (stereo) continue; // name = "(Stereo Playback)";

                    if (!name.StartsWith("0x"))
                    {
                        name = name + $" (0x{audio.Hash})";
                    }

                    var item = StreamList.Items.Add(new ItemInfo(name, audio.Type, audio.LengthStr, TextUtil.GetBytesReadable(audio.ByteLength), audio));
                }
                SaveButton.IsEnabled = true;
                RenameButton.IsEnabled = true;
                ReplaceButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
                MoreOptionsButton.IsEnabled = true;
            }
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            ReplaceAudioWindow window = new ReplaceAudioWindow(false);

            bool? result = window.ShowDialog();

            if (result == true)
            {
                for (int i = 0; i < StreamList.SelectedItems.Count; i++)
                {

                    if (Awc?.Streams != null)
                    {
                        for (int c = 0; c < Awc.Streams.Length; c++)
                        {
                            if (Awc.Streams[c].Name == (StreamList.SelectedItems[i] as ItemInfo).Stream.Name)
                            {

                                if ((window.CodecType == AwcCodecType.PCM) || (window.CodecType == AwcCodecType.ADPCM))
                                {
                                    if (Awc.Streams[c].VorbisChunk != null)
                                    {
                                        Awc.Streams[c].VorbisChunk = null;
                                    }

                                    List<AwcChunk> chunks = Awc.Streams[c].Chunks.ToList();

                                    for (int p = 0; p < chunks.Count; p++)
                                    {
                                        if (chunks[p].GetType() == typeof(AwcVorbisChunk))
                                        {
                                            chunks.Remove(chunks[p]);
                                        }
                                    }
                                    chunks.ToArray();

                                    if (Awc?.MultiChannelSource == null)
                                    {
                                        Unk1Chunk chunk = new Unk1Chunk(null);
                                        chunk.Samples = (uint)window.SampleCount;
                                        chunk.LoopPoint = -1;
                                        chunk.SamplesPerSecond = (ushort)window.SampleRate;
                                        chunk.Headroom = 0;
                                        chunk.Unk1 = (ushort)window.SampleCount;
                                        chunk.LoopBegin = 0;
                                        chunk.LoopEnd = 0;
                                        chunk.PlayEnd = 0;
                                        chunk.PlayBegin = 0;
                                        chunk.Unk2 = -1;
                                        chunk.Unk3 = 50000;
                                        chunk.Unk4 = 0;
                                        chunks.Add(chunk);
                                    }

                                    Awc.Streams[c].Chunks = chunks.ToArray();


                                }

                                if (Awc.MultiChannelFlag)
                                {
                                    if (Awc.Streams[c].StreamFormat != null)
                                    {
                                        Awc.Streams[c].StreamFormat.Samples = (uint)window.SampleCount;
                                        Awc.Streams[c].StreamFormat.SamplesPerSecond = (ushort)window.SampleRate;

                                        Awc.Streams[c].DataChunk = new AwcDataChunk(null);
                                        Awc.Streams[c].DataChunk.Data = window.PcmData;
                                    }
                                }
                                else
                                {
                                    if (Awc.Streams[c].FormatChunk != null)
                                    {
                                        Awc.Streams[c].FormatChunk.Samples = (uint)window.SampleCount;
                                        Awc.Streams[c].FormatChunk.SamplesPerSecond = (ushort)window.SampleRate;

                                        if (Awc.Streams[c].DataChunk == null) Awc.Streams[c].DataChunk = new AwcDataChunk(new AwcChunkInfo() { Type = AwcChunkType.data });

                                        Awc.Streams[c].DataChunk.Data = window.PcmData;
                                    }

                                    if (Awc.Streams[c].Unk1 != null)
                                    {
                                        Awc.Streams[c].Unk1.Samples = (uint)window.SampleCount;
                                        Awc.Streams[c].Unk1.SamplesPerSecond = (ushort)window.SampleRate;
                                        Awc.Streams[c].Unk1.Unk1 = (ushort)window.SampleCount; ///////May need to be adjusted

                                        if (Awc.Streams[c].DataChunk == null) Awc.Streams[c].DataChunk = new AwcDataChunk(new AwcChunkInfo() { Type = AwcChunkType.data });

                                        Awc.Streams[c].DataChunk.Data = window.PcmData;
                                    }
                                }


                                break;
                            }
                        }
                    }
                }
            }



            RefreshList();
        }

        private void Play()
        {
            Stop();

            if (StreamList.SelectedItems.Count == 1)
            {
                var item = StreamList.SelectedItems[0] as ItemInfo;

                var audio = item.Stream;

                IWaveProvider provider = new RawSourceWaveStream(new MemoryStream(audio.GetRawData()), new WaveFormat(audio.SamplesPerSecond, 16, 1));

                waveOut.Init(provider);
                waveOut.Play();
            }


        }

        private void Pause()
        {

        }

        private void Stop()
        {

        }

        private void PlayNext()
        {

        }

        private void PlayLast()
        {
            Stop();
            var nextIndex = StreamList.SelectedIndex - 1;
            if (nextIndex < StreamList.Items.Count)
            {
                //StreamList.Items[nextIndex].Selected = true;
                //StreamList.Items[nextIndex].Focused = true;
                Play();
            }
        }
    }
}
