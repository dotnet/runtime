// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class Uma_ReadWriteStruct
    {
        private const int UmaTestStruct_DisalignedSize = 2 * sizeof(int) + 2 * sizeof(bool) + sizeof(char);
        private const int UmaTestStruct_AlignedSize = 16; // potentially architecture dependent.
        private struct UmaTestStruct
        {
            public int int1;
            public bool bool1;
            public int int2;
            public char char1;
            public bool bool2;
        }

        private struct UmaTestStruct_ContainsReferenceType
        {
            public object referenceType;
        }

        [Fact]
        public static void UmaReadWriteStruct_NegativePosition()
        {
            const int capacity = 100;
            UmaTestStruct inStruct = new UmaTestStruct();
            using (TestSafeBuffer buffer = new TestSafeBuffer(capacity))
            using (UnmanagedMemoryAccessor uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                Assert.Throws<ArgumentOutOfRangeException>("position", () => uma.Write<UmaTestStruct>(-1, ref inStruct));
                Assert.Throws<ArgumentOutOfRangeException>("position", () => uma.Read<UmaTestStruct>(-1, out inStruct));
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
                Assert.Throws<ArgumentOutOfRangeException>("position", () => uma.Write<UmaTestStruct>(capacity, ref inStruct));
                Assert.Throws<ArgumentOutOfRangeException>("position", () => uma.Read<UmaTestStruct>(capacity, out inStruct));
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
                Assert.Throws<ArgumentException>(() => uma.Write<UmaTestStruct>(capacity - UmaTestStruct_DisalignedSize + 1, ref inStruct));
                Assert.Throws<ArgumentException>(() => uma.Write<UmaTestStruct>(capacity - UmaTestStruct_AlignedSize + 1, ref inStruct));
                Assert.Throws<ArgumentException>(() => uma.Read<UmaTestStruct>(capacity - UmaTestStruct_DisalignedSize + 1, out inStruct));
                Assert.Throws<ArgumentException>(() => uma.Read<UmaTestStruct>(capacity - UmaTestStruct_AlignedSize + 1, out inStruct));
            }
        }

        [Fact]
        public static void UmaReadStructWithReferenceType_ThrowsArgumentException()
        {
            const int capacity = 100;
            UmaTestStruct_ContainsReferenceType inStruct = new UmaTestStruct_ContainsReferenceType { referenceType = new object() };
            using (var buffer = new TestSafeBuffer(capacity))
            using (var uma = new UnmanagedMemoryAccessor(buffer, 0, capacity, FileAccess.ReadWrite))
            {
                Assert.Throws<ArgumentException>("type", () => uma.Write<UmaTestStruct_ContainsReferenceType>(0, ref inStruct));
                Assert.Throws<ArgumentException>("type", () => uma.Read<UmaTestStruct_ContainsReferenceType>(0, out inStruct));
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
