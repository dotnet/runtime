using System;
using System.Collections.Generic;

class ReferenceType
{
    internal byte Value;
    public ReferenceType(byte value) { Value = value; }
}

struct ValueTypeWithoutPointers
{
    internal byte Value;
    public ValueTypeWithoutPointers(byte value) { Value = value; }
}

struct ValueTypeWithPointers
{
    internal object Reference;
    public ValueTypeWithPointers(object reference) { Reference = reference; }
}

struct SevenBytesStruct
{
#pragma warning disable 0169
    byte b1, b2, b3, b4, b5, b6, b7;
#pragma warning restore 0169
}

class My
{
    static int Sum(Span<int> span)
    {
        int sum = 0;
        for (int i = 0; i < span.Length; i++)
            sum += span[i];
        return sum;
    }

    static void Main()
    {
        int failedTestsCount = 0;

        Test(CanAccessItemsViaIndexer, "CanAccessItemsViaIndexer", ref failedTestsCount);
        Test(CanAccessItemsViaIndexerStartCtor, "CanAccessItemsViaIndexerStartCtor", ref failedTestsCount);
        Test(CanAccessItemsViaIndexerStartLengthCtor, "CanAccessItemsViaIndexerStartLengthCtor", ref failedTestsCount);

        Test(TestBoundaryEmptySpanStartCtor, "TestBoundaryEmptySpanStartCtor", ref failedTestsCount);
        Test(TestBoundaryEmptySpanStartLengthCtor, "TestBoundaryEmptySpanStartLengthCtor", ref failedTestsCount);

        Test(ReferenceTypesAreSupported, "ReferenceTypesAreSupported", ref failedTestsCount);

        Test(CanUpdateUnderlyingArray, "CanUpdateUnderlyingArray", ref failedTestsCount);

        Test(MustNotMoveGcTypesToUnmanagedMemory, "MustNotMoveGcTypesToUnmanagedMemory", ref failedTestsCount);

        Test(TestArrayCoVariance, "TestArrayCoVariance", ref failedTestsCount);
        Test(TestArrayCoVarianceStartCtor, "TestArrayCoVarianceStartCtor", ref failedTestsCount);
        Test(TestArrayCoVarianceStartLengthCtor, "TestArrayCoVarianceStartLengthCtor", ref failedTestsCount);

        Test(TestArrayCoVarianceReadOnly, "TestArrayCoVarianceReadOnly", ref failedTestsCount);

        Test(CanCopyValueTypesWithoutPointersToSlice, "CanCopyValueTypesWithoutPointersToSlice", ref failedTestsCount);
        Test(CanCopyValueTypesWithoutPointersToArray, "CanCopyValueTypesWithoutPointersToArray", ref failedTestsCount);

        Test(CanCopyReferenceTypesToSlice, "CanCopyReferenceTypesToSlice", ref failedTestsCount);
        Test(CanCopyReferenceTypesToArray, "CanCopyReferenceTypesToArray", ref failedTestsCount);

        Test(CanCopyValueTypesWithPointersToSlice, "CanCopyValueTypesWithPointersToSlice", ref failedTestsCount);
        Test(CanCopyValueTypesWithPointersToArray, "CanCopyValueTypesWithPointersToArray", ref failedTestsCount);

        Test(CanCopyValueTypesWithoutPointersToUnmanagedMemory, "CanCopyValueTypesWithoutPointersToUnmanagedMemory", ref failedTestsCount);

        Test(CanCopyOverlappingSlicesOfValueTypeWithoutPointers, "CanCopyOverlappingSlicesOfValueTypeWithoutPointers", ref failedTestsCount);
        Test(CanCopyOverlappingSlicesOfValueTypeWithPointers, "CanCopyOverlappingSlicesOfValueTypeWithPointers", ref failedTestsCount);
        Test(CanCopyOverlappingSlicesOfReferenceTypes, "CanCopyOverlappingSlicesOfReferenceTypes", ref failedTestsCount);

        Test(MustNotCastSpanOfValueTypesWithPointers, "MustNotCastSpanOfValueTypesWithPointers", ref failedTestsCount);
        Test(IntArraySpanCastedToByteArraySpanHasSameBytesAsOriginalArray, "IntArraySpanCastedToByteArraySpanHasSameBytesAsOriginalArray", ref failedTestsCount);
        Test(ByteArraySpanCastedToIntArraySpanHasSameBytesAsOriginalArray, "ByteArraySpanCastedToIntArraySpanHasSameBytesAsOriginalArray", ref failedTestsCount);
        Test(SourceTypeLargerThanTargetOneCorrectlyCalcsTargetsLength, "SourceTypeLargerThanTargetOneCorrectlyCalcsTargetsLength", ref failedTestsCount);
        Test(WhenSourceDoesntFitIntoTargetLengthIsZero, "WhenSourceDoesntFitIntoTargetLengthIsZero", ref failedTestsCount);
        Test(WhenSourceFitsIntoTargetOnceLengthIsOne, "WhenSourceFitsIntoTargetOnceLengthIsOne", ref failedTestsCount);
        Test(WhenSourceTypeLargerThanTargetAndOverflowsInt32ThrowsException, "WhenSourceTypeLargerThanTargetAndOverflowsInt32ThrowsException", ref failedTestsCount);
        Test(CanCreateSpanFromString, "CanCreateSpanFromString", ref failedTestsCount);

        Test(WhenStartLargerThanLengthThrowsExceptionStartCtor, "WhenStartLargerThanLengthThrowsExceptionStartCtor", ref failedTestsCount);
        Test(WhenStartLargerThanLengthThrowsExceptionStartLengthCtor, "WhenStartLargerThanLengthThrowsExceptionStartLengthCtor", ref failedTestsCount);
        Test(WhenStartAndLengthLargerThanLengthThrowsExceptionStartLengthCtor, "WhenStartAndLengthLargerThanLengthThrowsExceptionStartLengthCtor", ref failedTestsCount);

        Console.WriteLine(string.Format("{0} tests has failed", failedTestsCount));
        Environment.Exit(failedTestsCount);
    }

