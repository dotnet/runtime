// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class ProductInfoHeaderValueTest
    {
        [Fact]
        public void Ctor_ProductOverload_MatchExpectation()
        {
            ProductInfoHeaderValue productInfo = new ProductInfoHeaderValue(new ProductHeaderValue("product"));
            Assert.Equal(new ProductHeaderValue("product"), productInfo.Product);
            Assert.Null(productInfo.Comment);

            ProductHeaderValue input = null;
            Assert.Throws<ArgumentNullException>(() => { new ProductInfoHeaderValue(input); });
        }

        [Fact]
        public void Ctor_ProductStringOverload_MatchExpectation()
        {
            ProductInfoHeaderValue productInfo = new ProductInfoHeaderValue("product", "1.0");
            Assert.Equal(new ProductHeaderValue("product", "1.0"), productInfo.Product);
            Assert.Null(productInfo.Comment);
        }

        [Fact]
        public void Ctor_CommentOverload_MatchExpectation()
        {
            ProductInfoHeaderValue productInfo = new ProductInfoHeaderValue("(this is a comment)");
            Assert.Null(productInfo.Product);
            Assert.Equal("(this is a comment)", productInfo.Comment);

            AssertExtensions.Throws<ArgumentNullException>("comment", () => { new ProductInfoHeaderValue((string)null); });
            Assert.Throws<FormatException>(() => { new ProductInfoHeaderValue("invalid comment"); });
            Assert.Throws<FormatException>(() => { new ProductInfoHeaderValue(" (leading space)"); });
            Assert.Throws<FormatException>(() => { new ProductInfoHeaderValue("(trailing space) "); });
        }

        [Fact]
        public void ToString_UseDifferentProductInfos_AllSerializedCorrectly()
        {
            ProductInfoHeaderValue productInfo = new ProductInfoHeaderValue("product", "1.0");
            Assert.Equal("product/1.0", productInfo.ToString());

            productInfo = new ProductInfoHeaderValue("(comment)");
            Assert.Equal("(comment)", productInfo.ToString());
        }

        [Fact]
        public void ToString_Aggregate_AllSerializedCorrectly()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            string input = string.Empty;

            ProductInfoHeaderValue productInfo = new ProductInfoHeaderValue("product", "1.0");
            Assert.Equal("product/1.0", productInfo.ToString());

            input += productInfo.ToString();
            request.Headers.UserAgent.Add(productInfo);

            productInfo = new ProductInfoHeaderValue("(comment)");
            Assert.Equal("(comment)", productInfo.ToString());

            input += " " + productInfo.ToString(); // Space delineated
            request.Headers.UserAgent.Add(productInfo);

            Assert.Equal(input, request.Headers.UserAgent.ToString());
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentProductInfos_SameOrDifferentHashCodes()
        {
            ProductInfoHeaderValue productInfo1 = new ProductInfoHeaderValue("product", "1.0");
            ProductInfoHeaderValue productInfo2 = new ProductInfoHeaderValue(new ProductHeaderValue("product", "1.0"));
            ProductInfoHeaderValue productInfo3 = new ProductInfoHeaderValue("(comment)");
            ProductInfoHeaderValue productInfo4 = new ProductInfoHeaderValue("(COMMENT)");

            Assert.Equal(productInfo1.GetHashCode(), productInfo2.GetHashCode());
            Assert.NotEqual(productInfo1.GetHashCode(), productInfo3.GetHashCode());
            Assert.NotEqual(productInfo3.GetHashCode(), productInfo4.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
        {
            ProductInfoHeaderValue productInfo1 = new ProductInfoHeaderValue("product", "1.0");
            ProductInfoHeaderValue productInfo2 = new ProductInfoHeaderValue(new ProductHeaderValue("product", "1.0"));
            ProductInfoHeaderValue productInfo3 = new ProductInfoHeaderValue("(comment)");
            ProductInfoHeaderValue productInfo4 = new ProductInfoHeaderValue("(COMMENT)");

            Assert.False(productInfo1.Equals(null), "product/1.0 vs. <null>");
            Assert.True(productInfo1.Equals(productInfo2), "product/1.0 vs. product/1.0");
            Assert.False(productInfo1.Equals(productInfo3), "product/1.0 vs. (comment)");
            Assert.False(productInfo3.Equals(productInfo4), "(comment) vs. (COMMENT)");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            ProductInfoHeaderValue source = new ProductInfoHeaderValue("product", "1.0");
            ProductInfoHeaderValue clone = (ProductInfoHeaderValue)((ICloneable)source).Clone();

            Assert.Equal(source.Product, clone.Product);
            Assert.Null(clone.Comment);

            source = new ProductInfoHeaderValue("(comment)");
            clone = (ProductInfoHeaderValue)((ICloneable)source).Clone();

            Assert.Null(clone.Product);
            Assert.Equal(source.Comment, clone.Comment);
        }

        [Fact]
        public void GetProductInfoLength_DifferentValidScenarios_AllReturnNonZero()
        {
            ProductInfoHeaderValue result = null;

            CallGetProductInfoLength(" product / 1.0 ", 1, 14, out result);
            Assert.Equal(new ProductHeaderValue("product", "1.0"), result.Product);
            Assert.Null(result.Comment);

            CallGetProductInfoLength("p/1.0", 0, 5, out result);
            Assert.Equal(new ProductHeaderValue("p", "1.0"), result.Product);
            Assert.Null(result.Comment);

            CallGetProductInfoLength(" (this is a comment)  , ", 1, 21, out result);
            Assert.Null(result.Product);
            Assert.Equal("(this is a comment)", result.Comment);

            CallGetProductInfoLength("(c)", 0, 3, out result);
            Assert.Null(result.Product);
            Assert.Equal("(c)", result.Comment);

            CallGetProductInfoLength("(comment/1.0)[", 0, 13, out result);
            Assert.Null(result.Product);
            Assert.Equal("(comment/1.0)", result.Comment);
        }

        [Fact]
        public void GetRangeLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetProductInfoLength(" p/1.0", 0); // no leading whitespace allowed
            CheckInvalidGetProductInfoLength(" (c)", 0); // no leading whitespace allowed
            CheckInvalidGetProductInfoLength("(invalid", 0);
            CheckInvalidGetProductInfoLength("product/", 0);
            CheckInvalidGetProductInfoLength("product/(1.0)", 0);

            CheckInvalidGetProductInfoLength("", 0);
            CheckInvalidGetProductInfoLength(null, 0);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParse("product", new ProductInfoHeaderValue("product", null));
            CheckValidParse(" product ", new ProductInfoHeaderValue("product", null));

            CheckValidParse(" (comment)   ", new ProductInfoHeaderValue("(comment)"));

            CheckValidParse(" Mozilla/5.0 ", new ProductInfoHeaderValue("Mozilla", "5.0"));
            CheckValidParse(" (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0) ",
                new ProductInfoHeaderValue("(compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)"));
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("p/1.0,");
            CheckInvalidParse("p/1.0\r\n"); // for \r\n to be a valid whitespace, it must be followed by space/tab
            CheckInvalidParse("p/1.0(comment)");
            CheckInvalidParse("(comment)[");

            CheckInvalidParse(" Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0) ");
            CheckInvalidParse("p/1.0 =");

            // "User-Agent" and "Server" don't allow empty values (unlike most other headers supporting lists of values)
            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
            CheckInvalidParse("  ");
            CheckInvalidParse("\t");
        }

        #region Helper methods

        private void CheckValidParse(string input, ProductInfoHeaderValue expectedResult)
        {
            ProductInfoHeaderValue result = ProductInfoHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(ProductInfoHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { ProductInfoHeaderValue.Parse(input); });

            Assert.False(ProductInfoHeaderValue.TryParse(input, out ProductInfoHeaderValue result));
            Assert.Null(result);
        }

        private static void CallGetProductInfoLength(string input, int startIndex, int expectedLength,
            out ProductInfoHeaderValue result)
        {
            Assert.Equal(expectedLength, ProductInfoHeaderValue.GetProductInfoLength(input, startIndex, out result));
        }

        private static void CheckInvalidGetProductInfoLength(string input, int startIndex)
        {
            ProductInfoHeaderValue result = null;
            Assert.Equal(0, ProductInfoHeaderValue.GetProductInfoLength(input, startIndex, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
