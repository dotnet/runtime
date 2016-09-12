// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCHasValue : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCHasValue
        // Test Case
        public override void AddChildren()
        {
            // for function TestHasValueNodeType_None
            {
                this.AddChild(new CVariation(TestHasValueNodeType_None) { Attribute = new Variation("HasValue On None") });
            }


            // for function TestHasValueNodeType_Element
            {
                this.AddChild(new CVariation(TestHasValueNodeType_Element) { Attribute = new Variation("HasValue On Element") { Pri = 0 } });
            }


            // for function TestHasValue1
            {
                this.AddChild(new CVariation(TestHasValue1) { Attribute = new Variation("Get node with a scalar value, verify the value with valid ReadString") });
            }


            // for function TestHasValueNodeType_Attribute
            {
                this.AddChild(new CVariation(TestHasValueNodeType_Attribute) { Attribute = new Variation("HasValue On Attribute") { Pri = 0 } });
            }


            // for function TestHasValueNodeType_Text
            {
                this.AddChild(new CVariation(TestHasValueNodeType_Text) { Attribute = new Variation("HasValue On Text") { Pri = 0 } });
            }


            // for function TestHasValueNodeType_CDATA
            {
                this.AddChild(new CVariation(TestHasValueNodeType_CDATA) { Attribute = new Variation("HasValue On CDATA") { Pri = 0 } });
            }


            // for function TestHasValueNodeType_ProcessingInstruction
            {
                this.AddChild(new CVariation(TestHasValueNodeType_ProcessingInstruction) { Attribute = new Variation("HasValue On ProcessingInstruction") { Pri = 0 } });
            }


            // for function TestHasValueNodeType_Comment
            {
                this.AddChild(new CVariation(TestHasValueNodeType_Comment) { Attribute = new Variation("HasValue On Comment") { Pri = 0 } });
            }


            // for function TestHasValueNodeType_Whitespace
            {
                this.AddChild(new CVariation(TestHasValueNodeType_Whitespace) { Attribute = new Variation("HasValue On Whitespace PreserveWhitespaces = true") { Pri = 0 } });
            }


            // for function TestHasValueNodeType_EndElement
            {
                this.AddChild(new CVariation(TestHasValueNodeType_EndElement) { Attribute = new Variation("HasValue On EndElement") });
            }


            // for function TestHasValueNodeType_XmlDeclaration
            {
                this.AddChild(new CVariation(TestHasValueNodeType_XmlDeclaration) { Attribute = new Variation("HasValue On XmlDeclaration") { Pri = 0 } });
            }


            // for function TestHasValueNodeType_EntityReference
            {
                this.AddChild(new CVariation(TestHasValueNodeType_EntityReference) { Attribute = new Variation("HasValue On EntityReference") });
            }


            // for function TestHasValueNodeType_EndEntity
            {
                this.AddChild(new CVariation(TestHasValueNodeType_EndEntity) { Attribute = new Variation("HasValue On EndEntity") });
            }


            // for function v13
            {
                this.AddChild(new CVariation(v13) { Attribute = new Variation("PI Value containing surrogates") { Pri = 0 } });
            }
        }
    }
}