    static void CanAccessItemsViaIndexer()
    {
        int[] a = new int[] { 1, 2, 3 };
        Span<int> slice = new Span<int>(a);
        AssertTrue(Sum(slice) == 6, "Failed to sum slice");
        
        Span<int> subslice = slice.Slice(1, 2);
        AssertTrue(Sum(subslice) == 5, "Failed to sum subslice");
    }

    static void CanAccessItemsViaIndexerStartCtor()
    {
        int[] a = new int[] { 1, 2, 3 };
        Span<int> slice = new Span<int>(a, start: 1);
        AssertTrue(Sum(slice) == 5, "Failed to sum slice");
    }

    static void CanAccessItemsViaIndexerStartLengthCtor()
    {
        int[] a = new int[] { 1, 2, 3 };
        Span<int> slice = new Span<int>(a, start: 1, length: 1);
        AssertTrue(Sum(slice) == 2, "Failed to sum slice");
    }

    static void TestBoundaryEmptySpanStartCtor()
    {
        int[] a = new int[5];

        Span<int> slice = new Span<int>(a, start: a.Length);
        AssertEqual(slice.Length, 0);
    }

    static void TestBoundaryEmptySpanStartLengthCtor()
    {
        int[] a = new int[5];

        Span<int> slice = new Span<int>(a, a.Length, 0);
        AssertEqual(slice.Length, 0);

        Span<int> subSlice = new Span<int>(a).Slice(a.Length, 0);
        AssertEqual(subSlice.Length, 0);
    }

    static void ReferenceTypesAreSupported()
    {
        var underlyingArray = new ReferenceType[] { new ReferenceType(0), new ReferenceType(1), new ReferenceType(2) };
        var slice = new Span<ReferenceType>(underlyingArray);

        for (int i = 0; i < underlyingArray.Length; i++)
        {
            AssertTrue(underlyingArray[i].Value == slice[i].Value, "Values are different");
            AssertTrue(object.ReferenceEquals(underlyingArray[i], slice[i]), "References are broken");
        }
    }

