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
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Default()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                Assert.Empty(fontCollection.Families);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddFontFile_AbsolutePath_Success()
        {
            // GDI+ on Windows 7 incorrectly throws a FileNotFoundException.
            if (PlatformDetection.IsWindows7)
            {
                return;
            }

            using (var fontCollection = new PrivateFontCollection())
            {
                fontCollection.AddFontFile(Helpers.GetTestBitmapPath("empty.file"));
                fontCollection.AddFontFile(Helpers.GetTestFontPath("CodeNewRoman.otf"));

                FontFamily fontFamily = Assert.Single(fontCollection.Families);
                Assert.Equal("Code New Roman", fontFamily.Name);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddFontFile_RelativePath_Success()
        {
            // GDI+ on Windows 7 incorrectly throws a FileNotFoundException.
            if (PlatformDetection.IsWindows7)
            {
                return;
            }

            using (var fontCollection = new PrivateFontCollection())
            {
                string relativePath = Path.Combine("fonts", "CodeNewRoman.ttf");
                fontCollection.AddFontFile(relativePath);

                FontFamily fontFamily = Assert.Single(fontCollection.Families);
                Assert.Equal("Code New Roman", fontFamily.Name);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddFontFile_SamePathMultipleTimes_FamiliesContainsOnlyOneFont()
        {
            // GDI+ on Windows 7 incorrectly throws a FileNotFoundException.
            if (PlatformDetection.IsWindows7)
            {
                return;
            }

            using (var fontCollection = new PrivateFontCollection())
            {
                fontCollection.AddFontFile(Helpers.GetTestFontPath("CodeNewRoman.ttf"));
                fontCollection.AddFontFile(Helpers.GetTestFontPath("CodeNewRoman.ttf"));

                FontFamily fontFamily = Assert.Single(fontCollection.Families);
                Assert.Equal("Code New Roman", fontFamily.Name);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddFontFile_SameNameMultipleTimes_FamiliesContainsFirstFontOnly()
        {
            // GDI+ on Windows 7 incorrectly throws a FileNotFoundException.
            if (PlatformDetection.IsWindows7)
            {
                return;
            }

            using (var fontCollection = new PrivateFontCollection())
            {
                fontCollection.AddFontFile(Helpers.GetTestFontPath("CodeNewRoman.ttf"));
                fontCollection.AddFontFile(Helpers.GetTestFontPath("CodeNewRoman.otf"));

                // Verify that the first file is used by checking that it contains metadata
                // associated with CodeNewRoman.ttf.
                const int FrenchLCID = 1036;
                FontFamily fontFamily = Assert.Single(fontCollection.Families);
                Assert.Equal("Code New Roman", fontFamily.Name);
                Assert.Equal("Bonjour", fontFamily.GetName(FrenchLCID));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddFontFile_NullFileName_ThrowsArgumentNullException()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                AssertExtensions.Throws<ArgumentNullException>("path", () => fontCollection.AddFontFile(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddFontFile_InvalidPath_ThrowsArgumentException()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                AssertExtensions.Throws<ArgumentException>("path", null, () => fontCollection.AddFontFile(string.Empty));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddFontFile_NoSuchFilePath_ThrowsFileNotFoundException()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                Assert.Throws<FileNotFoundException>(() => fontCollection.AddFontFile("fileName"));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/8655")]
        public void AddFontFile_LongFilePath_ThrowsException()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                // Throws PathTooLongException on Desktop and FileNotFoundException elsewhere.
                if (PlatformDetection.IsFullFramework)
                {
                    Assert.Throws<PathTooLongException>(
                        () => fontCollection.AddFontFile(new string('a', 261)));
                }
                else
                {
                    Assert.Throws<FileNotFoundException>(
                        () => fontCollection.AddFontFile(new string('a', 261)));
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddFontFile_Directory_ThrowsExternalException()
        {
            // GDI+ on Windows 7 and Windows 8.1 incorrectly does not throw.
            if (PlatformDetection.IsWindows || PlatformDetection.IsWindows8x)
            {
                return;
            }

            using (var fontCollection = new PrivateFontCollection())
            {
                Assert.Throws<ExternalException>(() => fontCollection.AddFontFile(AppContext.BaseDirectory));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddFontFile_Disposed_ThrowsArgumentException()
        {
            var fontCollection = new PrivateFontCollection();
            fontCollection.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontCollection.AddFontFile("fileName"));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
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

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddMemoryFont_ZeroMemory_ThrowsArgumentException()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => fontCollection.AddMemoryFont(IntPtr.Zero, 100));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(-1)]
        public void AddMemoryFont_InvalidLength_ThrowsArgumentException(int length)
        {
            // GDI+ on Windows 7 incorrectly throws a FileNotFoundException.
            if (PlatformDetection.IsWindows)
            {
                return;
            }

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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AddMemoryFont_Disposed_ThrowsArgumentException()
        {
            var fontCollection = new PrivateFontCollection();
            fontCollection.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontCollection.AddMemoryFont((IntPtr)10, 100));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Families_GetWhenDisposed_ThrowsArgumentException()
        {
            var fontCollection = new PrivateFontCollection();
            fontCollection.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontCollection.Families);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Dispose_MultipleTimes_Nop()
        {
            var fontCollection = new PrivateFontCollection();
            fontCollection.Dispose();
            fontCollection.Dispose();
        }
    }
}
