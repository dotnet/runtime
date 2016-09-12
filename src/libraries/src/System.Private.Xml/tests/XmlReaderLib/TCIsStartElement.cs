// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCIsStartElement : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCIsStartElement
        // Test Case
        public override void AddChildren()
        {
            // for function TestIsStartElement1
            {
                this.AddChild(new CVariation(TestIsStartElement1) { Attribute = new Variation("IsStartElement on Regular Element, no namespace") { Pri = 0 } });
            }


            // for function TestIsStartElement2
            {
                this.AddChild(new CVariation(TestIsStartElement2) { Attribute = new Variation("IsStartElement on Empty Element, no namespace") { Pri = 0 } });
            }


            // for function TestIsStartElement3
            {
                this.AddChild(new CVariation(TestIsStartElement3) { Attribute = new Variation("IsStartElement on regular Element, with namespace") { Pri = 0 } });
            }


            // for function TestIsStartElement4
            {
                this.AddChild(new CVariation(TestIsStartElement4) { Attribute = new Variation("IsStartElement on Empty Tag, with default namespace") { Pri = 0 } });
            }


            // for function TestIsStartElement5
            {
                this.AddChild(new CVariation(TestIsStartElement5) { Attribute = new Variation("IsStartElement with Name=String.Empty") });
            }


            // for function TestIsStartElement6
            {
                this.AddChild(new CVariation(TestIsStartElement6) { Attribute = new Variation("IsStartElement on Empty Element with Name and Namespace=String.Empty") });
            }


            // for function TestIsStartElement7
            {
                this.AddChild(new CVariation(TestIsStartElement7) { Attribute = new Variation("IsStartElement on CDATA") });
            }


            // for function TestIsStartElement8
            {
                this.AddChild(new CVariation(TestIsStartElement8) { Attribute = new Variation("IsStartElement on EndElement, no namespace") });
            }


            // for function TestIsStartElement9
            {
                this.AddChild(new CVariation(TestIsStartElement9) { Attribute = new Variation("IsStartElement on EndElement, with namespace") });
            }


            // for function TestIsStartElement10
            {
                this.AddChild(new CVariation(TestIsStartElement10) { Attribute = new Variation("IsStartElement on Attribute") });
            }


            // for function TestIsStartElement11
            {
                this.AddChild(new CVariation(TestIsStartElement11) { Attribute = new Variation("IsStartElement on Text") });
            }


            // for function TestIsStartElement12
            {
                this.AddChild(new CVariation(TestIsStartElement12) { Attribute = new Variation("IsStartElement on ProcessingInstruction") });
            }


            // for function TestIsStartElement13
            {
                this.AddChild(new CVariation(TestIsStartElement13) { Attribute = new Variation("IsStartElement on Comment") });
            }


            // for function TestIsStartElement15
            {
                this.AddChild(new CVariation(TestIsStartElement15) { Attribute = new Variation("IsStartElement on XmlDeclaration") });
            }


            // for function TestTextIsStartElement1
            {
                this.AddChild(new CVariation(TestTextIsStartElement1) { Attribute = new Variation("IsStartElement on EntityReference") });
            }


            // for function TestTextIsStartElement2
            {
                this.AddChild(new CVariation(TestTextIsStartElement2) { Attribute = new Variation("IsStartElement on EndEntity") });
            }
        }
    }
}