    static unsafe void MustNotMoveGcTypesToUnmanagedMemory()
    {
        byte* pointerToStack = stackalloc byte[256];

        try
        {
            new Span<ValueTypeWithPointers>(pointerToStack, 1);
            AssertTrue(false, "Expected exception for value types with references not thrown");
        }
        catch (System.ArgumentException ex)
        {
            AssertTrue(ex.Message == "Cannot use type 'ValueTypeWithPointers'. Only value types without pointers or references are supported.",
                "Exception message is incorrect");
        }

        try
        {
            new Span<ReferenceType>(pointerToStack, 1);
            AssertTrue(false, "Expected exception for reference types not thrown");
        }
        catch (System.ArgumentException ex)
        {
            AssertTrue(ex.Message == "Cannot use type 'ReferenceType'. Only value types without pointers or references are supported.",
                "Exception message is incorrect");
        }
    }

    static void TestArrayCoVariance()
    {
        var array = new ReferenceType[1];
        var objArray = (object[])array;
        try
        {
            new Span<object>(objArray);
            AssertTrue(false, "Expected exception not thrown");
        }
        catch (ArrayTypeMismatchException)
        {
        }

        var objEmptyArray = Array.Empty<ReferenceType>();
        try
        {
            new Span<object>(objEmptyArray);
            AssertTrue(false, "Expected exception not thrown");
        }
        catch (ArrayTypeMismatchException)
        {
        }
    }

    static void TestArrayCoVarianceStartCtor()
    {
        var array = new ReferenceType[1];
        var objArray = (object[])array;
        try
        {
            new Span<object>(objArray, start: 0);
            AssertTrue(false, "Expected exception not thrown");
        }
        catch (ArrayTypeMismatchException)
        {
        }

        var objEmptyArray = Array.Empty<ReferenceType>();
        try
        {
            new Span<object>(objEmptyArray, start: 0);
            AssertTrue(false, "Expected exception not thrown");
        }
        catch (ArrayTypeMismatchException)
        {
        }
    }

    static void TestArrayCoVarianceStartLengthCtor()
    {
        var array = new ReferenceType[1];
        var objArray = (object[])array;
        try
        {
            new Span<object>(objArray, start: 0, length: 1);
            AssertTrue(false, "Expected exception not thrown");
        }
        catch (ArrayTypeMismatchException)
        {
        }

        var objEmptyArray = Array.Empty<ReferenceType>();
        try
        {
            new Span<object>(objEmptyArray, start: 0, length: 1);
            AssertTrue(false, "Expected exception not thrown");
        }
        catch (ArrayTypeMismatchException)
        {
        }
    }

    static void TestArrayCoVarianceReadOnly()
    {
        var array = new ReferenceType[1];
        var objArray = (object[])array;
        AssertTrue(new ReadOnlySpan<object>(objArray).Length == 1, "Unexpected length");

        var objEmptyArray = Array.Empty<ReferenceType>();
        AssertTrue(new ReadOnlySpan<object>(objEmptyArray).Length == 0, "Unexpected length");
   }

    static void CanUpdateUnderlyingArray()
    {
        var underlyingArray = new int[] { 1, 2, 3 };
        var slice = new Span<int>(underlyingArray);

        slice[0] = 0;
        slice[1] = 1;
        slice[2] = 2;

        AssertTrue(underlyingArray[0] == 0, "Failed to update underlying array");
        AssertTrue(underlyingArray[1] == 1, "Failed to update underlying array");
        AssertTrue(underlyingArray[2] == 2, "Failed to update underlying array");
    }

    static void CanCopyValueTypesWithoutPointersToSlice()
    {
        var source = new Span<ValueTypeWithoutPointers>(
            new[]
            {
                new ValueTypeWithoutPointers(0),
                new ValueTypeWithoutPointers(1),
                new ValueTypeWithoutPointers(2),
                new ValueTypeWithoutPointers(3)
            });
        var underlyingArray = new ValueTypeWithoutPointers[4];
        var slice = new Span<ValueTypeWithoutPointers>(underlyingArray);

        var result = source.TryCopyTo(slice);

        AssertTrue(result, "Failed to copy value types without pointers");
        for (int i = 0; i < 4; i++)
        {
            AssertTrue(source[i].Value == slice[i].Value, "Failed to copy value types without pointers, values were not equal");
            AssertTrue(source[i].Value == underlyingArray[i].Value, "Failed to copy value types without pointers to underlying array, values were not equal");
        }
    }

