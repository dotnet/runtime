// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.InteropServices;

namespace System.MemoryTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void ReadOnlyMemory_TryGetString_Roundtrips()
        {
            string input = "0123456789";
            ReadOnlyMemory<char> m = input.AsMemory();
            Assert.False(m.IsEmpty);

            Assert.True(MemoryMarshal.TryGetString(m, out string text, out int start, out int length));
            Assert.Same(input, text);
            Assert.Equal(0, start);
            Assert.Equal(input.Length, length);

            m = m.Slice(1);
            Assert.True(MemoryMarshal.TryGetString(m, out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(1, start);
            Assert.Equal(input.Length - 1, length);

            m = m.Slice(1);
            Assert.True(MemoryMarshal.TryGetString(m, out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(2, start);
            Assert.Equal(input.Length - 2, length);

            m = m.Slice(3, 2);
            Assert.True(MemoryMarshal.TryGetString(m, out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(5, start);
            Assert.Equal(2, length);

            m = m.Slice(m.Length);
            Assert.True(MemoryMarshal.TryGetString(m, out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(7, start);
            Assert.Equal(0, length);

            m = m.Slice(0);
            Assert.True(MemoryMarshal.TryGetString(m, out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(7, start);
            Assert.Equal(0, length);

            m = m.Slice(0, 0);
            Assert.True(MemoryMarshal.TryGetString(m, out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(7, start);
            Assert.Equal(0, length);

            Assert.True(m.IsEmpty);
        }

        [Fact]
        public static void Array_TryGetString_ReturnsFalse()
        {
            ReadOnlyMemory<char> m = new char[10];
            Assert.False(MemoryMarshal.TryGetString(m, out string text, out int start, out int length));
            Assert.Null(text);
            Assert.Equal(0, start);
            Assert.Equal(0, length);
        }
    }
}
