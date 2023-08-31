using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using CodeWalker.GameFiles;

namespace RDR2AudioTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Static { get; private set; }
        public MainWindow()
        {
            Static = this;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AudioEditingWindow editWindow = new AudioEditingWindow();
            editWindow.Owner = this;
            editWindow.ShowDialog();
        }

            

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            StereoAudioEditingWindow editWindow = new StereoAudioEditingWindow();
            editWindow.Owner = this;
            editWindow.ShowDialog();
        }
    }
}
