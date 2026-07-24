// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

#pragma warning disable 618

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetNativeVariantForObjectTests
    {
        private void GetNativeVariantForObject_RoundtrippingPrimitives_Success(object primitive, VarEnum expectedVarType, IntPtr expectedValue)
        {
            GetNativeVariantForObject_ValidObject_Success(primitive, expectedVarType, expectedValue, primitive);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
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
            yield return new object[] { new char[] { 'a', 'b', 'c' }, (VarEnum.VT_ARRAY | VarEnum.VT_UI2), (IntPtr)(-1), new ushort[] { 'a', 'b', 'c' } };

            // IntPtr/UIntPtr objects are _always_ converted to int/uint respectively.
            // See OleVariant::MarshalOleVariantForObject conversion from ELEMENT_TYPE_I/ELEMENT_TYPE_U to VT_INT/VT_UINT
            yield return new object[] { (IntPtr)10, VarEnum.VT_INT, (IntPtr)10, 10 };
            yield return new object[] { (UIntPtr)10, VarEnum.VT_UINT, (IntPtr)10, (uint)10 };

            // IntPtr/UIntPtr objects in arrays are converted to the appropriate pointer width.
            // See OleVariant::GetVarTypeForTypeHandle conversion from IntPtr/UIntPtr to VT_INT/VT_UINT or VT_I8/VT_UI8 based on bitness
            if (IntPtr.Size == 4)
            {
                yield return new object[] { new IntPtr[] { (IntPtr)10, (IntPtr)11, (IntPtr)12 }, (VarEnum.VT_ARRAY | VarEnum.VT_INT), (IntPtr)(-1), new int[] { 10, 11, 12 } };
                yield return new object[] { new UIntPtr[] { (UIntPtr)10, (UIntPtr)11, (UIntPtr)12 }, (VarEnum.VT_ARRAY | VarEnum.VT_UINT), (IntPtr)(-1), new uint[] { 10, 11, 12 } };
            }
            else
            {
                yield return new object[] { new IntPtr[] { (IntPtr)10, (IntPtr)11, (IntPtr)12 }, (VarEnum.VT_ARRAY | VarEnum.VT_I8), (IntPtr)(-1), new long[] { 10, 11, 12 } };
                yield return new object[] { new UIntPtr[] { (UIntPtr)10, (UIntPtr)11, (UIntPtr)12 }, (VarEnum.VT_ARRAY | VarEnum.VT_UI8), (IntPtr)(-1), new ulong[] { 10, 11, 12 } };
            }

            // DateTime is converted to VT_DATE which is offset from December 30, 1899.
            DateTime earlyDateTime = new DateTime(1899, 12, 30);
            yield return new object[] { earlyDateTime, VarEnum.VT_DATE, IntPtr.Zero, new DateTime(1899, 12, 30) };

            // Wrappers.
            yield return new object[] { new UnknownWrapper(10), VarEnum.VT_UNKNOWN, IntPtr.Zero, null };
            yield return new object[] { new DispatchWrapper[] { new DispatchWrapper(null), new DispatchWrapper(null) }, (VarEnum.VT_ARRAY | VarEnum.VT_DISPATCH), (IntPtr)(-1), new object[] { null, null } };
            yield return new object[] { new ErrorWrapper(10), VarEnum.VT_ERROR, (IntPtr)10, 10 };
            yield return new object[] { new CurrencyWrapper(10), VarEnum.VT_CY, (IntPtr)100000, 10m };
            yield return new object[] { new BStrWrapper("a"), VarEnum.VT_BSTR, (IntPtr)(-1), "a" };
            yield return new object[] { new BStrWrapper(null), VarEnum.VT_BSTR, IntPtr.Zero, null };

            yield return new object[] { new UnknownWrapper[] { new UnknownWrapper(null), new UnknownWrapper(10) }, (VarEnum.VT_ARRAY | VarEnum.VT_UNKNOWN), (IntPtr)(-1), new object[] { null, 10 }  };
            yield return new object[] { new ErrorWrapper[] { new ErrorWrapper(10) }, (VarEnum.VT_ARRAY | VarEnum.VT_ERROR), (IntPtr)(-1), new uint[] { 10 } };
            yield return new object[] { new CurrencyWrapper[] { new CurrencyWrapper(10) }, (VarEnum.VT_ARRAY | VarEnum.VT_CY), (IntPtr)(-1), new decimal[] { 10 } };
            yield return new object[] { new BStrWrapper[] { new BStrWrapper("a"), new BStrWrapper(null), new BStrWrapper("c") }, (VarEnum.VT_ARRAY | VarEnum.VT_BSTR), (IntPtr)(-1), new string[] { "a", null, "c" } };

            // Objects.
            var nonGenericClass = new NonGenericClass();
            yield return new object[] { new NonGenericClass[] { nonGenericClass, null }, (VarEnum.VT_ARRAY | VarEnum.VT_DISPATCH), (IntPtr)(-1), new object[] { nonGenericClass, null } };

            var genericClass = new GenericClass<string>();
            yield return new object[] { new GenericClass<string>[] { genericClass, null }, (VarEnum.VT_ARRAY | VarEnum.VT_UNKNOWN), (IntPtr)(-1), new object[] { genericClass, null } };

            var classWithInterface = new ClassWithInterface();
            var structWithInterface = new StructWithInterface();
            yield return new object[] { new ClassWithInterface[] { classWithInterface, null }, (VarEnum.VT_ARRAY | VarEnum.VT_DISPATCH), (IntPtr)(-1), new object[] { classWithInterface, null } };
            yield return new object[] { new INonGenericInterface[] { classWithInterface, structWithInterface, null }, (VarEnum.VT_ARRAY | VarEnum.VT_DISPATCH), (IntPtr)(-1), new object[] { classWithInterface, structWithInterface, null } };

            // Enums.
            yield return new object[] { SByteEnum.Value2, VarEnum.VT_I1, (IntPtr)1, (sbyte)1 };
            yield return new object[] { Int16Enum.Value2, VarEnum.VT_I2, (IntPtr)1, (short)1 };
            yield return new object[] { Int32Enum.Value2, VarEnum.VT_I4, (IntPtr)1, 1 };
            yield return new object[] { Int64Enum.Value2, VarEnum.VT_I8, (IntPtr)1, (long)1 };
            yield return new object[] { ByteEnum.Value2, VarEnum.VT_UI1, (IntPtr)1, (byte)1 };
            yield return new object[] { UInt16Enum.Value2, VarEnum.VT_UI2, (IntPtr)1, (ushort)1 };
            yield return new object[] { UInt32Enum.Value2, VarEnum.VT_UI4, (IntPtr)1, (uint)1 };
            yield return new object[] { UInt64Enum.Value2, VarEnum.VT_UI8, (IntPtr)1, (ulong)1 };

            yield return new object[] { new SByteEnum[] { SByteEnum.Value2 }, (VarEnum.VT_ARRAY | VarEnum.VT_I1), (IntPtr)(-1), new sbyte[] { 1 } };
            yield return new object[] { new Int16Enum[] { Int16Enum.Value2 }, (VarEnum.VT_ARRAY | VarEnum.VT_I2), (IntPtr)(-1), new short[] { 1 } };
            yield return new object[] { new Int32Enum[] { Int32Enum.Value2 }, (VarEnum.VT_ARRAY | VarEnum.VT_I4), (IntPtr)(-1), new int[] { 1 } };
            yield return new object[] { new Int64Enum[] { Int64Enum.Value2 }, (VarEnum.VT_ARRAY | VarEnum.VT_I8), (IntPtr)(-1), new long[] { 1 } };
            yield return new object[] { new ByteEnum[] { ByteEnum.Value2 }, (VarEnum.VT_ARRAY | VarEnum.VT_UI1), (IntPtr)(-1), new byte[] { 1 } };
            yield return new object[] { new UInt16Enum[] { UInt16Enum.Value2 }, (VarEnum.VT_ARRAY | VarEnum.VT_UI2), (IntPtr)(-1), new ushort[] { 1 } };
            yield return new object[] { new UInt32Enum[] { UInt32Enum.Value2 }, (VarEnum.VT_ARRAY | VarEnum.VT_UI4), (IntPtr)(-1), new uint[] { 1 } };
            yield return new object[] { new UInt64Enum[] { UInt64Enum.Value2 }, (VarEnum.VT_ARRAY | VarEnum.VT_UI8), (IntPtr)(-1), new ulong[] { 1 } };

            // Color is converted to uint.
            yield return new object[] { Color.FromArgb(10), VarEnum.VT_UI4, (IntPtr)655360, (uint)655360 };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetNativeVariantForObject_NonRoundtrippingPrimitives_TestData))]
        public unsafe void GetNativeVariantForObject_ValidObject_Success(object primitive, VarEnum expectedVarType, IntPtr expectedValue, object expectedRoundtripValue)
        {
            ComVariant variant = default;
            bool variantInitialized = false;
            try
            {
                Marshal.GetNativeVariantForObject(primitive, (nint)(&variant));
                variantInitialized = true;

                Assert.Equal(expectedVarType, variant.VarType);
                if (expectedValue != (IntPtr)(-1))
                {
                    Assert.Equal(expectedValue, variant.GetRawDataRef<IntPtr>());
                }
                else
                {
                    Assert.NotEqual((IntPtr)(-1), variant.GetRawDataRef<IntPtr>());
                    Assert.NotEqual(IntPtr.Zero, variant.GetRawDataRef<IntPtr>());
                }

                // Make sure it roundtrips.
                Assert.Equal(expectedRoundtripValue, Marshal.GetObjectForNativeVariant((nint)(&variant)));
            }
            finally
            {
                if (variantInitialized)
                {
                    variant.Dispose();
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [InlineData("")]
        [InlineData("99")]
        public void GetNativeVariantForObject_String_Success(string obj)
        {
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf<ComVariant>());
            try
            {
                Marshal.GetNativeVariantForObject(obj, pNative);

                ComVariant result = Marshal.PtrToStructure<ComVariant>(pNative);
                try
                {
                    Assert.Equal(VarEnum.VT_BSTR, result.VarType);
                    Assert.Equal(obj, Marshal.PtrToStringBSTR(result.GetRawDataRef<IntPtr>()));

                    object o = Marshal.GetObjectForNativeVariant(pNative);
                    Assert.Equal(obj, o);
                }
                finally
                {
                    Marshal.FreeBSTR(result.GetRawDataRef<IntPtr>());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public unsafe void GetNativeVariantForObject_Guid_Success()
        {
            var guid = new Guid("0DD3E51B-3162-4D13-B906-030F402C5BA2");
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf<ComVariant>());
            try
            {
                if (PlatformDetection.IsWindowsNanoServer)
                {
                    Assert.Throws<NotSupportedException>(() => Marshal.GetNativeVariantForObject(guid, pNative));
                }
                else
                {
                    Marshal.GetNativeVariantForObject(guid, pNative);

                    ComVariant result = Marshal.PtrToStructure<ComVariant>(pNative);
                    Assert.Equal(VarEnum.VT_RECORD, result.VarType);
                    Assert.NotEqual(nint.Zero, result.GetRawDataRef<Record>()._recordInfo); // We should have an IRecordInfo instance.

                    var expectedBytes = new ReadOnlySpan<byte>(guid.ToByteArray());
                    var actualBytes = new ReadOnlySpan<byte>((void*)result.GetRawDataRef<Record>()._record, expectedBytes.Length);
                    Assert.Equal(expectedBytes, actualBytes);

                    object o = Marshal.GetObjectForNativeVariant(pNative);
                    Assert.Equal(guid, o);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [InlineData(3.14)]
        public unsafe void GetNativeVariantForObject_Double_Success(double obj)
        {
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf<ComVariant>());
            try
            {
                Marshal.GetNativeVariantForObject(obj, pNative);

                ComVariant result = Marshal.PtrToStructure<ComVariant>(pNative);
                Assert.Equal(VarEnum.VT_R8, result.VarType);
                Assert.Equal(*((ulong*)&obj), result.GetRawDataRef<ulong>());

                object o = Marshal.GetObjectForNativeVariant(pNative);
                Assert.Equal(obj, o);
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [InlineData(3.14f)]
        public unsafe void GetNativeVariantForObject_Float_Success(float obj)
        {
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf<ComVariant>());
            try
            {
                Marshal.GetNativeVariantForObject(obj, pNative);

                ComVariant result = Marshal.PtrToStructure<ComVariant>(pNative);
                Assert.Equal(VarEnum.VT_R4, result.VarType);
                Assert.Equal(*((uint*)&obj), result.GetRawDataRef<uint>());

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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetNativeVariantForObject_GenericObject_TestData))]
        public void GetNativeVariantForObject_GenericObject_ThrowsArgumentException(object obj)
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => Marshal.GetNativeVariantForObject(obj, (IntPtr)1));
            AssertExtensions.Throws<ArgumentException>("obj", () => Marshal.GetNativeVariantForObject<object>(obj, (IntPtr)1));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetNativeVariant_InvalidArray_ThrowsSafeArrayTypeMismatchException()
        {
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf<ComVariant>());
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetNativeVariant_VariantWrapper_TestData))]
        public void GetNativeVariant_VariantWrapper_ThrowsArgumentException(object obj)
        {
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf<ComVariant>());
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetNativeVariant_HandleObject_TestData))]
        public void GetNativeVariant_HandleObject_ThrowsArgumentException(object obj)
        {
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf<ComVariant>());
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public static void GetNativeVariantForObject_CantCastToObject_ThrowsInvalidCastException()
        {
            // While GetNativeVariantForObject supports taking chars, GetObjectForNativeVariant will
            // never return a char. The internal type is ushort, as mentioned above.
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf<ComVariant>());
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

        public class ClassWithInterface : INonGenericInterface { }
        public struct StructWithInterface : INonGenericInterface { }

        public enum SByteEnum : sbyte { Value1, Value2 }
        public enum Int16Enum : short { Value1, Value2 }
        public enum Int32Enum : int { Value1, Value2 }
        public enum Int64Enum : long { Value1, Value2 }

        public enum ByteEnum : byte { Value1, Value2 }
        public enum UInt16Enum : ushort { Value1, Value2 }
        public enum UInt32Enum : uint { Value1, Value2 }
        public enum UInt64Enum : ulong { Value1, Value2 }

        [StructLayout(LayoutKind.Sequential)]
        private struct Record
        {
            public nint _record;
            public nint _recordInfo;
        }

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
