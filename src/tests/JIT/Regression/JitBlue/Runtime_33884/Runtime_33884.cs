// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test showed CSE issues with struct return retyping.

using System;
using System.Runtime.CompilerServices;

struct RefWrapper
{
    public Object a; // a ref field
}

class TestStructs
{
    static RefWrapper[] arr;

    public static RefWrapper GetElement() // 8 byte size return will be retyped as a ref.
    {
        return arr[0];
    }

    public static int Main()
    {
        RefWrapper a = new RefWrapper();
        arr = new RefWrapper[1];
        arr[0] = a;

        RefWrapper e = GetElement(); // force struct retyping to ref.
        arr[0] = e; // a struct typed copy.
        return 100;
    }
}
