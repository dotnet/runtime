// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadStartElement : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadStartElement
        // Test Case
        public override void AddChildren()
        {
            // for function TestReadStartElement1
            {
                this.AddChild(new CVariation(TestReadStartElement1) { Attribute = new Variation("ReadStartElement on Regular Element, no namespace") { Pri = 0 } });
            }


            // for function TestReadStartElement2
            {
                this.AddChild(new CVariation(TestReadStartElement2) { Attribute = new Variation("ReadStartElement on Empty Element, no namespace") { Pri = 0 } });
            }


            // for function TestReadStartElement3
            {
                this.AddChild(new CVariation(TestReadStartElement3) { Attribute = new Variation("ReadStartElement on regular Element, with namespace") { Pri = 0 } });
            }


            // for function TestReadStartElement4
            {
                this.AddChild(new CVariation(TestReadStartElement4) { Attribute = new Variation("Passing ns=String.EmptyErrorCase: ReadStartElement on regular Element, with namespace") { Pri = 0 } });
            }


            // for function TestReadStartElement5
            {
                this.AddChild(new CVariation(TestReadStartElement5) { Attribute = new Variation("Passing no ns: ReadStartElement on regular Element, with namespace") { Pri = 0 } });
            }


            // for function TestReadStartElement6
            {
                this.AddChild(new CVariation(TestReadStartElement6) { Attribute = new Variation("ReadStartElement on Empty Tag, with namespace") });
            }


            // for function TestReadStartElement7
            {
                this.AddChild(new CVariation(TestReadStartElement7) { Attribute = new Variation("ErrorCase: ReadStartElement on Empty Tag, with namespace, passing ns=String.Empty") });
            }


            // for function TestReadStartElement8
            {
                this.AddChild(new CVariation(TestReadStartElement8) { Attribute = new Variation("ReadStartElement on Empty Tag, with namespace, passing no ns") });
            }


            // for function TestReadStartElement9
            {
                this.AddChild(new CVariation(TestReadStartElement9) { Attribute = new Variation("ReadStartElement with Name=String.Empty") });
            }


            // for function TestReadStartElement10
            {
                this.AddChild(new CVariation(TestReadStartElement10) { Attribute = new Variation("ReadStartElement on Empty Element with Name and Namespace=String.Empty") });
            }


            // for function TestReadStartElement11
            {
                this.AddChild(new CVariation(TestReadStartElement11) { Attribute = new Variation("ReadStartElement on CDATA") });
            }


            // for function TestReadStartElement12
            {
                this.AddChild(new CVariation(TestReadStartElement12) { Attribute = new Variation("ReadStartElement() on EndElement, no namespace") });
            }


            // for function TestReadStartElement13
            {
                this.AddChild(new CVariation(TestReadStartElement13) { Attribute = new Variation("ReadStartElement(n) on EndElement, no namespace") });
            }


            // for function TestReadStartElement14
            {
                this.AddChild(new CVariation(TestReadStartElement14) { Attribute = new Variation("ReadStartElement(n, String.Empty) on EndElement, no namespace") });
            }


            // for function TestReadStartElement15
            {
                this.AddChild(new CVariation(TestReadStartElement15) { Attribute = new Variation("ReadStartElement() on EndElement, with namespace") });
            }


            // for function TestReadStartElement16
            {
                this.AddChild(new CVariation(TestReadStartElement16) { Attribute = new Variation("ReadStartElement(n,ns) on EndElement, with namespace") });
            }
        }
    }
}
