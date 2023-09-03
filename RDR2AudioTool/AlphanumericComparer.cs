using CodeWalker.GameFiles;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RDR2AudioTool
{
    public class AlphanumericComparer : IComparer<AwcStream>
    {
        public int Compare(AwcStream x, AwcStream y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            string[] xParts = Regex.Split(x.Name, "([0-9]+)");
            string[] yParts = Regex.Split(y.Name, "([0-9]+)");

            for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
            {
                if (i % 2 == 0)
                {
                    int textCompare = string.Compare(xParts[i], yParts[i], StringComparison.OrdinalIgnoreCase);
                    if (textCompare != 0)
                        return textCompare;
                }
                else
                {
                    int xNum = int.Parse(xParts[i]);
                    int yNum = int.Parse(yParts[i]);
                    int numCompare = xNum.CompareTo(yNum);
                    if (numCompare != 0)
                        return numCompare;
                }
            }

            return xParts.Length.CompareTo(yParts.Length);
        }
    }
}
