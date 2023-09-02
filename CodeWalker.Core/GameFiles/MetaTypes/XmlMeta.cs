using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;


namespace CodeWalker.GameFiles
{
    public class XmlMeta
    {
        public static byte[] GetData(XmlDocument doc, MetaFormat mformat, string fpathin)
        {
            switch (mformat)
            {
                case MetaFormat.Awc:
                    return GetAwcData(doc, fpathin);
            }
            return null;
        }

        public static byte[] GetAwcData(XmlDocument doc, string fpathin)
        {
            var awc = XmlAwc.GetAwc(doc, fpathin);
            if (awc.Streams == null) return null;
            return awc.Save();
        }

        public static string GetXMLFormatName(MetaFormat mformat)
        {
            switch (mformat)
            {
                case MetaFormat.Awc: return "AWC XML";
                default: return "XML";
            }
        }
        public static MetaFormat GetXMLFormat(string fnamel, out int trimlength)
        {
            var mformat = MetaFormat.RSC;
            trimlength = 4;
            if (fnamel.EndsWith(".awc.xml"))
            {
                mformat = MetaFormat.Awc;
            }

            return mformat;
        }
        
        private static void Write(int val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(int));
        }

        private static void Write(uint val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(uint));
        }

        private static void Write(short val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(short));
        }

        private static void Write(ushort val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(ushort));
        }

        private static void Write(float val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(float));
        }

        public static MetaHash GetHash(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            if (str.StartsWith("hash_"))
            {
                return (MetaHash) Convert.ToUInt32(str.Substring(5), 16);
            }
            else
            {
                JenkIndex.Ensure(str);
                return JenkHash.GenHash(str);
            }
        }


        private static string[] Split(string str, int maxChunkSize)
        {
            var chunks = new List<String>();

            for (int i = 0; i < str.Length; i += maxChunkSize)
            {
                chunks.Add(str.Substring(i, Math.Min(maxChunkSize, str.Length - i)));
            }

            return chunks.ToArray();
        }

        public static T[] ReadItemArray<T>(XmlNode node, string name) where T : IMetaXmlItem, new()
        {
            var vnode2 = node.SelectSingleNode(name);
            if (vnode2 != null)
            {
                var inodes = vnode2.SelectNodes("Item");
                if (inodes?.Count > 0)
                {
                    var vlist = new List<T>();
                    foreach (XmlNode inode in inodes)
                    {
                        var v = new T();
                        v.ReadXml(inode);
                        vlist.Add(v);
                    }
                    return vlist.ToArray();
                }
            }
            return null;
        }

        public static T[] ReadItemArrayNullable<T>(XmlNode node, string name) where T : IMetaXmlItem, new()
        {
            var vnode2 = node.SelectSingleNode(name);
            if (vnode2 != null)
            {
                var inodes = vnode2.SelectNodes("Item");
                if (inodes?.Count > 0)
                {
                    var vlist = new List<T>();
                    foreach (XmlNode inode in inodes)
                    {
                        if (inode.HasChildNodes)
                        {
                            var v = new T();
                            v.ReadXml(inode);
                            vlist.Add(v);
                        }
                        else
                        {
                            vlist.Add(default(T));
                        }
                    }
                    return vlist.ToArray();
                }
            }
            return null;
        }


        public static MetaHash[] ReadHashItemArray(XmlNode node, string name)
        {
            var vnode = node.SelectSingleNode(name);
            if (vnode != null)
            {
                var inodes = vnode.SelectNodes("Item");
                if (inodes?.Count > 0)
                {
                    var vlist = new List<MetaHash>();
                    foreach (XmlNode inode in inodes)
                    {
                        vlist.Add(GetHash(inode.InnerText));
                    }
                    return vlist.ToArray();
                }
            }
            return null;
        }
        public static string[] ReadStringItemArray(XmlNode node, string name)
        {
            var vnode = node.SelectSingleNode(name);
            if (vnode != null)
            {
                var inodes = vnode.SelectNodes("Item");
                if (inodes?.Count > 0)
                {
                    var vlist = new List<string>();
                    foreach (XmlNode inode in inodes)
                    {
                        vlist.Add(inode.InnerText);
                    }
                    return vlist.ToArray();
                }
            }
            return null;
        }
    }
}
