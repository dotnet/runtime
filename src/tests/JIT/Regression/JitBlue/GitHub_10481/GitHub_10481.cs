// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Repro case for a bug where copies from one struct-typed field of a local
// to another were being illegally elided.

namespace N
{
    // Original Repro
    public struct BytesReader2
    {
        ArraySegmentWrapper _unreadSegments;
        ArraySegment<byte> _currentSegment;

        public BytesReader2(ArraySegmentWrapper bytes)
        {
            _unreadSegments = bytes;
            _currentSegment = _unreadSegments.First; // copy gets lost when inlined into RunTest()
        }

        public bool IsEmpty => _currentSegment.Count == 0;
    }

    public struct ArraySegmentWrapper
    {
        ArraySegment<byte> _first;

        public ArraySegment<byte> First => _first;

        public ArraySegmentWrapper(ArraySegment<byte> first)
        {
            _first = first;
        }
    }

    static class Repro1
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int RunTest()
        {
            var data = new byte[] { 1 };
            var segment = new ArraySegment<byte>(data);
            var wrapper = new ArraySegmentWrapper(segment);
            var reader2 = new BytesReader2(wrapper);
            return reader2.IsEmpty ? 50 : 100;
        }
    }

    // Simplified repro
    struct Inner
    {
        public long x;
        public long y;
    }

    struct Outer
    {
        public int z;
        public Inner i;
        public Inner j;
    }

    static class Repro2
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long sum(Outer o)
        {
            return o.i.x + o.i.y + o.j.x + o.j.y + (long)o.z;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Inner getInner()
        {
            return new Inner() { x = 7, y = 33 };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int RunTest()
        {
            Outer o;
            o.i = getInner();
            o.j = o.i;  // Copy gets lost
            o.z = 20;
            return (int)sum(o);
        }
    }

    public static class C
    {
        [Fact]
        public static int TestEntryPoint()
        {
            if (Repro1.RunTest() != 100)
            {
              return -1;
            }
            return Repro2.RunTest();
        }
    }
}
