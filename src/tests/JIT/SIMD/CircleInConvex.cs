// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Test generates Points, builds ConvexHull and then find the biggest Circle inside it.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

using Point = System.Numerics.Vector2;


namespace ClassLibrary
{

    public class test
    {
        const float EPS = Single.Epsilon;
        const int steps = 100;
        const float INF = Single.PositiveInfinity;

        public static float vectMul(Point a, Point b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public struct Line
        {
            public float a, b, c;
        };

        static public float abs(float a)
        {
            return a > 0 ? a : -a;
        }

        static public float dist(float x, float y, Line l)
        {
            float r = abs(x * l.a + y * l.b + l.c);
            return r;
        }

        static public float min(float a, float b)
        {
            return a < b ? a : b;
        }

        static public float max(float a, float b)
        {
            return a > b ? a : b;
        }

        static public void swap(ref float a, ref float b)
        {
            float c = a;
            a = b;
            b = c;
        }
		
		
		// Calc the radius of a circle, with a center in (x, y), the is bounded with Lines. 
        static public float radius(float x, float y, List<Line> l)
        {
            int n = (int)l.Count;
            float res = INF;
            for (int i = 0; i < n; ++i)
            {
                float d = dist(x, y, l[i]);

                res = min(res, d);
            }

            return res;
        }

		// Find y and calc the radius of a circle, with a center in (x), tha is bounded with Lines.
        static public float y_radius(float x, List<Point> a, List<Line> l, out float yOut)
        {
            int n = (int)a.Count;
            float ly = INF, ry = -INF;
            for (int i = 0; i < n; ++i)
            {
                float x1 = a[i].X, x2 = a[(i + 1) % n].X, y1 = a[i].Y, y2 = a[(i + 1) % n].Y;

                if (x1 == x2) continue;
                if (x1 > x2)
                {
                    swap(ref x1, ref x2);
                    swap(ref y1, ref y2);
                }
                if (x1 <= x + EPS && x - EPS <= x2)
                {
                    float y = y1 + (x - x1) * (y2 - y1) / (x2 - x1);

                    ly = min(ly, y);
                    ry = max(ry, y);
                }
            }
            for (int sy = 0; sy < steps; ++sy)
            {
                float diff = (ry - ly) / 3;
                float y1 = ly + diff, y2 = ry - diff;
                float f1 = radius(x, y1, l), f2 = radius(x, y2, l);
                if (f1 < f2)
                    ly = y1;
                else
                    ry = y2;
            }
            yOut = ly;
            return radius(x, ly, l);
        }

        static public Boolean Check(List<Point> points)
        {
            float zn = vectMul((points[2] - points[0]), (points[1] - points[0]));
            for (int i = 2; i < points.Count; ++i)
            {
                float z = vectMul((points[i] - points[i - 2]), (points[i - 1] - points[i - 2]));
                if (z * zn < 0)
                {

                    return false;
                }
                if (zn == 0) // If we have some points on 1 line it is not error.
                {
                    zn = z;
                }
            }
            return true;
        }

        static public Boolean FindCircle(List<Point> points, out Point O, out float r)
        {
            O.X = 0;
            O.Y = 0;
            r = 0;
            if (points.Count < 3)
                return false;
            points.Add(points[0]);
            if (Check(points) == false)
            {
                return false;
            }
            int n = points.Count;
            List<Line> l = new List<Line>(n);
            for (int i = 0; i < n; ++i)
            {
                Line currL = new Line();
                currL.a = points[i].Y - points[(i + 1) % n].Y;
                currL.b = points[(i + 1) % n].X - points[i].X;

                float sq = (float)System.Math.Sqrt(currL.a * currL.a + currL.b * currL.b);
                if (sq < EPS)
                    continue;
                currL.a /= sq;
                currL.b /= sq;

                currL.c = -(currL.a * points[i].X + currL.b * points[i].Y);
                l.Add(currL);
            }

            float lx = INF, rx = -INF;
            for (int i = 0; i < n; ++i)
            {
                lx = min(lx, points[i].X);
                rx = max(rx, points[i].X);
            }

            for (int sx = 0; sx < steps; ++sx)
            {
                float diff = (rx - lx) / 3;
                float x1 = lx + diff, x2 = rx - diff;
                float xOut;
                float f1 = y_radius(x1, points, l, out xOut), f2 = y_radius(x2, points, l, out xOut);
                if (f1 < f2)
                    lx = x1;
                else
                    rx = x2;
            }
            float y;
            float ans = y_radius(lx, points, l, out y);
            O.X = lx;
            O.Y = y;
            r = ans;
            return true;
        }


        static int cmp(Point a, Point b)
        {
            if (a.X == b.X)
                return a.Y < b.Y ? 1 : a.Y > b.Y ? -1 : 0;
            else
                return a.X < b.X ? 1 : a.X > b.X ? -1 : 0;
        }

        static bool cw(Point a, Point b, Point c)
        {
            return a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y) < 0;
        }

        static bool ccw(Point a, Point b, Point c)
        {
            return a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y) > 0;
        }

        static void convex_hull(List<Point> a)
        {
            if (a.Count == 1) return;
            a.Sort(cmp);
            Point p1 = a[0], p2 = a.Last();
            List<Point> up = new List<Point>(), down = new List<Point>();
            up.Add(p1);
            down.Add(p1);
            for (int i = 1; i < a.Count; ++i)
            {
                if (i == a.Count - 1 || cw(p1, a[i], p2))
                {
                    while (up.Count >= 2 && !cw(up[up.Count - 2], up[up.Count - 1], a[i]))
                        up.RemoveAt(up.Count - 1);
                    up.Add(a[i]);
                }
                if (i == a.Count - 1 || ccw(p1, a[i], p2))
                {
                    while (down.Count >= 2 && !ccw(down[down.Count - 2], down[down.Count - 1], a[i]))
                        down.RemoveAt(down.Count - 1);
                    down.Add(a[i]);
                }
            }
            a.Clear();
            for (int i = 0; i < up.Count; ++i)
                a.Add(up[i]);
            for (int i = down.Count - 2; i > 0; --i)
                a.Add(down[i]);
        }

        static float NextFloat(Random random)
        {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            double exponent = Math.Pow(2.0, random.Next(-32, 32));
            return (float)(mantissa * exponent);
        }

        static int Main(string[] args)
        {
            List<Point> points = new List<Point>();
            Random random = new Random(13);
            for (int i = 0; i < 100; ++i)
            {
                Point p;
                p.X = NextFloat(random);
                p.Y = NextFloat(random);
                points.Add(p);
            }
            convex_hull(points);
            Point O;
            float r;
            FindCircle(points, out O, out r);

            float expRes = 1191233374.188854F;
            float ulp    =             384.0F;
            if (Math.Abs(r - expRes) <= ulp)
                return 100;
            return 0;
        }
    }
}
