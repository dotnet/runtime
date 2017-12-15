// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryMarshalTests
    {
        public static IEnumerable<object[]> ReadOnlyMemoryInt32Instances()
        {
            yield return new object[] { ReadOnlyMemory<int>.Empty };
            yield return new object[] { new ReadOnlyMemory<int>(new int[0]) };
            yield return new object[] { new ReadOnlyMemory<int>(new int[10]) };
            yield return new object[] { new ReadOnlyMemory<int>(new int[10], 1, 3) };
            yield return new object[] { (ReadOnlyMemory<int>)new CustomMemoryForTest<int>(new int[10]).Memory };
        }

        public static IEnumerable<object[]> ReadOnlyMemoryObjectInstances()
        {
            yield return new object[] { ReadOnlyMemory<object>.Empty };
            yield return new object[] { new ReadOnlyMemory<object>(new object[0]) };
            yield return new object[] { new ReadOnlyMemory<object>(new object[10]) };
            yield return new object[] { new ReadOnlyMemory<object>(new object[10], 1, 3) };
            yield return new object[] { (ReadOnlyMemory<object>)new CustomMemoryForTest<object>(new object[10]).Memory };
        }

        public static IEnumerable<object[]> ReadOnlyMemoryCharInstances()
        {
            yield return new object[] { ReadOnlyMemory<char>.Empty };
            yield return new object[] { new ReadOnlyMemory<char>(new char[10], 1, 3) };
            yield return new object[] { (ReadOnlyMemory<char>)new CustomMemoryForTest<char>(new char[10]).Memory };
            yield return new object[] { "12345".AsReadOnlyMemory() };
        }

        [Theory]
        [MemberData(nameof(ReadOnlyMemoryInt32Instances))]
        public static void AsMemory_Roundtrips(ReadOnlyMemory<int> readOnlyMemory) => AsMemory_Roundtrips_Core(readOnlyMemory);

        [Theory]
        [MemberData(nameof(ReadOnlyMemoryObjectInstances))]
        public static void AsMemory_Roundtrips(ReadOnlyMemory<object> readOnlyMemory) => AsMemory_Roundtrips_Core(readOnlyMemory);

        [Theory]
        [MemberData(nameof(ReadOnlyMemoryCharInstances))]
        public static void AsMemory_Roundtrips(ReadOnlyMemory<char> readOnlyMemory)
        {
            AsMemory_Roundtrips_Core(readOnlyMemory);

            Memory<char> memory = MemoryMarshal.AsMemory(readOnlyMemory);
            ReadOnlyMemory<char> readOnlyClone = memory;

            // TryGetString
            bool gotString1 = readOnlyMemory.TryGetString(out string text1, out int start1, out int length1);
            Assert.Equal(gotString1, readOnlyClone.TryGetString(out string text2, out int start2, out int length2));
            Assert.Same(text1, text2);
            Assert.Equal(start1, start2);
            Assert.Equal(length1, length2);
            if (gotString1)
            {
                Assert.False(memory.TryGetArray(out ArraySegment<char> array));
            }
        }

        private static unsafe void AsMemory_Roundtrips_Core<T>(ReadOnlyMemory<T> readOnlyMemory)
        {
            Memory<T> memory = MemoryMarshal.AsMemory(readOnlyMemory);
            ReadOnlyMemory<T> readOnlyClone = memory;

            // Equals
            Assert.True(readOnlyMemory.Equals(readOnlyClone));
            Assert.True(readOnlyMemory.Equals((object)readOnlyClone));
            Assert.True(readOnlyMemory.Equals((object)memory));

            // Span
            Assert.True(readOnlyMemory.Span == readOnlyClone.Span);
            Assert.True(readOnlyMemory.Span == memory.Span);

            // TryGetArray
            Assert.True(MemoryMarshal.TryGetArray(readOnlyMemory, out ArraySegment<T> array1) == memory.TryGetArray(out ArraySegment<T> array2));
            Assert.Same(array1.Array, array2.Array);
            Assert.Equal(array1.Offset, array2.Offset);
            Assert.Equal(array1.Count, array2.Count);

            // Retain
            using (MemoryHandle readOnlyMemoryHandle = readOnlyMemory.Retain())
            using (MemoryHandle readOnlyCloneHandle = readOnlyMemory.Retain())
            using (MemoryHandle memoryHandle = readOnlyMemory.Retain())
            {
                Assert.Equal((IntPtr)readOnlyMemoryHandle.Pointer, (IntPtr)readOnlyCloneHandle.Pointer);
                Assert.Equal((IntPtr)readOnlyMemoryHandle.Pointer, (IntPtr)memoryHandle.Pointer);
            }
        }
    }
}
