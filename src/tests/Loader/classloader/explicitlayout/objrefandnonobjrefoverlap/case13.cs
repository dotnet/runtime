// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

public class Test
{
    public struct WithORefs
    {
        public object F;
    }

    public struct WithNoORefs
    {
        public int F;
    }

    public ref struct WithByRefs
    {
        public ref int F;
    }

    [StructLayout(LayoutKind.Explicit)]
    public ref struct Explicit1
    {
        [FieldOffset(0)] public Inner1 Field1;
        public ref struct Inner1
        {
            public WithORefs Field2;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public ref struct Explicit2
    {
        [FieldOffset(0)] public Inner2 Field1;
        public ref struct Inner2
        {
            public WithNoORefs Field2;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public ref struct Explicit3
    {
        [FieldOffset(0)] public Inner3 Field1;
        public ref struct Inner3
        {
            public WithByRefs Field2;
        }
    }

    [Fact]
    public static void Validate_Explicit1()
    {
        Load1();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string Load1()
        {
            return typeof(Explicit1).ToString();
        }
    }

    [Fact]
    public static void Validate_Explicit2()
    {
        Load2();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string Load2()
        {
            return typeof(Explicit2).ToString();
        }
    }

    [Fact]
    public static void Validate_Explicit3()
    {
        Load3();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string Load3()
        {
            return typeof(Explicit3).ToString();
        }
    }
}
