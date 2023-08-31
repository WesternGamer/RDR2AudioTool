using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CodeWalker.GameFiles
{
    public class MetaXml : MetaXmlBase
    {
        public static string GetXml(AwcFile awc, out string filename, string outputfolder)
        {
            var fn = (awc?.Name) ?? "";
            filename = fn + ".xml";
            return AwcXml.GetXml(awc, outputfolder);
        }
    }

    public class MetaXmlBase
    {
        public const string XmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

        public static void Indent(StringBuilder sb, int indent)
        {
            for (int i = 0; i < indent; i++)
            {
                sb.Append(" ");
            }
        }
        public static void ErrorXml(StringBuilder sb, int indent, string msg)
        {
            Indent(sb, indent);
            sb.Append("<error>");
            sb.Append(msg);
            sb.Append("</error>");
            sb.AppendLine();
        }
        public static void OpenTag(StringBuilder sb, int indent, string name, bool appendLine = true, string metaName = "")
        {
            Indent(sb, indent);
            sb.Append("<");
            sb.Append(name);
            if (string.IsNullOrWhiteSpace(metaName))
            {
                sb.Append(">");
            }
            else
            {
                sb.Append(" name=\"" + metaName + "\">");
            }
            if (appendLine) sb.AppendLine();
        }
        public static void CloseTag(StringBuilder sb, int indent, string name, bool appendLine = true)
        {
            Indent(sb, indent);
            sb.Append("</");
            sb.Append(name);
            sb.Append(">");
            if (appendLine) sb.AppendLine();
        }
        public static void ValueTag(StringBuilder sb, int indent, string name, string val, string attr = "value")
        {
            Indent(sb, indent);
            sb.Append("<");
            sb.Append(name);
            sb.Append(" ");
            sb.Append(attr);
            sb.Append("=\"");
            sb.Append(val);
            sb.Append("\" />");
            sb.AppendLine();
        }
        public static void OneLineTag(StringBuilder sb, int indent, string name, string text)
        {
            Indent(sb, indent);
            sb.Append("<");
            sb.Append(name);
            sb.Append(">");
            sb.Append(text);
            sb.Append("</");
            sb.Append(name);
            sb.Append(">");
            sb.AppendLine();
        }
        public static void SelfClosingTag(StringBuilder sb, int indent, string val)
        {
            Indent(sb, indent);
            sb.Append("<");
            sb.Append(val);
            sb.Append(" />");
            sb.AppendLine();
        }
        public static void StringTag(StringBuilder sb, int indent, string name, string text)
        {
            if (!string.IsNullOrEmpty(text)) OneLineTag(sb, indent, name, text);
            else SelfClosingTag(sb, indent, name);
        }

        public static void WriteRawArrayContent<T>(StringBuilder sb, T[] arr, int ind, Func<T, string> formatter = null, int arrRowSize = 10) where T : struct
        {
            var aCount = arr?.Length ?? 0;
            for (int n = 0; n < aCount; n++)
            {
                var col = n % arrRowSize;
                if (col == 0) Indent(sb, ind);
                if (col > 0) sb.Append(" ");
                string str = (formatter != null) ? formatter(arr[n]) : arr[n].ToString();
                sb.Append(str);
                bool lastcol = (col == (arrRowSize - 1));
                bool lastn = (n == (aCount - 1));
                if (lastcol || lastn) sb.AppendLine();
            }
        }
        public static void WriteRawArray<T>(StringBuilder sb, T[] arr, int ind, string name, string typeName, Func<T, string> formatter = null, int arrRowSize = 10) where T : struct
        {
            var aCount = arr?.Length ?? 0;
            //var arrRowSize = 10;
            var aind = ind + 1;
            var arrTag = name;// + " itemType=\"" + typeName + "\"";
            if (aCount > 0)
            {
                if (aCount <= arrRowSize)
                {
                    OpenTag(sb, ind, arrTag, false);
                    for (int n = 0; n < aCount; n++)
                    {
                        if (n > 0) sb.Append(" ");
                        string str = (formatter != null) ? formatter(arr[n]) : arr[n].ToString();
                        sb.Append(str);
                    }
                    CloseTag(sb, 0, name);
                }
                else
                {
                    OpenTag(sb, ind, arrTag);
                    for (int n = 0; n < aCount; n++)
                    {
                        var col = n % arrRowSize;
                        if (col == 0) Indent(sb, aind);
                        if (col > 0) sb.Append(" ");
                        string str = (formatter != null) ? formatter(arr[n]) : arr[n].ToString();
                        sb.Append(str);
                        bool lastcol = (col == (arrRowSize - 1));
                        bool lastn = (n == (aCount - 1));
                        if (lastcol || lastn) sb.AppendLine();
                    }
                    CloseTag(sb, ind, name);
                }
            }
            else
            {
                SelfClosingTag(sb, ind, arrTag);
            }
        }
        public static void WriteItemArray<T>(StringBuilder sb, T[] arr, int ind, string name, string typeName, Func<T, string> formatter) where T : struct
        {
            var aCount = arr?.Length ?? 0;
            var arrTag = name;// + " itemType=\"Hash\"";
            var aind = ind + 1;
            if (aCount > 0)
            {
                OpenTag(sb, ind, arrTag);
                for (int n = 0; n < aCount; n++)
                {
                    Indent(sb, aind);
                    sb.Append("<Item>");
                    sb.Append(formatter(arr[n]));
                    sb.AppendLine("</Item>");
                }
                CloseTag(sb, ind, name);
            }
            else
            {
                SelfClosingTag(sb, ind, arrTag);
            }
        }
        public static void WriteItemArray<T>(StringBuilder sb, T[] arr, int ind, string name) where T : IMetaXmlItem
        {
            var itemCount = arr?.Length ?? 0;
            if (itemCount > 0)
            {
                OpenTag(sb, ind, name);
                var cind = ind + 1;
                var cind2 = ind + 2;
                for (int i = 0; i < itemCount; i++)
                {
                    if (arr[i] != null)
                    {
                        OpenTag(sb, cind, "Item");
                        arr[i].WriteXml(sb, cind2);
                        CloseTag(sb, cind, "Item");
                    }
                    else
                    {
                        SelfClosingTag(sb, cind, "Item");
                    }
                }
                CloseTag(sb, ind, name);
            }
            else
            {
                SelfClosingTag(sb, ind, name);
            }
        }
        public static void WriteCustomItemArray<T>(StringBuilder sb, T[] arr, int ind, string name) where T : IMetaXmlItem
        {
            var itemCount = arr?.Length ?? 0;
            if (itemCount > 0)
            {
                OpenTag(sb, ind, name);
                var cind = ind + 1;
                for (int i = 0; i < itemCount; i++)
                {
                    if (arr[i] != null)
                    {
                        arr[i].WriteXml(sb, cind);
                    }
                }
                CloseTag(sb, ind, name);
            }
            else
            {
                SelfClosingTag(sb, ind, name);
            }
        }
        public static void WriteHashItemArray(StringBuilder sb, MetaHash[] arr, int ind, string name)
        {
            var itemCount = arr?.Length ?? 0;
            if (itemCount > 0)
            {
                OpenTag(sb, ind, name);
                var cind = ind + 1;
                for (int i = 0; i < itemCount; i++)
                {
                    var iname = HashString(arr[i]);
                    StringTag(sb, cind, "Item", iname);
                }
                CloseTag(sb, ind, name);
            }
            else
            {
                SelfClosingTag(sb, ind, name);
            }
        }

        public static string FormatHash(MetaHash h) //for use with WriteItemArray
        {
            var str = JenkIndex.TryGetString(h);
            if (!string.IsNullOrEmpty(str)) return str;
            return HashString(h);// "hash_" + h.Hex;
        }
        public static string FormatVector2(Vector2 v) //for use with WriteItemArray
        {
            return FloatUtil.GetVector2String(v);
        }
        public static string FormatVector3(Vector3 v) //for use with WriteItemArray
        {
            return FloatUtil.GetVector3String(v);
        }
        public static string FormatVector4(Vector4 v) //for use with WriteItemArray
        {
            return FloatUtil.GetVector4String(v);
        }
        public static string FormatHexByte(byte b)
        {
            return Convert.ToString(b, 16).ToUpperInvariant().PadLeft(2, '0'); //hex byte array
        }

        public static string HashString(MetaName h)
        {
            uint uh = (uint)h;
            if (uh == 0) return "";

            string str;
            if (MetaNames.TryGetString(uh, out str)) return str;

            str = JenkIndex.TryGetString(uh);
            if (!string.IsNullOrEmpty(str)) return str;

            return "hash_" + uh.ToString("X").PadLeft(8, '0');
        }
        public static string HashString(MetaHash h)
        {
            if (h == 0) return "";

            var str = JenkIndex.TryGetString(h);

            if (string.IsNullOrEmpty(str))
            {
                if (MetaNames.TryGetString(h, out str)) return str;
            }

            if (!string.IsNullOrEmpty(str)) return str;
            return "hash_" + h.Hex;
        }
        public static string HashString(TextHash h)
        {
            uint uh = h.Hash;
            if (uh == 0) return "";

            return "hash_" + uh.ToString("X").PadLeft(8, '0');
        }


        public static string UintString(uint h)
        {
            return "0x" + h.ToString("X");
        }

        public static string XmlEscape(string unescaped)
        {
            if (unescaped == null) return null;
            XmlDocument doc = new XmlDocument();
            XmlNode node = doc.CreateElement("root");
            node.InnerText = unescaped;
            var escaped = node.InnerXml.Replace("\"", "&quot;");
            if (escaped != unescaped)
            { }
            return escaped;
        }

        public enum XmlTagMode
        {
            None = 0,
            Structure = 1,
            Item = 2,
            ItemAndType = 3,
        }
    }

    public interface IMetaXmlItem
    {
        void WriteXml(StringBuilder sb, int indent);
        void ReadXml(XmlNode node);
    }

    public enum MetaFormat
    {
        XML = 0,
        RSC = 1,
        PSO = 2,
        RBF = 3,
        CacheFile = 4,
        AudioRel = 5,
        Ynd = 6,
        Ynv = 7,
        Ycd = 8,
        Ybn = 9,
        Ytd = 10,
        Ydr = 11,
        Ydd = 12,
        Yft = 13,
        Ypt = 14,
        Yld = 15,
        Yed = 16,
        Ywr = 17,
        Yvr = 18,
        Awc = 19,
        Fxc = 20,
        Heightmap = 21,
        Ypdb = 22,
        Mrf = 23,
    }
}
