using System;

using TC = System.ComponentModel.TypeConverterAttribute;
using EXP = System.ComponentModel.ExpandableObjectConverter;

namespace CodeWalker.GameFiles
{
    [TC(typeof(EXP))] public struct MetaHash
    {
        public uint Hash { get; set; }

        public string Hex
        {
            get
            {
                return Hash.ToString("X").PadLeft(8, '0');
            }
        }

        public float Float
        {
            get
            {
                return BitConverter.ToSingle(BitConverter.GetBytes(Hash));
            }
        }

        public short Short1
        {
            get
            {
                return (short)(Hash & 0xFFFF);
            }
        }
        public short Short2
        {
            get
            {
                return (short)((Hash >> 16) & 0xFFFF);
            }
        }


        public MetaHash(uint h) { Hash = h; }

        public override string ToString()
        {

            return Hex;
        }

        public string ToCleanString()
        {
            if (Hash == 0) return string.Empty;
            return ToString();
        }

        public static implicit operator uint(MetaHash h)
        {
            return h.Hash;  //implicit conversion
        }

        public static implicit operator MetaHash(uint v)
        {
            return new MetaHash(v);
        }
    }

    
    [TC(typeof(EXP))] public struct TextHash
    {
        public uint Hash { get; set; }

        public string Hex
        {
            get
            {
                return Hash.ToString("X");
            }
        }

        public TextHash(uint h) { Hash = h; }

        public override string ToString()
        {
            return Hex;
        }


        public static implicit operator uint(TextHash h)
        {
            return h.Hash;  //implicit conversion
        }

        public static implicit operator TextHash(uint v)
        {
            return new TextHash(v);
        }
    }








}