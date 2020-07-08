// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    struct V3
    {
        long a;
        long b;
        long c;
        long d;
        int e;
        short f;
        byte g;

        public V3(short bar)
        {
            a = 1;
            b = -2;
            c = 3;
            d = -4;
            e = 5;
            f = (short)-6;
            g = 7;
        }

        public Boolean Validate()
        {
            return (a == 1 && b == -2 && c == 3 && d == -4 && e == 5 && f == -6 && g == 7);
        }


        //	public override Boolean Equals(Object o) { return (o instanceof V3); }
        //	public override int GetHashCode() { return 0; }
    }


    public class bug
    {
        public static int Main(String[] args)
        {

            int size = 32;
            V3[] tmpV3Array = new V3[size];
            Object[] VarArray = new Object[size];
            for (int i = 0; i < size; i++)
                VarArray[i] = (new V3(1));
            Array.Copy(VarArray, tmpV3Array, size);
            for (int i = 0; i < size; i++)
                if (!tmpV3Array[i].Validate())
                    throw new Exception("tmpV3Array[" + i + "] didn't validate correctly!  got: " + tmpV3Array[i] + "  expected: " + VarArray[i]);

            return 100;

        }
    }
}
