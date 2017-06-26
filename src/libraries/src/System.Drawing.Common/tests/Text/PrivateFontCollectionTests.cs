// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing.Tests;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Drawing.Text.Tests
{
    public class PrivateFontCollectionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                Assert.Empty(fontCollection.Families);
            }
        }

        [Fact]
        public void AddFontFile_NullFileName_ThrowsArgumentException()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => fontCollection.AddFontFile(null));
            }
        }

        [Fact]
        public void AddFontFile_FontFile_Success()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                fontCollection.AddFontFile(Helpers.GetTestBitmapPath("empty.file"));
                fontCollection.AddFontFile(Helpers.GetTestFontPath("CodeNewRoman.otf"));

                FontFamily font = Assert.Single(fontCollection.Families);
                Assert.Equal("Code New Roman", font.Name);
            }
        }

        public static IEnumerable<object[]> InvalidFileName_TestData()
        {
            yield return new object[] { "" };
            yield return new object[] { "fileName" };
            yield return new object[] { new string('a', 261) };
        }

        [Theory]
        [MemberData(nameof(InvalidFileName_TestData))]
        public void AddFontFile_InvalidFileName_ThrowsFileNotFoundException(string fileName)
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                Assert.Throws<FileNotFoundException>(() => fontCollection.AddFontFile(fileName));
            }
        }

        [Fact]
        public void AddFontFile_Directory_ThrowsExternalException()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                Assert.Throws<ExternalException>(() => fontCollection.AddFontFile(AppContext.BaseDirectory));
            }
        }

        [Fact]
        public void AddFontFile_Disposed_ThrowsArgumentException()
        {
            var fontCollection = new PrivateFontCollection();
            fontCollection.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontCollection.AddFontFile("fileName"));
        }

        [Fact]
        public void AddMemoryFont_ValidMemory_Success()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                byte[] data = File.ReadAllBytes(Helpers.GetTestFontPath("CodeNewRoman.otf"));

                IntPtr fontBuffer = Marshal.AllocCoTaskMem(data.Length);
                try
                {
                    Marshal.Copy(data, 0, fontBuffer, data.Length);
                    fontCollection.AddMemoryFont(fontBuffer, data.Length);

                    FontFamily font = Assert.Single(fontCollection.Families);
                    Assert.Equal("Code New Roman", font.Name);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(fontBuffer);
                }
            }
        }

        [Fact]
        public void AddMemoryFont_ZeroMemory_ThrowsArgumentException()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => fontCollection.AddMemoryFont(IntPtr.Zero, 100));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void AddMemoryFont_InvalidLength_ThrowsArgumentException(int length)
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                byte[] data = File.ReadAllBytes(Helpers.GetTestFontPath("CodeNewRoman.otf"));

                IntPtr fontBuffer = Marshal.AllocCoTaskMem(data.Length);
                try
                {
                    Marshal.Copy(data, 0, fontBuffer, data.Length);
                    AssertExtensions.Throws<ArgumentException>(null, () => fontCollection.AddMemoryFont(fontBuffer, length));
                }
                finally
                {
                    Marshal.FreeCoTaskMem(fontBuffer);
                }
            }
        }

        [Fact]
        public void AddMemoryFont_Disposed_ThrowsArgumentException()
        {
            var fontCollection = new PrivateFontCollection();
            fontCollection.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontCollection.AddMemoryFont((IntPtr)10, 100));
        }

        [Fact]
        public void Families_GetWhenDisposed_ThrowsArgumentException()
        {
            var fontCollection = new PrivateFontCollection();
            fontCollection.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontCollection.Families);
        }

        [Fact]
        public void Dispose_MultipleTimes_Nop()
        {
            var fontCollection = new PrivateFontCollection();
            fontCollection.Dispose();
            fontCollection.Dispose();
        }
    }
}
