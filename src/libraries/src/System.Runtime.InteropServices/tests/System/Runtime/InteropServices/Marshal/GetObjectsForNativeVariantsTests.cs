// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Marshalling between VARIANT and Object is not supported in AppX")]
    public class GetObjectsForNativeVariantsTests
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Record
        {
            private IntPtr _record;
            private IntPtr _recordInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct UnionTypes
        {
            [FieldOffset(0)] internal SByte _i1;
            [FieldOffset(0)] internal Int16 _i2;
            [FieldOffset(0)] internal Int32 _i4;
            [FieldOffset(0)] internal Int64 _i8;
            [FieldOffset(0)] internal Byte _ui1;
            [FieldOffset(0)] internal UInt16 _ui2;
            [FieldOffset(0)] internal UInt32 _ui4;
            [FieldOffset(0)] internal UInt64 _ui8;
            [FieldOffset(0)] internal Int32 _int;
            [FieldOffset(0)] internal UInt32 _uint;
            [FieldOffset(0)] internal Single _r4;
            [FieldOffset(0)] internal Double _r8;
            [FieldOffset(0)] internal Int64 _cy;
            [FieldOffset(0)] internal double _date;
            [FieldOffset(0)] internal IntPtr _bstr;
            [FieldOffset(0)] internal IntPtr _unknown;
            [FieldOffset(0)] internal IntPtr _dispatch;
            [FieldOffset(0)] internal IntPtr _pvarVal;
            [FieldOffset(0)] internal IntPtr _byref;
            [FieldOffset(0)] internal Record _record;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TypeUnion
        {
            public ushort vt;
            public ushort wReserved1;
            public ushort wReserved2;
            public ushort wReserved3;
            public UnionTypes _unionTypes;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct Variant
        {
            [FieldOffset(0)] public TypeUnion m_Variant;
            [FieldOffset(0)] public decimal m_decimal;
        }
#pragma warning disable 618
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SByteType()
        {
            Variant v = new Variant();

            IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<sbyte>(99, pNative);
                Marshal.GetNativeVariantForObject<sbyte>(100, pNative + Marshal.SizeOf(v));

                sbyte[] actual = Marshal.GetObjectsForNativeVariants<sbyte>(pNative, 2);
                Assert.Equal(99, actual[0]);
                Assert.Equal(100, actual[1]);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ByteType()
        {
            Variant v = new Variant();

            IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<byte>(99, pNative);
                Marshal.GetNativeVariantForObject<byte>(100, pNative + Marshal.SizeOf(v));

                byte[] actual = Marshal.GetObjectsForNativeVariants<byte>(pNative, 2);
                Assert.Equal(99, actual[0]);
                Assert.Equal(100, actual[1]);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void DoubleType()
        {
            Variant v = new Variant();

            IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<double>(99, pNative);
                Marshal.GetNativeVariantForObject<double>(100, pNative + Marshal.SizeOf(v));

                double[] actual = Marshal.GetObjectsForNativeVariants<double>(pNative, 2);
                Assert.Equal(99, actual[0]);
                Assert.Equal(100, actual[1]);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ShortType()
        {
            Variant v = new Variant();

            IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<short>(99, pNative);
                Marshal.GetNativeVariantForObject<short>(100, pNative + Marshal.SizeOf(v));

                short[] actual = Marshal.GetObjectsForNativeVariants<short>(pNative, 2);
                Assert.Equal(99, actual[0]);
                Assert.Equal(100, actual[1]);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UshortType()
        {
            Variant v = new Variant();

            IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<ushort>(99, pNative);
                Marshal.GetNativeVariantForObject<ushort>(100, pNative + Marshal.SizeOf(v));

                ushort[] actual = Marshal.GetObjectsForNativeVariants<ushort>(pNative, 2);
                Assert.Equal(99, actual[0]);
                Assert.Equal(100, actual[1]);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void IntType()
        {
            Variant v = new Variant();

            IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<int>(99, pNative);
                Marshal.GetNativeVariantForObject<int>(100, pNative + Marshal.SizeOf(v));

                int[] actual = Marshal.GetObjectsForNativeVariants<int>(pNative, 2);
                Assert.Equal(99, actual[0]);
                Assert.Equal(100, actual[1]);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UIntType()
        {
            Variant v = new Variant();

            IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<uint>(99, pNative);
                Marshal.GetNativeVariantForObject<uint>(100, pNative + Marshal.SizeOf(v));

                uint[] actual = Marshal.GetObjectsForNativeVariants<uint>(pNative, 2);
                Assert.Equal<uint>(99, actual[0]);
                Assert.Equal<uint>(100, actual[1]);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void LongType()
        {
            Variant v = new Variant();

            IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<long>(99, pNative);
                Marshal.GetNativeVariantForObject<long>(100, pNative + Marshal.SizeOf(v));

                long[] actual = Marshal.GetObjectsForNativeVariants<long>(pNative, 2);
                Assert.Equal(99, actual[0]);
                Assert.Equal(100, actual[1]);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ULongType()
        {
            Variant v = new Variant();

            IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<ulong>(99, pNative);
                Marshal.GetNativeVariantForObject<ulong>(100, pNative + Marshal.SizeOf(v));

                ulong[] actual = Marshal.GetObjectsForNativeVariants<ulong>(pNative, 2);
                Assert.Equal<ulong>(99, actual[0]);
                Assert.Equal<ulong>(100, actual[1]);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FloatType()
        {
            Variant v = new Variant();

            IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject<float>(99, pNative);
                Marshal.GetNativeVariantForObject<float>(100, pNative + Marshal.SizeOf(v));

                float[] actual = Marshal.GetObjectsForNativeVariants<float>(pNative, 2);
                Assert.Equal(99, actual[0]);
                Assert.Equal(100, actual[1]);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void GetObjectsForNativeVariants_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetObjectsForNativeVariants(IntPtr.Zero, 10));
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetObjectsForNativeVariants<int>(IntPtr.Zero, 10));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void GetObjectsForNativeVariants_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("aSrcNativeVariant", () => Marshal.GetObjectsForNativeVariants(IntPtr.Zero, 10));
            AssertExtensions.Throws<ArgumentNullException>("aSrcNativeVariant", () => Marshal.GetObjectsForNativeVariants<int>(IntPtr.Zero, 10));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void GetObjectsForNativeVariants_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("cVars", () => Marshal.GetObjectsForNativeVariants((IntPtr)1, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("cVars", () => Marshal.GetObjectsForNativeVariants<int>((IntPtr)1, -1));
        }
#pragma warning restore 618
    }
}
