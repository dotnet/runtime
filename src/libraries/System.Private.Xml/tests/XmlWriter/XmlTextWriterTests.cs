// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Xml.XmlWriterTests
{
    public class XmlTextWriterTests
    {
        public static IEnumerable<object[]> PositiveTestCases
        {
            get
            {
                yield return new string[] { null }; // will be normalized to empty string
                yield return new string[] { "" };
                yield return new string[] { "This is some data." };
                yield return new string[] { " << brackets and whitespace >> " }; // brackets & surrounding whitespace are ok
                yield return new string[] { "&amp;" }; // entities are ok (treated opaquely)
                yield return new string[] { "Hello\r\nthere." }; // newlines are ok
                yield return new string[] { "\U0001F643 Upside-down smiley \U0001F643" }; // correctly paired surrogates are ok
                yield return new string[] { "\uFFFD\uFFFE\uFFFF" }; // replacement char & private use are ok
            }
        }

        public static IEnumerable<object[]> BadSurrogateTestCases
        {
            get
            {
                yield return new string[] { "\uD800 Unpaired high surrogate." };
                yield return new string[] { "\uDFFF Unpaired low surrogate." };
                yield return new string[] { "Unpaired high surrogate at end. \uD800" };
                yield return new string[] { "Unpaired low surrogate at end. \uDFFF" };
                yield return new string[] { "Unpaired surrogates \uDFFF\uD800 in middle." };
            }
        }

        [Theory]
        [MemberData(nameof(PositiveTestCases))]
        [InlineData("]]")] // ]] without trailing > is ok
        [InlineData("-->")] // end of comment marker ok (meaningless to cdata tag)
        public void WriteCData_SuccessCases(string cdataText)
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);

            xw.WriteCData(cdataText);

            Assert.Equal($"<![CDATA[{cdataText}]]>", sw.ToString());
        }

        [Theory]
        [MemberData(nameof(BadSurrogateTestCases), DisableDiscoveryEnumeration = true)] // disable enumeration to avoid test harness misinterpreting unpaired surrogates
        [InlineData("]]>")] // end of cdata marker forbidden (ambiguous close tag)
        public void WriteCData_FailureCases(string cdataText)
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);

            Assert.Throws<ArgumentException>(() => xw.WriteCData(cdataText));
        }

        [Theory]
        [MemberData(nameof(PositiveTestCases))]
        [InlineData("-12345")] // hyphen at beginning is ok
        [InlineData("123- -45")] // single hyphens are ok in middle
        [InlineData("]]>")] // end of cdata marker ok (meaningless to comment tag)
        public void WriteComment_SuccessCases(string commentText)
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);

            xw.WriteComment(commentText);

            Assert.Equal($"<!--{commentText}-->", sw.ToString());
        }

        [Theory]
        [MemberData(nameof(BadSurrogateTestCases), DisableDiscoveryEnumeration = true)] // disable enumeration to avoid test harness misinterpreting unpaired surrogates
        [InlineData("123--45")] // double-hyphen in middle is forbidden (ambiguous comment close tag)
        [InlineData("12345-")] // hyphen at end is forbidden (ambiguous comment close tag)
        [InlineData("-->")] // end of comment marker forbidden (ambiguous close tag)
        public void WriteComment_FailureCases(string commentText)
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);

            Assert.Throws<ArgumentException>(() => xw.WriteComment(commentText));
        }
    }
}
