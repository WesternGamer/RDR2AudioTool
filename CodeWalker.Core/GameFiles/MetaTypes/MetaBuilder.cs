using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace CodeWalker.GameFiles
{
    public class MetaBuilder
    {

        List<MetaBuilderBlock> Blocks = new List<MetaBuilderBlock>();

        int MaxBlockLength = 0x4000; //TODO: figure what this should be!


        public MetaBuilderBlock EnsureBlock(MetaName type)
        {
            foreach (var block in Blocks)
            {
                if (block.StructureNameHash == type)
                {
                    if (block.TotalSize < MaxBlockLength)
                    {
                        return block;
                    }
                }
            }
            return AddBlock(type);
        }
        public MetaBuilderBlock AddBlock(MetaName type)
        {
            MetaBuilderBlock b = new MetaBuilderBlock();
            b.StructureNameHash = type;
            b.Index = Blocks.Count;
            Blocks.Add(b);
            return b;
        }



        public MetaBuilderPointer AddItem(MetaName type, byte[] data)
        {
            MetaBuilderBlock block = EnsureBlock(type);
            int brem = data.Length % 16;
            if (brem > 0)
            {
                int newlen = data.Length - brem + 16;
                byte[] newdata = new byte[newlen];
                Buffer.BlockCopy(data, 0, newdata, 0, data.Length);
                data = newdata; //make sure item size is multiple of 16... so pointers don't need sub offsets!
            }
            int idx = block.AddItem(data);
            MetaBuilderPointer r = new MetaBuilderPointer();
            r.BlockID = block.Index + 1;
            r.Offset = (idx * data.Length);
            r.Length = data.Length;
            return r;
        }

        public MetaBuilderPointer AddItemArray(MetaName type, byte[] data, int length)
        {
            MetaBuilderBlock block = EnsureBlock(type);
            int datalen = data.Length;
            int newlen = datalen;
            int lenrem = newlen % 16;
            if (lenrem != 0)
            {
                newlen += (16 - lenrem);
            }
            byte[] newdata = new byte[newlen];
            Buffer.BlockCopy(data, 0, newdata, 0, datalen);
            int offs = block.TotalSize;
            int idx = block.AddItem(newdata);
            MetaBuilderPointer r = new MetaBuilderPointer();
            r.BlockID = block.Index + 1;
            r.Offset = offs; //(idx * data.Length);;
            r.Length = length;
            return r;
        }

        public byte[] GetData()
        {
            int totlen = 0;
            for (int i = 0; i < Blocks.Count; i++)
            {
                totlen += Blocks[i].TotalSize;
            }
            byte[] data = new byte[totlen];
            int offset = 0;
            for (int i = 0; i < Blocks.Count; i++)
            {
                var block = Blocks[i];
                for (int j = 0; j < block.Items.Count; j++)
                {
                    var bdata = block.Items[j];
                    Buffer.BlockCopy(bdata, 0, data, offset, bdata.Length);
                    offset += bdata.Length;
                }
            }
            if (offset != data.Length)
            { }
            return data;
        }



       

    }


    public class MetaBuilderBlock
    {
        public MetaName StructureNameHash { get; set; }
        public List<byte[]> Items { get; set; } = new List<byte[]>();
        public int TotalSize { get; set; } = 0;
        public int Index { get; set; } = 0;

        public int AddItem(byte[] item)
        {
            int idx = Items.Count;
            Items.Add(item);
            TotalSize += item.Length;
            return idx;
        }

        public uint BasePointer
        {
            get
            {
                return (((uint)Index + 1) & 0xFFF);
            }
        }


        


    }

    public struct MetaBuilderPointer
    {
        public int BlockID { get; set; } //1-based id
        public int Offset { get; set; } //byte offset
        public int Length { get; set; } //for temp use...
        public uint Pointer
        {
            get
            {
                uint bidx = (((uint)BlockID) & 0xFFF);
                uint offs = (((uint)Offset) & 0xFFFFF) << 12;
                return bidx + offs;
            }
        }
    }


}
