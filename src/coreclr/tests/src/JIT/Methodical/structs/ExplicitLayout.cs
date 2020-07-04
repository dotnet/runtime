// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class ExplicitLayout
{
#pragma warning disable 618
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    internal unsafe struct TestStruct
    {
        public const int SIZE = 32;

        [FieldOffset(0)]
        private fixed byte _data[SIZE];

        [FieldOffset(0), MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public Guid Guid1;

        [FieldOffset(16), MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public Guid Guid2;
    }
#pragma warning restore 618

    internal class Program
    {
        private static int Main()
        {
            int returnVal = 100;

            TestStruct t = new TestStruct();
            t.Guid1 = Guid.NewGuid();
            t.Guid2 = t.Guid1;

            if (t.Guid1 != t.Guid2)
            {
                Console.WriteLine("FAIL self-copy");
                returnVal = -1;
            }

            TestStruct t2 = new TestStruct();
            Guid newGuid = Guid.NewGuid();
            t2.Guid1 = newGuid;
            t2.Guid2 = newGuid;

            if (t2.Guid1 != t2.Guid2)
            {
                Console.WriteLine("FAIL other-copy");
                returnVal = -1;
            }

            return returnVal;
        }
    }
}
