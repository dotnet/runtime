// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.IO.Tests
{
    public class Uma_ReadWriteStructArray : Uma_TestStructs
    {
        [Fact]
        public static void UmaReadWriteStructArray_InvalidParameters()
        {
            const int capacity = 100;
            UmaTestStruct[] structArr = new UmaTestStruct[1];
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                AssertExtensions.Throws<ArgumentNullException>("array", () => uma.WriteArray<UmaTestStruct>(0, null, 0, 1));
                AssertExtensions.Throws<ArgumentNullException>("array", () => uma.ReadArray<UmaTestStruct>(0, null, 0, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => uma.WriteArray<UmaTestStruct>(0, structArr, -1, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => uma.ReadArray<UmaTestStruct>(0, structArr, -1, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => uma.WriteArray<UmaTestStruct>(0, structArr, 0, -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => uma.ReadArray<UmaTestStruct>(0, structArr, 0, -1));
                AssertExtensions.Throws<ArgumentException>(null, () => uma.WriteArray<UmaTestStruct>(0, structArr, 2, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => uma.ReadArray<UmaTestStruct>(0, structArr, 2, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => uma.WriteArray<UmaTestStruct>(0, structArr, 0, 2));
                AssertExtensions.Throws<ArgumentException>(null, () => uma.ReadArray<UmaTestStruct>(0, structArr, 0, 2));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => uma.WriteArray<UmaTestStruct>(-1, structArr, 0, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => uma.ReadArray<UmaTestStruct>(-1, structArr, 0, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => uma.WriteArray<UmaTestStruct>(capacity, structArr, 0, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => uma.ReadArray<UmaTestStruct>(capacity, structArr, 0, 1));
            }
        }

        [Fact]
        public static void UmaReadWriteStructArray_Closed()
        {
            const int capacity = 100;
            UmaTestStruct[] structArr = new UmaTestStruct[1];
            using (var buffer = new TestSafeBuffer(capacity))
            {
                UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite);
                uma.Dispose();
                Assert.Throws<ObjectDisposedException>(() => uma.WriteArray<UmaTestStruct>(0, structArr, 0, 1));
                Assert.Throws<ObjectDisposedException>(() => uma.ReadArray<UmaTestStruct>(0, structArr, 0, 1));
            }
        }

        [Fact]
        public static void UmaReadStructArray_WriteMode()
        {
            const int capacity = 100;
            UmaTestStruct[] structArr = new UmaTestStruct[1];
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => uma.ReadArray<UmaTestStruct>(0, structArr, 0, 1));
            }
        }

        [Fact]
        public static void UmaWriteStructArray_ReadMode()
        {
            const int capacity = 100;
            UmaTestStruct[] structArr = new UmaTestStruct[1];
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.Read))
            {
                Assert.Throws<NotSupportedException>(() => uma.WriteArray<UmaTestStruct>(0, structArr, 0, 1));
            }
        }

        [Fact]
        public static void UmaReadWriteStructArray_InsufficientSpace()
        {
            const int capacity = 100;
            UmaTestStruct[] structArr = new UmaTestStruct[1];
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => uma.WriteArray<UmaTestStruct>(capacity - UmaTestStruct_UnalignedSize + 1, structArr, 0, 1));
                AssertExtensions.Throws<ArgumentException>(null, () => uma.WriteArray<UmaTestStruct>(capacity - UmaTestStruct_AlignedSize + 1, structArr, 0, 1));
                Assert.Equal(0, uma.ReadArray<UmaTestStruct>(capacity - UmaTestStruct_UnalignedSize + 1, structArr, 0, 1));
                Assert.Equal(0, uma.ReadArray<UmaTestStruct>(capacity - UmaTestStruct_AlignedSize + 1, structArr, 0, 1));
            }
        }

        [Fact]
        public static void UmaReadWriteStructArrayWithReferenceType_ThrowsArgumentException()
        {
            const int capacity = 100;
            UmaTestStruct_ContainsReferenceType[] structArr = new UmaTestStruct_ContainsReferenceType[1] { new UmaTestStruct_ContainsReferenceType() { referenceType = new object() } };
            using (var buffer = new TestSafeBuffer(capacity))
            using (var uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                AssertExtensions.Throws<ArgumentException>(null, "type", () => uma.WriteArray<UmaTestStruct_ContainsReferenceType>(0, structArr, 0, 1));
                AssertExtensions.Throws<ArgumentException>(null, "type", () => uma.ReadArray<UmaTestStruct_ContainsReferenceType>(0, structArr, 0, 1));
            }
        }

        [Fact]
        public static void UmaReadWriteStructArrayGenericIntStruct_Valid()
        {
            const int capacity = 100;
            UmaTestStruct_Generic<int>[] inStructArr = new UmaTestStruct_Generic<int>[1] { new UmaTestStruct_Generic<int>() { ofT = 190 } };
            UmaTestStruct_Generic<int>[] outStructArr = new UmaTestStruct_Generic<int>[1];
            using (var buffer = new TestSafeBuffer(capacity))
            using (var uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                uma.WriteArray<UmaTestStruct_Generic<int>>(0, inStructArr, 0, 1);
                Assert.Equal(1, uma.ReadArray<UmaTestStruct_Generic<int>>(0, outStructArr, 0, 1));
                Assert.Equal(inStructArr[0].ofT, outStructArr[0].ofT);
            }
        }

        [Fact]
        public static void UmaReadWriteGenericStringStructArray_ThrowsArgumentException()
        {
            const int capacity = 100;
            UmaTestStruct_Generic<string>[] structArr = new UmaTestStruct_Generic<string>[1] { new UmaTestStruct_Generic<string>() { ofT = "Cats!" } };
            using (var buffer = new TestSafeBuffer(capacity))
            using (var uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                AssertExtensions.Throws<ArgumentException>(null, "type", () => uma.WriteArray<UmaTestStruct_Generic<string>>(0, structArr, 0, 1));
                AssertExtensions.Throws<ArgumentException>(null, "type", () => uma.ReadArray<UmaTestStruct_Generic<string>>(0, structArr, 0, 1));
            }
        }

        [Fact]
        public static void UmaReadWriteStructArray_OneItem()
        {
            const int capacity = 100;
            UmaTestStruct[] inStructArr = new UmaTestStruct[1] { new UmaTestStruct() { int1 = 1, int2 = 2, bool1 = false, char1 = 'p', bool2 = true } };
            UmaTestStruct[] outStructArr = new UmaTestStruct[1];
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                uma.WriteArray<UmaTestStruct>(capacity - UmaTestStruct_AlignedSize, inStructArr, 0, 1);
                Assert.Equal(1, uma.ReadArray<UmaTestStruct>(capacity - UmaTestStruct_AlignedSize, outStructArr, 0, 1));
                Assert.Equal(inStructArr[0].int1, outStructArr[0].int1);
                Assert.Equal(inStructArr[0].int2, outStructArr[0].int2);
                Assert.Equal(inStructArr[0].bool1, outStructArr[0].bool1);
                Assert.Equal(inStructArr[0].char1, outStructArr[0].char1);
                Assert.Equal(inStructArr[0].bool2, outStructArr[0].bool2);
            }
        }

        [Fact]
        public static void UmaReadWriteStructArray_Multiples()
        {
            const int numberOfStructs = 12;
            const int capacity = numberOfStructs * UmaTestStruct_AlignedSize;
            UmaTestStruct[] inStructArr = new UmaTestStruct[numberOfStructs];
            UmaTestStruct[] outStructArr = new UmaTestStruct[numberOfStructs];
            for (int i = 0; i < numberOfStructs; i++)
            {
                inStructArr[i] = new UmaTestStruct() { bool1 = false, bool2 = true, int1 = i, int2 = i + 1, char1 = (char)i };
            }
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                uma.WriteArray<UmaTestStruct>(0, inStructArr, 0, numberOfStructs);
                Assert.Equal(numberOfStructs, uma.ReadArray<UmaTestStruct>(0, outStructArr, 0, numberOfStructs));
                for (int i = 0; i < numberOfStructs; i++)
                {
                    Assert.Equal(i, outStructArr[i].int1);
                    Assert.Equal(i+1, outStructArr[i].int2);
                    Assert.False(outStructArr[i].bool1);
                    Assert.Equal((char)i, outStructArr[i].char1);
                    Assert.True(outStructArr[i].bool2);
                }
            }
        }

        [Fact]
        public static void UmaReadWriteStructArray_TryToReadMoreThanAvailable()
        {
            const int capacity = 100;
            UmaTestStruct[] inStructArr = new UmaTestStruct[1] { new UmaTestStruct() { int1 = 1, int2 = 2, bool1 = false, char1 = 'p', bool2 = true } };
            UmaTestStruct[] outStructArr = new UmaTestStruct[5000];
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                uma.WriteArray<UmaTestStruct>(0, inStructArr, 0, 1);
                int readCount = uma.ReadArray<UmaTestStruct>(0, outStructArr, 0, 5000);
                Assert.Equal(capacity / UmaTestStruct_AlignedSize, readCount);
            }
        }
    }
}
