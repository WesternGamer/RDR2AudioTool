using CodeWalker.GameFiles;
using Microsoft.Win32;
using NAudio.Wave;
using OggVorbisSharp;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
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

        public int SampleCount = 0;

        public int SampleRate = 0;

        public bool StereoInput = false;

        public ReplaceAudioWindow(bool multiChannelFlag = false)
        {
            InitializeComponent();
            if (!multiChannelFlag)
            {
                CodecSelectionBox.Items.Add("PCM");
            }
            CodecSelectionBox.Items.Add("ADPCM");
            CodecSelectionBox.Items.Add("Vorbis");
            CodecSelectionBox.SelectedIndex = 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Audio Files|*.wav; *.mp3";

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
            if (CodecSelectionBox.SelectedItem == "PCM")
            {
                CodecType = AwcCodecType.PCM;
                return;
            }
            if (CodecSelectionBox.SelectedItem == "ADPCM")
            {
                CodecType = AwcCodecType.ADPCM;
                return;
            }
            if (CodecSelectionBox.SelectedItem == "Vorbis")
            {
                CodecType = AwcCodecType.VORBIS;
                return;
            }
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FileTextBox.Text))
            {
                MessageBox.Show("Please enter a path to the file.");
                return;
            }

            if (!Path.Exists(FileTextBox.Text))
            {
                MessageBox.Show("Invalid path.");
                return;
            }
            ReadFile(FileTextBox.Text);
            DialogResult = true;
            Close();
        }

        private void ReadFile(string fileName)
        {
            switch (Path.GetExtension(fileName))
            {
                case ".wav":
                    ReadWavFile(fileName);
                    break;
                case ".mp3":
                    ReadMp3File(fileName);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported file extension.");
            }
        }

        private void ReadWavFile(string fileName)
        {
            WaveFileReader wavFile = new WaveFileReader(fileName);
            if (wavFile.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            {
                MessageBox.Show("PCM wav files are only accepted.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (wavFile.WaveFormat.Channels > 2)
            {
                MessageBox.Show("Wav file has too many channels.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            PcmData = EncodeIfNeeded(wavFile);
            SampleRate = wavFile.WaveFormat.SampleRate;
            if (wavFile.WaveFormat.Channels == 2)
            {
                StereoInput = true;
            }

            SampleCount = PcmData.Length / 2;
        }

        private void ReadMp3File(string fileName)
        {
            Mp3FileReader mp3File = new Mp3FileReader(fileName);
            if (mp3File.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            {
                MessageBox.Show("PCM wav files are only accepted.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (mp3File.WaveFormat.Channels > 2)
            {
                MessageBox.Show("Wav file has too many channels.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            PcmData = EncodeIfNeeded(mp3File);
            SampleRate = mp3File.WaveFormat.SampleRate;
            if (mp3File.WaveFormat.Channels == 2)
            {
                StereoInput = true;
            }

            SampleCount = PcmData.Length / 2;
        }

        private byte[] EncodeIfNeeded(IWaveProvider reader)
        {
            if (CodecType == AwcCodecType.ADPCM)
            {
                WaveFormat format = new WaveFormat(reader.WaveFormat.SampleRate, 16, reader.WaveFormat.Channels);

                MemoryStream outputStream = new MemoryStream();

                MediaFoundationResampler resampler = new MediaFoundationResampler(reader, format);

                byte[] array = new byte[resampler.WaveFormat.AverageBytesPerSecond * 4];
                while (true)
                {
                    int num = resampler.Read(array, 0, array.Length);
                    if (num == 0)
                    {
                        break;
                    }

                    outputStream.Write(array, 0, num);
                }

                resampler.Dispose();

                byte[] bytes = outputStream.ToArray();

                outputStream.Dispose();

                return bytes;
            }

            if (CodecType == AwcCodecType.VORBIS && reader.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                MemoryStream outputStream = new MemoryStream();

                byte[] array = new byte[reader.WaveFormat.AverageBytesPerSecond * 4];
                while (true)
                {
                    int num = reader.Read(array, 0, array.Length);
                    if (num == 0)
                    {
                        break;
                    }

                    outputStream.Write(array, 0, num);
                }

                byte[] bytes = outputStream.ToArray();

                outputStream.Dispose();

                return bytes;
            }
            else if (CodecType != AwcCodecType.PCM)
            {
                WaveFormat format = new WaveFormat(reader.WaveFormat.SampleRate, 16, reader.WaveFormat.Channels);

                MemoryStream outputStream = new MemoryStream();

                MediaFoundationResampler resampler = new MediaFoundationResampler(reader, format);

                Wave16ToFloatProvider floatProvider = new Wave16ToFloatProvider(resampler);

                byte[] array = new byte[floatProvider.WaveFormat.AverageBytesPerSecond * 4];
                while (true)
                {
                    int num = floatProvider.Read(array, 0, array.Length);
                    if (num == 0)
                    {
                        break;
                    }

                    outputStream.Write(array, 0, num);
                }

                resampler.Dispose();

                byte[] bytes = outputStream.ToArray();
                return bytes;
            }

            return null;
        }
    }
}
