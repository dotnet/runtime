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

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public ref struct Explicit4
    {
        [FieldOffset(0)]
        public Size1Byte Field1;
        [FieldOffset(1)]
        public Size1Byte Field2;

        public ref struct Size1Byte
        {
            public byte Value;
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

    [Fact]
    public static void Validate_Explicit4()
    {
        Load4();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string Load4()
        {
            return typeof(Explicit4).ToString();
        }
    }

    // Invalid. Explicit offset on second field is invalid
    // since first field on type will be misaligned.
    [StructLayout(LayoutKind.Explicit)]
    public ref struct Explicit5a_Invalid32
    {
        [FieldOffset(0)]
        public WithByRefs Field1;
        [FieldOffset(2)]
        public WithByRefs Field2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public ref struct Explicit5b_Invalid32
    {
        [FieldOffset(0)]
        public WithORefs Field1;
        [FieldOffset(2)]
        public WithORefs Field2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public ref struct Explicit5a_Invalid64
    {
        [FieldOffset(0)]
        public WithByRefs Field1;
        [FieldOffset(4)]
        public WithByRefs Field2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public ref struct Explicit5b_Invalid64
    {
        [FieldOffset(0)]
        public WithORefs Field1;
        [FieldOffset(4)]
        public WithORefs Field2;
    }

    [Fact]
    public static void Validate_Explicit5_Invalid()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Throws<TypeLoadException>(() => LoadA64());
            Assert.Throws<TypeLoadException>(() => LoadB64());
        }
        else
        {
            Assert.Throws<TypeLoadException>(() => LoadA32());
            Assert.Throws<TypeLoadException>(() => LoadB32());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string LoadA32()
        {
            return typeof(Explicit5a_Invalid32).ToString();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string LoadB32()
        {
            return typeof(Explicit5b_Invalid32).ToString();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string LoadA64()
        {
            return typeof(Explicit5a_Invalid64).ToString();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string LoadB64()
        {
            return typeof(Explicit5b_Invalid64).ToString();
        }
    }
}
