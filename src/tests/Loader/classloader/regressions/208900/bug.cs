// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for bug VSWhidbey 208900
// "Corrupt OBJECTREF when calling a virtual generic method instantiated at a struct which returns that struct"

#pragma warning disable 0414
using System;

using Xunit;

struct MyStruct
{
    public MyStruct(object _f1, int _f2) { f1 = _f1; f2 = _f2; }
    object f1;
    int f2;
}


class M
{
   public virtual U GenericMethod<U>(U x1) {  return x1; }
}

public class Test_bug
{
    [Fact]
    public static void TestEntryPoint()
    {
        M obj = new M();
        MyStruct myStruct = new MyStruct("obj", 787980);
        Assert.True(obj.GenericMethod<MyStruct>(myStruct).Equals(myStruct));
    }
}
