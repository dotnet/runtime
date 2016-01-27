// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Nullstone
{
    public class Test
    {

        public static int Main()
        {
            Test t = new Test();
            t.Init();
            t.kernel();
            t.Finish();
            return 100;
        }

        public static bool isIdeal = true;

        static int[] zero = new int[50];
        static short[] a = new short[50];

        public void kernel()
        {
            short reg;


            short i1;
            short i2;
            short i3;
            short i4;
            short i5;
            short i6;
            short i7;
            short i8;
            short i9;
            short i10;
            short i11;
            short i12;


            i1 = (((short)1));
            i2 = (((short)1));
            i3 = (((short)1));
            i4 = (((short)1));
            i5 = (((short)1));
            i6 = (((short)1));
            i7 = (((short)1));
            i8 = (((short)1));
            i9 = (((short)1));
            i10 = (((short)1));
            i11 = (((short)1));
            i12 = (((short)1));

            reg = (short)(i1 << i2 << i3 << i4 << i5 << i6 << i7 << i8 << i9 << i10 << i11 << i12);

            //System.Console.WriteLine("reg" + reg);  

            a[0] = reg;

            return;

        }

        public void Init()
        {
            a[0] = 1;
            return;
        }

        public void Finish()
        {

            System.Console.WriteLine(a[0]);

            return;
        }
    }
}
