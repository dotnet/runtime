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
                var a = new System.Numerics.Vector<int>(1);
                object b = a;
                if (b is System.Numerics.Vector<int>)
                {
                    var c = (System.Numerics.Vector<int>)b;
                    if (a != c)
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
            {
                var a = new System.Numerics.Vector4(1);
                object b = a;
                if (b is System.Numerics.Vector4)
                {
                    var c = (System.Numerics.Vector4)b;
                    if (a != c)
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
            return 100;
        }
    }
}
