// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test originally showed unsupported PUTARG_STK(OBJ(LCL_FLD_ADDR)) when both OBJ and LCL_FLD_ADDR were contained.
// codegenarmarch `genPutArgStk` did not expect that.

using System;
using System.Runtime.CompilerServices;
using Xunit;

struct Struct16bytes
{
    public int a;
    public int b;
    public int c;
    public int d;
}

struct StructWithStructField
{
    public bool a;
    public Struct16bytes structField;
}

public class DevDiv_714266
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    int foo(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, Struct16bytes s)
    {
        Console.WriteLine(s.a);
        return s.a;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        StructWithStructField s = new StructWithStructField();
        s.structField.a = 100;

        DevDiv_714266 test = new DevDiv_714266();
        return test.foo(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, s.structField);
    }

}