    static void CanCopyValueTypesWithoutPointersToArray()
    {
        var source = new Span<ValueTypeWithoutPointers>(
            new[]
            {
                new ValueTypeWithoutPointers(0),
                new ValueTypeWithoutPointers(1),
                new ValueTypeWithoutPointers(2),
                new ValueTypeWithoutPointers(3)
            });
        var array = new ValueTypeWithoutPointers[4];

        var result = source.TryCopyTo(array);

        AssertTrue(result, "Failed to copy value types without pointers");
        for (int i = 0; i < 4; i++)
        {
            AssertTrue(source[i].Value == array[i].Value, "Failed to copy value types without pointers, values were not equal");
        }
    }

    static void CanCopyReferenceTypesToSlice()
    {
        var source = new Span<ReferenceType>(
            new[]
            {
                    new ReferenceType(0),
                    new ReferenceType(1),
                    new ReferenceType(2),
                    new ReferenceType(3)
            });
        var underlyingArray = new ReferenceType[4];
        var slice = new Span<ReferenceType>(underlyingArray);

        var result = source.TryCopyTo(slice);

        AssertTrue(result, "Failed to copy reference types");
        for (int i = 0; i < 4; i++)
        {
            AssertTrue(source[i] != null && slice[i] != null, "Failed to copy reference types, references were null");
            AssertTrue(object.ReferenceEquals(source[i], slice[i]), "Failed to copy reference types, references were not equal");
            AssertTrue(source[i].Value == slice[i].Value, "Failed to copy reference types, values were not equal");

            AssertTrue(underlyingArray[i] != null, "Failed to copy reference types to underlying array, references were null");
            AssertTrue(object.ReferenceEquals(source[i], underlyingArray[i]), "Failed to copy reference types to underlying array, references were not equal");
            AssertTrue(source[i].Value == underlyingArray[i].Value, "Failed to copy reference types to underlying array, values were not equal");
        }
    }

    static void CanCopyReferenceTypesToArray()
    {
        var source = new Span<ReferenceType>(
            new[]
            {
                    new ReferenceType(0),
                    new ReferenceType(1),
                    new ReferenceType(2),
                    new ReferenceType(3)
            });
        var array = new ReferenceType[4];

        var result = source.TryCopyTo(array);

        AssertTrue(result, "Failed to copy reference types");
        for (int i = 0; i < 4; i++)
        {
            AssertTrue(source[i] != null && array[i] != null, "Failed to copy reference types, references were null");
            AssertTrue(object.ReferenceEquals(source[i], array[i]), "Failed to copy reference types, references were not equal");
            AssertTrue(source[i].Value == array[i].Value, "Failed to copy reference types, values were not equal");
        }
    }

    static void CanCopyValueTypesWithPointersToSlice()
    {
        var source = new Span<ValueTypeWithPointers>(
            new[]
            {
                    new ValueTypeWithPointers(new object()),
                    new ValueTypeWithPointers(new object()),
                    new ValueTypeWithPointers(new object()),
                    new ValueTypeWithPointers(new object())
            });
        var underlyingArray = new ValueTypeWithPointers[4];
        var slice = new Span<ValueTypeWithPointers>(underlyingArray);

        var result = source.TryCopyTo(slice);

        AssertTrue(result, "Failed to copy value types with pointers");
        for (int i = 0; i < 4; i++)
        {
            AssertTrue(object.ReferenceEquals(source[i].Reference, slice[i].Reference), "Failed to copy value types with pointers, references were not the same");
            AssertTrue(object.ReferenceEquals(source[i].Reference, underlyingArray[i].Reference), "Failed to copy value types with pointers to underlying array, references were not the same");
        }
    }

    static void CanCopyValueTypesWithPointersToArray()
    {
        var source = new Span<ValueTypeWithPointers>(
            new[]
            {
                    new ValueTypeWithPointers(new object()),
                    new ValueTypeWithPointers(new object()),
                    new ValueTypeWithPointers(new object()),
                    new ValueTypeWithPointers(new object())
            });
        var array = new ValueTypeWithPointers[4];

        var result = source.TryCopyTo(array);

        AssertTrue(result, "Failed to copy value types with pointers");
        for (int i = 0; i < 4; i++)
        {
            AssertTrue(object.ReferenceEquals(source[i].Reference, array[i].Reference), "Failed to copy value types with pointers, references were not the same");
        }
    }

