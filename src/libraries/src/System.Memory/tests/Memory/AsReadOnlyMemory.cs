// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    public static class AsReadOnlyMemory
    {
        [Fact]
        public static void EmptyMemoryAsReadOnlyMemory()
        {
            Memory<int> memory = default;
            Assert.True(memory.AsReadOnlyMemory().IsEmpty);
        }

        [Fact]
        public static void MemoryAsReadOnlyMemory()
        {
            int[] a = { 19, -17 };
            Memory<int> memory = new Memory<int>(a);
            ReadOnlyMemory<int> readOnlyMemory = memory.AsReadOnlyMemory();

            readOnlyMemory.Validate(a);
        }
    }
}
