// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;

unsafe class BufferMemmoveUnrolling
{
    static int Main()
    {
        // Carefully test 0..32
        TestMemmove((dst, src) => src.AsSpan(0, 0).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(0)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 1).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(1)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 2).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(2)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 3).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(3)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 4).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(4)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 5).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(5)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 6).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(6)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 7).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(7)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 8).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(8)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 9).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(9)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 10).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(10)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 11).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(11)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 12).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(12)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 13).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(13)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 14).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(14)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 15).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(15)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 16).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(16)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 17).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(17)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 18).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(18)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 19).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(19)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 20).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(20)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 21).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(21)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 22).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(22)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 23).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(23)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 24).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(24)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 25).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(25)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 26).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(26)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 27).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(27)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 28).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(28)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 29).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(29)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 30).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(30)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 31).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(31)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 32).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(32)).CopyTo(dst));

        // Some large simds
        TestMemmove((dst, src) => src.AsSpan(0, 33).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(33)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 47).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(47)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 48).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(48)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 49).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(49)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 63).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(63)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 64).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(64)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 65).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(65)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 95).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(95)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 96).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(96)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 97).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(97)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 127).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(127)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 128).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(128)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 129).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(129)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 159).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(159)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 160).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(160)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 161).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(161)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 191).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(191)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 192).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(192)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 193).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(193)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 255).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(255)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 256).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(256)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 257).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(257)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 511).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(511)).CopyTo(dst));
        TestMemmove((dst, src) => src.AsSpan(0, 512).CopyTo(dst), (dst, src) => src.AsSpan(0, ToVar(512)).CopyTo(dst));

        // A couple of tests for overlapped pointers
        TestMemmoveOverlap(
            (dst, src) => new Span<short>((void*)src, 1).CopyTo(new Span<short>((void*)dst, 1)),
            (dst, src) => new Span<short>((void*)src, ToVar(1)).CopyTo(new Span<short>((void*)dst, ToVar(1))));
        TestMemmoveOverlap(
            (dst, src) => new Span<short>((void*)src, 8).CopyTo(new Span<short>((void*)dst, 8)),
            (dst, src) => new Span<short>((void*)src, ToVar(8)).CopyTo(new Span<short>((void*)dst, ToVar(8))));
        TestMemmoveOverlap(
            (dst, src) => new Span<short>((void*)src, 10).CopyTo(new Span<short>((void*)dst, 10)),
            (dst, src) => new Span<short>((void*)src, ToVar(10)).CopyTo(new Span<short>((void*)dst, ToVar(10))));
        TestMemmoveOverlap(
            (dst, src) => new Span<short>((void*)src, 17).CopyTo(new Span<short>((void*)dst, 17)),
            (dst, src) => new Span<short>((void*)src, ToVar(17)).CopyTo(new Span<short>((void*)dst, ToVar(17))));
        TestMemmoveOverlap(
            (dst, src) => new Span<short>((void*)src, 64).CopyTo(new Span<short>((void*)dst, 64)),
            (dst, src) => new Span<short>((void*)src, ToVar(64)).CopyTo(new Span<short>((void*)dst, ToVar(64))));
        TestMemmoveOverlap(
            (dst, src) => new Span<short>((void*)src, 120).CopyTo(new Span<short>((void*)dst, 120)),
            (dst, src) => new Span<short>((void*)src, ToVar(120)).CopyTo(new Span<short>((void*)dst, ToVar(120))));
        TestMemmoveOverlap(
            (dst, src) => new Span<short>((void*)src, 256).CopyTo(new Span<short>((void*)dst, 256)),
            (dst, src) => new Span<short>((void*)src, ToVar(256)).CopyTo(new Span<short>((void*)dst, ToVar(256))));

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static T ToVar<T>(T t) => t;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestMemmove(Action<byte[], byte[]> testAction, Action<byte[], byte[]> refAction)
    {
        // Managed arrays here also test GC info in the tests (under GCStress)
        byte[] dst1 = new byte[512];
        byte[] src1 = new byte[512];
        dst1.AsSpan().Fill(0xB0);
        src1.AsSpan().Fill(0x0C);

        // Clone them for "reference" action
        byte[] dst2 = (byte[])dst1.Clone();
        byte[] src2 = (byte[])src1.Clone();

        testAction(dst1, src1);
        refAction(dst2, src2);

        // Make sure testAction and refAction modified the same elements
        // and src wasn't changed
        if (!src1.SequenceEqual(src2))
            throw new InvalidOperationException("TestMemmove: src and src2 don't match");

        if (!dst1.SequenceEqual(dst2))
            throw new InvalidOperationException("TestMemmove: dst and dst2 don't match");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestMemmoveOverlap(Action<IntPtr, IntPtr> testAction, Action<IntPtr, IntPtr> refAction)
    {
        Action<int, int> testAtOffset = (srcOffset, dstOffset) =>
        {
            byte[] src1 = Enumerable.Range(0, 1024).Select(i => (byte)i).ToArray();
            byte[] src2 = (byte[])src1.Clone();
            fixed (byte* p1 = src1)
            {
                fixed (byte* p2 = src2)
                {
                    byte* pSrc1 = p1 + srcOffset;
                    byte* pSrc2 = p2 + srcOffset;
                    byte* pDst1 = p1 + dstOffset;
                    byte* pDst2 = p2 + dstOffset;

                    testAction((IntPtr)pDst1, (IntPtr)pSrc1);
                    refAction((IntPtr)pDst2, (IntPtr)pSrc2);
                }
            }
            if (!src1.SequenceEqual(src2))
                throw new InvalidOperationException("TestMemmoveOverlap: src1 and src2 don't match");
        };

        for (int i = 0; i < 32; i++)
        {
            testAtOffset(i, 32);
            testAtOffset(32, i);
        }
        testAtOffset(0, 63);
        testAtOffset(0, 64);
        testAtOffset(0, 127);
        testAtOffset(0, 128);
        testAtOffset(128, 63);
        testAtOffset(128, 64);
        testAtOffset(256, 127);
        testAtOffset(256, 128);
    }
}