    static unsafe void CanCopyValueTypesWithoutPointersToUnmanagedMemory()
    {
        var source = new Span<byte>(
            new byte[]
            {
                    0,
                    1,
                    2,
                    3
            });
        byte* pointerToStack = stackalloc byte[256];

        var result = source.TryCopyTo(new Span<byte>(pointerToStack, 4));

        AssertTrue(result, "Failed to copy value types without pointers to unamanaged memory");
        for (int i = 0; i < 4; i++)
        {
            AssertTrue(source[i] == pointerToStack[i], "Failed to copy value types without pointers to unamanaged memory, values were not equal");
        }
    }

    static void CanCopyOverlappingSlicesOfValueTypeWithoutPointers()
    {
        var sourceArray = new[]
            {
                new ValueTypeWithoutPointers(0),
                new ValueTypeWithoutPointers(1),
                new ValueTypeWithoutPointers(2)
            };
        var firstAndSecondElements = new Span<ValueTypeWithoutPointers>(sourceArray, 0, 2); // 0, 1
        var secondAndThirdElements = new Span<ValueTypeWithoutPointers>(sourceArray, 1, 2); // 1, 2

        // 0 1 2 sourceArray
        // 0 1 - firstAndSecondElements
        // - 1 2 secondAndThirdElements
        var result = firstAndSecondElements.TryCopyTo(secondAndThirdElements); // to avoid overlap we should copy backward now
        // - 0 1 secondAndThirdElements
        // 0 0 - firstAndSecondElements     
        // 0 0 1 sourceArray

        AssertTrue(result, "Failed to copy overlapping value types without pointers");

        AssertTrue(secondAndThirdElements[1].Value == 1, "secondAndThirdElements[1] should get replaced by 1");
        AssertTrue(secondAndThirdElements[0].Value == 0 && firstAndSecondElements[1].Value == 0, "secondAndThirdElements[0] and firstAndSecondElements[1] point to the same element, should get replaced by 0");
        AssertTrue(firstAndSecondElements[0].Value == 0, "firstAndSecondElements[0] should remain the same");

        // let's try the other direction to make sure it works as well!

        sourceArray = new[]
            {
                new ValueTypeWithoutPointers(0),
                new ValueTypeWithoutPointers(1),
                new ValueTypeWithoutPointers(2)
            };
        firstAndSecondElements = new Span<ValueTypeWithoutPointers>(sourceArray, 0, 2); // 0, 1
        secondAndThirdElements = new Span<ValueTypeWithoutPointers>(sourceArray, 1, 2); // 1, 2

        // 0 1 2 sourceArray
        // 0 1 - firstAndSecondElements
        // - 1 2 secondAndThirdElements
        result = secondAndThirdElements.TryCopyTo(firstAndSecondElements); // to avoid overlap we should copy forward now
        // 1 2 - firstAndSecondElements
        // - 2 2 secondAndThirdElements
        // 1 2 2 sourceArray

        AssertTrue(result, "Failed to copy overlapping value types without pointers");

        AssertTrue(secondAndThirdElements[1].Value == 2, "secondAndThirdElements[1] should remain the same");
        AssertTrue(firstAndSecondElements[1].Value == 2 && secondAndThirdElements[0].Value == 2, "secondAndThirdElements[0] && firstAndSecondElements[1] point to the same element, should get replaced by 2");
        AssertTrue(firstAndSecondElements[0].Value == 1, "firstAndSecondElements[0] should get replaced by 1");
    }

