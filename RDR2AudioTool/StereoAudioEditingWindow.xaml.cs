
using CodeWalker;
using CodeWalker.GameFiles;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace RDR2AudioTool
{
    /// <summary>
    /// Interaction logic for StereoAudioEditingWindow.xaml
    /// </summary>
    public partial class StereoAudioEditingWindow : Window
    {
        AwcFile? Awc = null;

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

        public StereoAudioEditingWindow()
        {
            Loaded += delegate (object sender, RoutedEventArgs e) { this.Owner.Hide(); };
            Closed += delegate (object? sender, EventArgs e) { this.Owner.Close(); };
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
                MoreOptionsButton.IsEnabled = true;
            }

            StreamList.SelectedIndex = 0;
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            ReplaceAudioWindow window = new ReplaceAudioWindow(true);

            bool? result = window.ShowDialog();

            if (result == true)
            {
                //Awc.ChunkIndicesFlag = false;
                //Awc.ChunkIndices = null;

                if (Awc?.Streams != null)
                {
                    Awc?.ReplaceAudioStream(null, (uint)(window.SampleCount / 2), (uint)window.SampleRate, window.PcmData, window.CodecType);
                }
            }



            RefreshList();
        }
    }
}