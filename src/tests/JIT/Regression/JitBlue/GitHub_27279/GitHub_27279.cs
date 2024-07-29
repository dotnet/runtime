// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;
public class GitHub_27279
{
    [Fact]
    public unsafe static int TestEntryPoint()
    {
        bool res = Unsafe.IsAddressLessThan(ref Unsafe.AsRef<byte>((void*)(-1)), ref Unsafe.AsRef<byte>((void*)(1)));
        Console.WriteLine(res.ToString());
        if (res)
        {
            return 101;
        }        
        return 100;
    }
}