    static void CanCopyOverlappingSlicesOfValueTypeWithPointers()
    {
        string zero = "0", one = "1", two = "2";
        var sourceArray = new[]
            {
                new ValueTypeWithPointers(zero),
                new ValueTypeWithPointers(one),
                new ValueTypeWithPointers(two)
            };
        var firstAndSecondElements = new Span<ValueTypeWithPointers>(sourceArray, 0, 2); // 0, 1
        var secondAndThirdElements = new Span<ValueTypeWithPointers>(sourceArray, 1, 2); // 1, 2

        // 0 1 2 sourceArray
        // 0 1 - firstAndSecondElements
        // - 1 2 secondAndThirdElements
        var result = firstAndSecondElements.TryCopyTo(secondAndThirdElements); // to avoid overlap we should copy backward now
        // - 0 1 secondAndThirdElements
        // 0 0 - firstAndSecondElements
        // 0 0 1 sourceArray

        AssertTrue(result, "Failed to copy overlapping value types with pointers");

        AssertTrue(object.ReferenceEquals(secondAndThirdElements[1].Reference, one), "secondAndThirdElements[1] should get replaced by 1");
        AssertTrue(object.ReferenceEquals(secondAndThirdElements[0].Reference, zero) && object.ReferenceEquals(firstAndSecondElements[1].Reference, zero), "secondAndThirdElements[0] and firstAndSecondElements[1] point to the same element, should get replaced by 0");
        AssertTrue(object.ReferenceEquals(firstAndSecondElements[0].Reference, zero), "firstAndSecondElements[0] should remain the same");

        // let's try the other direction to make sure it works as well!

        sourceArray = new[]
            {
                new ValueTypeWithPointers(zero),
                new ValueTypeWithPointers(one),
                new ValueTypeWithPointers(two)
            };
        firstAndSecondElements = new Span<ValueTypeWithPointers>(sourceArray, 0, 2); // 0, 1
        secondAndThirdElements = new Span<ValueTypeWithPointers>(sourceArray, 1, 2); // 1, 2

        // 0 1 2 sourceArray
        // 0 1 - firstAndSecondElements
        // - 1 2 secondAndThirdElements
        result = secondAndThirdElements.TryCopyTo(firstAndSecondElements); // to avoid overlap we should copy forward now
        // 1 2 - firstAndSecondElements
        // - 2 2 secondAndThirdElements
        // 1 2 2 sourceArray

        AssertTrue(result, "Failed to copy overlapping value types with pointers");

        AssertTrue(object.ReferenceEquals(secondAndThirdElements[1].Reference, two), "secondAndThirdElements[1] should remain the same");
        AssertTrue(object.ReferenceEquals(firstAndSecondElements[1].Reference, two) && object.ReferenceEquals(secondAndThirdElements[0].Reference, two), "secondAndThirdElements[0] && firstAndSecondElements[1] point to the same element, should get replaced by 2");
        AssertTrue(object.ReferenceEquals(firstAndSecondElements[0].Reference, one), "firstAndSecondElements[0] should get replaced by 1");
    }

    static void CanCopyOverlappingSlicesOfReferenceTypes()
    {
        var sourceArray = new ReferenceType[] { new ReferenceType(0), new ReferenceType(1), new ReferenceType(2) };

        var firstAndSecondElements = new Span<ReferenceType>(sourceArray, 0, 2); // 0, 1
        var secondAndThirdElements = new Span<ReferenceType>(sourceArray, 1, 2); // 1, 2

        // 0 1 2 sourceArray
        // 0 1 - firstAndSecondElements
        // - 1 2 secondAndThirdElements
        var result = firstAndSecondElements.TryCopyTo(secondAndThirdElements); // to avoid overlap we should copy backward now
        // - 0 1 secondAndThirdElements
        // 0 0 - firstAndSecondElements
        // 0 0 1 sourceArray

        AssertTrue(result, "Failed to copy overlapping reference types");

        AssertTrue(secondAndThirdElements[1].Value == 1, "secondAndThirdElements[1] should get replaced by 1");
        AssertTrue(secondAndThirdElements[0].Value == 0 && firstAndSecondElements[1].Value == 0, "secondAndThirdElements[0] and firstAndSecondElements[1] point to the same element, should get replaced by 0");
        AssertTrue(firstAndSecondElements[0].Value == 0, "firstAndSecondElements[0] should remain the same");

        // let's try the other direction to make sure it works as well!

        sourceArray = new[]
            {
                new ReferenceType(0),
                new ReferenceType(1),
                new ReferenceType(2)
            };
        firstAndSecondElements = new Span<ReferenceType>(sourceArray, 0, 2); // 0, 1
        secondAndThirdElements = new Span<ReferenceType>(sourceArray, 1, 2); // 1, 2

        // 0 1 2 sourceArray
        // 0 1 - firstAndSecondElements
        // - 1 2 secondAndThirdElements
        result = secondAndThirdElements.TryCopyTo(firstAndSecondElements); // to avoid overlap we should copy forward now
        // 1 2 - firstAndSecondElements
        // - 2 2 secondAndThirdElements
        // 1 2 2 sourceArray

        AssertTrue(result, "Failed to copy overlapping reference types");

        AssertTrue(secondAndThirdElements[1].Value == 2, "secondAndThirdElements[1] should remain the same");
        AssertTrue(firstAndSecondElements[1].Value == 2 && secondAndThirdElements[0].Value == 2, "secondAndThirdElements[0] && firstAndSecondElements[1] point to the same element, should get replaced by 2");
        AssertTrue(firstAndSecondElements[0].Value == 1, "firstAndSecondElements[0] should get replaced by 1");
    }

