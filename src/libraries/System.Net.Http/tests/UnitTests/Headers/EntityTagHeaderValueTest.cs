// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using Xunit;

namespace System.Net.Http.Tests
{
    public class EntityTagHeaderValueTest
    {
        [Fact]
        public void Ctor_ETagNull_Throw()
        {
            AssertExtensions.Throws<ArgumentException>("tag", () => { new EntityTagHeaderValue(null); });
        }

        [Fact]
        public void Ctor_ETagEmpty_Throw()
        {
            // null and empty should be treated the same. So we also throw for empty strings.
            AssertExtensions.Throws<ArgumentException>("tag", () => { new EntityTagHeaderValue(string.Empty); });
        }

        [Fact]
        public void Ctor_ETagInvalidFormat_ThrowFormatException()
        {
            // When adding values using strongly typed objects, no leading/trailing LWS (whitespace) are allowed.
            AssertFormatException("tag");
            AssertFormatException("*");
            AssertFormatException(" tag ");
            AssertFormatException("\"tag\" invalid");
            AssertFormatException("\"tag");
            AssertFormatException("tag\"");
            AssertFormatException("\"tag\"\"");
            AssertFormatException("\"\"tag\"\"");
            AssertFormatException("\"\"tag\"");
            AssertFormatException("W/\"tag\"");
        }

        [Fact]
        public void Ctor_ETagValidFormat_SuccessfullyCreated()
        {
            EntityTagHeaderValue etag = new EntityTagHeaderValue("\"tag\"");
            Assert.Equal("\"tag\"", etag.Tag);
            Assert.False(etag.IsWeak);
        }

        [Fact]
        public void Ctor_ETagValidFormatAndIsWeak_SuccessfullyCreated()
        {
            EntityTagHeaderValue etag = new EntityTagHeaderValue("\"e tag\"", true);
            Assert.Equal("\"e tag\"", etag.Tag);
            Assert.True(etag.IsWeak);
        }

