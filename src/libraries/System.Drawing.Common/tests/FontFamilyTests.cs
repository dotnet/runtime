// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing.Text;
using Xunit;

namespace System.Drawing.Tests
{
    public class FontFamilyTests
    {
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(GenericFontFamilies.Serif - 1, "Courier New")] // Value is outside the enum range.
        [InlineData(GenericFontFamilies.Monospace + 1, "Courier New")] // Value is outside the enum range.
        [InlineData(GenericFontFamilies.Monospace, "Courier New")]
        [InlineData(GenericFontFamilies.SansSerif, "Microsoft Sans Serif")]
        [InlineData(GenericFontFamilies.Serif, "Times New Roman")]
        public void Ctor_GenericFamily(GenericFontFamilies genericFamily, string expectedName)
        {
            using (var fontFamily = new FontFamily(genericFamily))
            {
                Assert.Equal(expectedName, fontFamily.Name);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData("Courier New", "Courier New")]
        [InlineData("Microsoft Sans Serif", "Microsoft Sans Serif")]
        [InlineData("Times New Roman", "Times New Roman")]
        [InlineData("times new roman", "Times New Roman")]
        public void Ctor_Name(string name, string expectedName)
        {
            using (var fontFamily = new FontFamily(name))
            {
                Assert.Equal(expectedName, fontFamily.Name);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Name_FontCollection()
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                fontCollection.AddFontFile(Helpers.GetTestFontPath("CodeNewRoman.otf"));

                using (var fontFamily = new FontFamily("Code New Roman", fontCollection))
                {
                    Assert.Equal("Code New Roman", fontFamily.Name);
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(null)]
        [InlineData("NoSuchFont")]
        [InlineData("Serif")]
        public void Ctor_NoSuchFontName_ThrowsArgumentException(string name)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new FontFamily(name));
            AssertExtensions.Throws<ArgumentException>(null, () => new FontFamily(name, null));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NoSuchFontNameInCollection_ThrowsArgumentException()
        {
            var fontCollection = new PrivateFontCollection();
            Assert.Throws<ArgumentException>(null, () => new FontFamily("Times New Roman", fontCollection));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            FontFamily fontFamily = FontFamily.GenericMonospace;
            yield return new object[] { fontFamily, fontFamily, true };
            yield return new object[] { FontFamily.GenericMonospace, FontFamily.GenericMonospace, true };
            yield return new object[] { FontFamily.GenericMonospace, FontFamily.GenericSansSerif, false };

            yield return new object[] { FontFamily.GenericSansSerif, new object(), false };
            yield return new object[] { FontFamily.GenericSansSerif, null, false };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(FontFamily fontFamily, object other, bool expected)
        {
            try
            {
                Assert.Equal(expected, fontFamily.Equals(other));
            }
            finally
            {
                fontFamily.Dispose();
                (other as IDisposable)?.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Families_Get_ReturnsExpected()
        {
#pragma warning disable 0618 // FontFamily.GetFamilies is deprecated.
            using (var image = new Bitmap(10, 10))
            using (var graphics = Graphics.FromImage(image))
            {
                FontFamily[] families = FontFamily.Families;
                FontFamily[] familiesWithGraphics = FontFamily.GetFamilies(graphics);

                // FontFamily.Equals uses the native handle to determine equality. However, GDI+ does not always
                // cache handles, so we cannot just Assert.Equal(families, familiesWithGraphics); 
                Assert.Equal(families.Length, familiesWithGraphics.Length);

                for (int i = 0; i < families.Length; i++)
                {
                    Assert.Equal(families[i].Name, familiesWithGraphics[i].Name);
                }

                foreach (FontFamily fontFamily in families)
                {
                    using (FontFamily copy = new FontFamily(fontFamily.Name))
                    {
                        Assert.Equal(fontFamily.Name, copy.Name);
                    }
                }
            }
#pragma warning restore 0618
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GenericMonospace_Get_ReturnsExpected()
        {
            using (FontFamily fontFamily1 = FontFamily.GenericMonospace)
            {
                using (FontFamily fontFamily2 = FontFamily.GenericMonospace)
                {
                    Assert.NotSame(fontFamily1, fontFamily2);
                    Assert.Equal("Courier New", fontFamily2.Name);
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GenericSansSerif_Get_ReturnsExpected()
        {
            using (FontFamily fontFamily1 = FontFamily.GenericSansSerif)
            {
                using (FontFamily fontFamily2 = FontFamily.GenericSansSerif)
                {
                    Assert.NotSame(fontFamily1, fontFamily2);
                    Assert.Equal("Microsoft Sans Serif", fontFamily2.Name);
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GenericSerif_Get_ReturnsExpected()
        {
            using (FontFamily fontFamily1 = FontFamily.GenericSerif)
            {
                using (FontFamily fontFamily2 = FontFamily.GenericSerif)
                {
                    Assert.NotSame(fontFamily1, fontFamily2);
                    Assert.Equal("Times New Roman", fontFamily2.Name);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetFamilies_NullGraphics_ThrowsArgumentNullException()
        {
#pragma warning disable 0618 // FontFamily.GetFamilies is deprecated.
            AssertExtensions.Throws<ArgumentNullException>("graphics", () => FontFamily.GetFamilies(null));
#pragma warning restore 0618
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetHashCode_Invoke_ReturnsNameHashCode()
        {
            using (FontFamily fontFamily = FontFamily.GenericSansSerif)
            {
                Assert.Equal(fontFamily.GetName(0).GetHashCode(), fontFamily.GetHashCode());
            }
        }

        public static IEnumerable<object[]> FontStyle_TestData()
        {
            yield return new object[] { FontStyle.Bold };
            yield return new object[] { FontStyle.Italic };
            yield return new object[] { FontStyle.Regular };
            yield return new object[] { FontStyle.Strikeout };
            yield return new object[] { FontStyle.Strikeout };
            yield return new object[] { FontStyle.Regular - 1 };
            yield return new object[] { FontStyle.Strikeout + 1 };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(FontStyle_TestData))]
        public void FontFamilyProperties_CustomFont_ReturnsExpected(FontStyle style)
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                fontCollection.AddFontFile(Helpers.GetTestFontPath("CodeNewRoman.otf"));

                using (var fontFamily = new FontFamily("Code New Roman", fontCollection))
                {
                    Assert.True(fontFamily.IsStyleAvailable(style));
                    Assert.Equal(1884, fontFamily.GetCellAscent(style));
                    Assert.Equal(514, fontFamily.GetCellDescent(style));
                    Assert.Equal(2048, fontFamily.GetEmHeight(style));
                    Assert.Equal(2398, fontFamily.GetLineSpacing(style));
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void IsStyleAvailable_Disposed_ThrowsArgumentException()
        {
            FontFamily fontFamily = FontFamily.GenericMonospace;
            fontFamily.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontFamily.IsStyleAvailable(FontStyle.Italic));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetEmHeight_Disposed_ThrowsArgumentException()
        {
            FontFamily fontFamily = FontFamily.GenericMonospace;
            fontFamily.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontFamily.GetEmHeight(FontStyle.Italic));
        }

        private const int FrenchLCID = 1036;

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1, "Code New Roman")]
        [InlineData(0, "Code New Roman")]
        [InlineData(int.MaxValue, "Code New Roman")]
        // This font has been modified to change the name to "Bonjour" if the language is French.
        [InlineData(FrenchLCID, "Bonjour")]
        public void GetName_LanguageCode_ReturnsExpected(int languageCode, string expectedName)
        {
            using (var fontCollection = new PrivateFontCollection())
            {
                fontCollection.AddFontFile(Helpers.GetTestFontPath("CodeNewRoman.ttf"));

                using (var fontFamily = new FontFamily("Code New Roman", fontCollection))
                {
                    Assert.Equal(expectedName, fontFamily.GetName(languageCode));
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetName_Disposed_ThrowsArgumentException()
        {
            FontFamily fontFamily = FontFamily.GenericMonospace;
            fontFamily.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontFamily.GetName(0));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetCellAscent_Disposed_ThrowsArgumentException()
        {
            FontFamily fontFamily = FontFamily.GenericMonospace;
            fontFamily.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontFamily.GetCellAscent(FontStyle.Italic));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetCellDescent_Disposed_ThrowsArgumentException()
        {
            FontFamily fontFamily = FontFamily.GenericMonospace;
            fontFamily.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontFamily.GetCellDescent(FontStyle.Italic));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetLineSpacing_Disposed_ThrowsArgumentException()
        {
            FontFamily fontFamily = FontFamily.GenericMonospace;
            fontFamily.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => fontFamily.GetLineSpacing(FontStyle.Italic));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Dispose_MultipleTimes_Nop()
        {
            FontFamily fontFamily = FontFamily.GenericMonospace;
            fontFamily.Dispose();
            fontFamily.Dispose();
        }
    }
}
