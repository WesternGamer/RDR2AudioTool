/*using OggVorbisSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CodeWalker.Core.Utils
{
    internal static unsafe class VorbisHelper
    {
        public static vorbis_dsp_state CreateHeader(long channels, long sampleRate, out byte[] streamIdData, out byte[] commentData, out byte[] codebookData)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(memoryStream);

            vorbis_info info = new vorbis_info();
            Vorbis.vorbis_info_init(&info);
            VorbisEnc.vorbis_encode_init_vbr(&info, channels, sampleRate, 1);
            vorbis_dsp_state state = new vorbis_dsp_state();
            Vorbis.vorbis_analysis_init(&state, &info);
            vorbis_comment comment = new vorbis_comment();
            Vorbis.vorbis_comment_init(&comment);
            ogg_packet streamId = new ogg_packet();
            ogg_packet commentPacket = new ogg_packet();
            ogg_packet codebookPacket = new ogg_packet();
            Vorbis.vorbis_analysis_headerout(&state, &comment, &streamId, &commentPacket, &codebookPacket);

            streamIdData = new byte[streamId.bytes.Value];
            commentData = new byte[commentPacket.bytes.Value];
            codebookData = new byte[codebookPacket.bytes.Value];

            Marshal.Copy((IntPtr)streamId.packet, streamIdData, 0, (int)streamId.bytes.Value);
            Marshal.Copy((IntPtr)commentPacket.packet, commentData, 0, (int)commentPacket.bytes.Value);
            Marshal.Copy((IntPtr)codebookPacket.packet, codebookData, 0, (int)codebookPacket.bytes.Value);

            Vorbis.vorbis_comment_clear(&comment);
            Vorbis.vorbis_info_clear(&info);

            return state; 
        }

        public static unsafe byte[] Encode(byte[] data, long channels, long sampleRate, out byte[] streamIdData, out byte[] commentData, out byte[] codebookData)
        {
            vorbis_info info = new vorbis_info();
            Vorbis.vorbis_info_init(&info);
            VorbisEnc.vorbis_encode_init_vbr(&info, channels, sampleRate, 1);

            vorbis_comment comment = new vorbis_comment();
            Vorbis.vorbis_comment_init(&comment);

            vorbis_dsp_state state = new vorbis_dsp_state();
            Vorbis.vorbis_analysis_init(&state, &info);

            vorbis_block block = new vorbis_block();
            Vorbis.vorbis_block_init(&state, &block);

            MemoryStream stream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(stream);

            ogg_packet streamId = new ogg_packet();
            ogg_packet commentPacket = new ogg_packet();
            ogg_packet codebookPacket = new ogg_packet();
            Vorbis.vorbis_analysis_headerout(&state, &comment, &streamId, &commentPacket, &codebookPacket);

            streamIdData = new byte[streamId.bytes.Value];
            commentData = new byte[commentPacket.bytes.Value];
            codebookData = new byte[codebookPacket.bytes.Value];

            Marshal.Copy((IntPtr)streamId.packet, streamIdData, 0, (int)streamId.bytes.Value);
            Marshal.Copy((IntPtr)commentPacket.packet, commentData, 0, (int)commentPacket.bytes.Value);
            Marshal.Copy((IntPtr)codebookPacket.packet, codebookData, 0, (int)codebookPacket.bytes.Value);

            bool endOfFile = false;
            long previousPostion = 0;

            MemoryStream memoryStream = new MemoryStream(data);
            BinaryReader br = new BinaryReader(memoryStream);

            while (!endOfFile)
            {
                byte[] buffer = br.ReadBytes(1024 * 4);

                long bytesRead = br.BaseStream.Position - previousPostion;

                if (bytesRead == 0) 
                {
                    Vorbis.vorbis_analysis_wrote(&state, 0);
                }
                else
                {
                    int i = 0;
                    float** inputBuffer = Vorbis.vorbis_analysis_buffer(&state, 1024);

                    for (i = 0; i < bytesRead; i += 4)
                    {
                        inputBuffer[0][i] = BitConverter.ToSingle(buffer, i);
                    }

                    Vorbis.vorbis_analysis_wrote(&state, i);
                }

                previousPostion = br.BaseStream.Position;

                while (Vorbis.vorbis_analysis_blockout(&state, &block) == 1)
                {
                    Vorbis.vorbis_analysis(&block, null);
                    Vorbis.vorbis_bitrate_addblock(&block);

                    ogg_packet dataOut = new ogg_packet();
                    while (Vorbis.vorbis_bitrate_flushpacket(&state, &dataOut) == 1)
                    {
                        byte[] rawData = new byte[dataOut.bytes.Value];
                        Marshal.Copy((IntPtr)dataOut.packet, rawData, 0, (int)dataOut.bytes.Value);
                        binaryWriter.Write(rawData);

                        if (buffer.Length < 1024 * 4)
                        {
                            endOfFile = true;
                        }
                    }
                }
            }

            Vorbis.vorbis_block_clear(&block);
            Vorbis.vorbis_dsp_clear(&state);
            Vorbis.vorbis_comment_clear(&comment);
            Vorbis.vorbis_info_clear(&info);

            br.Dispose();
            memoryStream.Dispose();

            byte[] finaldata = stream.ToArray();

            binaryWriter.Dispose();
            stream.Dispose();

            return finaldata;
        }

        public static unsafe byte[] Decode(byte[] data, long channels, long sampleRate, byte[] streamIdData, byte[] commentData, byte[] codebookData)
        {
            ogg_packet streamId = new ogg_packet();
            fixed (byte* ptr = streamIdData)
            {
                streamId.packet = ptr;
            }
            streamId.bytes = new CLong(streamIdData.Length);
            streamId.b_o_s = new CLong(1);
            streamId.e_o_s = new CLong(0);
            streamId.granulepos = 0;
            streamId.packetno = 0;

            ogg_packet commentPacket = new ogg_packet();
            fixed (byte* ptr = commentData)
            {
                commentPacket.packet = ptr;
            }
            commentPacket.bytes = new CLong(commentData.Length);
            commentPacket.b_o_s = new CLong(0);
            commentPacket.e_o_s = new CLong(0);
            commentPacket.granulepos = 0;
            commentPacket.packetno = 1;

            ogg_packet codebookPacket = new ogg_packet();
            fixed (byte* ptr = codebookData)
            {
                codebookPacket.packet = ptr;
            }
            codebookPacket.bytes = new CLong(codebookData.Length);
            codebookPacket.b_o_s = new CLong(0);
            codebookPacket.e_o_s = new CLong(0);
            commentPacket.granulepos = 0;
            codebookPacket.packetno = 2;

            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);
            
            while (stream.Position < stream.Length)
            {

            }

            vorbis_info vorbis_Info = new vorbis_info();
            vorbis_comment vorbis_Comment = new vorbis_comment();
            Vorbis.vorbis_info_init(&vorbis_Info);
            Vorbis.vorbis_comment_init(&vorbis_Comment);

            Vorbis.vorbis_synthesis_headerin(&vorbis_Info, &vorbis_Comment, &streamId);
            Vorbis.vorbis_synthesis_headerin(&vorbis_Info, &vorbis_Comment, &commentPacket);
            Vorbis.vorbis_synthesis_headerin(&vorbis_Info, &vorbis_Comment, &codebookPacket);

            vorbis_dsp_state state = new vorbis_dsp_state();
            vorbis_block block = new vorbis_block();
            if (Vorbis.vorbis_synthesis_init(&state, &vorbis_Info) == 0)
            {
                Vorbis.vorbis_block_init(&state, &block);
            }
            else
            {
                throw new Exception();
            }
            return null;
        }
    }

    internal static unsafe class VorbisEnc
    {
        private const string LibraryName = "vorbis";

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int vorbis_encode_init_vbr(vorbis_info* vi, long channels, long rate, float base_quality);
    }
}*/