// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCCheckChars : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCCheckChars
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter || WriterType == WriterType.CharCheckingWriter)
            {
                return;
            }
            // for function checkChars_1
            {
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteString") { Param = "String", id = 1, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteEntityRef") { Param = "EntityRef", id = 5, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteSurrogateCharEntity") { Param = "SurrogateCharEntity", id = 6, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WritePI") { Param = "PI", id = 7, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteWhitespace") { Param = "Whitespace", id = 8, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteChars") { Param = "Chars", id = 9, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteRaw(String)") { Param = "RawString", id = 10, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteRaw(Chars)") { Param = "RawChars", id = 11, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteValue(string)") { Param = "WriteValue", id = 12, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid name chars WriteDocType(name)") { Param = "WriteDocTypeName", id = 13, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid name chars WriteDocType(sysid)") { Param = "WriteDocTypeSysid", id = 14, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid name chars WriteDocType(pubid)") { Param = "WriteDocTypePubid", id = 15, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteCData") { Param = "CData", id = 2, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteComment") { Param = "Comment", id = 3, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_1) { Attribute = new Variation("CheckChars=true, invalid XML test WriteCharEntity") { Param = "CharEntity", id = 4, Pri = 1 } });
            }


            // for function checkChars_2
            {
                this.AddChild(new CVariation(checkChars_2) { Attribute = new Variation("CheckChars=false, invalid XML characters should be escaped, WriteValue(string)") { Param = "WriteValue", id = 23, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_2) { Attribute = new Variation("CheckChars=false, invalid XML characters should be escaped, WriteCharEntity") { Param = "CharEntity", id = 21, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_2) { Attribute = new Variation("CheckChars=false, invalid XML characters should be escaped, WriteChars") { Param = "Chars", id = 22, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_2) { Attribute = new Variation("CheckChars=false, invalid XML characters should be escaped, WriteString") { Param = "String", id = 20, Pri = 1 } });
            }


            // for function checkChars_3
            {
                this.AddChild(new CVariation(checkChars_3) { Attribute = new Variation("CheckChars=false, invalid XML characters in WritePI are not escaped") { Param = "PI", id = 32, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_3) { Attribute = new Variation("CheckChars=false, invalid XML characters in WriteRaw(String) are not escaped") { Param = "RawString", id = 34, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_3) { Attribute = new Variation("CheckChars=false, invalid XML characters in WriteRaw(Chars) are not escaped") { Param = "RawChars", id = 35, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_3) { Attribute = new Variation("CheckChars=false, invalid XML characters in WriteComment are not escaped") { Param = "Comment", id = 30, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_3) { Attribute = new Variation("CheckChars=false, invalid XML characters in WriteCData are not escaped") { Param = "CData", id = 33, Pri = 1 } });
            }


            // for function checkChars_4
            {
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=false, invalid XML characters in WriteEntityRef should error") { Params = new object[] { "EntityRef", false }, id = 42, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=true, invalid XML characters in WriteEntityRef should error") { Params = new object[] { "EntityRef", true }, id = 48, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=true, invalid XML characters in WriteName should error") { Params = new object[] { "Name", true }, id = 49, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=true, invalid XML characters in WriteNmToken should error") { Params = new object[] { "NmToken", true }, id = 50, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=true, invalid XML characters in WriteQualifiedName should error") { Params = new object[] { "QName", true }, id = 51, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=false, invalid XML characters in WriteSurrogateCharEntity should error") { Params = new object[] { "Surrogate", false }, id = 41, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=false, invalid XML characters in WriteWhitespace should error") { Params = new object[] { "Whitespace", false }, id = 40, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=false, invalid XML characters in WriteName should error") { Params = new object[] { "Name", false }, id = 43, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=false, invalid XML characters in WriteNmToken should error") { Params = new object[] { "NmToken", false }, id = 44, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=false, invalid XML characters in WriteQualifiedName should error") { Params = new object[] { "QName", false }, id = 45, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=true, invalid XML characters in WriteWhitespace should error") { Params = new object[] { "Whitespace", true }, id = 46, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_4) { Attribute = new Variation("CheckChars=true, invalid XML characters in WriteSurrogateCharEntity should error") { Params = new object[] { "Surrogate", true }, id = 47, Pri = 1 } });
            }


            // for function checkChars_7
            {
                this.AddChild(new CVariation(checkChars_7) { Attribute = new Variation("CheckChars = true and IndentChars contains invalid surrogate char") { Param = "~surogate~", id = 83, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_7) { Attribute = new Variation("CheckChars = true and IndentChars contains <") { Param = "<", id = 80, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_7) { Attribute = new Variation("CheckChars = true and IndentChars contains &") { Param = "&", id = 81, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_7) { Attribute = new Variation("CheckChars = true and IndentChars contains ]]>") { Param = "]]>", id = 82, Pri = 1 } });
            }


            // for function checkChars_8
            {
                this.AddChild(new CVariation(checkChars_8) { Attribute = new Variation("CheckChars = true and NewLineChars contains <") { Param = "<", id = 90, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_8) { Attribute = new Variation("CheckChars = true and NewLineChars contains ]]>") { Param = "]]>", id = 92, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_8) { Attribute = new Variation("CheckChars = true and NewLineChars contains &") { Param = "&", id = 91, Pri = 1 } });
                this.AddChild(new CVariation(checkChars_8) { Attribute = new Variation("CheckChars = true and NewLineChars contains invalid surrogate char") { Param = "~surogate~", id = 93, Pri = 1 } });
            }


            // for function checkChars_9
            {
                this.AddChild(new CVariation(checkChars_9) { Attribute = new Variation("CheckChars = true, NewLineOnAttributes = true and IndentChars contains non-whitespace char") { id = 99, Pri = 1 } });
            }
        }
    }
}
