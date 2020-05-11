// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class CachedBitmapTests
    {
        [ConditionalFact(Helpers.IsCachedBitmapSupported)]
        public void Ctor_Throws_ArgumentNullException()
        {
            using var bitmap = new Bitmap(10, 10);
            using var graphics = Graphics.FromImage(bitmap);

            Assert.Throws<ArgumentNullException>(() => new CachedBitmap(bitmap, null));
            Assert.Throws<ArgumentNullException>(() => new CachedBitmap(null, graphics));
        }

        [ConditionalFact(Helpers.IsCachedBitmapSupported)]
        public void Disposed_CachedBitmap_Throws_ArgumentException()
        {
            using var bitmap = new Bitmap(10, 10);
            using var graphics = Graphics.FromImage(bitmap);
            using var cached = new CachedBitmap(bitmap, graphics);

            cached.Dispose();

            Assert.Throws<ArgumentException>(() => graphics.DrawCachedBitmap(cached, 0, 0));
        }

        [ConditionalFact(Helpers.IsCachedBitmapSupported)]
        public void DrawCachedBitmap_Throws_ArgumentNullException()
        {
            using var bitmap = new Bitmap(10, 10);
            using var graphics = Graphics.FromImage(bitmap);
            Assert.Throws<ArgumentNullException>(() => graphics.DrawCachedBitmap(null, 0, 0));
        }

        static string[] bitmaps = new string[]
        {
            "81674-2bpp.png",
            "64x64_one_entry_8bit.ico",
            "16x16_one_entry_4bit.ico",
            "16x16_nonindexed_24bit.png"
        };

        public class CachedBitmapOffsetTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                foreach (string bitmap in bitmaps)
                {
                    yield return new object[] { bitmap, 0, 0 };
                    yield return new object[] { bitmap, 20, 20 };
                    yield return new object[] { bitmap, 200, 200 };
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        static void CompareEqual(Bitmap expected, Bitmap actual, int xOffset = 0, int yOffset = 0)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                for (int y = 0; y < expected.Height; y++)
                {
                    Color expectedColor = expected.GetPixel(x, y);
                    Color actualColor = actual.GetPixel(x + xOffset, y + yOffset);
                    Assert.Equal(expectedColor, actualColor);
                }
            }
        }

        [ConditionalTheory(Helpers.IsCachedBitmapSupported)]
        [ClassData(typeof(CachedBitmapOffsetTestData))]
        public void CachedBitmap_Drawing_Roundtrips(string filename, int xOffset, int yOffset)
        {
            using var originalBitmap = new Bitmap(Helpers.GetTestBitmapPath(filename));

            using var surface = new Bitmap(originalBitmap.Width + xOffset, originalBitmap.Height + yOffset);
            using var graphics = Graphics.FromImage(surface);
            using var cachedBitmap = new CachedBitmap(originalBitmap, graphics);

            graphics.DrawCachedBitmap(cachedBitmap, xOffset, yOffset);

            CompareEqual(originalBitmap, surface, xOffset, yOffset);
        }

        [ConditionalFact(Helpers.IsCachedBitmapSupported)]
        public void CachedBitmap_Respects_ClipRectangle()
        {
            using var originalBitmap = new Bitmap(Helpers.GetTestBitmapPath("cachedbitmap_test_original.png"));
            using var clippedBitmap = new Bitmap(Helpers.GetTestBitmapPath("cachedbitmap_test_clip_20_20_20_20.png"));

            using var surface = new Bitmap(originalBitmap.Width, originalBitmap.Height);
            using var graphics = Graphics.FromImage(surface);
            using var cachedBitmap = new CachedBitmap(originalBitmap, graphics);

            graphics.Clip = new Region(new Rectangle(20, 20, 20, 20));
            graphics.DrawCachedBitmap(cachedBitmap, 0, 0);

            CompareEqual(clippedBitmap, surface); 
        }

        [ConditionalFact(Helpers.IsCachedBitmapSupported)]
        public void CachedBitmap_Respects_TranslationMatrix()
        {
            using var originalBitmap = new Bitmap(Helpers.GetTestBitmapPath("cachedbitmap_test_original.png"));
            using var translatedBitmap = new Bitmap(Helpers.GetTestBitmapPath("cachedbitmap_test_translate_30_30.png"));

            using var surface = new Bitmap(originalBitmap.Width, originalBitmap.Height);
            using var graphics = Graphics.FromImage(surface);
            using var cachedBitmap = new CachedBitmap(originalBitmap, graphics);

            graphics.TranslateTransform(30, 30);
            graphics.DrawCachedBitmap(cachedBitmap, 0, 0);

            CompareEqual(translatedBitmap, surface);

            graphics.ScaleTransform(30, 30);
            Assert.Throws<InvalidOperationException>(() => graphics.DrawCachedBitmap(cachedBitmap, 0, 0));
            graphics.RotateTransform(30);
            Assert.Throws<InvalidOperationException>(() => graphics.DrawCachedBitmap(cachedBitmap, 0, 0));
        }
    }
}
