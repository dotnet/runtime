// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

#pragma warning disable 618

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
            yield return new object[] { new ComImportObject[] { empty, null }, (VarEnum)8205, new object[] { empty, null } };

            var nonDualEmpty = new NonDualComObjectEmpty();
            var autoDispatchEmpty = new AutoDispatchComObjectEmpty();
            var autoDualEmpty = new AutoDualComObjectEmpty();

            yield return new object[] { new NonDualComObjectEmpty[] { nonDualEmpty, null }, (VarEnum)8205, new object[] { nonDualEmpty, null } };
            yield return new object[] { new AutoDispatchComObjectEmpty[] { autoDispatchEmpty, null }, (VarEnum)8205, new object[] { autoDispatchEmpty, null } };
            yield return new object[] { new AutoDualComObjectEmpty[] { autoDualEmpty, null }, (VarEnum)8205, new object[] { autoDualEmpty, null } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(GetNativeVariantForObject_ComObjectArray_TestData))]
        public void GetNativeVariantForObject_ComObjectArray_Success(object obj, VarEnum expectedVarType, object expectedRoundtripValue)
        {
            GetNativeVariantForObject_ValidObject_Success(obj, expectedVarType, (IntPtr)(-1), expectedRoundtripValue);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
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

            if (!PlatformDetection.IsNetCore)
            {
                yield return new object[] { new DispatchWrapper(empty), empty, VarEnum.VT_DISPATCH };
                yield return new object[] { new DispatchWrapper(dual), dual, VarEnum.VT_DISPATCH };
                yield return new object[] { new DispatchWrapper(iUnknown), iUnknown, VarEnum.VT_DISPATCH };
                yield return new object[] { new DispatchWrapper(iDispatch), iDispatch, VarEnum.VT_DISPATCH };
                yield return new object[] { new DispatchWrapper(iInspectable), iInspectable, VarEnum.VT_DISPATCH };
                yield return new object[] { new DispatchWrapper(nonDual), nonDual, VarEnum.VT_DISPATCH };
                yield return new object[] { new DispatchWrapper(autoDispatch), autoDispatch, VarEnum.VT_DISPATCH };
                yield return new object[] { new DispatchWrapper(autoDual), autoDual, VarEnum.VT_DISPATCH };
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => new DispatchWrapper(10));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
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
            yield return new object[] { new IInspectableComObject[] { new IInspectableComObject(), null } };

            yield return new object[] { new NonDualComObject[] { new NonDualComObject(), null } };
            yield return new object[] { new AutoDispatchComObject[] { new AutoDispatchComObject(), null } };
            yield return new object[] { new AutoDualComObject[] { new AutoDualComObject(), null } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(GetNativeVariantForObject_InvalidArrayType_TestData))]
        public void GetNativeVariantForObject_InvalidArrayType_ThrowsInvalidCastException(object obj)
        {
            Variant v = new Variant();
            IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
            try
            {
                Assert.Throws<InvalidCastException>(() => Marshal.GetNativeVariantForObject(obj, pNative));
                Assert.Throws<InvalidCastException>(() => Marshal.GetNativeVariantForObject<object>(obj, pNative));
            }
            finally
            {
                Marshal.FreeHGlobal(pNative);
            }
        }
    }
}

#pragma warning restore 618
