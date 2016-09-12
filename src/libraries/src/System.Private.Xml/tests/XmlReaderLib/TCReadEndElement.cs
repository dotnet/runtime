// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadEndElement : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadEndElement
        // Test Case
        public override void AddChildren()
        {
            // for function TestReadEndElement1
            {
                this.AddChild(new CVariation(TestReadEndElement1) { Attribute = new Variation("ReadEndElement() on EndElement, no namespace") { Pri = 0 } });
            }


            // for function TestReadEndElement2
            {
                this.AddChild(new CVariation(TestReadEndElement2) { Attribute = new Variation("ReadEndElement() on EndElement, with namespace") { Pri = 0 } });
            }


            // for function TestReadEndElement3
            {
                this.AddChild(new CVariation(TestReadEndElement3) { Attribute = new Variation("ReadEndElement on Start Element, no namespace") });
            }


            // for function TestReadEndElement4
            {
                this.AddChild(new CVariation(TestReadEndElement4) { Attribute = new Variation("ReadEndElement on Empty Element, no namespace") { Pri = 0 } });
            }


            // for function TestReadEndElement5
            {
                this.AddChild(new CVariation(TestReadEndElement5) { Attribute = new Variation("ReadEndElement on regular Element, with namespace") { Pri = 0 } });
            }


            // for function TestReadEndElement6
            {
                this.AddChild(new CVariation(TestReadEndElement6) { Attribute = new Variation("ReadEndElement on Empty Tag, with namespace") { Pri = 0 } });
            }


            // for function TestReadEndElement7
            {
                this.AddChild(new CVariation(TestReadEndElement7) { Attribute = new Variation("ReadEndElement on CDATA") });
            }


            // for function TestReadEndElement9
            {
                this.AddChild(new CVariation(TestReadEndElement9) { Attribute = new Variation("ReadEndElement on Text") });
            }


            // for function TestReadEndElement10
            {
                this.AddChild(new CVariation(TestReadEndElement10) { Attribute = new Variation("ReadEndElement on ProcessingInstruction") });
            }


            // for function TestReadEndElement11
            {
                this.AddChild(new CVariation(TestReadEndElement11) { Attribute = new Variation("ReadEndElement on Comment") });
            }


            // for function TestReadEndElement13
            {
                this.AddChild(new CVariation(TestReadEndElement13) { Attribute = new Variation("ReadEndElement on XmlDeclaration") });
            }


            // for function TestTextReadEndElement1
            {
                this.AddChild(new CVariation(TestTextReadEndElement1) { Attribute = new Variation("ReadEndElement on EntityReference") });
            }


            // for function TestTextReadEndElement2
            {
                this.AddChild(new CVariation(TestTextReadEndElement2) { Attribute = new Variation("ReadEndElement on EndEntity") });
            }
        }
    }
}