    static void MustNotCastSpanOfValueTypesWithPointers()
    {
        var spanOfValueTypeWithPointers = new Span<ValueTypeWithPointers>(new[] { new ValueTypeWithPointers(new object()) });

        try
        {
            var impossible = spanOfValueTypeWithPointers.AsBytes();
            AssertTrue(false, "Expected exception for wrong type not thrown");
        }
        catch (System.ArgumentException ex)
        {
            AssertTrue(ex.Message == "Cannot use type 'ValueTypeWithPointers'. Only value types without pointers or references are supported.",
                "Exception message is incorrect");
        }

        try
        {
            var impossible = spanOfValueTypeWithPointers.NonPortableCast<ValueTypeWithPointers, byte>();
            AssertTrue(false, "Expected exception for wrong type not thrown");
        }
        catch (System.ArgumentException ex)
        {
            AssertTrue(ex.Message == "Cannot use type 'ValueTypeWithPointers'. Only value types without pointers or references are supported.",
                "Exception message is incorrect");
        }

        var spanOfBytes = new Span<byte>(new byte[10]);
        try
        {
            var impossible = spanOfBytes.NonPortableCast<byte, ValueTypeWithPointers>();
            AssertTrue(false, "Expected exception for wrong type not thrown");
        }
        catch (System.ArgumentException ex)
        {
            AssertTrue(ex.Message == "Cannot use type 'ValueTypeWithPointers'. Only value types without pointers or references are supported.",
                "Exception message is incorrect");
        }
    }

    static void IntArraySpanCastedToByteArraySpanHasSameBytesAsOriginalArray()
    {
        var ints = new int[100000];
        Random r = new Random(42324232);
        for (int i = 0; i < ints.Length; i++) { ints[i] = r.Next(); }
        var bytes = new Span<int>(ints).AsBytes();
        AssertEqual(bytes.Length, ints.Length * sizeof(int));
        for (int i = 0; i < ints.Length; i++)
        {
            AssertEqual(bytes[i * 4], (ints[i] & 0xff));
            AssertEqual(bytes[i * 4 + 1], (ints[i] >> 8 & 0xff));
            AssertEqual(bytes[i * 4 + 2], (ints[i] >> 16 & 0xff));
            AssertEqual(bytes[i * 4 + 3], (ints[i] >> 24 & 0xff));
        }
    }

    static void ByteArraySpanCastedToIntArraySpanHasSameBytesAsOriginalArray()
    {
        var bytes = new byte[100000];
        Random r = new Random(541345);
        for (int i = 0; i < bytes.Length; i++) { bytes[i] = (byte)r.Next(256); }
        var ints = new Span<byte>(bytes).NonPortableCast<byte, int>();
        AssertEqual(ints.Length, bytes.Length / sizeof(int));
        for (int i = 0; i < ints.Length; i++)
        {
            AssertEqual(BitConverter.ToInt32(bytes, i * 4), ints[i]);
        }
    }

    static void SourceTypeLargerThanTargetOneCorrectlyCalcsTargetsLength()
    {
        for (int sourceLength = 0; sourceLength <= 4; sourceLength++)
        {
            var sourceSlice = new Span<SevenBytesStruct>(new SevenBytesStruct[sourceLength]);

            var targetSlice = sourceSlice.NonPortableCast<SevenBytesStruct, short>();

            AssertEqual((sourceLength * 7) / sizeof(short), targetSlice.Length);
        }
    }

