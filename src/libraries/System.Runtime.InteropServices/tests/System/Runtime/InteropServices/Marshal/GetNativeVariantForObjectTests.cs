// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

#pragma warning disable 618

namespace System.Runtime.InteropServices.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "GetNativeVariantForObject() not supported on UWP")]
    public partial class GetNativeVariantForObjectTests
    {
        public static IEnumerable<object[]> GetNativeVariantForObject_RoundtrippingPrimitives_TestData()
        {
            yield return new object[] { null, VarEnum.VT_EMPTY, IntPtr.Zero };

            yield return new object[] { (sbyte)10, VarEnum.VT_I1, (IntPtr)10 };
            yield return new object[] { (short)10, VarEnum.VT_I2, (IntPtr)10 };
            yield return new object[] { 10, VarEnum.VT_I4, (IntPtr)10 };
            yield return new object[] { (long)10, VarEnum.VT_I8, (IntPtr)10 };
            yield return new object[] { (byte)10, VarEnum.VT_UI1, (IntPtr)10 };
            yield return new object[] { (ushort)10, VarEnum.VT_UI2, (IntPtr)10 };
            yield return new object[] { (uint)10, VarEnum.VT_UI4, (IntPtr)10 };
            yield return new object[] { (ulong)10, VarEnum.VT_UI8, (IntPtr)10 };

            yield return new object[] { true, VarEnum.VT_BOOL, (IntPtr)ushort.MaxValue };
            yield return new object[] { false, VarEnum.VT_BOOL, IntPtr.Zero };

            yield return new object[] { 10m, VarEnum.VT_DECIMAL, (IntPtr)10 };

            // Well known types.
            DateTime dateTime = new DateTime(1899, 12, 30).AddDays(20);
            yield return new object[] { dateTime, VarEnum.VT_DATE, (IntPtr)(-1) };

            yield return new object[] { DBNull.Value, VarEnum.VT_NULL, IntPtr.Zero };
            yield return new object[] { DBNull.Value, VarEnum.VT_NULL, IntPtr.Zero };

            // Arrays.
            yield return new object[] { new sbyte[] { 10, 11, 12 }, (VarEnum)8208, (IntPtr)(-1) };
            yield return new object[] { new short[] { 10, 11, 12 }, (VarEnum)8194, (IntPtr)(-1) };
            yield return new object[] { new int[] { 10, 11, 12 }, (VarEnum)8195, (IntPtr)(-1) };
            yield return new object[] { new long[] { 10, 11, 12 }, (VarEnum)8212, (IntPtr)(-1) };
            yield return new object[] { new byte[] { 10, 11, 12 }, (VarEnum)8209, (IntPtr)(-1) };
            yield return new object[] { new ushort[] { 10, 11, 12 }, (VarEnum)8210, (IntPtr)(-1) };
            yield return new object[] { new uint[] { 10, 11, 12 }, (VarEnum)8211, (IntPtr)(-1) };
            yield return new object[] { new ulong[] { 10, 11, 12 }, (VarEnum)8213, (IntPtr)(-1) };

            yield return new object[] { new bool[] { true, false }, (VarEnum)8203, (IntPtr)(-1) };

            yield return new object[] { new float[] { 10, 11, 12 }, (VarEnum)8196, (IntPtr)(-1) };
            yield return new object[] { new double[] { 10, 11, 12 }, (VarEnum)8197, (IntPtr)(-1) };
            yield return new object[] { new decimal[] { 10m, 11m, 12m }, (VarEnum)8206, (IntPtr)(-1) };

            yield return new object[] { new object[] { 10, 11, 12 }, (VarEnum)8204, (IntPtr)(-1) };
            yield return new object[] { new string[] { "a", "b", "c" }, (VarEnum)8200, (IntPtr)(-1) };

            yield return new object[] { new TimeSpan[] { new TimeSpan(10) }, (VarEnum)8228, (IntPtr)(-1) };
            yield return new object[] { new int[,] { { 10 }, { 11 }, { 12 } }, (VarEnum)8195, (IntPtr)(-1) };

            // Objects.
            var nonGenericClass = new NonGenericClass();
            yield return new object[] { nonGenericClass, VarEnum.VT_DISPATCH, (IntPtr)(-1) };

            var valueType = new StructWithValue { Value = 10 };
            yield return new object[] { valueType, VarEnum.VT_RECORD, (IntPtr)(-1) };

            var genericClass = new GenericClass<string>();
            yield return new object[] { new object[] { nonGenericClass, genericClass, null }, (VarEnum)8204, (IntPtr)(-1) };

            yield return new object[] { new object[] { valueType, null }, (VarEnum)8204, (IntPtr)(-1) };

            // Delegate.
            MethodInfo method = typeof(GetNativeVariantForObjectTests).GetMethod(nameof(NonGenericMethod));
            Delegate d = method.CreateDelegate(typeof(NonGenericDelegate));
            yield return new object[] { d, VarEnum.VT_DISPATCH, (IntPtr)(-1) };
        }

        [Theory]
        [MemberData(nameof(GetNativeVariantForObject_RoundtrippingPrimitives_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [ActiveIssue(31077, ~TargetFrameworkMonikers.NetFramework)]
        public void GetNativeVariantForObject_RoundtrippingPrimitives_Success(object primitive, VarEnum expectedVarType, IntPtr expectedValue)
        {
            GetNativeVariantForObject_ValidObject_Success(primitive, expectedVarType, expectedValue, primitive);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetNativeVariantForObject_TypeMissing_Success()
        {
            // This cannot be in the test data as XUnit uses MethodInfo.Invoke to call test methods and
            // Type.Missing is handled specially for parameters with default values.
            GetNativeVariantForObject_RoundtrippingPrimitives_Success(Type.Missing, VarEnum.VT_ERROR, (IntPtr)(-1));
        }

        public static IEnumerable<object[]> GetNativeVariantForObject_NonRoundtrippingPrimitives_TestData()
        {
            // GetNativeVariantForObject supports char, but internally recognizes it the same as ushort
            // because the native variant type uses mscorlib type VarEnum to store what type it contains.
            // To get back the original char, use GetObjectForNativeVariant<ushort> and cast to char.
            yield return new object[] { 'a', VarEnum.VT_UI2, (IntPtr)'a', (ushort)97 };
            yield return new object[] { new char[] { 'a', 'b', 'c' }, (VarEnum)8210, (IntPtr)(-1), new ushort[] { 'a', 'b', 'c' } };

            // IntPtr/UIntPtr objects are converted to int/uint respectively.
            yield return new object[] { (IntPtr)10, VarEnum.VT_INT, (IntPtr)10, 10 };
            yield return new object[] { (UIntPtr)10, VarEnum.VT_UINT, (IntPtr)10, (uint)10 };

            yield return new object[] { new IntPtr[] { (IntPtr)10, (IntPtr)11, (IntPtr)12 }, (VarEnum)8212, (IntPtr)(-1), new long[] { 10, 11, 12 } };
            yield return new object[] { new UIntPtr[] { (UIntPtr)10, (UIntPtr)11, (UIntPtr)12 }, (VarEnum)8213, (IntPtr)(-1), new ulong[] { 10, 11, 12 } };

            // DateTime is converted to VT_DATE which is offset from December 30, 1899.
            DateTime earlyDateTime = new DateTime(1899, 12, 30);
            yield return new object[] { earlyDateTime, VarEnum.VT_DATE, IntPtr.Zero, new DateTime(1899, 12, 30) };

            // Wrappers.
            yield return new object[] { new UnknownWrapper(10), VarEnum.VT_UNKNOWN, IntPtr.Zero, null };
            if (!PlatformDetection.IsNetCore)
            {
                yield return new object[] { new DispatchWrapper(10), VarEnum.VT_DISPATCH, IntPtr.Zero, null };
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => new DispatchWrapper(10));
            }
            yield return new object[] { new ErrorWrapper(10), VarEnum.VT_ERROR, (IntPtr)10, 10 };
            yield return new object[] { new CurrencyWrapper(10), VarEnum.VT_CY, (IntPtr)100000, 10m };
            yield return new object[] { new BStrWrapper("a"), VarEnum.VT_BSTR, (IntPtr)(-1), "a" };
            yield return new object[] { new BStrWrapper(null), VarEnum.VT_BSTR, IntPtr.Zero, null };

            yield return new object[] { new UnknownWrapper[] { new UnknownWrapper(null), new UnknownWrapper(10) }, (VarEnum)8205, (IntPtr)(-1), new object[] { null, 10 }  };
            if (!PlatformDetection.IsNetCore)
            {
                yield return new object[] { new DispatchWrapper[] { new DispatchWrapper(null), new DispatchWrapper(10) }, (VarEnum)8201, (IntPtr)(-1), new object[] { null, 10 } };
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => new DispatchWrapper(10));
            }
            yield return new object[] { new ErrorWrapper[] { new ErrorWrapper(10) }, (VarEnum)8202, (IntPtr)(-1), new uint[] { 10 } };
            yield return new object[] { new CurrencyWrapper[] { new CurrencyWrapper(10) }, (VarEnum)8198, (IntPtr)(-1), new decimal[] { 10 } };
            yield return new object[] { new BStrWrapper[] { new BStrWrapper("a"), new BStrWrapper(null), new BStrWrapper("c") }, (VarEnum)8200, (IntPtr)(-1), new string[] { "a", null, "c" } };

            // Objects.
            var nonGenericClass = new NonGenericClass();
            yield return new object[] { new NonGenericClass[] { nonGenericClass, null }, (VarEnum)8201, (IntPtr)(-1), new object[] { nonGenericClass, null } };

            var genericClass = new GenericClass<string>();
            yield return new object[] { new GenericClass<string>[] { genericClass, null }, (VarEnum)8205, (IntPtr)(-1), new object[] { genericClass, null } };

            var nonGenericStruct = new NonGenericStruct();
            yield return new object[] { new NonGenericStruct[] { nonGenericStruct }, (VarEnum)8228, (IntPtr)(-1), new NonGenericStruct[] { nonGenericStruct } };

            var classWithInterface = new ClassWithInterface();
            var structWithInterface = new StructWithInterface();
            yield return new object[] { new ClassWithInterface[] { classWithInterface, null }, (VarEnum)8201, (IntPtr)(-1), new object[] { classWithInterface, null } };
            yield return new object[] { new StructWithInterface[] { structWithInterface }, (VarEnum)8228, (IntPtr)(-1), new StructWithInterface[] { structWithInterface } };
            yield return new object[] { new NonGenericInterface[] { classWithInterface, structWithInterface, null }, (VarEnum)8201, (IntPtr)(-1), new object[] { classWithInterface, structWithInterface, null } };

            // Enums.
            yield return new object[] { SByteEnum.Value2, VarEnum.VT_I1, (IntPtr)1, (sbyte)1 };
            yield return new object[] { Int16Enum.Value2, VarEnum.VT_I2, (IntPtr)1, (short)1 };
            yield return new object[] { Int32Enum.Value2, VarEnum.VT_I4, (IntPtr)1, 1 };
            yield return new object[] { Int64Enum.Value2, VarEnum.VT_I8, (IntPtr)1, (long)1 };
            yield return new object[] { ByteEnum.Value2, VarEnum.VT_UI1, (IntPtr)1, (byte)1 };
            yield return new object[] { UInt16Enum.Value2, VarEnum.VT_UI2, (IntPtr)1, (ushort)1 };
            yield return new object[] { UInt32Enum.Value2, VarEnum.VT_UI4, (IntPtr)1, (uint)1 };
            yield return new object[] { UInt64Enum.Value2, VarEnum.VT_UI8, (IntPtr)1, (ulong)1 };

            yield return new object[] { new SByteEnum[] { SByteEnum.Value2 }, (VarEnum)8208, (IntPtr)(-1), new sbyte[] { 1 } };
            yield return new object[] { new Int16Enum[] { Int16Enum.Value2 }, (VarEnum)8194, (IntPtr)(-1), new short[] { 1 } };
            yield return new object[] { new Int32Enum[] { Int32Enum.Value2 }, (VarEnum)8195, (IntPtr)(-1), new int[] { 1 } };
            yield return new object[] { new Int64Enum[] { Int64Enum.Value2 }, (VarEnum)8212, (IntPtr)(-1), new long[] { 1 } };
            yield return new object[] { new ByteEnum[] { ByteEnum.Value2 }, (VarEnum)8209, (IntPtr)(-1), new byte[] { 1 } };
            yield return new object[] { new UInt16Enum[] { UInt16Enum.Value2 }, (VarEnum)8210, (IntPtr)(-1), new ushort[] { 1 } };
            yield return new object[] { new UInt32Enum[] { UInt32Enum.Value2 }, (VarEnum)8211, (IntPtr)(-1), new uint[] { 1 } };
            yield return new object[] { new UInt64Enum[] { UInt64Enum.Value2 }, (VarEnum)8213, (IntPtr)(-1), new ulong[] { 1 } };

            // Color is converted to uint.
            yield return new object[] { Color.FromArgb(10), VarEnum.VT_UI4, (IntPtr)655360, (uint)655360 };
        }

        [Theory]
        [MemberData(nameof(GetNativeVariantForObject_NonRoundtrippingPrimitives_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [ActiveIssue(31077, ~TargetFrameworkMonikers.NetFramework)]
        public void GetNativeVariantForObject_ValidObject_Success(object primitive, VarEnum expectedVarType, IntPtr expectedValue, object expectedRoundtripValue)
        {
            var v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject(primitive, pNative);

                Variant result = Marshal.PtrToStructure<Variant>(pNative);
                Assert.Equal(expectedVarType, (VarEnum)result.vt);
                if (expectedValue != (IntPtr)(-1))
                {
                    Assert.Equal(expectedValue, result.bstrVal);
                }
                else
                {
                    Assert.NotEqual((IntPtr)(-1), result.bstrVal);
                    Assert.NotEqual(IntPtr.Zero, result.bstrVal);
                }

                // Make sure it roundtrips.
                Assert.Equal(expectedRoundtripValue, Marshal.GetObjectForNativeVariant(pNative));
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("99")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetNativeVariantForObject_String_Success(string obj)
        {
            var v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject(obj, pNative);

                Variant result = Marshal.PtrToStructure<Variant>(pNative);
                try
                {
                    Assert.Equal(VarEnum.VT_BSTR, (VarEnum)result.vt);
                    Assert.Equal(obj, Marshal.PtrToStringBSTR(result.bstrVal));

                    object o = Marshal.GetObjectForNativeVariant(pNative);
                    Assert.Equal(obj, o);
                }
                finally
                {
                    Marshal.FreeBSTR(result.bstrVal);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Theory]
        [InlineData(3.14)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public unsafe void GetNativeVariantForObject_Double_Success(double obj)
        {
            var v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject(obj, pNative);

                Variant result = Marshal.PtrToStructure<Variant>(pNative);
                Assert.Equal(VarEnum.VT_R8, (VarEnum)result.vt);
                Assert.Equal(*((ulong*)&obj), *((ulong*)&result.bstrVal));

                object o = Marshal.GetObjectForNativeVariant(pNative);
                Assert.Equal(obj, o);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Theory]
        [InlineData(3.14f)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public unsafe void GetNativeVariantForObject_Float_Success(float obj)
        {
            var v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Marshal.GetNativeVariantForObject(obj, pNative);

                Variant result = Marshal.PtrToStructure<Variant>(pNative);
                Assert.Equal(VarEnum.VT_R4, (VarEnum)result.vt);
                Assert.Equal(*((uint*)&obj), *((uint*)&result.bstrVal));

                object o = Marshal.GetObjectForNativeVariant(pNative);
                Assert.Equal(obj, o);
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
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetNativeVariantForObject(1, IntPtr.Zero));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetNativeVariantForObject_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pDstNativeVariant", () => Marshal.GetNativeVariantForObject(new object(), IntPtr.Zero));
            AssertExtensions.Throws<ArgumentNullException>("pDstNativeVariant", () => Marshal.GetNativeVariantForObject<int>(1, IntPtr.Zero));
        }

        public static IEnumerable<object[]> GetNativeVariantForObject_GenericObject_TestData()
        {
            yield return new object[] { new GenericClass<string>() };
            yield return new object[] { new GenericStruct<string>() };
        }

        [Theory]
        [MemberData(nameof(GetNativeVariantForObject_GenericObject_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetNativeVariantForObject_GenericObject_ThrowsArgumentException(object obj)
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => Marshal.GetNativeVariantForObject(obj, (IntPtr)1));
            AssertExtensions.Throws<ArgumentException>("obj", () => Marshal.GetNativeVariantForObject<object>(obj, (IntPtr)1));
        }

        public static IEnumerable<object[]> GetNativeVariant_NotInteropCompatible_TestData()
        {
            yield return new object[] { new TimeSpan(10) };

            yield return new object[] { new object[] { new GenericStruct<string>() } };

            yield return new object[] { new GenericStruct<string>[0]};
            yield return new object[] { new GenericStruct<string>[] { new GenericStruct<string>() } };

            yield return new object[] { new Color[0] };
            yield return new object[] { new Color[] { Color.FromArgb(10) } };
        }

        [Theory]
        [MemberData(nameof(GetNativeVariant_NotInteropCompatible_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [ActiveIssue(31077, ~TargetFrameworkMonikers.NetFramework)]
        public void GetNativeVariant_NotInteropCompatible_ThrowsArgumentException(object obj)
        {
            var v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Marshal.GetNativeVariantForObject(obj, pNative));
                AssertExtensions.Throws<ArgumentException>(null, () => Marshal.GetNativeVariantForObject<object>(obj, pNative));
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetNativeVariant_InvalidArray_ThrowsSafeArrayTypeMismatchException()
        {
            var v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Assert.Throws<SafeArrayTypeMismatchException>(() => Marshal.GetNativeVariantForObject(new int[][] { }, pNative));
                Assert.Throws<SafeArrayTypeMismatchException>(() => Marshal.GetNativeVariantForObject<object>(new int[][] { }, pNative));
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        public static IEnumerable<object[]> GetNativeVariant_VariantWrapper_TestData()
        {
            yield return new object[] { new VariantWrapper(null) };
            yield return new object[] { new VariantWrapper[] { new VariantWrapper(null) } };
        }

        [Theory]
        [MemberData(nameof(GetNativeVariant_VariantWrapper_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetNativeVariant_VariantWrapper_ThrowsArgumentException(object obj)
        {
            var v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Marshal.GetNativeVariantForObject(obj, pNative));
                AssertExtensions.Throws<ArgumentException>(null, () => Marshal.GetNativeVariantForObject<object>(obj, pNative));
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        public static IEnumerable<object[]> GetNativeVariant_HandleObject_TestData()
        {
            yield return new object[] { new FakeSafeHandle() };
            yield return new object[] { new FakeCriticalHandle() };

            yield return new object[] { new FakeSafeHandle[] { new FakeSafeHandle() } };
            yield return new object[] { new FakeCriticalHandle[] { new FakeCriticalHandle() } };
        }

        [Theory]
        [MemberData(nameof(GetNativeVariant_HandleObject_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetNativeVariant_HandleObject_ThrowsArgumentException(object obj)
        {
            var v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Marshal.GetNativeVariantForObject(obj, pNative));
                AssertExtensions.Throws<ArgumentException>(null, () => Marshal.GetNativeVariantForObject<object>(obj, pNative));
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void GetNativeVariantForObject_CantCastToObject_ThrowsInvalidCastException()
        {
            // While GetNativeVariantForObject supports taking chars, GetObjectForNativeVariant will
            // never return a char. The internal type is ushort, as mentioned above. This behavior
            // is the same on ProjectN and Desktop CLR.
            var v = new Variant();
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

        public struct StructWithValue
        {
            public int Value;
        }

        public class ClassWithInterface : NonGenericInterface { }
        public struct StructWithInterface : NonGenericInterface { }

        public enum SByteEnum : sbyte { Value1, Value2 }
        public enum Int16Enum : short { Value1, Value2 }
        public enum Int32Enum : int { Value1, Value2 }
        public enum Int64Enum : long { Value1, Value2 }

        public enum ByteEnum : byte { Value1, Value2 }
        public enum UInt16Enum : ushort { Value1, Value2 }
        public enum UInt32Enum : uint { Value1, Value2 }
        public enum UInt64Enum : ulong { Value1, Value2 }

        public static void NonGenericMethod(int i) { }
        public delegate void NonGenericDelegate(int i);

        public class FakeSafeHandle : SafeHandle
        {
            public FakeSafeHandle() : base(IntPtr.Zero, false) { }

            public override bool IsInvalid => throw new NotImplementedException();

            protected override bool ReleaseHandle() => throw new NotImplementedException();

            protected override void Dispose(bool disposing) { }
        }

        public class FakeCriticalHandle : CriticalHandle
        {
            public FakeCriticalHandle() : base(IntPtr.Zero) { }

            public override bool IsInvalid => true;

            protected override bool ReleaseHandle() => throw new NotImplementedException();

            protected override void Dispose(bool disposing) { }
        }
    }
}

#pragma warning restore 618
