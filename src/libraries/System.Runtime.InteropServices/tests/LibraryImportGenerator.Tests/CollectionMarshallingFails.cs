// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes;
using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class MarshallingFails
        {
            [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf8)]
            public static partial void Utf8StringSpan(ReadOnlySpan<string> s);

            [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf8)]
            public static partial void Utf8StringArray(string[] s);

            [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf8)]
            public static partial void MarshalSingleDimensionalArray(
                [MarshalUsing(typeof(EnforceLastElementMarshalledCleanupBoolStruct), ElementIndirectionDepth = 1)]
                BoolStruct[] c);

            [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf8)]
            public static partial void MarshalMultidimensionalArray_CheckOuterArrayIsIndexTracked(
                [MarshalUsing(typeof(EnforceLastElementMarshalledCleanupBoolStructArray), ElementIndirectionDepth = 1)]
                BoolStruct[][] c);

            [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf8)]
            public static partial void MarshalMultidimensionalArray_CheckInnerArraysAreCleared(
                [MarshalUsing(typeof(EnforceClearedMemoryCleanup), ElementIndirectionDepth = 2)]
                BoolStruct[][] c);

            [LibraryImport("DoesNotExist")]
            public static partial void MarshalArray_Ref(
                [MarshalUsing(typeof(EnforceLastElementMarshalledCleanupBoolStruct), ElementIndirectionDepth = 1)]
                [MarshalUsing(ConstantElementCount = 10)]
                ref BoolStruct[] c);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_out")]
            public static partial void NegateBoolsOut(
                BoolStruct[] boolStruct,
                int numValues,
                [MarshalUsing(typeof(EnforceAllElementsCleanedUpBoolStruct), ElementIndirectionDepth = 1)]
                [MarshalUsing(CountElementName = nameof(numValues))]
                out BoolStruct[] pBoolStructOut);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_out_2d")]
            public static partial void NegateBoolsOut2D(
                BoolStruct[][] boolStruct,
                int length,
                int[] widths,
                [MarshalUsing(typeof(EnforceAllElementsCleanedUpBoolStruct), ElementIndirectionDepth = 2)]
                [MarshalUsing(CountElementName = nameof(widths), ElementIndirectionDepth = 1)]
                [MarshalUsing(CountElementName = nameof(length))]
                out BoolStruct[][] pBoolStructOut);


            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "fill_range_array")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static partial bool FillRangeArray(
                [MarshalUsing(typeof(EnforceAllElementsCleanedUpIntStruct), ElementIndirectionDepth = 1)]
                [Out]
                IntStructWrapper[] array,
                int length,
                int start);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "fill_range_array_2d")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static partial bool FillRangeArray2D(
                [MarshalUsing(typeof(EnforceAllElementsCleanedUpIntStruct), ElementIndirectionDepth = 2)]
                [MarshalUsing(CountElementName = nameof(widths), ElementIndirectionDepth = 1)]
                [Out]
                IntStructWrapper[][] array,
                int length,
                int[] widths,
                int start);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_ref")]
            public static partial void NegateBoolsRef(
                [MarshalUsing(typeof(EnforceLastElementMarshalledCleanupBoolStruct), ElementIndirectionDepth = 1)]
                [MarshalUsing(CountElementName = nameof(numValues))]
                ref BoolStruct[] boolStruct,
                int numValues);

            [LibraryImport("DoesNotExist", EntryPoint = "negate_bool_struct_array_ref_2d")]
            public static partial void NegateBoolsRef2D_LastElementMarshalling(
                [MarshalUsing(typeof(EnforceLastElementMarshalledCleanupBoolStructArray), ElementIndirectionDepth = 1)]
                [MarshalUsing(CountElementName = nameof(length))]
                ref BoolStruct[][] boolStruct,
                int length);

            [LibraryImport("DoesNotExist", EntryPoint = "negate_bool_struct_array_ref_2d")]
            public static partial void NegateBoolsRef2D_ClearMarshalling(
                [MarshalUsing(typeof(EnforceClearedMemoryCleanup), ElementIndirectionDepth = 2)]
                [MarshalUsing(CountElementName = nameof(widths), ElementIndirectionDepth = 1)]
                [MarshalUsing(CountElementName = nameof(length))]
                ref BoolStruct[][] boolStruct,
                int length,
                int[] widths);
        }
    }

    public class CollectionMarshallingFails
    {
        [Fact]
        public void UTFStringConversionFailures()
        {
            bool threw = false;
            try
            {
                var a = new string[] { new string((char)0xaaaa, 1_000_000_000), "Hello" }; // Conversion of the very long string to utf8 is going to fail
                NativeExportsNE.MarshallingFails.Utf8StringSpan(a);
            }
            catch (ArgumentException) { threw = true; }
            catch (OutOfMemoryException) { threw = true; }
            Assert.True(threw);

            threw = false;
            try
            {
                var a = new string[] { new string((char)0xaaaa, 1_000_000_000), "Hello" }; // Conversion of the very long string to utf8 is going to fail
                NativeExportsNE.MarshallingFails.Utf8StringArray(a);
            }
            catch (ArgumentException) { threw = true; }
            catch (OutOfMemoryException) { threw = true; }
            Assert.True(threw);
        }

        private T[][] GetMultiDimensionalArray<T>(int dim1, int dim2)
        {
            var arr = new T[dim1][];
            for (int i = 0; i < dim1; i++)
            {
                arr[i] = new T[dim2];
            }
            return arr;
        }

        [Fact]
        public void SingleDimensionalArray_EnsureLastIndexArrayIsTracked()
        {
            var arr = new BoolStruct[10];
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                EnforceLastElementMarshalledCleanupBoolStruct.ThrowOnNthMarshalledElement(throwOn);
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.MarshalSingleDimensionalArray(arr);
                });
            }
        }

        [Fact]
        public void MultidimensionalArray_CheckOuterArrayIsIndexTracked()
        {
            var arr = GetMultiDimensionalArray<BoolStruct>(10, 10);
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                EnforceLastElementMarshalledCleanupBoolStructArray.ThrowOnNthMarshalledElement(throwOn);
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.MarshalMultidimensionalArray_CheckOuterArrayIsIndexTracked(arr);
                });
            }
        }

        [Fact]
        public void MultidimensionalArray_CheckInnerArraysAreCleared()
        {
            var arr = GetMultiDimensionalArray<BoolStruct>(10, 10);
            foreach (var throwOn in new int[] { 0, 1, 45, 99 })
            {
                EnforceClearedMemoryCleanup.ThrowOnNthMarshalledElement(throwOn);
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.MarshalMultidimensionalArray_CheckInnerArraysAreCleared(arr);
                });
            }
        }

        [Fact]
        public void SingleDimensionalOutArray_EnsureAllCleaned()
        {
            var arr = new BoolStruct[10];
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                EnforceAllElementsCleanedUpBoolStruct.ThrowOnNthUnmarshalledElement(throwOn);
                EnforceAllElementsCleanedUpBoolStruct.ExpectedCleanupNumber = 10;
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsOut(arr, arr.Length, out var boolsOut);
                });
                EnforceAllElementsCleanedUpBoolStruct.AssertAllHaveBeenCleaned();
            }
            // Run without throwing - this is okay only because the native code doesn't actually use the array, it creates a whole new one
            EnforceAllElementsCleanedUpBoolStruct.ThrowOnNthUnmarshalledElement(-1);
            EnforceAllElementsCleanedUpBoolStruct.ExpectedCleanupNumber = 10;
            NativeExportsNE.MarshallingFails.NegateBoolsOut(arr, arr.Length, out var boolsOut);
            EnforceAllElementsCleanedUpBoolStruct.AssertAllHaveBeenCleaned();
        }

        [Fact]
        public void MultiDimensionalOutArray_EnsureAllCleaned()
        {
            var arr = GetMultiDimensionalArray<BoolStruct>(10, 10);
            var widths = new int[10] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
            foreach (var throwOn in new int[] { 0, 1, 45, 99 })
            {
                EnforceAllElementsCleanedUpBoolStruct.ThrowOnNthUnmarshalledElement(throwOn);
                EnforceAllElementsCleanedUpBoolStruct.ExpectedCleanupNumber = 100;
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsOut2D(arr, arr.Length, widths, out BoolStruct[][] boolsOut);
                });
                EnforceAllElementsCleanedUpBoolStruct.AssertAllHaveBeenCleaned();
            }
            // Run without throwing - this is okay only because the native code doesn't actually use the array, it creates a whole new one
            EnforceAllElementsCleanedUpBoolStruct.ThrowOnNthUnmarshalledElement(-1);
            EnforceAllElementsCleanedUpBoolStruct.ExpectedCleanupNumber = 100;
            NativeExportsNE.MarshallingFails.NegateBoolsOut2D(arr, arr.Length, widths, out BoolStruct[][] boolsOut);
            EnforceAllElementsCleanedUpBoolStruct.AssertAllHaveBeenCleaned();
        }

        [Fact]
        public void SingleDimensionalOutAttributedArray_EnsureAllCleaned()
        {
            var arr = new IntStructWrapper[10];
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                EnforceAllElementsCleanedUpIntStruct.ThrowOnNthUnmarshalledElement(throwOn);
                // FillRangeArray will fill 0-9
                EnforceAllElementsCleanedUpIntStruct.ExpectedFreedElements = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
                Assert.Throws<ArgumentException>(() =>
                    NativeExportsNE.MarshallingFails.FillRangeArray(arr, 10, 0)
                );
                EnforceAllElementsCleanedUpIntStruct.AssertAllHaveBeenCleaned();
            }
            // Run without throwing - this is okay only because the native code doesn't actually use the array, it creates a whole new one
            EnforceAllElementsCleanedUpIntStruct.ThrowOnNthUnmarshalledElement(-1);
            EnforceAllElementsCleanedUpIntStruct.ExpectedFreedElements = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
            NativeExportsNE.MarshallingFails.FillRangeArray(arr, 10, 0);
            EnforceAllElementsCleanedUpIntStruct.AssertAllHaveBeenCleaned();
        }

        [Fact]
        public void MultiDimensionalOutAttributedArray_EnsureAllCleaned()
        {
            var arr = GetMultiDimensionalArray<IntStructWrapper>(10, 10);
            var widths = new int[10] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
            foreach (var throwOn in new int[] { 0, 1, 45, 99 })
            {
                EnforceAllElementsCleanedUpIntStruct.ThrowOnNthUnmarshalledElement(throwOn);
                EnforceAllElementsCleanedUpIntStruct.ExpectedFreedElements = Enumerable.Range(0, 100).ToArray();
                Assert.Throws<ArgumentException>(() =>
                    NativeExportsNE.MarshallingFails.FillRangeArray2D(arr, 10, widths, 0)
                );
                EnforceAllElementsCleanedUpIntStruct.AssertAllHaveBeenCleaned();
            }
            // Run without throwing - this is okay only because the native code doesn't actually use the array, it creates a whole new one
            EnforceAllElementsCleanedUpIntStruct.ThrowOnNthUnmarshalledElement(-1);
            EnforceAllElementsCleanedUpIntStruct.ExpectedFreedElements = Enumerable.Range(0, 100).ToArray();
            NativeExportsNE.MarshallingFails.FillRangeArray2D(arr, 10, widths, 0);
            EnforceAllElementsCleanedUpIntStruct.AssertAllHaveBeenCleaned();
        }

        [Fact]
        public void SingleDimensionalRefArray_EnsureLastIndexArrayIsTracked()
        {
            var arr = new BoolStruct[10];
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                EnforceLastElementMarshalledCleanupBoolStruct.ThrowOnNthMarshalledElement(throwOn);
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsRef(ref arr, arr.Length);
                });
            }
        }

        [Fact]
        public void MultiDimensionalRefArray_EnsureOuterArrayLastIndexArrayIsTracked()
        {
            var arr = GetMultiDimensionalArray<BoolStruct>(10, 10);
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                EnforceLastElementMarshalledCleanupBoolStructArray.ThrowOnNthMarshalledElement(throwOn);
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsRef2D_LastElementMarshalling(ref arr, arr.Length);
                });
            }
        }

        [Fact]
        public void MultiDimensionalRefArray_EnsureInnerArraysAreCleared()
        {
            var arr = GetMultiDimensionalArray<BoolStruct>(10, 10);
            var widths = new int[10] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
            foreach (var throwOn in new int[] { 0, 1, 45, 99 })
            {
                EnforceClearedMemoryCleanup.ThrowOnNthMarshalledElement(throwOn);
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsRef2D_ClearMarshalling(ref arr, arr.Length, widths);
                });
            }
        }
    }

    /// <summary>
    /// Use to ensure that the generated code frees N elements. Make sure to set <see cref="ExpectedCleanupNumber"/> to the number of elements that are expected to be freed, then after calling the LibraryImport method, call <see cref="AssertAllHaveBeenCleaned"/>.
    /// </summary>
    [CustomMarshaller(typeof(IntStructWrapper), MarshalMode.ElementOut, typeof(EnforceAllElementsCleanedUpIntStruct))]
    static unsafe class EnforceAllElementsCleanedUpIntStruct
    {
        private static MarshallingExceptionManager<IntStructWrapper, int> s_IntStructWrapperMarshalling = new(default, default);

        public static void ThrowOnNthMarshalledElement(int n)
        {
            s_IntStructWrapperMarshalling.ThrowOnNthMarshalledElement(n);
        }
        public static void ThrowOnNthUnmarshalledElement(int n)
        {
            s_IntStructWrapperMarshalling.ThrowOnNthUnmarshalledElement(n);
        }

        public static IntStructWrapper ConvertToManaged(int unmanaged) => s_IntStructWrapperMarshalling.ConvertToManaged(unmanaged);

        public static int ConvertToUnmanaged(IntStructWrapper managed) => throw new NotImplementedException();

        /// <summary>
        /// The number of elements that are expected to be cleaned up / freed.
        /// </summary>
        public static int ExpectedCleanupNumber { get; set; } = 0;

        /// <summary>
        /// An array of the unmanaged elements that are expected to be freed, in the order in which they should be freed
        /// </summary>
        public static int[]? ExpectedFreedElements { get => _expectedFreedElements; set => (_expectedFreedElements, ExpectedCleanupNumber, _freedCount) = (value, value.Length, 0); }
        static int[]? _expectedFreedElements;

        public static void AssertAllHaveBeenCleaned(int remaining = 0)
        {
            if (ExpectedCleanupNumber - _freedCount - remaining != 0)
                s_IntStructWrapperMarshalling.Throw($"Incorrected number of elements freed. Expected {ExpectedCleanupNumber - remaining} more elements to be freed.");
            _freedCount = 0;
        }

        static int _freedCount = 0;
        public static void Free(int obj)
        {
            if (ExpectedFreedElements?[_freedCount] is { } expected && obj != expected)
                s_IntStructWrapperMarshalling.Throw($"Unexpected freed item '{obj}', expected '{ExpectedFreedElements[_freedCount]}'");

            if (++_freedCount > ExpectedCleanupNumber)
                s_IntStructWrapperMarshalling.Throw($"Freed too many objects");
        }
    }

    public struct BoolStructNative
    {
        public byte b1;
        public byte b2;
        public byte b3;
        public bool Equals(BoolStructNative other) => other.b1 == b1 && other.b2 == b2 && other.b3 == b3;
    }

    /// <summary>
    /// Use to ensure that the generated code frees N elements. Make sure to set <see cref="ExpectedCleanupNumber"/> to the number of elements that are expected to be freed, then after calling the LibraryImport method, call <see cref="AssertAllHaveBeenCleaned"/>.
    /// </summary>
    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementOut, typeof(EnforceAllElementsCleanedUpBoolStruct))]
    internal static class EnforceAllElementsCleanedUpBoolStruct
    {
        private static MarshallingExceptionManager<BoolStruct, BoolStructNative> s_BoolStructMarshalling = new(default, default);

        public static void ThrowOnNthMarshalledElement(int n)
        {
            s_BoolStructMarshalling.ThrowOnNthMarshalledElement(n);
        }
        public static void ThrowOnNthUnmarshalledElement(int n)
        {
            s_BoolStructMarshalling.ThrowOnNthUnmarshalledElement(n);
        }

        public static BoolStruct ConvertToManaged(BoolStructNative unmanaged) => s_BoolStructMarshalling.ConvertToManaged(unmanaged);

        public static BoolStructNative ConvertToUnmanaged(BoolStruct managed) => throw new NotImplementedException();

        /// <summary>
        /// The number of elements that are expected to be cleaned up / freed.
        /// </summary>
        public static int ExpectedCleanupNumber { get; set; } = 0;

        public static void AssertAllHaveBeenCleaned(int remaining = 0)
        {
            if (ExpectedCleanupNumber - remaining != 0)
                s_BoolStructMarshalling.Throw($"Incorrected number of elements freed. Expected {ExpectedCleanupNumber - remaining} more elements to be freed.");
        }

        public static void Free(BoolStructNative obj)
        {
            if (ExpectedCleanupNumber-- < 0)
                s_BoolStructMarshalling.Throw($"Freed too many objects");
        }
    }

    /// <summary>
    /// Use to ensure that the generated code only frees elements that have been marshalled. It will create a dummy pointer for marshalled elements,
    /// throw an exception when marshaling the Nth element, and ensure all freed memory is the dummy pointer. This will not properly marshal elements,
    /// so the pinvoke should not be run if it will access marshalled objects. Make sure to call ThrowOnNthMarshalledElement such that marshalling
    /// the array will fail before the pinvoke is run.
    /// </summary>
    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementIn, typeof(EnforceLastElementMarshalledCleanupBoolStruct))]
    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementRef, typeof(EnforceLastElementMarshalledCleanupBoolStruct))]
    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementOut, typeof(EnforceLastElementMarshalledCleanupBoolStruct))]
    static class EnforceLastElementMarshalledCleanupBoolStruct
    {
        private static MarshallingExceptionManager<BoolStruct, BoolStructNative> s_BoolStructMarshalling = new(default, new BoolStructNative() { b1 = 0xC1, b2 = 0xBE, b3 = 0xD0 });

        private static BoolStructNative s_dummyBoolStruct = new BoolStructNative() { b1 = 0xC1, b2 = 0xBE, b3 = 0xD0 };

        public static void ThrowOnNthMarshalledElement(int n) => s_BoolStructMarshalling.ThrowOnNthMarshalledElement(n);

        public static BoolStructNative ConvertToUnmanaged(BoolStruct managed) => s_BoolStructMarshalling.ConvertToUnmanaged(managed);

        public static BoolStruct ConvertToManaged(BoolStructNative unmanaged) => throw new NotImplementedException();

        public static void Free(BoolStructNative obj)
        {
            if (!obj.Equals(s_dummyBoolStruct))
                s_BoolStructMarshalling.Throw($"Freed unmarshalled pointer: {{ b1: {obj.b1}, be: {obj.b2}, b3: {obj.b3} }}");
        }
    }

    /// <summary>
    /// Use to ensure that the generated code only frees elements that have been marshalled. It will create a dummy pointer for marshalled elements,
    /// throw an exception when marshaling the Nth element, and ensure all freed memory is the dummy pointer. This will not properly marshal elements,
    /// so the pinvoke should not be run if it will access marshalled objects. Make sure to call ThrowOnNthMarshalledElement such that marshalling
    /// the array will fail before the pinvoke is run.
    /// </summary>
    [CustomMarshaller(typeof(BoolStruct[]), MarshalMode.ElementIn, typeof(EnforceLastElementMarshalledCleanupBoolStructArray))]
    [CustomMarshaller(typeof(BoolStruct[]), MarshalMode.ElementRef, typeof(EnforceLastElementMarshalledCleanupBoolStructArray))]
    static class EnforceLastElementMarshalledCleanupBoolStructArray
    {
        private static MarshallingExceptionManager<BoolStruct[], nint> s_BoolStructArrayMarshalling = new(default, _dummyPtr);

        public static void ThrowOnNthMarshalledElement(int n) => s_BoolStructArrayMarshalling.ThrowOnNthMarshalledElement(n);

        static nint _dummyPtr => 0xA1FA1FA;

        public static nint ConvertToUnmanaged(BoolStruct[] managed) => s_BoolStructArrayMarshalling.ConvertToUnmanaged(managed);

        public static void Free(nint obj)
        {
            if (obj != _dummyPtr)
                s_BoolStructArrayMarshalling.Throw($"Freed unmarshalled pointer: {obj}");
        }

        public static BoolStruct[] ConvertToManaged(nint unmanaged) => throw new NotImplementedException();
    }


    /// <summary>
    /// Use to ensure that an array is cleared before elements are marshalled. It will create a dummy pointer for marshalled elements, throw an exception when marshaling the Nth element,
    /// and ensure all freed memory is either the dummy pointer or null. This will not properly marshal elements, so the pinvoke should not be run if it will access marshalled objects.
    /// Make sure to call ThrowOnNthMarshalledElement such that marshalling the array will fail before the pinvoke is run.
    /// </summary>
    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementIn, typeof(EnforceClearedMemoryCleanup))]
    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementRef, typeof(EnforceClearedMemoryCleanup))]
    static class EnforceClearedMemoryCleanup
    {
        private static MarshallingExceptionManager<BoolStruct, BoolStructNative> s_exceptionManager = new(default, s_dummyBoolStruct);

        private static BoolStructNative s_dummyBoolStruct = new BoolStructNative() { b1 = 0xC1, b2 = 0xBE, b3 = 0xD0 };

        public static void ThrowOnNthMarshalledElement(int n) => s_exceptionManager.ThrowOnNthMarshalledElement(n);

        public static int ThrowOnElementNumber { get; set; } = -1;

        public static BoolStructNative ConvertToUnmanaged(BoolStruct managed) => s_exceptionManager.ConvertToUnmanaged(managed);

        public static BoolStruct ConvertToManaged(BoolStructNative unmanaged) => throw new NotImplementedException();

        public static void Free(BoolStructNative obj)
        {
            if (!(obj.Equals(s_dummyBoolStruct) || obj.Equals(default)))
                s_exceptionManager.Throw($"Freed unmarshalled value: {{ b1: {obj.b1}, b2: {obj.b2}, b3: {obj.b3} }}");
        }
    }

    /// <summary>
    /// A class that provides helpers to managed throwing exceptions during marshalling collections.
    /// Marshals and unmarshalls
    /// </summary>
    /// <typeparam name="TManaged"></typeparam>
    /// <typeparam name="TUnmanaged"></typeparam>
    internal class MarshallingExceptionManager<TManaged, TUnmanaged>
    {
        private int _marshalledCount = 0;
        private int _unmarshalledCount = 0;
        private int _throwOnMarshallingElement = -1;
        private int _throwOnUnmarshallingElement = -1;
        private readonly TUnmanaged _marshalledValue;
        private readonly TManaged _unmarshalledValue;

        public MarshallingExceptionManager(TManaged unmarshalledValue, TUnmanaged marshalledValue)
        {
            _marshalledValue = marshalledValue;
            _unmarshalledValue = unmarshalledValue;
        }

        /// <summary>
        /// Force marshalling to fail on the nth element.
        /// </summary>
        /// <param name="n"></param>
        public void ThrowOnNthMarshalledElement(int n)
        {
            _marshalledCount = 0;
            _throwOnMarshallingElement = n;
        }

        /// <summary>
        /// Force unmarshalling to fail on the nth element.
        /// </summary>
        public void ThrowOnNthUnmarshalledElement(int n)
        {
            _unmarshalledCount = 0;
            _throwOnUnmarshallingElement = n;
        }

        public TUnmanaged ConvertToUnmanaged(TManaged managed)
        {
            if (_marshalledCount++ == _throwOnMarshallingElement)
            {
                _marshalledCount = 0;
                _throwOnMarshallingElement = -1;
                throw new ArgumentException("Marshalling failed");
            }
            return _marshalledValue;
        }

        public TManaged ConvertToManaged(TUnmanaged unmanaged)
        {
            if (_unmarshalledCount++ == _throwOnUnmarshallingElement)
            {
                _unmarshalledCount = 0;
                _throwOnUnmarshallingElement = -1;
                throw new ArgumentException("Unmarshalling failed");
            }
            return _unmarshalledValue;
        }

        public void Throw(string message) => throw new InvalidMarshallingException(message);

        /// <summary>
        /// An exception that isn't able to be accidentally caught by try catch blocks (Except for catch (Exception e))
        /// </summary>
        [Serializable]
        private sealed class InvalidMarshallingException : Exception
        {
            public InvalidMarshallingException(string? message) : base(message)
            {
            }
        }
    }
}
