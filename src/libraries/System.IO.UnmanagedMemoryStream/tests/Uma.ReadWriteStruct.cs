// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.IO.Tests
{
    public class Uma_ReadWriteStruct : Uma_TestStructs
    {
        [Fact]
        public static void UmaReadWriteStruct_NegativePosition()
        {
            const int capacity = 100;
            UmaTestStruct inStruct = new UmaTestStruct();
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => uma.Write<UmaTestStruct>(-1, ref inStruct));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => uma.Read<UmaTestStruct>(-1, out inStruct));
            }
        }

        [Fact]
        public static void UmaReadWriteStruct_Closed()
        {
            const int capacity = 100;
            UmaTestStruct inStruct = new UmaTestStruct();
            using (var buffer = new TestSafeBuffer(capacity))
            {
                UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite);
                uma.Dispose();
                Assert.Throws<ObjectDisposedException>(() => uma.Write<UmaTestStruct>(0, ref inStruct));
                Assert.Throws<ObjectDisposedException>(() => uma.Read<UmaTestStruct>(0, out inStruct));
            }
        }

        [Fact]
        public static void UmaReadStruct_WriteMode()
        {
            const int capacity = 100;
            UmaTestStruct inStruct = new UmaTestStruct();
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => uma.Read<UmaTestStruct>(0, out inStruct));
            }
        }

        [Fact]
        public static void UmaWriteStruct_ReadMode()
        {
            const int capacity = 100;
            UmaTestStruct inStruct = new UmaTestStruct();
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.Read))
            {
                Assert.Throws<NotSupportedException>(() => uma.Write<UmaTestStruct>(0, ref inStruct));
            }
        }

        [Fact]
        public static void UmaReadWriteStruct_PositionGreaterThanCapacity()
        {
            const int capacity = 100;
            UmaTestStruct inStruct = new UmaTestStruct();
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => uma.Write<UmaTestStruct>(capacity, ref inStruct));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => uma.Read<UmaTestStruct>(capacity, out inStruct));
            }
        }

        [Fact]
        public static void UmaReadWriteStruct_InsufficientSpace()
        {
            const int capacity = 100;
            UmaTestStruct inStruct = new UmaTestStruct();
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                AssertExtensions.Throws<ArgumentException>("position", () => uma.Write<UmaTestStruct>(capacity - UmaTestStruct_UnalignedSize + 1, ref inStruct));
                AssertExtensions.Throws<ArgumentException>("position", () => uma.Write<UmaTestStruct>(capacity - UmaTestStruct_AlignedSize + 1, ref inStruct));
                AssertExtensions.Throws<ArgumentException>("position", () => uma.Read<UmaTestStruct>(capacity - UmaTestStruct_UnalignedSize + 1, out inStruct));
                AssertExtensions.Throws<ArgumentException>("position", () => uma.Read<UmaTestStruct>(capacity - UmaTestStruct_AlignedSize + 1, out inStruct));
            }
        }

        [Fact]
        public static void UmaReadWriteStructWithReferenceType_ThrowsArgumentException()
        {
            const int capacity = 100;
            UmaTestStruct_ContainsReferenceType inStruct = new UmaTestStruct_ContainsReferenceType { referenceType = new object() };
            using (var buffer = new TestSafeBuffer(capacity))
            using (var uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                AssertExtensions.Throws<ArgumentException>(null, "type", () => uma.Write<UmaTestStruct_ContainsReferenceType>(0, ref inStruct));
                AssertExtensions.Throws<ArgumentException>(null, "type", () => uma.Read<UmaTestStruct_ContainsReferenceType>(0, out inStruct));
            }
        }

        [Fact]
        public static void UmaReadWriteGenericIntStruct_Valid()
        {
            const int capacity = 100;
            UmaTestStruct_Generic<int> inStruct = new UmaTestStruct_Generic<int> () { ofT = 158 };
            UmaTestStruct_Generic<int> outStruct;
            using (var buffer = new TestSafeBuffer(capacity))
            using (var uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                uma.Write<UmaTestStruct_Generic<int>>(0, ref inStruct);
                uma.Read<UmaTestStruct_Generic<int>>(0, out outStruct);
                Assert.Equal(inStruct.ofT, outStruct.ofT);
            }
        }

        [Fact]
        public static void UmaReadWriteGenericStringStruct_ThrowsArgumentException()
        {
            const int capacity = 100;
            UmaTestStruct_Generic<string> inStruct = new UmaTestStruct_Generic<string>() { ofT = "Cats!" };
            using (var buffer = new TestSafeBuffer(capacity))
            using (var uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                AssertExtensions.Throws<ArgumentException>(null, "type", () => uma.Write<UmaTestStruct_Generic<string>>(0, ref inStruct));
                AssertExtensions.Throws<ArgumentException>(null, "type", () => uma.Read<UmaTestStruct_Generic<string>>(0, out inStruct));
            }
        }

        [Fact]
        public static void UmaReadWriteStruct_Valid()
        {
            const int capacity = 100;
            UmaTestStruct inStruct = new UmaTestStruct(){ int1 = 1, int2 = 2, bool1 = false, char1 = 'p', bool2 = true};
            UmaTestStruct outStruct;
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                uma.Write<UmaTestStruct>(capacity - UmaTestStruct_AlignedSize, ref inStruct);
                uma.Read<UmaTestStruct>(capacity - UmaTestStruct_AlignedSize, out outStruct);
                Assert.Equal(inStruct.int1, outStruct.int1);
                Assert.Equal(inStruct.int2, outStruct.int2);
                Assert.Equal(inStruct.bool1, outStruct.bool1);
                Assert.Equal(inStruct.char1, outStruct.char1);
                Assert.Equal(inStruct.bool2, outStruct.bool2);
            }
        }
    }
}
