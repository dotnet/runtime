// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_73951
{
    [ThreadStatic]
    public static IRuntime s_rt;
    [ThreadStatic]
    public static S1 s_17;

    public static ushort s_result;

    [Fact]
    public static int TestEntryPoint()
    {
        Problem(new Runtime());

        return s_result == 0 ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Problem(IRuntime rt)
    {
        s_rt = rt;
        S0 vr21 = s_17.F1;
        new S1(new object()).M105(vr21);

        var vr22 = new C0(vr21.F3);
        s_rt.Capture(vr22.F1);
    }

    public class C0
    {
        public ushort F1;
        public C0(ushort f1)
        {
            F1 = f1;
        }
    }

    public struct S0
    {
        public uint F1;
        public int F2;
        public byte F3;
    }

    public struct S1
    {
        public object F0;
        public S0 F1;
        public S1(object f0) : this()
        {
            F0 = f0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public S1 M105(S0 arg0)
        {
            return this;
        }
    }

    public interface IRuntime
    {
        void Capture(ushort value);
    }

    public class Runtime : IRuntime
    {
        public void Capture(ushort value) => s_result = value;
    }
}
