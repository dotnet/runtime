// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public class AllowedAsciiCodePointsTests
    {
        [Fact]
        public void AllowedAsciiCodePointsTestBattery()
        {
            OptimizedInboxTextEncoder._RunAllowedAsciiCodePointsTestBattery();
        }
    }
}

namespace System.Text.Encodings.Web
{
    internal partial class OptimizedInboxTextEncoder
    {
        internal static void _RunAllowedAsciiCodePointsTestBattery()
        {
            // Arrange
            // Allow only characters that are multiples of 3 or 7.

            static bool IsValueAllowed(int value) => ((value % 3) == 0) || ((value % 7) == 0);

            var bitmap = new AllowedBmpCodePointsBitmap();
            for (int i = 0; i < 1024; i++) // include C0 controls & characters beyond ASCII range
            {
                if (IsValueAllowed(i)) { bitmap.AllowChar((char)i); }
            }

            // Act

            using BoundedMemory<AllowedAsciiCodePoints> boundedMemory = BoundedMemory.Allocate<AllowedAsciiCodePoints>(1); // use BoundedMemory to detect out-of-bound accesses
            ref var allowedAsciiCodePoints = ref boundedMemory.Span[0];

            allowedAsciiCodePoints.PopulateAllowedCodePoints(bitmap);
            boundedMemory.MakeReadonly();

            // Assert
            // Note: We test negative inputs as well to exercise edge cases in memory accesses

            for (int i = -1024; i < 1024; i++)
            {
                bool expected = UnicodeUtility.IsAsciiCodePoint((uint)i) && !char.IsControl((char)i) && IsValueAllowed(i);
                bool actual = allowedAsciiCodePoints.IsAllowedAsciiCodePoint((uint)i);
                Assert.Equal(expected, actual);
            }
        }
    }
}
