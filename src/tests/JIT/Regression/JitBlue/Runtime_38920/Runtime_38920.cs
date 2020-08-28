// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

class TestStructs
{
    static int exitStatus = 100;

    struct StructA
    {
        public int a;
    }

    // Make both structs with >4 fields to prevent struct promoting and force byref passing.
    struct CastFromStruct20
    {
        int z;
        public StructA structField;
        public int b;
        public int c;
        int q;
    }

    struct CastToStruct8
    {
        bool a;
        bool b;
        bool c;
        bool d;
        public int e; // Overlaps with b in CastFromStruct
    }

    struct CastToStruct12
    {
        bool a;
        bool b;
        bool c;
        bool d;
        public int e; // Overlaps with b in CastFromStruct
        public int f; // Overlaps with c in CastFromStruct
    }


    // our src local var has to be an implicit byref, otherwise we won't try to recover struct handle from it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void UnsafeCastFromAPrimitiveStructFieldToStruct(CastFromStruct20 impByRefStruct20, int b, int c)
    {
        CastToStruct8  to8  = new CastToStruct8();
        CastToStruct12 to12 = new CastToStruct12();

        // possible incorrect CSE def of impByRefStruct20.structField
        //
        to8 = Unsafe.As<StructA, CastToStruct8>(ref impByRefStruct20.structField);

        for (int i =0; i<10; i++)
        {
            // possible incorrect CSE use of impByRefStruct20.structField
            //
            to12 = Unsafe.As<StructA, CastToStruct12>(ref impByRefStruct20.structField);
        }

        // Check that we modified to8.e correctly
        if (to8.e != b)
        {
            Console.WriteLine("to8.e has the wrong value: " + to8.e + " expected " + b);
            exitStatus = -1;
        }

        // Check that we modified to12.e correctly
        if (to12.e != b)
        {
            Console.WriteLine("to12.e has the wrong value: " + to12.e + " expected " + b);
            exitStatus = -1;
        }

        // Check that we modified to12.f correctly
        if (to12.f != c)
        {
            Console.WriteLine("to12.f has the wrong value: " + to12.f + " expected " + c);
            exitStatus = -1;
        }
    }

    public static int Main()
    {
        int b = 1;
        int c = 2;

        CastFromStruct20 s = new CastFromStruct20();

        s.b = b;
        s.c = c;
        UnsafeCastFromAPrimitiveStructFieldToStruct(s, b, c);

        s.b = ++b;  // 2
        s.c = ++c;  // 3
        UnsafeCastFromAPrimitiveStructFieldToStruct(s, b, c);

        b = b * b;
        c = c * c;
        s.b = b;    // 4
        s.c = c;    // 9
        UnsafeCastFromAPrimitiveStructFieldToStruct(s, b, c);

        if (exitStatus == 100)
        {
            Console.WriteLine("Test Passed");
        }
        else
        {
            Console.WriteLine("FAILED");
        }

        return exitStatus;
    }
}
