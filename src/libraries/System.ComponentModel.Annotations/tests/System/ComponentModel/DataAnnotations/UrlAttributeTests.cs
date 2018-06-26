// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class UrlAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new UrlAttribute(), null);
            yield return new TestCase(new UrlAttribute(), "http://foo.bar");
            yield return new TestCase(new UrlAttribute(), "https://foo.bar");
            yield return new TestCase(new UrlAttribute(), "ftp://foo.bar");
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new UrlAttribute(), "file:///foo.bar");
            yield return new TestCase(new UrlAttribute(), "foo.png");
            yield return new TestCase(new UrlAttribute(), new object());
        }

        [Fact]
        public static void DataType_CustomDataType_ReturnsExpected()
        {
            var attribute = new UrlAttribute();
            Assert.Equal(DataType.Url, attribute.DataType);
            Assert.Null(attribute.CustomDataType);
        }
    }
}
