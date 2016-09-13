// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class FileExtensionsAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(GetAttribute("png,jpg,jpeg,gif"), null);
            yield return new TestCase(GetAttribute(" j p. .e ..g "), "file.jpeg");
            yield return new TestCase(GetAttribute("jpeg"), "file.jpeg");
            yield return new TestCase(GetAttribute("jpeg,.,png,,jpg"), "file.jpeg");
            yield return new TestCase(GetAttribute("jpeg,.,png,,jpg"), "file.png");
            yield return new TestCase(GetAttribute("myExt, .otherExt, UPPERCASE_extension"), "myfile.myExt");
            yield return new TestCase(GetAttribute("myExt, .otherExt, UPPERCASE_extension"), "some.Other.File.otherext");
            yield return new TestCase(GetAttribute("myExt, .otherExt, UPPERCASE_extension"), "Case.Does.Not.matter.uppercase_EXTENSION");
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(GetAttribute("png,jpg,jpeg,gif"), "");
            yield return new TestCase(GetAttribute("png,jpg,jpeg,gif"), "someFile.nonContainedExtension");
            yield return new TestCase(GetAttribute("myExt, .otherExt, UPPERCASE_extension"), "someFile.nonContainedExtension");

            yield return new TestCase(GetAttribute(" "), "");
            yield return new TestCase(GetAttribute(" "), "a");
            yield return new TestCase(GetAttribute(" , "), "");
            yield return new TestCase(GetAttribute(" , "), "a");

            yield return new TestCase(GetAttribute(" . "), "");
            yield return new TestCase(GetAttribute(" . "), "a");
            yield return new TestCase(GetAttribute("."), "a.");

            yield return new TestCase(GetAttribute("png,jpg,jpeg,gif"), Path.GetInvalidPathChars()[0].ToString());

            yield return new TestCase(GetAttribute("png,jpg,jpeg,gif"), new object());
        }

        private static FileExtensionsAttribute GetAttribute(string extensions) => new FileExtensionsAttribute() { Extensions = extensions };

        [Fact]
        public static void Ctor()
        {
            var attribute = new FileExtensionsAttribute();
            Assert.Equal(DataType.Upload, attribute.DataType);
            Assert.Null(attribute.CustomDataType);

            Assert.Equal("png,jpg,jpeg,gif", attribute.Extensions);
        }

        [Theory]
        [InlineData("test1, test2, test3", "test1, test2, test3")]
        [InlineData("", "png,jpg,jpeg,gif")]
        [InlineData(null, "png,jpg,jpeg,gif")]
        public static void Extensions_Get_Set(string newValue, string expected)
        {
            var attribute = new FileExtensionsAttribute();
            attribute.Extensions = newValue;
            Assert.Equal(expected, attribute.Extensions);
        }
    }
}
