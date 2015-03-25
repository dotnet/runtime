// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlReaderTest.Common
{
    public partial class TCIsEmptyElement : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.Common.TCIsEmptyElement
        // Test Case
        public override void AddChildren()
        {
            // for function TestEmpty1
            {
                this.AddChild(new CVariation(TestEmpty1) { Attribute = new Variation("Set and Get an element that ends with />") { Pri = 0 } });
            }


            // for function TestEmpty2
            {
                this.AddChild(new CVariation(TestEmpty2) { Attribute = new Variation("Set and Get an element with an attribute that ends with />") { Pri = 0 } });
            }


            // for function TestEmpty3
            {
                this.AddChild(new CVariation(TestEmpty3) { Attribute = new Variation("Set and Get an element that ends without />") { Pri = 0 } });
            }


            // for function TestEmpty4
            {
                this.AddChild(new CVariation(TestEmpty4) { Attribute = new Variation("Set and Get an element with an attribute that ends with />") { Pri = 0 } });
            }


            // for function TestEmptyNodeType_Element
            {
                this.AddChild(new CVariation(TestEmptyNodeType_Element) { Attribute = new Variation("IsEmptyElement On Element") { Pri = 0 } });
            }


            // for function TestEmptyNodeType_None
            {
                this.AddChild(new CVariation(TestEmptyNodeType_None) { Attribute = new Variation("IsEmptyElement On None") });
            }


            // for function TestEmptyNodeType_Text
            {
                this.AddChild(new CVariation(TestEmptyNodeType_Text) { Attribute = new Variation("IsEmptyElement On Text") });
            }


            // for function TestEmptyNodeType_CDATA
            {
                this.AddChild(new CVariation(TestEmptyNodeType_CDATA) { Attribute = new Variation("IsEmptyElement On CDATA") });
            }


            // for function TestEmptyNodeType_ProcessingInstruction
            {
                this.AddChild(new CVariation(TestEmptyNodeType_ProcessingInstruction) { Attribute = new Variation("IsEmptyElement On ProcessingInstruction") });
            }


            // for function TestEmptyNodeType_Comment
            {
                this.AddChild(new CVariation(TestEmptyNodeType_Comment) { Attribute = new Variation("IsEmptyElement On Comment") });
            }


            // for function TestEmptyNodeType_Whitespace
            {
                this.AddChild(new CVariation(TestEmptyNodeType_Whitespace) { Attribute = new Variation("IsEmptyElement On Whitespace PreserveWhitespaces = true") });
            }


            // for function TestEmptyNodeType_EndElement
            {
                this.AddChild(new CVariation(TestEmptyNodeType_EndElement) { Attribute = new Variation("IsEmptyElement On EndElement") });
            }


            // for function TestEmptyNodeType_XmlDeclaration
            {
                this.AddChild(new CVariation(TestEmptyNodeType_XmlDeclaration) { Attribute = new Variation("IsEmptyElement On XmlDeclaration") });
            }


            // for function TestEmptyNodeType_EntityReference
            {
                this.AddChild(new CVariation(TestEmptyNodeType_EntityReference) { Attribute = new Variation("IsEmptyElement On EntityReference") });
            }


            // for function TestEmptyNodeType_EndEntity
            {
                this.AddChild(new CVariation(TestEmptyNodeType_EndEntity) { Attribute = new Variation("IsEmptyElement On EndEntity") });
            }
        }
    }
}
