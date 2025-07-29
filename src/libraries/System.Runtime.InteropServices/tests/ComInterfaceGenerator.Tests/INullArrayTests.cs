// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using SharedTypes;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    /// <summary>
    /// Tests for edge cases involving null arrays when their length parameters are non-zero.
    /// This addresses https://github.com/dotnet/runtime/issues/118135
    /// </summary>
    public unsafe partial class INullArrayTests
    {
        private static INullArrayCases CreateTestInterface()
        {
            INullArrayCases originalObject = new INullArrayCasesImpl();
            ComWrappers cw = new StrategyBasedComWrappers();
            nint ptr = cw.GetOrCreateComInterfaceForObject(originalObject, CreateComInterfaceFlags.None);
            object obj = cw.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.None);
            return (INullArrayCases)obj;
        }

        [Fact]
        public void SingleNullArray_WithNonZeroLength_DoesNotCrash()
        {
            // Arrange
            var testInterface = CreateTestInterface();

            // Act & Assert - Should not throw or crash
            testInterface.SingleNullArrayWithLength(10, null);
        }

        [Fact]
        public void SingleNullArray_WithZeroLength_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            // Should not throw or crash
            testInterface.SingleNullArrayWithLength(0, null);
        }

        [Fact]
        public void SingleNullArray_WithValidArray_WorksNormally()
        {
            var testInterface = CreateTestInterface();
            int[] array = new int[5];

            testInterface.SingleNullArrayWithLength(5, array);

            Assert.Equal(new int[] { 0, 2, 4, 6, 8 }, array);
        }

        [Fact]
        public void MultipleArrays_SomeNull_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();
            int[] array1 = new int[3];
            int[] array3 = new int[3];

            testInterface.MultipleArraysSharedLength(3, array1, null, array3);

            Assert.Equal(new int[] { 0, 1, 2 }, array1);
            Assert.Equal(new int[] { 0, 100, 200 }, array3);
        }

        [Fact]
        public void MultipleArrays_AllNull_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            testInterface.MultipleArraysSharedLength(5, null, null, null);
        }

        [Fact]
        public void NonBlittableArray_Null_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            testInterface.NonBlittableNullArray(10, null);
        }

        [Fact]
        public void NonBlittableArray_ValidArray_WorksNormally()
        {
            var testInterface = CreateTestInterface();
            var array = new IntStructWrapper[3];

            testInterface.NonBlittableNullArray(3, array);

            Assert.Equal(0, array[0].Value);
            Assert.Equal(3, array[1].Value);
            Assert.Equal(6, array[2].Value);
        }

        [Fact]
        public void ZeroLength_NullArray_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            testInterface.ZeroLengthArray(0, null);
        }

        [Fact]
        public void LargeLength_NullArray_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            testInterface.LargeLengthNullArray(int.MaxValue, null);
        }

        [Fact]
        public void SpanNull_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            var (__this, __vtable) = ((IUnmanagedVirtualMethodTableProvider)testInterface).GetVirtualMethodTableInfoForKey(typeof(INullArrayCases));
            int length = 10;
            var __target = (delegate* unmanaged[MemberFunction]<void*, int, int**, int>)__vtable[11];
            int* __span_native = null;
            int __invokeRetVal = __target(__this, length, &__span_native);
            Marshal.ThrowExceptionForHR(__invokeRetVal);
        }

        [Fact]
        public void SpanNull_WithZeroLength_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            var (__this, __vtable) = ((IUnmanagedVirtualMethodTableProvider)testInterface).GetVirtualMethodTableInfoForKey(typeof(INullArrayCases));
            int length = 0;
            var __target = (delegate* unmanaged[MemberFunction]<void*, int, int**, int>)__vtable[11];
            int* __span_native = null;
            int __invokeRetVal = __target(__this, length, &__span_native);
            Marshal.ThrowExceptionForHR(__invokeRetVal);
        }

        [Fact]
        public void SpanNull_WithLargeLength_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            var (__this, __vtable) = ((IUnmanagedVirtualMethodTableProvider)testInterface).GetVirtualMethodTableInfoForKey(typeof(INullArrayCases));
            int length = int.MaxValue;
            var __target = (delegate* unmanaged[MemberFunction]<void*, int, int**, int>)__vtable[11];
            int* __span_native = null;
            int __invokeRetVal = __target(__this, length, &__span_native);
            Marshal.ThrowExceptionForHR(__invokeRetVal);
        }

        [Fact]
        public void SpanNonBlittable_Null_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            var (__this, __vtable) = ((IUnmanagedVirtualMethodTableProvider)testInterface).GetVirtualMethodTableInfoForKey(typeof(INullArrayCases));
            int length = 10;
            var __target = (delegate* unmanaged[MemberFunction]<void*, int, IntStructWrapper**, int>)__vtable[12];
            IntStructWrapper* __span_native = null;
            int __invokeRetVal = __target(__this, length, &__span_native);
            Marshal.ThrowExceptionForHR(__invokeRetVal);
        }

        [Fact]
        public void SpanValid_WorksNormally()
        {
            var testInterface = CreateTestInterface();

            var span = new Span<int>(new int[5]);
            testInterface.SpanNullCase(5, ref span);

            Assert.Equal(0, span[0]);
            Assert.Equal(5, span[1]);
            Assert.Equal(10, span[2]);
            Assert.Equal(15, span[3]);
            Assert.Equal(20, span[4]);
        }

        [Fact]
        public void SpanNonBlittableValid_WorksNormally()
        {
            var testInterface = CreateTestInterface();

            var span = new Span<IntStructWrapper>(new IntStructWrapper[3]);
            testInterface.SpanNonBlittableNullCase(3, ref span);

            Assert.Equal(0, span[0].Value);
            Assert.Equal(7, span[1].Value);
            Assert.Equal(14, span[2].Value);
        }

        [Fact]
        public void InputOnlyArray_Null_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            testInterface.InOnlyNullArray(10, null);
        }

        [Fact]
        public void OutputOnlyArray_Null_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            testInterface.OutOnlyNullArray(10, null);
        }

        [Fact]
        public void ReferenceArray_Null_DoesNotCrash()
        {
            var testInterface = CreateTestInterface();

            testInterface.ReferenceArrayNullCase(10, null);
        }

        [Fact]
        public void ReferenceArray_ValidArray_WorksNormally()
        {
            var testInterface = CreateTestInterface();
            string[] array = new string[3];

            testInterface.ReferenceArrayNullCase(3, array);

            Assert.Equal(new string[] { "Item 0", "Item 1", "Item 2" }, array);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public void NullArray_VariousLengths_DoesNotCrash(int length)
        {
            var testInterface = CreateTestInterface();

            testInterface.SingleNullArrayWithLength(length, null);
        }
    }
}
