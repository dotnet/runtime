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
        public static void IndexOverflow()
        {
            //
            // Although Span constrains indexes to 0..2Gb, it does not similarly constrain index * sizeof(T).
            // Make sure that internal offset calculcations handle the >2Gb case properly.
            //
            unsafe
            {
                byte* pMemory;
                try
                {
                    pMemory = (byte*)Marshal.AllocHGlobal((IntPtr)ThreeGiB);
                }
                catch (Exception)
                {
                    return;  // It's not implausible to believe that a 3gb allocation will fail - if so, skip this test to avoid unnecessary test flakiness.
                }

                try
                {
                    ReadOnlySpan<Guid> span = new ReadOnlySpan<Guid>(pMemory, GuidThreeGiBLimit);

                    int bigIndex = checked(GuidTwoGiBLimit + 1);
                    uint byteOffset = checked((uint)bigIndex * (uint)sizeof(Guid));
                    Assert.True(byteOffset > (uint)int.MaxValue);  // Make sure byteOffset actually overflows 2Gb, or this test is pointless.
                    ref Guid expected = ref Unsafe.AsRef<Guid>(((byte*)pMemory) + byteOffset);
                    Guid expectedGuid = Guid.NewGuid();
                    expected = expectedGuid;
                    Guid actualGuid = span[bigIndex];
                    Assert.Equal(expectedGuid, actualGuid);

                    ReadOnlySpan<Guid> slice = span.Slice(bigIndex);
                    Assert.True(Unsafe.AreSame<Guid>(ref expected, ref slice.DangerousGetPinnableReference()));

                    slice = span.Slice(bigIndex, 1);
                    Assert.True(Unsafe.AreSame<Guid>(ref expected, ref slice.DangerousGetPinnableReference()));
                }
                finally
                {
                    Marshal.FreeHGlobal((IntPtr)pMemory);
                }
            }
        }

        private const long ThreeGiB = 3L * 1024L * 1024L * 1024L;
        private const long TwoGiB = 2L * 1024L * 1024L * 1024L;
        private const long OneGiB = 1L * 1024L * 1024L * 1024L;

        private static readonly int GuidThreeGiBLimit = (int)(ThreeGiB / Unsafe.SizeOf<Guid>());  // sizeof(Guid) requires unsafe keyword and I don't want to mark the entire class unsafe.
        private static readonly int GuidTwoGiBLimit = (int)(TwoGiB / Unsafe.SizeOf<Guid>());
        private static readonly int GuidOneGiBLimit = (int)(OneGiB / Unsafe.SizeOf<Guid>());
    }
}
