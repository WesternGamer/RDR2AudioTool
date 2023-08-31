using OggVorbisSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RDR2AudioTool
{
    internal class Libvorbisencl
    {
        [DllImport("vorbisenc", ExactSpelling = true)]
        public unsafe static extern int vorbis_encode_init_vbr(vorbis_info* vi, long channels, long rate, float base_quality);
    }
}
