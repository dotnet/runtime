// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Text;
using Microsoft.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeReaderFunctionalTests : TestModule
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests
        // Test Module
        [Fact]
        [OuterLoop]
        public static void RunTests()
        {
            TestInput.CommandLine = "";
            XNodeReaderFunctionalTests module = new XNodeReaderFunctionalTests();
            module.Init();

            module.AddChild(new XNodeReaderTests() { Attribute = new TestCaseAttribute() { Name = "XNodeReader", Desc = "XLinq XNodeReader Tests" } });
            module.Execute();

            Assert.False(module.HasFailures, module.GetFailuresInfo());
        }

        #region Class
        public partial class XNodeReaderTests : XLinqTestCase
        {
            // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests
            public override void AddChildren()
            {
                this.AddChild(new TCDispose() { Attribute = new TestCaseAttribute() { Name = "Dispose", Desc = "Dispose" } });
                this.AddChild(new TCDepth() { Attribute = new TestCaseAttribute() { Name = "Depth", Desc = "Depth" } });
                this.AddChild(new TCNamespace() { Attribute = new TestCaseAttribute() { Name = "Namespace", Desc = "Namespace" } });
                this.AddChild(new TCLookupNamespace() { Attribute = new TestCaseAttribute() { Name = "LookupNamespace", Desc = "LookupNamespace" } });
                this.AddChild(new TCHasValue() { Attribute = new TestCaseAttribute() { Name = "HasValue", Desc = "HasValue" } });
                this.AddChild(new TCIsEmptyElement2() { Attribute = new TestCaseAttribute() { Name = "IsEmptyElement", Desc = "IsEmptyElement" } });
                this.AddChild(new TCXmlSpace() { Attribute = new TestCaseAttribute() { Name = "XmlSpace", Desc = "XmlSpace" } });
                this.AddChild(new TCXmlLang() { Attribute = new TestCaseAttribute() { Name = "XmlLang", Desc = "XmlLang" } });
                this.AddChild(new TCSkip() { Attribute = new TestCaseAttribute() { Name = "Skip", Desc = "Skip" } });
                this.AddChild(new TCIsDefault() { Attribute = new TestCaseAttribute() { Name = "IsDefault", Desc = "IsDefault" } });
                this.AddChild(new TCBaseURI() { Attribute = new TestCaseAttribute() { Name = "BaseUri", Desc = "BaseUri" } });
                this.AddChild(new TCAttributeAccess() { Attribute = new TestCaseAttribute() { Name = "AttributeAccess", Desc = "AttributeAccess" } });
                this.AddChild(new TCThisName() { Attribute = new TestCaseAttribute() { Name = "ThisName", Desc = "ThisName" } });
                this.AddChild(new TCMoveToAttributeReader() { Attribute = new TestCaseAttribute() { Name = "MoveToAttribute", Desc = "MoveToAttribute" } });
                this.AddChild(new TCGetAttributeOrdinal() { Attribute = new TestCaseAttribute() { Name = "GetAttributeOrdinal", Desc = "GetAttributeOrdinal" } });
                this.AddChild(new TCGetAttributeName() { Attribute = new TestCaseAttribute() { Name = "GetAttributeName", Desc = "GetAttributeName" } });
                this.AddChild(new TCThisOrdinal() { Attribute = new TestCaseAttribute() { Name = "ThisOrdinal", Desc = "ThisOrdinal" } });
                this.AddChild(new TCMoveToAttributeOrdinal() { Attribute = new TestCaseAttribute() { Name = "MoveToAttributeOrdinal", Desc = "MoveToAttributeOrdinal" } });
                this.AddChild(new TCMoveToFirstAttribute() { Attribute = new TestCaseAttribute() { Name = "MoveToFirstAttribute", Desc = "MoveToFirstAttribute" } });
                this.AddChild(new TCMoveToNextAttribute() { Attribute = new TestCaseAttribute() { Name = "MoveToNextAttribute", Desc = "MoveToNextAttribute" } });
                this.AddChild(new TCAttributeTest() { Attribute = new TestCaseAttribute() { Name = "AttributeTest", Desc = "AttributeTest" } });
                this.AddChild(new TCXmlns() { Attribute = new TestCaseAttribute() { Name = "Xlmns", Desc = "Xlmns" } });
                this.AddChild(new TCXmlnsPrefix() { Attribute = new TestCaseAttribute() { Name = "XlmnsPrefix", Desc = "XlmnsPrefix" } });
                this.AddChild(new TCReadState() { Attribute = new TestCaseAttribute() { Name = "ReadState", Desc = "ReadState" } });
                this.AddChild(new TCReadInnerXml() { Attribute = new TestCaseAttribute() { Name = "ReadInnerXml", Desc = "ReadInnerXml" } });
                this.AddChild(new TCMoveToContent() { Attribute = new TestCaseAttribute() { Name = "MoveToContent", Desc = "MoveToContent" } });
                this.AddChild(new TCIsStartElement() { Attribute = new TestCaseAttribute() { Name = "IsStartElement", Desc = "IsStartElement" } });
                this.AddChild(new TCReadStartElement() { Attribute = new TestCaseAttribute() { Name = "ReadStartElement", Desc = "ReadStartElement" } });
                this.AddChild(new TCReadEndElement() { Attribute = new TestCaseAttribute() { Name = "ReadEndElement", Desc = "ReadEndElement" } });
                this.AddChild(new TCMoveToElement() { Attribute = new TestCaseAttribute() { Name = "MoveToElement", Desc = "MoveToElement" } });
                this.AddChild(new ErrorConditions() { Attribute = new TestCaseAttribute() { Name = "ErrorConditions" } });
                this.AddChild(new TCXMLIntegrityBase() { Attribute = new TestCaseAttribute() { Name = "XMLIntegrityBase", Desc = "XMLIntegrityBase" } });
                this.AddChild(new TCReadContentAsBase64() { Attribute = new TestCaseAttribute() { Name = "ReadContentAsBase64", Desc = "ReadContentAsBase64" } });
                this.AddChild(new TCReadElementContentAsBase64() { Attribute = new TestCaseAttribute() { Name = "ReadElementContentAsBase64", Desc = "ReadElementContentAsBase64" } });
                this.AddChild(new TCReadContentAsBinHex() { Attribute = new TestCaseAttribute() { Name = "ReadContentAsBinHex", Desc = "ReadContentAsBinHex" } });
                this.AddChild(new TCReadElementContentAsBinHex() { Attribute = new TestCaseAttribute() { Name = "ReadElementContentAsBinHex", Desc = "ReadElementContentAsBinHex" } });
                this.AddChild(new CReaderTestModule() { Attribute = new TestCaseAttribute() { Name = "ReaderProperty", Desc = "Reader Property" } });
                this.AddChild(new TCReadOuterXml() { Attribute = new TestCaseAttribute() { Name = "ReadOuterXml", Desc = "ReadOuterXml" } });
                this.AddChild(new TCReadSubtree() { Attribute = new TestCaseAttribute() { Name = "ReadSubtree", Desc = "ReadSubtree" } });
                this.AddChild(new TCReadToDescendant() { Attribute = new TestCaseAttribute() { Name = "ReadToDescendant", Desc = "ReadToDescendant" } });
                this.AddChild(new TCReadToFollowing() { Attribute = new TestCaseAttribute() { Name = "ReadToFollowing", Desc = "ReadToFollowing" } });
                this.AddChild(new TCReadToNextSibling() { Attribute = new TestCaseAttribute() { Name = "ReadToNextSibling", Desc = "ReadToNextSibling" } });
                this.AddChild(new TCReadValue() { Attribute = new TestCaseAttribute() { Name = "ReadValue", Desc = "ReadValue" } });
                this.AddChild(new XNodeReaderAPI() { Attribute = new TestCaseAttribute() { Name = "API Tests" } });
            }
            public partial class TCDispose : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCDispose
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(Variation1) { Attribute = new VariationAttribute("Test Integrity of all values after Dispose") });
                    this.AddChild(new TestVariation(Variation2) { Attribute = new VariationAttribute("Call Dispose Multiple(3) Times") });
                }
            }
            public partial class TCDepth : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCDepth
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestDepth1) { Attribute = new VariationAttribute("XmlReader Depth at the Root") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestDepth2) { Attribute = new VariationAttribute("XmlReader Depth at Empty Tag") });
                    this.AddChild(new TestVariation(TestDepth3) { Attribute = new VariationAttribute("XmlReader Depth at Empty Tag with Attributes") });
                    this.AddChild(new TestVariation(TestDepth4) { Attribute = new VariationAttribute("XmlReader Depth at Non Empty Tag with Text") });
                }
            }
            public partial class TCNamespace : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCNamespace
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestNamespace1) { Attribute = new VariationAttribute("Namespace test within a scope (no nested element)") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestNamespace2) { Attribute = new VariationAttribute("Namespace test within a scope (with nested element)") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestNamespace3) { Attribute = new VariationAttribute("Namespace test immediately outside the Namespace scope") });
                    this.AddChild(new TestVariation(TestNamespace4) { Attribute = new VariationAttribute("Namespace test Attribute should has no default namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestNamespace5) { Attribute = new VariationAttribute("Namespace test with multiple Namespace declaration") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestNamespace6) { Attribute = new VariationAttribute("Namespace test with multiple Namespace declaration, including default namespace") });
                    this.AddChild(new TestVariation(TestNamespace7) { Attribute = new VariationAttribute("Namespace URI for xml prefix") { Priority = 0 } });
                }
            }
            public partial class TCLookupNamespace : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCLookupNamespace
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(LookupNamespace1) { Attribute = new VariationAttribute("LookupNamespace test within EmptyTag") });
                    this.AddChild(new TestVariation(LookupNamespace2) { Attribute = new VariationAttribute("LookupNamespace test with Default namespace within EmptyTag") { Priority = 0 } });
                    this.AddChild(new TestVariation(LookupNamespace3) { Attribute = new VariationAttribute("LookupNamespace test within a scope (no nested element)") { Priority = 0 } });
                    this.AddChild(new TestVariation(LookupNamespace4) { Attribute = new VariationAttribute("LookupNamespace test within a scope (with nested element)") { Priority = 0 } });
                    this.AddChild(new TestVariation(LookupNamespace5) { Attribute = new VariationAttribute("LookupNamespace test immediately outside the Namespace scope") });
                    this.AddChild(new TestVariation(LookupNamespace6) { Attribute = new VariationAttribute("LookupNamespace test with multiple Namespace declaration") { Priority = 0 } });
                    this.AddChild(new TestVariation(LookupNamespace7) { Attribute = new VariationAttribute("Namespace test with multiple Namespace declaration, including default namespace") });
                    this.AddChild(new TestVariation(LookupNamespace8) { Attribute = new VariationAttribute("LookupNamespace on whitespace node PreserveWhitespaces = true") { Priority = 0 } });
                    this.AddChild(new TestVariation(LookupNamespace9) { Attribute = new VariationAttribute("Different prefix on inner element for the same namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(LookupNamespace10) { Attribute = new VariationAttribute("LookupNamespace when Namespaces = false") { Priority = 0 } });
                }
            }
            public partial class TCHasValue : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCHasValue
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestHasValueNodeType_None) { Attribute = new VariationAttribute("HasValue On None") });
                    this.AddChild(new TestVariation(TestHasValueNodeType_Element) { Attribute = new VariationAttribute("HasValue On Element") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestHasValue1) { Attribute = new VariationAttribute("Get node with a scalar value, verify the value with valid ReadString") });
                    this.AddChild(new TestVariation(TestHasValueNodeType_Attribute) { Attribute = new VariationAttribute("HasValue On Attribute") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestHasValueNodeType_Text) { Attribute = new VariationAttribute("HasValue On Text") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestHasValueNodeType_CDATA) { Attribute = new VariationAttribute("HasValue On CDATA") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestHasValueNodeType_ProcessingInstruction) { Attribute = new VariationAttribute("HasValue On ProcessingInstruction") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestHasValueNodeType_Comment) { Attribute = new VariationAttribute("HasValue On Comment") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestHasValueNodeType_DocumentType) { Attribute = new VariationAttribute("HasValue On DocumentType") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestHasValueNodeType_Whitespace) { Attribute = new VariationAttribute("HasValue On Whitespace PreserveWhitespaces = true") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestHasValueNodeType_EndElement) { Attribute = new VariationAttribute("HasValue On EndElement") });
                    this.AddChild(new TestVariation(TestHasValueNodeType_XmlDeclaration) { Attribute = new VariationAttribute("HasValue On XmlDeclaration") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestHasValueNodeType_EntityReference) { Attribute = new VariationAttribute("HasValue On EntityReference") });
                    this.AddChild(new TestVariation(TestHasValueNodeType_EndEntity) { Attribute = new VariationAttribute("HasValue On EndEntity") });
                    this.AddChild(new TestVariation(v13) { Attribute = new VariationAttribute("PI Value containing surrogates") { Priority = 0 } });
                }
            }
            public partial class TCIsEmptyElement2 : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCIsEmptyElement2
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestEmpty1) { Attribute = new VariationAttribute("Set and Get an element that ends with />") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestEmpty2) { Attribute = new VariationAttribute("Set and Get an element with an attribute that ends with />") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestEmpty3) { Attribute = new VariationAttribute("Set and Get an element that ends without />") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestEmpty4) { Attribute = new VariationAttribute("Set and Get an element with an attribute that ends with />") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestEmptyNodeType_Element) { Attribute = new VariationAttribute("IsEmptyElement On Element") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestEmptyNodeType_None) { Attribute = new VariationAttribute("IsEmptyElement On None") });
                    this.AddChild(new TestVariation(TestEmptyNodeType_Text) { Attribute = new VariationAttribute("IsEmptyElement On Text") });
                    this.AddChild(new TestVariation(TestEmptyNodeType_CDATA) { Attribute = new VariationAttribute("IsEmptyElement On CDATA") });
                    this.AddChild(new TestVariation(TestEmptyNodeType_ProcessingInstruction) { Attribute = new VariationAttribute("IsEmptyElement On ProcessingInstruction") });
                    this.AddChild(new TestVariation(TestEmptyNodeType_Comment) { Attribute = new VariationAttribute("IsEmptyElement On Comment") });
                    this.AddChild(new TestVariation(TestEmptyNodeType_DocumentType) { Attribute = new VariationAttribute("IsEmptyElement On DocumentType") });
                    this.AddChild(new TestVariation(TestEmptyNodeType_Whitespace) { Attribute = new VariationAttribute("IsEmptyElement On Whitespace PreserveWhitespaces = true") });
                    this.AddChild(new TestVariation(TestEmptyNodeType_EndElement) { Attribute = new VariationAttribute("IsEmptyElement On EndElement") });
                    this.AddChild(new TestVariation(TestEmptyNodeType_EntityReference) { Attribute = new VariationAttribute("IsEmptyElement On EntityReference") });
                    this.AddChild(new TestVariation(TestEmptyNodeType_EndEntity) { Attribute = new VariationAttribute("IsEmptyElement On EndEntity") });
                }
            }
            public partial class TCXmlSpace : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCXmlSpace
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestXmlSpace1) { Attribute = new VariationAttribute("XmlSpace test within EmptyTag") });
                    this.AddChild(new TestVariation(TestXmlSpace2) { Attribute = new VariationAttribute("Xmlspace test within a scope (no nested element)") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestXmlSpace3) { Attribute = new VariationAttribute("Xmlspace test within a scope (with nested element)") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestXmlSpace4) { Attribute = new VariationAttribute("Xmlspace test immediately outside the XmlSpace scope") });
                    this.AddChild(new TestVariation(TestXmlSpace5) { Attribute = new VariationAttribute("XmlSpace test with multiple XmlSpace declaration") });
                }
            }
            public partial class TCXmlLang : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCXmlLang
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestXmlLang1) { Attribute = new VariationAttribute("XmlLang test within EmptyTag") });
                    this.AddChild(new TestVariation(TestXmlLang2) { Attribute = new VariationAttribute("XmlLang test within a scope (no nested element)") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestXmlLang3) { Attribute = new VariationAttribute("XmlLang test within a scope (with nested element)") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestXmlLang4) { Attribute = new VariationAttribute("XmlLang test immediately outside the XmlLang scope") });
                    this.AddChild(new TestVariation(TestXmlLang5) { Attribute = new VariationAttribute("XmlLang test with multiple XmlLang declaration") });
                    this.AddChild(new TestVariation(TestXmlLang6) { Attribute = new VariationAttribute("XmlLang valid values") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestXmlTextReaderLang1) { Attribute = new VariationAttribute("More XmlLang valid values") });
                }
            }
            public partial class TCSkip : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCSkip
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestSkip1) { Attribute = new VariationAttribute("Call Skip on empty element") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestSkip2) { Attribute = new VariationAttribute("Call Skip on element") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestSkip3) { Attribute = new VariationAttribute("Call Skip on element with content") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestSkip4) { Attribute = new VariationAttribute("Call Skip on text node (leave node)") { Priority = 0 } });
                    this.AddChild(new TestVariation(skip307543) { Attribute = new VariationAttribute("Call Skip in while read loop") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestSkip5) { Attribute = new VariationAttribute("Call Skip on text node with another element: <elem2>text<elem3></elem3></elem2>") });
                    this.AddChild(new TestVariation(TestSkip6) { Attribute = new VariationAttribute("Call Skip on attribute") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestSkip7) { Attribute = new VariationAttribute("Call Skip on text node of attribute") });
                    this.AddChild(new TestVariation(TestSkip8) { Attribute = new VariationAttribute("Call Skip on CDATA") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestSkip9) { Attribute = new VariationAttribute("Call Skip on Processing Instruction") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestSkip10) { Attribute = new VariationAttribute("Call Skip on Comment") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestSkip12) { Attribute = new VariationAttribute("Call Skip on Whitespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestSkip13) { Attribute = new VariationAttribute("Call Skip on EndElement") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestSkip14) { Attribute = new VariationAttribute("Call Skip on root Element") });
                    this.AddChild(new TestVariation(XmlTextReaderDoesNotThrowWhenHandlingAmpersands) { Attribute = new VariationAttribute("XmlTextReader ArgumentOutOfRangeException when handling ampersands") });
                }
            }
            public partial class TCBaseURI : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCBaseURI
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestBaseURI1) { Attribute = new VariationAttribute("BaseURI for element node") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestBaseURI2) { Attribute = new VariationAttribute("BaseURI for attribute node") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestBaseURI3) { Attribute = new VariationAttribute("BaseURI for text node") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestBaseURI4) { Attribute = new VariationAttribute("BaseURI for CDATA node") });
                    this.AddChild(new TestVariation(TestBaseURI6) { Attribute = new VariationAttribute("BaseURI for PI node") });
                    this.AddChild(new TestVariation(TestBaseURI7) { Attribute = new VariationAttribute("BaseURI for Comment node") });
                    this.AddChild(new TestVariation(TestBaseURI9) { Attribute = new VariationAttribute("BaseURI for Whitespace node PreserveWhitespaces = true") });
                    this.AddChild(new TestVariation(TestBaseURI10) { Attribute = new VariationAttribute("BaseURI for EndElement node") });
                    this.AddChild(new TestVariation(TestTextReaderBaseURI4) { Attribute = new VariationAttribute("BaseURI for external General Entity") });
                }
            }
            public partial class TCAttributeAccess : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCAttributeAccess
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestAttributeAccess1) { Attribute = new VariationAttribute("Attribute Access test using ordinal (Ascending Order)") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestAttributeAccess2) { Attribute = new VariationAttribute("Attribute Access test using ordinal (Descending Order)") });
                    this.AddChild(new TestVariation(TestAttributeAccess3) { Attribute = new VariationAttribute("Attribute Access test using ordinal (Odd number)") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestAttributeAccess4) { Attribute = new VariationAttribute("Attribute Access test using ordinal (Even number)") });
                    this.AddChild(new TestVariation(TestAttributeAccess5) { Attribute = new VariationAttribute("Attribute Access with namespace=null") });
                }
            }
            public partial class TCThisName : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCThisName
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(ThisWithName1) { Attribute = new VariationAttribute("This[Name] Verify with GetAttribute(Name)") { Priority = 0 } });
                    this.AddChild(new TestVariation(ThisWithName2) { Attribute = new VariationAttribute("This[Name, null] Verify with GetAttribute(Name)") });
                    this.AddChild(new TestVariation(ThisWithName3) { Attribute = new VariationAttribute("This[Name] Verify with GetAttribute(Name,null)") });
                    this.AddChild(new TestVariation(ThisWithName4) { Attribute = new VariationAttribute("This[Name, NamespaceURI] Verify with GetAttribute(Name, NamespaceURI)") { Priority = 0 } });
                    this.AddChild(new TestVariation(ThisWithName5) { Attribute = new VariationAttribute("This[Name, null] Verify not the same as GetAttribute(Name, NamespaceURI)") });
                    this.AddChild(new TestVariation(ThisWithName6) { Attribute = new VariationAttribute("This[Name, NamespaceURI] Verify not the same as GetAttribute(Name, null)") });
                    this.AddChild(new TestVariation(ThisWithName7) { Attribute = new VariationAttribute("This[Name] Verify with MoveToAttribute(Name)") { Priority = 0 } });
                    this.AddChild(new TestVariation(ThisWithName8) { Attribute = new VariationAttribute("This[Name, null] Verify with MoveToAttribute(Name)") });
                    this.AddChild(new TestVariation(ThisWithName9) { Attribute = new VariationAttribute("This[Name] Verify with MoveToAttribute(Name,null)") });
                    this.AddChild(new TestVariation(ThisWithName10) { Attribute = new VariationAttribute("This[Name, NamespaceURI] Verify not the same as MoveToAttribute(Name, null)") { Priority = 0 } });
                    this.AddChild(new TestVariation(ThisWithName11) { Attribute = new VariationAttribute("This[Name, null] Verify not the same as MoveToAttribute(Name, NamespaceURI)") });
                    this.AddChild(new TestVariation(ThisWithName12) { Attribute = new VariationAttribute("This[Name, namespace] Verify not the same as MoveToAttribute(Name, namespace)") });
                    this.AddChild(new TestVariation(ThisWithName13) { Attribute = new VariationAttribute("This(String.Empty)") });
                    this.AddChild(new TestVariation(ThisWithName14) { Attribute = new VariationAttribute("This[String.Empty,String.Empty]") });
                    this.AddChild(new TestVariation(ThisWithName15) { Attribute = new VariationAttribute("This[QName] Verify with GetAttribute(Name, NamespaceURI)") { Priority = 0 } });
                    this.AddChild(new TestVariation(ThisWithName16) { Attribute = new VariationAttribute("This[QName] invalid Qname") });
                }
            }
            public partial class TCMoveToAttributeReader : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCMoveToAttributeReader
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(MoveToAttributeWithName1) { Attribute = new VariationAttribute("MoveToAttribute(String.Empty)") });
                    this.AddChild(new TestVariation(MoveToAttributeWithName2) { Attribute = new VariationAttribute("MoveToAttribute(String.Empty,String.Empty)") });
                }
            }
            public partial class TCGetAttributeOrdinal : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCGetAttributeOrdinal
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(GetAttributeWithGetAttrDoubleQ) { Attribute = new VariationAttribute("GetAttribute(i) Verify with This[i] - Double Quote") { Priority = 0 } });
                    this.AddChild(new TestVariation(OrdinalWithGetAttrSingleQ) { Attribute = new VariationAttribute("GetAttribute[i] Verify with This[i] - Single Quote") });
                    this.AddChild(new TestVariation(GetAttributeWithMoveAttrDoubleQ) { Attribute = new VariationAttribute("GetAttribute(i) Verify with MoveToAttribute[i] - Double Quote") { Priority = 0 } });
                    this.AddChild(new TestVariation(GetAttributeWithMoveAttrSingleQ) { Attribute = new VariationAttribute("GetAttribute(i) Verify with MoveToAttribute[i] - Single Quote") });
                }
            }
            public partial class TCGetAttributeName : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCGetAttributeName
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(GetAttributeWithName1) { Attribute = new VariationAttribute("GetAttribute(Name) Verify with This[Name]") { Priority = 0 } });
                    this.AddChild(new TestVariation(GetAttributeWithName2) { Attribute = new VariationAttribute("GetAttribute(Name, null) Verify with This[Name]") });
                    this.AddChild(new TestVariation(GetAttributeWithName3) { Attribute = new VariationAttribute("GetAttribute(Name) Verify with This[Name,null]") });
                    this.AddChild(new TestVariation(GetAttributeWithName4) { Attribute = new VariationAttribute("GetAttribute(Name, NamespaceURI) Verify with This[Name, NamespaceURI]") { Priority = 0 } });
                    this.AddChild(new TestVariation(GetAttributeWithName5) { Attribute = new VariationAttribute("GetAttribute(Name, null) Verify not the same as This[Name, NamespaceURI]") });
                    this.AddChild(new TestVariation(GetAttributeWithName6) { Attribute = new VariationAttribute("GetAttribute(Name, NamespaceURI) Verify not the same as This[Name, null]") });
                    this.AddChild(new TestVariation(GetAttributeWithName7) { Attribute = new VariationAttribute("GetAttribute(Name) Verify with MoveToAttribute(Name)") });
                    this.AddChild(new TestVariation(GetAttributeWithName8) { Attribute = new VariationAttribute("GetAttribute(Name,null) Verify with MoveToAttribute(Name)") { Priority = 1 } });
                    this.AddChild(new TestVariation(GetAttributeWithName9) { Attribute = new VariationAttribute("GetAttribute(Name) Verify with MoveToAttribute(Name,null)") { Priority = 1 } });
                    this.AddChild(new TestVariation(GetAttributeWithName10) { Attribute = new VariationAttribute("GetAttribute(Name, NamespaceURI) Verify not the same as MoveToAttribute(Name, null)") });
                    this.AddChild(new TestVariation(GetAttributeWithName11) { Attribute = new VariationAttribute("GetAttribute(Name, null) Verify not the same as MoveToAttribute(Name, NamespaceURI)") });
                    this.AddChild(new TestVariation(GetAttributeWithName12) { Attribute = new VariationAttribute("GetAttribute(Name, namespace) Verify not the same as MoveToAttribute(Name, namespace)") });
                    this.AddChild(new TestVariation(GetAttributeWithName13) { Attribute = new VariationAttribute("GetAttribute(String.Empty)") });
                    this.AddChild(new TestVariation(GetAttributeWithName14) { Attribute = new VariationAttribute("GetAttribute(String.Empty,String.Empty)") });
                }
            }
            public partial class TCThisOrdinal : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCThisOrdinal
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(OrdinalWithGetAttrDoubleQ) { Attribute = new VariationAttribute("This[i] Verify with GetAttribute[i] - Double Quote") { Priority = 0 } });
                    this.AddChild(new TestVariation(OrdinalWithGetAttrSingleQ) { Attribute = new VariationAttribute("This[i] Verify with GetAttribute[i] - Single Quote") });
                    this.AddChild(new TestVariation(OrdinalWithMoveAttrSingleQ) { Attribute = new VariationAttribute("This[i] Verify with MoveToAttribute[i] - Single Quote") });
                    this.AddChild(new TestVariation(OrdinalWithMoveAttrDoubleQ) { Attribute = new VariationAttribute("This[i] Verify with MoveToAttribute[i] - Double Quote") { Priority = 0 } });
                }
            }
            public partial class TCMoveToAttributeOrdinal : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCMoveToAttributeOrdinal
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(MoveToAttributeWithGetAttrDoubleQ) { Attribute = new VariationAttribute("MoveToAttribute(i) Verify with This[i] - Double Quote") { Priority = 0 } });
                    this.AddChild(new TestVariation(MoveToAttributeWithGetAttrSingleQ) { Attribute = new VariationAttribute("MoveToAttribute(i) Verify with This[i] - Single Quote") });
                    this.AddChild(new TestVariation(MoveToAttributeWithMoveAttrDoubleQ) { Attribute = new VariationAttribute("MoveToAttribute(i) Verify with GetAttribute(i) - Double Quote") { Priority = 0 } });
                    this.AddChild(new TestVariation(MoveToAttributeWithMoveAttrSingleQ) { Attribute = new VariationAttribute("MoveToAttribute(i) Verify with GetAttribute[i] - Single Quote") });
                    this.AddChild(new TestVariation(NegativeOneOrdinal) { Attribute = new VariationAttribute("MoveToAttribute(i) NegativeOneOrdinal") { Priority = 0 } });
                    this.AddChild(new TestVariation(FieldCountOrdinal) { Attribute = new VariationAttribute("MoveToAttribute(i) FieldCountOrdinal") });
                    this.AddChild(new TestVariation(OrdinalPlusOne) { Attribute = new VariationAttribute("MoveToAttribute(i) OrdinalPlusOne") { Priority = 0 } });
                    this.AddChild(new TestVariation(OrdinalMinusOne) { Attribute = new VariationAttribute("MoveToAttribute(i) OrdinalMinusOne") });
                }
            }
            public partial class TCMoveToFirstAttribute : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCMoveToFirstAttribute
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(MoveToFirstAttribute1) { Attribute = new VariationAttribute("MoveToFirstAttribute() When AttributeCount=0, <EMPTY1/> ") { Priority = 0 } });
                    this.AddChild(new TestVariation(MoveToFirstAttribute2) { Attribute = new VariationAttribute("MoveToFirstAttribute() When AttributeCount=0, <NONEMPTY1>ABCDE</NONEMPTY1> ") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute3) { Attribute = new VariationAttribute("MoveToFirstAttribute() When iOrdinal=0, with namespace") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute4) { Attribute = new VariationAttribute("MoveToFirstAttribute() When iOrdinal=0, without namespace") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute5) { Attribute = new VariationAttribute("MoveToFirstAttribute() When iOrdinal=mIddle, with namespace") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute6) { Attribute = new VariationAttribute("MoveToFirstAttribute() When iOrdinal=mIddle, without namespace") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute7) { Attribute = new VariationAttribute("MoveToFirstAttribute() When iOrdinal=end, with namespace") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute8) { Attribute = new VariationAttribute("MoveToFirstAttribute() When iOrdinal=end, without namespace") });
                }
            }
            public partial class TCMoveToNextAttribute : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCMoveToNextAttribute
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(MoveToNextAttribute1) { Attribute = new VariationAttribute("MoveToNextAttribute() When AttributeCount=0, <EMPTY1/> ") { Priority = 0 } });
                    this.AddChild(new TestVariation(MoveToNextAttribute2) { Attribute = new VariationAttribute("MoveToNextAttribute() When AttributeCount=0, <NONEMPTY1>ABCDE</NONEMPTY1> ") });
                    this.AddChild(new TestVariation(MoveToNextAttribute3) { Attribute = new VariationAttribute("MoveToNextAttribute() When iOrdinal=0, with namespace") });
                    this.AddChild(new TestVariation(MoveToNextAttribute4) { Attribute = new VariationAttribute("MoveToNextAttribute() When iOrdinal=0, without namespace") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute5) { Attribute = new VariationAttribute("MoveToFirstAttribute() When iOrdinal=mIddle, with namespace") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute6) { Attribute = new VariationAttribute("MoveToFirstAttribute() When iOrdinal=mIddle, without namespace") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute7) { Attribute = new VariationAttribute("MoveToFirstAttribute() When iOrdinal=end, with namespace") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute8) { Attribute = new VariationAttribute("MoveToFirstAttribute() When iOrdinal=end, without namespace") });
                }
            }
            public partial class TCAttributeTest : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCAttributeTest
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_None) { Attribute = new VariationAttribute("Attribute Test On None") });
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_Element) { Attribute = new VariationAttribute("Attribute Test  On Element") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_Text) { Attribute = new VariationAttribute("Attribute Test On Text") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_CDATA) { Attribute = new VariationAttribute("Attribute Test On CDATA") });
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_ProcessingInstruction) { Attribute = new VariationAttribute("Attribute Test On ProcessingInstruction") });
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_Comment) { Attribute = new VariationAttribute("AttributeTest On Comment") });
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_DocumentType) { Attribute = new VariationAttribute("AttributeTest On DocumentType") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_Whitespace) { Attribute = new VariationAttribute("AttributeTest On Whitespace") });
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_EndElement) { Attribute = new VariationAttribute("AttributeTest On EndElement") });
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_XmlDeclaration) { Attribute = new VariationAttribute("AttributeTest On XmlDeclaration") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestAttributeTestNodeType_EndEntity) { Attribute = new VariationAttribute("AttributeTest On EndEntity") });
                }
            }
            public partial class TCXmlns : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCXmlns
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TXmlns1) { Attribute = new VariationAttribute("Name, LocalName, Prefix and Value with xmlns=ns attribute") { Priority = 0 } });
                    this.AddChild(new TestVariation(TXmlns2) { Attribute = new VariationAttribute("Name, LocalName, Prefix and Value with xmlns:p=ns attribute") });
                    this.AddChild(new TestVariation(TXmlns3) { Attribute = new VariationAttribute("LookupNamespace with xmlns=ns attribute") });
                    this.AddChild(new TestVariation(TXmlns4) { Attribute = new VariationAttribute("MoveToAttribute access on xmlns attribute") });
                    this.AddChild(new TestVariation(TXmlns5) { Attribute = new VariationAttribute("GetAttribute access on xmlns attribute") });
                    this.AddChild(new TestVariation(TXmlns6) { Attribute = new VariationAttribute("this[xmlns] attribute access") });
                }
            }
            public partial class TCXmlnsPrefix : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCXmlnsPrefix
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TXmlnsPrefix1) { Attribute = new VariationAttribute("NamespaceURI of xmlns:a attribute") { Priority = 0 } });
                    this.AddChild(new TestVariation(TXmlnsPrefix2) { Attribute = new VariationAttribute("NamespaceURI of element/attribute with xmlns attribute") { Priority = 0 } });
                    this.AddChild(new TestVariation(TXmlnsPrefix3) { Attribute = new VariationAttribute("LookupNamespace with xmlns prefix") });
                    this.AddChild(new TestVariation(TXmlnsPrefix4) { Attribute = new VariationAttribute("Define prefix for 'www.w3.org/2000/xmlns'") { Priority = 0 } });
                    this.AddChild(new TestVariation(TXmlnsPrefix5) { Attribute = new VariationAttribute("Redefine namespace attached to xmlns prefix") });
                }
            }
            public partial class TCReadState : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadState
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(ReadState1) { Attribute = new VariationAttribute("XmlReader ReadState Initial") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadState2) { Attribute = new VariationAttribute("XmlReader ReadState Interactive") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadState3) { Attribute = new VariationAttribute("XmlReader ReadState EndOfFile") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadState4) { Attribute = new VariationAttribute("XmlReader ReadState Initial") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadState5) { Attribute = new VariationAttribute("XmlReader ReadState EndOfFile") { Priority = 0 } });
                }
            }
            public partial class TCReadInnerXml : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadInnerXml
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestReadInnerXml1) { Attribute = new VariationAttribute("ReadInnerXml on Empty Tag") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadInnerXml2) { Attribute = new VariationAttribute("ReadInnerXml on non Empty Tag") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadInnerXml3) { Attribute = new VariationAttribute("ReadInnerXml on non Empty Tag with text content") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadInnerXml6) { Attribute = new VariationAttribute("ReadInnerXml with multiple Level of elements") });
                    this.AddChild(new TestVariation(TestReadInnerXml7) { Attribute = new VariationAttribute("ReadInnerXml with multiple Level of elements, text and attributes") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadInnerXml8) { Attribute = new VariationAttribute("ReadInnerXml with entity references, EntityHandling = ExpandEntities") });
                    this.AddChild(new TestVariation(TestReadInnerXml9) { Attribute = new VariationAttribute("ReadInnerXml on attribute node") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadInnerXml10) { Attribute = new VariationAttribute("ReadInnerXml on attribute node with entity reference in value") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadInnerXml11) { Attribute = new VariationAttribute("ReadInnerXml on Text") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadInnerXml12) { Attribute = new VariationAttribute("ReadInnerXml on CDATA") });
                    this.AddChild(new TestVariation(TestReadInnerXml13) { Attribute = new VariationAttribute("ReadInnerXml on ProcessingInstruction") });
                    this.AddChild(new TestVariation(TestReadInnerXml14) { Attribute = new VariationAttribute("ReadInnerXml on Comment") });
                    this.AddChild(new TestVariation(TestReadInnerXml16) { Attribute = new VariationAttribute("ReadInnerXml on EndElement") });
                    this.AddChild(new TestVariation(TestReadInnerXml17) { Attribute = new VariationAttribute("ReadInnerXml on XmlDeclaration") });
                    this.AddChild(new TestVariation(TestReadInnerXml18) { Attribute = new VariationAttribute("Current node after ReadInnerXml on element") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadInnerXml19) { Attribute = new VariationAttribute("Current node after ReadInnerXml on element") });
                    this.AddChild(new TestVariation(TestTextReadInnerXml2) { Attribute = new VariationAttribute("ReadInnerXml with entity references, EntityHandling = ExpandCharEntities") });
                    this.AddChild(new TestVariation(TestTextReadInnerXml4) { Attribute = new VariationAttribute("ReadInnerXml on EntityReference") });
                    this.AddChild(new TestVariation(TestTextReadInnerXml5) { Attribute = new VariationAttribute("ReadInnerXml on EndEntity") });
                    this.AddChild(new TestVariation(TestTextReadInnerXml18) { Attribute = new VariationAttribute("One large element") });
                }
            }
            public partial class TCMoveToContent : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCMoveToContent
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestMoveToContent1) { Attribute = new VariationAttribute("MoveToContent on Skip XmlDeclaration") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestMoveToContent2) { Attribute = new VariationAttribute("MoveToContent on Read through All valid Content Node(Element, Text, CDATA, and EndElement)") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestMoveToContent3) { Attribute = new VariationAttribute("MoveToContent on Read through All invalid Content Node(PI, Comment and whitespace)") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestMoveToContent4) { Attribute = new VariationAttribute("MoveToContent on Read through Mix valid and Invalid Content Node") });
                    this.AddChild(new TestVariation(TestMoveToContent5) { Attribute = new VariationAttribute("MoveToContent on Attribute") { Priority = 0 } });
                }
            }
            public partial class TCIsStartElement : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCIsStartElement
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestIsStartElement1) { Attribute = new VariationAttribute("IsStartElement on Regular Element, no namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestIsStartElement2) { Attribute = new VariationAttribute("IsStartElement on Empty Element, no namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestIsStartElement3) { Attribute = new VariationAttribute("IsStartElement on regular Element, with namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestIsStartElement4) { Attribute = new VariationAttribute("IsStartElement on Empty Tag, with default namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestIsStartElement5) { Attribute = new VariationAttribute("IsStartElement with Name=String.Empty") });
                    this.AddChild(new TestVariation(TestIsStartElement6) { Attribute = new VariationAttribute("IsStartElement on Empty Element with Name and Namespace=String.Empty") });
                    this.AddChild(new TestVariation(TestIsStartElement7) { Attribute = new VariationAttribute("IsStartElement on CDATA") });
                    this.AddChild(new TestVariation(TestIsStartElement8) { Attribute = new VariationAttribute("IsStartElement on EndElement, no namespace") });
                    this.AddChild(new TestVariation(TestIsStartElement9) { Attribute = new VariationAttribute("IsStartElement on EndElement, with namespace") });
                    this.AddChild(new TestVariation(TestIsStartElement10) { Attribute = new VariationAttribute("IsStartElement on Attribute") });
                    this.AddChild(new TestVariation(TestIsStartElement11) { Attribute = new VariationAttribute("IsStartElement on Text") });
                    this.AddChild(new TestVariation(TestIsStartElement12) { Attribute = new VariationAttribute("IsStartElement on ProcessingInstruction") });
                    this.AddChild(new TestVariation(TestIsStartElement13) { Attribute = new VariationAttribute("IsStartElement on Comment") });
                }
            }
            public partial class TCReadStartElement : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadStartElement
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestReadStartElement1) { Attribute = new VariationAttribute("ReadStartElement on Regular Element, no namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadStartElement2) { Attribute = new VariationAttribute("ReadStartElement on Empty Element, no namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadStartElement3) { Attribute = new VariationAttribute("ReadStartElement on regular Element, with namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadStartElement4) { Attribute = new VariationAttribute("Passing ns=String.EmptyErrorCase: ReadStartElement on regular Element, with namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadStartElement5) { Attribute = new VariationAttribute("Passing no ns: ReadStartElement on regular Element, with namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadStartElement6) { Attribute = new VariationAttribute("ReadStartElement on Empty Tag, with namespace") });
                    this.AddChild(new TestVariation(TestReadStartElement7) { Attribute = new VariationAttribute("ErrorCase: ReadStartElement on Empty Tag, with namespace, passing ns=String.Empty") });
                    this.AddChild(new TestVariation(TestReadStartElement8) { Attribute = new VariationAttribute("ReadStartElement on Empty Tag, with namespace, passing no ns") });
                    this.AddChild(new TestVariation(TestReadStartElement9) { Attribute = new VariationAttribute("ReadStartElement with Name=String.Empty") });
                    this.AddChild(new TestVariation(TestReadStartElement10) { Attribute = new VariationAttribute("ReadStartElement on Empty Element with Name and Namespace=String.Empty") });
                    this.AddChild(new TestVariation(TestReadStartElement11) { Attribute = new VariationAttribute("ReadStartElement on CDATA") });
                    this.AddChild(new TestVariation(TestReadStartElement12) { Attribute = new VariationAttribute("ReadStartElement() on EndElement, no namespace") });
                    this.AddChild(new TestVariation(TestReadStartElement13) { Attribute = new VariationAttribute("ReadStartElement(n) on EndElement, no namespace") });
                    this.AddChild(new TestVariation(TestReadStartElement14) { Attribute = new VariationAttribute("ReadStartElement(n, String.Empty) on EndElement, no namespace") });
                    this.AddChild(new TestVariation(TestReadStartElement15) { Attribute = new VariationAttribute("ReadStartElement() on EndElement, with namespace") });
                    this.AddChild(new TestVariation(TestReadStartElement16) { Attribute = new VariationAttribute("ReadStartElement(n,ns) on EndElement, with namespace") });
                }
            }
            public partial class TCReadEndElement : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadEndElement
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestReadEndElement3) { Attribute = new VariationAttribute("ReadEndElement on Start Element, no namespace") });
                    this.AddChild(new TestVariation(TestReadEndElement4) { Attribute = new VariationAttribute("ReadEndElement on Empty Element, no namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadEndElement5) { Attribute = new VariationAttribute("ReadEndElement on regular Element, with namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadEndElement6) { Attribute = new VariationAttribute("ReadEndElement on Empty Tag, with namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadEndElement7) { Attribute = new VariationAttribute("ReadEndElement on CDATA") });
                    this.AddChild(new TestVariation(TestReadEndElement9) { Attribute = new VariationAttribute("ReadEndElement on Text") });
                    this.AddChild(new TestVariation(TestReadEndElement10) { Attribute = new VariationAttribute("ReadEndElement on ProcessingInstruction") });
                    this.AddChild(new TestVariation(TestReadEndElement11) { Attribute = new VariationAttribute("ReadEndElement on Comment") });
                    this.AddChild(new TestVariation(TestReadEndElement13) { Attribute = new VariationAttribute("ReadEndElement on XmlDeclaration") });
                    this.AddChild(new TestVariation(TestTextReadEndElement1) { Attribute = new VariationAttribute("ReadEndElement on EntityReference") });
                    this.AddChild(new TestVariation(TestTextReadEndElement2) { Attribute = new VariationAttribute("ReadEndElement on EndEntity") });
                }
            }

            public partial class TCMoveToElement : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCMoveToElement
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(v1) { Attribute = new VariationAttribute("Attribute node") });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("Element node") });
                    this.AddChild(new TestVariation(v5) { Attribute = new VariationAttribute("Comment node") });
                }
            }
            public partial class ErrorConditions : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+ErrorConditions
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(Variation3) { Attribute = new VariationAttribute("IsStartElement") });
                    this.AddChild(new TestVariation(Variation4) { Attribute = new VariationAttribute("LookupNamespace") });
                    this.AddChild(new TestVariation(Variation5) { Attribute = new VariationAttribute("MoveToAttribute") });
                    this.AddChild(new TestVariation(Variation6) { Attribute = new VariationAttribute("Other APIs") });
                    this.AddChild(new TestVariation(Variation7) { Attribute = new VariationAttribute("ReadContentAs(null, null)") });
                    this.AddChild(new TestVariation(Variation8) { Attribute = new VariationAttribute("ReadContentAsBase64") });
                    this.AddChild(new TestVariation(Variation9) { Attribute = new VariationAttribute("ReadContentAsBinHex") });
                    this.AddChild(new TestVariation(Variation10) { Attribute = new VariationAttribute("ReadContentAsBoolean") });
                    this.AddChild(new TestVariation(Variation11b) { Attribute = new VariationAttribute("ReadContentAsDateTimeOffset") });
                    this.AddChild(new TestVariation(Variation12) { Attribute = new VariationAttribute("ReadContentAsDecimal") });
                    this.AddChild(new TestVariation(Variation13) { Attribute = new VariationAttribute("ReadContentAsDouble") });
                    this.AddChild(new TestVariation(Variation14) { Attribute = new VariationAttribute("ReadContentAsFloat") });
                    this.AddChild(new TestVariation(Variation15) { Attribute = new VariationAttribute("ReadContentAsInt") });
                    this.AddChild(new TestVariation(Variation16) { Attribute = new VariationAttribute("ReadContentAsLong") });
                    this.AddChild(new TestVariation(Variation17) { Attribute = new VariationAttribute("ReadElementContentAs(null, null)") });
                    this.AddChild(new TestVariation(Variation18) { Attribute = new VariationAttribute("ReadElementContentAsBase64") });
                    this.AddChild(new TestVariation(Variation19) { Attribute = new VariationAttribute("ReadElementContentAsBinHex") });
                    this.AddChild(new TestVariation(Variation20) { Attribute = new VariationAttribute("ReadElementContentAsBoolean") });
                    this.AddChild(new TestVariation(Variation22) { Attribute = new VariationAttribute("ReadElementContentAsDecimal") });
                    this.AddChild(new TestVariation(Variation23) { Attribute = new VariationAttribute("ReadElementContentAsDouble") });
                    this.AddChild(new TestVariation(Variation24) { Attribute = new VariationAttribute("ReadElementContentAsFloat") });
                    this.AddChild(new TestVariation(Variation25) { Attribute = new VariationAttribute("ReadElementContentAsInt") });
                    this.AddChild(new TestVariation(Variation26) { Attribute = new VariationAttribute("ReadElementContentAsLong") });
                    this.AddChild(new TestVariation(Variation27) { Attribute = new VariationAttribute("ReadElementContentAsObject") });
                    this.AddChild(new TestVariation(Variation28) { Attribute = new VariationAttribute("ReadElementContentAsString") });
                    this.AddChild(new TestVariation(Variation30) { Attribute = new VariationAttribute("ReadStartElement") });
                    this.AddChild(new TestVariation(Variation31) { Attribute = new VariationAttribute("ReadToDescendant(null)") });
                    this.AddChild(new TestVariation(Variation32) { Attribute = new VariationAttribute("ReadToDescendant(String.Empty)") });
                    this.AddChild(new TestVariation(Variation33) { Attribute = new VariationAttribute("ReadToFollowing(null)") });
                    this.AddChild(new TestVariation(Variation34) { Attribute = new VariationAttribute("ReadToFollowing(String.Empty)") });
                    this.AddChild(new TestVariation(Variation35) { Attribute = new VariationAttribute("ReadToNextSibling(null)") });
                    this.AddChild(new TestVariation(Variation36) { Attribute = new VariationAttribute("ReadToNextSibling(String.Empty)") });
                    this.AddChild(new TestVariation(Variation37) { Attribute = new VariationAttribute("ReadValueChunk") });
                    this.AddChild(new TestVariation(Variation38) { Attribute = new VariationAttribute("ReadElementContentAsObject") });
                    this.AddChild(new TestVariation(Variation39) { Attribute = new VariationAttribute("ReadElementContentAsString") });
                }
            }
            public partial class TCXMLIntegrityBase : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCXMLIntegrityBase
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(GetXmlReaderNodeType) { Attribute = new VariationAttribute("NodeType") });
                    this.AddChild(new TestVariation(GetXmlReaderName) { Attribute = new VariationAttribute("Name") });
                    this.AddChild(new TestVariation(GetXmlReaderLocalName) { Attribute = new VariationAttribute("LocalName") });
                    this.AddChild(new TestVariation(Namespace) { Attribute = new VariationAttribute("NamespaceURI") });
                    this.AddChild(new TestVariation(Prefix) { Attribute = new VariationAttribute("Prefix") });
                    this.AddChild(new TestVariation(HasValue) { Attribute = new VariationAttribute("HasValue") });
                    this.AddChild(new TestVariation(GetXmlReaderValue) { Attribute = new VariationAttribute("Value") });
                    this.AddChild(new TestVariation(GetDepth) { Attribute = new VariationAttribute("Depth") });
                    this.AddChild(new TestVariation(GetBaseURI) { Attribute = new VariationAttribute("BaseURI") });
                    this.AddChild(new TestVariation(IsEmptyElement) { Attribute = new VariationAttribute("IsEmptyElement") });
                    this.AddChild(new TestVariation(IsDefault) { Attribute = new VariationAttribute("IsDefault") });
                    this.AddChild(new TestVariation(GetXmlSpace) { Attribute = new VariationAttribute("XmlSpace") });
                    this.AddChild(new TestVariation(GetXmlLang) { Attribute = new VariationAttribute("XmlLang") });
                    this.AddChild(new TestVariation(AttributeCount) { Attribute = new VariationAttribute("AttributeCount") });
                    this.AddChild(new TestVariation(HasAttribute) { Attribute = new VariationAttribute("HasAttributes") });
                    this.AddChild(new TestVariation(GetAttributeName) { Attribute = new VariationAttribute("GetAttributes(name)") });
                    this.AddChild(new TestVariation(GetAttributeEmptyName) { Attribute = new VariationAttribute("GetAttribute(String.Empty)") });
                    this.AddChild(new TestVariation(GetAttributeNameNamespace) { Attribute = new VariationAttribute("GetAttribute(name,ns)") });
                    this.AddChild(new TestVariation(GetAttributeEmptyNameNamespace) { Attribute = new VariationAttribute("GetAttribute(String.Empty, String.Empty)") });
                    this.AddChild(new TestVariation(GetAttributeOrdinal) { Attribute = new VariationAttribute("GetAttribute(i)") });
                    this.AddChild(new TestVariation(HelperThisOrdinal) { Attribute = new VariationAttribute("this[i]") });
                    this.AddChild(new TestVariation(HelperThisName) { Attribute = new VariationAttribute("this[name]") });
                    this.AddChild(new TestVariation(HelperThisNameNamespace) { Attribute = new VariationAttribute("this[name,namespace]") });
                    this.AddChild(new TestVariation(MoveToAttributeName) { Attribute = new VariationAttribute("MoveToAttribute(name)") });
                    this.AddChild(new TestVariation(MoveToAttributeNameNamespace) { Attribute = new VariationAttribute("MoveToAttributeNameNamespace(name,ns)") });
                    this.AddChild(new TestVariation(MoveToAttributeOrdinal) { Attribute = new VariationAttribute("MoveToAttribute(i)") });
                    this.AddChild(new TestVariation(MoveToFirstAttribute) { Attribute = new VariationAttribute("MoveToFirstAttribute()") });
                    this.AddChild(new TestVariation(MoveToNextAttribute) { Attribute = new VariationAttribute("MoveToNextAttribute()") });
                    this.AddChild(new TestVariation(MoveToElement) { Attribute = new VariationAttribute("MoveToElement()") });
                    this.AddChild(new TestVariation(ReadTestAfterClose) { Attribute = new VariationAttribute("Read") });
                    this.AddChild(new TestVariation(GetEOF) { Attribute = new VariationAttribute("GetEOF") });
                    this.AddChild(new TestVariation(GetReadState) { Attribute = new VariationAttribute("GetReadState") });
                    this.AddChild(new TestVariation(XMLSkip) { Attribute = new VariationAttribute("Skip") });
                    this.AddChild(new TestVariation(TestNameTable) { Attribute = new VariationAttribute("NameTable") });
                    this.AddChild(new TestVariation(ReadInnerXmlTestAfterClose) { Attribute = new VariationAttribute("ReadInnerXml") });
                    this.AddChild(new TestVariation(TestReadOuterXml) { Attribute = new VariationAttribute("ReadOuterXml") });
                    this.AddChild(new TestVariation(TestMoveToContent) { Attribute = new VariationAttribute("MoveToContent") });
                    this.AddChild(new TestVariation(TestIsStartElement) { Attribute = new VariationAttribute("IsStartElement") });
                    this.AddChild(new TestVariation(TestIsStartElementName) { Attribute = new VariationAttribute("IsStartElement(name)") });
                    this.AddChild(new TestVariation(TestIsStartElementName2) { Attribute = new VariationAttribute("IsStartElement(String.Empty)") });
                    this.AddChild(new TestVariation(TestIsStartElementNameNs) { Attribute = new VariationAttribute("IsStartElement(name, ns)") });
                    this.AddChild(new TestVariation(TestIsStartElementNameNs2) { Attribute = new VariationAttribute("IsStartElement(String.Empty,String.Empty)") });
                    this.AddChild(new TestVariation(TestReadStartElement) { Attribute = new VariationAttribute("ReadStartElement") });
                    this.AddChild(new TestVariation(TestReadStartElementName) { Attribute = new VariationAttribute("ReadStartElement(name)") });
                    this.AddChild(new TestVariation(TestReadStartElementName2) { Attribute = new VariationAttribute("ReadStartElement(String.Empty)") });
                    this.AddChild(new TestVariation(TestReadStartElementNameNs) { Attribute = new VariationAttribute("ReadStartElement(name, ns)") });
                    this.AddChild(new TestVariation(TestReadStartElementNameNs2) { Attribute = new VariationAttribute("ReadStartElement(String.Empty,String.Empty)") });
                    this.AddChild(new TestVariation(TestReadEndElement) { Attribute = new VariationAttribute("ReadEndElement") });
                    this.AddChild(new TestVariation(LookupNamespace) { Attribute = new VariationAttribute("LookupNamespace") });
                    this.AddChild(new TestVariation(ReadAttributeValue) { Attribute = new VariationAttribute("ReadAttributeValue") });
                    this.AddChild(new TestVariation(CloseTest) { Attribute = new VariationAttribute("Close") });
                }
            }
            public partial class TCReadContentAsBase64 : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadContentAsBase64
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestReadBase64_1) { Attribute = new VariationAttribute("ReadBase64 Element with all valid value") });
                    this.AddChild(new TestVariation(TestReadBase64_2) { Attribute = new VariationAttribute("ReadBase64 Element with all valid Num value") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadBase64_3) { Attribute = new VariationAttribute("ReadBase64 Element with all valid Text value") });
                    this.AddChild(new TestVariation(TestReadBase64_5) { Attribute = new VariationAttribute("ReadBase64 Element with all valid value (from concatenation), Priority=0") });
                    this.AddChild(new TestVariation(TestReadBase64_6) { Attribute = new VariationAttribute("ReadBase64 Element with Long valid value (from concatenation), Priority=0") });
                    this.AddChild(new TestVariation(ReadBase64_7) { Attribute = new VariationAttribute("ReadBase64 with count > buffer size") });
                    this.AddChild(new TestVariation(ReadBase64_8) { Attribute = new VariationAttribute("ReadBase64 with count < 0") });
                    this.AddChild(new TestVariation(ReadBase64_9) { Attribute = new VariationAttribute("ReadBase64 with index > buffer size") });
                    this.AddChild(new TestVariation(ReadBase64_10) { Attribute = new VariationAttribute("ReadBase64 with index < 0") });
                    this.AddChild(new TestVariation(ReadBase64_11) { Attribute = new VariationAttribute("ReadBase64 with index + count exceeds buffer") });
                    this.AddChild(new TestVariation(ReadBase64_12) { Attribute = new VariationAttribute("ReadBase64 index & count =0") });
                    this.AddChild(new TestVariation(TestReadBase64_13) { Attribute = new VariationAttribute("ReadBase64 Element multiple into same buffer (using offset), Priority=0") });
                    this.AddChild(new TestVariation(TestReadBase64_14) { Attribute = new VariationAttribute("ReadBase64 with buffer == null") });
                    this.AddChild(new TestVariation(TestReadBase64_15) { Attribute = new VariationAttribute("ReadBase64 after failure") });
                    this.AddChild(new TestVariation(TestReadBase64_16) { Attribute = new VariationAttribute("Read after partial ReadBase64") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadBase64_17) { Attribute = new VariationAttribute("Current node on multiple calls") });
                    this.AddChild(new TestVariation(TestReadBase64_18) { Attribute = new VariationAttribute("No op node types") });
                    this.AddChild(new TestVariation(TestTextReadBase64_23) { Attribute = new VariationAttribute("ReadBase64 with incomplete sequence") });
                    this.AddChild(new TestVariation(TestTextReadBase64_24) { Attribute = new VariationAttribute("ReadBase64 when end tag doesn't exist") });
                    this.AddChild(new TestVariation(TestTextReadBase64_26) { Attribute = new VariationAttribute("ReadBase64 with whitespace in the mIddle") });
                    this.AddChild(new TestVariation(TestTextReadBase64_27) { Attribute = new VariationAttribute("ReadBase64 with = in the mIddle") });
                    this.AddChild(new TestVariation(RunBase64DoesnNotRunIntoOverflow) { Attribute = new VariationAttribute("ReadBase64 runs into an Overflow") { Params = new object[] { "10000000" } } });
                    this.AddChild(new TestVariation(RunBase64DoesnNotRunIntoOverflow) { Attribute = new VariationAttribute("ReadBase64 runs into an Overflow") { Params = new object[] { "1000000" } } });
                    this.AddChild(new TestVariation(RunBase64DoesnNotRunIntoOverflow) { Attribute = new VariationAttribute("ReadBase64 runs into an Overflow") { Params = new object[] { "10000" } } });
                }
            }
            public partial class TCReadElementContentAsBase64 : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadElementContentAsBase64
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestReadBase64_1) { Attribute = new VariationAttribute("ReadBase64 Element with all valid value") });
                    this.AddChild(new TestVariation(TestReadBase64_2) { Attribute = new VariationAttribute("ReadBase64 Element with all valid Num value") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadBase64_3) { Attribute = new VariationAttribute("ReadBase64 Element with all valid Text value") });
                    this.AddChild(new TestVariation(TestReadBase64_5) { Attribute = new VariationAttribute("ReadBase64 Element with all valid value (from concatenation), Priority=0") });
                    this.AddChild(new TestVariation(TestReadBase64_6) { Attribute = new VariationAttribute("ReadBase64 Element with Long valid value (from concatenation), Priority=0") });
                    this.AddChild(new TestVariation(ReadBase64_7) { Attribute = new VariationAttribute("ReadBase64 with count > buffer size") });
                    this.AddChild(new TestVariation(ReadBase64_8) { Attribute = new VariationAttribute("ReadBase64 with count < 0") });
                    this.AddChild(new TestVariation(ReadBase64_9) { Attribute = new VariationAttribute("ReadBase64 with index > buffer size") });
                    this.AddChild(new TestVariation(ReadBase64_10) { Attribute = new VariationAttribute("ReadBase64 with index < 0") });
                    this.AddChild(new TestVariation(ReadBase64_11) { Attribute = new VariationAttribute("ReadBase64 with index + count exceeds buffer") });
                    this.AddChild(new TestVariation(ReadBase64_12) { Attribute = new VariationAttribute("ReadBase64 index & count =0") });
                    this.AddChild(new TestVariation(TestReadBase64_13) { Attribute = new VariationAttribute("ReadBase64 Element multiple into same buffer (using offset), Priority=0") });
                    this.AddChild(new TestVariation(TestReadBase64_14) { Attribute = new VariationAttribute("ReadBase64 with buffer == null") });
                    this.AddChild(new TestVariation(TestReadBase64_15) { Attribute = new VariationAttribute("ReadBase64 after failure") });
                    this.AddChild(new TestVariation(TestReadBase64_16) { Attribute = new VariationAttribute("Read after partial ReadBase64") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadBase64_17) { Attribute = new VariationAttribute("Current node on multiple calls") });
                    this.AddChild(new TestVariation(TestTextReadBase64_23) { Attribute = new VariationAttribute("ReadBase64 with incomplete sequence") });
                    this.AddChild(new TestVariation(TestTextReadBase64_24) { Attribute = new VariationAttribute("ReadBase64 when end tag doesn't exist") });
                    this.AddChild(new TestVariation(TestTextReadBase64_26) { Attribute = new VariationAttribute("ReadBase64 with whitespace in the mIddle") });
                    this.AddChild(new TestVariation(TestTextReadBase64_27) { Attribute = new VariationAttribute("ReadBase64 with = in the mIddle") });
                    this.AddChild(new TestVariation(ReadBase64DoesNotRunIntoOverflow2) { Attribute = new VariationAttribute("105376: ReadBase64 runs into an Overflow") { Params = new object[] { "10000000" } } });
                    this.AddChild(new TestVariation(ReadBase64DoesNotRunIntoOverflow2) { Attribute = new VariationAttribute("105376: ReadBase64 runs into an Overflow") { Params = new object[] { "1000000" } } });
                    this.AddChild(new TestVariation(ReadBase64DoesNotRunIntoOverflow2) { Attribute = new VariationAttribute("105376: ReadBase64 runs into an Overflow") { Params = new object[] { "10000" } } });
                    this.AddChild(new TestVariation(SubtreeReaderInsertedAttributesWontWorkWithReadContentAsBase64) { Attribute = new VariationAttribute("430329: SubtreeReader inserted attributes don't work with ReadContentAsBase64") });
                }
            }
            public partial class TCReadContentAsBinHex : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadContentAsBinHex
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestReadBinHex_1) { Attribute = new VariationAttribute("ReadBinHex Element with all valid value") });
                    this.AddChild(new TestVariation(TestReadBinHex_2) { Attribute = new VariationAttribute("ReadBinHex Element with all valid Num value") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadBinHex_3) { Attribute = new VariationAttribute("ReadBinHex Element with all valid Text value") });
                    this.AddChild(new TestVariation(TestReadBinHex_4) { Attribute = new VariationAttribute("ReadBinHex Element on CDATA") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadBinHex_5) { Attribute = new VariationAttribute("ReadBinHex Element with all valid value (from concatenation), Priority=0") });
                    this.AddChild(new TestVariation(TestReadBinHex_6) { Attribute = new VariationAttribute("ReadBinHex Element with all long valid value (from concatenation)") });
                    this.AddChild(new TestVariation(TestReadBinHex_7) { Attribute = new VariationAttribute("ReadBinHex with count > buffer size") });
                    this.AddChild(new TestVariation(TestReadBinHex_8) { Attribute = new VariationAttribute("ReadBinHex with count < 0") });
                    this.AddChild(new TestVariation(vReadBinHex_9) { Attribute = new VariationAttribute("ReadBinHex with index > buffer size") });
                    this.AddChild(new TestVariation(TestReadBinHex_10) { Attribute = new VariationAttribute("ReadBinHex with index < 0") });
                    this.AddChild(new TestVariation(TestReadBinHex_11) { Attribute = new VariationAttribute("ReadBinHex with index + count exceeds buffer") });
                    this.AddChild(new TestVariation(TestReadBinHex_12) { Attribute = new VariationAttribute("ReadBinHex index & count =0") });
                    this.AddChild(new TestVariation(TestReadBinHex_13) { Attribute = new VariationAttribute("ReadBinHex Element multiple into same buffer (using offset), Priority=0") });
                    this.AddChild(new TestVariation(TestReadBinHex_14) { Attribute = new VariationAttribute("ReadBinHex with buffer == null") });
                    this.AddChild(new TestVariation(TestReadBinHex_15) { Attribute = new VariationAttribute("ReadBinHex after failed ReadBinHex") });
                    this.AddChild(new TestVariation(TestReadBinHex_16) { Attribute = new VariationAttribute("Read after partial ReadBinHex") });
                    this.AddChild(new TestVariation(TestReadBinHex_17) { Attribute = new VariationAttribute("Current node on multiple calls") });
                    this.AddChild(new TestVariation(TestTextReadBinHex_21) { Attribute = new VariationAttribute("ReadBinHex with whitespace") });
                    this.AddChild(new TestVariation(TestTextReadBinHex_22) { Attribute = new VariationAttribute("ReadBinHex with odd number of chars") });
                    this.AddChild(new TestVariation(TestTextReadBinHex_23) { Attribute = new VariationAttribute("ReadBinHex when end tag doesn't exist") });
                    this.AddChild(new TestVariation(TestTextReadBinHex_24) { Attribute = new VariationAttribute("WS:WireCompat:hex binary fails to send/return data after 1787 bytes going Whidbey to Everett") });
                    this.AddChild(new TestVariation(DebugAssertInReadContentAsBinHex) { Attribute = new VariationAttribute("DebugAssert in ReadContentAsBinHex") });
                }
            }
            public partial class TCReadElementContentAsBinHex : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadElementContentAsBinHex
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestReadBinHex_1) { Attribute = new VariationAttribute("ReadBinHex Element with all valid value") });
                    this.AddChild(new TestVariation(TestReadBinHex_2) { Attribute = new VariationAttribute("ReadBinHex Element with all valid Num value") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadBinHex_3) { Attribute = new VariationAttribute("ReadBinHex Element with all valid Text value") });
                    this.AddChild(new TestVariation(TestReadBinHex_4) { Attribute = new VariationAttribute("ReadBinHex Element with Comments and PIs") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadBinHex_5) { Attribute = new VariationAttribute("ReadBinHex Element with all valid value (from concatenation), Priority=0") });
                    this.AddChild(new TestVariation(TestReadBinHex_6) { Attribute = new VariationAttribute("ReadBinHex Element with all long valid value (from concatenation)") });
                    this.AddChild(new TestVariation(TestReadBinHex_7) { Attribute = new VariationAttribute("ReadBinHex with count > buffer size") });
                    this.AddChild(new TestVariation(TestReadBinHex_8) { Attribute = new VariationAttribute("ReadBinHex with count < 0") });
                    this.AddChild(new TestVariation(vReadBinHex_9) { Attribute = new VariationAttribute("ReadBinHex with index > buffer size") });
                    this.AddChild(new TestVariation(TestReadBinHex_10) { Attribute = new VariationAttribute("ReadBinHex with index < 0") });
                    this.AddChild(new TestVariation(TestReadBinHex_11) { Attribute = new VariationAttribute("ReadBinHex with index + count exceeds buffer") });
                    this.AddChild(new TestVariation(TestReadBinHex_12) { Attribute = new VariationAttribute("ReadBinHex index & count =0") });
                    this.AddChild(new TestVariation(TestReadBinHex_13) { Attribute = new VariationAttribute("ReadBinHex Element multiple into same buffer (using offset), Priority=0") });
                    this.AddChild(new TestVariation(TestReadBinHex_14) { Attribute = new VariationAttribute("ReadBinHex with buffer == null") });
                    this.AddChild(new TestVariation(TestReadBinHex_15) { Attribute = new VariationAttribute("ReadBinHex after failed ReadBinHex") });
                    this.AddChild(new TestVariation(TestReadBinHex_16) { Attribute = new VariationAttribute("Read after partial ReadBinHex") });
                    this.AddChild(new TestVariation(TestTextReadBinHex_21) { Attribute = new VariationAttribute("ReadBinHex with whitespace") });
                    this.AddChild(new TestVariation(TestTextReadBinHex_22) { Attribute = new VariationAttribute("ReadBinHex with odd number of chars") });
                    this.AddChild(new TestVariation(TestTextReadBinHex_23) { Attribute = new VariationAttribute("ReadBinHex when end tag doesn't exist") });
                    this.AddChild(new TestVariation(TestTextReadBinHex_24) { Attribute = new VariationAttribute("WS:WireCompat:hex binary fails to send/return data after 1787 bytes going Whidbey to Everett") });
                    this.AddChild(new TestVariation(TestTextReadBinHex_25) { Attribute = new VariationAttribute("SubtreeReader inserted attributes don't work with ReadContentAsBinHex") });
                }
            }
            public partial class CReaderTestModule : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+CReaderTestModule
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(v1) { Attribute = new VariationAttribute("Reader Property empty doc") { Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("Reader Property after Read") { Priority = 0 } });
                    this.AddChild(new TestVariation(v3) { Attribute = new VariationAttribute("Reader Property after EOF") { Priority = 0 } });
                    this.AddChild(new TestVariation(v4) { Attribute = new VariationAttribute("Default Reader Settings") { Priority = 0 } });
                }
            }
            public partial class TCReadOuterXml : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadOuterXml
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(ReadOuterXml1) { Attribute = new VariationAttribute("ReadOuterXml on empty element w/o attributes") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadOuterXml2) { Attribute = new VariationAttribute("ReadOuterXml on empty element w/ attributes") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadOuterXml3) { Attribute = new VariationAttribute("ReadOuterXml on full empty element w/o attributes") });
                    this.AddChild(new TestVariation(ReadOuterXml4) { Attribute = new VariationAttribute("ReadOuterXml on full empty element w/ attributes") });
                    this.AddChild(new TestVariation(ReadOuterXml5) { Attribute = new VariationAttribute("ReadOuterXml on element with text content") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadOuterXml6) { Attribute = new VariationAttribute("ReadOuterXml on element with attributes") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadOuterXml7) { Attribute = new VariationAttribute("ReadOuterXml on element with text and markup content") });
                    this.AddChild(new TestVariation(ReadOuterXml8) { Attribute = new VariationAttribute("ReadOuterXml with multiple level of elements") });
                    this.AddChild(new TestVariation(ReadOuterXml9) { Attribute = new VariationAttribute("ReadOuterXml with multiple level of elements, text and attributes") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadOuterXml10) { Attribute = new VariationAttribute("ReadOuterXml on element with complex content (CDATA, PIs, Comments)") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadOuterXml12) { Attribute = new VariationAttribute("ReadOuterXml on attribute node of empty element") });
                    this.AddChild(new TestVariation(ReadOuterXml13) { Attribute = new VariationAttribute("ReadOuterXml on attribute node of full empty element") });
                    this.AddChild(new TestVariation(ReadOuterXml14) { Attribute = new VariationAttribute("ReadOuterXml on attribute node") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadOuterXml15) { Attribute = new VariationAttribute("ReadOuterXml on attribute with entities, EntityHandling = ExpandEntities") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReadOuterXml17) { Attribute = new VariationAttribute("ReadOuterXml on ProcessingInstruction") });
                    this.AddChild(new TestVariation(ReadOuterXml24) { Attribute = new VariationAttribute("ReadOuterXml on CDATA") });
                    this.AddChild(new TestVariation(TRReadOuterXml27) { Attribute = new VariationAttribute("ReadOuterXml on element with entities, EntityHandling = ExpandCharEntities") });
                    this.AddChild(new TestVariation(TRReadOuterXml28) { Attribute = new VariationAttribute("ReadOuterXml on attribute with entities, EntityHandling = ExpandCharEntities") });
                    this.AddChild(new TestVariation(TestTextReadOuterXml29) { Attribute = new VariationAttribute("One large element") });
                    this.AddChild(new TestVariation(ReadOuterXmlWhenNamespacesEqualsToFalseAndHasAnAttributeXmlns) { Attribute = new VariationAttribute("Read OuterXml when Namespaces=false and has an attribute xmlns") });
                }
            }
            public partial class TCReadSubtree : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadSubtree
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(ReadSubtreeOnlyWorksOnElementNode) { Attribute = new VariationAttribute("ReadSubtree only works on Element Node") });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("ReadSubtree Test depth=1") { Params = new object[] { "elem1", "", "ELEMENT", "elem5", "", "ELEMENT" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("ReadSubtree Test depth=2") { Params = new object[] { "elem2", "", "ELEMENT", "elem1", "", "ENDELEMENT" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("ReadSubtree Test empty element") { Params = new object[] { "elem5", "", "ELEMENT", "elem6", "", "ELEMENT" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("ReadSubtree Test on Root") { Params = new object[] { "root", "", "ELEMENT", "", "", "NONE" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("ReadSubtree Test Comment after element") { Params = new object[] { "elem", "", "ELEMENT", "", "Comment", "COMMENT" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("ReadSubtree Test depth=4") { Params = new object[] { "elem4", "", "ELEMENT", "x:elem3", "", "ENDELEMENT" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("ReadSubtree Test depth=3") { Params = new object[] { "x:elem3", "", "ELEMENT", "elem2", "", "ENDELEMENT" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("ReadSubtree Test empty element before root") { Params = new object[] { "elem6", "", "ELEMENT", "root", "", "ENDELEMENT" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("ReadSubtree Test PI after element") { Params = new object[] { "elempi", "", "ELEMENT", "pi", "target", "PROCESSINGINSTRUCTION" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v3) { Attribute = new VariationAttribute("Read with entities") { Priority = 1 } });
                    this.AddChild(new TestVariation(v4) { Attribute = new VariationAttribute("Inner XML on Subtree reader") { Priority = 1 } });
                    this.AddChild(new TestVariation(v5) { Attribute = new VariationAttribute("Outer XML on Subtree reader") { Priority = 1 } });
                    this.AddChild(new TestVariation(v6) { Attribute = new VariationAttribute("ReadString on Subtree reader") { Priority = 1 } });
                    this.AddChild(new TestVariation(v7) { Attribute = new VariationAttribute("Close on inner reader with CloseInput should not close the outer reader") { Params = new object[] { "true" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v7) { Attribute = new VariationAttribute("Close on inner reader with CloseInput should not close the outer reader") { Params = new object[] { "false" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v8) { Attribute = new VariationAttribute("Nested Subtree reader calls") { Priority = 2 } });
                    this.AddChild(new TestVariation(v100) { Attribute = new VariationAttribute("ReadSubtree for element depth more than 4K chars") { Priority = 2 } });
                    this.AddChild(new TestVariation(MultipleNamespacesOnSubtreeReader) { Attribute = new VariationAttribute("Multiple Namespaces on Subtree reader") { Priority = 1 } });
                    this.AddChild(new TestVariation(SubtreeReaderCachesNodeTypeAndReportsNodeTypeOfAttributeOnSubsequentReads) { Attribute = new VariationAttribute("Subtree Reader caches the NodeType and reports node type of Attribute on subsequent reads.") { Priority = 1 } });
                }
            }
            public partial class TCReadToDescendant : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadToDescendant
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(v) { Attribute = new VariationAttribute("Simple positive test") { Params = new object[] { "NNS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v) { Attribute = new VariationAttribute("Simple positive test") { Params = new object[] { "DNS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v) { Attribute = new VariationAttribute("Simple positive test") { Params = new object[] { "NS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("Read on a deep tree at least more than 4K boundary") { Priority = 2 } });
                    this.AddChild(new TestVariation(v3) { Attribute = new VariationAttribute("Read on descendant with same names") { Params = new object[] { "DNS" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v3) { Attribute = new VariationAttribute("Read on descendant with same names") { Params = new object[] { "NNS" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v3) { Attribute = new VariationAttribute("Read on descendant with same names") { Params = new object[] { "NS" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v4) { Attribute = new VariationAttribute("If name not found, stop at end element of the subtree") { Priority = 1 } });
                    this.AddChild(new TestVariation(v5) { Attribute = new VariationAttribute("Positioning on a level and try to find the name which is on a level higher") { Priority = 1 } });
                    this.AddChild(new TestVariation(v6) { Attribute = new VariationAttribute("Read to Descendant on one level and again to level below it") { Priority = 1 } });
                    this.AddChild(new TestVariation(v7) { Attribute = new VariationAttribute("Read to Descendant on one level and again to level below it, with namespace") { Priority = 1 } });
                    this.AddChild(new TestVariation(v8) { Attribute = new VariationAttribute("Read to Descendant on one level and again to level below it, with prefix") { Priority = 1 } });
                    this.AddChild(new TestVariation(v9) { Attribute = new VariationAttribute("Multiple Reads to children and then next siblings, NNS") { Priority = 2 } });
                    this.AddChild(new TestVariation(v10) { Attribute = new VariationAttribute("Multiple Reads to children and then next siblings, DNS") { Priority = 2 } });
                    this.AddChild(new TestVariation(v11) { Attribute = new VariationAttribute("Multiple Reads to children and then next siblings, NS") { Priority = 2 } });
                    this.AddChild(new TestVariation(v12) { Attribute = new VariationAttribute("Call from different nodetypes") { Priority = 1 } });
                    this.AddChild(new TestVariation(v14) { Attribute = new VariationAttribute("Only child has namespaces and read to it") { Priority = 2 } });
                    this.AddChild(new TestVariation(v15) { Attribute = new VariationAttribute("Pass null to both arguments throws ArgumentException") { Priority = 2 } });
                    this.AddChild(new TestVariation(v17) { Attribute = new VariationAttribute("Different names, same uri works correctly") { Priority = 2 } });
                    this.AddChild(new TestVariation(v18) { Attribute = new VariationAttribute("On Root Node") { Params = new object[] { "DNS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v18) { Attribute = new VariationAttribute("On Root Node") { Params = new object[] { "NNS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v18) { Attribute = new VariationAttribute("On Root Node") { Params = new object[] { "NS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v19) { Attribute = new VariationAttribute("427176	Assertion failed when call XmlReader.ReadToDescendant() for non-existing node") { Priority = 1 } });
                }
            }
            public partial class TCReadToFollowing : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadToFollowing
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(v1) { Attribute = new VariationAttribute("Simple positive test") { Params = new object[] { "DNS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v1) { Attribute = new VariationAttribute("Simple positive test") { Params = new object[] { "NS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v1) { Attribute = new VariationAttribute("Simple positive test") { Params = new object[] { "NNS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("Read on following with same names") { Params = new object[] { "NNS" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("Read on following with same names") { Params = new object[] { "NS" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("Read on following with same names") { Params = new object[] { "DNS" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v4) { Attribute = new VariationAttribute("If name not found, go to eof") { Priority = 1 } });
                    this.AddChild(new TestVariation(v5_1) { Attribute = new VariationAttribute("If localname not found go to eof") { Priority = 1 } });
                    this.AddChild(new TestVariation(v5_2) { Attribute = new VariationAttribute("If uri not found, go to eof") { Priority = 1 } });
                    this.AddChild(new TestVariation(v6) { Attribute = new VariationAttribute("Read to Following on one level and again to level below it") { Priority = 1 } });
                    this.AddChild(new TestVariation(v7) { Attribute = new VariationAttribute("Read to Following on one level and again to level below it, with namespace") { Priority = 1 } });
                    this.AddChild(new TestVariation(v8) { Attribute = new VariationAttribute("Read to Following on one level and again to level below it, with prefix") { Priority = 1 } });
                    this.AddChild(new TestVariation(v9) { Attribute = new VariationAttribute("Multiple Reads to children and then next siblings, NNS") { Priority = 2 } });
                    this.AddChild(new TestVariation(v10) { Attribute = new VariationAttribute("Multiple Reads to children and then next siblings, DNS") { Priority = 2 } });
                    this.AddChild(new TestVariation(v11) { Attribute = new VariationAttribute("Multiple Reads to children and then next siblings, NS") { Priority = 2 } });
                    this.AddChild(new TestVariation(v14) { Attribute = new VariationAttribute("Only child has namespaces and read to it") { Priority = 2 } });
                    this.AddChild(new TestVariation(v15) { Attribute = new VariationAttribute("Pass null to both arguments throws ArgumentException") { Priority = 2 } });
                    this.AddChild(new TestVariation(v17) { Attribute = new VariationAttribute("Different names, same uri works correctly") { Priority = 2 } });
                }
            }
            public partial class TCReadToNextSibling : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadToNextSibling
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(v) { Attribute = new VariationAttribute("Simple positive test 2") { Params = new object[] { "DNS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v) { Attribute = new VariationAttribute("Simple positive test 3") { Params = new object[] { "NS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v) { Attribute = new VariationAttribute("Simple positive test 1") { Params = new object[] { "NNS" }, Priority = 0 } });
                    this.AddChild(new TestVariation(v2) { Attribute = new VariationAttribute("Read on a deep tree at least more than 4K boundary") { Priority = 2 } });
                    this.AddChild(new TestVariation(v3) { Attribute = new VariationAttribute("Read to next sibling with same names 1") { Params = new object[] { "NNS", "<root><a att='1'/><a att='2'/><a att='3'/></root>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v3) { Attribute = new VariationAttribute("Read on next sibling with same names 2") { Params = new object[] { "DNS", "<root xmlns='a'><a att='1'/><a att='2'/><a att='3'/></root>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v3) { Attribute = new VariationAttribute("Read on next sibling with same names 3") { Params = new object[] { "NS", "<root xmlns:a='a'><a:a att='1'/><a:a att='2'/><a:a att='3'/></root>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(v4) { Attribute = new VariationAttribute("If name not found, stop at end element of the subtree") { Priority = 1 } });
                    this.AddChild(new TestVariation(v5) { Attribute = new VariationAttribute("Positioning on a level and try to find the name which is on a level higher") { Priority = 1 } });
                    this.AddChild(new TestVariation(v6) { Attribute = new VariationAttribute("Read to next sibling on one level and again to level below it") { Priority = 1 } });
                    this.AddChild(new TestVariation(v12) { Attribute = new VariationAttribute("Call from different nodetypes") { Priority = 1 } });
                    this.AddChild(new TestVariation(v16) { Attribute = new VariationAttribute("Pass null to both arguments throws ArgumentException") { Priority = 2 } });
                    this.AddChild(new TestVariation(v17) { Attribute = new VariationAttribute("Different names, same uri works correctly") { Priority = 2 } });
                }
            }
            public partial class TCReadValue : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+TCReadValue
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(TestReadValuePri0) { Attribute = new VariationAttribute("ReadValue") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadValuePri0onElement) { Attribute = new VariationAttribute("ReadValue on Element") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadValueOnAttribute0) { Attribute = new VariationAttribute("ReadValue on Attribute") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadValueOnAttribute1) { Attribute = new VariationAttribute("ReadValue on Attribute after ReadAttributeValue") { Priority = 2 } });
                    this.AddChild(new TestVariation(TestReadValue2Pri0) { Attribute = new VariationAttribute("ReadValue on empty buffer") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadValue3Pri0) { Attribute = new VariationAttribute("ReadValue on negative count") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadValue4Pri0) { Attribute = new VariationAttribute("ReadValue on negative offset") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadValue1) { Attribute = new VariationAttribute("ReadValue with buffer = element content / 2") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadValue2) { Attribute = new VariationAttribute("ReadValue entire value in one call") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadValue3) { Attribute = new VariationAttribute("ReadValue bit by bit") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadValue4) { Attribute = new VariationAttribute("ReadValue for value more than 4K") { Priority = 0 } });
                    this.AddChild(new TestVariation(TestReadValue5) { Attribute = new VariationAttribute("ReadValue for value more than 4K and invalid element") { Priority = 1 } });
                    this.AddChild(new TestVariation(TestReadValue6) { Attribute = new VariationAttribute("ReadValue with Entity Reference, EntityHandling = ExpandEntities") });
                    this.AddChild(new TestVariation(TestReadValue7) { Attribute = new VariationAttribute("ReadValue with count > buffer size") });
                    this.AddChild(new TestVariation(TestReadValue8) { Attribute = new VariationAttribute("ReadValue with index > buffer size") });
                    this.AddChild(new TestVariation(TestReadValue10) { Attribute = new VariationAttribute("ReadValue with index + count exceeds buffer") });
                    this.AddChild(new TestVariation(TestReadChar11) { Attribute = new VariationAttribute("ReadValue with combination Text, CDATA and Whitespace") });
                    this.AddChild(new TestVariation(TestReadChar12) { Attribute = new VariationAttribute("ReadValue with combination Text, CDATA and SignificantWhitespace") });
                    this.AddChild(new TestVariation(TestReadChar13) { Attribute = new VariationAttribute("ReadValue with buffer == null") });
                    this.AddChild(new TestVariation(TestReadChar14) { Attribute = new VariationAttribute("ReadValue with multiple different inner nodes") });
                    this.AddChild(new TestVariation(TestReadChar15) { Attribute = new VariationAttribute("ReadValue after failed ReadValue") });
                    this.AddChild(new TestVariation(TestReadChar16) { Attribute = new VariationAttribute("Read after partial ReadValue") });
                    this.AddChild(new TestVariation(TestReadChar19) { Attribute = new VariationAttribute("Test error after successful ReadValue") });
                    this.AddChild(new TestVariation(TestReadChar21) { Attribute = new VariationAttribute("Call on invalid element content after 4k boundary") { Priority = 1 } });
                    this.AddChild(new TestVariation(TestTextReadValue25) { Attribute = new VariationAttribute("ReadValue with whitespace") });
                    this.AddChild(new TestVariation(TestTextReadValue26) { Attribute = new VariationAttribute("ReadValue when end tag doesn't exist") });
                    this.AddChild(new TestVariation(TestCharEntities0) { Attribute = new VariationAttribute("Testing with character entities") });
                    this.AddChild(new TestVariation(TestCharEntities1) { Attribute = new VariationAttribute("Testing with character entities when value more than 4k") });
                    this.AddChild(new TestVariation(TestCharEntities2) { Attribute = new VariationAttribute("Testing with character entities with another pattern") });
                    this.AddChild(new TestVariation(TestReadValueOnBig) { Attribute = new VariationAttribute("Testing a use case pattern with large file") });
                    this.AddChild(new TestVariation(TestReadValueOnComments0) { Attribute = new VariationAttribute("ReadValue on Comments with IgnoreComments") });
                    this.AddChild(new TestVariation(TestReadValueOnPIs0) { Attribute = new VariationAttribute("ReadValue on PI with IgnorePI") });
                    this.AddChild(new TestVariation(bug340158) { Attribute = new VariationAttribute("Skip after ReadAttributeValue/ReadValueChunk") });
                }
            }
            public partial class XNodeReaderAPI : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeReaderTests+XNodeReaderAPI
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(OpenOnNodeType) { Attribute = new VariationAttribute("Open on node type: Text") { Params = new object[] { XmlNodeType.Text, 1, new string[] { "some_text" }, 1 }, Priority = 1 } });
                    this.AddChild(new TestVariation(OpenOnNodeType) { Attribute = new VariationAttribute("Open on node type: PI (root level)") { Params = new object[] { XmlNodeType.ProcessingInstruction, 0, new string[] { "PI", "" }, 1 }, Priority = 2 } });
                    this.AddChild(new TestVariation(OpenOnNodeType) { Attribute = new VariationAttribute("Open on node type: Comment") { Params = new object[] { XmlNodeType.Comment, 1, new string[] { "comm2" }, 1 }, Priority = 2 } });
                    this.AddChild(new TestVariation(OpenOnNodeType) { Attribute = new VariationAttribute("Open on node type: Text (root level)") { Params = new object[] { XmlNodeType.Text, 0, new string[] { "\t" }, 1 }, Priority = 0 } });
                    this.AddChild(new TestVariation(OpenOnNodeType) { Attribute = new VariationAttribute("Open on node type: XElement (root)") { Params = new object[] { XmlNodeType.Element, 0, new string[] { "A", "", "" }, 15 }, Priority = 1 } });
                    this.AddChild(new TestVariation(OpenOnNodeType) { Attribute = new VariationAttribute("Open on node type: PI") { Params = new object[] { XmlNodeType.ProcessingInstruction, 1, new string[] { "PIX", "click" }, 1 }, Priority = 2 } });
                    this.AddChild(new TestVariation(OpenOnNodeType) { Attribute = new VariationAttribute("Open on node type: Comment (root level)") { Params = new object[] { XmlNodeType.Comment, 0, new string[] { "comment1" }, 1 }, Priority = 2 } });
                    this.AddChild(new TestVariation(OpenOnNodeType) { Attribute = new VariationAttribute("Open on node type: XElement (in the mIddle)") { Params = new object[] { XmlNodeType.Element, 1, new string[] { "B", "", "x" }, 11 }, Priority = 0 } });
                    this.AddChild(new TestVariation(OpenOnNodeType) { Attribute = new VariationAttribute("Open on node type: XElement (leaf I.)") { Params = new object[] { XmlNodeType.Element, 3, new string[] { "D", "", "y" }, 2 }, Priority = 0 } });
                    this.AddChild(new TestVariation(OpenOnNodeType) { Attribute = new VariationAttribute("Open on node type: XElement (leaf II.)") { Params = new object[] { XmlNodeType.Element, 4, new string[] { "E", "p", "nsp" }, 1 }, Priority = 1 } });
                    this.AddChild(new TestVariation(Namespaces) { Attribute = new VariationAttribute("Namespaces - Comment") { Params = new object[] { XmlNodeType.Comment, 1, new string[] { "", "x" }, new string[] { "p", "nsp" } } } });
                    this.AddChild(new TestVariation(Namespaces) { Attribute = new VariationAttribute("Namespaces - root element") { Params = new object[] { XmlNodeType.Element, 0, new string[] { "", "" } } } });
                    this.AddChild(new TestVariation(Namespaces) { Attribute = new VariationAttribute("Namespaces - element") { Params = new object[] { XmlNodeType.Element, 1, new string[] { "", "x" }, new string[] { "p", "nsp" } } } });
                    this.AddChild(new TestVariation(Namespaces) { Attribute = new VariationAttribute("Namespaces - element, def. ns redef") { Params = new object[] { XmlNodeType.Element, 3, new string[] { "", "y" }, new string[] { "p", "nsp" } } } });
                    this.AddChild(new TestVariation(ReadSubtreeSanity) { Attribute = new VariationAttribute("ReadSubtree (sanity)") { Priority = 0 } });
                    this.AddChild(new TestVariation(AdjacentTextNodes1) { Attribute = new VariationAttribute("Adjacent text nodes (sanity I.)") { Priority = 0 } });
                    this.AddChild(new TestVariation(AdjacentTextNodes2) { Attribute = new VariationAttribute("Adjacent text nodes (sanity II.) : ReadElementContent") { Priority = 0 } });
                    this.AddChild(new TestVariation(AdjacentTextNodesI) { Attribute = new VariationAttribute("Adjacent text nodes (sanity IV.) : ReadInnerXml") { Priority = 0 } });
                    this.AddChild(new TestVariation(AdjacentTextNodes3) { Attribute = new VariationAttribute("Adjacent text nodes (sanity III.) : ReadContent") { Priority = 0 } });
                }
            }
        }
        #endregion
    }
}
