// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Globalization;

namespace System.Globalization.Extensions.Tests
{
    public class GetAsciiTests
    {
        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void SimpleValidationTests()
        {
            var idn = new IdnMapping();

            Assert.Equal("xn--yda", idn.GetAscii("\u0101"));
            Assert.Equal("xn--yda", idn.GetAscii("\u0101", 0));
            Assert.Equal("xn--yda", idn.GetAscii("\u0101", 0, 1));

            Assert.Equal("xn--aa-cla", idn.GetAscii("\u0101\u0061\u0041"));
            Assert.Equal("xn--ab-dla", idn.GetAscii("\u0061\u0101\u0062"));
            Assert.Equal("xn--ab-ela", idn.GetAscii("\u0061\u0062\u0101"));
        }

        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void SurrogatePairsConsecutive()
        {
            var idn = new IdnMapping();

            Assert.Equal("xn--097ccd", idn.GetAscii("\uD800\uDF00\uD800\uDF01\uD800\uDF02"));
        }

        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void SurrogatePairsSeparatedByAscii()
        {
            var idn = new IdnMapping();

            Assert.Equal("xn--ab-ic6nfag", idn.GetAscii("\uD800\uDF00\u0061\uD800\uDF01\u0042\uD800\uDF02"));
        }

        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void SurrogatePairsSeparatedByNonAscii()
        {
            var idn = new IdnMapping();

            Assert.Equal("xn--yda263v6b6kfag", idn.GetAscii("\uD800\uDF00\u0101\uD800\uDF01\u305D\uD800\uDF02"));
        }

        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void SurrogatePairsSeparatedByAsciiAndNonAscii()
        {
            var idn = new IdnMapping();

            Assert.Equal("xn--a-nha4529qfag", idn.GetAscii("\uD800\uDF00\u0101\uD800\uDF01\u0061\uD800\uDF02"));
        }

        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void FullyQualifiedDomainNameVsIndividualLabels()
        {
            var idn = new IdnMapping();

            // ASCII only code points
            Assert.Equal("\u0061\u0062\u0063", idn.GetAscii("\u0061\u0062\u0063"));
            // non-ASCII only code points
            Assert.Equal("xn--d9juau41awczczp", idn.GetAscii("\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067"));
            // ASCII and non-ASCII code points
            Assert.Equal("xn--de-jg4avhby1noc0d", idn.GetAscii("\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0"));
            // Fully Qualified Domain Name
            Assert.Equal("abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0"));
        }

        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void EmbeddedNulls()
        {
            var idn = new IdnMapping();

            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0101\u0000"));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0101\u0000", 0));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0101\u0000", 0, 2));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0101\u0000\u0101"));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0101\u0000\u0101", 0));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0101\u0000\u0101", 0, 3));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0101\u0000\u0101\u0000"));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0101\u0000\u0101\u0000", 0));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0101\u0000\u0101\u0000", 0, 4));
        }

        /// <summary>
        /// Embedded domain name conversion (NLS+ only) (Priority 1)
        ///
        /// Per the spec [7], “The index and count parameters (when provided) allow the 
        /// conversion to be done on a larger string where the domain name is embedded 
        /// (such as a URI or IRI). The output string is only the converted FQDN or 
        /// label, not the whole input string (if the input string contains more 
        /// character than the substring to convert).” 
        ///
        /// Fully Qualified Domain Name (Label1.Label2.Label3)
        /// </summary>
        /// <remarks>
        /// An FQDN/label can NOT begin with a label separator, but may end
        /// with one.  This will cause an ArgumentException.
        /// </remarks>
        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void EmbeddedDomainNameConversion()
        {
            var idn = new IdnMapping();

            Assert.Equal("abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0));
            Assert.Equal("abc.xn--d9juau41awczczp", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 11));
            Assert.Equal("abc.xn--d9juau41awczczp.", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 12));
            Assert.Equal("abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 21));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 3));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 3, 8));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 3, 9));
            Assert.Equal("xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 4));
            Assert.Equal("xn--d9juau41awczczp", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 4, 7));
            Assert.Equal("xn--d9juau41awczczp.", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 4, 8));
            Assert.Equal("xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 4, 17));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 11));
            Assert.Throws<ArgumentException>(() => idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 11, 10));
            Assert.Equal("xn--de-jg4avhby1noc0d", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 12));
            Assert.Equal("xn--de-jg4avhby1noc0d", idn.GetAscii("\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 12, 9));
        }
    }
}
