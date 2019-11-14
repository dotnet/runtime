// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCAttributeTest : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCAttributeTest
        // Test Case
        public override void AddChildren()
        {
            // for function TestAttributeTestNodeType_None
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_None) { Attribute = new Variation("Attribute Test On None") });
            }


            // for function TestAttributeTestNodeType_Element
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_Element) { Attribute = new Variation("Attribute Test  On Element") { Pri = 0 } });
            }


            // for function TestAttributeTestNodeType_Text
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_Text) { Attribute = new Variation("Attribute Test On Text") { Pri = 0 } });
            }


            // for function TestAttributeTestNodeType_CDATA
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_CDATA) { Attribute = new Variation("Attribute Test On CDATA") });
            }


            // for function TestAttributeTestNodeType_ProcessingInstruction
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_ProcessingInstruction) { Attribute = new Variation("Attribute Test On ProcessingInstruction") });
            }


            // for function TestAttributeTestNodeType_Comment
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_Comment) { Attribute = new Variation("AttributeTest On Comment") });
            }


            // for function TestAttributeTestNodeType_DocumentType
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_DocumentType) { Attribute = new Variation("AttributeTest On DocumentType") { Pri = 0 } });
            }


            // for function TestAttributeTestNodeType_Whitespace
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_Whitespace) { Attribute = new Variation("AttributeTest On Whitespace") });
            }


            // for function TestAttributeTestNodeType_EndElement
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_EndElement) { Attribute = new Variation("AttributeTest On EndElement") });
            }


            // for function TestAttributeTestNodeType_XmlDeclaration
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_XmlDeclaration) { Attribute = new Variation("AttributeTest On XmlDeclaration") { Pri = 0 } });
            }


            // for function TestAttributeTestNodeType_EntityReference
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_EntityReference) { Attribute = new Variation("Attribute Test On EntityReference") });
            }


            // for function TestAttributeTestNodeType_EndEntity
            {
                this.AddChild(new CVariation(TestAttributeTestNodeType_EndEntity) { Attribute = new Variation("AttributeTest On EndEntity") });
            }
        }
    }
}
