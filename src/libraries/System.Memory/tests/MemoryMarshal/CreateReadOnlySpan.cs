// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static System.TestHelpers;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void CreateReadOnlySpan()
        {
            TestClass testClass = new TestClass
            {
                C0 = 'a',
                C1 = 'b',
                C2 = 'c',
                C3 = 'd',
                C4 = 'e'
            };
            ReadOnlySpan<char> span = MemoryMarshal.CreateReadOnlySpan<char>(ref testClass.C1, 3);
            span.Validate('b', 'c', 'd');

            ref char pc1 = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
            Assert.True(Unsafe.AreSame(ref testClass.C1, ref pc1));
        }

        [Fact]
        public static void ReadOnlySpanGetReferencePointerCreateReadOnlySpan()
        {
            TestClass testClass = new TestClass();
            ReadOnlySpan<char> span = MemoryMarshal.CreateReadOnlySpan<char>(ref testClass.C1, 3);

            ref char pinnableReference = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
            Assert.True(Unsafe.AreSame(ref testClass.C1, ref pinnableReference));
        }
    }
}
