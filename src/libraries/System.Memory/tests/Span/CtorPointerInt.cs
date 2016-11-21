// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;

#pragma warning disable 0649 //Field 'SpanTests.InnerStruct.J' is never assigned to, and will always have its default value 0

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void CtorPointerInt()
        {
            unsafe
            {
                int[] a = { 90, 91, 92 };
                fixed (int *pa = a)
                {
                    Span<int> span = new Span<int>(pa, 3);
                    span.Validate<int>(90, 91, 92);
                    Assert.True(Unsafe.AreSame<int>(ref Unsafe.AsRef<int>(pa), ref span.DangerousGetPinnableReference()));
                }
            }
        }

        [Fact]
        public static void CtorPointerNull()
        {
            unsafe
            {
                Span<int> span = new Span<int>((void*)null, 0);
                span.Validate<int>();
                Assert.True(Unsafe.AreSame<int>(ref Unsafe.AsRef<int>((void*)null), ref span.DangerousGetPinnableReference()));
            }
        }

        [Fact]
        public static void CtorPointerRangeChecks()
        {
            unsafe
            {
                Assert.Throws<ArgumentOutOfRangeException>(
                    delegate ()
                    {
                        int i = 42;
                        Span<int> span = new Span<int>(&i, -1);
                    });
            }
        }

        [Fact]
        public static void CtorPointerNoContainsReferenceEnforcement()
        {
            unsafe
            {
                new Span<int>((void*)null, 0);
                new Span<int?>((void*)null, 0);
                Assert.Throws<ArgumentException>(() => new Span<object>((void*)null, 0).DontBox());
                Assert.Throws<ArgumentException>(() => new Span<StructWithReferences>((void*)null, 0).DontBox());
            }
        }

        private struct StructWithReferences
        {
            public int I;
            public InnerStruct Inner;
        }

        private struct InnerStruct
        {
            public int J;
            public object O;
        }
    }
}
