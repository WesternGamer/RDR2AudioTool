using CodeWalker.GameFiles;
using Microsoft.Win32;
using NAudio.Wave;
using OggVorbisSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace RDR2AudioTool
{
    /// <summary>
    /// Interaction logic for ReplaceAudioWindow.xaml
    /// </summary>
    public partial class ReplaceAudioWindow : Window
    {
        public AwcCodecType CodecType = AwcCodecType.PCM;

        public byte[] PcmData = null;

        public byte[] PcmDataLeft = null;

        public byte[] PcmDataRight = null;

        public int SampleCount = 0;

        public int SampleRate = 0;

        private readonly bool StereoMode = false;

        public ReplaceAudioWindow(bool stereoMode = false)
        {
            StereoMode = stereoMode;
            InitializeComponent();
            CodecSelectionBox.SelectedIndex = 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            if (dialog.ShowDialog().Value)
            {
                FileTextBox.Text = dialog.FileName;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CodecSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((CodecSelectionBox.SelectedItem as ComboBoxItem).Content.ToString() == "PCM")
            {
                CodecType = AwcCodecType.PCM;
                return;
            }
            if ((CodecSelectionBox.SelectedItem as ComboBoxItem).Content.ToString() == "ADPCM")
            {
                CodecType = AwcCodecType.ADPCM;
                return;
            }
            if ((CodecSelectionBox.SelectedItem as ComboBoxItem).Content.ToString() == "VORBIS")
            {
                CodecType = AwcCodecType.VORBIS;
                return;
            }
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckFileIsUsable(out byte[] data, out byte[] leftData, out byte[] rightData))
            {
                return;
            }

            PcmData = data;
            PcmDataLeft = leftData;
            PcmDataRight = rightData;
            DialogResult = true;
        }

        private bool CheckFileIsUsable(out byte[]? pcmData, out byte[]? leftPcmData, out byte[]? rightPcmData)
        {
            pcmData = null;
            leftPcmData = null;
            rightPcmData = null;

            if (FileTextBox.Text == "")
            {
                MessageBox.Show("Please enter a file name.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!File.Exists(FileTextBox.Text))
            {
                MessageBox.Show("You do not have the right permissions to access the file, the file does not exist, or an invalid path was entered. Please check the path and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            using (WaveFileReader reader = new WaveFileReader(FileTextBox.Text))
            {
                if (reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                {
                    MessageBox.Show("PCM wav files are only accepted.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }


                var id = Guid.NewGuid().ToString();
                using (MediaFoundationResampler file = EncodeTo16BitPCMIfNeeded(reader))
                {
                    WaveFileWriter.CreateWaveFile(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{id}"), file);
                }

                using (WaveFileReader reader2 = new WaveFileReader(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{id}")))
                    {
                        pcmData = new byte[reader2.Length];
                        reader2.Read(pcmData, 0, (int)reader2.Length);

                        if (reader2.WaveFormat.Channels != 1 && !StereoMode)
                        {
                            MessageBox.Show("Only mono .wav files supported in this editor mode!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }

                        SampleCount = (pcmData.Length / (reader2.WaveFormat.BitsPerSample / 8));

                        if (StereoMode)
                        {
                            if (reader2.WaveFormat.Channels != 2)
                            {
                                MessageBox.Show($"Wav file has too many channels. {reader2.WaveFormat.Channels} channels detected.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return false;
                            }

                            var outputs = new List<List<byte>>();
                            for (int i = 0; i < reader2.WaveFormat.Channels; i++)
                            {
                                outputs.Add(new List<byte>());
                            }

                            int channelCount = reader2.WaveFormat.Channels;
                            var count = 0;
                            for (int i = 0; i < pcmData.Length; i += (reader2.WaveFormat.BitsPerSample / 8))
                            {
                                for (int j = 0; j < (reader2.WaveFormat.BitsPerSample / 8); j++)
                                {
                                    outputs[count].Add(pcmData[i + j]);
                                }
                                count++;
                                channelCount--;
                                if (channelCount >= 1) continue;
                                channelCount = reader2.WaveFormat.Channels;
                                count = 0;
                            }


                            for (int i = 0; i < outputs.Count; i++)
                            {
                                byte[] data = outputs[i].ToArray();

                                if (!EncodeIfneeded(reader2.WaveFormat.Encoding, reader2.WaveFormat.BitsPerSample, ref data))
                                {
                                    return false;
                                }

                                outputs[i] = data.ToList();
                            }

                            leftPcmData = outputs[0].ToArray();
                            rightPcmData = outputs[1].ToArray();
                        }
                        else
                        {
                            if (!EncodeIfneeded(reader2.WaveFormat.Encoding, reader2.WaveFormat.BitsPerSample, ref pcmData))
                            {
                                return false;
                            }
                        }


                        SampleRate = reader2.WaveFormat.SampleRate;
                    }

                try
                {
                    File.Delete(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{id}"));
                }
                catch { }
                
            }
                

            return true;
        }


        public unsafe bool EncodeIfneeded(WaveFormatEncoding encoding, int bitsPerSample, ref byte[] pcmData)
        {
            if (CodecType == AwcCodecType.ADPCM && encoding != NAudio.Wave.WaveFormatEncoding.Adpcm)// convert PCM wav to ADPCM where required
            {
                switch (bitsPerSample)
                {
                    case 8:
                        MessageBox.Show("Please encode PCM as 16 bit signed before importing or choose PCM instead.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    case 16:
                        pcmData = ADPCMCodec.EncodeADPCM(pcmData, SampleCount);
                        break;
                    case 32:
                        MessageBox.Show("Please encode PCM as 16 bit signed before importing or choose PCM instead.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                }
            }
            else if (CodecType == AwcCodecType.VORBIS)
            {
                vorbis_info info;
                Vorbis.vorbis_info_init(&info);
                Libvorbisencl.vorbis_encode_init_vbr(&info, 2, SampleRate, 1);
            }

            return true;
        }

        private MediaFoundationResampler EncodeTo16BitPCMIfNeeded(WaveFileReader reader)
        {
            WaveFormat format = new WaveFormat(reader.WaveFormat.SampleRate, 16, reader.WaveFormat.Channels);

            return new MediaFoundationResampler(reader, format);

        }
    }
}
