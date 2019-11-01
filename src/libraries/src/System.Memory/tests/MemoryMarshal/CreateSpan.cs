// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static System.TestHelpers;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void CreateSpan()
        {
            TestClass testClass = new TestClass
            {
                C0 = 'a',
                C1 = 'b',
                C2 = 'c',
                C3 = 'd',
                C4 = 'e'
            };
            Span<char> span = MemoryMarshal.CreateSpan<char>(ref testClass.C1, 3);
            span.Validate('b', 'c', 'd');

            ref char pc1 = ref MemoryMarshal.GetReference(span);
            Assert.True(Unsafe.AreSame(ref testClass.C1, ref pc1));
        }

        [Fact]
        public static void SpanGetReferencePointerCreateSpan()
        {
            TestClass testClass = new TestClass();
            Span<char> span = MemoryMarshal.CreateSpan<char>(ref testClass.C1, 3);

            ref char pinnableReference = ref MemoryMarshal.GetReference(span);
            Assert.True(Unsafe.AreSame(ref testClass.C1, ref pinnableReference));
        }

    }
}
