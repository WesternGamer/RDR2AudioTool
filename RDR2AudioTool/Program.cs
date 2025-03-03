using CodeWalker.GameFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RDR2AudioTool
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
                if (File.Exists("strings.txt"))
                {
                    JenkIndex.LoadStringsList("strings.txt");
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var app = new App();
                app.InitializeComponent();
                app.Run();
#if !DEBUG
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "An Critical Error Occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif
}
    }
}
