// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        [SkipOnCI("Allocates enough memory that the OOM killer can kill the process on our Helix machines.")]
        public void BigUTFStringConversionFailures()
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
            //foreach (var throwOn in new int[] { 0, 1, 45, 99 })
            //{
            //    EnforceAllElementsCleanedUpBoolStruct.ThrowOnNthUnmarshalledElement(throwOn);
            //    EnforceAllElementsCleanedUpBoolStruct.ExpectedCleanupNumber = 100;
            //    Assert.Throws<ArgumentException>(() =>
            //    {
            //        NativeExportsNE.MarshallingFails.NegateBoolsOut2D(arr, arr.Length, widths, out BoolStruct[][] boolsOut);
            //    });
            //    EnforceAllElementsCleanedUpBoolStruct.AssertAllHaveBeenCleaned();
            //}
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
                EnforceAllElementsCleanedUpIntStruct.ExpectedCleanupNumber = 10;
                Assert.Throws<ArgumentException>(() =>
                    NativeExportsNE.MarshallingFails.FillRangeArray(arr, 10, 0)
                );
                EnforceAllElementsCleanedUpIntStruct.AssertAllHaveBeenCleaned();
            }
            // Run without throwing - this is okay only because the native code doesn't actually use the array, it creates a whole new one
            EnforceAllElementsCleanedUpIntStruct.ThrowOnNthUnmarshalledElement(-1);
            EnforceAllElementsCleanedUpIntStruct.ExpectedCleanupNumber = 10;
            NativeExportsNE.MarshallingFails.FillRangeArray(arr, 0, 9);
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
                EnforceAllElementsCleanedUpIntStruct.ExpectedCleanupNumber = 100;
                Assert.Throws<ArgumentException>(() =>
                    NativeExportsNE.MarshallingFails.FillRangeArray2D(arr, 10, widths, 0)
                );
                EnforceAllElementsCleanedUpIntStruct.AssertAllHaveBeenCleaned();
            }
            // Run without throwing - this is okay only because the native code doesn't actually use the array, it creates a whole new one
            EnforceAllElementsCleanedUpIntStruct.ThrowOnNthUnmarshalledElement(-1);
            EnforceAllElementsCleanedUpIntStruct.ExpectedCleanupNumber = 100;
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
        private static MarshallingExceptionManager<IntStructWrapper> s_IntStructWrapperMarshalling = new(0, default);

        public static void ThrowOnNthMarshalledElement(int n)
        {
            s_IntStructWrapperMarshalling.ThrowOnNthMarshalledElement(n);
        }
        public static void ThrowOnNthUnmarshalledElement(int n)
        {
            s_IntStructWrapperMarshalling.ThrowOnNthUnmarshalledElement(n);
        }

        public static IntStructWrapper ConvertToManaged(nint unmanaged) => s_IntStructWrapperMarshalling.ConvertToManaged(unmanaged);

        public static nint ConvertToUnmanaged(IntStructWrapper managed) => throw new NotImplementedException();

        /// <summary>
        /// The number of elements that are expected to be cleaned up / freed.
        /// </summary>
        public static int ExpectedCleanupNumber { get; set; } = 0;

        public static void AssertAllHaveBeenCleaned(int remaining = 0)
        {
            if (ExpectedCleanupNumber - remaining != 0)
                s_IntStructWrapperMarshalling.Throw($"Incorrected number of elements freed. Expected {ExpectedCleanupNumber - remaining} more elements to be freed.");
        }

        public static void Free(nint obj)
        {
            if (ExpectedCleanupNumber-- < 0)
                s_IntStructWrapperMarshalling.Throw($"Freed too many objects");
        }
    }

    /// <summary>
    /// Use to ensure that the generated code frees N elements. Make sure to set <see cref="ExpectedCleanupNumber"/> to the number of elements that are expected to be freed, then after calling the LibraryImport method, call <see cref="AssertAllHaveBeenCleaned"/>.
    /// </summary>
    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementOut, typeof(EnforceAllElementsCleanedUpBoolStruct))]
    internal static class EnforceAllElementsCleanedUpBoolStruct
    {
        private static MarshallingExceptionManager<BoolStruct> s_BoolStructMarshalling = new(0, default);

        public static void ThrowOnNthMarshalledElement(int n)
        {
            s_BoolStructMarshalling.ThrowOnNthMarshalledElement(n);
        }
        public static void ThrowOnNthUnmarshalledElement(int n)
        {
            s_BoolStructMarshalling.ThrowOnNthUnmarshalledElement(n);
        }

        public static BoolStruct ConvertToManaged(nint unmanaged) => s_BoolStructMarshalling.ConvertToManaged(unmanaged);

        public static nint ConvertToUnmanaged(BoolStruct managed) => throw new NotImplementedException();

        /// <summary>
        /// The number of elements that are expected to be cleaned up / freed.
        /// </summary>
        public static int ExpectedCleanupNumber { get; set; } = 0;

        public static void AssertAllHaveBeenCleaned(int remaining = 0)
        {
            if (ExpectedCleanupNumber - remaining != 0)
                s_BoolStructMarshalling.Throw($"Incorrected number of elements freed. Expected {ExpectedCleanupNumber - remaining} more elements to be freed.");
        }

        public static void Free(nint obj)
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
        private static MarshallingExceptionManager<BoolStruct> s_BoolStructMarshalling = new(_dummyPtr, default);

        public static void ThrowOnNthMarshalledElement(int n) => s_BoolStructMarshalling.ThrowOnNthMarshalledElement(n);

        static nint _dummyPtr => 0xA1FA1FA;

        public static nint ConvertToUnmanaged(BoolStruct managed) => s_BoolStructMarshalling.ConvertToUnmanaged(managed);

        public static void Free(nint obj)
        {
            if (obj != _dummyPtr)
                s_BoolStructMarshalling.Throw($"Freed unmarshalled pointer: {obj}");
        }

        public static BoolStruct ConvertToManaged(nint unmanaged) => throw new NotImplementedException();
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
        private static MarshallingExceptionManager<BoolStruct[]> s_BoolStructArrayMarshalling = new(_dummyPtr, default);

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
        private static MarshallingExceptionManager<BoolStruct> s_exceptionManager = new(_dummyPtr, default);

        public static void ThrowOnNthMarshalledElement(int n) => s_exceptionManager.ThrowOnNthMarshalledElement(n);

        public static int ThrowOnElementNumber { get; set; } = -1;

        static nint _dummyPtr => 0xA1FA1FA;

        public static nint ConvertToUnmanaged(BoolStruct managed) => s_exceptionManager.ConvertToUnmanaged(managed);

        public static BoolStruct ConvertToManaged(nint unmanaged) => throw new NotImplementedException();

        public static void Free(nint obj)
        {
            if (obj != _dummyPtr && obj != 0)
                s_exceptionManager.Throw($"Freed unmarshalled pointer: {obj}");
        }
    }

    internal class MarshallingExceptionManager<TManaged>
    {
        private int _marshalledCount = 0;
        private int _unmarshalledCount = 0;
        private int _throwOnMarshallingElement = -1;
        private int _throwOnUnmarshallingElement = -1;
        private readonly nint _marshalledValue;
        private readonly TManaged _unmarshalledValue;

        public MarshallingExceptionManager(nint marshalledValue, TManaged unmarshalledValue)
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

        public nint ConvertToUnmanaged(TManaged managed)
        {
            if (_marshalledCount++ == _throwOnMarshallingElement)
            {
                _marshalledCount = 0;
                _throwOnMarshallingElement = -1;
                throw new ArgumentException("Marshalling failed");
            }
            return _marshalledValue;
        }

        public TManaged ConvertToManaged(nint unmanaged)
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

        [Serializable]
        private sealed class InvalidMarshallingException : Exception
        {
            public InvalidMarshallingException(string? message) : base(message)
            {
            }
        }
    }
}
