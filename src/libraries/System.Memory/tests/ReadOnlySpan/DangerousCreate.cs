// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void DangerousCreateNullObject()
        {
            Assert.Throws<ArgumentNullException>(
                delegate ()
                {
                    int dummy = 4;
                    ReadOnlySpan<int>.DangerousCreate(null, ref dummy, 0);
                });
        }

        [Fact]
        public static void DangerousCreateBadLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                delegate ()
                {
                    TestClass testClass = new TestClass();
                    ReadOnlySpan<char> span = ReadOnlySpan<char>.DangerousCreate(testClass, ref testClass.C1, -1);
                });
        }

        [Fact]
        public static void DangerousCreate1()
        {
            TestClass testClass = new TestClass();
            testClass.C0 = 'a';
            testClass.C1 = 'b';
            testClass.C2 = 'c';
            testClass.C3 = 'd';
            testClass.C4 = 'e';
            ReadOnlySpan<char> span = ReadOnlySpan<char>.DangerousCreate(testClass, ref testClass.C1, 3);
            span.Validate<char>('b', 'c', 'd');

            ref char pc1 = ref span.DangerousGetPinnableReference();
            Assert.True(Unsafe.AreSame<char>(ref testClass.C1, ref pc1));
        }
    }
}
