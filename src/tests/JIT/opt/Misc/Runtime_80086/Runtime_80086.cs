// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

#pragma warning disable CS8500

namespace Runtime_80086
{
    public static unsafe class Test
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Span<int> Marshal1(Span<int> a)
        {
            return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(a), a.Length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Span<int> Marshal2(Span<int>* a)
        {
            return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(*a), a->Length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Span<int> Copy1(Span<int> s) => s;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Span<int> Copy2(Span<int> a)
        {
            ref Span<int> ra = ref a;
            return ra;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Span<int> Copy3(Span<int>* a)
        {
            return *a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Span<int> Copy4(scoped ref Span<int> a)
        {
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Span<int> Copy5(ReadOnlySpan<int> a)
        {
            // Example is used to check code generation but shouldn't be used elsewhere
            return *(Span<int>*)&a;
        }

        [Fact]
        public static void TestEntryPoint()
        {
            Span<int> s = new int[1] { 13 };
            s = Marshal1(s);
            s = Marshal2(&s);
            s = Copy1(s);
            s = Copy2(s);
            s = Copy3(&s);
            s = Copy4(ref s);
            s = Copy5(s);
            Assert.Equal(13, s[0]);
        }
   }
}
