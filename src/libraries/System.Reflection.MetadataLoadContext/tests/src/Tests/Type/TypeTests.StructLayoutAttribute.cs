// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Tests
{
    public static class TypeTests_StructLayoutAttribute
    {
        [Fact]
        public static void Test_UndecoratedClass()
        {
            Type t = typeof(UndecoratedClass).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Auto, s.Value);
            Assert.Equal(CharSet.Ansi, s.CharSet);
            Assert.Equal(8, s.Pack);
            Assert.Equal(0, s.Size);
        }


        [Fact]
        public static void Test_AutoAnsiEightZero()
        {
            Type t = typeof(AutoAnsiEightZero).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Auto, s.Value);
            Assert.Equal(CharSet.Ansi, s.CharSet);
            Assert.Equal(8, s.Pack);
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void Test_SequentialAnsiEightZero()
        {
            Type t = typeof(SequentialAnsiEightZero).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Sequential, s.Value);
            Assert.Equal(CharSet.Ansi, s.CharSet);
            Assert.Equal(8, s.Pack);
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void Test_ExplicitAnsiEightZero()
        {
            Type t = typeof(ExplicitAnsiEightZero).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Explicit, s.Value);
            Assert.Equal(CharSet.Ansi, s.CharSet);
            Assert.Equal(8, s.Pack);
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void Test_SequentialUnicodeEightZero()
        {
            Type t = typeof(SequentialUnicodeEightZero).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Sequential, s.Value);
            Assert.Equal(CharSet.Unicode, s.CharSet);
            Assert.Equal(8, s.Pack);
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void TestSequentialAutoZeroZero()
        {
            Type t = typeof(SequentialAutoZeroZero).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Sequential, s.Value);
            Assert.Equal(CharSet.Auto, s.CharSet);
            Assert.Equal(8, s.Pack);  // Not an error: Pack=0 is treated as if it were Pack=8.
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void Test_SequentialAutoOneZero()
        {
            Type t = typeof(SequentialAutoOneZero).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Sequential, s.Value);
            Assert.Equal(CharSet.Auto, s.CharSet);
            Assert.Equal(1, s.Pack);
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void Test_SequentialAutoTwoZero()
        {
            Type t = typeof(SequentialAutoTwoZero).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Sequential, s.Value);
            Assert.Equal(CharSet.Auto, s.CharSet);
            Assert.Equal(2, s.Pack);
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void Test_SequentialAutoFourZero()
        {
            Type t = typeof(SequentialAutoFourZero).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Sequential, s.Value);
            Assert.Equal(CharSet.Auto, s.CharSet);
            Assert.Equal(4, s.Pack);
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void Test_SequentialAutoEightZero()
        {
            Type t = typeof(SequentialAutoEightZero).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Sequential, s.Value);
            Assert.Equal(CharSet.Auto, s.CharSet);
            Assert.Equal(8, s.Pack);
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void Test_SequentialAutoSixteeentZero()
        {
            Type t = typeof(SequentialAutoSixteenZero).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Sequential, s.Value);
            Assert.Equal(CharSet.Auto, s.CharSet);
            Assert.Equal(16, s.Pack);
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void Test_ExplicitAutoEightFortyTwo()
        {
            Type t = typeof(ExplicitAutoEightFortyTwo).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Explicit, s.Value);
            Assert.Equal(CharSet.Auto, s.CharSet);
            Assert.Equal(8, s.Pack);
            Assert.Equal(42, s.Size);
        }

        [Fact]
        public static void Test_Derived()
        {
            // Though the layout engine honors StructLayout attributes on base classes, Type.StructLayoutAttribute does not. It only looks at the immediate class.
            Type t = typeof(Derived).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Auto, s.Value);
            Assert.Equal(CharSet.Ansi, s.CharSet);
            Assert.Equal(8, s.Pack);
            Assert.Equal(0, s.Size);
        }

        [Fact]
        public static void Test_Generic()
        {
            // Type.StructLayoutAttribute treats generic instance classes as if they were the generic type definition. (The runtime layout engine, on the other hand
            // generally doesn't allow these.)
            Type t = typeof(Generic<int>).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Equal(LayoutKind.Sequential, s.Value);
            Assert.Equal(CharSet.Auto, s.CharSet);
            Assert.Equal(4, s.Pack);
            Assert.Equal(40, s.Size);
        }

        [Fact]
        public static void Test_Array()
        {
            // HasElement types always return null for this property.
            Type t = typeof(SequentialAnsiEightZero[]).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Null(s);
        }

        [Fact]
        public static void Test_GenericParameter()
        {
            // GenericParameter types always return null for this property.
            Type t = typeof(GenericParameterHolder<>).Project().GetTypeInfo().GenericTypeParameters[0];
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Null(s);
        }

        [Fact]
        public static void Test_Interface()
        {
            // Interafces return null for this property.
            Type t = typeof(IInterface).Project();
            StructLayoutAttribute s = t.StructLayoutAttribute;
            Assert.Null(s);
        }

        private class UndecoratedClass { }

        [StructLayout(LayoutKind.Auto, CharSet = CharSet.Ansi, Pack = 8, Size = 0)]
        private class AutoAnsiEightZero { }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8, Size = 0)]
        private class SequentialAnsiEightZero { }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi, Pack = 8, Size = 0)]
        private class ExplicitAnsiEightZero { }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8, Size = 0)]
        private class SequentialUnicodeEightZero { }

        // A "Pack = 0" emits different IL metadata from "Pack = 8". However, both the runtime layout engine and the Type.StructLayoutAttribute
        // property treat it as if were "8".
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 0, Size = 0)]
        private class SequentialAutoZeroZero { }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1, Size = 0)]
        private class SequentialAutoOneZero { }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 2, Size = 0)]
        private class SequentialAutoTwoZero { }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4, Size = 0)]
        private class SequentialAutoFourZero { }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 8, Size = 0)]
        private class SequentialAutoEightZero { }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 16, Size = 0)]
        private class SequentialAutoSixteenZero { }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto, Pack = 8, Size = 42)]
        private class ExplicitAutoEightFortyTwo { }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto, Pack = 4, Size = 42)]
        private class ExplicitAutoFourFortyTwo { }

        private class Derived : ExplicitAutoFourFortyTwo { }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4, Size = 40)]
        private class Generic<T> { }

        private class GenericParameterHolder<T> where T : ExplicitAutoFourFortyTwo { }

        private interface IInterface { }
    }
}
