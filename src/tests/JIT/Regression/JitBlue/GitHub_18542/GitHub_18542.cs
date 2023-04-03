// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class C
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void M()
    {
    }
}

public struct S1
{
    private C _c;

    public S1(C c) => _c = c;
    public void M() => _c.M();
}

public struct S2
{
    private S1 _s;

    public S2(S1 s) => _s = s;
    public void M() => _s.M();
}

public struct S3
{
    private S2 _s;

    public S3(S2 s) => _s = s;
    public void M() => _s.M();
}

public struct S4
{
    private S3 _s;

    public S4(S3 s) => _s = s;
    public void M() => _s.M();
}

public struct S5
{
    private S4 _s;

    public S5(S4 s) => _s = s;
    public void M() => _s.M();
}

public static class GitHub_18542
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ViaClass()
    {
        var c = new C();
        c.M();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ViaStruct1()
    {
        var s1 = new S1(new C());
        s1.M();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ViaStruct2()
    {
        var s2 = new S2(new S1(new C()));
        s2.M();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ViaStruct3()
    {
        var s3 = new S3(new S2(new S1(new C())));
        s3.M();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ViaStruct4()
    {
        var s4 = new S4(new S3(new S2(new S1(new C()))));
        s4.M();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ViaStruct5()
    {
        var s5 = new S5(new S4(new S3(new S2(new S1(new C())))));
        s5.M();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        ViaClass();
        ViaStruct1();
        ViaStruct2();
        ViaStruct3();
        ViaStruct4();
        ViaStruct5();
        return 100;
    }
}
