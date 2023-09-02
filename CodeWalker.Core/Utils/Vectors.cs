using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeWalker.GameFiles;

namespace CodeWalker
{
    public static class Vectors
    {
        public static Vector3 XYZ(this Vector4 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static Vector3 Round(this Vector3 v)
        {
            return new Vector3((float)Math.Round(v.X), (float)Math.Round(v.Y), (float)Math.Round(v.Z));
        }

        public static Vector3 Floor(this Vector3 v)
        {
            return new Vector3((float)Math.Floor(v.X), (float)Math.Floor(v.Y), (float)Math.Floor(v.Z));
        }
        public static Vector3 Ceiling(this Vector3 v)
        {
            return new Vector3((float)Math.Ceiling(v.X), (float)Math.Ceiling(v.Y), (float)Math.Ceiling(v.Z));
        }

        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }

        public static int CompareTo(this Vector3 a, Vector3 b)
        {
            int c;
            c = a.X.CompareTo(b.X); if (c != 0) return c;
            c = a.Y.CompareTo(b.Y); if (c != 0) return c;
            c = a.Z.CompareTo(b.Z); if (c != 0) return c;
            return 0;
        }


        public static Vector4 Floor(this Vector4 v)
        {
            return new Vector4((float)Math.Floor(v.X), (float)Math.Floor(v.Y), (float)Math.Floor(v.Z), (float)Math.Floor(v.W));
        }
        public static Vector4 Ceiling(this Vector4 v)
        {
            return new Vector4((float)Math.Ceiling(v.X), (float)Math.Ceiling(v.Y), (float)Math.Ceiling(v.Z), (float)Math.Ceiling(v.W));
        }

        public static Vector4 Abs(this Vector4 v)
        {
            return new Vector4(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z), Math.Abs(v.W));
        }

        public static Quaternion ToQuaternion(this Vector4 v)
        {
            return new Quaternion(v.X, v.Y, v.Z, v.W);
        }
    }


    public struct Vector2I
    {
        public int X;
        public int Y;

        public Vector2I(int x, int y)
        {
            X = x;
            Y = y;
        }
        public Vector2I(Vector2 v)
        {
            X = (int)Math.Floor(v.X);
            Y = (int)Math.Floor(v.Y);
        }

        public override string ToString()
        {
            return X.ToString() + ", " + Y.ToString();
        }


        public static Vector2I operator +(Vector2I a, Vector2I b)
        {
            return new Vector2I(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2I operator -(Vector2I a, Vector2I b)
        {
            return new Vector2I(a.X - b.X, a.Y - b.Y);
        }

    }




   


    

    public static class LineMath
    {


        public static float PointSegmentDistance(ref Vector3 v, ref Vector3 a, ref Vector3 b)
        {
            //https://stackoverflow.com/questions/4858264/find-the-distance-from-a-3d-point-to-a-line-segment
            Vector3 ab = b - a;
            Vector3 av = v - a;

            if (Vector3.Dot(av, ab) <= 0.0f)// Point is lagging behind start of the segment, so perpendicular distance is not viable.
            {
                return av.Length();         // Use distance to start of segment instead.
            }

            Vector3 bv = v - b;
            if (Vector3.Dot(bv, ab) >= 0.0f)// Point is advanced past the end of the segment, so perpendicular distance is not viable.
            {
                return bv.Length();         // Use distance to end of the segment instead.
            }

            return Vector3.Cross(ab, av).Length() / ab.Length();// Perpendicular distance of point to segment.
        }

        public static Vector3 PointSegmentNormal(ref Vector3 v, ref Vector3 a, ref Vector3 b)
        {
            Vector3 ab = b - a;
            Vector3 av = v - a;

            if (Vector3.Dot(av, ab) <= 0.0f)
            {
                return Vector3.Normalize(av);
            }

            Vector3 bv = v - b;
            if (Vector3.Dot(bv, ab) >= 0.0f)
            {
                return Vector3.Normalize(bv);
            }

            return Vector3.Normalize(Vector3.Cross(Vector3.Cross(ab, av), ab));
        }

        public static float PointRayDist(ref Vector3 p, ref Vector3 ro, ref Vector3 rd)
        {
            return Vector3.Cross(rd, p - ro).Length();
        }

    }


    public static class TriangleMath
    {

        public static float AreaPart(ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, out float angle)
        {
            var va = v2 - v1;
            var vb = v3 - v1;
            var na = Vector3.Normalize(va);
            var nb = Vector3.Normalize(vb);
            var a = va.Length();
            var b = vb.Length();
            var c = Math.Acos(Vector3.Dot(na, nb));
            var area = (float)(0.5 * a * b * Math.Sin(c));
            angle = (float)Math.Abs(c);
            return area;
        }

        public static float Area(ref Vector3 v1, ref Vector3 v2, ref Vector3 v3)
        {
            var a1 = AreaPart(ref v1, ref v2, ref v3, out float t1);
            var a2 = AreaPart(ref v2, ref v3, ref v1, out float t2);
            var a3 = AreaPart(ref v3, ref v1, ref v2, out float t3);
            var fp = (float)Math.PI;
            var d1 = Math.Min(t1, Math.Abs(t1 - fp));
            var d2 = Math.Min(t2, Math.Abs(t2 - fp));
            var d3 = Math.Min(t3, Math.Abs(t3 - fp));
            if ((d1 >= d2) && (a1 != 0))
            {
                if ((d1 >= d3) || (a3 == 0))
                {
                    return a1;
                }
                else
                {
                    return a3;
                }
            }
            else
            {
                if ((d2 >= d3) || (a3 == 0))
                {
                    return a2;
                }
                else
                {
                    return a3;
                }
            }
        }

    }



}