    static void WhenSourceDoesntFitIntoTargetLengthIsZero()
    {
        for (int sourceLength = 0; sourceLength <= 3; sourceLength++)
        {
            var sourceSlice = new Span<short>(new short[sourceLength]);

            var targetSlice = sourceSlice.NonPortableCast<short, SevenBytesStruct>();

            AssertEqual(0, targetSlice.Length);
        }
    }

    static void WhenSourceFitsIntoTargetOnceLengthIsOne()
    {
        foreach (var sourceLength in new int[] { 4, 6 })
        {
            var sourceSlice = new Span<short>(new short[sourceLength]);

            var targetSlice = sourceSlice.NonPortableCast<short, SevenBytesStruct>();

            AssertEqual(1, targetSlice.Length);
        }
    }

    static void WhenSourceTypeLargerThanTargetAndOverflowsInt32ThrowsException()
    {
        unsafe
        {
            byte dummy;
            int sourceLength = 620000000;
            var sourceSlice = new Span<SevenBytesStruct>(&dummy, sourceLength);

            try
            {
                var targetSlice = sourceSlice.NonPortableCast<SevenBytesStruct, short>();
                AssertTrue(false, "Expected exception for overflow not thrown");
            }
            catch (System.OverflowException)
            {
            }
        }
    }

    static void CanCreateSpanFromString()
    {
        const string fullText = "new Span<byte>()";
        var spanFromFull = fullText.Slice();
        AssertEqualContent(fullText, spanFromFull);

        string firstHalfOfString = fullText.Substring(0, fullText.Length / 2);
        var spanFromFirstHalf = fullText.Slice(0, fullText.Length / 2);
        AssertEqualContent(firstHalfOfString, spanFromFirstHalf);

        string secondHalfOfString = fullText.Substring(fullText.Length / 2);
        var spanFromSecondHalf = fullText.Slice(fullText.Length / 2);
        AssertEqualContent(secondHalfOfString, spanFromSecondHalf);
    }

    static void WhenStartLargerThanLengthThrowsExceptionStartCtor()
    {
        try
        {
            var data = new byte[10];
            var slice = new Span<byte>(data, start: 11);
            AssertTrue(false, "Expected exception for Argument Out of Range not thrown");
        }
        catch (System.ArgumentOutOfRangeException)
        {
        }
    }
    
    static void WhenStartLargerThanLengthThrowsExceptionStartLengthCtor()
    {
        try
        {
            var data = new byte[10];
            var slice = new Span<byte>(data, start: 11, length: 0);
            AssertTrue(false, "Expected exception for Argument Out of Range not thrown");
        }
        catch (System.ArgumentOutOfRangeException)
        {
        }
    }

    static void WhenStartAndLengthLargerThanLengthThrowsExceptionStartLengthCtor()
    {
        try
        {
            var data = new byte[10];
            var slice = new Span<byte>(data, start: 1, length: 10);
            AssertTrue(false, "Expected exception for Argument Out of Range not thrown");
        }
        catch (System.ArgumentOutOfRangeException)
        {
        }
    }

    static void Test(Action test, string testName, ref int failedTestsCount)
    {
        try
        {
            test();

            Console.WriteLine(testName + " test has passed");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(testName + " test has failed with exception: " + ex.Message);

            ++failedTestsCount;
        }
        finally
        {
            Console.WriteLine("-------------------");
        }
    }

    static void AssertTrue(bool condition, string errorMessage)
    {
        if (condition == false)
        {
            throw new Exception(errorMessage);
        }
    }

    static void AssertEqual<T>(T left, T right)
        where T : IEquatable<T>
    {
        if (left.Equals(right) == false)
        {
            throw new Exception(string.Format("Values were not equal! {0} and {1}", left, right));
        }
    }

    static void AssertEqualContent(string text, ReadOnlySpan<char> span)
    {
        AssertEqual(text.Length, span.Length);
        for (int i = 0; i < text.Length; i++)
        {
            AssertEqual(text[i], span[i]);
        }
    }
}
