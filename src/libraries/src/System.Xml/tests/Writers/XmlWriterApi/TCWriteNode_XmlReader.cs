// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCWriteNode_XmlReader : ReaderParamTestCase
    {
        // Type is System.Xml.Tests.TCWriteNode_XmlReader
        // Test Case
        public override void AddChildren()
        {
            // for function writeNode_XmlReader1
            {
                this.AddChild(new CVariation(writeNode_XmlReader1) { Attribute = new Variation("WriteNode with null reader") { id = 1, Pri = 1 } });
            }


            // for function writeNode_XmlReader2
            {
                this.AddChild(new CVariation(writeNode_XmlReader2) { Attribute = new Variation("WriteNode with reader positioned on attribute, no operation") { id = 2, Pri = 1 } });
            }


            // for function writeNode_XmlReader3
            {
                this.AddChild(new CVariation(writeNode_XmlReader3) { Attribute = new Variation("WriteNode before reader.Read()") { id = 3, Pri = 1 } });
            }


            // for function writeNode_XmlReader4
            {
                this.AddChild(new CVariation(writeNode_XmlReader4) { Attribute = new Variation("WriteNode after first reader.Read()") { id = 4, Pri = 1 } });
            }


            // for function writeNode_XmlReader5
            {
                this.AddChild(new CVariation(writeNode_XmlReader5) { Attribute = new Variation("WriteNode when reader is positioned on middle of an element node") { id = 5, Pri = 1 } });
            }


            // for function writeNode_XmlReader6
            {
                this.AddChild(new CVariation(writeNode_XmlReader6) { Attribute = new Variation("WriteNode when reader state is EOF") { id = 6, Pri = 1 } });
            }


            // for function writeNode_XmlReader7
            {
                this.AddChild(new CVariation(writeNode_XmlReader7) { Attribute = new Variation("WriteNode when reader state is Closed") { id = 7, Pri = 1 } });
            }


            // for function writeNode_XmlReader8
            {
                this.AddChild(new CVariation(writeNode_XmlReader8) { Attribute = new Variation("WriteNode with reader on empty element node") { id = 8, Pri = 1 } });
            }


            // for function writeNode_XmlReader9
            {
                this.AddChild(new CVariation(writeNode_XmlReader9) { Attribute = new Variation("WriteNode with reader on 100 Nodes") { id = 9, Pri = 1 } });
            }


            // for function writeNode_XmlReader10
            {
                this.AddChild(new CVariation(writeNode_XmlReader10) { Attribute = new Variation("WriteNode with reader on node with mixed content") { id = 10, Pri = 1 } });
            }


            // for function writeNode_XmlReader11
            {
                this.AddChild(new CVariation(writeNode_XmlReader11) { Attribute = new Variation("WriteNode with reader on node with declared namespace in parent") { id = 11, Pri = 1 } });
            }

            // for function writeNode_XmlReader14
            {
                this.AddChild(new CVariation(writeNode_XmlReader14) { Attribute = new Variation("WriteNode with element that has different prefix") { id = 14, Pri = 1 } });
            }


            // for function writeNode_XmlReader15
            {
                this.AddChild(new CVariation(writeNode_XmlReader15) { Attribute = new Variation("Call WriteNode with default attributes = true and DTD") { id = 15, Pri = 1 } });
            }


            // for function writeNode_XmlReader16
            {
                this.AddChild(new CVariation(writeNode_XmlReader16) { Attribute = new Variation("Call WriteNode with default attributes = false and DTD") { id = 16, Pri = 1 } });
            }


            // for function writeNode_XmlReader17
            {
                this.AddChild(new CVariation(writeNode_XmlReader17) { Attribute = new Variation("Bug 53478 testcase: WriteNode with reader on empty element with attributes") { id = 17, Pri = 1 } });
            }


            // for function writeNode_XmlReader18
            {
                this.AddChild(new CVariation(writeNode_XmlReader18) { Attribute = new Variation("Bug 53479 testcase: WriteNode with document containing just empty element with attributes") { id = 18, Pri = 1 } });
            }


            // for function writeNode_XmlReader19
            {
                this.AddChild(new CVariation(writeNode_XmlReader19) { Attribute = new Variation("Bug 53683 testcase: Call WriteNode with special entity references as attribute value") { id = 19, Pri = 1 } });
            }


            // for function writeNode_XmlReader21
            {
                this.AddChild(new CVariation(writeNode_XmlReader21) { Attribute = new Variation("Call WriteNode with full end element") { id = 21, Pri = 1 } });
            }


            // for function writeNode_XmlReader21a
            {
                this.AddChild(new CVariation(writeNode_XmlReader21a) { Attribute = new Variation("Call WriteNode with tag mismatch") });
            }


            // for function writeNode_XmlReader21b
            {
                this.AddChild(new CVariation(writeNode_XmlReader21b) { Attribute = new Variation("Call WriteNode with default NS from DTD.UnexpToken") });
            }


            // for function writeNode_XmlReader22
            {
                this.AddChild(new CVariation(writeNode_XmlReader22) { Attribute = new Variation("Call WriteNode with reader on element with 100 attributes") { id = 22, Pri = 1 } });
            }


            // for function writeNode_XmlReader23
            {
                this.AddChild(new CVariation(writeNode_XmlReader23) { Attribute = new Variation("Call WriteNode with reader on text node") { id = 23, Pri = 1 } });
            }


            // for function writeNode_XmlReader24
            {
                this.AddChild(new CVariation(writeNode_XmlReader24) { Attribute = new Variation("Call WriteNode with reader on CDATA node") { id = 24, Pri = 1 } });
            }


            // for function writeNode_XmlReader25
            {
                this.AddChild(new CVariation(writeNode_XmlReader25) { Attribute = new Variation("Call WriteNode with reader on PI node") { id = 25, Pri = 1 } });
            }


            // for function writeNode_XmlReader26
            {
                this.AddChild(new CVariation(writeNode_XmlReader26) { Attribute = new Variation("Call WriteNode with reader on Comment node") { id = 26, Pri = 1 } });
            }


            // for function writeNode_XmlReader28
            {
                this.AddChild(new CVariation(writeNode_XmlReader28) { Attribute = new Variation("Call WriteNode with reader on XmlDecl (OmitXmlDecl false)") { Pri = 1 } });
            }


            // for function writeNode_XmlReader27
            {
                this.AddChild(new CVariation(writeNode_XmlReader27) { Attribute = new Variation("WriteNode should only write required namespaces") { id = 27, Pri = 1 } });
            }


            // for function writeNode_XmlReader28b
            {
                this.AddChild(new CVariation(writeNode_XmlReader28b) { Attribute = new Variation("Reader.WriteNode should only write required namespaces, include xmlns:xml") { id = 28, Pri = 1 } });
            }


            // for function writeNode_XmlReader29
            {
                this.AddChild(new CVariation(writeNode_XmlReader29) { Attribute = new Variation("WriteNode should only write required namespaces, exclude xmlns:xml") { id = 29, Pri = 1 } });
            }


            // for function writeNode_XmlReader30
            {
                this.AddChild(new CVariation(writeNode_XmlReader30) { Attribute = new Variation("WriteNode should only write required namespaces, change default ns at top level") { id = 30, Pri = 1 } });
            }


            // for function writeNode_XmlReader31
            {
                this.AddChild(new CVariation(writeNode_XmlReader31) { Attribute = new Variation("WriteNode should only write required namespaces, change default ns at same level") { id = 31, Pri = 1 } });
            }


            // for function writeNode_XmlReader32
            {
                this.AddChild(new CVariation(writeNode_XmlReader32) { Attribute = new Variation("WriteNode should only write required namespaces, change default ns at both levels") { id = 32, Pri = 1 } });
            }


            // for function writeNode_XmlReader33
            {
                this.AddChild(new CVariation(writeNode_XmlReader33) { Attribute = new Variation("WriteNode should only write required namespaces, change ns uri for same prefix") { id = 33, Pri = 1 } });
            }


            // for function writeNode_XmlReader34
            {
                this.AddChild(new CVariation(writeNode_XmlReader34) { Attribute = new Variation("WriteNode should only write required namespaces, reuse prefix from top level") { id = 34, Pri = 1 } });
            }


            // for function writeNode_XmlReader35
            {
                this.AddChild(new CVariation(writeNode_XmlReader35) { Attribute = new Variation("XDocument does not format content while Saving") { Param = "<?xml version='1.0'?><?pi?><?pi?>  <shouldbeindented><a>text</a></shouldbeindented><?pi?>" } });
                this.AddChild(new CVariation(writeNode_XmlReader35) { Attribute = new Variation("XDocument does not format content while Saving") { Param = "<?xml version='1.0'?><?pi?><?pi?>  <shouldbeindented><a>text</a></shouldbeindented><?pi?>" } });
            }


            // for function writeNode_XmlReader36
            {
                this.AddChild(new CVariation(writeNode_XmlReader36) { Attribute = new Variation("2.WriteNode with ascii encoding") { Param = false } });
                this.AddChild(new CVariation(writeNode_XmlReader36) { Attribute = new Variation("1.WriteNode with ascii encoding") { Param = true } });
            }


            // for function writeNode_XmlReader37
            {
                this.AddChild(new CVariation(writeNode_XmlReader37) { Attribute = new Variation("WriteNode DTD PUBLIC with identifier") { Param = true } });
                this.AddChild(new CVariation(writeNode_XmlReader37) { Attribute = new Variation("WriteNode DTD PUBLIC with identifier") { Param = false } });
            }


            // for function writeNode_XmlReader38
            {
                this.AddChild(new CVariation(writeNode_XmlReader38) { Attribute = new Variation("WriteNode DTD SYSTEM with identifier") { Param = false } });
                this.AddChild(new CVariation(writeNode_XmlReader38) { Attribute = new Variation("WriteNode DTD SYSTEM with identifier") { Param = true } });
            }


            // for function writeNode_XmlReader39
            {
                this.AddChild(new CVariation(writeNode_XmlReader39) { Attribute = new Variation("WriteNode DTD SYSTEM with valid surrogate pair") { Param = false } });
                this.AddChild(new CVariation(writeNode_XmlReader39) { Attribute = new Variation("WriteNode DTD SYSTEM with valid surrogate pair") { Param = true } });
            }
        }
    }
}
