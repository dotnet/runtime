// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
// The legendary 37-byte value class.
namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    internal struct V3
    {
        internal long a;
        internal long b;
        internal long c;
        internal long d;
        internal int e;
        internal short f;
        internal byte g;

        public V3(int unused_param)
        {
            a = 1;
            b = -2;
            c = 3;
            d = -4;
            e = 5;
            f = (short)-6;
            g = 7;
        }

        public bool Validate()
        {
            return a == 1 && b == -2 && c == 3 && d == -4 && e == 5 && f == -6 && g == 7;
        }

        public override bool Equals(Object o) { return false; }
        public override int GetHashCode() { return 0; }
    }

    public class jitBug
    {
        [Fact]
        public static int TestEntryPoint()
        {
            V3[] V3Array = new V3[5];
            for (int i = 0; i < V3Array.Length; i++)
                V3Array[i] = new V3();

            /*
            V3[] clone = null;
    		
            clone = (V3[]) V3Array.Clone();
    		
            if (clone.length != V3Array.length)
                throw new Exception("V3[] length mismatch!  cloned length: "+clone.length);
            for(int i=0; i<V3Array.length; i++) {
                clone[i].Validate();
            }
            Console.WriteLine("V3 array test worked");
            /* */

            return 100;
        }
    }
}
