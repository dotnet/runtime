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
                [MarshalUsing(typeof(BoolStructInMarshaller), ElementIndirectionDepth = 1)]
                BoolStruct[] c);

            [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf8)]
            public static partial void MarshalMultidimensionalArray_CheckOuterArrayIsIndexTracked(
                [MarshalUsing(typeof(BoolStructArrayMarshaller), ElementIndirectionDepth = 1)]
                BoolStruct[][] c);

            [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf8)]
            public static partial void MarshalMultidimensionalArray_CheckInnerArraysAreCleared(
                [MarshalUsing(typeof(BoolStructInMarshallerAllowNull), ElementIndirectionDepth = 2)]
                BoolStruct[][] c);

            [LibraryImport("DoesNotExist")]
            public static partial void MarshalArray_Ref(
                [MarshalUsing(typeof(BoolStructInMarshaller), ElementIndirectionDepth = 1)]
                [MarshalUsing(ConstantElementCount = 10)]
                ref BoolStruct[] c);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_out")]
            public static partial void NegateBoolsOut(
                [MarshalUsing(typeof(BoolStructInMarshaller), ElementIndirectionDepth = 1)]
                BoolStruct[] boolStruct,
                int numValues,
                [MarshalUsing(typeof(BoolStructOutMarshaller), ElementIndirectionDepth = 1)]
                [MarshalUsing(CountElementName = nameof(numValues))]
                out BoolStruct[] pBoolStructOut);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_out_2d")]
            public static partial void NegateBoolsOut2D(
                [MarshalUsing(typeof(BoolStructInMarshaller), ElementIndirectionDepth = 2)]
                BoolStruct[][] boolStruct,
                int length,
                int[] widths,
                [MarshalUsing(typeof(BoolStructOutMarshaller), ElementIndirectionDepth = 2)]
                [MarshalUsing(CountElementName = nameof(widths), ElementIndirectionDepth = 1)]
                [MarshalUsing(CountElementName = nameof(length))]
                out BoolStruct[][] pBoolStructOut);


            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "fill_range_array")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static partial bool FillRangeArray(
                [MarshalUsing(typeof(FillRangeArrayMarshaller), ElementIndirectionDepth = 1)]
                [Out]
                IntStructWrapper[] array,
                int length,
                int start);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "fill_range_array_2d")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static partial bool FillRangeArray2D(
                [MarshalUsing(typeof(FillRangeArrayMarshaller), ElementIndirectionDepth = 2)]
                [MarshalUsing(CountElementName = nameof(widths), ElementIndirectionDepth = 1)]
                [Out]
                IntStructWrapper[][] array,
                int length,
                int[] widths,
                int start);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_ref")]
            public static partial void NegateBoolsRef(
                [MarshalUsing(typeof(BoolStructInMarshaller), ElementIndirectionDepth = 1)]
                [MarshalUsing(CountElementName = nameof(numValues))]
                ref BoolStruct[] boolStruct,
                int numValues);

            [LibraryImport("DoesNotExist", EntryPoint = "negate_bool_struct_array_ref_2d")]
            public static partial void NegateBoolsRef2D_LastElementMarshalling(
                [MarshalUsing(typeof(BoolStructArrayMarshaller), ElementIndirectionDepth = 1)]
                [MarshalUsing(CountElementName = nameof(length))]
                ref BoolStruct[][] boolStruct,
                int length);

            [LibraryImport("DoesNotExist", EntryPoint = "negate_bool_struct_array_ref_2d")]
            public static partial void NegateBoolsRef2D_ClearMarshalling(
                [MarshalUsing(typeof(BoolStructInMarshallerAllowNull), ElementIndirectionDepth = 2)]
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
            var myBoolStruct = new BoolStruct() { b1 = true, b2 = true, b3 = false };
            var myBoolStructNative = new BoolStructNative() { b1 = 1, b2 = 1, b3 = 0 };
            var arr = Enumerable.Range(0, 10).Select(_ => myBoolStruct).ToArray();
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                BoolStructInMarshaller.Marshaller.MarshallingFailsIndex = throwOn;
                BoolStructInMarshaller.Marshaller.ExpectedFreedValues = Enumerable.Range(0, throwOn).Select(i => myBoolStructNative).ToArray();
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
                BoolStructArrayMarshaller.Marshaller.MarshallingFailsIndex = throwOn;
                BoolStructArrayMarshaller.Marshaller.ExpectedFreeCount = throwOn;
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.MarshalMultidimensionalArray_CheckOuterArrayIsIndexTracked(arr);
                });
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/93423")]
        public void MultidimensionalArray_CheckInnerArraysAreCleared()
        {
            var arr = GetMultiDimensionalArray<BoolStruct>(10, 10);
            foreach (var throwOn in new int[] { 0, 1, 45, 99 })
            {
                BoolStructInMarshallerAllowNull.Marshaller.MarshallingFailsIndex = throwOn;
                BoolStructInMarshallerAllowNull.Marshaller.ExpectedFreeCount = throwOn;
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.MarshalMultidimensionalArray_CheckInnerArraysAreCleared(arr);
                });
                BoolStructInMarshallerAllowNull.Marshaller.AssertAllHaveBeenCleaned();
            }
        }

        [Fact]
        public void SingleDimensionalOutArray_EnsureAllCleaned()
        {
            BoolStruct[] arr = new BoolStruct[10].Select(_ => new BoolStruct() { b1 = true, b2 = true, b3 = false }).ToArray();
            BoolStructNative[] nativeArr = new BoolStruct[10].Select(_ => new BoolStructNative() { b1 = 1, b2 = 1, b3 = 0 }).ToArray();
            BoolStructNative[] nativeNegated = new BoolStruct[10].Select(_ => new BoolStructNative() { b1 = 0, b2 = 0, b3 = 1 }).ToArray();
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                BoolStructOutMarshaller.Marshaller.UnmarshallingFailsIndex = throwOn;
                BoolStructInMarshaller.Marshaller.ExpectedFreedValues = nativeArr;
                BoolStructOutMarshaller.Marshaller.ExpectedFreedValues = nativeNegated;
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsOut(arr, arr.Length, out var boolsOut);
                });
                BoolStructInMarshaller.Marshaller.AssertAllHaveBeenCleaned();
                BoolStructOutMarshaller.Marshaller.AssertAllHaveBeenCleaned();
            }
            // Run without throwing - this is okay only because the native code doesn't actually use the array, it creates a whole new one
            BoolStructOutMarshaller.Marshaller.UnmarshallingFailsIndex = -1;
            BoolStructInMarshaller.Marshaller.ExpectedFreedValues = nativeArr;
            BoolStructOutMarshaller.Marshaller.ExpectedFreedValues = nativeNegated;
            NativeExportsNE.MarshallingFails.NegateBoolsOut(arr, arr.Length, out var boolsOut);
            BoolStructInMarshaller.Marshaller.AssertAllHaveBeenCleaned();
            BoolStructOutMarshaller.Marshaller.AssertAllHaveBeenCleaned();
        }

        [Fact]
        public void MultiDimensionalOutArray_EnsureAllCleaned()
        {
            var arr = GetMultiDimensionalArray<BoolStruct>(10, 10);
            var widths = new int[10] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
            foreach (var throwOn in new int[] { 0, 1, 45, 99 })
            {
                // Set up unmarshalling asserts
                BoolStructOutMarshaller.Marshaller.UnmarshallingFailsIndex = throwOn;
                BoolStructOutMarshaller.Marshaller.ExpectedFreedValues = Enumerable.Range(0, 100).Select(_ => new BoolStructNative() { b1 = 1, b2 = 1, b3 = 1 }).ToArray();
                // https://github.com/dotnet/runtime/issues/93423
                //NegateBoolStructInMarshaller.Marshaller.ExpectedFreedValues = Enumerable.Range(0, 100).Select(_ => new BoolStructNative() { b1 = 0, b2 = 0, b3 = 0 }).ToArray();
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsOut2D(arr, arr.Length, widths, out BoolStruct[][] boolsOut);
                });
                // https://github.com/dotnet/runtime/issues/93423
                //NegateBoolStructInMarshaller.Marshaller.AssertAllHaveBeenCleaned();
                BoolStructInMarshaller.Marshaller.Reset();
                BoolStructOutMarshaller.Marshaller.AssertAllHaveBeenCleaned();
            }
            // Run without throwing - this is okay only because the native code doesn't actually use the array, it creates a whole new one
            BoolStructOutMarshaller.Marshaller.UnmarshallingFailsIndex = -1;
            BoolStructOutMarshaller.Marshaller.ExpectedFreedValues = Enumerable.Range(0, 100).Select(_ => new BoolStructNative() { b1 = 1, b2 = 1, b3 = 1 }).ToArray();
            // https://github.com/dotnet/runtime/issues/93423
            //NegateBoolStructInMarshaller.Marshaller.UnmarshallingFailsIndex = -1;
            //NegateBoolStructInMarshaller.Marshaller.ExpectedFreeCount = 100;
            NativeExportsNE.MarshallingFails.NegateBoolsOut2D(arr, arr.Length, widths, out BoolStruct[][] boolsOut);
            // https://github.com/dotnet/runtime/issues/93423
            //NegateBoolStructInMarshaller.Marshaller.AssertAllHaveBeenCleaned();
            BoolStructInMarshaller.Marshaller.Reset();
            BoolStructOutMarshaller.Marshaller.AssertAllHaveBeenCleaned();
        }

        [Fact]
        public void SingleDimensionalOutAttributedArray_EnsureAllCleaned()
        {
            var arr = new IntStructWrapper[10];
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                FillRangeArrayMarshaller.Marshaller.UnmarshallingFailsIndex = throwOn;
                // FillRangeArray will fill 0-9
                FillRangeArrayMarshaller.Marshaller.ExpectedFreedValues = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
                Assert.Throws<ArgumentException>(() =>
                    NativeExportsNE.MarshallingFails.FillRangeArray(arr, arr.Length, 0)
                );
                FillRangeArrayMarshaller.Marshaller.AssertAllHaveBeenCleaned();
            }
            FillRangeArrayMarshaller.Marshaller.UnmarshallingFailsIndex = -1;
            // FillRangeArray will fill 0-9
            FillRangeArrayMarshaller.Marshaller.ExpectedFreedValues = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
            NativeExportsNE.MarshallingFails.FillRangeArray(arr, arr.Length, 0);
            FillRangeArrayMarshaller.Marshaller.AssertAllHaveBeenCleaned();
        }

        [Fact]
        public void MultiDimensionalOutAttributedArray_EnsureAllCleaned()
        {
            var arr = GetMultiDimensionalArray<IntStructWrapper>(10, 10);
            var widths = new int[10] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
            foreach (var throwOn in new int[] { 0, 1, 45, 99 })
            {
                FillRangeArrayMarshaller.Marshaller.UnmarshallingFailsIndex = throwOn;
                FillRangeArrayMarshaller.Marshaller.ExpectedFreedValues = Enumerable.Range(0, 100).ToArray();
                Assert.Throws<ArgumentException>(() =>
                    NativeExportsNE.MarshallingFails.FillRangeArray2D(arr, arr.Length, widths, 0)
                );
                FillRangeArrayMarshaller.Marshaller.AssertAllHaveBeenCleaned();
            }
            FillRangeArrayMarshaller.Marshaller.UnmarshallingFailsIndex = -1;
            FillRangeArrayMarshaller.Marshaller.ExpectedFreedValues = Enumerable.Range(0, 100).ToArray();
            NativeExportsNE.MarshallingFails.FillRangeArray2D(arr, arr.Length, widths, 0);
            FillRangeArrayMarshaller.Marshaller.AssertAllHaveBeenCleaned();
        }

        [Fact]
        public void SingleDimensionalRefArray_EnsureLastIndexArrayIsTracked()
        {
            var myBoolStruct = new BoolStruct() { b1 = true, b2 = true, b3 = false };
            var myBoolStructNative = new BoolStructNative() { b1 = 1, b2 = 1, b3 = 0 };
            var arr = Enumerable.Range(0, 10).Select(_ => myBoolStruct).ToArray();
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                BoolStructInMarshaller.Marshaller.MarshallingFailsIndex = throwOn;
                BoolStructInMarshaller.Marshaller.ExpectedFreedValues = Enumerable.Range(0, throwOn).Select(i => myBoolStructNative).ToArray();
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsRef(ref arr, arr.Length);
                });
                BoolStructInMarshaller.Marshaller.AssertAllHaveBeenCleaned();
            }
        }

        [Fact]
        public void MultiDimensionalRefArray_EnsureOuterArrayLastIndexArrayIsTracked()
        {
            var arr = GetMultiDimensionalArray<BoolStruct>(10, 10);
            foreach (var throwOn in new int[] { 0, 1, 5, 9 })
            {
                BoolStructArrayMarshaller.Marshaller.MarshallingFailsIndex = throwOn;
                BoolStructArrayMarshaller.Marshaller.ExpectedFreeCount = throwOn;
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsRef2D_LastElementMarshalling(ref arr, arr.Length);
                });
                BoolStructArrayMarshaller.Marshaller.AssertAllHaveBeenCleaned();
            }
        }

        [Fact]
        public void MultiDimensionalRefArray_EnsureInnerArraysAreCleared()
        {
            var arr = GetMultiDimensionalArray<BoolStruct>(10, 10);
            var widths = new int[10] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
            foreach (var throwOn in new int[] { 0, 1, 45, 99 })
            {
                BoolStructInMarshallerAllowNull.Marshaller.MarshallingFailsIndex = throwOn;
                // https://github.com/dotnet/runtime/issues/93431
                BoolStructInMarshallerAllowNull.Marshaller.ExpectedFreeCount = throwOn - throwOn % 10;
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsRef2D_ClearMarshalling(ref arr, arr.Length, widths);
                });
                BoolStructInMarshallerAllowNull.Marshaller.AssertAllHaveBeenCleaned();
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/93431")]
        public void MultiDimensionalRefArray_EnsureInnerArraysAreCleared_ProperCleanup()
        {
            var arr = GetMultiDimensionalArray<BoolStruct>(10, 10);
            var widths = new int[10] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
            foreach (var throwOn in new int[] { 0, 1, 45, 99 })
            {
                BoolStructInMarshallerAllowNull.Marshaller.MarshallingFailsIndex = throwOn;
                // Expected Behavior - Should free all elements of inner arrays that were partially marshalled
                BoolStructInMarshallerAllowNull.Marshaller.ExpectedFreeCount = throwOn + 10 - (throwOn % 10);
                Assert.Throws<ArgumentException>(() =>
                {
                    NativeExportsNE.MarshallingFails.NegateBoolsRef2D_ClearMarshalling(ref arr, arr.Length, widths);
                });
                BoolStructInMarshallerAllowNull.Marshaller.AssertAllHaveBeenCleaned();
            }
        }
    }

    public struct BoolStructNative : IEquatable<BoolStructNative>
    {
        public byte b1;
        public byte b2;
        public byte b3;
        public bool Equals(BoolStructNative other) => other.b1 == b1 && other.b2 == b2 && other.b3 == b3;
    }

    [CustomMarshaller(typeof(BoolStruct[]), MarshalMode.ElementIn, typeof(BoolStructArrayMarshaller))]
    [CustomMarshaller(typeof(BoolStruct[]), MarshalMode.ElementRef, typeof(BoolStructArrayMarshaller))]
    static class BoolStructArrayMarshaller
    {
        public static FailingMarshaller<BoolStruct[], nint> Marshaller = new(
            _ => 0xa1fa1fa,
            _ => throw new NotImplementedException(),
            (nint unmanaged, int index) => unmanaged == 0xa1fa1fa
        );

        public static nint ConvertToUnmanaged(BoolStruct[] managed) => Marshaller.ConvertToUnmanaged(managed);
        public static BoolStruct[] ConvertToManaged(nint unmanaged) => throw new NotImplementedException();
        public static void Free(nint unmanaged) => Marshaller.Free(unmanaged);
    }

    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementOut, typeof(BoolStructOutMarshaller))]
    public static class BoolStructOutMarshaller
    {
        public static FailingMarshaller<BoolStruct, BoolStructNative> Marshaller = new(
            BoolStructMarshaller.ConvertToUnmanaged,
            BoolStructMarshaller.ConvertToManaged,
            (BoolStructNative unmanaged, int index) => Marshaller.ExpectedFreedValues != null
        );

        public static BoolStruct ConvertToManaged(BoolStructNative unmanaged) => Marshaller.ConvertToManaged(unmanaged);
        public static BoolStructNative ConvertToUnmanaged(BoolStruct managed) => Marshaller.ConvertToUnmanaged(managed);
        public static void Free(BoolStructNative unmanaged) => Marshaller.Free(unmanaged);
    }

    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementIn, typeof(BoolStructInMarshaller))]
    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementRef, typeof(BoolStructInMarshaller))]
    public static class BoolStructInMarshaller
    {
        public static FailingMarshaller<BoolStruct, BoolStructNative> Marshaller = new(
            BoolStructMarshaller.ConvertToUnmanaged,
            BoolStructMarshaller.ConvertToManaged,
            (BoolStructNative unmanaged, int index) => Marshaller.ExpectedFreedValues != null
        );

        public static BoolStruct ConvertToManaged(BoolStructNative unmanaged) => Marshaller.ConvertToManaged(unmanaged);
        public static BoolStructNative ConvertToUnmanaged(BoolStruct managed) => Marshaller.ConvertToUnmanaged(managed);
        public static void Free(BoolStructNative unmanaged) => Marshaller.Free(unmanaged);
    }

    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementIn, typeof(BoolStructInMarshallerAllowNull))]
    [CustomMarshaller(typeof(BoolStruct), MarshalMode.ElementRef, typeof(BoolStructInMarshallerAllowNull))]
    public static class BoolStructInMarshallerAllowNull
    {
        public static FailingMarshaller<BoolStruct, BoolStructNative> Marshaller = new(
            BoolStructMarshaller.ConvertToUnmanaged,
            BoolStructMarshaller.ConvertToManaged,
            (BoolStructNative unmanaged, int index) => true
        );

        public static BoolStruct ConvertToManaged(BoolStructNative unmanaged) => Marshaller.ConvertToManaged(unmanaged);
        public static BoolStructNative ConvertToUnmanaged(BoolStruct managed) => Marshaller.ConvertToUnmanaged(managed);
        public static void Free(BoolStructNative unmanaged) => Marshaller.Free(unmanaged);
    }

    [CustomMarshaller(typeof(IntStructWrapper), MarshalMode.ElementOut, typeof(FillRangeArrayMarshaller))]
    public static class FillRangeArrayMarshaller
    {
        public static FailingMarshaller<IntStructWrapper, int> Marshaller = new(
            IntStructWrapperMarshaller.ConvertToUnmanaged,
            IntStructWrapperMarshaller.ConvertToManaged,
            (int unmanaged, int i) => unmanaged == i
        );

        public static IntStructWrapper ConvertToManaged(int unmanaged) => Marshaller.ConvertToManaged(unmanaged);
        public static int ConvertToUnmanaged(IntStructWrapper managed) => Marshaller.ConvertToUnmanaged(managed);
        public static void Free(int unmanaged) => Marshaller.Free(unmanaged);
    }

    file static class IntStructWrapperMarshaller
    {
        public static IntStructWrapper ConvertToManaged(int unmanaged) => new IntStructWrapper() { Value = unmanaged };

        public static int ConvertToUnmanaged(IntStructWrapper managed) => managed.Value;
    }

    file static class BoolStructMarshaller
    {
        public static BoolStruct ConvertToManaged(BoolStructNative unmanaged)
            => new BoolStruct()
            {
                b1 = unmanaged.b1 != 0,
                b2 = unmanaged.b2 != 0,
                b3 = unmanaged.b3 != 0
            };

        public static BoolStructNative ConvertToUnmanaged(BoolStruct managed)
            => new BoolStructNative()
            {
                b1 = (byte)(managed.b1 ? 1 : 0),
                b2 = (byte)(managed.b2 ? 1 : 0),
                b3 = (byte)(managed.b3 ? 1 : 0)
            };
    }

    public class FailingMarshaller<T, TUnmanaged> where TUnmanaged : unmanaged, IEquatable<TUnmanaged>
    {
        Func<T, TUnmanaged> _marshal;
        Func<TUnmanaged, T> _unmarshal;
        Func<TUnmanaged, int, bool> _okayToFree;

        int _freeCount = 0;
        int _expectedFreeCount = 0;
        public int ExpectedFreeCount { get => _expectedFreeCount; set => (_expectedFreeCount, _freeCount) = (value, 0); }
        private TUnmanaged[]? _expectedFreedValues;
        public TUnmanaged[]? ExpectedFreedValues { get => _expectedFreedValues; set => (_expectedFreedValues, ExpectedFreeCount, _freeCount) = (value, value?.Length ?? 0, 0); }
        int _marshalledCount = 0;
        int _unmarshalledCount = 0;

        public int MarshallingFailsIndex { get; set; } = -1;
        public int UnmarshallingFailsIndex { get; set; } = -1;


        public FailingMarshaller(Func<T, TUnmanaged> marshal, Func<TUnmanaged, T> unmarshal, Func<TUnmanaged, int, bool> okayToFree)
        {
            _marshal = marshal;
            _unmarshal = unmarshal;
            _okayToFree = okayToFree;
        }
        public void Reset()
        {
            ExpectedFreeCount = 0;
            ExpectedFreedValues = null;
            _marshalledCount = 0;
            _unmarshalledCount = 0;
            _freeCount = 0;
        }

        public void AssertAllHaveBeenCleaned()
        {
            if (ExpectedFreeCount - _freeCount != 0)
                throw new InvalidMarshallingException($"Incorrected number of elements freed. Expected {ExpectedFreeCount - _freeCount} more elements to be freed.");
            Reset();
        }

        public void Free(TUnmanaged unmanaged)
        {
            if (!_okayToFree(unmanaged, _freeCount))
                throw new InvalidMarshallingException("Freed unmanaged value that was not expected to be freed");

            if (_freeCount + 1 > ExpectedFreeCount)
                throw new InvalidMarshallingException($"Freed too many unmanaged values. Expected to free {ExpectedFreeCount} values.");

            if (ExpectedFreedValues?[_freeCount] is { } expected && !unmanaged.Equals(expected))
                throw new InvalidMarshallingException("Freed unmanaged value that was not expected to be freed");

            _freeCount++;
        }

        public T ConvertToManaged(TUnmanaged unmanaged)
        {
            if (_unmarshalledCount == UnmarshallingFailsIndex)
            {
                int tmp = _unmarshalledCount;
                _unmarshalledCount = 0;
                UnmarshallingFailsIndex = -1;
                throw new ArgumentException($"Unmarshalling failed on element number {tmp}");
            }
            _unmarshalledCount++;
            return _unmarshal(unmanaged);
        }

        public TUnmanaged ConvertToUnmanaged(T managed)
        {
            if (_marshalledCount == MarshallingFailsIndex)
            {
                int tmp = _marshalledCount;
                _marshalledCount = 0;
                MarshallingFailsIndex = -1;
                throw new ArgumentException($"Marshalling failed on element number {tmp}");
            }
            _marshalledCount++;
            return _marshal(managed);
        }

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
