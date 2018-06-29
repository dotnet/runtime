// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Text.Encodings.Tests
{
    public class EncodingMiscTests
    {
        public static IEnumerable<object[]> Encoding_TestData()
        {
            //                          CodePage   Name         BodyName      HeaderName    IsBrowserDisplay IsBrowserSave  IsMailNewsDisplay   IsMailNewsSave WindowsCodePage            
            yield return new object[] { 20127,    "us-ascii",   "us-ascii",   "us-ascii",   false,            false,          true,               true,           1252 };
            yield return new object[] { 28591,    "iso-8859-1", "iso-8859-1", "iso-8859-1", true,             true,           true,               true,           1252 };
            yield return new object[] { 65000,    "utf-7",      "utf-7",      "utf-7",      false,            false,          true,               true,           1200 };
            yield return new object[] { 65001,    "utf-8",      "utf-8",      "utf-8",      true,             true,           true,               true,           1200 };
            yield return new object[] { 1200,     "utf-16",     "utf-16",     "utf-16",     false,            true,           false,              false,          1200 };
            yield return new object[] { 1201,     "utf-16BE",   "utf-16BE",   "utf-16BE",   false,            false,          false,              false,          1200 };
            yield return new object[] { 12000,    "utf-32",     "utf-32",     "utf-32",     false,            false,          false,              false,          1200 };
            yield return new object[] { 12001,    "utf-32BE",   "utf-32BE",   "utf-32BE",   false,            false,          false,              false,          1200 };
        }

        public static IEnumerable<object[]> Normalization_TestData()
        {
            //                                          codepage isNormalized   IsNormalized(FormC)     IsNormalized(FormD)     IsNormalized(FormKC)    IsNormalized(FormKD)
            /* us-ascii   */ yield return new object[] { 20127,     false,          false,                      false,              false,                  false };
            /* iso-8859-1 */ yield return new object[] { 28591,     true,           true,                       false,              false,                  false };
            /* utf-7      */ yield return new object[] { 65000,     false,          false,                      false,              false,                  false };
            /* utf-8      */ yield return new object[] { 65001,     false,          false,                      false,              false,                  false };
            /* utf-16     */ yield return new object[] { 1200,      false,          false,                      false,              false,                  false };
            /* utf-16BE   */ yield return new object[] { 1201,      false,          false,                      false,              false,                  false };
            /* utf-32     */ yield return new object[] { 12000,     false,          false,                      false,              false,                  false };
            /* utf-32BE   */ yield return new object[] { 12001,     false,          false,                      false,              false,                  false };
        }

        [Fact]
        public static void DefaultEncodingTest()
        {
            Encoding enc = (Encoding) Encoding.Default.Clone();
            Assert.Equal(enc.WebName, Encoding.Default.WebName);
            Assert.Equal(enc.GetBytes("Some string"), Encoding.Default.GetBytes("Some string"));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework uses system ACP and not UTF8")]
        public static void DefaultEncodingBOMTest()
        {
            UTF8Encoding defaultEncoding = Encoding.Default as UTF8Encoding;
            Assert.True(defaultEncoding != null);
            Assert.Equal(0, defaultEncoding.GetPreamble().Length);
        }

        [Fact]
        public static void GetEncodingsTest()
        {
            EncodingInfo [] encodingList = Encoding.GetEncodings();
            foreach (var info in encodingList)
            {
                Encoding encoding = Encoding.GetEncoding(info.CodePage);
                Assert.Equal(encoding, info.GetEncoding());
                Assert.Equal(encoding.WebName, info.Name);
                Assert.False(string.IsNullOrEmpty(info.DisplayName));
            }
        }

        [Theory]
        [MemberData(nameof(Encoding_TestData))]
        public static void NormalizationTest(int codepage, string name, string bodyName, string headerName, bool isBrowserDisplay, 
                                            bool isBrowserSave, bool isMailNewsDisplay, bool isMailNewsSave, int windowsCodePage)
        {
            Encoding encoding = Encoding.GetEncoding(codepage);
            Assert.Equal(name, encoding.WebName);
            Assert.Equal(bodyName, encoding.BodyName);
            Assert.Equal(headerName, encoding.HeaderName);
            Assert.Equal(isBrowserDisplay, encoding.IsBrowserDisplay);
            Assert.Equal(isBrowserSave, encoding.IsBrowserSave);
            Assert.Equal(isMailNewsDisplay, encoding.IsMailNewsDisplay);
            Assert.Equal(isMailNewsSave, encoding.IsMailNewsSave);
            Assert.Equal(windowsCodePage, encoding.WindowsCodePage);
        }

        [Theory]
        [MemberData(nameof(Normalization_TestData))]
        public static void NormalizationTest(int codepage, bool normalized, bool normalizedC, bool normalizedD, bool normalizedKC, bool normalizedKD)
        {
            Encoding encoding = Encoding.GetEncoding(codepage);
            Assert.True(encoding.IsReadOnly);
            Assert.Equal(normalized,  encoding.IsAlwaysNormalized());
            Assert.Equal(normalizedC, encoding.IsAlwaysNormalized(NormalizationForm.FormC));
            Assert.Equal(normalizedD, encoding.IsAlwaysNormalized(NormalizationForm.FormD));
            Assert.Equal(normalizedKC, encoding.IsAlwaysNormalized(NormalizationForm.FormKC));
            Assert.Equal(normalizedKD, encoding.IsAlwaysNormalized(NormalizationForm.FormKD));
        }
    }
}
