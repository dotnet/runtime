// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Internal;
using System.Collections.Immutable;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class HashTests
    {
        [Fact]
        public void GetFNVHashCodeByteTest()
        {
            Assert.Equal(-1088511923, Hash.GetFNVHashCode(new byte[] { 0xFF, 0xD1 }));
        }

        [Fact]
        public void GetFNVHashCodeImmutableByteTest()
        {
            Assert.Equal(-1088511923, Hash.GetFNVHashCode(ImmutableArray.Create((byte)0xFF, (byte)0xD1).AsSpan()));
        }

        [Fact]
        public void CombineIntInt()
        {
            Assert.Equal(536869063, Hash.Combine(13, 42));
        }

        [Fact]
        public void CombineUIntInt()
        {
            Assert.Equal(536869063, Hash.Combine((uint)13, 42));
        }

        [Fact]
        public void CombineBoolInt()
        {
            Assert.Equal(-1521134253, Hash.Combine(true, 42));
        }
    }
}
