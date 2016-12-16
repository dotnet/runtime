// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCFullEndElement : XmlWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCFullEndElement
        // Test Case
        public override void AddChildren()
        {
            // for function fullEndElement_1
            {
                this.AddChild(new CVariation(fullEndElement_1) { Attribute = new Variation("Sanity test for WriteFullEndElement()") { id = 1, Pri = 0 } });
            }


            // for function fullEndElement_2
            {
                this.AddChild(new CVariation(fullEndElement_2) { Attribute = new Variation("Call WriteFullEndElement before calling WriteStartElement") { id = 2, Pri = 2 } });
            }


            // for function fullEndElement_3
            {
                this.AddChild(new CVariation(fullEndElement_3) { Attribute = new Variation("Call WriteFullEndElement after WriteEndElement") { id = 3, Pri = 2 } });
            }


            // for function fullEndElement_4
            {
                this.AddChild(new CVariation(fullEndElement_4) { Attribute = new Variation("Call WriteFullEndElement without closing attributes") { id = 4, Pri = 1 } });
            }


            // for function fullEndElement_5
            {
                this.AddChild(new CVariation(fullEndElement_5) { Attribute = new Variation("Call WriteFullEndElement after WriteStartAttribute") { id = 5, Pri = 1 } });
            }


            // for function fullEndElement_6
            {
                this.AddChild(new CVariation(fullEndElement_6) { Attribute = new Variation("WriteFullEndElement for 100 nested elements") { id = 6, Pri = 1 } });
            }


            // for class System.Xml.Tests.TCFullEndElement+TCElemNamespace
            {
                this.AddChild(new TCElemNamespace() { Attribute = new TestCase() { Name = "Element Namespace" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCAttrNamespace
            {
                this.AddChild(new TCAttrNamespace() { Attribute = new TestCase() { Name = "Attribute Namespace" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCCData
            {
                this.AddChild(new TCCData() { Attribute = new TestCase() { Name = "WriteCData" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCComment
            {
                this.AddChild(new TCComment() { Attribute = new TestCase() { Name = "WriteComment" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCEntityRef
            {
                this.AddChild(new TCEntityRef() { Attribute = new TestCase() { Name = "WriteEntityRef" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCCharEntity
            {
                this.AddChild(new TCCharEntity() { Attribute = new TestCase() { Name = "WriteCharEntity" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCSurrogateCharEntity
            {
                this.AddChild(new TCSurrogateCharEntity() { Attribute = new TestCase() { Name = "WriteSurrogateCharEntity" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCPI
            {
                this.AddChild(new TCPI() { Attribute = new TestCase() { Name = "WriteProcessingInstruction" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCWriteNmToken
            {
                this.AddChild(new TCWriteNmToken() { Attribute = new TestCase() { Name = "WriteNmToken" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCWriteName
            {
                this.AddChild(new TCWriteName() { Attribute = new TestCase() { Name = "WriteName" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCWriteQName
            {
                this.AddChild(new TCWriteQName() { Attribute = new TestCase() { Name = "WriteQualifiedName" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCWriteChars
            {
                this.AddChild(new TCWriteChars() { Attribute = new TestCase() { Name = "WriteChars" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCWriteString
            {
                this.AddChild(new TCWriteString() { Attribute = new TestCase() { Name = "WriteString" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCWhiteSpace
            {
                this.AddChild(new TCWhiteSpace() { Attribute = new TestCase() { Name = "WriteWhitespace" } });
            }
            // for class System.Xml.Tests.TCFullEndElement+TCWriteValue
            {
                this.AddChild(new TCWriteValue() { Attribute = new TestCase() { Name = "WriteValue" } });
            }
        }
        public partial class TCElemNamespace : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCElemNamespace
            // Test Case
            public override void AddChildren()
            {
                // for function elemNamespace_1
                {
                    this.AddChild(new CVariation(elemNamespace_1) { Attribute = new Variation("Multiple NS decl for same prefix on an element") { id = 1, Pri = 1 } });
                }


                // for function elemNamespace_2
                {
                    this.AddChild(new CVariation(elemNamespace_2) { Attribute = new Variation("Multiple NS decl for same prefix (same NS value) on an element") { id = 2, Pri = 1 } });
                }


                // for function elemNamespace_3
                {
                    this.AddChild(new CVariation(elemNamespace_3) { Attribute = new Variation("Element and attribute have same prefix, but different namespace value") { id = 3, Pri = 2 } });
                }


                // for function elemNamespace_4
                {
                    this.AddChild(new CVariation(elemNamespace_4) { Attribute = new Variation("Nested elements have same prefix, but different namespace") { id = 4, Pri = 1 } });
                }


                // for function elemNamespace_5
                {
                    this.AddChild(new CVariation(elemNamespace_5) { Attribute = new Variation("Mapping reserved prefix xml to invalid namespace") { id = 5, Pri = 1 } });
                }


                // for function elemNamespace_6
                {
                    this.AddChild(new CVariation(elemNamespace_6) { Attribute = new Variation("Mapping reserved prefix xml to correct namespace") { id = 6, Pri = 1 } });
                }


                // for function elemNamespace_7
                {
                    this.AddChild(new CVariation(elemNamespace_7) { Attribute = new Variation("Write element with prefix beginning with xml") { id = 7, Pri = 1 } });
                }


                // for function elemNamespace_8
                {
                    this.AddChild(new CVariation(elemNamespace_8) { Attribute = new Variation("Reuse prefix that refers the same as default namespace") { id = 8, Pri = 2 } });
                }


                // for function elemNamespace_9
                {
                    this.AddChild(new CVariation(elemNamespace_9) { Attribute = new Variation("Should throw error for prefix=xmlns") { id = 9, Pri = 2 } });
                }


                // for function elemNamespace_10
                {
                    this.AddChild(new CVariation(elemNamespace_10) { Attribute = new Variation("Create nested element without prefix but with namespace of parent element with a defined prefix") { id = 10, Pri = 2 } });
                }


                // for function elemNamespace_11
                {
                    this.AddChild(new CVariation(elemNamespace_11) { Attribute = new Variation("Create different prefix for element and attribute that have same namespace") { id = 11, Pri = 2 } });
                }


                // for function elemNamespace_12
                {
                    this.AddChild(new CVariation(elemNamespace_12) { Attribute = new Variation("Create same prefix for element and attribute that have same namespace") { id = 12, Pri = 2 } });
                }


                // for function elemNamespace_13
                {
                    this.AddChild(new CVariation(elemNamespace_13) { Attribute = new Variation("Try to re-define NS prefix on attribute which is aleady defined on an element") { id = 13, Pri = 2 } });
                }


                // for function elemNamespace_14
                {
                    this.AddChild(new CVariation(elemNamespace_14) { Attribute = new Variation("Bug 71001 testcase: Namespace string contains surrogates, reuse at different levels") { id = 14, Pri = 1 } });
                }


                // for function elemNamespace_15
                {
                    this.AddChild(new CVariation(elemNamespace_15) { Attribute = new Variation("Bug 65929 testcase: Namespace containing entities, use at multiple levels") { id = 15, Pri = 1 } });
                }


                // for function elemNamespace_16
                {
                    this.AddChild(new CVariation(elemNamespace_16) { Attribute = new Variation("Bug 53518 testcase: Verify it resets default namespace when redefined earlier in the stack") { id = 16, Pri = 1 } });
                }


                // for function elemNamespace_17
                {
                    this.AddChild(new CVariation(elemNamespace_17) { Attribute = new Variation("The default namespace for an element can not be changed once it is written out") { id = 17, Pri = 1 } });
                }


                // for function elemNamespace_18
                {
                    this.AddChild(new CVariation(elemNamespace_18) { Attribute = new Variation("Map XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix") { id = 18, Pri = 1 } });
                }


                // for function elemNamespace_19
                {
                    this.AddChild(new CVariation(elemNamespace_19) { Attribute = new Variation("Bug 101315 testcase: Pass NULL as NS to WriteStartElement") { id = 19, Pri = 1 } });
                }


                // for function elemNamespace_20
                {
                    this.AddChild(new CVariation(elemNamespace_20) { Attribute = new Variation("Write element in reserved XML namespace, should error") { id = 20, Pri = 1 } });
                }


                // for function elemNamespace_21
                {
                    this.AddChild(new CVariation(elemNamespace_21) { Attribute = new Variation("Write element in reserved XMLNS namespace, should error") { id = 21, Pri = 1 } });
                }


                // for function elemNamespace_22
                {
                    this.AddChild(new CVariation(elemNamespace_22) { Attribute = new Variation("Mapping a prefix to empty ns should error") { id = 22, Pri = 1 } });
                }


                // for function elemNamespace_23
                {
                    this.AddChild(new CVariation(elemNamespace_23) { Attribute = new Variation("Pass null prefix to WriteStartElement()") { id = 23, Pri = 1 } });
                }


                // for function elemNamespace_24
                {
                    this.AddChild(new CVariation(elemNamespace_24) { Attribute = new Variation("Pass String.Empty prefix to WriteStartElement()") { id = 24, Pri = 1 } });
                }


                // for function elemNamespace_25
                {
                    this.AddChild(new CVariation(elemNamespace_25) { Attribute = new Variation("Pass null ns to WriteStartElement()") { id = 25, Pri = 1 } });
                }


                // for function elemNamespace_26
                {
                    this.AddChild(new CVariation(elemNamespace_26) { Attribute = new Variation("Pass String.Empty ns to WriteStartElement()") { id = 26, Pri = 1 } });
                }


                // for function elemNamespace_27
                {
                    this.AddChild(new CVariation(elemNamespace_27) { Attribute = new Variation("Pass null prefix to WriteStartElement() when namespace is in scope") { id = 27, Pri = 1 } });
                }


                // for function elemNamespace_28
                {
                    this.AddChild(new CVariation(elemNamespace_28) { Attribute = new Variation("Pass String.Empty prefix to WriteStartElement() when namespace is in scope") { id = 28, Pri = 1 } });
                }


                // for function elemNamespace_29
                {
                    this.AddChild(new CVariation(elemNamespace_29) { Attribute = new Variation("Pass null ns to WriteStartElement() when prefix is in scope") { id = 29, Pri = 1 } });
                }


                // for function elemNamespace_30
                {
                    this.AddChild(new CVariation(elemNamespace_30) { Attribute = new Variation("Pass String.Empty ns to WriteStartElement() when prefix is in scope") { id = 30, Pri = 1 } });
                }


                // for function elemNamespace_31
                {
                    this.AddChild(new CVariation(elemNamespace_31) { Attribute = new Variation("Pass String.Empty ns to WriteStartElement() when prefix is in scope") { id = 31, Pri = 1 } });
                }


                // for function elemNamespace_32
                {
                    this.AddChild(new CVariation(elemNamespace_32) { Attribute = new Variation("Mapping empty ns uri to a prefix should error") { id = 31, Pri = 1 } });
                }
            }
        }
        public partial class TCAttrNamespace : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCAttrNamespace
            // Test Case
            public override void AddChildren()
            {
                // for function attrNamespace_1
                {
                    this.AddChild(new CVariation(attrNamespace_1) { Attribute = new Variation("Define prefix 'xml' with invalid namespace URI 'foo'") { id = 1, Pri = 1 } });
                }


                // for function attrNamespace_2
                {
                    this.AddChild(new CVariation(attrNamespace_2) { Attribute = new Variation("Bind NS prefix 'xml' with valid namespace URI") { id = 2, Pri = 1 } });
                }


                // for function attrNamespace_3
                {
                    this.AddChild(new CVariation(attrNamespace_3) { Attribute = new Variation("Bind NS prefix 'xmlA' with namespace URI 'foo'") { id = 3, Pri = 1 } });
                }


                // for function attrNamespace_4
                {
                    this.AddChild(new CVariation(attrNamespace_4) { Attribute = new Variation("Write attribute xml:space with correct namespace") { id = 4, Pri = 1 } });
                }


                // for function attrNamespace_5
                {
                    this.AddChild(new CVariation(attrNamespace_5) { Attribute = new Variation("Write attribute xml:space with incorrect namespace") { id = 5, Pri = 1 } });
                }


                // for function attrNamespace_6
                {
                    this.AddChild(new CVariation(attrNamespace_6) { Attribute = new Variation("Write attribute xml:lang with incorrect namespace") { id = 6, Pri = 1 } });
                }


                // for function attrNamespace_7
                {
                    this.AddChild(new CVariation(attrNamespace_7) { Attribute = new Variation("WriteAttribute, define namespace attribute before value attribute") { id = 7, Pri = 1 } });
                }


                // for function attrNamespace_8
                {
                    this.AddChild(new CVariation(attrNamespace_8) { Attribute = new Variation("WriteAttribute, define namespace attribute after value attribute") { id = 8, Pri = 1 } });
                }


                // for function attrNamespace_9
                {
                    this.AddChild(new CVariation(attrNamespace_9) { Attribute = new Variation("WriteAttribute, redefine prefix at different scope and use both of them") { id = 9, Pri = 1 } });
                }


                // for function attrNamespace_10
                {
                    this.AddChild(new CVariation(attrNamespace_10) { Attribute = new Variation("WriteAttribute, redefine namespace at different scope and use both of them") { id = 10, Pri = 1 } });
                }


                // for function attrNamespace_11
                {
                    this.AddChild(new CVariation(attrNamespace_11) { Attribute = new Variation("WriteAttribute with colliding prefix with element") { id = 11, Pri = 1 } });
                }


                // for function attrNamespace_12
                {
                    this.AddChild(new CVariation(attrNamespace_12) { Attribute = new Variation("WriteAttribute with colliding namespace with element") { id = 12, Pri = 1 } });
                }


                // for function attrNamespace_13
                {
                    this.AddChild(new CVariation(attrNamespace_13) { Attribute = new Variation("WriteAttribute with namespace but no prefix") { id = 13, Pri = 1 } });
                }


                // for function attrNamespace_14
                {
                    this.AddChild(new CVariation(attrNamespace_14) { Attribute = new Variation("WriteAttribute for 2 attributes with same prefix but different namespace") { id = 14, Pri = 1 } });
                }


                // for function attrNamespace_15
                {
                    this.AddChild(new CVariation(attrNamespace_15) { Attribute = new Variation("WriteAttribute with String.Empty and null as namespace and prefix values") { id = 15, Pri = 1 } });
                }


                // for function attrNamespace_16
                {
                    this.AddChild(new CVariation(attrNamespace_16) { Attribute = new Variation("WriteAttribute to manually create attribute of xmlns:x") { id = 16, Pri = 1 } });
                }


                // for function attrNamespace_17
                {
                    this.AddChild(new CVariation(attrNamespace_17) { Attribute = new Variation("Bug 59657 testcase: WriteAttribute with namespace value = null while a prefix exists") { id = 17, Pri = 1 } });
                }


                // for function attrNamespace_18
                {
                    this.AddChild(new CVariation(attrNamespace_18) { Attribute = new Variation("Bug 59657 testcase: WriteAttribute with namespace value = String.Empty while a prefix exists") { id = 18, Pri = 1 } });
                }


                // for function attrNamespace_19
                {
                    this.AddChild(new CVariation(attrNamespace_19) { Attribute = new Variation("WriteAttribe in nested elements with same namespace but different prefix") { id = 19, Pri = 1 } });
                }


                // for function attrNamespace_20
                {
                    this.AddChild(new CVariation(attrNamespace_20) { Attribute = new Variation("WriteAttribute for x:a and xmlns:a diff namespace") { id = 20, Pri = 1 } });
                }


                // for function attrNamespace_21
                {
                    this.AddChild(new CVariation(attrNamespace_21) { Attribute = new Variation("WriteAttribute for x:a and xmlns:a same namespace") { id = 21, Pri = 1 } });
                }


                // for function attrNamespace_22
                {
                    this.AddChild(new CVariation(attrNamespace_22) { Attribute = new Variation("WriteAttribute with colliding NS and prefix for 2 attributes") { id = 22, Pri = 1 } });
                }


                // for function attrNamespace_23
                {
                    this.AddChild(new CVariation(attrNamespace_23) { Attribute = new Variation("WriteAttribute with DQ in namespace") { id = 23, Pri = 2 } });
                }


                // for function attrNamespace_24
                {
                    this.AddChild(new CVariation(attrNamespace_24) { Attribute = new Variation("Bug 57606 testcase: Attach prefix with empty namespace") { id = 24, Pri = 1 } });
                }


                // for function attrNamespace_25
                {
                    this.AddChild(new CVariation(attrNamespace_25) { Attribute = new Variation("Explicitly write namespace attribute that maps XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix") { id = 25, Pri = 1 } });
                }


                // for function attrNamespace_26
                {
                    this.AddChild(new CVariation(attrNamespace_26) { Attribute = new Variation("Map XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix") { id = 26, Pri = 1 } });
                }


                // for function attrNamespace_27
                {
                    this.AddChild(new CVariation(attrNamespace_27) { Attribute = new Variation("DCR 61633 testcase: Pass empty namespace to WriteAttributeString(prefix, name, ns, value)") { id = 27, Pri = 1 } });
                }


                // for function attrNamespace_28
                {
                    this.AddChild(new CVariation(attrNamespace_28) { Attribute = new Variation("Write attribute with prefix = xmlns") { id = 28, Pri = 1 } });
                }


                // for function attrNamespace_29
                {
                    this.AddChild(new CVariation(attrNamespace_29) { Attribute = new Variation("Write attribute in reserved XML namespace, should error") { id = 29, Pri = 1 } });
                }


                // for function attrNamespace_30
                {
                    this.AddChild(new CVariation(attrNamespace_30) { Attribute = new Variation("Write attribute in reserved XMLNS namespace, should error") { id = 30, Pri = 1 } });
                }


                // for function attrNamespace_31
                {
                    this.AddChild(new CVariation(attrNamespace_31) { Attribute = new Variation("bug 110206: WriteAttributeString with no namespace under element with empty prefix") { id = 31, Pri = 1 } });
                }


                // for function attrNamespace_32
                {
                    this.AddChild(new CVariation(attrNamespace_32) { Attribute = new Variation("Pass null prefix to WriteAttributeString()") { id = 32, Pri = 1 } });
                }


                // for function attrNamespace_33
                {
                    this.AddChild(new CVariation(attrNamespace_33) { Attribute = new Variation("Pass String.Empty prefix to WriteAttributeString()") { id = 33, Pri = 1 } });
                }


                // for function attrNamespace_34
                {
                    this.AddChild(new CVariation(attrNamespace_34) { Attribute = new Variation("Pass null ns to WriteAttributeString()") { id = 34, Pri = 1 } });
                }


                // for function attrNamespace_35
                {
                    this.AddChild(new CVariation(attrNamespace_35) { Attribute = new Variation("Pass String.Empty ns to WriteAttributeString()") { id = 35, Pri = 1 } });
                }


                // for function attrNamespace_36
                {
                    this.AddChild(new CVariation(attrNamespace_36) { Attribute = new Variation("Pass null prefix to WriteAttributeString() when namespace is in scope") { id = 36, Pri = 1 } });
                }


                // for function attrNamespace_37
                {
                    this.AddChild(new CVariation(attrNamespace_37) { Attribute = new Variation("Pass String.Empty prefix to WriteAttributeString() when namespace is in scope") { id = 37, Pri = 1 } });
                }


                // for function attrNamespace_38
                {
                    this.AddChild(new CVariation(attrNamespace_38) { Attribute = new Variation("Pass null ns to WriteAttributeString() when prefix is in scope") { id = 38, Pri = 1 } });
                }


                // for function attrNamespace_39
                {
                    this.AddChild(new CVariation(attrNamespace_39) { Attribute = new Variation("Pass String.Empty ns to WriteAttributeString() when prefix is in scope") { id = 39, Pri = 1 } });
                }


                // for function attrNamespace_40
                {
                    this.AddChild(new CVariation(attrNamespace_40) { Attribute = new Variation("Mapping empty ns uri to a prefix should error") { id = 40, Pri = 1 } });
                }


                // for function attrNamespace_42
                {
                    this.AddChild(new CVariation(attrNamespace_42) { Attribute = new Variation("WriteStartAttribute with prefix = null, localName = xmlns - case 2") { id = 42, Pri = 1 } });
                }
            }
        }
        public partial class TCCData : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCCData
            // Test Case
            public override void AddChildren()
            {
                // for function CData_1
                {
                    this.AddChild(new CVariation(CData_1) { Attribute = new Variation("WriteCData with null") { id = 1, Pri = 1 } });
                }


                // for function CData_2
                {
                    this.AddChild(new CVariation(CData_2) { Attribute = new Variation("WriteCData with String.Empty") { id = 2, Pri = 1 } });
                }


                // for function CData_3
                {
                    this.AddChild(new CVariation(CData_3) { Attribute = new Variation("WriteCData Sanity test") { id = 3, Pri = 0 } });
                }


                // for function CData_4
                {
                    this.AddChild(new CVariation(CData_4) { Attribute = new Variation("WriteCData with valid surrogate pair") { id = 4, Pri = 1 } });
                }


                // for function CData_5
                {
                    this.AddChild(new CVariation(CData_5) { Attribute = new Variation("WriteCData with ]]>") { id = 5, Pri = 1 } });
                }


                // for function CData_6
                {
                    this.AddChild(new CVariation(CData_6) { Attribute = new Variation("WriteCData with & < > chars, they should not be escaped") { id = 6, Pri = 2 } });
                }


                // for function CData_7
                {
                    this.AddChild(new CVariation(CData_7) { Attribute = new Variation("WriteCData with <![CDATA[") { id = 7, Pri = 2 } });
                }


                // for function CData_8
                {
                    this.AddChild(new CVariation(CData_8) { Attribute = new Variation("CData state machine") { id = 8, Pri = 2 } });
                }


                // for function CData_9
                {
                    this.AddChild(new CVariation(CData_9) { Attribute = new Variation("WriteCData with invalid surrogate pair") { id = 9, Pri = 1 } });
                }


                // for function CData_10
                {
                    this.AddChild(new CVariation(CData_10) { Attribute = new Variation("WriteCData after root element") { id = 10 } });
                }


                // for function CData_11
                {
                    this.AddChild(new CVariation(CData_11) { Attribute = new Variation("Call WriteCData twice - that should write two CData blocks") { id = 11, Pri = 1 } });
                }


                // for function CData_12
                {
                    this.AddChild(new CVariation(CData_12) { Attribute = new Variation("WriteCData with empty string at the buffer boundary") { id = 12, Pri = 1 } });
                }


                // for function CData_13
                {
                    this.AddChild(new CVariation(CData_13) { Attribute = new Variation("WriteCData with 0x0A with NewLineHandling.Entitize") { Params = new object[] { 10, NewLineHandling.Entitize, "<r><![CDATA[\n]]></r>" }, id = 18, Pri = 1 } });
                    this.AddChild(new CVariation(CData_13) { Attribute = new Variation("WriteCData with 0x0D with NewLineHandling.Replace") { Params = new object[] { 13, NewLineHandling.Replace, string.Format("<r><![CDATA[{0}]]></r>", Environment.NewLine) }, id = 13, Pri = 1 } });
                    this.AddChild(new CVariation(CData_13) { Attribute = new Variation("WriteCData with 0x0A with NewLineHandling.Replace") { Params = new object[] { 10, NewLineHandling.Replace, string.Format("<r><![CDATA[{0}]]></r>", Environment.NewLine) }, id = 16, Pri = 1 } });
                    this.AddChild(new CVariation(CData_13) { Attribute = new Variation("WriteCData with 0x0A with NewLineHandling.None") { Params = new object[] { 10, NewLineHandling.None, "<r><![CDATA[\n]]></r>" }, id = 17, Pri = 1 } });
                    this.AddChild(new CVariation(CData_13) { Attribute = new Variation("WriteCData with 0x0D with NewLineHandling.Entitize") { Params = new object[] { 13, NewLineHandling.Entitize, "<r><![CDATA[\r]]></r>" }, id = 15, Pri = 1 } });
                    this.AddChild(new CVariation(CData_13) { Attribute = new Variation("WriteCData with 0x0D with NewLineHandling.None") { Params = new object[] { 13, NewLineHandling.None, "<r><![CDATA[\r]]></r>" }, id = 14, Pri = 1 } });
                }
            }
        }
        public partial class TCComment : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCComment
            // Test Case
            public override void AddChildren()
            {
                // for function comment_1
                {
                    this.AddChild(new CVariation(comment_1) { Attribute = new Variation("Sanity test for WriteComment") { id = 1, Pri = 0 } });
                }


                // for function comment_2
                {
                    this.AddChild(new CVariation(comment_2) { Attribute = new Variation("Comment value = String.Empty") { id = 2, Pri = 0 } });
                }


                // for function comment_3
                {
                    this.AddChild(new CVariation(comment_3) { Attribute = new Variation("Comment value = null") { id = 3, Pri = 0 } });
                }


                // for function comment_4
                {
                    this.AddChild(new CVariation(comment_4) { Attribute = new Variation("WriteComment with valid surrogate pair") { id = 4, Pri = 1 } });
                }


                // for function comment_5
                {
                    this.AddChild(new CVariation(comment_5) { Attribute = new Variation("WriteComment with invalid surrogate pair") { id = 5, Pri = 1 } });
                }


                // for function comment_6
                {
                    this.AddChild(new CVariation(comment_6) { Attribute = new Variation("WriteComment with -- in value") { id = 6, Pri = 1 } });
                }
            }
        }
        public partial class TCEntityRef : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCEntityRef
            // Test Case
            public override void AddChildren()
            {
                // for function entityRef_1
                {
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with value = String.Empty") { Param = "String.Empty", id = 2, Pri = 1 } });
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with value = null") { Param = "null", id = 1, Pri = 1 } });
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with invalid value DQ") { Param = "test\"test", id = 8, Pri = 1 } });
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with invalid value <;") { Param = "test<test", id = 3, Pri = 1 } });
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with invalid value >") { Param = "test>test", id = 4, Pri = 1 } });
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with invalid value &") { Param = "test&test", id = 5, Pri = 1 } });
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with invalid value & and ;") { Param = "&test;", id = 6, Pri = 1 } });
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with invalid value SQ") { Param = "test'test", id = 7, Pri = 1 } });
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with #xD#xA") { Param = "\r\n", id = 11, Pri = 1 } });
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with #xD") { Param = "\r", id = 9, Pri = 1 } });
                    this.AddChild(new CVariation(entityRef_1) { Attribute = new Variation("WriteEntityRef with #xA") { Param = "\r", id = 10, Pri = 1 } });
                }
            }
        }
        public partial class TCCharEntity : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCCharEntity
            // Test Case
            public override void AddChildren()
            {
                // for function charEntity_1
                {
                    this.AddChild(new CVariation(charEntity_1) { Attribute = new Variation("WriteCharEntity with valid Unicode character") { id = 1, Pri = 0 } });
                }


                // for function charEntity_2
                {
                    this.AddChild(new CVariation(charEntity_2) { Attribute = new Variation("Call WriteCharEntity after WriteStartElement/WriteEndElement") { id = 2, Pri = 0 } });
                }


                // for function charEntity_3
                {
                    this.AddChild(new CVariation(charEntity_3) { Attribute = new Variation("Call WriteCharEntity after WriteStartAttribute/WriteEndAttribute") { id = 3, Pri = 0 } });
                }


                // for function charEntity_4
                {
                    this.AddChild(new CVariation(charEntity_4) { Attribute = new Variation("Character from low surrogate range") { id = 4, Pri = 1 } });
                }


                // for function charEntity_5
                {
                    this.AddChild(new CVariation(charEntity_5) { Attribute = new Variation("Character from high surrogate range") { id = 5, Pri = 1 } });
                }


                // for function charEntity_7
                {
                    this.AddChild(new CVariation(charEntity_7) { Attribute = new Variation("Sanity test, pass 'a'") { id = 7, Pri = 0 } });
                }


                // for function charEntity_8
                {
                    this.AddChild(new CVariation(charEntity_8) { Attribute = new Variation("WriteCharEntity for special attributes") { id = 8, Pri = 1 } });
                }


                // for function bug35637
                {
                    this.AddChild(new CVariation(bug35637) { Attribute = new Variation("35637: XmlWriter generates invalid XML") { id = 9, Pri = 1 } });
                }
            }
        }
        public partial class TCSurrogateCharEntity : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCSurrogateCharEntity
            // Test Case
            public override void AddChildren()
            {
                // for function surrogateEntity_1
                {
                    this.AddChild(new CVariation(surrogateEntity_1) { Attribute = new Variation("SurrogateCharEntity after WriteStartElement/WriteEndElement") { id = 1, Pri = 1 } });
                }


                // for function surrogateEntity_2
                {
                    this.AddChild(new CVariation(surrogateEntity_2) { Attribute = new Variation("SurrogateCharEntity after WriteStartAttribute/WriteEndAttribute") { id = 2, Pri = 1 } });
                }


                // for function surrogateEntity_3
                {
                    this.AddChild(new CVariation(surrogateEntity_3) { Attribute = new Variation("Test with limits of surrogate range") { id = 3, Pri = 1 } });
                }


                // for function surrogateEntity_4
                {
                    this.AddChild(new CVariation(surrogateEntity_4) { Attribute = new Variation("Middle surrogate character") { id = 4, Pri = 1 } });
                }


                // for function surrogateEntity_5
                {
                    this.AddChild(new CVariation(surrogateEntity_5) { Attribute = new Variation("Invalid high surrogate character") { id = 5, Pri = 1 } });
                }


                // for function surrogateEntity_6
                {
                    this.AddChild(new CVariation(surrogateEntity_6) { Attribute = new Variation("Invalid low surrogate character") { id = 6, Pri = 1 } });
                }


                // for function surrogateEntity_7
                {
                    this.AddChild(new CVariation(surrogateEntity_7) { Attribute = new Variation("Swap high-low surrogate characters") { id = 7, Pri = 1 } });
                }


                // for function surrogateEntity_8
                {
                    this.AddChild(new CVariation(surrogateEntity_8) { Attribute = new Variation("WriteSurrogateCharEntity for special attributes") { id = 8, Pri = 1 } });
                }
            }
        }
        public partial class TCPI : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCPI
            // Test Case
            public override void AddChildren()
            {
                // for function pi_1
                {
                    this.AddChild(new CVariation(pi_1) { Attribute = new Variation("Sanity test for WritePI") { id = 1, Pri = 0 } });
                }


                // for function pi_2
                {
                    this.AddChild(new CVariation(pi_2) { Attribute = new Variation("PI text value = null") { id = 2, Pri = 1 } });
                }


                // for function pi_3
                {
                    this.AddChild(new CVariation(pi_3) { Attribute = new Variation("PI text value = String.Empty") { id = 3, Pri = 1 } });
                }


                // for function pi_4
                {
                    this.AddChild(new CVariation(pi_4) { Attribute = new Variation("PI name = null should error") { id = 4, Pri = 1 } });
                }


                // for function pi_5
                {
                    this.AddChild(new CVariation(pi_5) { Attribute = new Variation("PI name = String.Empty should error") { id = 5, Pri = 1 } });
                }


                // for function pi_6
                {
                    this.AddChild(new CVariation(pi_6) { Attribute = new Variation("WritePI with xmlns as the name value") { id = 6 } });
                }


                // for function pi_7
                {
                    this.AddChild(new CVariation(pi_7) { Attribute = new Variation("WritePI with XmL as the name value") { id = 7 } });
                }


                // for function pi_8
                {
                    this.AddChild(new CVariation(pi_8) { Attribute = new Variation("WritePI before XmlDecl") { id = 8, Pri = 1 } });
                }


                // for function pi_9
                {
                    this.AddChild(new CVariation(pi_9) { Attribute = new Variation("WritePI (after StartDocument) with name = 'xml' text = 'version = 1.0' should error") { id = 9, Pri = 1 } });
                }


                // for function pi_10
                {
                    this.AddChild(new CVariation(pi_10) { Attribute = new Variation("WritePI (before StartDocument) with name = 'xml' text = 'version = 1.0' should error") { id = 10, Pri = 1 } });
                }


                // for function pi_11
                {
                    this.AddChild(new CVariation(pi_11) { Attribute = new Variation("Include PI end tag ?> as part of the text value") { id = 11, Pri = 1 } });
                }


                // for function pi_12
                {
                    this.AddChild(new CVariation(pi_12) { Attribute = new Variation("WriteProcessingInstruction with valid surrogate pair") { id = 12, Pri = 1 } });
                }


                // for function pi_13
                {
                    this.AddChild(new CVariation(pi_13) { Attribute = new Variation("WritePI with invalid surrogate pair") { id = 13, Pri = 1 } });
                }
            }
        }
        public partial class TCWriteNmToken : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCWriteNmToken
            // Test Case
            public override void AddChildren()
            {
                // for function writeNmToken_1
                {
                    this.AddChild(new CVariation(writeNmToken_1) { Attribute = new Variation("Name = null") { Param = "null", id = 1, Pri = 1 } });
                    this.AddChild(new CVariation(writeNmToken_1) { Attribute = new Variation("Name = String.Empty") { Param = "String.Empty", id = 2, Pri = 1 } });
                }


                // for function writeNmToken_2
                {
                    this.AddChild(new CVariation(writeNmToken_2) { Attribute = new Variation("Sanity test, Name = foo") { id = 2, Pri = 1 } });
                }


                // for function writeNmToken_3
                {
                    this.AddChild(new CVariation(writeNmToken_3) { Attribute = new Variation("Name contains letters, digits, . _ - : chars") { id = 3, Pri = 1 } });
                }


                // for function writeNmToken_4
                {
                    this.AddChild(new CVariation(writeNmToken_4) { Attribute = new Variation("Name contains SQ") { Param = "test'", id = 6, Pri = 1 } });
                    this.AddChild(new CVariation(writeNmToken_4) { Attribute = new Variation("Name contains DQ") { Param = "\"test", id = 7, Pri = 1 } });
                    this.AddChild(new CVariation(writeNmToken_4) { Attribute = new Variation("Name contains whitespace char") { Param = "test test", id = 4, Pri = 1 } });
                    this.AddChild(new CVariation(writeNmToken_4) { Attribute = new Variation("Name contains ? char") { Param = "test?", id = 5, Pri = 1 } });
                }
            }
        }
        public partial class TCWriteName : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCWriteName
            // Test Case
            public override void AddChildren()
            {
                // for function writeName_1
                {
                    this.AddChild(new CVariation(writeName_1) { Attribute = new Variation("Name = null") { Param = "null", id = 1, Pri = 1 } });
                    this.AddChild(new CVariation(writeName_1) { Attribute = new Variation("Name = String.Empty") { Param = "String.Empty", id = 2, Pri = 1 } });
                }


                // for function writeName_2
                {
                    this.AddChild(new CVariation(writeName_2) { Attribute = new Variation("Sanity test, Name = foo") { id = 3, Pri = 1 } });
                }


                // for function writeName_3
                {
                    this.AddChild(new CVariation(writeName_3) { Attribute = new Variation("Sanity test, Name = foo:bar") { id = 3, Pri = 1 } });
                }


                // for function writeName_4
                {
                    this.AddChild(new CVariation(writeName_4) { Attribute = new Variation("Name contains whitespace char") { Param = "foo bar", id = 5, Pri = 1 } });
                    this.AddChild(new CVariation(writeName_4) { Attribute = new Variation("Name starts with :") { Param = ":bar", id = 4, Pri = 1 } });
                }
            }
        }
        public partial class TCWriteQName : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCWriteQName
            // Test Case
            public override void AddChildren()
            {
                // for function writeQName_1
                {
                    this.AddChild(new CVariation(writeQName_1) { Attribute = new Variation("Name = String.Empty") { Param = "String.Empty", id = 2, Pri = 1 } });
                    this.AddChild(new CVariation(writeQName_1) { Attribute = new Variation("Name = null") { Param = "null", id = 1, Pri = 1 } });
                }


                // for function writeQName_2
                {
                    this.AddChild(new CVariation(writeQName_2) { Attribute = new Variation("WriteQName with correct NS") { id = 3, Pri = 1 } });
                }


                // for function writeQName_3
                {
                    this.AddChild(new CVariation(writeQName_3) { Attribute = new Variation("WriteQName when NS is auto-generated") { id = 4, Pri = 1 } });
                }


                // for function writeQName_4
                {
                    this.AddChild(new CVariation(writeQName_4) { Attribute = new Variation("QName = foo:bar when foo is not in scope") { id = 5, Pri = 1 } });
                }


                // for function writeQName_5
                {
                    this.AddChild(new CVariation(writeQName_5) { Attribute = new Variation("Name contains whitespace char") { Param = "foo bar", id = 7, Pri = 1 } });
                    this.AddChild(new CVariation(writeQName_5) { Attribute = new Variation("Name starts with :") { Param = ":bar", id = 6, Pri = 1 } });
                }
            }
        }
        public partial class TCWriteChars : TCWriteBuffer
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCWriteChars
            // Test Case
            public override void AddChildren()
            {
                // for function writeChars_1
                {
                    this.AddChild(new CVariation(writeChars_1) { Attribute = new Variation("WriteChars with valid buffer, number, count") { id = 1, Pri = 0 } });
                }


                // for function writeChars_2
                {
                    this.AddChild(new CVariation(writeChars_2) { Attribute = new Variation("WriteChars with & < >") { id = 2, Pri = 1 } });
                }


                // for function writeChars_3
                {
                    this.AddChild(new CVariation(writeChars_3) { Attribute = new Variation("WriteChars following WriteStartAttribute") { id = 3, Pri = 1 } });
                }


                // for function writeChars_4
                {
                    this.AddChild(new CVariation(writeChars_4) { Attribute = new Variation("WriteChars with entity ref included") { id = 4, Pri = 1 } });
                }


                // for function writeChars_5
                {
                    this.AddChild(new CVariation(writeChars_5) { Attribute = new Variation("WriteChars with buffer = null") { id = 5, Pri = 2 } });
                }


                // for function writeChars_6
                {
                    this.AddChild(new CVariation(writeChars_6) { Attribute = new Variation("WriteChars with count > buffer size") { id = 6, Pri = 1 } });
                }


                // for function writeChars_7
                {
                    this.AddChild(new CVariation(writeChars_7) { Attribute = new Variation("WriteChars with count < 0") { id = 7, Pri = 1 } });
                }


                // for function writeChars_8
                {
                    this.AddChild(new CVariation(writeChars_8) { Attribute = new Variation("WriteChars with index > buffer size") { id = 8, Pri = 1 } });
                }


                // for function writeChars_9
                {
                    this.AddChild(new CVariation(writeChars_9) { Attribute = new Variation("WriteChars with index < 0") { id = 9, Pri = 1 } });
                }


                // for function writeChars_10
                {
                    this.AddChild(new CVariation(writeChars_10) { Attribute = new Variation("WriteChars with index + count exceeds buffer") { id = 10, Pri = 1 } });
                }


                // for function writeChars_11
                {
                    this.AddChild(new CVariation(writeChars_11) { Attribute = new Variation("WriteChars for xml:lang attribute, index = count = 0") { id = 11, Pri = 1 } });
                }
            }
        }
        public partial class TCWriteString : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCWriteString
            // Test Case
            public override void AddChildren()
            {
                // for function writeString_1
                {
                    this.AddChild(new CVariation(writeString_1) { Attribute = new Variation("WriteString(null)") { id = 1, Pri = 0 } });
                }


                // for function writeString_2
                {
                    this.AddChild(new CVariation(writeString_2) { Attribute = new Variation("WriteString(String.Empty)") { id = 2, Pri = 1 } });
                }


                // for function writeString_3
                {
                    this.AddChild(new CVariation(writeString_3) { Attribute = new Variation("WriteString with valid surrogate pair") { id = 3, Pri = 1 } });
                }


                // for function writeString_4
                {
                    this.AddChild(new CVariation(writeString_4) { Attribute = new Variation("WriteString with invalid surrogate pair") { id = 4, Pri = 1 } });
                }


                // for function writeString_5
                {
                    this.AddChild(new CVariation(writeString_5) { Attribute = new Variation("WriteString with entity reference") { id = 5, Pri = 1 } });
                }


                // for function writeString_6
                {
                    this.AddChild(new CVariation(writeString_6) { Attribute = new Variation("WriteString with single/double quote, &, <, >") { id = 6, Pri = 1 } });
                }


                // for function writeString_9
                {
                    this.AddChild(new CVariation(writeString_9) { Attribute = new Variation("WriteString for value greater than x1F") { id = 9, Pri = 1 } });
                }


                // for function writeString_10
                {
                    this.AddChild(new CVariation(writeString_10) { Attribute = new Variation("WriteString with CR, LF, CR LF inside element") { id = 10, Pri = 1 } });
                }


                // for function writeString_11
                {
                    this.AddChild(new CVariation(writeString_11) { Attribute = new Variation("WriteString with CR, LF, CR LF inside attribute value") { id = 11, Pri = 1 } });
                }


                // for function writeString_12
                {
                    this.AddChild(new CVariation(writeString_12) { Attribute = new Variation("Bug 54499 testcase: Call WriteString for LF inside attribute") { id = 12, Pri = 1 } });
                }


                // for function writeString_13
                {
                    this.AddChild(new CVariation(writeString_13) { Attribute = new Variation("Surrogate charaters in text nodes, range limits") { id = 13, Pri = 1 } });
                }


                // for function writeString_14
                {
                    this.AddChild(new CVariation(writeString_14) { Attribute = new Variation("High surrogate on last position") { id = 14, Pri = 1 } });
                }


                // for function writeString_15
                {
                    this.AddChild(new CVariation(writeString_15) { Attribute = new Variation("Low surrogate on first position") { id = 15, Pri = 1 } });
                }


                // for function writeString_16
                {
                    this.AddChild(new CVariation(writeString_16) { Attribute = new Variation("Swap low-high surrogates") { id = 16, Pri = 1 } });
                }
            }
        }
        public partial class TCWhiteSpace : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCWhiteSpace
            // Test Case
            public override void AddChildren()
            {
                // for function whitespace_1
                {
                    this.AddChild(new CVariation(whitespace_1) { Attribute = new Variation("WriteWhitespace with values #x20 #x9 #xD #xA") { id = 1, Pri = 1 } });
                }


                // for function whitespace_2
                {
                    this.AddChild(new CVariation(whitespace_2) { Attribute = new Variation("WriteWhitespace in the middle of text") { id = 2, Pri = 1 } });
                }


                // for function whitespace_3
                {
                    this.AddChild(new CVariation(whitespace_3) { Attribute = new Variation("WriteWhitespace before and after root element") { id = 3, Pri = 1 } });
                }


                // for function whitespace_4
                {
                    this.AddChild(new CVariation(whitespace_4) { Attribute = new Variation("281444.WriteWhitespace with null ") { Param = "null", id = 4, Pri = 1 } });
                    this.AddChild(new CVariation(whitespace_4) { Attribute = new Variation("281444.WriteWhitespace with String.Empty ") { Param = "String.Empty", id = 5, Pri = 1 } });
                }


                // for function whitespace_5
                {
                    this.AddChild(new CVariation(whitespace_5) { Attribute = new Variation("WriteWhitespace with invalid char") { Param = "\u001f", id = 10, Pri = 1 } });
                    this.AddChild(new CVariation(whitespace_5) { Attribute = new Variation("WriteWhitespace with invalid char") { Param = "\0", id = 8, Pri = 1 } });
                    this.AddChild(new CVariation(whitespace_5) { Attribute = new Variation("WriteWhitespace with invalid char") { Param = "\u0010", id = 9, Pri = 1 } });
                    this.AddChild(new CVariation(whitespace_5) { Attribute = new Variation("WriteWhitespace with invalid char") { Param = "a", id = 6, Pri = 1 } });
                    this.AddChild(new CVariation(whitespace_5) { Attribute = new Variation("WriteWhitespace with invalid char") { Param = "\u000e", id = 7, Pri = 1 } });
                }
            }
        }
        public partial class TCWriteValue : XmlWriterTestCaseBase
        {
            // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue
            // Test Case
            public override void AddChildren()
            {
                // for function writeValue_1
                {
                    this.AddChild(new CVariation(writeValue_1) { Attribute = new Variation("Write multiple atomic values inside element") { Pri = 1 } });
                }


                // for function writeValue_2
                {
                    this.AddChild(new CVariation(writeValue_2) { Attribute = new Variation("Write multiple atomic values inside attribute") { Pri = 1 } });
                }


                // for function writeValue_3
                {
                    this.AddChild(new CVariation(writeValue_3) { Attribute = new Variation("Write multiple atomic values inside element, seperate by WriteWhitespace(' ')") { Pri = 1 } });
                }


                // for function writeValue_4
                {
                    this.AddChild(new CVariation(writeValue_4) { Attribute = new Variation("Write multiple atomic values inside element, seperate by WriteString(' ')") { Pri = 1 } });
                }


                // for function writeValue_5
                {
                    this.AddChild(new CVariation(writeValue_5) { Attribute = new Variation("Write multiple atomic values inside attribute, separate by WriteWhitespace(' ')") { Pri = 1 } });
                }


                // for function writeValue_6
                {
                    this.AddChild(new CVariation(writeValue_6) { Attribute = new Variation("Write multiple atomic values inside attribute, seperate by WriteString(' ')") { Pri = 1 } });
                }


                // for function writeValue_7
                {
                    this.AddChild(new CVariation(writeValue_7) { Attribute = new Variation("WriteValue(long)") { Pri = 1 } });
                }


                // for function writeValue_8
                {
                    this.AddChild(new CVariation(writeValue_8) { Attribute = new Variation("WriteValue((object)null)") { Param = "object", Pri = 1 } });
                    this.AddChild(new CVariation(writeValue_8) { Attribute = new Variation("WriteValue((string)null)") { Param = "string", Pri = 1 } });
                }


                // for function writeValue_27
                {
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToDecimal)") { Params = new object[] { 2, "UInt32", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToUInt64)") { Params = new object[] { 1, "ByteArray", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToUInt64)") { Params = new object[] { 1, "TimeSpan", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToUInt64)") { Params = new object[] { 1, "Uri", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToUInt64)") { Params = new object[] { 1, "Double", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToUInt64)") { Params = new object[] { 1, "Single", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToUInt64)") { Params = new object[] { 1, "XmlQualifiedName", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToUInt64)") { Params = new object[] { 1, "string", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToInt64)") { Params = new object[] { 1, "UInt64", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToInt64)") { Params = new object[] { 1, "UInt32", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToInt64)") { Params = new object[] { 1, "UInt16", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToInt64)") { Params = new object[] { 1, "Int64", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToInt64)") { Params = new object[] { 1, "Int32", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToInt64)") { Params = new object[] { 1, "Int16", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToInt64)") { Params = new object[] { 1, "Byte", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToInt64)") { Params = new object[] { 1, "SByte", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToInt64)") { Params = new object[] { 1, "Decimal", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToInt64)") { Params = new object[] { 1, "float", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToInt64)") { Params = new object[] { 1, "object", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToInt64)") { Params = new object[] { 1, "bool", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToInt64)") { Params = new object[] { 1, "DateTime", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToInt64)") { Params = new object[] { 1, "DateTimeOffset", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToInt64)") { Params = new object[] { 1, "ByteArray", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToInt64)") { Params = new object[] { 1, "List", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToInt64)") { Params = new object[] { 1, "TimeSpan", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToInt64)") { Params = new object[] { 1, "Uri", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToInt64)") { Params = new object[] { 1, "Double", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToInt64)") { Params = new object[] { 1, "Single", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToInt64)") { Params = new object[] { 1, "XmlQualifiedName", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToInt64)") { Params = new object[] { 1, "string", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToUInt32)") { Params = new object[] { 1, "UInt64", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToUInt32)") { Params = new object[] { 1, "UInt32", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToUInt32)") { Params = new object[] { 1, "UInt16", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToUInt32)") { Params = new object[] { 1, "Int64", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToUInt32)") { Params = new object[] { 1, "Int32", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToUInt32)") { Params = new object[] { 1, "Int16", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToUInt32)") { Params = new object[] { 1, "Byte", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToUInt32)") { Params = new object[] { 1, "SByte", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToUInt32)") { Params = new object[] { 1, "Decimal", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToUInt32)") { Params = new object[] { 1, "float", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToUInt32)") { Params = new object[] { 1, "object", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToUInt32)") { Params = new object[] { 1, "bool", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToUInt32)") { Params = new object[] { 1, "DateTime", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToUInt32)") { Params = new object[] { 1, "DateTimeOffset", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToUInt32)") { Params = new object[] { 1, "ByteArray", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToUInt32)") { Params = new object[] { 1, "List", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToUInt32)") { Params = new object[] { 1, "TimeSpan", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToUInt32)") { Params = new object[] { 1, "Uri", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToUInt32)") { Params = new object[] { 1, "Double", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToUInt32)") { Params = new object[] { 1, "Single", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToUInt32)") { Params = new object[] { 1, "XmlQualifiedName", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToUInt32)") { Params = new object[] { 1, "string", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToInt32)") { Params = new object[] { 1, "UInt64", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToInt32)") { Params = new object[] { 1, "UInt32", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToInt32)") { Params = new object[] { 1, "UInt16", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToInt32)") { Params = new object[] { 1, "Int64", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToInt32)") { Params = new object[] { 1, "Int32", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToInt32)") { Params = new object[] { 1, "Int16", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToInt32)") { Params = new object[] { 1, "Byte", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToInt32)") { Params = new object[] { 1, "SByte", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToInt32)") { Params = new object[] { 1, "Decimal", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToInt32)") { Params = new object[] { 1, "float", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToInt32)") { Params = new object[] { 1, "object", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToInt32)") { Params = new object[] { 1, "bool", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToInt32)") { Params = new object[] { 1, "DateTime", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToInt32)") { Params = new object[] { 1, "DateTimeOffset", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToInt32)") { Params = new object[] { 1, "ByteArray", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToInt32)") { Params = new object[] { 1, "List", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToInt32)") { Params = new object[] { 1, "TimeSpan", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToInt32)") { Params = new object[] { 1, "Uri", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToInt32)") { Params = new object[] { 1, "Double", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToInt32)") { Params = new object[] { 1, "Single", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToInt32)") { Params = new object[] { 1, "XmlQualifiedName", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToInt32)") { Params = new object[] { 1, "string", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToUInt16)") { Params = new object[] { 1, "UInt64", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToUInt16)") { Params = new object[] { 1, "UInt32", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToUInt16)") { Params = new object[] { 1, "UInt16", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToUInt16)") { Params = new object[] { 1, "Int64", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToUInt16)") { Params = new object[] { 1, "Int32", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToUInt16)") { Params = new object[] { 1, "Int16", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToUInt16)") { Params = new object[] { 1, "Byte", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToUInt16)") { Params = new object[] { 1, "SByte", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToUInt16)") { Params = new object[] { 1, "Decimal", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToUInt16)") { Params = new object[] { 1, "float", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToUInt16)") { Params = new object[] { 1, "object", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToUInt16)") { Params = new object[] { 1, "bool", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToUInt16)") { Params = new object[] { 1, "DateTime", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToUInt16)") { Params = new object[] { 1, "DateTimeOffset", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToUInt16)") { Params = new object[] { 1, "ByteArray", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToUInt16)") { Params = new object[] { 1, "List", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToUInt16)") { Params = new object[] { 1, "TimeSpan", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToUInt16)") { Params = new object[] { 1, "Uri", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToUInt16)") { Params = new object[] { 1, "Double", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToUInt16)") { Params = new object[] { 1, "Single", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToUInt16)") { Params = new object[] { 1, "XmlQualifiedName", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToUInt16)") { Params = new object[] { 1, "string", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToInt16)") { Params = new object[] { 1, "UInt64", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToInt16)") { Params = new object[] { 1, "UInt32", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToInt16)") { Params = new object[] { 1, "UInt16", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToInt16)") { Params = new object[] { 1, "Int64", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToInt16)") { Params = new object[] { 1, "Int32", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToInt16)") { Params = new object[] { 1, "Int16", "Int16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToInt16)") { Params = new object[] { 1, "Byte", "Int16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToInt16)") { Params = new object[] { 1, "SByte", "Int16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToInt16)") { Params = new object[] { 1, "Decimal", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToInt16)") { Params = new object[] { 1, "float", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToInt16)") { Params = new object[] { 1, "object", "Int16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToInt16)") { Params = new object[] { 1, "bool", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToInt16)") { Params = new object[] { 1, "DateTime", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToInt16)") { Params = new object[] { 1, "DateTimeOffset", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToInt16)") { Params = new object[] { 1, "ByteArray", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToInt16)") { Params = new object[] { 1, "List", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToInt16)") { Params = new object[] { 1, "TimeSpan", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToInt16)") { Params = new object[] { 1, "Uri", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToInt16)") { Params = new object[] { 1, "Double", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToInt16)") { Params = new object[] { 1, "Single", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToInt16)") { Params = new object[] { 1, "XmlQualifiedName", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToInt16)") { Params = new object[] { 1, "string", "Int16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToByte)") { Params = new object[] { 1, "UInt64", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToByte)") { Params = new object[] { 1, "UInt32", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToByte)") { Params = new object[] { 1, "UInt16", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToByte)") { Params = new object[] { 1, "Int64", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToByte)") { Params = new object[] { 1, "Int32", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToByte)") { Params = new object[] { 1, "Int16", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToByte)") { Params = new object[] { 1, "Byte", "Byte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToByte)") { Params = new object[] { 1, "SByte", "Byte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToByte)") { Params = new object[] { 1, "Decimal", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToByte)") { Params = new object[] { 1, "float", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToByte)") { Params = new object[] { 1, "object", "Byte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToByte)") { Params = new object[] { 1, "bool", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToByte)") { Params = new object[] { 1, "DateTime", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToByte)") { Params = new object[] { 1, "DateTimeOffset", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToByte)") { Params = new object[] { 1, "ByteArray", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToByte)") { Params = new object[] { 1, "List", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToByte)") { Params = new object[] { 1, "TimeSpan", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToByte)") { Params = new object[] { 1, "Uri", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToByte)") { Params = new object[] { 1, "Double", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToByte)") { Params = new object[] { 1, "Single", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToByte)") { Params = new object[] { 1, "XmlQualifiedName", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToByte)") { Params = new object[] { 1, "string", "Byte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToSByte)") { Params = new object[] { 1, "UInt64", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToSByte)") { Params = new object[] { 1, "UInt32", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToSByte)") { Params = new object[] { 1, "UInt16", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToSByte)") { Params = new object[] { 1, "Int64", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToSByte)") { Params = new object[] { 1, "Int32", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToSByte)") { Params = new object[] { 1, "Int16", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToSByte)") { Params = new object[] { 1, "Byte", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToSByte)") { Params = new object[] { 1, "SByte", "SByte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToSByte)") { Params = new object[] { 1, "Decimal", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToSByte)") { Params = new object[] { 1, "float", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToSByte)") { Params = new object[] { 1, "object", "SByte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToSByte)") { Params = new object[] { 1, "bool", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToSByte)") { Params = new object[] { 1, "DateTime", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToSByte)") { Params = new object[] { 1, "DateTimeOffset", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToSByte)") { Params = new object[] { 1, "ByteArray", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToSByte)") { Params = new object[] { 1, "List", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToSByte)") { Params = new object[] { 1, "TimeSpan", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToSByte)") { Params = new object[] { 1, "Uri", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToSByte)") { Params = new object[] { 1, "Double", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToSByte)") { Params = new object[] { 1, "Single", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToSByte)") { Params = new object[] { 1, "XmlQualifiedName", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToSByte)") { Params = new object[] { 1, "string", "SByte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToDecimal)") { Params = new object[] { 1, "UInt64", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToDecimal)") { Params = new object[] { 1, "UInt32", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToDecimal)") { Params = new object[] { 1, "UInt16", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToDecimal)") { Params = new object[] { 1, "Int64", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToDecimal)") { Params = new object[] { 1, "Int32", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToDecimal)") { Params = new object[] { 1, "Int16", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToDecimal)") { Params = new object[] { 1, "Byte", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToDecimal)") { Params = new object[] { 1, "SByte", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToDecimal)") { Params = new object[] { 1, "Decimal", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToDecimal)") { Params = new object[] { 1, "float", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToDecimal)") { Params = new object[] { 1, "object", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToDecimal)") { Params = new object[] { 1, "bool", "Decimal", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToDecimal)") { Params = new object[] { 1, "DateTime", "Decimal", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToDecimal)") { Params = new object[] { 1, "DateTimeOffset", "Decimal", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToDecimal)") { Params = new object[] { 1, "ByteArray", "Decimal", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToDecimal)") { Params = new object[] { 1, "List", "Decimal", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToDecimal)") { Params = new object[] { 1, "TimeSpan", "Decimal", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToDecimal)") { Params = new object[] { 1, "Uri", "Decimal", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToDecimal)") { Params = new object[] { 1, "Double", "Decimal", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToDecimal)") { Params = new object[] { 1, "Single", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToDecimal)") { Params = new object[] { 1, "XmlQualifiedName", "Decimal", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToDecimal)") { Params = new object[] { 1, "string", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToFloat)") { Params = new object[] { 1, "UInt64", "float", true, 1.844674E+19F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToFloat)") { Params = new object[] { 1, "UInt32", "float", true, 4.294967E+09F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToFloat)") { Params = new object[] { 1, "UInt16", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToFloat)") { Params = new object[] { 1, "Int64", "float", true, 9.223372E+18F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToFloat)") { Params = new object[] { 1, "Int32", "float", true, 2.147484E+09F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToFloat)") { Params = new object[] { 1, "Int16", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToFloat)") { Params = new object[] { 1, "Byte", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToFloat)") { Params = new object[] { 1, "SByte", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToFloat)") { Params = new object[] { 1, "Decimal", "float", true, 7.922816E+28 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToFloat)") { Params = new object[] { 1, "float", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToFloat)") { Params = new object[] { 1, "object", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToFloat)") { Params = new object[] { 1, "bool", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToFloat)") { Params = new object[] { 1, "DateTime", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToFloat)") { Params = new object[] { 1, "DateTimeOffset", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToFloat)") { Params = new object[] { 1, "ByteArray", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToFloat)") { Params = new object[] { 1, "List", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToFloat)") { Params = new object[] { 1, "TimeSpan", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToFloat)") { Params = new object[] { 1, "Uri", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToFloat)") { Params = new object[] { 1, "Double", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleTofloat)") { Params = new object[] { 1, "Single", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToFloat)") { Params = new object[] { 1, "XmlQualifiedName", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToFloat)") { Params = new object[] { 1, "string", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToBool)") { Params = new object[] { 1, "UInt64", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToBool)") { Params = new object[] { 1, "UInt32", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToBool)") { Params = new object[] { 1, "UInt16", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToBool)") { Params = new object[] { 1, "Int64", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToBool)") { Params = new object[] { 1, "Int32", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToBool)") { Params = new object[] { 1, "Int16", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToBool)") { Params = new object[] { 1, "Byte", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToBool)") { Params = new object[] { 1, "SByte", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToBool)") { Params = new object[] { 1, "Decimal", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToBool)") { Params = new object[] { 1, "float", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToBool)") { Params = new object[] { 1, "object", "bool", true, false } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToBool)") { Params = new object[] { 1, "bool", "bool", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToBool)") { Params = new object[] { 1, "DateTime", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToBool)") { Params = new object[] { 1, "DateTimeOffset", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToBool)") { Params = new object[] { 1, "ByteArray", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToBool)") { Params = new object[] { 1, "List", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToBool)") { Params = new object[] { 1, "TimeSpan", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToBool)") { Params = new object[] { 1, "Uri", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToBool)") { Params = new object[] { 1, "Double", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleTobool)") { Params = new object[] { 1, "Single", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToBool)") { Params = new object[] { 1, "XmlQualifiedName", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToBool)") { Params = new object[] { 1, "string", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToDateTime)") { Params = new object[] { 1, "UInt64", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToDateTime)") { Params = new object[] { 1, "UInt32", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToDateTime)") { Params = new object[] { 1, "UInt16", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToDateTime)") { Params = new object[] { 1, "Int64", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToDateTime)") { Params = new object[] { 1, "Int32", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToDateTime)") { Params = new object[] { 1, "Int16", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToDateTime)") { Params = new object[] { 1, "Byte", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToDateTime)") { Params = new object[] { 1, "SByte", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToDateTime)") { Params = new object[] { 1, "Decimal", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToDateTime)") { Params = new object[] { 1, "float", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToDateTime)") { Params = new object[] { 1, "object", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToDateTime)") { Params = new object[] { 1, "bool", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToDateTime)") { Params = new object[] { 1, "DateTime", "DateTime", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToDateTime)") { Params = new object[] { 1, "DateTimeOffset", "DateTime", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToDateTime)") { Params = new object[] { 1, "ByteArray", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToDateTime)") { Params = new object[] { 1, "List", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToDateTime)") { Params = new object[] { 1, "TimeSpan", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToDateTime)") { Params = new object[] { 1, "Uri", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToDateTime)") { Params = new object[] { 1, "Double", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToDateTime)") { Params = new object[] { 1, "Single", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToDateTime)") { Params = new object[] { 1, "XmlQualifiedName", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToDateTime)") { Params = new object[] { 1, "string", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToDateTimeOffset)") { Params = new object[] { 1, "UInt64", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToDateTimeOffset)") { Params = new object[] { 1, "UInt32", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToDateTimeOffset)") { Params = new object[] { 1, "UInt16", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToDateTimeOffset)") { Params = new object[] { 1, "Int64", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToDateTimeOffset)") { Params = new object[] { 1, "Int32", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToDateTimeOffset)") { Params = new object[] { 1, "Int16", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToDateTimeOffset)") { Params = new object[] { 1, "Byte", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToDateTimeOffset)") { Params = new object[] { 1, "SByte", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToDateTimeOffset)") { Params = new object[] { 1, "Decimal", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToDateTimeOffset)") { Params = new object[] { 1, "float", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToDateTimeOffset)") { Params = new object[] { 1, "object", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToDateTimeOffset)") { Params = new object[] { 1, "bool", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToDateTimeOffset)") { Params = new object[] { 1, "DateTime", "DateTimeOffset", true, 0 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToDateTimeOffset)") { Params = new object[] { 1, "DateTimeOffset", "DateTimeOffset", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToDateTimeOffset)") { Params = new object[] { 1, "ByteArray", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToDateTimeOffset)") { Params = new object[] { 1, "List", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToDateTimeOffset)") { Params = new object[] { 1, "TimeSpan", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToDateTimeOffset)") { Params = new object[] { 1, "Uri", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToDateTimeOffset)") { Params = new object[] { 1, "Double", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToDateTimeOffset)") { Params = new object[] { 1, "Single", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToDateTimeOffset)") { Params = new object[] { 1, "XmlQualifiedName", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToDateTimeOffset)") { Params = new object[] { 1, "string", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToList)") { Params = new object[] { 1, "UInt64", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToList)") { Params = new object[] { 1, "UInt32", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToList)") { Params = new object[] { 1, "UInt16", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToList)") { Params = new object[] { 1, "Int64", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToList)") { Params = new object[] { 1, "Int32", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToList)") { Params = new object[] { 1, "Int16", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToList)") { Params = new object[] { 1, "Byte", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToList)") { Params = new object[] { 1, "SByte", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToList)") { Params = new object[] { 1, "Decimal", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToList)") { Params = new object[] { 1, "float", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToList)") { Params = new object[] { 1, "object", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToList)") { Params = new object[] { 1, "bool", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToList)") { Params = new object[] { 1, "DateTime", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToList)") { Params = new object[] { 1, "DateTimeOffset", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToList)") { Params = new object[] { 1, "ByteArray", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToList)") { Params = new object[] { 1, "List", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToList)") { Params = new object[] { 1, "TimeSpan", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToList)") { Params = new object[] { 1, "Uri", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToList)") { Params = new object[] { 1, "Double", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToList)") { Params = new object[] { 1, "Single", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToList)") { Params = new object[] { 1, "XmlQualifiedName", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToList)") { Params = new object[] { 1, "string", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToUri)") { Params = new object[] { 1, "UInt64", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToUri)") { Params = new object[] { 1, "UInt32", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToUri)") { Params = new object[] { 1, "UInt16", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToUri)") { Params = new object[] { 1, "Int64", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToUri)") { Params = new object[] { 1, "Int32", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToUri)") { Params = new object[] { 1, "Int16", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToUri)") { Params = new object[] { 1, "Byte", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToUri)") { Params = new object[] { 1, "SByte", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToUri)") { Params = new object[] { 1, "Decimal", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToUri)") { Params = new object[] { 1, "float", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToUri)") { Params = new object[] { 1, "object", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToUri)") { Params = new object[] { 1, "bool", "Uri", true, "false" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToUri)") { Params = new object[] { 1, "DateTime", "Uri", true, 1 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToUri)") { Params = new object[] { 1, "DateTimeOffset", "Uri", true, 2 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToUri)") { Params = new object[] { 1, "ByteArray", "Uri", true, "2H4=" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToUri)") { Params = new object[] { 1, "List", "Uri", true, "" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToUri)") { Params = new object[] { 1, "TimeSpan", "Uri", true, "PT0S" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToUri)") { Params = new object[] { 1, "Uri", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToUri)") { Params = new object[] { 1, "Double", "Uri", true, "1.7976931348623157E+308" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToUri)") { Params = new object[] { 1, "Single", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToUri)") { Params = new object[] { 1, "XmlQualifiedName", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToUri)") { Params = new object[] { 1, "string", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToDouble)") { Params = new object[] { 1, "UInt64", "Double", true, 1.84467440737096E+19 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToDouble)") { Params = new object[] { 1, "UInt32", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToDouble)") { Params = new object[] { 1, "UInt16", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToDouble)") { Params = new object[] { 1, "Int64", "Double", true, 9.22337203685478E+18 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToDouble)") { Params = new object[] { 1, "Int32", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToDouble)") { Params = new object[] { 1, "Int16", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToDouble)") { Params = new object[] { 1, "Byte", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToDouble)") { Params = new object[] { 1, "SByte", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToDouble)") { Params = new object[] { 1, "Decimal", "Double", true, 7.92281625142643E+28 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToDouble)") { Params = new object[] { 1, "float", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToDouble)") { Params = new object[] { 1, "object", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToDouble)") { Params = new object[] { 1, "bool", "Double", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToDouble)") { Params = new object[] { 1, "DateTime", "Double", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToDouble)") { Params = new object[] { 1, "DateTimeOffset", "Double", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToDouble)") { Params = new object[] { 1, "ByteArray", "Double", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToDouble)") { Params = new object[] { 1, "List", "Double", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToDouble)") { Params = new object[] { 1, "TimeSpan", "Double", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToDouble)") { Params = new object[] { 1, "Uri", "Double", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToDouble)") { Params = new object[] { 1, "Double", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToDouble)") { Params = new object[] { 1, "Single", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToDouble)") { Params = new object[] { 1, "XmlQualifiedName", "Double", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToDouble)") { Params = new object[] { 1, "string", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToSingle)") { Params = new object[] { 1, "UInt64", "Single", true, 1.844674E+19F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToSingle)") { Params = new object[] { 1, "UInt32", "Single", true, 4.294967E+09F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToSingle)") { Params = new object[] { 1, "UInt16", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToSingle)") { Params = new object[] { 1, "Int64", "Single", true, 9.223372E+18F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToSingle)") { Params = new object[] { 1, "Int32", "Single", true, 2.147484E+09F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToSingle)") { Params = new object[] { 1, "Int16", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToSingle)") { Params = new object[] { 1, "Byte", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToSingle)") { Params = new object[] { 1, "SByte", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToSingle)") { Params = new object[] { 1, "Decimal", "Single", true, 7.922816E+28 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToSingle)") { Params = new object[] { 1, "float", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToSingle)") { Params = new object[] { 1, "object", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToSingle)") { Params = new object[] { 1, "bool", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToSingle)") { Params = new object[] { 1, "DateTime", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToSingle)") { Params = new object[] { 1, "DateTimeOffset", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToSingle)") { Params = new object[] { 1, "ByteArray", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToSingle)") { Params = new object[] { 1, "List", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToSingle)") { Params = new object[] { 1, "TimeSpan", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToSingle)") { Params = new object[] { 1, "Uri", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToSingle)") { Params = new object[] { 1, "Double", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToSingle)") { Params = new object[] { 1, "Single", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameToSingle)") { Params = new object[] { 1, "XmlQualifiedName", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToSingle)") { Params = new object[] { 1, "string", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToObject)") { Params = new object[] { 1, "UInt64", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToObject)") { Params = new object[] { 1, "UInt32", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToObject)") { Params = new object[] { 1, "UInt16", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToObject)") { Params = new object[] { 1, "Int64", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToObject)") { Params = new object[] { 1, "Int32", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToObject)") { Params = new object[] { 1, "Int16", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToObject)") { Params = new object[] { 1, "Byte", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToObject)") { Params = new object[] { 1, "SByte", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToObject)") { Params = new object[] { 1, "Decimal", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToObject)") { Params = new object[] { 1, "float", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToObject)") { Params = new object[] { 1, "object", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToObject)") { Params = new object[] { 1, "bool", "object", true, "false" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToObject)") { Params = new object[] { 1, "DateTime", "object", true, 1 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToObject)") { Params = new object[] { 1, "DateTimeOffset", "object", true, 2 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToObject)") { Params = new object[] { 1, "ByteArray", "object", true, "2H4=" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToObject)") { Params = new object[] { 1, "List", "object", true, "" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToObject)") { Params = new object[] { 1, "TimeSpan", "object", true, "PT0S" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToObject)") { Params = new object[] { 1, "Uri", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToObject)") { Params = new object[] { 1, "Double", "object", true, "1.7976931348623157E+308" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToObject)") { Params = new object[] { 1, "Single", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToObject)") { Params = new object[] { 1, "XmlQualifiedName", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToObject)") { Params = new object[] { 1, "string", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToByteArray)") { Params = new object[] { 1, "ByteArray", "ByteArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(BoolArrayToBoolArray)") { Params = new object[] { 1, "BoolArray", "BoolArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ObjectArrayToObjectArray)") { Params = new object[] { 1, "ObjectArray", "ObjectArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeArrayToDateTimeArray)") { Params = new object[] { 1, "DateTimeArray", "DateTimeArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetArrayToDateTimeOffsetArray)") { Params = new object[] { 1, "DateTimeOffsetArray", "DateTimeOffsetArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalArrayToDecimalArray)") { Params = new object[] { 1, "DecimalArray", "DecimalArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleArrayToDoubleArray)") { Params = new object[] { 1, "DoubleArray", "DoubleArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ArrayToInt16Array)") { Params = new object[] { 1, "Int16Array", "Int16Array", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ArrayToInt32Array)") { Params = new object[] { 1, "Int32Array", "Int32Array", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ArrayToInt64Array)") { Params = new object[] { 1, "Int64Array", "Int64Array", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteArrayToSByteArray)") { Params = new object[] { 1, "SByteArray", "SByteArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleArrayToSingleArray)") { Params = new object[] { 1, "SingleArray", "SingleArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(StringArrayToStringArray)") { Params = new object[] { 1, "StringArray", "StringArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanArrayToTimeSpanArray)") { Params = new object[] { 1, "TimeSpanArray", "TimeSpanArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ArrayToUInt16Array)") { Params = new object[] { 1, "UInt16Array", "UInt16Array", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ArrayToUInt32Array)") { Params = new object[] { 1, "UInt32Array", "UInt32Array", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ArrayToUInt64Array)") { Params = new object[] { 1, "UInt64Array", "UInt64Array", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriArrayToUriArray)") { Params = new object[] { 1, "UriArray", "UriArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameArrayToXmlQualifiedNameArray)") { Params = new object[] { 1, "XmlQualifiedNameArray", "XmlQualifiedNameArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToTimeSpan)") { Params = new object[] { 1, "TimeSpan", "TimeSpan", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameToXmlQualifiedName)") { Params = new object[] { 1, "XmlQualifiedName", "XmlQualifiedName", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToString)") { Params = new object[] { 2, "Int16", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToString)") { Params = new object[] { 2, "Byte", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToString)") { Params = new object[] { 2, "SByte", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToString)") { Params = new object[] { 2, "Decimal", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToString)") { Params = new object[] { 2, "float", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToString)") { Params = new object[] { 2, "object", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(boolToString)") { Params = new object[] { 2, "bool", "string", true, "False" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UriToString)") { Params = new object[] { 2, "Uri", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DoubleToString)") { Params = new object[] { 2, "Double", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleToString)") { Params = new object[] { 2, "Single", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameTypeToString)") { Params = new object[] { 2, "XmlQualifiedName", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToString)") { Params = new object[] { 2, "string", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToUInt64)") { Params = new object[] { 2, "UInt64", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToUInt64)") { Params = new object[] { 2, "UInt32", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToUInt64)") { Params = new object[] { 2, "UInt16", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToUInt64)") { Params = new object[] { 2, "Int64", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToUInt64)") { Params = new object[] { 2, "Int32", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToUInt64)") { Params = new object[] { 2, "Int16", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ListToUInt64)") { Params = new object[] { 2, "List", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(TimeSpanToUInt64)") { Params = new object[] { 2, "TimeSpan", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UriToUInt64)") { Params = new object[] { 2, "Uri", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DoubleToUInt64)") { Params = new object[] { 2, "Double", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleToUInt64)") { Params = new object[] { 2, "Single", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameTypeToUInt64)") { Params = new object[] { 2, "XmlQualifiedName", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToUInt64)") { Params = new object[] { 2, "string", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToInt64)") { Params = new object[] { 2, "UInt64", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToInt64)") { Params = new object[] { 2, "UInt32", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToInt64)") { Params = new object[] { 2, "UInt16", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToInt64)") { Params = new object[] { 2, "Int64", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToInt64)") { Params = new object[] { 2, "Int32", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToInt64)") { Params = new object[] { 2, "Int16", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToInt64)") { Params = new object[] { 2, "Byte", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(TimeSpanToInt64)") { Params = new object[] { 2, "TimeSpan", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UriToInt64)") { Params = new object[] { 2, "Uri", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DoubleToInt64)") { Params = new object[] { 2, "Double", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleToInt64)") { Params = new object[] { 2, "Single", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameTypeToInt64)") { Params = new object[] { 2, "XmlQualifiedName", "Int64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToInt64)") { Params = new object[] { 2, "string", "Int64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToUInt32)") { Params = new object[] { 2, "UInt64", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToUInt32)") { Params = new object[] { 2, "UInt32", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToUInt32)") { Params = new object[] { 2, "UInt16", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToUInt32)") { Params = new object[] { 2, "Int64", "UInt32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToUInt32)") { Params = new object[] { 2, "Int32", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToUInt32)") { Params = new object[] { 2, "Int16", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToUInt32)") { Params = new object[] { 2, "Byte", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToUInt32)") { Params = new object[] { 2, "SByte", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToUInt32)") { Params = new object[] { 2, "string", "UInt32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToInt32)") { Params = new object[] { 2, "UInt64", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToInt32)") { Params = new object[] { 2, "UInt32", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToInt32)") { Params = new object[] { 2, "UInt16", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToInt32)") { Params = new object[] { 2, "Int64", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToInt32)") { Params = new object[] { 2, "Int32", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToInt32)") { Params = new object[] { 2, "Int16", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToInt32)") { Params = new object[] { 2, "Byte", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToInt32)") { Params = new object[] { 2, "SByte", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleToInt32)") { Params = new object[] { 2, "Single", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameTypeToInt32)") { Params = new object[] { 2, "XmlQualifiedName", "Int32", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToInt32)") { Params = new object[] { 2, "string", "Int32", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToUInt16)") { Params = new object[] { 2, "UInt64", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToUInt16)") { Params = new object[] { 2, "UInt32", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToUInt16)") { Params = new object[] { 2, "UInt16", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToUInt16)") { Params = new object[] { 2, "Int64", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToUInt16)") { Params = new object[] { 2, "Int32", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToUInt16)") { Params = new object[] { 2, "Int16", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToUInt16)") { Params = new object[] { 2, "Byte", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToUInt16)") { Params = new object[] { 2, "SByte", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToUInt16)") { Params = new object[] { 2, "Decimal", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToUInt16)") { Params = new object[] { 2, "float", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToUInt16)") { Params = new object[] { 2, "object", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(boolToUInt16)") { Params = new object[] { 2, "bool", "UInt16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToUInt16)") { Params = new object[] { 2, "string", "UInt16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToInt16)") { Params = new object[] { 2, "UInt64", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToInt16)") { Params = new object[] { 2, "UInt32", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToInt16)") { Params = new object[] { 2, "UInt16", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToInt16)") { Params = new object[] { 2, "Int64", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToUInt64)") { Params = new object[] { 1, "DateTimeOffset", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToInt16)") { Params = new object[] { 2, "Int16", "Int16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToInt16)") { Params = new object[] { 2, "Byte", "Int16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToInt16)") { Params = new object[] { 2, "SByte", "Int16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToInt16)") { Params = new object[] { 2, "Decimal", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToInt16)") { Params = new object[] { 2, "float", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToInt16)") { Params = new object[] { 2, "object", "Int16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(boolToInt16)") { Params = new object[] { 2, "bool", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DateTimeToInt16)") { Params = new object[] { 2, "DateTime", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToInt16)") { Params = new object[] { 2, "string", "Int16", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToByte)") { Params = new object[] { 2, "UInt64", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToByte)") { Params = new object[] { 2, "UInt32", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToByte)") { Params = new object[] { 2, "UInt16", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToByte)") { Params = new object[] { 2, "Int64", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToByte)") { Params = new object[] { 2, "Int32", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToByte)") { Params = new object[] { 2, "Int16", "Byte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToByte)") { Params = new object[] { 2, "Byte", "Byte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToByte)") { Params = new object[] { 2, "SByte", "Byte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToByte)") { Params = new object[] { 2, "string", "Byte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToSByte)") { Params = new object[] { 2, "UInt64", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToSByte)") { Params = new object[] { 2, "UInt32", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToSByte)") { Params = new object[] { 2, "UInt16", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToSByte)") { Params = new object[] { 2, "Int64", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToSByte)") { Params = new object[] { 2, "Int32", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UriToSByte)") { Params = new object[] { 2, "Uri", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DoubleToSByte)") { Params = new object[] { 2, "Double", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleToSByte)") { Params = new object[] { 2, "Single", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameTypeToSByte)") { Params = new object[] { 2, "XmlQualifiedName", "SByte", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToSByte)") { Params = new object[] { 2, "string", "SByte", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToDecimal)") { Params = new object[] { 2, "UInt64", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToString)") { Params = new object[] { 1, "UInt64", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToDecimal)") { Params = new object[] { 2, "UInt16", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToDecimal)") { Params = new object[] { 2, "Int64", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToDecimal)") { Params = new object[] { 2, "Int32", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToDecimal)") { Params = new object[] { 2, "Int16", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToDecimal)") { Params = new object[] { 2, "Byte", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToDecimal)") { Params = new object[] { 2, "SByte", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToDecimal)") { Params = new object[] { 2, "Decimal", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToDecimal)") { Params = new object[] { 2, "float", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToDecimal)") { Params = new object[] { 2, "object", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameTypeToDecimal)") { Params = new object[] { 21, "XmlQualifiedName", "Decimal", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToDecimal)") { Params = new object[] { 2, "string", "Decimal", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToFloat)") { Params = new object[] { 2, "UInt64", "float", true, 1.844674E+19F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToFloat)") { Params = new object[] { 2, "UInt32", "float", true, 4.294967E+09F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToFloat)") { Params = new object[] { 2, "UInt16", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToFloat)") { Params = new object[] { 2, "Int64", "float", true, 9.223372E+18F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToFloat)") { Params = new object[] { 2, "Int32", "float", true, 2.147484E+09F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToFloat)") { Params = new object[] { 2, "Int16", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToFloat)") { Params = new object[] { 2, "Byte", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToFloat)") { Params = new object[] { 2, "SByte", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToFloat)") { Params = new object[] { 2, "Decimal", "float", true, 7.922816E+28 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToFloat)") { Params = new object[] { 2, "float", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToFloat)") { Params = new object[] { 2, "object", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(boolToFloat)") { Params = new object[] { 2, "bool", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleTofloat)") { Params = new object[] { 2, "Single", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameTypeToFloat)") { Params = new object[] { 2, "XmlQualifiedName", "float", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToFloat)") { Params = new object[] { 2, "string", "float", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToBool)") { Params = new object[] { 2, "UInt64", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToBool)") { Params = new object[] { 2, "UInt32", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToBool)") { Params = new object[] { 2, "object", "bool", true, false } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DateTimeToBool)") { Params = new object[] { 2, "DateTime", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DateTimeOffsetToBool)") { Params = new object[] { 2, "DateTimeOffset", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteArrayToBool)") { Params = new object[] { 2, "ByteArray", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ListToBool)") { Params = new object[] { 2, "List", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(TimeSpanToBool)") { Params = new object[] { 2, "TimeSpan", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UriToBool)") { Params = new object[] { 2, "Uri", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DoubleToBool)") { Params = new object[] { 2, "Double", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleTobool)") { Params = new object[] { 2, "Single", "bool", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToDateTime)") { Params = new object[] { 2, "float", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToDateTime)") { Params = new object[] { 2, "object", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(boolToDateTime)") { Params = new object[] { 2, "bool", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteArrayToDateTime)") { Params = new object[] { 2, "ByteArray", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ListToDateTime)") { Params = new object[] { 2, "List", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UriToDateTime)") { Params = new object[] { 2, "Uri", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DoubleToDateTime)") { Params = new object[] { 2, "Double", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleToDateTime)") { Params = new object[] { 2, "Single", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameTypeToDateTime)") { Params = new object[] { 2, "XmlQualifiedName", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToDateTime)") { Params = new object[] { 2, "string", "DateTime", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToDateTimeOffset)") { Params = new object[] { 2, "UInt64", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToDateTimeOffset)") { Params = new object[] { 2, "UInt32", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToDateTimeOffset)") { Params = new object[] { 2, "UInt16", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToDateTimeOffset)") { Params = new object[] { 2, "Int64", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToDateTimeOffset)") { Params = new object[] { 2, "Int32", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToDateTimeOffset)") { Params = new object[] { 2, "Int16", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToDateTimeOffset)") { Params = new object[] { 2, "Byte", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToDateTimeOffset)") { Params = new object[] { 2, "SByte", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToDateTimeOffset)") { Params = new object[] { 2, "Decimal", "DateTimeOffset", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToList)") { Params = new object[] { 2, "UInt64", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToList)") { Params = new object[] { 2, "UInt32", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToList)") { Params = new object[] { 2, "UInt16", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToList)") { Params = new object[] { 2, "Int64", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToList)") { Params = new object[] { 2, "Int32", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToList)") { Params = new object[] { 2, "Int16", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToList)") { Params = new object[] { 2, "Byte", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToList)") { Params = new object[] { 2, "SByte", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToList)") { Params = new object[] { 2, "Decimal", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToList)") { Params = new object[] { 2, "float", "List", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToUri)") { Params = new object[] { 2, "UInt64", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToUri)") { Params = new object[] { 2, "UInt32", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToUri)") { Params = new object[] { 2, "UInt16", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToUri)") { Params = new object[] { 2, "Int64", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToUri)") { Params = new object[] { 2, "Int32", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToUri)") { Params = new object[] { 2, "Int16", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToUri)") { Params = new object[] { 2, "Byte", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToUri)") { Params = new object[] { 2, "SByte", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToUri)") { Params = new object[] { 2, "Decimal", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToUri)") { Params = new object[] { 2, "float", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToUri)") { Params = new object[] { 2, "object", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(boolToUri)") { Params = new object[] { 2, "bool", "Uri", true, "False" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UriToUri)") { Params = new object[] { 2, "Uri", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DoubleToUri)") { Params = new object[] { 2, "Double", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleToUri)") { Params = new object[] { 2, "Single", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameTypeToUri)") { Params = new object[] { 2, "XmlQualifiedName", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToUri)") { Params = new object[] { 2, "string", "Uri", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToDouble)") { Params = new object[] { 2, "UInt64", "Double", true, 1.84467440737096E+19 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToDouble)") { Params = new object[] { 2, "UInt32", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToDouble)") { Params = new object[] { 2, "UInt16", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToDouble)") { Params = new object[] { 2, "Int64", "Double", true, 9.22337203685478E+18 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToDouble)") { Params = new object[] { 2, "Int32", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToDouble)") { Params = new object[] { 2, "Int16", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToDouble)") { Params = new object[] { 2, "Byte", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToDouble)") { Params = new object[] { 2, "SByte", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToDouble)") { Params = new object[] { 2, "Decimal", "Double", true, 7.92281625142643E+28 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToDouble)") { Params = new object[] { 2, "float", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToDouble)") { Params = new object[] { 2, "object", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(boolToDouble)") { Params = new object[] { 2, "bool", "Double", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DoubleToDouble)") { Params = new object[] { 2, "Double", "Double", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleToDouble)") { Params = new object[] { 2, "Single", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToDouble)") { Params = new object[] { 2, "string", "Double", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToSingle)") { Params = new object[] { 2, "UInt64", "Single", true, 1.844674E+19F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt32ToSingle)") { Params = new object[] { 2, "UInt32", "Single", true, 4.294967E+09F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt16ToSingle)") { Params = new object[] { 2, "UInt16", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int64ToSingle)") { Params = new object[] { 2, "Int64", "Single", true, 9.223372E+18F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToSingle)") { Params = new object[] { 2, "Int32", "Single", true, 2.147484E+09F } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToSingle)") { Params = new object[] { 2, "Int16", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToSingle)") { Params = new object[] { 2, "Byte", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToSingle)") { Params = new object[] { 2, "SByte", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToSingle)") { Params = new object[] { 2, "Decimal", "Single", true, 7.922816E+28 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToSingle)") { Params = new object[] { 2, "float", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToSingle)") { Params = new object[] { 2, "object", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(boolToSingle)") { Params = new object[] { 2, "bool", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DateTimeOffsetToSingle)") { Params = new object[] { 2, "DateTimeOffset", "Single", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SingleToSingle)") { Params = new object[] { 2, "Single", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToSingle)") { Params = new object[] { 2, "string", "Single", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UInt64ToObject)") { Params = new object[] { 2, "UInt64", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToObject)") { Params = new object[] { 2, "Int32", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int16ToObject)") { Params = new object[] { 2, "Int16", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ByteToObject)") { Params = new object[] { 2, "Byte", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(SByteToObject)") { Params = new object[] { 2, "SByte", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(DecimalToObject)") { Params = new object[] { 2, "Decimal", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(floatToObject)") { Params = new object[] { 2, "float", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(objectToObject)") { Params = new object[] { 2, "object", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(boolToObject)") { Params = new object[] { 2, "bool", "object", true, "False" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameTypeToObject)") { Params = new object[] { 2, "XmlQualifiedName", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(stringToObject)") { Params = new object[] { 2, "string", "object", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(ObjectArrayToObjectArray)") { Params = new object[] { 2, "ObjectArray", "ObjectArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(StringArrayToStringArray)") { Params = new object[] { 2, "StringArray", "StringArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(UriArrayToUriArray)") { Params = new object[] { 2, "UriArray", "UriArray", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(XmlQualifiedNameToXmlQualifiedName)") { Params = new object[] { 2, "XmlQualifiedName", "XmlQualifiedName", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("attr.WriteValue(Int32ToInt16)") { Params = new object[] { 2, "Int32", "Int16", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToString)") { Params = new object[] { 1, "UInt32", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToString)") { Params = new object[] { 1, "UInt16", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToString)") { Params = new object[] { 1, "Int64", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToString)") { Params = new object[] { 1, "Int32", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToString)") { Params = new object[] { 1, "Int16", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToString)") { Params = new object[] { 1, "Byte", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToString)") { Params = new object[] { 1, "SByte", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToUInt64)") { Params = new object[] { 1, "List", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToString)") { Params = new object[] { 1, "Decimal", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToString)") { Params = new object[] { 1, "float", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToString)") { Params = new object[] { 1, "object", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToString)") { Params = new object[] { 1, "bool", "string", true, "false" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToString)") { Params = new object[] { 1, "DateTime", "string", true, 1 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeOffsetToString)") { Params = new object[] { 1, "DateTimeOffset", "string", true, 2 } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteArrayToString)") { Params = new object[] { 1, "ByteArray", "string", true, "2H4=" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ListToString)") { Params = new object[] { 1, "List", "string", true, "" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(TimeSpanToString)") { Params = new object[] { 1, "TimeSpan", "string", true, "PT0S" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UriToString)") { Params = new object[] { 1, "Uri", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DoubleToString)") { Params = new object[] { 1, "Double", "string", true, "1.7976931348623157E+308" } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SingleToString)") { Params = new object[] { 1, "Single", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(XmlQualifiedNameTypeToString)") { Params = new object[] { 1, "XmlQualifiedName", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(stringToString)") { Params = new object[] { 1, "string", "string", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt64ToUInt64)") { Params = new object[] { 1, "UInt64", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt32ToUInt64)") { Params = new object[] { 1, "UInt32", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(UInt16ToUInt64)") { Params = new object[] { 1, "UInt16", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int64ToUInt64)") { Params = new object[] { 1, "Int64", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int32ToUInt64)") { Params = new object[] { 1, "Int32", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(Int16ToUInt64)") { Params = new object[] { 1, "Int16", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(ByteToUInt64)") { Params = new object[] { 1, "Byte", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(SByteToUInt64)") { Params = new object[] { 1, "SByte", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DecimalToUInt64)") { Params = new object[] { 1, "Decimal", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(floatToUInt64)") { Params = new object[] { 1, "float", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(objectToUInt64)") { Params = new object[] { 1, "object", "UInt64", true, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(boolToUInt64)") { Params = new object[] { 1, "bool", "UInt64", false, null } } });
                    this.AddChild(new CVariation(writeValue_27) { Attribute = new Variation("elem.WriteValue(DateTimeToUInt64)") { Params = new object[] { 1, "DateTime", "UInt64", false, null } } });
                }


                // for function writeValue_28
                {
                    this.AddChild(new CVariation(writeValue_28) { Attribute = new Variation("WriteValue(XmlException)") { Param = 1, Pri = 2 } });
                    this.AddChild(new CVariation(writeValue_28) { Attribute = new Variation("WriteValue(XmlQualifiedName)") { Param = 3, Pri = 2 } });
                    this.AddChild(new CVariation(writeValue_28) { Attribute = new Variation("WriteValue(Guid)") { Param = 4, Pri = 2 } });
                    this.AddChild(new CVariation(writeValue_28) { Attribute = new Variation("WriteValue(NewLineHandling.Entitize)") { Param = 6, Pri = 2 } });
                    this.AddChild(new CVariation(writeValue_28) { Attribute = new Variation("ConformanceLevel.Auto") { Param = 7, Pri = 2 } });
                    this.AddChild(new CVariation(writeValue_28) { Attribute = new Variation("WriteValue(DayOfWeek)") { Param = 2, Pri = 2 } });
                    this.AddChild(new CVariation(writeValue_28) { Attribute = new Variation("WriteValue(Tuple)") { Param = 9, Pri = 2 } });
                }


                // for function writeValue_30
                {
                    this.AddChild(new CVariation(writeValue_30) { Attribute = new Variation("WriteValue(stringToXmlQualifiedName-invalid)") { Param = 1, Pri = 1 } });
                    this.AddChild(new CVariation(writeValue_30) { Attribute = new Variation("WriteValue(stringToXmlQualifiedName-invalid attr)") { Param = 2, Pri = 1 } });
                }


                // for function writeValue_31
                {
                    this.AddChild(new CVariation(writeValue_31) { Attribute = new Variation("5.WriteValue(DateTimeOffset) - valid") { Params = new object[] { "9999-12-31T12:59:59.9999999+14:00", "<Root>9999-12-31T12:59:59.9999999+14:00</Root>" } } });
                    this.AddChild(new CVariation(writeValue_31) { Attribute = new Variation("1.WriteValue(DateTimeOffset) - valid") { Params = new object[] { "2002-12-30T00:00:00-08:00", "<Root>2002-12-30T00:00:00-08:00</Root>" } } });
                    this.AddChild(new CVariation(writeValue_31) { Attribute = new Variation("3.WriteValue(DateTimeOffset) - valid") { Params = new object[] { "0001-01-01T00:00:00+00:00", "<Root>0001-01-01T00:00:00Z</Root>" } } });
                    this.AddChild(new CVariation(writeValue_31) { Attribute = new Variation("4.WriteValue(DateTimeOffset) - valid") { Params = new object[] { "0001-01-01T00:00:00.9999999-14:00", "<Root>0001-01-01T00:00:00.9999999-14:00</Root>" } } });
                    this.AddChild(new CVariation(writeValue_31) { Attribute = new Variation("2.WriteValue(DateTimeOffset) - valid") { Params = new object[] { "2000-02-29T23:59:59.999999999999-13:60", "<Root>2000-03-01T00:00:00-14:00</Root>" } } });
                    this.AddChild(new CVariation(writeValue_31) { Attribute = new Variation("7.WriteValue(DateTimeOffset) - valid") { Params = new object[] { "2000-02-29T23:59:59.999999999999+13:60", "<Root>2000-03-01T00:00:00+14:00</Root>" } } });
                    this.AddChild(new CVariation(writeValue_31) { Attribute = new Variation("6.WriteValue(DateTimeOffset) - valid") { Params = new object[] { "9999-12-31T12:59:59-11:00", "<Root>9999-12-31T12:59:59-11:00</Root>" } } });
                }


                // for function writeValue_32
                {
                    this.AddChild(new CVariation(writeValue_32) { Attribute = new Variation("WriteValue(new DateTimeOffset) - valid") { Pri = 2 } });
                }


                // for class System.Xml.Tests.TCFullEndElement+TCWriteValue+TCLookUpPrefix
                {
                    this.AddChild(new TCLookUpPrefix() { Attribute = new TestCase() { Name = "LookupPrefix" } });
                }
                // for class System.Xml.Tests.TCFullEndElement+TCWriteValue+TCXmlSpace
                {
                    this.AddChild(new TCXmlSpace() { Attribute = new TestCase() { Name = "XmlSpace" } });
                }
                // for class System.Xml.Tests.TCFullEndElement+TCWriteValue+TCXmlLang
                {
                    this.AddChild(new TCXmlLang() { Attribute = new TestCase() { Name = "XmlLang" } });
                }
                // for class System.Xml.Tests.TCFullEndElement+TCWriteValue+TCWriteRaw
                {
                    this.AddChild(new TCWriteRaw() { Attribute = new TestCase() { Name = "WriteRaw" } });
                }
                // for class System.Xml.Tests.TCFullEndElement+TCWriteValue+TCWriteBase64
                {
                    this.AddChild(new TCWriteBase64() { Attribute = new TestCase() { Name = "WriteBase64" } });
                }
                // for class System.Xml.Tests.TCFullEndElement+TCWriteValue+TCWriteBinHex
                {
                    this.AddChild(new TCWriteBinHex() { Attribute = new TestCase() { Name = "WriteBinHex" } });
                }
                // for class System.Xml.Tests.TCFullEndElement+TCWriteValue+TCWriteState
                {
                    this.AddChild(new TCWriteState() { Attribute = new TestCase() { Name = "WriteState" } });
                }
                // for class System.Xml.Tests.TCFullEndElement+TCWriteValue+TC_NDP20_NewMethods
                {
                    this.AddChild(new TC_NDP20_NewMethods() { Attribute = new TestCase() { Name = "NDP20_NewMethods" } });
                }
                // for class System.Xml.Tests.TCFullEndElement+TCWriteValue+TCGlobalization
                {
                    this.AddChild(new TCGlobalization() { Attribute = new TestCase() { Name = "Globalization" } });
                }
                // for class System.Xml.Tests.TCFullEndElement+TCWriteValue+TCClose
                {
                    this.AddChild(new TCClose() { Attribute = new TestCase() { Name = "Close()" } });
                }
            }
            public partial class TCLookUpPrefix : XmlWriterTestCaseBase
            {
                // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue+TCLookUpPrefix
                // Test Case
                public override void AddChildren()
                {
                    // for function lookupPrefix_1
                    {
                        this.AddChild(new CVariation(lookupPrefix_1) { Attribute = new Variation("LookupPrefix with null") { id = 1, Pri = 2 } });
                    }


                    // for function lookupPrefix_2
                    {
                        this.AddChild(new CVariation(lookupPrefix_2) { Attribute = new Variation("LookupPrefix with String.Empty should return String.Empty") { id = 2, Pri = 1 } });
                    }


                    // for function lookupPrefix_3
                    {
                        this.AddChild(new CVariation(lookupPrefix_3) { Attribute = new Variation("LookupPrefix with generated namespace used for attributes") { id = 3, Pri = 1 } });
                    }


                    // for function lookupPrefix_4
                    {
                        this.AddChild(new CVariation(lookupPrefix_4) { Attribute = new Variation("LookupPrefix for namespace used with element") { id = 4, Pri = 0 } });
                    }


                    // for function lookupPrefix_5
                    {
                        this.AddChild(new CVariation(lookupPrefix_5) { Attribute = new Variation("LookupPrefix for namespace used with attribute") { id = 5, Pri = 0 } });
                    }


                    // for function lookupPrefix_6
                    {
                        this.AddChild(new CVariation(lookupPrefix_6) { Attribute = new Variation("Lookup prefix for a default namespace") { id = 6, Pri = 1 } });
                    }


                    // for function lookupPrefix_7
                    {
                        this.AddChild(new CVariation(lookupPrefix_7) { Attribute = new Variation("Lookup prefix for nested element with same namespace but different prefix") { id = 7, Pri = 1 } });
                    }


                    // for function lookupPrefix_8
                    {
                        this.AddChild(new CVariation(lookupPrefix_8) { Attribute = new Variation("Lookup prefix for multiple prefix associated with the same namespace") { id = 8, Pri = 1 } });
                    }


                    // for function lookupPrefix_9
                    {
                        this.AddChild(new CVariation(lookupPrefix_9) { Attribute = new Variation("Lookup prefix for namespace defined outside the scope of an empty element and also defined in its parent") { id = 9, Pri = 1 } });
                    }


                    // for function lookupPrefix_10
                    {
                        this.AddChild(new CVariation(lookupPrefix_10) { Attribute = new Variation("Bug 53940: Lookup prefix for namespace declared as default and also with a prefix") { id = 10, Pri = 1 } });
                    }
                }
            }
            public partial class TCXmlSpace : XmlWriterTestCaseBase
            {
                // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue+TCXmlSpace
                // Test Case
                public override void AddChildren()
                {
                    // for function xmlSpace_1
                    {
                        this.AddChild(new CVariation(xmlSpace_1) { Attribute = new Variation("Verify XmlSpace as Preserve") { id = 1, Pri = 0 } });
                    }


                    // for function xmlSpace_2
                    {
                        this.AddChild(new CVariation(xmlSpace_2) { Attribute = new Variation("Verify XmlSpace as Default") { id = 2, Pri = 0 } });
                    }


                    // for function xmlSpace_3
                    {
                        this.AddChild(new CVariation(xmlSpace_3) { Attribute = new Variation("Verify XmlSpace as None") { id = 3, Pri = 0 } });
                    }


                    // for function xmlSpace_4
                    {
                        this.AddChild(new CVariation(xmlSpace_4) { Attribute = new Variation("Verify XmlSpace within an empty element") { id = 4, Pri = 1 } });
                    }


                    // for function xmlSpace_5
                    {
                        this.AddChild(new CVariation(xmlSpace_5) { Attribute = new Variation("Verify XmlSpace - scope with nested elements (both PROLOG and EPILOG)") { id = 5, Pri = 1 } });
                    }


                    // for function xmlSpace_6
                    {
                        this.AddChild(new CVariation(xmlSpace_6) { Attribute = new Variation("Verify XmlSpace - outside defined scope") { id = 6, Pri = 1 } });
                    }


                    // for function xmlSpace_7
                    {
                        this.AddChild(new CVariation(xmlSpace_7) { Attribute = new Variation("Verify XmlSpace with invalid space value") { id = 7, Pri = 0 } });
                    }


                    // for function xmlSpace_8
                    {
                        this.AddChild(new CVariation(xmlSpace_8) { Attribute = new Variation("Duplicate xml:space attr should error") { id = 8, Pri = 1 } });
                    }


                    // for function xmlSpace_9
                    {
                        this.AddChild(new CVariation(xmlSpace_9) { Attribute = new Variation("Veify XmlSpace value when received through WriteString") { id = 9, Pri = 1 } });
                    }
                }
            }
            public partial class TCXmlLang : XmlWriterTestCaseBase
            {
                // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue+TCXmlLang
                // Test Case
                public override void AddChildren()
                {
                    // for function XmlLang_1
                    {
                        this.AddChild(new CVariation(XmlLang_1) { Attribute = new Variation("Verify XmlLang sanity test") { id = 1, Pri = 0 } });
                    }


                    // for function XmlLang_2
                    {
                        this.AddChild(new CVariation(XmlLang_2) { Attribute = new Variation("Verify that default value of XmlLang is NULL") { id = 2, Pri = 1 } });
                    }


                    // for function XmlLang_3
                    {
                        this.AddChild(new CVariation(XmlLang_3) { Attribute = new Variation("Verify XmlLang scope inside nested elements (both PROLOG and EPILOG)") { id = 3, Pri = 1 } });
                    }


                    // for function XmlLang_4
                    {
                        this.AddChild(new CVariation(XmlLang_4) { Attribute = new Variation("Duplicate xml:lang attr should error") { id = 4, Pri = 1 } });
                    }


                    // for function XmlLang_5
                    {
                        this.AddChild(new CVariation(XmlLang_5) { Attribute = new Variation("Veify XmlLang value when received through WriteAttributes") { id = 5, Pri = 1 } });
                    }


                    // for function XmlLang_6
                    {
                        this.AddChild(new CVariation(XmlLang_6) { Attribute = new Variation("Veify XmlLang value when received through WriteString") { id = 6 } });
                    }


                    // for function XmlLang_7
                    {
                        this.AddChild(new CVariation(XmlLang_7) { Attribute = new Variation("Should not check XmlLang value") { id = 7, Pri = 2 } });
                    }


                    // for function XmlLang_8
                    {
                        this.AddChild(new CVariation(XmlLang_8) { Attribute = new Variation("More XmlLang with valid sequence") { id = 8, Pri = 1 } });
                    }
                }
            }
            public partial class TCWriteRaw : TCWriteBuffer
            {
                // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue+TCWriteRaw
                // Test Case
                public override void AddChildren()
                {
                    // for function writeRaw_1
                    {
                        this.AddChild(new CVariation(writeRaw_1) { Attribute = new Variation("Call both WriteRaw Methods") { id = 1, Pri = 1 } });
                    }


                    // for function writeRaw_2
                    {
                        this.AddChild(new CVariation(writeRaw_2) { Attribute = new Variation("WriteRaw with entites and entitized characters") { id = 2, Pri = 1 } });
                    }


                    // for function writeRaw_3
                    {
                        this.AddChild(new CVariation(writeRaw_3) { Attribute = new Variation("WriteRaw with entire Xml Document in string") { id = 3, Pri = 1 } });
                    }


                    // for function writeRaw_4
                    {
                        this.AddChild(new CVariation(writeRaw_4) { Attribute = new Variation("Call WriteRaw to write the value of xml:space") { id = 4 } });
                    }


                    // for function writerRaw_5
                    {
                        this.AddChild(new CVariation(writerRaw_5) { Attribute = new Variation("Call WriteRaw to write the value of xml:lang") { id = 5, Pri = 1 } });
                    }


                    // for function writeRaw_6
                    {
                        this.AddChild(new CVariation(writeRaw_6) { Attribute = new Variation("WriteRaw with count > buffer size") { id = 6, Pri = 1 } });
                    }


                    // for function writeRaw_7
                    {
                        this.AddChild(new CVariation(writeRaw_7) { Attribute = new Variation("WriteRaw with count < 0") { id = 7, Pri = 1 } });
                    }


                    // for function writeRaw_8
                    {
                        this.AddChild(new CVariation(writeRaw_8) { Attribute = new Variation("WriteRaw with index > buffer size") { id = 8, Pri = 1 } });
                    }


                    // for function writeRaw_9
                    {
                        this.AddChild(new CVariation(writeRaw_9) { Attribute = new Variation("WriteRaw with index < 0") { id = 9, Pri = 1 } });
                    }


                    // for function writeRaw_10
                    {
                        this.AddChild(new CVariation(writeRaw_10) { Attribute = new Variation("WriteRaw with index + count exceeds buffer") { id = 10, Pri = 1 } });
                    }


                    // for function writeRaw_11
                    {
                        this.AddChild(new CVariation(writeRaw_11) { Attribute = new Variation("WriteRaw with buffer = null") { id = 11, Pri = 1 } });
                    }


                    // for function writeRaw_12
                    {
                        this.AddChild(new CVariation(writeRaw_12) { Attribute = new Variation("WriteRaw with valid surrogate pair") { id = 12, Pri = 1 } });
                    }


                    // for function writeRaw_13
                    {
                        this.AddChild(new CVariation(writeRaw_13) { Attribute = new Variation("WriteRaw with invalid surrogate pair") { id = 13, Pri = 1 } });
                    }


                    // for function writeRaw_14
                    {
                        this.AddChild(new CVariation(writeRaw_14) { Attribute = new Variation("Index = Count = 0") { id = 14, Pri = 1 } });
                    }
                }
            }
            public partial class TCWriteBase64 : TCWriteBuffer
            {
                // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue+TCWriteBase64
                // Test Case
                public override void AddChildren()
                {
                    // for function Base64_1
                    {
                        this.AddChild(new CVariation(Base64_1) { Attribute = new Variation("Call WriteBase64 with 4*1024 chars") { Param = "4096", id = 14, Pri = 0 } });
                        this.AddChild(new CVariation(Base64_1) { Attribute = new Variation("Call WriteBase64 with 77 chars") { Param = "77", id = 12, Pri = 0 } });
                        this.AddChild(new CVariation(Base64_1) { Attribute = new Variation("Call WriteBase64 with 1024 chars") { Param = "1024", id = 13, Pri = 0 } });
                        this.AddChild(new CVariation(Base64_1) { Attribute = new Variation("Call WriteBase64 with 75 chars") { Param = "75", id = 10, Pri = 0 } });
                        this.AddChild(new CVariation(Base64_1) { Attribute = new Variation("Call WriteBase64 with 76 chars") { Param = "76", id = 11, Pri = 0 } });
                    }


                    // for function Base64_2
                    {
                        this.AddChild(new CVariation(Base64_2) { Attribute = new Variation("WriteBase64 with count > buffer size") { id = 20, Pri = 1 } });
                    }


                    // for function Base64_3
                    {
                        this.AddChild(new CVariation(Base64_3) { Attribute = new Variation("WriteBase64 with count < 0") { id = 30, Pri = 1 } });
                    }


                    // for function Base64_4
                    {
                        this.AddChild(new CVariation(Base64_4) { Attribute = new Variation("WriteBase64 with index > buffer size") { id = 40, Pri = 1 } });
                    }


                    // for function Base64_5
                    {
                        this.AddChild(new CVariation(Base64_5) { Attribute = new Variation("WriteBase64 with index < 0") { id = 50, Pri = 1 } });
                    }


                    // for function Base64_6
                    {
                        this.AddChild(new CVariation(Base64_6) { Attribute = new Variation("WriteBase64 with index + count exceeds buffer") { id = 60, Pri = 1 } });
                    }


                    // for function Base64_7
                    {
                        this.AddChild(new CVariation(Base64_7) { Attribute = new Variation("WriteBase64 with buffer = null") { id = 70, Pri = 1 } });
                    }


                    // for function Base64_8
                    {
                        this.AddChild(new CVariation(Base64_8) { Attribute = new Variation("Index = Count = 0") { id = 80, Pri = 1 } });
                    }


                    // for function Base64_9
                    {
                        this.AddChild(new CVariation(Base64_9) { Attribute = new Variation("Base64 should not be allowed inside xml:lang value") { Param = "lang", id = 90, Pri = 1 } });
                        this.AddChild(new CVariation(Base64_9) { Attribute = new Variation("Base64 should not be allowed inside namespace decl") { Param = "ns", id = 92, Pri = 1 } });
                        this.AddChild(new CVariation(Base64_9) { Attribute = new Variation("Base64 should not be allowed inside xml:space value") { Param = "space", id = 91, Pri = 1 } });
                    }


                    // for function Base64_11
                    {
                        this.AddChild(new CVariation(Base64_11) { Attribute = new Variation("WriteBase64 should flush the buffer if WriteString is called") { id = 94, Pri = 1 } });
                    }


                    // for function Base64_12
                    {
                        this.AddChild(new CVariation(Base64_12) { Attribute = new Variation("XmlWriter.WriteBase64 inserts new lines where they should not be...") { id = 95, Pri = 1 } });
                    }


                    // for function Base64_13
                    {
                        this.AddChild(new CVariation(Base64_13) { Attribute = new Variation("421637 - XmlWriter does not flush Base64 data on the Close") { id = 96, Pri = 1 } });
                    }
                }
            }
            public partial class TCWriteBinHex : TCWriteBuffer
            {
                // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue+TCWriteBinHex
                // Test Case
                public override void AddChildren()
                {
                    // for function BinHex_1
                    {
                        this.AddChild(new CVariation(BinHex_1) { Attribute = new Variation("Call WriteBinHex with correct byte, index, and count") { id = 1, Pri = 0 } });
                    }


                    // for function BinHex_2
                    {
                        this.AddChild(new CVariation(BinHex_2) { Attribute = new Variation("WriteBinHex with count > buffer size") { id = 2, Pri = 1 } });
                    }


                    // for function BinHex_3
                    {
                        this.AddChild(new CVariation(BinHex_3) { Attribute = new Variation("WriteBinHex with count < 0") { id = 3, Pri = 1 } });
                    }


                    // for function BinHex_4
                    {
                        this.AddChild(new CVariation(BinHex_4) { Attribute = new Variation("WriteBinHex with index > buffer size") { id = 4, Pri = 1 } });
                    }


                    // for function BinHex_5
                    {
                        this.AddChild(new CVariation(BinHex_5) { Attribute = new Variation("WriteBinHex with index < 0") { id = 5, Pri = 1 } });
                    }


                    // for function BinHex_6
                    {
                        this.AddChild(new CVariation(BinHex_6) { Attribute = new Variation("WriteBinHex with index + count exceeds buffer") { id = 6, Pri = 1 } });
                    }


                    // for function BinHex_7
                    {
                        this.AddChild(new CVariation(BinHex_7) { Attribute = new Variation("WriteBinHex with buffer = null") { id = 7, Pri = 1 } });
                    }


                    // for function BinHex_8
                    {
                        this.AddChild(new CVariation(BinHex_8) { Attribute = new Variation("Index = Count = 0") { id = 8, Pri = 1 } });
                    }


                    // for function BinHex_9
                    {
                        this.AddChild(new CVariation(BinHex_9) { Attribute = new Variation("Call WriteBinHex as an attribute value") { id = 9, Pri = 1 } });
                    }


                    // for function BinHex_10
                    {
                        this.AddChild(new CVariation(BinHex_10) { Attribute = new Variation("Call WriteBinHex and verify results can be read as a string") { id = 10, Pri = 1 } });
                    }
                }
            }
            public partial class TCWriteState : XmlWriterTestCaseBase
            {
                // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue+TCWriteState
                // Test Case
                public override void AddChildren()
                {
                    // for function writeState_1
                    {
                        this.AddChild(new CVariation(writeState_1) { Attribute = new Variation("Verify WriteState.Start when nothing has been written yet") { id = 1, Pri = 0 } });
                    }


                    // for function writeState_2
                    {
                        this.AddChild(new CVariation(writeState_2) { Attribute = new Variation("Verify correct state when writing in Prolog") { id = 2, Pri = 1 } });
                    }


                    // for function writeState_3
                    {
                        this.AddChild(new CVariation(writeState_3) { Attribute = new Variation("Verify correct state when writing an attribute") { id = 3, Pri = 1 } });
                    }


                    // for function writeState_4
                    {
                        this.AddChild(new CVariation(writeState_4) { Attribute = new Variation("Verify correct state when writing element content") { id = 4, Pri = 1 } });
                    }


                    // for function writeState_5
                    {
                        this.AddChild(new CVariation(writeState_5) { Attribute = new Variation("Verify correct state after Close has been called") { id = 5, Pri = 1 } });
                    }


                    // for function writeState_6
                    {
                        this.AddChild(new CVariation(writeState_6) { Attribute = new Variation("Verify WriteState = Error after an exception") { id = 6, Pri = 1 } });
                    }


                    // for function writeState_7
                    {
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call Flush after WriteState = Error") { Param = "Flush", id = 32, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteWhitespace after WriteState = Error") { Param = "WriteWhitespace", id = 18, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteNode(reader) after WriteState = Error") { Param = "WriteNodeReader", id = 31, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteStartDocument after WriteState = Error") { Param = "WriteStartDocument", id = 7, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteStartElement after WriteState = Error") { Param = "WriteStartElement", id = 8, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteEndElement after WriteState = Error") { Param = "WriteEndElement", id = 9, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteStartAttribute after WriteState = Error") { Param = "WriteStartAttribute", id = 10, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteEndAttribute after WriteState = Error") { Param = "WriteEndAttribute", id = 11, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteCData after WriteState = Error") { Param = "WriteCData", id = 12, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteComment after WriteState = Error") { Param = "WriteComment", id = 13, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WritePI after WriteState = Error") { Param = "WritePI", id = 14, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteEntityRef after WriteState = Error") { Param = "WriteEntityRef", id = 15, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteCharEntiry after WriteState = Error") { Param = "WriteCharEntity", id = 16, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteSurrogateCharEntity after WriteState = Error") { Param = "WriteSurrogateCharEntity", id = 17, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteString after WriteState = Error") { Param = "WriteString", id = 19, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteChars after WriteState = Error") { Param = "WriteChars", id = 20, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteRaw after WriteState = Error") { Param = "WriteRaw", id = 21, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteBase64 after WriteState = Error") { Param = "WriteBase64", id = 22, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteBinHex after WriteState = Error") { Param = "WriteBinHex", id = 23, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call LookupPrefix after WriteState = Error") { Param = "LookupPrefix", id = 24, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteNmToken after WriteState = Error") { Param = "WriteNmToken", id = 25, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteName after WriteState = Error") { Param = "WriteName", id = 26, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteQualifiedName after WriteState = Error") { Param = "WriteQualifiedName", id = 27, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteValue after WriteState = Error") { Param = "WriteValue", id = 28, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_7) { Attribute = new Variation("Call WriteAttributes after WriteState = Error") { Param = "WriteAttributes", id = 29, Pri = 1 } });
                    }


                    // for function writeState_8
                    {
                        this.AddChild(new CVariation(writeState_8) { Attribute = new Variation("XmlLang property after WriteState = Error") { Param = "XmlSpace", id = 34, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_8) { Attribute = new Variation("XmlSpace property after WriteState = Error") { Param = "XmlSpace", id = 33, Pri = 1 } });
                    }


                    // for function writeState_9
                    {
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteStartDocument after Close()") { Param = "WriteStartDocument", id = 6, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteAttributes after Close()") { Param = "WriteAttributes", id = 28, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteNode(reader) after Close()") { Param = "WriteNodeReader", id = 30, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteQualifiedName after Close()") { Param = "WriteQualifiedName", id = 26, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteEndElement after Close()") { Param = "WriteEndElement", id = 8, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteStartAttribute after Close()") { Param = "WriteStartAttribute", id = 9, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteEndAttribute after Close()") { Param = "WriteEndAttribute", id = 10, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteCData after Close()") { Param = "WriteCData", id = 11, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call Flush after Close()") { Param = "Flush", id = 31, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteStartElement after Close()") { Param = "WriteStartElement", id = 7, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteSurrogateCharEntity after Close()") { Param = "WriteSurrogateCharEntity", id = 16, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteWhitespace after Close()") { Param = "WriteWhitespace", id = 17, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteName after Close()") { Param = "WriteName", id = 25, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WritePI after Close()") { Param = "WritePI", id = 13, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteValue after Close()") { Param = "WriteValue", id = 27, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteEntityRef after Close()") { Param = "WriteEntityRef", id = 14, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteCharEntiry after Close()") { Param = "WriteCharEntity", id = 15, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteRaw after Close()") { Param = "WriteRaw", id = 20, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteNmToken after Close()") { Param = "WriteNmToken", id = 24, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteString after Close()") { Param = "WriteString", id = 18, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteChars after Close()") { Param = "WriteChars", id = 19, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteComment after Close()") { Param = "WriteComment", id = 12, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteBase64 after Close()") { Param = "WriteBase64", id = 21, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call WriteBinHex after Close()") { Param = "WriteBinHex", id = 22, Pri = 1 } });
                        this.AddChild(new CVariation(writeState_9) { Attribute = new Variation("Call LookupPrefix after Close()") { Param = "LookupPrefix", id = 23, Pri = 1 } });
                    }
                }
            }
            public partial class TC_NDP20_NewMethods : XmlWriterTestCaseBase
            {
                // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue+TC_NDP20_NewMethods
                // Test Case
                public override void AddChildren()
                {
                    // for function var_1
                    {
                        this.AddChild(new CVariation(var_1) { Attribute = new Variation("WriteElementString(prefix, name, ns, value) sanity test") { id = 1, Pri = 0 } });
                    }


                    // for function var_2
                    {
                        this.AddChild(new CVariation(var_2) { Attribute = new Variation("WriteElementString(prefix = xml, ns = XML namespace)") { id = 2, Pri = 1 } });
                    }


                    // for function var_3
                    {
                        this.AddChild(new CVariation(var_3) { Attribute = new Variation("WriteStartAttribute(string name) sanity test") { id = 3, Pri = 0 } });
                    }


                    // for function var_4
                    {
                        this.AddChild(new CVariation(var_4) { Attribute = new Variation("WriteElementString followed by attribute should error") { id = 4, Pri = 1 } });
                    }


                    // for function var_5
                    {
                        this.AddChild(new CVariation(var_5) { Attribute = new Variation("XmlWellformedWriter wrapping another XmlWriter should check the duplicate attributes first") { id = 5, Pri = 1 } });
                    }


                    // for function var_6a
                    {
                        this.AddChild(new CVariation(var_6a) { Attribute = new Variation("XmlWriter::WriteStartDocument(false)") { Param = false, id = 7, Pri = 1 } });
                        this.AddChild(new CVariation(var_6a) { Attribute = new Variation("XmlWriter::WriteStartDocument(true)") { Param = true, id = 6, Pri = 1 } });
                    }


                    // for function var_6b
                    {
                        this.AddChild(new CVariation(var_6b) { Attribute = new Variation("487037: Wrapped XmlWriter::WriteStartDocument(true) is missing standalone attribute") { id = 8, Pri = 1 } });
                    }
                }
            }
            public partial class TCGlobalization : XmlWriterTestCaseBase
            {
                // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue+TCGlobalization
                // Test Case
                public override void AddChildren()
                {
                    // for function var_1
                    {
                        this.AddChild(new CVariation(var_1) { Attribute = new Variation("Characters between 0xdfff and 0xfffe are valid Unicode characters (BUG #20002589)") { id = 1, Pri = 1 } });
                    }


                    // for function var_2
                    {
                        this.AddChild(new CVariation(var_2) { Attribute = new Variation("370663.XmlWriter using UTF-16BE encoding writes out wrong encoding name value in the xml decl") { id = 2, Pri = 1 } });
                    }
                }
            }
            public partial class TCClose : XmlWriterTestCaseBase
            {
                // Type is System.Xml.Tests.TCFullEndElement+TCWriteValue+TCClose
                // Test Case
                public override void AddChildren()
                {
                    // for function var_1
                    {
                        this.AddChild(new CVariation(var_1) { Attribute = new Variation("356141.Closing an XmlWriter should close all opened elements") { id = 1, Pri = 1 } });
                    }


                    // for function var_2
                    {
                        this.AddChild(new CVariation(var_2) { Attribute = new Variation("356141.Disposing an XmlWriter should close all opened elements") { id = 2, Pri = 1 } });
                    }


                    // for function var_3
                    {
                        this.AddChild(new CVariation(var_3) { Attribute = new Variation("389827.Dispose() shouldn't throw when a tag is not closed and inner stream is closed") { id = 3, Pri = 1 } });
                    }


                    // for function var_4
                    {
                        this.AddChild(new CVariation(var_4) { Attribute = new Variation("433155: Close() should be allowed when XML doesn't have content") { id = 4, Pri = 1 } });
                    }


                    // for function SettingIndetingToFalseAllowsIndentingWhileWritingBase64
                    {
                        this.AddChild(new CVariation(SettingIndetingToFalseAllowsIndentingWhileWritingBase64) { Attribute = new Variation("SettingIndetingToFalseAllowsIndentingWhileWritingBase64 - XmlWriter: Setting Indenting to false still allows indending while writing base64 out") });
                    }


                    // for function WriteStateReturnsContentAfterDocumentClosed
                    {
                        this.AddChild(new CVariation(WriteStateReturnsContentAfterDocumentClosed) { Attribute = new Variation("WriteStateReturnsContentAfterDocumentClosed - WriteState returns Content even though document element has been closed") });
                    }
                }
            }
        }
    }
}
