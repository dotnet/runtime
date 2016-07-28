// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadOuterXml : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadOuterXml
        // Test Case
        public override void AddChildren()
        {
            // for function ReadOuterXml1
            {
                this.AddChild(new CVariation(ReadOuterXml1) { Attribute = new Variation("ReadOuterXml on empty element w/o attributes") { Pri = 0 } });
            }


            // for function ReadOuterXml2
            {
                this.AddChild(new CVariation(ReadOuterXml2) { Attribute = new Variation("ReadOuterXml on empty element w/ attributes") { Pri = 0 } });
            }


            // for function ReadOuterXml3
            {
                this.AddChild(new CVariation(ReadOuterXml3) { Attribute = new Variation("ReadOuterXml on full empty element w/o attributes") });
            }


            // for function ReadOuterXml4
            {
                this.AddChild(new CVariation(ReadOuterXml4) { Attribute = new Variation("ReadOuterXml on full empty element w/ attributes") });
            }


            // for function ReadOuterXml5
            {
                this.AddChild(new CVariation(ReadOuterXml5) { Attribute = new Variation("ReadOuterXml on element with text content") { Pri = 0 } });
            }


            // for function ReadOuterXml6
            {
                this.AddChild(new CVariation(ReadOuterXml6) { Attribute = new Variation("ReadOuterXml on element with attributes") { Pri = 0 } });
            }


            // for function ReadOuterXml7
            {
                this.AddChild(new CVariation(ReadOuterXml7) { Attribute = new Variation("ReadOuterXml on element with text and markup content") });
            }


            // for function ReadOuterXml8
            {
                this.AddChild(new CVariation(ReadOuterXml8) { Attribute = new Variation("ReadOuterXml with multiple level of elements") });
            }


            // for function ReadOuterXml9
            {
                this.AddChild(new CVariation(ReadOuterXml9) { Attribute = new Variation("ReadOuterXml with multiple level of elements, text and attributes") { Pri = 0 } });
            }


            // for function ReadOuterXml10
            {
                this.AddChild(new CVariation(ReadOuterXml10) { Attribute = new Variation("ReadOuterXml on element with complex content (CDATA, PIs, Comments)") { Pri = 0 } });
            }


            // for function ReadOuterXml11
            {
                this.AddChild(new CVariation(ReadOuterXml11) { Attribute = new Variation("ReadOuterXml on element with entities, EntityHandling = ExpandEntities") });
            }


            // for function ReadOuterXml12
            {
                this.AddChild(new CVariation(ReadOuterXml12) { Attribute = new Variation("ReadOuterXml on attribute node of empty element") });
            }


            // for function ReadOuterXml13
            {
                this.AddChild(new CVariation(ReadOuterXml13) { Attribute = new Variation("ReadOuterXml on attribute node of full empty element") });
            }


            // for function ReadOuterXml14
            {
                this.AddChild(new CVariation(ReadOuterXml14) { Attribute = new Variation("ReadOuterXml on attribute node") { Pri = 0 } });
            }


            // for function ReadOuterXml15
            {
                this.AddChild(new CVariation(ReadOuterXml15) { Attribute = new Variation("ReadOuterXml on attribute with entities, EntityHandling = ExpandEntities") { Pri = 0 } });
            }


            // for function ReadOuterXml16
            {
                this.AddChild(new CVariation(ReadOuterXml16) { Attribute = new Variation("ReadOuterXml on Comment") });
            }


            // for function ReadOuterXml17
            {
                this.AddChild(new CVariation(ReadOuterXml17) { Attribute = new Variation("ReadOuterXml on ProcessingInstruction") });
            }


            // for function ReadOuterXml19
            {
                this.AddChild(new CVariation(ReadOuterXml19) { Attribute = new Variation("ReadOuterXml on XmlDeclaration") });
            }


            // for function ReadOuterXml20
            {
                this.AddChild(new CVariation(ReadOuterXml20) { Attribute = new Variation("ReadOuterXml on EndElement") });
            }


            // for function ReadOuterXml21
            {
                this.AddChild(new CVariation(ReadOuterXml21) { Attribute = new Variation("ReadOuterXml on Text") });
            }


            // for function ReadOuterXml22
            {
                this.AddChild(new CVariation(ReadOuterXml22) { Attribute = new Variation("ReadOuterXml on EntityReference") });
            }


            // for function ReadOuterXml23
            {
                this.AddChild(new CVariation(ReadOuterXml23) { Attribute = new Variation("ReadOuterXml on EndEntity") });
            }


            // for function ReadOuterXml24
            {
                this.AddChild(new CVariation(ReadOuterXml24) { Attribute = new Variation("ReadOuterXml on CDATA") });
            }


            // for function ReadOuterXml25
            {
                this.AddChild(new CVariation(ReadOuterXml25) { Attribute = new Variation("ReadOuterXml on XmlDeclaration attributes") });
            }


            // for function TRReadOuterXml27
            {
                this.AddChild(new CVariation(TRReadOuterXml27) { Attribute = new Variation("ReadOuterXml on element with entities, EntityHandling = ExpandCharEntities") });
            }


            // for function TRReadOuterXml28
            {
                this.AddChild(new CVariation(TRReadOuterXml28) { Attribute = new Variation("ReadOuterXml on attribute with entities, EntityHandling = ExpandCharEntites") });
            }


            // for function TestTextReadOuterXml29
            {
                this.AddChild(new CVariation(TestTextReadOuterXml29) { Attribute = new Variation("One large element") });
            }


            // for function ReadOuterXmlWhenNamespacesIgnoredWorksWithXmlns
            {
                this.AddChild(new CVariation(ReadOuterXmlWhenNamespacesIgnoredWorksWithXmlns) { Attribute = new Variation("Read OuterXml when Namespaces=false and has an attribute xmlns") });
            }


            // for function SubtreeXmlReaderOutputsSingleNamespaceDeclaration
            {
                this.AddChild(new CVariation(SubtreeXmlReaderOutputsSingleNamespaceDeclaration) { Attribute = new Variation("XmlReader.ReadOuterXml outputs multiple namespace declarations if called within multiple XmlReader.ReadSubtree() calls") });
            }
        }
    }
}
