// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetNativeVariantForObjectTests
    {
        public static IEnumerable<object[]> GetNativeVariantForObject_ComObject_TestData()
        {
            // Objects.
            yield return new object[] { new ComImportObject(), VarEnum.VT_DISPATCH };

            yield return new object[] { new DualComObject(), VarEnum.VT_DISPATCH };
            yield return new object[] { new IUnknownComObject(), VarEnum.VT_DISPATCH };
            yield return new object[] { new IDispatchComObject(), VarEnum.VT_DISPATCH };
            yield return new object[] { new IInspectableComObject(), VarEnum.VT_DISPATCH };

            yield return new object[] { new NonDualComObject(), VarEnum.VT_DISPATCH };
            yield return new object[] { new AutoDispatchComObject(), VarEnum.VT_DISPATCH };
            yield return new object[] { new AutoDualComObject(), VarEnum.VT_DISPATCH };

            yield return new object[] { new NonDualComObjectEmpty(), VarEnum.VT_DISPATCH };
            yield return new object[] { new AutoDispatchComObjectEmpty(), VarEnum.VT_DISPATCH };
            yield return new object[] { new AutoDualComObjectEmpty(), VarEnum.VT_DISPATCH };
        }

        public static IEnumerable<object[]> GetNativeVariantForObject_ComObjectArray_TestData()
        {
            // Arrays.
            var empty = new ComImportObject();
            yield return new object[] { new ComImportObject[] { empty, null }, (VarEnum.VT_ARRAY | VarEnum.VT_UNKNOWN), new object[] { empty, null } };

            var nonDualEmpty = new NonDualComObjectEmpty();
            var autoDispatchEmpty = new AutoDispatchComObjectEmpty();
            var autoDualEmpty = new AutoDualComObjectEmpty();

            yield return new object[] { new NonDualComObjectEmpty[] { nonDualEmpty, null }, (VarEnum.VT_ARRAY | VarEnum.VT_UNKNOWN), new object[] { nonDualEmpty, null } };
            yield return new object[] { new AutoDispatchComObjectEmpty[] { autoDispatchEmpty, null }, (VarEnum.VT_ARRAY | VarEnum.VT_UNKNOWN), new object[] { autoDispatchEmpty, null } };
            yield return new object[] { new AutoDualComObjectEmpty[] { autoDualEmpty, null }, (VarEnum.VT_ARRAY | VarEnum.VT_UNKNOWN), new object[] { autoDualEmpty, null } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetNativeVariantForObject_ComObjectArray_TestData))]
        public void GetNativeVariantForObject_ComObjectArray_Success(object obj, VarEnum expectedVarType, object expectedRoundtripValue)
        {
            GetNativeVariantForObject_ValidObject_Success(obj, expectedVarType, (IntPtr)(-1), expectedRoundtripValue);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetNativeVariantForObject_ComObject_TestData))]
        public void GetNativeVariantForObject_ComObject_Success(object obj, VarEnum expectedVarType)
        {
            GetNativeVariantForObject_ValidObject_Success(obj, expectedVarType, (IntPtr)(-1), obj);
        }

        public static IEnumerable<object[]> GetNativeVariantForObject_WrappedComObject_TestData()
        {
            var empty = new ComImportObject();
            var dual = new DualComObject();
            var iUnknown = new IUnknownComObject();
            var iDispatch = new IDispatchComObject();
            var iInspectable = new IInspectableComObject();
            var nonDual = new NonDualComObject();
            var autoDispatch = new AutoDispatchComObject();
            var autoDual = new AutoDualComObject();

            yield return new object[] { new UnknownWrapper(empty), empty, VarEnum.VT_UNKNOWN };
            yield return new object[] { new UnknownWrapper(dual), dual, VarEnum.VT_UNKNOWN };
            yield return new object[] { new UnknownWrapper(iUnknown), iUnknown, VarEnum.VT_UNKNOWN };
            yield return new object[] { new UnknownWrapper(iDispatch), iDispatch, VarEnum.VT_UNKNOWN };
            yield return new object[] { new UnknownWrapper(iInspectable), iInspectable, VarEnum.VT_UNKNOWN };
            yield return new object[] { new UnknownWrapper(nonDual), nonDual, VarEnum.VT_UNKNOWN };
            yield return new object[] { new UnknownWrapper(autoDispatch), autoDispatch, VarEnum.VT_UNKNOWN };
            yield return new object[] { new UnknownWrapper(autoDual), autoDual, VarEnum.VT_UNKNOWN };

            yield return new object[] { new DispatchWrapper(empty), empty, VarEnum.VT_DISPATCH };
            yield return new object[] { new DispatchWrapper(dual), dual, VarEnum.VT_DISPATCH };
            yield return new object[] { new DispatchWrapper(iUnknown), iUnknown, VarEnum.VT_DISPATCH };
            yield return new object[] { new DispatchWrapper(iDispatch), iDispatch, VarEnum.VT_DISPATCH };
            yield return new object[] { new DispatchWrapper(iInspectable), iInspectable, VarEnum.VT_DISPATCH };
            yield return new object[] { new DispatchWrapper(nonDual), nonDual, VarEnum.VT_DISPATCH };
            yield return new object[] { new DispatchWrapper(autoDispatch), autoDispatch, VarEnum.VT_DISPATCH };
            yield return new object[] { new DispatchWrapper(autoDual), autoDual, VarEnum.VT_DISPATCH };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetNativeVariantForObject_WrappedComObject_TestData))]
        public void GetNativeVariantForObject_WrappedComObject_Success(object obj, object wrapped, VarEnum expectedVarType)
        {
            GetNativeVariantForObject_ValidObject_Success(obj, expectedVarType, (IntPtr)(-1), wrapped);
        }

        public static IEnumerable<object[]> GetNativeVariantForObject_InvalidArrayType_TestData()
        {
            yield return new object[] { new DualComObject[] { new DualComObject() } };
            yield return new object[] { new IUnknownComObject[] { new IUnknownComObject(), null } };
            yield return new object[] { new IDispatchComObject[] { new IDispatchComObject(), null } };
            yield return new object[] { new NonDualComObject[] { new NonDualComObject(), null } };
            yield return new object[] { new AutoDispatchComObject[] { new AutoDispatchComObject(), null } };
            yield return new object[] { new AutoDualComObject[] { new AutoDualComObject(), null } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetNativeVariantForObject_InvalidArrayType_TestData))]
        public unsafe void GetNativeVariantForObject_InvalidArrayType_ThrowsInvalidCastException(object obj)
        {
            ComVariant variant = default;
            nint pNative = (nint)(&variant);

            Assert.Throws<InvalidCastException>(() => Marshal.GetNativeVariantForObject(obj, pNative));
            Assert.Throws<InvalidCastException>(() => Marshal.GetNativeVariantForObject<object>(obj, pNative));
        }

        public static bool IsVariantMarshallingSupported => PlatformDetection.IsBuiltInComEnabled || PlatformDetection.IsNativeAot;

        public static IEnumerable<object[]> GetNativeVariantForObject_Char_TestData()
        {
            yield return new object[] { 'q' };
            yield return new object[] { new CharConvertible('q') };
        }

        [ConditionalTheory(nameof(IsVariantMarshallingSupported))]
        [MemberData(nameof(GetNativeVariantForObject_Char_TestData))]
        public unsafe void GetNativeVariantForObject_Char_ReturnsVariantUI2(object value)
        {
            ComVariant variant = default;
            Marshal.GetNativeVariantForObject(value, (nint)(&variant));
            Assert.Equal(VarEnum.VT_UI2, variant.VarType);
            Assert.Equal((ushort)'q', variant.GetRawDataRef<ushort>());
        }

        private sealed class CharConvertible(char value) : IConvertible
        {
            public TypeCode GetTypeCode() => TypeCode.Char;
            public char ToChar(IFormatProvider provider) => value;

            public bool ToBoolean(IFormatProvider provider) => throw new InvalidCastException();
            public byte ToByte(IFormatProvider provider) => throw new InvalidCastException();
            public DateTime ToDateTime(IFormatProvider provider) => throw new InvalidCastException();
            public decimal ToDecimal(IFormatProvider provider) => throw new InvalidCastException();
            public double ToDouble(IFormatProvider provider) => throw new InvalidCastException();
            public short ToInt16(IFormatProvider provider) => throw new InvalidCastException();
            public int ToInt32(IFormatProvider provider) => throw new InvalidCastException();
            public long ToInt64(IFormatProvider provider) => throw new InvalidCastException();
            public sbyte ToSByte(IFormatProvider provider) => throw new InvalidCastException();
            public float ToSingle(IFormatProvider provider) => throw new InvalidCastException();
            public string ToString(IFormatProvider provider) => value.ToString();
            public object ToType(Type conversionType, IFormatProvider provider) => throw new InvalidCastException();
            public ushort ToUInt16(IFormatProvider provider) => throw new InvalidCastException();
            public uint ToUInt32(IFormatProvider provider) => throw new InvalidCastException();
            public ulong ToUInt64(IFormatProvider provider) => throw new InvalidCastException();
        }
    }
}
