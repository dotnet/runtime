// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using Xunit;

namespace System.Net.Http.Tests
{
    public class HeaderUtilitiesTest
    {
        [Fact]
        public void AreEqualCollections_UseSetOfEqualCollections_ReturnsTrue()
        {
            ICollection<NameValueHeaderValue> x = new List<NameValueHeaderValue>();
            ICollection<NameValueHeaderValue> y = new List<NameValueHeaderValue>();

            Assert.True(HeaderUtilities.AreEqualCollections(x, y));

            x.Add(new NameValueHeaderValue("a"));
            x.Add(new NameValueHeaderValue("c"));
            x.Add(new NameValueHeaderValue("b"));
            x.Add(new NameValueHeaderValue("c"));

            y.Add(new NameValueHeaderValue("c"));
            y.Add(new NameValueHeaderValue("c"));
            y.Add(new NameValueHeaderValue("b"));
            y.Add(new NameValueHeaderValue("a"));

            Assert.True(HeaderUtilities.AreEqualCollections(x, y));
            Assert.True(HeaderUtilities.AreEqualCollections(y, x));
        }

        [Fact]
        public void AreEqualCollections_UseSetOfNotEqualCollections_ReturnsFalse()
        {
            ICollection<NameValueHeaderValue> x = new List<NameValueHeaderValue>();
            ICollection<NameValueHeaderValue> y = new List<NameValueHeaderValue>();

            Assert.True(HeaderUtilities.AreEqualCollections(x, y), "Expected '<empty>' == '<empty>'");

            x.Add(new NameValueHeaderValue("a"));
            x.Add(new NameValueHeaderValue("c"));
            x.Add(new NameValueHeaderValue("b"));
            x.Add(new NameValueHeaderValue("c"));

            y.Add(new NameValueHeaderValue("a"));
            y.Add(new NameValueHeaderValue("b"));
            y.Add(new NameValueHeaderValue("c"));
            y.Add(new NameValueHeaderValue("d"));

            Assert.False(HeaderUtilities.AreEqualCollections(x, y));
            Assert.False(HeaderUtilities.AreEqualCollections(y, x));

            y.Clear();
            y.Add(new NameValueHeaderValue("a"));
            y.Add(new NameValueHeaderValue("b"));
            y.Add(new NameValueHeaderValue("b"));
            y.Add(new NameValueHeaderValue("c"));

            Assert.False(HeaderUtilities.AreEqualCollections(x, y));
            Assert.False(HeaderUtilities.AreEqualCollections(y, x));
        }

        [Fact]
        public void GetNextNonEmptyOrWhitespaceIndex_UseDifferentInput_MatchExpectation()
        {
            CheckGetNextNonEmptyOrWhitespaceIndex("x , , ,,  ,\t\r\n , ,x", 1, true, 18, true);
            CheckGetNextNonEmptyOrWhitespaceIndex("x , ,   ", 1, false, 4, true); // stop at the second ','
            CheckGetNextNonEmptyOrWhitespaceIndex("x , ,   ", 1, true, 8, true);
            CheckGetNextNonEmptyOrWhitespaceIndex(" x", 0, true, 1, false);
            CheckGetNextNonEmptyOrWhitespaceIndex(" ,x", 0, true, 2, true);
            CheckGetNextNonEmptyOrWhitespaceIndex(" ,x", 0, false, 2, true);
            CheckGetNextNonEmptyOrWhitespaceIndex(" ,,x", 0, true, 3, true);
            CheckGetNextNonEmptyOrWhitespaceIndex(" ,,x", 0, false, 2, true);
        }

        [Fact]
        public void CheckValidQuotedString_ValidAndInvalidvalues_MatchExpectation()
        {
            // No exception expected for the following input.
            HeaderUtilities.CheckValidQuotedString("\"x\"", "param");
            HeaderUtilities.CheckValidQuotedString("\"x y\"", "param");

            Assert.Throws<ArgumentException>(() => { HeaderUtilities.CheckValidQuotedString(null, "param"); });
            Assert.Throws<ArgumentException>(() => { HeaderUtilities.CheckValidQuotedString("", "param"); });
            Assert.Throws<FormatException>(() => { HeaderUtilities.CheckValidQuotedString("\"x", "param"); });
            Assert.Throws<FormatException>(() => { HeaderUtilities.CheckValidQuotedString("\"x\"y", "param"); });
        }

        #region Helper methods

        private static void CheckGetNextNonEmptyOrWhitespaceIndex(string input, int startIndex,
            bool supportsEmptyValues, int expectedIndex, bool expectedSeparatorFound)
        {
            bool separatorFound = false;
            Assert.Equal(expectedIndex, HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, startIndex,
                supportsEmptyValues, out separatorFound));
            Assert.Equal(expectedSeparatorFound, separatorFound);
        }
        #endregion
    }
}