        [Fact]
        public void ToString_UseDifferentETags_AllSerializedCorrectly()
        {
            EntityTagHeaderValue etag = new EntityTagHeaderValue("\"e tag\"");
            Assert.Equal("\"e tag\"", etag.ToString());

            etag = new EntityTagHeaderValue("\"e tag\"", true);
            Assert.Equal("W/\"e tag\"", etag.ToString());

            etag = new EntityTagHeaderValue("\"\"", false);
            Assert.Equal("\"\"", etag.ToString());
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentETags_SameOrDifferentHashCodes()
        {
            EntityTagHeaderValue etag1 = new EntityTagHeaderValue("\"tag\"");
            EntityTagHeaderValue etag2 = new EntityTagHeaderValue("\"TAG\"");
            EntityTagHeaderValue etag3 = new EntityTagHeaderValue("\"tag\"", true);
            EntityTagHeaderValue etag4 = new EntityTagHeaderValue("\"tag1\"");
            EntityTagHeaderValue etag5 = new EntityTagHeaderValue("\"tag\"");
            EntityTagHeaderValue etag6 = EntityTagHeaderValue.Any;

            Assert.NotEqual(etag1.GetHashCode(), etag2.GetHashCode());
            Assert.NotEqual(etag1.GetHashCode(), etag3.GetHashCode());
            Assert.NotEqual(etag1.GetHashCode(), etag4.GetHashCode());
            Assert.NotEqual(etag1.GetHashCode(), etag6.GetHashCode());
            Assert.Equal(etag1.GetHashCode(), etag5.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentETags_EqualOrNotEqualNoExceptions()
        {
            EntityTagHeaderValue etag1 = new EntityTagHeaderValue("\"tag\"");
            EntityTagHeaderValue etag2 = new EntityTagHeaderValue("\"TAG\"");
            EntityTagHeaderValue etag3 = new EntityTagHeaderValue("\"tag\"", true);
            EntityTagHeaderValue etag4 = new EntityTagHeaderValue("\"tag1\"");
            EntityTagHeaderValue etag5 = new EntityTagHeaderValue("\"tag\"");
            EntityTagHeaderValue etag6 = EntityTagHeaderValue.Any;

            Assert.False(etag1.Equals(etag2));
            Assert.False(etag2.Equals(etag1));
            Assert.False(etag1.Equals(null));
            Assert.False(etag1.Equals(etag3));
            Assert.False(etag3.Equals(etag1));
            Assert.False(etag1.Equals(etag4));
            Assert.False(etag1.Equals(etag6));
            Assert.True(etag1.Equals(etag5));
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            EntityTagHeaderValue source = new EntityTagHeaderValue("\"tag\"");
            EntityTagHeaderValue clone = (EntityTagHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Tag, clone.Tag);
            Assert.Equal(source.IsWeak, clone.IsWeak);

            source = new EntityTagHeaderValue("\"tag\"", true);
            clone = (EntityTagHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Tag, clone.Tag);
            Assert.Equal(source.IsWeak, clone.IsWeak);

            Assert.Same(EntityTagHeaderValue.Any, ((ICloneable)EntityTagHeaderValue.Any).Clone());
        }

        [Fact]
        public void GetEntityTagLength_DifferentValidScenarios_AllReturnNonZero()
        {
            EntityTagHeaderValue result = null;

            Assert.Equal(6, EntityTagHeaderValue.GetEntityTagLength("\"ta\u4F1Ag\"", 0, out result));
            Assert.Equal("\"ta\u4F1Ag\"", result.Tag);
            Assert.False(result.IsWeak);

            Assert.Equal(9, EntityTagHeaderValue.GetEntityTagLength("W/\"tag\"  ", 0, out result));
            Assert.Equal("\"tag\"", result.Tag);
            Assert.True(result.IsWeak);

            // Note that even if after a valid tag & whitespace there are invalid characters, GetEntityTagLength()
            // will return the length of the valid tag and ignore the invalid characters at the end. It is the callers
            // responsibility to consider the whole string invalid if after a valid ETag there are invalid chars.
            Assert.Equal(11, EntityTagHeaderValue.GetEntityTagLength("\"tag\"  \r\n  !!", 0, out result));
            Assert.Equal("\"tag\"", result.Tag);
            Assert.False(result.IsWeak);

            Assert.Equal(7, EntityTagHeaderValue.GetEntityTagLength("\"W/tag\"", 0, out result));
            Assert.Equal("\"W/tag\"", result.Tag);
            Assert.False(result.IsWeak);

            Assert.Equal(9, EntityTagHeaderValue.GetEntityTagLength("W/  \"tag\"", 0, out result));
            Assert.Equal("\"tag\"", result.Tag);
            Assert.True(result.IsWeak);

            // We also accept lower-case 'w': e.g. 'w/"tag"' rather than 'W/"tag"'
            Assert.Equal(4, EntityTagHeaderValue.GetEntityTagLength("w/\"\"", 0, out result));
            Assert.Equal("\"\"", result.Tag);
            Assert.True(result.IsWeak);

            Assert.Equal(2, EntityTagHeaderValue.GetEntityTagLength("\"\"", 0, out result));
            Assert.Equal("\"\"", result.Tag);
            Assert.False(result.IsWeak);

            Assert.Equal(2, EntityTagHeaderValue.GetEntityTagLength(",* , ", 1, out result));
            Assert.Same(EntityTagHeaderValue.Any, result);
        }

        [Fact]
        public void GetEntityTagLength_DifferentInvalidScenarios_AllReturnZero()
        {
            EntityTagHeaderValue result = null;

            // no leading spaces allowed.
            Assert.Equal(0, EntityTagHeaderValue.GetEntityTagLength(" \"tag\"", 0, out result));
            Assert.Null(result);
            Assert.Equal(0, EntityTagHeaderValue.GetEntityTagLength("\"tag", 0, out result));
            Assert.Null(result);
            Assert.Equal(0, EntityTagHeaderValue.GetEntityTagLength("tag\"", 0, out result));
            Assert.Null(result);
            Assert.Equal(0, EntityTagHeaderValue.GetEntityTagLength("a/\"tag\"", 0, out result));
            Assert.Null(result);
            Assert.Equal(0, EntityTagHeaderValue.GetEntityTagLength("W//\"tag\"", 0, out result));
            Assert.Null(result);
            Assert.Equal(0, EntityTagHeaderValue.GetEntityTagLength("W", 0, out result));
            Assert.Null(result);
            Assert.Equal(0, EntityTagHeaderValue.GetEntityTagLength("W/", 0, out result));
            Assert.Null(result);
            Assert.Equal(0, EntityTagHeaderValue.GetEntityTagLength("W/\"", 0, out result));
            Assert.Null(result);
            Assert.Equal(0, EntityTagHeaderValue.GetEntityTagLength(null, 0, out result));
            Assert.Null(result);
            Assert.Equal(0, EntityTagHeaderValue.GetEntityTagLength(string.Empty, 0, out result));
            Assert.Null(result);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParse("\"tag\"", new EntityTagHeaderValue("\"tag\""));
            CheckValidParse(" \"tag\" ", new EntityTagHeaderValue("\"tag\""));
            CheckValidParse("\r\n \"tag\"\r\n ", new EntityTagHeaderValue("\"tag\""));
            CheckValidParse("\"tag\"", new EntityTagHeaderValue("\"tag\""));
            CheckValidParse("\"tag\u4F1A\"", new EntityTagHeaderValue("\"tag\u4F1A\""));
            CheckValidParse("W/\"tag\"", new EntityTagHeaderValue("\"tag\"", true));
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
            CheckInvalidParse("  ");
            CheckInvalidParse("  !");
            CheckInvalidParse("tag\"  !");
            CheckInvalidParse("!\"tag\"");
            CheckInvalidParse("\"tag\",");
            CheckInvalidParse("\"tag\" \"tag2\"");
            CheckInvalidParse("/\"tag\"");
            CheckInvalidParse("*"); // "any" is not allowed as ETag value.
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidTryParse("\"tag\"", new EntityTagHeaderValue("\"tag\""));
            CheckValidTryParse(" \"tag\" ", new EntityTagHeaderValue("\"tag\""));
            CheckValidTryParse("\r\n \"tag\"\r\n ", new EntityTagHeaderValue("\"tag\""));
            CheckValidTryParse("\"tag\"", new EntityTagHeaderValue("\"tag\""));
            CheckValidTryParse("\"tag\u4F1A\"", new EntityTagHeaderValue("\"tag\u4F1A\""));
            CheckValidTryParse("W/\"tag\"", new EntityTagHeaderValue("\"tag\"", true));
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidTryParse(null);
            CheckInvalidTryParse(string.Empty);
            CheckInvalidTryParse("  ");
            CheckInvalidTryParse("  !");
            CheckInvalidTryParse("tag\"  !");
            CheckInvalidTryParse("!\"tag\"");
            CheckInvalidTryParse("\"tag\",");
            CheckInvalidTryParse("\"tag\" \"tag2\"");
            CheckInvalidTryParse("/\"tag\"");
            CheckInvalidTryParse("*"); // "any" is not allowed as ETag value.
        }

        #region Helper methods

        private void CheckValidParse(string input, EntityTagHeaderValue expectedResult)
        {
            EntityTagHeaderValue result = EntityTagHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { EntityTagHeaderValue.Parse(input); });
        }

        private void CheckValidTryParse(string input, EntityTagHeaderValue expectedResult)
        {
            EntityTagHeaderValue result = null;
            Assert.True(EntityTagHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidTryParse(string input)
        {
            EntityTagHeaderValue result = null;
            Assert.False(EntityTagHeaderValue.TryParse(input, out result));
            Assert.Null(result);
        }

        private static void AssertFormatException(string tag)
        {
            Assert.Throws<FormatException>(() => { new EntityTagHeaderValue(tag); });
        }
        #endregion
    }
}
