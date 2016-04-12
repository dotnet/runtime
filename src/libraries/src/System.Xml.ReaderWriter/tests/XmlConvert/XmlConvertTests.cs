// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlConvertTests : CTestModule
    {
        [Fact]
        [OuterLoop]
        public static void EncodeDecodeTests()
        {
            RunTestCase(new EncodeDecodeTests { Attribute = new TestCase { Name = "EncodeName/DecodeName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void MiscellaneousTests()
        {
            RunTestCase(new MiscellaneousTests { Attribute = new TestCase { Name = "Misc. Bug Regressions", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void SqlXmlConvertTests0()
        {
            RunTestCase(new SqlXmlConvertTests0 { Attribute = new TestCase { Name = "2. XmlConvert (SQL-XML EncodeName) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void SqlXmlConvertTests1()
        {
            RunTestCase(new SqlXmlConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (SQL-XML EncodeName) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void SqlXmlConvertTests2()
        {
            RunTestCase(new SqlXmlConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert (SQL-XML EncodeName) EncodeName-DecodeName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void SqlXmlConvertTests3()
        {
            RunTestCase(new SqlXmlConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (SQL-XML EncodeName) EncodeLocalName only", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void ToTypeTests()
        {
            RunTestCase(new ToTypeTests { Attribute = new TestCase { Name = "XmlConvert type conversion functions", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void VerifyNameTests1()
        {
            RunTestCase(new VerifyNameTests1 { Attribute = new TestCase { Name = "VerifyName,VerifyNCName,VerifyNMTOKEN,VerifyXmlChar,VerifyWhitespace,VerifyPublicId", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void VerifyNameTests2()
        {
            RunTestCase(new VerifyNameTests2 { Attribute = new TestCase { Name = "VerifyName,VerifyNCName,VerifyNMTOKEN,VerifyXmlChar,VerifyWhitespace,VerifyPublicId", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void VerifyNameTests3()
        {
            RunTestCase(new VerifyNameTests3 { Attribute = new TestCase { Name = "VerifyName,VerifyNCName,VerifyNMTOKEN,VerifyXmlChar,VerifyWhitespace,VerifyPublicId", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void VerifyNameTests4()
        {
            RunTestCase(new VerifyNameTests4 { Attribute = new TestCase { Name = "VerifyName,VerifyNCName,VerifyNMTOKEN,VerifyXmlChar,VerifyWhitespace,VerifyPublicId", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void VerifyNameTests5()
        {
            RunTestCase(new VerifyNameTests5 { Attribute = new TestCase { Name = "VerifyName,VerifyNCName,VerifyNMTOKEN,VerifyXmlChar,VerifyWhitespace,VerifyPublicId", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlBaseCharConvertTests1()
        {
            RunTestCase(new XmlBaseCharConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (Boundary Base Char) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlBaseCharConvertTests2()
        {
            RunTestCase(new XmlBaseCharConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert (Boundary Base Char) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlBaseCharConvertTests3()
        {
            RunTestCase(new XmlBaseCharConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (Boundary Base Char) EncodeName-DecodeName ", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlCombiningCharConvertTests1()
        {
            RunTestCase(new XmlCombiningCharConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (Boundary Combining Char) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlCombiningCharConvertTests2()
        {
            RunTestCase(new XmlCombiningCharConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert (Boundary Combining Char) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlCombiningCharConvertTests3()
        {
            RunTestCase(new XmlCombiningCharConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (Boundary Combining Char) EncodeName-DecodeName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlDigitCharConvertTests1()
        {
            RunTestCase(new XmlDigitCharConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (Boundary Digit Char) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlDigitCharConvertTests2()
        {
            RunTestCase(new XmlDigitCharConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert (Boundary Digit Char) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlDigitCharConvertTests3()
        {
            RunTestCase(new XmlDigitCharConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (Boundary Digit Char) EncodeName-DecodeName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlEmbeddedNullCharConvertTests1()
        {
            RunTestCase(new XmlEmbeddedNullCharConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (EmbeddedNull Char) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlEmbeddedNullCharConvertTests2()
        {
            RunTestCase(new XmlEmbeddedNullCharConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert (EmbeddedNull Char) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlEmbeddedNullCharConvertTests3()
        {
            RunTestCase(new XmlEmbeddedNullCharConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (EmbeddedNull Char) EncodeName-DecodeName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlIdeographicCharConvertTests1()
        {
            RunTestCase(new XmlIdeographicCharConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (Boundary Ideographic Char) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlIdeographicCharConvertTests2()
        {
            RunTestCase(new XmlIdeographicCharConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert  (Boundary Ideographic Char) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
        }

        [Fact]
        [OuterLoop]
        public static void XmlIdeographicCharConvertTests3()
        {
            RunTestCase(new XmlIdeographicCharConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (Boundary Ideographic Char) EncodeName-DecodeName", Desc = "XmlConvert" } });
        }

        private static void RunTestCase(CTestBase testCase)
        {
            var module = new XmlConvertTests();

            module.Init(null);
            module.AddChild(testCase);
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }
    }
}
