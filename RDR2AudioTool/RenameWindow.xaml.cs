using CodeWalker.GameFiles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
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

namespace RDR2AudioTool
{
    /// <summary>
    /// Interaction logic for RenameWindow.xaml
    /// </summary>
    public partial class RenameWindow : Window
    {
        public string? String = null;

        public RenameWindow(string name)
        {
            String = name;
            InitializeComponent();
            NameTextbox.Text = String;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if(NameTextbox.Text.StartsWith("0x"))
            {
                try
                {
                    var _ = (uint)new System.ComponentModel.UInt32Converter().ConvertFromString(NameTextbox.Text);
                }
                catch (ArgumentException)
                { 
                    InvalidHashWarning.Visibility = Visibility.Visible;
                    return;
                }
            }
            else
            {
                JenkIndex.Ensure(NameTextbox.Text);
            }
            JenkIndex.Ensure(NameTextbox.Text);
            String = NameTextbox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
