// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

public class ClassWithNoNamespace { }

namespace System.Drawing.Tests
{
    public class bitmap_173x183_indexed_8bit { }

    public class ToolboxBitmapAttributeTests : RemoteExecutorTestBase
    {
        public static IEnumerable<object[]> Ctor_FileName_TestData()
        {
            yield return new object[] { null, new Size(16, 16) };
            yield return new object[] { Helpers.GetTestBitmapPath("bitmap_173x183_indexed_8bit.bmp"), new Size(173, 183) };
            yield return new object[] { Helpers.GetTestBitmapPath("48x48_multiple_entries_4bit.ico"), new Size(16, 16) };
            yield return new object[] { Helpers.GetTestBitmapPath("invalid.ico"), new Size(0, 0) };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Ctor_FileName_TestData))]
        public void Ctor_FileName(string fileName, Size size)
        {
            var attribute = new ToolboxBitmapAttribute(fileName);

            using (Image image = attribute.GetImage(null))
            {
                if (size == Size.Empty)
                {
                    Assert.Throws<ArgumentException>(null, () => image.Size);
                }
                else
                {
                    Assert.Equal(size, image.Size);
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(null, -1, -1)]
        [InlineData(typeof(ClassWithNoNamespace), -1, -1)]
        [InlineData(typeof(bitmap_173x183_indexed_8bit), 173, 183)]
        [InlineData(typeof(ToolboxBitmapAttributeTests), -1, -1)]
        public void Ctor_Type(Type type, int width, int height)
        {
            var attribute = new ToolboxBitmapAttribute(type);
            using (Image image = attribute.GetImage(type))
            {
                if (width == -1 && height == -1)
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => image.Size);   
                }
                else
                {
                    Assert.Equal(new Size(width, height), image.Size);
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(null, null, -1, -1)]
        [InlineData(null, "invalid.ico", -1, -1)]
        [InlineData(typeof(ClassWithNoNamespace), null, -1, -1)]
        [InlineData(typeof(ToolboxBitmapAttributeTests), "", -1, -1)]
        [InlineData(typeof(ToolboxBitmapAttributeTests), null, -1, -1)]
        [InlineData(typeof(ToolboxBitmapAttributeTests), "invalid.ico", -1, -1)]
        [InlineData(typeof(ToolboxBitmapAttributeTests), "48x48_multiple_entries_4bit", 16, 16)]
        [InlineData(typeof(ToolboxBitmapAttributeTests), "48x48_multiple_entries_4bit.ico", 16, 16)]
        [InlineData(typeof(ToolboxBitmapAttributeTests), "empty.file", -1, -1)]
        [InlineData(typeof(ToolboxBitmapAttributeTests), "bitmap_173x183_indexed_8bit", 173, 183)]
        [InlineData(typeof(ToolboxBitmapAttributeTests), "bitmap_173x183_indexed_8bit.bmp", 173, 183)]
        public void Ctor_Type_String(Type type, string fileName, int width, int height)
        {
            var attribute = new ToolboxBitmapAttribute(type, fileName);

            using (Image image = attribute.GetImage(type, fileName, false))
            {
                if (width == -1 && height == -1)
                {
                    Assert.Throws<ArgumentException>(null, () => image.Size);
                }
                else
                {
                    Assert.Equal(new Size(width, height), image.Size);
                }
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData("bitmap_173x183_indexed_8bit.bmp", 173, 183)]
        [InlineData("48x48_multiple_entries_4bit.ico", 16, 16)]
        public void GetImage_TypeFileNameBool_ReturnsExpected(string fileName, int width, int height)
        {
            var attribute = new ToolboxBitmapAttribute((string)null);
            using (Image image = attribute.GetImage(typeof(ToolboxBitmapAttributeTests), fileName, large: true))
            {
                Assert.Equal(new Size(32, 32), image.Size);
            }

            using (Image image = attribute.GetImage(typeof(ToolboxBitmapAttributeTests), fileName, large: false))
            {
                Assert.Equal(new Size(width, height), image.Size);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetImage_NullComponent_ReturnsNull()
        {
            var attribute = new ToolboxBitmapAttribute((string)null);
            Assert.Null(attribute.GetImage((object)null));
            Assert.Null(attribute.GetImage((object)null, true));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetImage_Component_ReturnsExpected()
        {
            ToolboxBitmapAttribute attribute = new ToolboxBitmapAttribute((string)null);

            using (Image smallImage = attribute.GetImage(new bitmap_173x183_indexed_8bit(), large: false))
            {
                Assert.Equal(new Size(173, 183), smallImage.Size);

                using (Image largeImage = attribute.GetImage(new bitmap_173x183_indexed_8bit(), large: true))
                {
                    Assert.Equal(new Size(32, 32), largeImage.Size);
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetImage_Default_ReturnsExpected()
        {
            ToolboxBitmapAttribute attribute = ToolboxBitmapAttribute.Default;

            using (Image image = attribute.GetImage(typeof(ToolboxBitmapAttributeTests), "bitmap_173x183_indexed_8bit", large: true))
            {
                Assert.Equal(new Size(32, 32), image.Size);
            }

            using (Image image = attribute.GetImage(typeof(ToolboxBitmapAttributeTests), "bitmap_173x183_indexed_8bit", large: false))
            {
                Assert.Equal(new Size(173, 183), image.Size);
            }
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { ToolboxBitmapAttribute.Default, ToolboxBitmapAttribute.Default, true };
            yield return new object[] { ToolboxBitmapAttribute.Default, new ToolboxBitmapAttribute(typeof(ToolboxBitmapAttribute), "bitmap_173x183_indexed_8bit"), true };

            yield return new object[] { ToolboxBitmapAttribute.Default, new object(), false };
            yield return new object[] { ToolboxBitmapAttribute.Default, null, false };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(ToolboxBitmapAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }
    }
}
