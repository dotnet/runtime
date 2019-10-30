// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCRead2 : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCRead2
        // Test Case
        public override void AddChildren()
        {
            // for function v2
            {
                this.AddChild(new CVariation(v2) { Attribute = new Variation("Read after Close") });
            }


            // for function v3
            {
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Read stream less than 4K") });
            }


            // for function v4
            {
                this.AddChild(new CVariation(v4) { Attribute = new Variation("Read with surrogate character entity") });
            }


            // for function v6
            {
                this.AddChild(new CVariation(v6) { Attribute = new Variation("Read with surrogates inside, comments/PIs, text, CDATA") });
            }


            // for function v10
            {
                this.AddChild(new CVariation(v10) { Attribute = new Variation("Tag name > 4K") });
            }


            // for function v12
            {
                this.AddChild(new CVariation(v12) { Attribute = new Variation("Whitespace characters in character entities") });
            }


            // for function v13
            {
                this.AddChild(new CVariation(v13) { Attribute = new Variation("Root element 18 chars length") });
            }


            // for function v14
            {
                this.AddChild(new CVariation(v14) { Attribute = new Variation("File with external DTD") });
            }


            // for function Read31
            {
                this.AddChild(new CVariation(Read31) { Attribute = new Variation("Namespace prefix starting with xml") });
            }


            // for function XmlExceptionCtorWithNoParamsDoesNotThrow
            {
                this.AddChild(new CVariation(XmlExceptionCtorWithNoParamsDoesNotThrow) { Attribute = new Variation("Instantiate an XmlException object without a parameter") });
            }


            // for function ReadEmpty
            {
                this.AddChild(new CVariation(ReadEmpty) { Attribute = new Variation("Test Read of Empty Elements") });
            }


            // for function Read33
            {
                this.AddChild(new CVariation(Read33) { Attribute = new Variation("1.Parsing this 'some]' as fragment fails with 'Unexpected EOF' error") });
            }


            // for function Read33a
            {
                this.AddChild(new CVariation(Read33a) { Attribute = new Variation("2. Parsing this 'some]' as fragment fails with 'Unexpected EOF' error") });
            }


            // for function Read34
            {
                this.AddChild(new CVariation(Read34) { Attribute = new Variation("Parsing xml:space attribute with spaces") });
            }


            // for function Read35
            {
                this.AddChild(new CVariation(Read35) { Attribute = new Variation("Parsing valid xml in ASCII encoding") });
            }


            // for function Read36
            {
                this.AddChild(new CVariation(Read36) { Attribute = new Variation("Parsing valid xml with huge attributes") });
            }


            // for function Read37
            {
                this.AddChild(new CVariation(Read37) { Attribute = new Variation("XmlReader accepts invalid <!ATTLIST e a NOTATION (prefix:name) #IMPLIED> declaration") });
            }


            // for function Read38
            {
                this.AddChild(new CVariation(Read38) { Attribute = new Variation("XmlReader reports strange error message on &#; character entity reference") });
            }


            // for function Read39
            {
                this.AddChild(new CVariation(Read39) { Attribute = new Variation("Assert and wrong XmlException.Message when run non-wf xml") });
            }


            // for function Read41
            {
                this.AddChild(new CVariation(Read41) { Attribute = new Variation("Testing general entity references itself") });
            }


            // for function Read42
            {
                this.AddChild(new CVariation(Read42) { Attribute = new Variation("Testing duplicate attribute") });
            }


            // for function Read43
            {
                this.AddChild(new CVariation(Read43) { Attribute = new Variation("Testing xml without root element") });
            }


            // for function Read44
            {
                this.AddChild(new CVariation(Read44) { Attribute = new Variation("Testing xml with unexpected token") });
            }


            // for function Read45
            {
                this.AddChild(new CVariation(Read45) { Attribute = new Variation("XmlException when run non-wf xml") });
            }


            // for function Read46
            {
                this.AddChild(new CVariation(Read46) { Attribute = new Variation("Parsing valid xml with 100 attributes with same names and diff.namespaces") });
            }


            // for function Read47
            {
                this.AddChild(new CVariation(Read47) { Attribute = new Variation("Parsing xml with invalid surrogate pair in PUBLIC") });
            }


            // for function Read48
            {
                this.AddChild(new CVariation(Read48) { Attribute = new Variation("Recursive entity reference inside attribute") });
            }


            // for function Read49
            {
                this.AddChild(new CVariation(Read49) { Attribute = new Variation("Parsing valid xml with large number of attributes inside single element") });
            }


            // for function Read50
            {
                this.AddChild(new CVariation(Read50) { Attribute = new Variation("3.Test DTD with namespaces") { Param = 3 } });
                this.AddChild(new CVariation(Read50) { Attribute = new Variation("1.Test DTD with namespaces") { Param = 1 } });
                this.AddChild(new CVariation(Read50) { Attribute = new Variation("2.Test DTD with namespaces") { Param = 2 } });
            }


            // for function Read53
            {
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("4.Parsing invalid DOCTYPE") { Param = 4 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("2.Parsing invalid DOCTYPE") { Param = 2 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("3.Parsing invalid DOCTYPE") { Param = 3 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("1.Parsing invalid DOCTYPE") { Param = 1 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("5.Parsing invalid DOCTYPE") { Param = 5 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("6.Parsing invalid DOCTYPE") { Param = 6 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("7.Parsing invalid DOCTYPE") { Param = 7 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("8.Parsing invalid xml version") { Param = 8 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("9.Parsing invalid xml version,DOCTYPE") { Param = 9 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("10.Parsing invalid xml version") { Param = 10 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("11.Parsing invalid xml version") { Param = 11 } });
                this.AddChild(new CVariation(Read53) { Attribute = new Variation("12.Parsing invalid xml version") { Param = 12 } });
            }


            // for function Read54
            {
                this.AddChild(new CVariation(Read54) { Attribute = new Variation("Parse an XML declaration that will have some whitespace before the closing") });
            }


            // for function Read55
            {
                this.AddChild(new CVariation(Read55) { Attribute = new Variation("Parsing xml with DTD and 200 attributes") { Param = 1 } });
                this.AddChild(new CVariation(Read55) { Attribute = new Variation("Parsing xml with DTD and 200 attributes with ns") { Param = 2 } });
            }


            // for function Read56
            {
                this.AddChild(new CVariation(Read56) { Attribute = new Variation("Parsing xml with DTD and 200 attributes and 1 duplicate") { Param = 1 } });
                this.AddChild(new CVariation(Read56) { Attribute = new Variation("Parsing xml with DTD and 200 attributes with ns and 1 duplicate") { Param = 2 } });
            }


            // for function Read57
            {
                this.AddChild(new CVariation(Read57) { Attribute = new Variation("Parse xml with whitespace nodes") });
            }


            // for function Read58
            {
                this.AddChild(new CVariation(Read58) { Attribute = new Variation("Parse xml with whitespace nodes and invalid char") });
            }


            // for function Read59
            {
                this.AddChild(new CVariation(Read59) { Attribute = new Variation("Parse xml with uri attribute") });
            }


            // for function Read63
            {
                this.AddChild(new CVariation(Read63) { Attribute = new Variation("XmlReader doesn't fail when numeric character entity computation overflows") });
            }


            // for function Read64
            {
                this.AddChild(new CVariation(Read64) { Attribute = new Variation("XmlReader should fail on ENTITY name with colons in it") { Param = 1 } });
                this.AddChild(new CVariation(Read64) { Attribute = new Variation("XmlReader should fail on ENTITY name with colons in it") { Param = 1 } });
            }


            // for function Read65
            {
            }


            // for function Read66
            {
                this.AddChild(new CVariation(Read66) { Attribute = new Variation("Parse input with a character zero 0x00 at root level.") });
            }


            // for function Read68
            {
                this.AddChild(new CVariation(Read68) { Attribute = new Variation("3.Parse input with utf-16 encoding") { Param = "charset03.xml" } });
                this.AddChild(new CVariation(Read68) { Attribute = new Variation("1.Parse input with utf-16 encoding") { Param = "charset01.xml" } });
            }


            // for function Read68a
            {
                this.AddChild(new CVariation(Read68a) { Attribute = new Variation("2.Parse input with utf-16 encoding") { Param = "charset02.xml" } });
            }


            // for function Read70
            {
                this.AddChild(new CVariation(Read70) { Attribute = new Variation("Add column position to the exception reported when end tag does not match the start tag") });
            }
        }
    }
}
