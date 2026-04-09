// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text.Unicode;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public class AllowedBmpCodePointsBitmapTests
    {
        [Fact]
        public void Ctor_EmptyByDefault()
        {
            // Act
            var bitmap = new AllowedBmpCodePointsBitmap();

            // Assert
            for (int i = 0; i <= char.MaxValue; i++)
            {
                Assert.False(bitmap.IsCharAllowed((char)i));
            }
        }

        [Fact]
        public void Allow_Forbid_ZigZag()
        {
            // Arrange - we'll use BoundedMemory in this test to guard against
            // out-of-bounds accesses on the bitmap instance.
            using var boundedMem = BoundedMemory.Allocate<AllowedBmpCodePointsBitmap>(1);
            boundedMem.Span.Clear();
            ref var bitmap = ref boundedMem.Span[0];

            // Act
            // The only chars which are allowed are those whose code points are multiples of 3 or 7
            // who aren't also multiples of 5. Exception: multiples of 35 are allowed.
            for (int i = 0; i <= char.MaxValue; i += 3)
            {
                bitmap.AllowChar((char)i);
            }
            for (int i = 0; i <= char.MaxValue; i += 5)
            {
                bitmap.ForbidChar((char)i);
            }
            for (int i = 0; i <= char.MaxValue; i += 7)
            {
                bitmap.AllowChar((char)i);
            }

            // Assert
            for (int i = 0; i <= char.MaxValue; i++)
            {
                bool isAllowed = false;
                if (i % 3 == 0) { isAllowed = true; }
                if (i % 5 == 0) { isAllowed = false; }
                if (i % 7 == 0) { isAllowed = true; }
                Assert.Equal(isAllowed, bitmap.IsCharAllowed((char)i));
                Assert.Equal(isAllowed, bitmap.IsCodePointAllowed((uint)i));
            }
        }

        [Fact]
        public void CopyByVal_MakesDeepCopy()
        {
            // Arrange
            var originalBitmap = new AllowedBmpCodePointsBitmap();
            originalBitmap.AllowChar('x');

            // Act
            var clonedBitmap = originalBitmap; // struct byval copy
            clonedBitmap.AllowChar('y');

            // Assert
            Assert.True(originalBitmap.IsCharAllowed('x'));
            Assert.False(originalBitmap.IsCharAllowed('y'));
            Assert.True(clonedBitmap.IsCharAllowed('x'));
            Assert.True(clonedBitmap.IsCharAllowed('y'));
        }

        [Fact]
        public void ForbidUndefinedCharacters_RemovesUndefinedChars()
        {
            // Arrange
            // We only allow odd-numbered characters in this test so that
            // we can validate that we properly merged the two bitmaps together
            // rather than simply overwriting the target.
            var bitmap = new AllowedBmpCodePointsBitmap();
            for (int i = 1; i <= char.MaxValue; i += 2)
            {
                bitmap.AllowChar((char)i);
            }

            // Act
            bitmap.ForbidUndefinedCharacters();

            // Assert
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (i % 2 == 0)
                {
                    Assert.False(bitmap.IsCharAllowed((char)i)); // these chars were never allowed in the original description
                }
                else
                {
                    Assert.Equal(UnicodeTestHelpers.IsCharacterDefined((char)i), bitmap.IsCharAllowed((char)i));
                }
            }
        }

        [Fact]
        public void IsCodePointAllowed_NonBmpCodePoints_ReturnsFalse()
        {
            // Arrange - we'll use BoundedMemory in this test to guard against
            // out-of-bounds accesses on the bitmap instance.
            using var boundedMem = BoundedMemory.Allocate<AllowedBmpCodePointsBitmap>(1);
            ref var bitmap = ref boundedMem.Span[0];

            Assert.False(bitmap.IsCodePointAllowed(0x10000)); // start of supplementary plane
            Assert.False(bitmap.IsCodePointAllowed(0x10FFFF)); // end of supplementary plane
            Assert.False(bitmap.IsCodePointAllowed(0x110000));
            Assert.False(bitmap.IsCodePointAllowed(uint.MaxValue));
        }
    }
}
