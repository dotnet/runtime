// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "GetNativeVariantForObject() not supported on UWP")]
    public class GetNativeVariantForObjectTests
    {
        internal struct Variant
        {
#pragma warning disable 0649
            public ushort vt;
            public ushort wReserved1;
            public ushort wReserved2;
            public ushort wReserved3;
            public IntPtr bstrVal;
            public IntPtr pRecInfo;
#pragma warning restore 0649
        }

#pragma warning disable 618

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void EmptyObject()
        {
            Variant v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject(null, pNative);
                object o = Marshal.GetObjectForNativeVariant(pNative);
                Assert.Null(o);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void PrimitiveType()
        {
            Variant v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<ushort>(99, pNative);
                ushort actual = Marshal.GetObjectForNativeVariant<ushort>(pNative);
                Assert.Equal(99, actual);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void CharType()
        {
            // GetNativeVariantForObject supports char, but internally recognizes it the same as ushort
            // because the native variant type uses mscorlib type VarEnum to store what type it contains.
            // To get back the original char, use GetObjectForNativeVariant<ushort> and cast to char.
            Variant v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<char>('a', pNative);
                ushort actual = Marshal.GetObjectForNativeVariant<ushort>(pNative);
                char actualChar = (char)actual;
                Assert.Equal('a', actualChar);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void CharTypeNegative()
        {
            // While GetNativeVariantForObject supports taking chars, GetObjectForNativeVariant will
            // never return a char. The internal type is ushort, as mentioned above. This behavior
            // is the same on ProjectN and Desktop CLR.
            Variant v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<char>('a', pNative);
                Assert.Throws<InvalidCastException>(() => Marshal.GetObjectForNativeVariant<char>(pNative));
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void StringType()
        {
            Variant v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<string>("99", pNative);
                string actual = Marshal.GetObjectForNativeVariant<string>(pNative);
                Assert.Equal("99", actual);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void DoubleType()
        {
            Variant v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<double>(3.14, pNative);
                double actual = Marshal.GetObjectForNativeVariant<double>(pNative);
                Assert.Equal(3.14, actual);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetNativeVariantForObject_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetNativeVariantForObject(new object(), IntPtr.Zero));
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetNativeVariantForObject<int>(1, IntPtr.Zero));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void GetNativeVariantForObject_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pDstNativeVariant", () => Marshal.GetNativeVariantForObject(new object(), IntPtr.Zero));
            AssertExtensions.Throws<ArgumentNullException>("pDstNativeVariant", () => Marshal.GetNativeVariantForObject<int>(1, IntPtr.Zero));
        }
#pragma warning restore 618
    }
}
