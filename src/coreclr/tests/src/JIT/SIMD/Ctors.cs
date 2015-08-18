using System;
using System.Collections.Generic;
using System.Numerics;

namespace VectorMathTests
{
    class Program
    {

        static int Vector2Ctors()
        {
            Vector2 a = new Vector2(45, 12);
            if (a.X != 45 || a.Y != 12)
                return 0;
            a.X = 100;
            Vector2 b = new Vector2(65);

            if (b.X != 65 || b.Y != 65)
                return 0;
            return 100;

        }

        static int Vector3Ctors()
        {
            Vector3 a = new Vector3(0, 1, 2);
            if (a.X != 0 || a.Y != 1 || a.Z != 2)
                return 0;
            Vector3 b = new Vector3(2);
            if (b.X != 2 || b.Y != 2 || b.Z != 2)
                return 0;
            Vector2 q = new Vector2(10, 1);
            Vector3 c = new Vector3(q, 5);
            if (c.X != q.X || c.Y != q.Y || c.Z != 5)
                return 0;
            return 100;

        }

        static int Vector4Ctors()
        {
            Vector4 a = new Vector4(0, 1, 2, 3);
            if (a.X != 0 || a.Y != 1 || a.Z != 2 || a.W != 3)
                return 0;
            Vector4 b = new Vector4(2);
            if (b.X != 2 || b.Y != 2 || b.Z != 2 || b.W != 2)
                return 0;
            Vector2 q = new Vector2(10, 1);
            Vector4 c = new Vector4(q, 10, -1);
            if (c.X != q.X || c.Y != q.Y || c.Z != 10 || c.W != -1)
                return 0;
            Vector3 w = new Vector3(5);
            Vector4 d = new Vector4(w, 2);
            if (d.X != w.X || d.Y != w.Y || d.Z != w.Z || d.W != 2)
                return 0;
            return 100;
        }


        static int Main(string[] args)
        {
            if (Vector2Ctors() != 100 || Vector3Ctors() != 100 || Vector4Ctors() != 100)
                return 0;
            return 100;
        }
    }
}
