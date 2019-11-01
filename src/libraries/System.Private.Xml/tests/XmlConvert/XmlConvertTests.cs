// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlConvertTests : CTestModule
    {
        [Theory]
        [XmlTests(nameof(Create))]
        public void RunTests(XunitTestCase testCase)
        {
            testCase.Run();
        }

        public static CTestModule Create()
        {
            var module = new XmlConvertTests();

            module.Init(null);
            module.AddChild(new EncodeDecodeTests { Attribute = new TestCase { Name = "EncodeName/DecodeName", Desc = "XmlConvert" } });
            module.AddChild(new MiscellaneousTests { Attribute = new TestCase { Name = "Misc. Bug Regressions", Desc = "XmlConvert" } });
            module.AddChild(new SqlXmlConvertTests0 { Attribute = new TestCase { Name = "2. XmlConvert (SQL-XML EncodeName) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
            module.AddChild(new SqlXmlConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (SQL-XML EncodeName) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
            module.AddChild(new SqlXmlConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert (SQL-XML EncodeName) EncodeName-DecodeName", Desc = "XmlConvert" } });
            module.AddChild(new SqlXmlConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (SQL-XML EncodeName) EncodeLocalName only", Desc = "XmlConvert" } });
            module.AddChild(new ToTypeTests { Attribute = new TestCase { Name = "XmlConvert type conversion functions", Desc = "XmlConvert" } });
            module.AddChild(new VerifyNameTests1 { Attribute = new TestCase { Name = "VerifyName,VerifyNCName,VerifyNMTOKEN,VerifyXmlChar,VerifyWhitespace,VerifyPublicId", Desc = "XmlConvert" } });
            module.AddChild(new VerifyNameTests2 { Attribute = new TestCase { Name = "VerifyName,VerifyNCName,VerifyNMTOKEN,VerifyXmlChar,VerifyWhitespace,VerifyPublicId", Desc = "XmlConvert" } });
            module.AddChild(new VerifyNameTests3 { Attribute = new TestCase { Name = "VerifyName,VerifyNCName,VerifyNMTOKEN,VerifyXmlChar,VerifyWhitespace,VerifyPublicId", Desc = "XmlConvert" } });
            module.AddChild(new VerifyNameTests4 { Attribute = new TestCase { Name = "VerifyName,VerifyNCName,VerifyNMTOKEN,VerifyXmlChar,VerifyWhitespace,VerifyPublicId", Desc = "XmlConvert" } });
            module.AddChild(new VerifyNameTests5 { Attribute = new TestCase { Name = "VerifyName,VerifyNCName,VerifyNMTOKEN,VerifyXmlChar,VerifyWhitespace,VerifyPublicId", Desc = "XmlConvert" } });
            module.AddChild(new XmlBaseCharConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (Boundary Base Char) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
            module.AddChild(new XmlBaseCharConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert (Boundary Base Char) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
            module.AddChild(new XmlBaseCharConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (Boundary Base Char) EncodeName-DecodeName ", Desc = "XmlConvert" } });
            module.AddChild(new XmlCombiningCharConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (Boundary Combining Char) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
            module.AddChild(new XmlCombiningCharConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert (Boundary Combining Char) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
            module.AddChild(new XmlCombiningCharConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (Boundary Combining Char) EncodeName-DecodeName", Desc = "XmlConvert" } });
            module.AddChild(new XmlDigitCharConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (Boundary Digit Char) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
            module.AddChild(new XmlDigitCharConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert (Boundary Digit Char) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
            module.AddChild(new XmlDigitCharConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (Boundary Digit Char) EncodeName-DecodeName", Desc = "XmlConvert" } });
            module.AddChild(new XmlEmbeddedNullCharConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (EmbeddedNull Char) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
            module.AddChild(new XmlEmbeddedNullCharConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert (EmbeddedNull Char) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
            module.AddChild(new XmlEmbeddedNullCharConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (EmbeddedNull Char) EncodeName-DecodeName", Desc = "XmlConvert" } });
            module.AddChild(new XmlIdeographicCharConvertTests1 { Attribute = new TestCase { Name = "1. XmlConvert (Boundary Ideographic Char) EncodeName-EncodeLocalName", Desc = "XmlConvert" } });
            module.AddChild(new XmlIdeographicCharConvertTests2 { Attribute = new TestCase { Name = "2. XmlConvert  (Boundary Ideographic Char) EncodeNmToken-EncodeLocalNmToken", Desc = "XmlConvert" } });
            module.AddChild(new XmlIdeographicCharConvertTests3 { Attribute = new TestCase { Name = "3. XmlConvert (Boundary Ideographic Char) EncodeName-DecodeName", Desc = "XmlConvert" } });

            return module;
        }
    }
}
