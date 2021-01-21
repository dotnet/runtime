// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;

class SmallStructEnreg
{
    struct S
    {
        public short i0;
        public short i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static S GetS1()
    {
        return new S();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static S GetS2()
    {
        S s = new S();
        s.i0 = 1;
        s.i1 = 2;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestS(S s)
    {}

    [MethodImpl(MethodImplOptions.NoInlining)]
    static S Test()
    {
        S s = new S();
        TestS(s);
        s = GetS1();
        TestS(s);
        s = GetS2();
        TestS(s);
        return s;
    }

    public static int Main(String[] args)
    {
        S s = Test();
        if (s.i0 != 1 || s.i1 != 2)
        {
            return 101;
        }
        return 100;
    }
}
