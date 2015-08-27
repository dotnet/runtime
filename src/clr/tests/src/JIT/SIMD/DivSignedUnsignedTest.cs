using System;
using System.Collections.Generic;
using System.Numerics;

namespace VectorMathTests
{
    class Program
    {



        static int Main(string[] args)
        {
            {
                var a = new Vector<uint>(1000000);
                var b = new Vector<uint>(0);
                var c = new Vector<uint>(1);
                var d = b - c;
                var e = d / a;
                if (e[0] != 4294)
                {
                    return 0;
                }
            }
            {
                var a = new Vector<int>(1000000);
                var b = new Vector<int>(0);
                var c = new Vector<int>(1);
                var d = b - c;
                var e = d / a;
                if (e[0] != 0)
                {
                    return 0;
                }
            }
            return 100;
        }
    }
}
