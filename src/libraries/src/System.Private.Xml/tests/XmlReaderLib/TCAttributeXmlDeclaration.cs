// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCAttributeXmlDeclaration : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCAttributeXmlDeclaration
        // Test Case
        public override void AddChildren()
        {
            // for function TAXmlDecl_1
            {
                this.AddChild(new CVariation(TAXmlDecl_1) { Attribute = new Variation("AttributeCount and HasAttributes") { Pri = 0 } });
            }


            // for function TAXmlDecl_2
            {
                this.AddChild(new CVariation(TAXmlDecl_2) { Attribute = new Variation("MoveToFirstAttribute/MoveToNextAttribute navigation") { Pri = 0 } });
            }


            // for function TAXmlDecl_3
            {
                this.AddChild(new CVariation(TAXmlDecl_3) { Attribute = new Variation("MoveToFirstAttribute/MoveToNextAttribute successive calls") });
            }


            // for function TAXmlDecl_4
            {
                this.AddChild(new CVariation(TAXmlDecl_4) { Attribute = new Variation("MoveToAttribute attribute access") });
            }


            // for function TAXmlDecl_5
            {
                this.AddChild(new CVariation(TAXmlDecl_5) { Attribute = new Variation("MoveToAttribute attribute access with invalid index") });
            }


            // for function TAXmlDecl_6
            {
                this.AddChild(new CVariation(TAXmlDecl_6) { Attribute = new Variation("GetAttribute attribute access") });
            }


            // for function TAXmlDecl_7
            {
                this.AddChild(new CVariation(TAXmlDecl_7) { Attribute = new Variation("GetAttribute attribute access with invalid index") });
            }


            // for function TAXmlDecl_8
            {
                this.AddChild(new CVariation(TAXmlDecl_8) { Attribute = new Variation("this[] attribute access") });
            }


            // for function TAXmlDecl_9
            {
                this.AddChild(new CVariation(TAXmlDecl_9) { Attribute = new Variation("this[] attribute access with invalid index") });
            }


            // for function TAXmlDecl_10
            {
                this.AddChild(new CVariation(TAXmlDecl_10) { Attribute = new Variation("ReadAttributeValue on XmlDecl attributes") });
            }


            // for function TAXmlDecl_11
            {
                this.AddChild(new CVariation(TAXmlDecl_11) { Attribute = new Variation("LocalName, NamespaceURI and Prefix on XmlDecl attributes") });
            }


            // for function TAXmlDecl_12
            {
                this.AddChild(new CVariation(TAXmlDecl_12) { Attribute = new Variation("Whitespace between XmlDecl attributes") });
            }


            // for function TAXmlDecl_13
            {
                this.AddChild(new CVariation(TAXmlDecl_13) { Attribute = new Variation("MoveToElement on XmlDeclaration attributes") });
            }
        }
    }
}
