// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCResolveEntity : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCResolveEntity
        // Test Case
        public override void AddChildren()
        {
            // for function TestResolveEntityNodeType_None
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_None) { Attribute = new Variation("ResolveEntity On None") });
            }


            // for function TestResolveEntityNodeType_Element
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_Element) { Attribute = new Variation("ResolveEntity On Element") });
            }


            // for function TestResolveEntityNodeType_Attribute
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_Attribute) { Attribute = new Variation("ResolveEntity On Attribute") });
            }


            // for function TestResolveEntityNodeType_Text
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_Text) { Attribute = new Variation("ResolveEntity On Text") });
            }


            // for function TestResolveEntityNodeType_CDATA
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_CDATA) { Attribute = new Variation("ResolveEntity On CDATA") });
            }


            // for function TestResolveEntityNodeType_ProcessingInstruction
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_ProcessingInstruction) { Attribute = new Variation("ResolveEntity On ProcessingInstruction") });
            }


            // for function TestResolveEntityNodeType_Comment
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_Comment) { Attribute = new Variation("ResolveEntity On Comment") });
            }


            // for function TestResolveEntityNodeType_Whitespace
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_Whitespace) { Attribute = new Variation("ResolveEntity On Whitespace") });
            }


            // for function TestResolveEntityNodeType_EndElement
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_EndElement) { Attribute = new Variation("ResolveEntity On EndElement") });
            }


            // for function TestResolveEntityNodeType_XmlDeclaration
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_XmlDeclaration) { Attribute = new Variation("ResolveEntity On XmlDeclaration") });
            }


            // for function TestResolveEntityNodeType_EndEntity
            {
                this.AddChild(new CVariation(TestResolveEntityNodeType_EndEntity) { Attribute = new Variation("ResolveEntity On EndEntity") });
            }
        }
    }
}
