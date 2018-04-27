// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Text;
using Microsoft.Test.ModuleCore;
using System.Xml.Linq;
using System.IO;
using XmlCoreTest.Common;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeBuilderFunctionalTests : TestModule
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests
        // Test Module
        [Fact]
        [OuterLoop]
        public static void RunTests()
        {
            TestInput.CommandLine = "";
            XNodeBuilderFunctionalTests module = new XNodeBuilderFunctionalTests();
            module.Init();

            {
                module.AddChild(new XNodeBuilderTests() { Attribute = new TestCaseAttribute() { Name = "XNodeBuilder", Desc = "XLinq XNodeBuilder Tests" } });
            }
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }

        #region Code
        public partial class XNodeBuilderTests : XLinqTestCase
        {
            // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests
            // Test Case
            public override void AddChildren()
            {
                this.AddChild(new TCAutoComplete() { Attribute = new TestCaseAttribute() { Name = "Auto-completion of tokens", Param = "XNodeBuilder" } });
                this.AddChild(new TCDocument() { Attribute = new TestCaseAttribute() { Name = "WriteStart/EndDocument", Param = "XNodeBuilder" } });
                this.AddChild(new TCDocType() { Attribute = new TestCaseAttribute() { Name = "WriteDocType", Param = "XNodeBuilder" } });
                this.AddChild(new TCElement() { Attribute = new TestCaseAttribute() { Name = "WriteStart/EndElement", Param = "XNodeBuilder" } });
                this.AddChild(new TCAttribute() { Attribute = new TestCaseAttribute() { Name = "WriteStart/EndAttribute", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteAttributes() { Attribute = new TestCaseAttribute() { Name = "WriteAttributes", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteNode_XmlReader() { Attribute = new TestCaseAttribute() { Name = "WriteNode", Param = "XNodeBuilder" } });
                this.AddChild(new TCFullEndElement() { Attribute = new TestCaseAttribute() { Name = "WriteFullEndElement", Param = "XNodeBuilder" } });
                this.AddChild(new TCElemNamespace() { Attribute = new TestCaseAttribute() { Name = "Element Namespace", Param = "XNodeBuilder" } });
                this.AddChild(new TCAttrNamespace() { Attribute = new TestCaseAttribute() { Name = "Attribute Namespace", Param = "XNodeBuilder" } });
                this.AddChild(new TCCData() { Attribute = new TestCaseAttribute() { Name = "WriteCData", Param = "XNodeBuilder" } });
                this.AddChild(new TCComment() { Attribute = new TestCaseAttribute() { Name = "WriteComment", Param = "XNodeBuilder" } });
                this.AddChild(new TCEntityRef() { Attribute = new TestCaseAttribute() { Name = "WriteEntityRef", Param = "XNodeBuilder" } });
                this.AddChild(new TCCharEntity() { Attribute = new TestCaseAttribute() { Name = "WriteCharEntity", Param = "XNodeBuilder" } });
                this.AddChild(new TCSurrogateCharEntity() { Attribute = new TestCaseAttribute() { Name = "WriteSurrogateCharEntity", Param = "XNodeBuilder" } });
                this.AddChild(new TCPI() { Attribute = new TestCaseAttribute() { Name = "WriteProcessingInstruction", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteNmToken() { Attribute = new TestCaseAttribute() { Name = "WriteNmToken", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteName() { Attribute = new TestCaseAttribute() { Name = "WriteName", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteQName() { Attribute = new TestCaseAttribute() { Name = "WriteQualifiedName", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteChars() { Attribute = new TestCaseAttribute() { Name = "WriteChars", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteString() { Attribute = new TestCaseAttribute() { Name = "WriteString", Param = "XNodeBuilder" } });
                this.AddChild(new TCWhiteSpace() { Attribute = new TestCaseAttribute() { Name = "WriteWhitespace", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteValue() { Attribute = new TestCaseAttribute() { Name = "WriteValue", Param = "XNodeBuilder" } });
                this.AddChild(new TCLookUpPrefix() { Attribute = new TestCaseAttribute() { Name = "LookupPrefix", Param = "XNodeBuilder" } });
                this.AddChild(new TCXmlSpaceWriter() { Attribute = new TestCaseAttribute() { Name = "XmlSpace", Param = "XNodeBuilder" } });
                this.AddChild(new TCXmlLangWriter() { Attribute = new TestCaseAttribute() { Name = "XmlLang", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteRaw() { Attribute = new TestCaseAttribute() { Name = "WriteRaw", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteBase64() { Attribute = new TestCaseAttribute() { Name = "WriteBase64", Param = "XNodeBuilder" } });
                this.AddChild(new TCWriteState() { Attribute = new TestCaseAttribute() { Name = "WriteState", Param = "XNodeBuilder" } });
                this.AddChild(new TC_NDP20_NewMethods() { Attribute = new TestCaseAttribute() { Name = "NDP20_NewMethods", Param = "XNodeBuilder" } });
                this.AddChild(new TCGlobalization() { Attribute = new TestCaseAttribute() { Name = "Globalization", Param = "XNodeBuilder" } });
                this.AddChild(new TCClose() { Attribute = new TestCaseAttribute() { Name = "Close", Param = "XNodeBuilder" } });
                this.AddChild(new TCEOFHandling() { Attribute = new TestCaseAttribute() { Name = "NewLineHandling", Param = "XNodeBuilder" } });
                this.AddChild(new XObjectBuilderTest() { Attribute = new TestCaseAttribute() { Name = "Error Conditions", Param = "XNodeBuilder" } });
                this.AddChild(new NamespacehandlingWriterSanity() { Attribute = new TestCaseAttribute() { Name = "NamespaceHandling::OmitDuplicate (writer sanity)" } });
                this.AddChild(new OmitAnotation() { Attribute = new TestCaseAttribute() { Name = "OmitDuplicates with annotations", Desc = "OmitDuplicates with annotations" } });
                this.AddChild(new NamespacehandlingSaveOptions() { Attribute = new TestCaseAttribute() { Name = "NamespaceHandling::OmitDuplicate Save() (SaveOptions) - XElement", Params = new object[] { "Save", typeof(XElement) } } });
                this.AddChild(new NamespacehandlingSaveOptions() { Attribute = new TestCaseAttribute() { Name = "NamespaceHandling::OmitDuplicate ToString() (SaveOptions) - XDocument", Params = new object[] { "ToString", typeof(XDocument) } } });
                this.AddChild(new NamespacehandlingSaveOptions() { Attribute = new TestCaseAttribute() { Name = "NamespaceHandling::OmitDuplicate Save() (SaveOptions) - XDocument", Params = new object[] { "Save", typeof(XDocument) } } });
                this.AddChild(new NamespacehandlingSaveOptions() { Attribute = new TestCaseAttribute() { Name = "NamespaceHandling::OmitDuplicate ToString() (SaveOptions) - XElement", Params = new object[] { "ToString", typeof(XElement) } } });
                this.AddChild(new Writer_Settings() { Attribute = new TestCaseAttribute() { Name = "Writer settings" } });
                this.AddChild(new TCCheckChars() { Attribute = new TestCaseAttribute() { Name = "CheckCharacters" } });
                this.AddChild(new TCNewLineHandling() { Attribute = new TestCaseAttribute() { Name = "NewLineHandling2" } });
                this.AddChild(new TCIndent() { Attribute = new TestCaseAttribute() { Name = "Indent" } });
                this.AddChild(new TCNewLineOnAttributes() { Attribute = new TestCaseAttribute() { Name = "NewLineOnAttributes" } });
                this.AddChild(new TCStandAlone() { Attribute = new TestCaseAttribute() { Name = "Standalone" } });
                this.AddChild(new TCFragmentCL() { Attribute = new TestCaseAttribute() { Name = "CL = Fragment Tests" } });
                this.AddChild(new TCAutoCL() { Attribute = new TestCaseAttribute() { Name = "CL = Auto Tests" } });
            }
            public partial class TCAutoComplete : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCAutoComplete
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(var_1) { Attribute = new VariationAttribute("Missing EndAttr, followed by element") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(var_2) { Attribute = new VariationAttribute("Missing EndAttr, followed by comment") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(var_3) { Attribute = new VariationAttribute("Write EndDocument with unclosed element tag") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(var_4) { Attribute = new VariationAttribute("WriteStartDocument - WriteEndDocument") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(var_5) { Attribute = new VariationAttribute("WriteEndElement without WriteStartElement") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(var_6) { Attribute = new VariationAttribute("WriteFullEndElement without WriteStartElement") { Id = 6, Priority = 1 } });
                }
            }
            public partial class TCDocument : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCDocument
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(document_1) { Attribute = new VariationAttribute("StartDocument-EndDocument Sanity Test") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(document_2) { Attribute = new VariationAttribute("Multiple StartDocument should error") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(document_3) { Attribute = new VariationAttribute("Missing StartDocument should be fixed") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(document_4) { Attribute = new VariationAttribute("Multiple EndDocument should error") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(document_5) { Attribute = new VariationAttribute("Missing EndDocument should be fixed") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(document_6) { Attribute = new VariationAttribute("Call Start-EndDocument multiple times, should error") { Id = 6, Priority = 2 } });
                    this.AddChild(new TestVariation(document_7) { Attribute = new VariationAttribute("Multiple root elements should error") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(document_8) { Attribute = new VariationAttribute("Start-EndDocument without any element should error") { Id = 8, Priority = 2 } });
                    this.AddChild(new TestVariation(document_9) { Attribute = new VariationAttribute("Top level text should error - PROLOG") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(document_10) { Attribute = new VariationAttribute("Top level text should error - EPILOG") { Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(document_11) { Attribute = new VariationAttribute("Top level atomic value should error - PROLOG") { Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(document_12) { Attribute = new VariationAttribute("Top level atomic value should error - EPILOG") { Id = 12, Priority = 1 } });
                }
            }
            public partial class TCDocType : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCDocType
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(docType_4) { Attribute = new VariationAttribute("WriteDocType with name value = null") { Param = "null", Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(docType_4) { Attribute = new VariationAttribute("WriteDocType with name value = String.Empty") { Param = "String.Empty", Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(docType_6) { Attribute = new VariationAttribute("Call WriteDocType in the root element") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(docType_7) { Attribute = new VariationAttribute("Call WriteDocType following root element") { Id = 8, Priority = 1 } });
                }
            }
            public partial class TCElement : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCElement
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(element_1) { Attribute = new VariationAttribute("StartElement-EndElement Sanity Test") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(element_2) { Attribute = new VariationAttribute("Sanity test for overload WriteStartElement(string prefix, string name, string ns)") { Id = 2, Priority = 0 } });
                    this.AddChild(new TestVariation(element_3) { Attribute = new VariationAttribute("Sanity test for overload WriteStartElement(string name, string ns)") { Id = 3, Priority = 0 } });
                    this.AddChild(new TestVariation(element_4) { Attribute = new VariationAttribute("Element name = String.Empty should error") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(element_5) { Attribute = new VariationAttribute("Element name = null should error") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(element_6) { Attribute = new VariationAttribute("Element NS = String.Empty") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(element_7) { Attribute = new VariationAttribute("Element NS = null") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(element_8) { Attribute = new VariationAttribute("Write 100 nested elements") { Id = 8, Priority = 1 } });
                }
            }
            public partial class TCAttribute : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCAttribute
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(attribute_1) { Attribute = new VariationAttribute("Sanity test for WriteAttribute") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(attribute_2) { Attribute = new VariationAttribute("Missing EndAttribute should be fixed") { Id = 2, Priority = 0 } });
                    this.AddChild(new TestVariation(attribute_3) { Attribute = new VariationAttribute("WriteStartAttribute followed by WriteStartAttribute") { Id = 3, Priority = 0 } });
                    this.AddChild(new TestVariation(attribute_4) { Attribute = new VariationAttribute("Multiple WritetAttributeString") { Id = 4, Priority = 0 } });
                    this.AddChild(new TestVariation(attribute_5) { Attribute = new VariationAttribute("WriteStartAttribute followed by WriteString") { Id = 5, Priority = 0 } });
                    this.AddChild(new TestVariation(attribute_6) { Attribute = new VariationAttribute("Sanity test for overload WriteStartAttribute(name, ns)") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_7) { Attribute = new VariationAttribute("Sanity test for overload WriteStartAttribute(prefix, name, ns)") { Id = 7, Priority = 0 } });
                    this.AddChild(new TestVariation(attribute_8) { Attribute = new VariationAttribute("Duplicate attribute 'attr1'") { Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_9) { Attribute = new VariationAttribute("Duplicate attribute 'ns1:attr1'") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_10) { Attribute = new VariationAttribute("Attribute name = String.Empty should error") { Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_11) { Attribute = new VariationAttribute("Attribute name = null") { Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_12) { Attribute = new VariationAttribute("WriteAttribute with names Foo, fOo, foO, FOO") { Id = 12, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_13) { Attribute = new VariationAttribute("Invalid value of xml:space") { Id = 13, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_14) { Attribute = new VariationAttribute("SingleQuote in attribute value should be allowed") { Id = 14 } });
                    this.AddChild(new TestVariation(attribute_15) { Attribute = new VariationAttribute("DoubleQuote in attribute value should be escaped") { Id = 15 } });
                    this.AddChild(new TestVariation(attribute_16) { Attribute = new VariationAttribute("WriteAttribute with value = &, #65, #x20") { Id = 16, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_17) { Attribute = new VariationAttribute("WriteAttributeString followed by WriteString") { Id = 17, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_18) { Attribute = new VariationAttribute("WriteAttribute followed by WriteString") { Id = 18, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_19) { Attribute = new VariationAttribute("WriteAttribute with all whitespace characters") { Id = 19, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_20) { Attribute = new VariationAttribute("< > & chars should be escaped in attribute value") { Id = 20, Priority = 1 } });
                    this.AddChild(new TestVariation(attribute_21) { Attribute = new VariationAttribute("testcase: Redefine auto generated prefix n1") { Id = 21 } });
                    this.AddChild(new TestVariation(attribute_22) { Attribute = new VariationAttribute("testcase: Reuse and redefine existing prefix") { Id = 22 } });
                    this.AddChild(new TestVariation(attribute_23) { Attribute = new VariationAttribute("WriteStartAttribute(attr) sanity test") { Id = 23 } });
                    this.AddChild(new TestVariation(attribute_24) { Attribute = new VariationAttribute("WriteStartAttribute(attr) inside an element with changed default namespace") { Id = 24 } });
                    this.AddChild(new TestVariation(attribute_25) { Attribute = new VariationAttribute("WriteStartAttribute(attr) and duplicate attrs") { Id = 25 } });
                    this.AddChild(new TestVariation(attribute_26) { Attribute = new VariationAttribute("WriteStartAttribute(attr) when element has ns:attr") { Id = 26 } });
                    this.AddChild(new TestVariation(attribute_27) { Attribute = new VariationAttribute("XmlCharCheckingWriter should not normalize newLines in attribute values when NewLinesHandling = Replace") { Id = 27 } });
                    this.AddChild(new TestVariation(attribute_29) { Attribute = new VariationAttribute("WriteAttributeString doesn't fail on invalid surrogate pair sequences") { Id = 29 } });
                }
            }
            public partial class TCWriteAttributes : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteAttributes
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(writeAttributes_1) { Attribute = new VariationAttribute("Call WriteAttributes with default DTD attributes = true") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_2) { Attribute = new VariationAttribute("Call WriteAttributes with default DTD attributes = false") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_3) { Attribute = new VariationAttribute("Call WriteAttributes with XmlReader = null") { Id = 3 } });
                    this.AddChild(new TestVariation(writeAttributes_4) { Attribute = new VariationAttribute("Call WriteAttributes when reader is located on element") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_5) { Attribute = new VariationAttribute("Call WriteAttributes when reader is located in the mIddle attribute") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_6) { Attribute = new VariationAttribute("Call WriteAttributes when reader is located in the last attribute") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_8) { Attribute = new VariationAttribute("Call WriteAttributes with reader on XmlDeclaration") { Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_9) { Attribute = new VariationAttribute("Call WriteAttributes with reader on Text") { Param = "Text", Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_9) { Attribute = new VariationAttribute("Call WriteAttributes with reader on PI") { Param = "ProcessingInstruction", Id = 12, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_9) { Attribute = new VariationAttribute("Call WriteAttributes with reader on Comment") { Param = "Comment", Id = 13, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_9) { Attribute = new VariationAttribute("Call WriteAttributes with reader on CDATA") { Param = "CDATA", Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_12) { Attribute = new VariationAttribute("Call WriteAttributes with 100 attributes") { Id = 19, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_13) { Attribute = new VariationAttribute("WriteAttributes with different builtin entities in attribute value") { Id = 20, Priority = 1 } });
                    this.AddChild(new TestVariation(writeAttributes_14) { Attribute = new VariationAttribute("WriteAttributes tries to duplicate attribute") { Id = 21, Priority = 1 } });
                }
            }
            public partial class TCWriteNode_XmlReader : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteNode_XmlReader
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(writeNode_XmlReader1) { Attribute = new VariationAttribute("WriteNode with null reader") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader2) { Attribute = new VariationAttribute("WriteNode with reader positioned on attribute, no operation") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader3) { Attribute = new VariationAttribute("WriteNode before reader.Read()") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader4) { Attribute = new VariationAttribute("WriteNode after first reader.Read()") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader5) { Attribute = new VariationAttribute("WriteNode when reader is positioned on mIddle of an element node") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader6) { Attribute = new VariationAttribute("WriteNode when reader state is EOF") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader7) { Attribute = new VariationAttribute("WriteNode when reader state is Closed") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader8) { Attribute = new VariationAttribute("WriteNode with reader on empty element node") { Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader9) { Attribute = new VariationAttribute("WriteNode with reader on 100 Nodes") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader10) { Attribute = new VariationAttribute("WriteNode with reader on node with mixed content") { Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader11) { Attribute = new VariationAttribute("WriteNode with reader on node with declared namespace in parent") { Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader14) { Attribute = new VariationAttribute("WriteNode with element that has different prefix") { Id = 14, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader15) { Attribute = new VariationAttribute("Call WriteNode with default attributes = true and DTD") { Id = 15, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader16) { Attribute = new VariationAttribute("Call WriteNode with default attributes = false and DTD") { Id = 16, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader17) { Attribute = new VariationAttribute("WriteNode with reader on empty element with attributes") { Id = 17, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader18) { Attribute = new VariationAttribute("WriteNode with document containing just empty element with attributes") { Id = 18, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader19) { Attribute = new VariationAttribute("Call WriteNode with special entity references as attribute value") { Id = 19, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader21) { Attribute = new VariationAttribute("Call WriteNode with full end element") { Id = 21, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader22) { Attribute = new VariationAttribute("Call WriteNode with reader on element with 100 attributes") { Id = 22, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader23) { Attribute = new VariationAttribute("Call WriteNode with reader on text node") { Id = 23, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader24) { Attribute = new VariationAttribute("Call WriteNode with reader on CDATA node") { Id = 24, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader25) { Attribute = new VariationAttribute("Call WriteNode with reader on PI node") { Id = 25, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader26) { Attribute = new VariationAttribute("Call WriteNode with reader on Comment node") { Id = 26, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader27) { Attribute = new VariationAttribute("WriteNode should only write required namespaces") { Id = 27, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader28) { Attribute = new VariationAttribute("WriteNode should only write required namespaces, include xmlns:xml") { Id = 28, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader29) { Attribute = new VariationAttribute("WriteNode should only write required namespaces, exclude xmlns:xml") { Id = 29, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader30) { Attribute = new VariationAttribute("WriteNode should only write required namespaces, change default ns at top level") { Id = 30, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader31) { Attribute = new VariationAttribute("WriteNode should only write required namespaces, change default ns at same level") { Id = 31, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader32) { Attribute = new VariationAttribute("WriteNode should only write required namespaces, change default ns at both levels") { Id = 32, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader33) { Attribute = new VariationAttribute("WriteNode should only write required namespaces, change ns uri for same prefix") { Id = 33, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNode_XmlReader34) { Attribute = new VariationAttribute("WriteNode should only write required namespaces, reuse prefix from top level") { Id = 34, Priority = 1 } });
                }
            }

            public partial class TCFullEndElement : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCFullEndElement
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(fullEndElement_1) { Attribute = new VariationAttribute("Sanity test for WriteFullEndElement()") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(fullEndElement_2) { Attribute = new VariationAttribute("Call WriteFullEndElement before calling WriteStartElement") { Id = 2, Priority = 2 } });
                    this.AddChild(new TestVariation(fullEndElement_3) { Attribute = new VariationAttribute("Call WriteFullEndElement after WriteEndElement") { Id = 3, Priority = 2 } });
                    this.AddChild(new TestVariation(fullEndElement_4) { Attribute = new VariationAttribute("Call WriteFullEndElement without closing attributes") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(fullEndElement_5) { Attribute = new VariationAttribute("Call WriteFullEndElement after WriteStartAttribute") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(fullEndElement_6) { Attribute = new VariationAttribute("WriteFullEndElement for 100 nested elements") { Id = 6, Priority = 1 } });
                }
            }
            public partial class TCElemNamespace : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCElemNamespace
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(elemNamespace_1) { Attribute = new VariationAttribute("Multiple NS decl for same prefix on an element") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_2) { Attribute = new VariationAttribute("Multiple NS decl for same prefix (same NS value) on an element") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_3) { Attribute = new VariationAttribute("Element and attribute have same prefix, but different namespace value") { Id = 3, Priority = 2 } });
                    this.AddChild(new TestVariation(elemNamespace_4) { Attribute = new VariationAttribute("Nested elements have same prefix, but different namespace") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_5) { Attribute = new VariationAttribute("Mapping reserved prefix xml to invalid namespace") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_6) { Attribute = new VariationAttribute("Mapping reserved prefix xml to correct namespace") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_7) { Attribute = new VariationAttribute("Write element with prefix beginning with xml") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_8) { Attribute = new VariationAttribute("Reuse prefix that refers the same as default namespace") { Id = 8, Priority = 2 } });
                    this.AddChild(new TestVariation(elemNamespace_9) { Attribute = new VariationAttribute("Should throw error for prefix=xmlns") { Id = 9, Priority = 2 } });
                    this.AddChild(new TestVariation(elemNamespace_10) { Attribute = new VariationAttribute("Create nested element without prefix but with namespace of parent element with a defined prefix") { Id = 10, Priority = 2 } });
                    this.AddChild(new TestVariation(elemNamespace_11) { Attribute = new VariationAttribute("Create different prefix for element and attribute that have same namespace") { Id = 11, Priority = 2 } });
                    this.AddChild(new TestVariation(elemNamespace_12) { Attribute = new VariationAttribute("Create same prefix for element and attribute that have same namespace") { Id = 12, Priority = 2 } });
                    this.AddChild(new TestVariation(elemNamespace_13) { Attribute = new VariationAttribute("Try to re-define NS prefix on attribute which is already defined on an element") { Id = 13, Priority = 2 } });
                    this.AddChild(new TestVariation(elemNamespace_14) { Attribute = new VariationAttribute("Namespace string contains surrogates, reuse at different levels") { Id = 14, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_15) { Attribute = new VariationAttribute("Namespace containing entities, use at multiple levels") { Id = 15, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_16) { Attribute = new VariationAttribute("Verify it resets default namespace when redefined earlier in the stack") { Id = 16, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_17) { Attribute = new VariationAttribute("The default namespace for an element can not be changed once it is written out") { Id = 17, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_18) { Attribute = new VariationAttribute("Map XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix") { Id = 18, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_19) { Attribute = new VariationAttribute("Pass NULL as NS to WriteStartElement") { Id = 19, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_20) { Attribute = new VariationAttribute("Write element in reserved XML namespace, should error") { Id = 20, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_21) { Attribute = new VariationAttribute("Write element in reserved XMLNS namespace, should error") { Id = 21, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_22) { Attribute = new VariationAttribute("Mapping a prefix to empty ns should error") { Id = 22, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_23) { Attribute = new VariationAttribute("Pass null prefix to WriteStartElement()") { Id = 23, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_24) { Attribute = new VariationAttribute("Pass String.Empty prefix to WriteStartElement()") { Id = 24, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_25) { Attribute = new VariationAttribute("Pass null ns to WriteStartElement()") { Id = 25, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_26) { Attribute = new VariationAttribute("Pass String.Empty ns to WriteStartElement()") { Id = 26, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_27) { Attribute = new VariationAttribute("Pass null prefix to WriteStartElement() when namespace is in scope") { Id = 27, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_28) { Attribute = new VariationAttribute("Pass String.Empty prefix to WriteStartElement() when namespace is in scope") { Id = 28, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_29) { Attribute = new VariationAttribute("Pass null ns to WriteStartElement() when prefix is in scope") { Id = 29, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_30) { Attribute = new VariationAttribute("Pass String.Empty ns to WriteStartElement() when prefix is in scope") { Id = 30, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_31) { Attribute = new VariationAttribute("Pass String.Empty ns to WriteStartElement() when prefix is in scope") { Id = 31, Priority = 1 } });
                    this.AddChild(new TestVariation(elemNamespace_32) { Attribute = new VariationAttribute("Mapping empty ns uri to a prefix should error") { Id = 31, Priority = 1 } });
                }
            }
            public partial class TCAttrNamespace : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCAttrNamespace
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(attrNamespace_1) { Attribute = new VariationAttribute("Define prefix 'xml' with invalid namespace URI 'foo'") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_2) { Attribute = new VariationAttribute("Bind NS prefix 'xml' with valid namespace URI") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_3) { Attribute = new VariationAttribute("Bind NS prefix 'xmlA' with namespace URI 'foo'") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_4) { Attribute = new VariationAttribute("Write attribute xml:space with correct namespace") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_5) { Attribute = new VariationAttribute("Write attribute xml:space with incorrect namespace") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_6) { Attribute = new VariationAttribute("Write attribute xml:lang with incorrect namespace") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_7) { Attribute = new VariationAttribute("WriteAttribute, define namespace attribute before value attribute") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_8) { Attribute = new VariationAttribute("WriteAttribute, define namespace attribute after value attribute") { Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_9) { Attribute = new VariationAttribute("WriteAttribute, redefine prefix at different scope and use both of them") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_10) { Attribute = new VariationAttribute("WriteAttribute, redefine namespace at different scope and use both of them") { Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_11) { Attribute = new VariationAttribute("WriteAttribute with collIding prefix with element") { Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_12) { Attribute = new VariationAttribute("WriteAttribute with collIding namespace with element") { Id = 12, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_13) { Attribute = new VariationAttribute("WriteAttribute with namespace but no prefix") { Id = 13, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_14) { Attribute = new VariationAttribute("WriteAttribute for 2 attributes with same prefix but different namespace") { Id = 14, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_15) { Attribute = new VariationAttribute("WriteAttribute with String.Empty and null as namespace and prefix values") { Id = 15, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_16) { Attribute = new VariationAttribute("WriteAttribute to manually create attribute of xmlns:x") { Id = 16, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_17) { Attribute = new VariationAttribute("WriteAttribute with namespace value = null while a prefix exists") { Id = 17, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_18) { Attribute = new VariationAttribute("WriteAttribute with namespace value = String.Empty while a prefix exists") { Id = 18, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_19) { Attribute = new VariationAttribute("WriteAttribe in nested elements with same namespace but different prefix") { Id = 19, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_20) { Attribute = new VariationAttribute("WriteAttribute for x:a and xmlns:a diff namespace") { Id = 20, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_21) { Attribute = new VariationAttribute("WriteAttribute for x:a and xmlns:a same namespace") { Id = 21, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_22) { Attribute = new VariationAttribute("WriteAttribute with collIding NS and prefix for 2 attributes") { Id = 22, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_23) { Attribute = new VariationAttribute("WriteAttribute with DQ in namespace") { Id = 23, Priority = 2 } });
                    this.AddChild(new TestVariation(attrNamespace_24) { Attribute = new VariationAttribute("Attach prefix with empty namespace") { Id = 24, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_25) { Attribute = new VariationAttribute("Explicitly write namespace attribute that maps XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix") { Id = 25, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_26) { Attribute = new VariationAttribute("Map XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix") { Id = 26, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_27) { Attribute = new VariationAttribute("Pass empty namespace to WriteAttributeString(prefix, name, ns, value)") { Id = 27, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_28) { Attribute = new VariationAttribute("Write attribute with prefix = xmlns") { Id = 28, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_29) { Attribute = new VariationAttribute("Write attribute in reserved XML namespace, should error") { Id = 29, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_30) { Attribute = new VariationAttribute("Write attribute in reserved XMLNS namespace, should error") { Id = 30, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_31) { Attribute = new VariationAttribute("WriteAttributeString with no namespace under element with empty prefix") { Id = 31, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_32) { Attribute = new VariationAttribute("Pass null prefix to WriteAttributeString()") { Id = 32, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_33) { Attribute = new VariationAttribute("Pass String.Empty prefix to WriteAttributeString()") { Id = 33, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_34) { Attribute = new VariationAttribute("Pass null ns to WriteAttributeString()") { Id = 34, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_35) { Attribute = new VariationAttribute("Pass String.Empty ns to WriteAttributeString()") { Id = 35, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_36) { Attribute = new VariationAttribute("Pass null prefix to WriteAttributeString() when namespace is in scope") { Id = 36, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_37) { Attribute = new VariationAttribute("Pass String.Empty prefix to WriteAttributeString() when namespace is in scope") { Id = 37, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_38) { Attribute = new VariationAttribute("Pass null ns to WriteAttributeString() when prefix is in scope") { Id = 38, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_39) { Attribute = new VariationAttribute("Pass String.Empty ns to WriteAttributeString() when prefix is in scope") { Id = 39, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_40) { Attribute = new VariationAttribute("Mapping empty ns uri to a prefix should error") { Id = 40, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_41) { Attribute = new VariationAttribute("WriteStartAttribute with prefix = null, localName = xmlns - case 1") { Id = 41, Priority = 1 } });
                    this.AddChild(new TestVariation(attrNamespace_42) { Attribute = new VariationAttribute("WriteStartAttribute with prefix = null, localName = xmlns - case 2") { Id = 42, Priority = 1 } });
                }
            }
            public partial class TCCData : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCCData
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(CData_1) { Attribute = new VariationAttribute("WriteCData with null") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(CData_2) { Attribute = new VariationAttribute("WriteCData with String.Empty") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(CData_3) { Attribute = new VariationAttribute("WriteCData Sanity test") { Id = 3, Priority = 0 } });
                    this.AddChild(new TestVariation(CData_4) { Attribute = new VariationAttribute("WriteCData with valid surrogate pair") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(CData_6) { Attribute = new VariationAttribute("WriteCData with & < > chars, they should not be escaped") { Id = 6, Priority = 2 } });
                    this.AddChild(new TestVariation(CData_7) { Attribute = new VariationAttribute("WriteCData with <![CDATA[") { Id = 7, Priority = 2 } });
                    this.AddChild(new TestVariation(CData_8) { Attribute = new VariationAttribute("CData state machine") { Id = 8, Priority = 2 } });
                    this.AddChild(new TestVariation(CData_9) { Attribute = new VariationAttribute("WriteCData with invalid surrogate pair") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(CData_10) { Attribute = new VariationAttribute("WriteCData after root element") { Id = 10 } });
                    this.AddChild(new TestVariation(CData_11) { Attribute = new VariationAttribute("Call WriteCData twice - that should write two CData blocks") { Id = 11, Priority = 1 } });
                }
            }
            public partial class TCComment : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCComment
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(comment_1) { Attribute = new VariationAttribute("Sanity test for WriteComment") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(comment_2) { Attribute = new VariationAttribute("Comment value = String.Empty") { Id = 2, Priority = 0 } });
                    this.AddChild(new TestVariation(comment_3) { Attribute = new VariationAttribute("Comment value = null") { Id = 3, Priority = 0 } });
                    this.AddChild(new TestVariation(comment_4) { Attribute = new VariationAttribute("WriteComment with valid surrogate pair") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(comment_5) { Attribute = new VariationAttribute("WriteComment with invalid surrogate pair") { Id = 5, Priority = 1 } });
                }
            }
            public partial class TCEntityRef : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCEntityRef
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with invalid value SQ") { Param = "test'test", Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with invalid value <;") { Param = "test<test", Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with invalid value & and ;") { Param = "&test;", Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with value = null") { Param = "null", Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with invalid value DQ") { Param = "test\"test", Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with #xD") { Param = "\xD", Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with invalid value &") { Param = "test&test", Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with value = String.Empty") { Param = "String.Empty", Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with #xD#xA") { Param = "\xD\xA", Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with #xA") { Param = "\r", Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_1) { Attribute = new VariationAttribute("WriteEntityRef with invalid value >") { Param = "test>test", Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_2) { Attribute = new VariationAttribute("XmlWriter: Entity Refs: amp") { Id = 12, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_3) { Attribute = new VariationAttribute("XmlWriter: Entity Refs: apos") { Id = 13, Priority = 1 } });
                    this.AddChild(new TestVariation(var_4) { Attribute = new VariationAttribute("XmlWriter: Entity Refs: lt") { Id = 14, Priority = 1 } });
                    this.AddChild(new TestVariation(var_5) { Attribute = new VariationAttribute("XmlWriter: Entity Refs: quot") { Id = 15, Priority = 1 } });
                    this.AddChild(new TestVariation(entityRef_6) { Attribute = new VariationAttribute("XmlWriter: Entity Refs: gt") { Id = 16, Priority = 1 } });
                }
            }
            public partial class TCCharEntity : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCCharEntity
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(charEntity_1) { Attribute = new VariationAttribute("WriteCharEntity with valid Unicode character") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(charEntity_2) { Attribute = new VariationAttribute("Call WriteCharEntity after WriteStartElement/WriteEndElement") { Id = 2, Priority = 0 } });
                    this.AddChild(new TestVariation(charEntity_3) { Attribute = new VariationAttribute("Call WriteCharEntity after WriteStartAttribute/WriteEndAttribute") { Id = 3, Priority = 0 } });
                    this.AddChild(new TestVariation(charEntity_4) { Attribute = new VariationAttribute("Character from low surrogate range") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(charEntity_5) { Attribute = new VariationAttribute("Character from high surrogate range") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(charEntity_7) { Attribute = new VariationAttribute("Sanity test, pass 'a'") { Id = 7, Priority = 0 } });
                    this.AddChild(new TestVariation(charEntity_8) { Attribute = new VariationAttribute("WriteCharEntity for special attributes") { Id = 8, Priority = 1 } });
                }
            }
            public partial class TCSurrogateCharEntity : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCSurrogateCharEntity
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(surrogateEntity_1) { Attribute = new VariationAttribute("SurrogateCharEntity after WriteStartElement/WriteEndElement") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(surrogateEntity_2) { Attribute = new VariationAttribute("SurrogateCharEntity after WriteStartAttribute/WriteEndAttribute") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(surrogateEntity_3) { Attribute = new VariationAttribute("Test with limits of surrogate range") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(surrogateEntity_4) { Attribute = new VariationAttribute("MIddle surrogate character") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(surrogateEntity_5) { Attribute = new VariationAttribute("Invalid high surrogate character") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(surrogateEntity_6) { Attribute = new VariationAttribute("Invalid low surrogate character") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(surrogateEntity_7) { Attribute = new VariationAttribute("Swap high-low surrogate characters") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(surrogateEntity_8) { Attribute = new VariationAttribute("WriteSurrogateCharEntity for special attributes") { Id = 8, Priority = 1 } });
                }
            }
            public partial class TCPI : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCPI
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(pi_1) { Attribute = new VariationAttribute("Sanity test for WritePI") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(pi_2) { Attribute = new VariationAttribute("PI text value = null") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(pi_3) { Attribute = new VariationAttribute("PI text value = String.Empty") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(pi_4) { Attribute = new VariationAttribute("PI name = null should error") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(pi_5) { Attribute = new VariationAttribute("PI name = String.Empty should error") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(pi_6) { Attribute = new VariationAttribute("WritePI with xmlns as the name value") { Id = 6 } });
                    this.AddChild(new TestVariation(pi_7) { Attribute = new VariationAttribute("WritePI with XmL as the name value") { Id = 7 } });
                    this.AddChild(new TestVariation(pi_8) { Attribute = new VariationAttribute("WritePI before XmlDecl") { Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(pi_9) { Attribute = new VariationAttribute("WritePI (after StartDocument) with name = 'xml' text = 'version = 1.0' should error") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(pi_10) { Attribute = new VariationAttribute("WritePI (before StartDocument) with name = 'xml' text = 'version = 1.0' should error") { Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(pi_12) { Attribute = new VariationAttribute("WriteProcessingInstruction with valid surrogate pair") { Id = 12, Priority = 1 } });
                    this.AddChild(new TestVariation(pi_13) { Attribute = new VariationAttribute("WritePI with invalid surrogate pair") { Id = 13, Priority = 1 } });
                }
            }
            public partial class TCWriteNmToken : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteNmToken
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(writeNmToken_1) { Attribute = new VariationAttribute("Name = String.Empty") { Param = "String.Empty", Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNmToken_1) { Attribute = new VariationAttribute("Name = null") { Param = "null", Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNmToken_2) { Attribute = new VariationAttribute("Sanity test, Name = foo") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNmToken_3) { Attribute = new VariationAttribute("Name contains letters, digits, . _ - : chars") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNmToken_4) { Attribute = new VariationAttribute("Name contains whitespace char") { Param = "test test", Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNmToken_4) { Attribute = new VariationAttribute("Name contains ? char") { Param = "test?", Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNmToken_4) { Attribute = new VariationAttribute("Name contains DQ") { Param = "\"test", Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(writeNmToken_4) { Attribute = new VariationAttribute("Name contains SQ") { Param = "test'", Id = 6, Priority = 1 } });
                }
            }
            public partial class TCWriteName : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteName
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(writeName_1) { Attribute = new VariationAttribute("Name = null") { Param = "null", Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(writeName_1) { Attribute = new VariationAttribute("Name = String.Empty") { Param = "String.Empty", Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(writeName_2) { Attribute = new VariationAttribute("Sanity test, Name = foo") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(writeName_3) { Attribute = new VariationAttribute("Sanity test, Name = foo:bar") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(writeName_4) { Attribute = new VariationAttribute("Name contains whitespace char") { Param = "foo bar", Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(writeName_4) { Attribute = new VariationAttribute("Name starts with :") { Param = ":bar", Id = 4, Priority = 1 } });
                }
            }
            public partial class TCWriteQName : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteQName
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(writeQName_1) { Attribute = new VariationAttribute("Name = null") { Param = "null", Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(writeQName_1) { Attribute = new VariationAttribute("Name = String.Empty") { Param = "String.Empty", Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(writeQName_2) { Attribute = new VariationAttribute("WriteQName with correct NS") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(writeQName_3) { Attribute = new VariationAttribute("WriteQName when NS is auto-generated") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(writeQName_4) { Attribute = new VariationAttribute("QName = foo:bar when foo is not in scope") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(writeQName_5) { Attribute = new VariationAttribute("Name contains whitespace char") { Param = "foo bar", Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(writeQName_5) { Attribute = new VariationAttribute("Name starts with :") { Param = ":bar", Id = 6, Priority = 1 } });
                }
            }
            public partial class TCWriteChars : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteChars
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(writeChars_1) { Attribute = new VariationAttribute("WriteChars with valid buffer, number, count") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(writeChars_2) { Attribute = new VariationAttribute("WriteChars with & < >") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(writeChars_3) { Attribute = new VariationAttribute("WriteChars following WriteStartAttribute") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(writeChars_4) { Attribute = new VariationAttribute("WriteChars with entity ref included") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(writeChars_5) { Attribute = new VariationAttribute("WriteChars with buffer = null") { Id = 5, Priority = 2 } });
                    this.AddChild(new TestVariation(writeChars_6) { Attribute = new VariationAttribute("WriteChars with count > buffer size") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(writeChars_7) { Attribute = new VariationAttribute("WriteChars with count < 0") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(writeChars_8) { Attribute = new VariationAttribute("WriteChars with index > buffer size") { Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(writeChars_9) { Attribute = new VariationAttribute("WriteChars with index < 0") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(writeChars_10) { Attribute = new VariationAttribute("WriteChars with index + count exceeds buffer") { Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(writeChars_11) { Attribute = new VariationAttribute("WriteChars for xml:lang attribute, index = count = 0") { Id = 11, Priority = 1 } });
                }
            }
            public partial class TCWriteString : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteString
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(writeString_1) { Attribute = new VariationAttribute("WriteString(null)") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(writeString_2) { Attribute = new VariationAttribute("WriteString(String.Empty)") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_3) { Attribute = new VariationAttribute("WriteString with valid surrogate pair") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_4) { Attribute = new VariationAttribute("WriteString with invalid surrogate pair") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_5) { Attribute = new VariationAttribute("WriteString with entity reference") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_6) { Attribute = new VariationAttribute("WriteString with single/double quote, &, <, >") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_9) { Attribute = new VariationAttribute("WriteString for value greater than x1F") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_11) { Attribute = new VariationAttribute("WriteString with CR, LF, CR LF inside attribute value") { Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_12) { Attribute = new VariationAttribute("Call WriteString for LF inside attribute") { Id = 12, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_13) { Attribute = new VariationAttribute("Surrogate characters in text nodes, range limits") { Id = 13, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_14) { Attribute = new VariationAttribute("High surrogate on last position") { Id = 14, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_15) { Attribute = new VariationAttribute("Low surrogate on first position") { Id = 15, Priority = 1 } });
                    this.AddChild(new TestVariation(writeString_16) { Attribute = new VariationAttribute("Swap low-high surrogates") { Id = 16, Priority = 1 } });
                }
            }
            public partial class TCWhiteSpace : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWhiteSpace
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(whitespace_3) { Attribute = new VariationAttribute("WriteWhitespace before and after root element") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(whitespace_4) { Attribute = new VariationAttribute("WriteWhitespace with String.Empty ") { Param = "String.Empty", Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(whitespace_4) { Attribute = new VariationAttribute("WriteWhitespace with null ") { Param = "null", Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(whitespace_5) { Attribute = new VariationAttribute("WriteWhitespace with invalid char") { Param = 0, Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(whitespace_5) { Attribute = new VariationAttribute("WriteWhitespace with invalid char") { Param = 16, Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(whitespace_5) { Attribute = new VariationAttribute("WriteWhitespace with invalid char") { Param = 97, Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(whitespace_5) { Attribute = new VariationAttribute("WriteWhitespace with invalid char") { Param = 31, Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(whitespace_5) { Attribute = new VariationAttribute("WriteWhitespace with invalid char") { Param = 14, Id = 7, Priority = 1 } });
                }
            }
            public partial class TCWriteValue : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteValue
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(writeValue_1) { Attribute = new VariationAttribute("WriteValue(boolean)") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_2) { Attribute = new VariationAttribute("WriteValue(DateTime)") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_3) { Attribute = new VariationAttribute("WriteValue(decimal)") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_4) { Attribute = new VariationAttribute("WriteValue(double)") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_5) { Attribute = new VariationAttribute("WriteValue(int32)") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_6) { Attribute = new VariationAttribute("WriteValue(int64)") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_7) { Attribute = new VariationAttribute("WriteValue(single)") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_8) { Attribute = new VariationAttribute("WriteValue(string)") { Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_9) { Attribute = new VariationAttribute("WriteValue(DateTimeOffset)") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_11) { Attribute = new VariationAttribute("Write multiple atomic values inside element") { Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_12) { Attribute = new VariationAttribute("Write multiple atomic values inside attribute") { Id = 12, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_13) { Attribute = new VariationAttribute("Write multiple atomic values inside element, separate by WriteWhitespace(' ')") { Id = 13, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_14) { Attribute = new VariationAttribute("Write multiple atomic values inside element, separate by WriteString(' ')") { Id = 14, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_15) { Attribute = new VariationAttribute("Write multiple atomic values inside attribute, separate by WriteWhitespace(' ')") { Id = 15, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_16) { Attribute = new VariationAttribute("Write multiple atomic values inside attribute, seperate by WriteString(' ')") { Id = 16, Priority = 1 } });
                    this.AddChild(new TestVariation(writeValue_17) { Attribute = new VariationAttribute("WriteValue(long)") { Id = 17, Priority = 1 } });
                }
            }
            public partial class TCLookUpPrefix : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCLookUpPrefix
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(lookupPrefix_1) { Attribute = new VariationAttribute("LookupPrefix with null") { Id = 1, Priority = 2 } });
                    this.AddChild(new TestVariation(lookupPrefix_2) { Attribute = new VariationAttribute("LookupPrefix with String.Empty should if(!String.Empty") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(lookupPrefix_3) { Attribute = new VariationAttribute("LookupPrefix with generated namespace used for attributes") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(lookupPrefix_4) { Attribute = new VariationAttribute("LookupPrefix for namespace used with element") { Id = 4, Priority = 0 } });
                    this.AddChild(new TestVariation(lookupPrefix_5) { Attribute = new VariationAttribute("LookupPrefix for namespace used with attribute") { Id = 5, Priority = 0 } });
                    this.AddChild(new TestVariation(lookupPrefix_6) { Attribute = new VariationAttribute("Lookup prefix for a default namespace") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(lookupPrefix_7) { Attribute = new VariationAttribute("Lookup prefix for nested element with same namespace but different prefix") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(lookupPrefix_8) { Attribute = new VariationAttribute("Lookup prefix for multiple prefix associated with the same namespace") { Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(lookupPrefix_9) { Attribute = new VariationAttribute("Lookup prefix for namespace defined outside the scope of an empty element and also defined in its parent") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(lookupPrefix_10) { Attribute = new VariationAttribute("Lookup prefix for namespace declared as default and also with a prefix") { Id = 10, Priority = 1 } });
                }
            }
            public partial class TCXmlSpaceWriter : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCXmlSpaceWriter
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(xmlSpace_1) { Attribute = new VariationAttribute("Verify XmlSpace as Preserve") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(xmlSpace_2) { Attribute = new VariationAttribute("Verify XmlSpace as Default") { Id = 2, Priority = 0 } });
                    this.AddChild(new TestVariation(xmlSpace_3) { Attribute = new VariationAttribute("Verify XmlSpace as None") { Id = 3, Priority = 0 } });
                    this.AddChild(new TestVariation(xmlSpace_4) { Attribute = new VariationAttribute("Verify XmlSpace within an empty element") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(xmlSpace_5) { Attribute = new VariationAttribute("Verify XmlSpace - scope with nested elements (both PROLOG and EPILOG)") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(xmlSpace_6) { Attribute = new VariationAttribute("Verify XmlSpace - outside defined scope") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(xmlSpace_7) { Attribute = new VariationAttribute("Verify XmlSpace with invalid space value") { Id = 7, Priority = 0 } });
                    this.AddChild(new TestVariation(xmlSpace_8) { Attribute = new VariationAttribute("Duplicate xml:space attr should error") { Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(xmlSpace_9) { Attribute = new VariationAttribute("Verify XmlSpace value when received through WriteString") { Id = 9, Priority = 1 } });
                }
            }
            public partial class TCXmlLangWriter : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCXmlLangWriter
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(XmlLang_1) { Attribute = new VariationAttribute("Verify XmlLang sanity test") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(XmlLang_2) { Attribute = new VariationAttribute("Verify that default value of XmlLang is NULL") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(XmlLang_3) { Attribute = new VariationAttribute("Verify XmlLang scope inside nested elements (both PROLOG and EPILOG)") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(XmlLang_4) { Attribute = new VariationAttribute("Duplicate xml:lang attr should error") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(XmlLang_5) { Attribute = new VariationAttribute("Verify XmlLang value when received through WriteAttributes") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(XmlLang_6) { Attribute = new VariationAttribute("Verify XmlLang value when received through WriteString") { Id = 6 } });
                    this.AddChild(new TestVariation(XmlLang_7) { Attribute = new VariationAttribute("Should not check XmlLang value") { Id = 7, Priority = 2 } });
                    this.AddChild(new TestVariation(XmlLang_8) { Attribute = new VariationAttribute("More XmlLang with valid sequence") { Id = 8, Priority = 1 } });
                }
            }
            public partial class TCWriteRaw : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteRaw
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(writeRaw_1) { Attribute = new VariationAttribute("Call both WriteRaw Methods") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(writeRaw_4) { Attribute = new VariationAttribute("Call WriteRaw to write the value of xml:space") { Id = 4 } });
                    this.AddChild(new TestVariation(writerRaw_5) { Attribute = new VariationAttribute("Call WriteRaw to write the value of xml:lang") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(writeRaw_6) { Attribute = new VariationAttribute("WriteRaw with count > buffer size") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(writeRaw_7) { Attribute = new VariationAttribute("WriteRaw with count < 0") { Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(writeRaw_8) { Attribute = new VariationAttribute("WriteRaw with index > buffer size") { Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(writeRaw_9) { Attribute = new VariationAttribute("WriteRaw with index < 0") { Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(writeRaw_10) { Attribute = new VariationAttribute("WriteRaw with index + count exceeds buffer") { Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(writeRaw_11) { Attribute = new VariationAttribute("WriteRaw with buffer = null") { Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(writeRaw_12) { Attribute = new VariationAttribute("WriteRaw with valid surrogate pair") { Id = 12, Priority = 1 } });
                    this.AddChild(new TestVariation(writeRaw_13) { Attribute = new VariationAttribute("WriteRaw with invalid surrogate pair") { Id = 13, Priority = 1 } });
                    this.AddChild(new TestVariation(writeRaw_14) { Attribute = new VariationAttribute("Index = Count = 0") { Id = 14, Priority = 1 } });
                }
            }
            public partial class TCWriteBase64 : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteBase64
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(Base64_2) { Attribute = new VariationAttribute("WriteBase64 with count > buffer size") { Id = 20, Priority = 1 } });
                    this.AddChild(new TestVariation(Base64_3) { Attribute = new VariationAttribute("WriteBase64 with count < 0") { Id = 30, Priority = 1 } });
                    this.AddChild(new TestVariation(Base64_4) { Attribute = new VariationAttribute("WriteBase64 with index > buffer size") { Id = 40, Priority = 1 } });
                    this.AddChild(new TestVariation(Base64_5) { Attribute = new VariationAttribute("WriteBase64 with index < 0") { Id = 50, Priority = 1 } });
                    this.AddChild(new TestVariation(Base64_6) { Attribute = new VariationAttribute("WriteBase64 with index + count exceeds buffer") { Id = 60, Priority = 1 } });
                    this.AddChild(new TestVariation(Base64_7) { Attribute = new VariationAttribute("WriteBase64 with buffer = null") { Id = 70, Priority = 1 } });
                    this.AddChild(new TestVariation(Base64_9) { Attribute = new VariationAttribute("Base64 should not be allowed inside namespace decl") { Param = "ns", Id = 92, Priority = 1 } });
                    this.AddChild(new TestVariation(Base64_9) { Attribute = new VariationAttribute("Base64 should not be allowed inside xml:lang value") { Param = "lang", Id = 90, Priority = 1 } });
                    this.AddChild(new TestVariation(Base64_9) { Attribute = new VariationAttribute("Base64 should not be allowed inside xml:space value") { Param = "space", Id = 91, Priority = 1 } });
                }
            }
            public partial class TCWriteState : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCWriteState
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(writeState_1) { Attribute = new VariationAttribute("Verify WriteState.Start when nothing has been written yet") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(writeState_2) { Attribute = new VariationAttribute("Verify correct state when writing in Prolog") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_3) { Attribute = new VariationAttribute("Verify correct state when writing an attribute") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_4) { Attribute = new VariationAttribute("Verify correct state when writing element content") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_5) { Attribute = new VariationAttribute("Verify correct state after Close has been called") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_6) { Attribute = new VariationAttribute("Verify WriteState = Error after an exception") { Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteNmToken after WriteState = Error") { Param = "WriteNmToken", Id = 25, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteBinHex after WriteState = Error") { Param = "WriteBinHex", Id = 23, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call LookupPrefix after WriteState = Error") { Param = "LookupPrefix", Id = 24, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteComment after WriteState = Error") { Param = "WriteComment", Id = 13, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteWhitespace after WriteState = Error") { Param = "WriteWhitespace", Id = 18, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WritePI after WriteState = Error") { Param = "WritePI", Id = 14, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteName after WriteState = Error") { Param = "WriteName", Id = 26, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteString after WriteState = Error") { Param = "WriteString", Id = 19, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteQualifiedName after WriteState = Error") { Param = "WriteQualifiedName", Id = 27, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteValue after WriteState = Error") { Param = "WriteValue", Id = 28, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteAttributes after WriteState = Error") { Param = "WriteAttributes", Id = 29, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteNode(reader) after WriteState = Error") { Param = "WriteNodeReader", Id = 31, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call Flush after WriteState = Error") { Param = "Flush", Id = 32, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteRaw after WriteState = Error") { Param = "WriteRaw", Id = 21, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteSurrogateCharEntity after WriteState = Error") { Param = "WriteSurrogateCharEntity", Id = 17, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteEntityRef after WriteState = Error") { Param = "WriteEntityRef", Id = 15, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteCharEntiry after WriteState = Error") { Param = "WriteCharEntity", Id = 16, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteBase64 after WriteState = Error") { Param = "WriteBase64", Id = 22, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteChars after WriteState = Error") { Param = "WriteChars", Id = 20, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteStartDocument after WriteState = Error") { Param = "WriteStartDocument", Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteStartElement after WriteState = Error") { Param = "WriteStartElement", Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteEndElement after WriteState = Error") { Param = "WriteEndElement", Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteStartAttribute after WriteState = Error") { Param = "WriteStartAttribute", Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteEndAttribute after WriteState = Error") { Param = "WriteEndAttribute", Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_7) { Attribute = new VariationAttribute("Call WriteCData after WriteState = Error") { Param = "WriteCData", Id = 12, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_8) { Attribute = new VariationAttribute("XmlSpace property after WriteState = Error") { Param = "XmlSpace", Id = 33, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_8) { Attribute = new VariationAttribute("XmlLang property after WriteState = Error") { Param = "XmlSpace", Id = 34, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteValue after Close()") { Param = "WriteValue", Id = 27, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WritePI after Close()") { Param = "WritePI", Id = 13, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteEntityRef after Close()") { Param = "WriteEntityRef", Id = 14, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteCharEntiry after Close()") { Param = "WriteCharEntity", Id = 15, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteSurrogateCharEntity after Close()") { Param = "WriteSurrogateCharEntity", Id = 16, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteWhitespace after Close()") { Param = "WriteWhitespace", Id = 17, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteString after Close()") { Param = "WriteString", Id = 18, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteChars after Close()") { Param = "WriteChars", Id = 19, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteRaw after Close()") { Param = "WriteRaw", Id = 20, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteBase64 after Close()") { Param = "WriteBase64", Id = 21, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteBinHex after Close()") { Param = "WriteBinHex", Id = 22, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call LookupPrefix after Close()") { Param = "LookupPrefix", Id = 23, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteNmToken after Close()") { Param = "WriteNmToken", Id = 24, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteName after Close()") { Param = "WriteName", Id = 25, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteQualifiedName after Close()") { Param = "WriteQualifiedName", Id = 26, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteStartDocument after Close()") { Param = "WriteStartDocument", Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteAttributes after Close()") { Param = "WriteAttributes", Id = 28, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteNode(reader) after Close()") { Param = "WriteNodeReader", Id = 30, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call Flush after Close()") { Param = "Flush", Id = 31, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteStartElement after Close()") { Param = "WriteStartElement", Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteEndElement after Close()") { Param = "WriteEndElement", Id = 8, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteStartAttribute after Close()") { Param = "WriteStartAttribute", Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteEndAttribute after Close()") { Param = "WriteEndAttribute", Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteCData after Close()") { Param = "WriteCData", Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(writeState_9) { Attribute = new VariationAttribute("Call WriteComment after Close()") { Param = "WriteComment", Id = 12, Priority = 1 } });
                }
            }
            public partial class TC_NDP20_NewMethods : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TC_NDP20_NewMethods
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(var_1) { Attribute = new VariationAttribute("WriteElementString(prefix, name, ns, value) sanity test") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(var_2) { Attribute = new VariationAttribute("WriteElementString(prefix = xml, ns = XML namespace)") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(var_3) { Attribute = new VariationAttribute("WriteStartAttribute(string name) sanity test") { Id = 3, Priority = 0 } });
                    this.AddChild(new TestVariation(var_4) { Attribute = new VariationAttribute("WriteElementString followed by attribute should error") { Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(var_5) { Attribute = new VariationAttribute("429445: XmlWellformedWriter wrapping another XmlWriter should check the duplicate attributes first") { Id = 5, Priority = 1 } });
                }
            }
            public partial class TCGlobalization : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCGlobalization
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(var_1) { Attribute = new VariationAttribute("Characters between 0xdfff and 0xfffe are valid Unicode characters") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(var_2) { Attribute = new VariationAttribute("XmlWriter using UTF-16BE encoding writes out wrong encoding name value in the xml decl") { Id = 2, Priority = 1 } });
                }
            }
            public partial class TCClose : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCClose
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(var_1) { Attribute = new VariationAttribute("Closing an XmlWriter should close all opened elements") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(var_2) { Attribute = new VariationAttribute("Disposing an XmlWriter should close all opened elements") { Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(var_3) { Attribute = new VariationAttribute("Dispose() shouldn't throw when a tag is not closed and inner stream is closed") { Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(var_4) { Attribute = new VariationAttribute("Close() should be allowed when XML doesn't have content") { Id = 4, Priority = 1 } });
                }
            }
            public partial class TCEOFHandling : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCEOFHandling
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(EOF_Handling_01) { Attribute = new VariationAttribute("NewLineHandling Default value - NewLineHandling.Replace") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_02) { Attribute = new VariationAttribute("XmlWriter creation with NewLineHandling.Entitize") { Param = 1, Id = 2, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_02) { Attribute = new VariationAttribute("XmlWriter creation with NewLineHandling.None") { Param = 2, Id = 4, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_02) { Attribute = new VariationAttribute("XmlWriter creation with NewLineHandling.Replace") { Param = 0, Id = 3, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_06) { Attribute = new VariationAttribute("Check for tab character in element with 'Replace'") { Param = 0, Id = 15, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_06) { Attribute = new VariationAttribute("Check for tab character in element with 'Entitize'") { Param = 1, Id = 14, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_06) { Attribute = new VariationAttribute("Check for tab character in element with 'None'") { Param = 2, Id = 16, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_07) { Attribute = new VariationAttribute("Check for combinations of NewLine characters in attribute with 'Entitize'") { Param = 1, Id = 17, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_07) { Attribute = new VariationAttribute("Check for combinations of NewLine characters in attribute with 'Replace'") { Param = 0, Id = 18, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_08) { Attribute = new VariationAttribute("Check for combinations of entities in attribute with 'None'") { Param = 2, Id = 22, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_08) { Attribute = new VariationAttribute("Check for combinations of entities in attribute with 'Entitize'") { Param = 1, Id = 20, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_08) { Attribute = new VariationAttribute("Check for combinations of entities in attribute with 'Replace'") { Param = 0, Id = 21, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_09) { Attribute = new VariationAttribute("Check for combinations of NewLine characters and entities in element with 'Entitize'") { Param = 1, Id = 23, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_09) { Attribute = new VariationAttribute("Check for combinations of NewLine characters and entities in element with 'Replace'") { Param = 0, Id = 24, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_10) { Attribute = new VariationAttribute("Check for tab character in attribute with 'Replace'") { Param = 0, Id = 27, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_10) { Attribute = new VariationAttribute("Check for tab character in attribute with 'Entitize'") { Param = 1, Id = 26, Priority = 0 } });
                    this.AddChild(new TestVariation(EOF_Handling_11) { Attribute = new VariationAttribute("NewLineChars and IndentChars Default values and test for proper indentation, None") { Param = 2, Id = 31, Priority = 1 } });
                    this.AddChild(new TestVariation(EOF_Handling_11) { Attribute = new VariationAttribute("NewLineChars and IndentChars Default values and test for proper indentation, Replace") { Param = 0, Id = 30, Priority = 1 } });
                    this.AddChild(new TestVariation(EOF_Handling_11) { Attribute = new VariationAttribute("NewLineChars and IndentChars Default values and test for proper indentation, Entitize") { Param = 1, Id = 29, Priority = 1 } });
                    this.AddChild(new TestVariation(EOF_Handling_13) { Attribute = new VariationAttribute("Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\\r', '  '") { Params = new object[] { NewLineHandling.Replace, "\r", "  " }, Id = 33, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_13) { Attribute = new VariationAttribute("Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\\r', '  '") { Params = new object[] { NewLineHandling.None, "\r", "  " }, Id = 34, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_13) { Attribute = new VariationAttribute("Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '&#xA;', '  '") { Params = new object[] { NewLineHandling.Entitize, "&#xA;", "  " }, Id = 35, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_13) { Attribute = new VariationAttribute("Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\\r', '\\n'") { Params = new object[] { NewLineHandling.None, "\r", "\n" }, Id = 40, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_13) { Attribute = new VariationAttribute("Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\\r', '  '") { Params = new object[] { NewLineHandling.Entitize, "\r", "  " }, Id = 32, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_13) { Attribute = new VariationAttribute("Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '&#xA;', '  '") { Params = new object[] { NewLineHandling.Replace, "&#xA;", "  " }, Id = 36, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_13) { Attribute = new VariationAttribute("Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '&#xA;', '  '") { Params = new object[] { NewLineHandling.None, "&#xA;", "  " }, Id = 37, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_13) { Attribute = new VariationAttribute("Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\\r', '\\n'") { Params = new object[] { NewLineHandling.Entitize, "\r", "\n" }, Id = 38, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_13) { Attribute = new VariationAttribute("Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\\r', '\\n'") { Params = new object[] { NewLineHandling.Replace, "\r", "\n" }, Id = 39, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_15) { Attribute = new VariationAttribute("NewLine handling in attribute when Indent=true; Entitize, '\\r'") { Params = new object[] { NewLineHandling.Entitize, "\r" }, Id = 53, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_15) { Attribute = new VariationAttribute("NewLine handling in attribute when Indent=true; Replace, '\\r\\n'") { Params = new object[] { NewLineHandling.Replace, "\r\n" }, Id = 51, Priority = 1 } });
                    this.AddChild(new TestVariation(EOF_Handling_15) { Attribute = new VariationAttribute("NewLine handling in attribute when Indent=true; Replace, '\\r'") { Params = new object[] { NewLineHandling.Replace, "\r" }, Id = 54, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_15) { Attribute = new VariationAttribute("NewLine handling in attribute when Indent=true; Replace, '---'") { Params = new object[] { NewLineHandling.Replace, "---" }, Id = 55, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_15) { Attribute = new VariationAttribute("NewLine handling in attribute when Indent=true; Entitize, '---'") { Params = new object[] { NewLineHandling.Entitize, "---" }, Id = 54, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_15) { Attribute = new VariationAttribute("NewLine handling in attribute when Indent=true; Entitize, '\\r\\n'") { Params = new object[] { NewLineHandling.Entitize, "\r\n" }, Id = 50, Priority = 1 } });
                    this.AddChild(new TestVariation(EOF_Handling_16) { Attribute = new VariationAttribute("NewLine handling between attributes when NewLineOnAttributes=true; None, '\\r\\n'") { Params = new object[] { NewLineHandling.None, "\r\n" }, Id = 58, Priority = 1 } });
                    this.AddChild(new TestVariation(EOF_Handling_16) { Attribute = new VariationAttribute("NewLine handling between attributes when NewLineOnAttributes=true; None, '\\r'") { Params = new object[] { NewLineHandling.None, "\r" }, Id = 61, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_16) { Attribute = new VariationAttribute("NewLine handling between attributes when NewLineOnAttributes=true; Replace, '\\r'") { Params = new object[] { NewLineHandling.Replace, "\r" }, Id = 60, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_16) { Attribute = new VariationAttribute("NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '\\r'") { Params = new object[] { NewLineHandling.Entitize, "\r" }, Id = 59, Priority = 2 } });
                    this.AddChild(new TestVariation(EOF_Handling_16) { Attribute = new VariationAttribute("NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '\\r\\n'") { Params = new object[] { NewLineHandling.Entitize, "\r\n" }, Id = 56, Priority = 1 } });
                    this.AddChild(new TestVariation(EOF_Handling_16) { Attribute = new VariationAttribute("NewLine handling between attributes when NewLineOnAttributes=true; Replace, '\\r\\n'") { Params = new object[] { NewLineHandling.Replace, "\r\n" }, Id = 57, Priority = 1 } });
                }
            }
            public partial class XObjectBuilderTest : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+XObjectBuilderTest
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(var_1) { Attribute = new VariationAttribute("LookupPrefix(null)") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_2) { Attribute = new VariationAttribute("WriteAttributes(null, false)") { Param = false, Priority = 2 } });
                    this.AddChild(new TestVariation(var_2) { Attribute = new VariationAttribute("WriteAttributes(null, true)") { Param = true, Priority = 2 } });
                    this.AddChild(new TestVariation(var_3) { Attribute = new VariationAttribute("WriteAttributeString(null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_3) { Attribute = new VariationAttribute("WriteAttributeString(String.Empty)") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_4) { Attribute = new VariationAttribute("WriteBase64(null)") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_5) { Attribute = new VariationAttribute("WriteBinHex(null)") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_6) { Attribute = new VariationAttribute("WriteCData('')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_6) { Attribute = new VariationAttribute("WriteCData(null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_7) { Attribute = new VariationAttribute("WriteChars(null)") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_8) { Attribute = new VariationAttribute("Other APIs") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_9) { Attribute = new VariationAttribute("WriteDocType(null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_9) { Attribute = new VariationAttribute("WriteDocType('')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_10) { Attribute = new VariationAttribute("WriteElementString(String.Empty)") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_10) { Attribute = new VariationAttribute("WriteElementString(null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_11) { Attribute = new VariationAttribute("WriteEndAttribute()") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_12) { Attribute = new VariationAttribute("WriteEndDocument()") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_13) { Attribute = new VariationAttribute("WriteEndElement()") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_14) { Attribute = new VariationAttribute("WriteEntityRef(null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_14) { Attribute = new VariationAttribute("WriteEntityRef('')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_15) { Attribute = new VariationAttribute("WriteFullEndElement()") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_16) { Attribute = new VariationAttribute("WriteName('')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_16) { Attribute = new VariationAttribute("WriteName(null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_17) { Attribute = new VariationAttribute("WriteNmToken('')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_17) { Attribute = new VariationAttribute("WriteNmToken(null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_18) { Attribute = new VariationAttribute("WriteNode(null)") { Params = new object[] { "reader", true }, Priority = 2 } });
                    this.AddChild(new TestVariation(var_18) { Attribute = new VariationAttribute("WriteNode(null)") { Params = new object[] { "reader", false }, Priority = 2 } });
                    this.AddChild(new TestVariation(var_19) { Attribute = new VariationAttribute("WriteProcessingInstruction('', '')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_19) { Attribute = new VariationAttribute("WriteProcessingInstruction(null, null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_20) { Attribute = new VariationAttribute("WriteQualifiedName('', '')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_20) { Attribute = new VariationAttribute("WriteQualifiedName(null, null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_21) { Attribute = new VariationAttribute("WriteRaw(null, 0, 0)") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_22) { Attribute = new VariationAttribute("WriteStartAttribute(null, null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_22) { Attribute = new VariationAttribute("WriteStartAttribute('', '')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_23) { Attribute = new VariationAttribute("WriteStartElement(null, null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_23) { Attribute = new VariationAttribute("WriteStartElement('', '')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_24) { Attribute = new VariationAttribute("WriteStartDocument(true)") { Param = 2, Priority = 2 } });
                    this.AddChild(new TestVariation(var_24) { Attribute = new VariationAttribute("WriteStartDocument()") { Param = 1, Priority = 2 } });
                    this.AddChild(new TestVariation(var_24) { Attribute = new VariationAttribute("WriteStartDocument(false)") { Param = 3, Priority = 2 } });
                    this.AddChild(new TestVariation(var_25) { Attribute = new VariationAttribute("WriteString(null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_25) { Attribute = new VariationAttribute("WriteString('')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_26) { Attribute = new VariationAttribute("WriteValue(false)") { Param = false, Priority = 2 } });
                    this.AddChild(new TestVariation(var_26) { Attribute = new VariationAttribute("WriteValue(true)") { Param = true, Priority = 2 } });
                    this.AddChild(new TestVariation(var_27) { Attribute = new VariationAttribute("WriteWhitespace('')") { Param = "", Priority = 2 } });
                    this.AddChild(new TestVariation(var_27) { Attribute = new VariationAttribute("WriteWhitespace(null)") { Param = null, Priority = 2 } });
                    this.AddChild(new TestVariation(var_28) { Attribute = new VariationAttribute("EntityRef after Document should error - PROLOG") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_29) { Attribute = new VariationAttribute("EntityRef after Document should error - EPILOG") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_30) { Attribute = new VariationAttribute("CharEntity after Document should error - PROLOG") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_31) { Attribute = new VariationAttribute("CharEntity after Document should error - EPILOG") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_32) { Attribute = new VariationAttribute("SurrogateCharEntity after Document should error - PROLOG") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_33) { Attribute = new VariationAttribute("SurrogateCharEntity after Document should error - EPILOG") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_34) { Attribute = new VariationAttribute("Attribute after Document should error - PROLOG") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_35) { Attribute = new VariationAttribute("Attribute after Document should error - EPILOG") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_36) { Attribute = new VariationAttribute("CDATA after Document should error - PROLOG") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_37) { Attribute = new VariationAttribute("CDATA after Document should error - EPILOG") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_38) { Attribute = new VariationAttribute("Element followed by Document should error") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_39) { Attribute = new VariationAttribute("Element followed by DocType should error") { Priority = 2 } });
                    this.AddChild(new TestVariation(Variation41) { Attribute = new VariationAttribute("WriteBase64") });
                    this.AddChild(new TestVariation(Variation42) { Attribute = new VariationAttribute("WriteEntityRef") });
                }
            }
            public partial class NamespacehandlingWriterSanity : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+NamespacehandlingWriterSanity
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Xml namespace II.") { Params = new object[] { "<p:A xmlns:p='nsp'><p:B xmlns:p='nsp'><p:C xmlns:p='nsp' xmlns:xml='http://www.w3.org/XML/1998/namespace'/></p:B></p:A>" }, Priority = 3 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Xml namespace III.") { Params = new object[] { "<p:A xmlns:p='nsp'><p:B xmlns:xml='http://www.w3.org/XML/1998/namespace' xmlns:p='nsp'><p:C xmlns:p='nsp' xmlns:xml='http://www.w3.org/XML/1998/namespace'/></p:B></p:A>" }, Priority = 3 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("2 levels down") { Params = new object[] { "<p:A xmlns:p='nsp'><B><p:C xmlns:p='nsp'/></B></p:A>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("2 levels down II.") { Params = new object[] { "<p:A xmlns:p='nsp'><B><C xmlns:p='nsp'/></B></p:A>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("2 levels down III.") { Params = new object[] { "<A xmlns:p='nsp'><B><p:C xmlns:p='nsp'/></B></A>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Siblings") { Params = new object[] { "<A xmlns:p='nsp'><p:B xmlns:p='nsp'/><C xmlns:p='nsp'/><p:C xmlns:p='nsp'/></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Children") { Params = new object[] { "<A xmlns:p='nsp'><p:B xmlns:p='nsp'><C xmlns:p='nsp'><p:C xmlns:p='nsp'/></C></p:B></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Xml namespace I.") { Params = new object[] { "<A xmlns:xml='http://www.w3.org/XML/1998/namespace'/>" }, Priority = 3 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("1 level down") { Params = new object[] { "<p:A xmlns:p='nsp'><p:B xmlns:p='nsp'><p:C xmlns:p='nsp'/></p:B></p:A>" }, Priority = 0 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("1 level down II.") { Params = new object[] { "<A><p:B xmlns:p='nsp'><p:C xmlns:p='nsp'/></p:B></A>" }, Priority = 0 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Not used NS declarations") { Params = new object[] { "<A xmlns='nsp' xmlns:u='not-used'><p:B xmlns:p='nsp'><C xmlns:u='not-used' xmlns='nsp' /></p:B></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("SameNS, different prefix") { Params = new object[] { "<p:A xmlns:p='nsp'><B xmlns:q='nsp'><p:C xmlns:p='nsp'/></B></p:A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Default namespaces") { Params = new object[] { "<A xmlns='nsp'><p:B xmlns:p='nsp'><C xmlns='nsp' /></p:B></A>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeTricky) { Attribute = new VariationAttribute("Default ns parent autogenerated") { Priority = 1 } });
                    this.AddChild(new TestVariation(testConflicts) { Attribute = new VariationAttribute("Conflicts: NS undeclaration, default NS") { Params = new object[] { "<A xmlns='nsp'><B xmlns=''><C xmlns='nsp'><D xmlns='nsp'/></C></B></A>", "<A xmlns='nsp'><B xmlns=''><C xmlns='nsp'><D/></C></B></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testConflicts) { Attribute = new VariationAttribute("Conflicts: NS redefinition") { Params = new object[] { "<p:A xmlns:p='nsp'><p:B xmlns:p='ns-other'><p:C xmlns:p='nsp'><D xmlns:p='nsp'/></p:C></p:B></p:A>", "<p:A xmlns:p='nsp'><p:B xmlns:p='ns-other'><p:C xmlns:p='nsp'><D/></p:C></p:B></p:A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testConflicts) { Attribute = new VariationAttribute("Conflicts: NS redefinition, default NS") { Params = new object[] { "<A xmlns='nsp'><B xmlns='ns-other'><C xmlns='nsp'><D xmlns='nsp'/></C></B></A>", "<A xmlns='nsp'><B xmlns='ns-other'><C xmlns='nsp'><D/></C></B></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testConflicts) { Attribute = new VariationAttribute("Conflicts: NS redefinition, default NS II.") { Params = new object[] { "<A xmlns=''><B xmlns='ns-other'><C xmlns=''><D xmlns=''/></C></B></A>", "<A><B xmlns='ns-other'><C xmlns=''><D/></C></B></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testFromChildNode1) { Attribute = new VariationAttribute("Not from root") { Priority = 1 } });
                    this.AddChild(new TestVariation(testFromChildNode2) { Attribute = new VariationAttribute("Not from root II.") { Priority = 1 } });
                    this.AddChild(new TestVariation(testFromChildNode3) { Attribute = new VariationAttribute("Not from root III.") { Priority = 2 } });
                    this.AddChild(new TestVariation(testFromChildNode4) { Attribute = new VariationAttribute("Not from root IV.") { Priority = 2 } });
                    this.AddChild(new TestVariation(testIntoOpenedWriter) { Attribute = new VariationAttribute("Write into used reader III.") { Params = new object[] { "<p1:A xmlns:p1='nsp'><B xmlns:p1='nsp'/></p1:A>", "<p1:root xmlns:p1='nsp'><p1:A><B/></p1:A></p1:root>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testIntoOpenedWriter) { Attribute = new VariationAttribute("Write into used reader I.") { Params = new object[] { "<A xmlns:p1='nsp'/>", "<p1:root xmlns:p1='nsp'><A/></p1:root>" }, Priority = 0 } });
                    this.AddChild(new TestVariation(testIntoOpenedWriter) { Attribute = new VariationAttribute("Write into used reader II.") { Params = new object[] { "<p1:A xmlns:p1='nsp'/>", "<p1:root xmlns:p1='nsp'><p1:A/></p1:root>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testIntoOpenedWriterDefaultNS) { Attribute = new VariationAttribute("Write into used reader I. (def. ns.)") { Params = new object[] { "<A xmlns='nsp'/>", "<root xmlns='nsp'><A/></root>" }, Priority = 0 } });
                    this.AddChild(new TestVariation(testIntoOpenedWriterDefaultNS) { Attribute = new VariationAttribute("Write into used reader II. (def. ns.)") { Params = new object[] { "<A xmlns='ns-other'><B xmlns='nsp'><C xmlns='nsp'/></B></A>", "<root xmlns='nsp'><A xmlns='ns-other'><B xmlns='nsp'><C/></B></A></root>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testIntoOpenedWriterXlinqLookup1) { Attribute = new VariationAttribute("Write into used reader (Xlinq lookup + existing hint in the Writer; different prefix)") { Params = new object[] { "<p1:root xmlns:p1='nsp'><p2:B xmlns:p2='nsp'/></p1:root>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testIntoOpenedWriterXlinqLookup2) { Attribute = new VariationAttribute("Write into used reader (Xlinq lookup + existing hint in the Writer; same prefix)") { Params = new object[] { "<p1:root xmlns:p1='nsp'><p1:B /></p1:root>" }, Priority = 2 } });
                }
            }
            public partial class OmitAnotation : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+OmitAnotation
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(NoAnnotation) { Attribute = new VariationAttribute("No annotation - element") { Params = new object[] { typeof(XElement), "Simple.xml" }, Priority = 0 } });
                    this.AddChild(new TestVariation(NoAnnotation) { Attribute = new VariationAttribute("No annotation - document") { Params = new object[] { typeof(XDocument), "Simple.xml" }, Priority = 0 } });
                    this.AddChild(new TestVariation(AnnotationWithoutTheOmitDuplicates) { Attribute = new VariationAttribute("Annotation on document without omit - DisableFormating") { Params = new object[] { typeof(XDocument), "Simple.xml", SaveOptions.DisableFormatting }, Priority = 2 } });
                    this.AddChild(new TestVariation(AnnotationWithoutTheOmitDuplicates) { Attribute = new VariationAttribute("Annotation on element without omit - None") { Params = new object[] { typeof(XElement), "Simple.xml", SaveOptions.None }, Priority = 2 } });
                    this.AddChild(new TestVariation(AnnotationWithoutTheOmitDuplicates) { Attribute = new VariationAttribute("Annotation on element without omit - DisableFormating") { Params = new object[] { typeof(XElement), "Simple.xml", SaveOptions.DisableFormatting }, Priority = 2 } });
                    this.AddChild(new TestVariation(AnnotationWithoutTheOmitDuplicates) { Attribute = new VariationAttribute("Annotation on document without omit - None") { Params = new object[] { typeof(XDocument), "Simple.xml", SaveOptions.None }, Priority = 2 } });
                    this.AddChild(new TestVariation(XDocAnnotation) { Attribute = new VariationAttribute("Annotation on document - Omit + Disable") { Params = new object[] { typeof(XDocument), "Simple.xml", SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting }, Priority = 1 } });
                    this.AddChild(new TestVariation(XDocAnnotation) { Attribute = new VariationAttribute("Annotation on element - Omit + Disable") { Params = new object[] { typeof(XElement), "Simple.xml", SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting }, Priority = 1 } });
                    this.AddChild(new TestVariation(XDocAnnotation) { Attribute = new VariationAttribute("Annotation on element - Omit") { Params = new object[] { typeof(XElement), "Simple.xml", SaveOptions.OmitDuplicateNamespaces }, Priority = 0 } });
                    this.AddChild(new TestVariation(XDocAnnotation) { Attribute = new VariationAttribute("Annotation on document - Omit") { Params = new object[] { typeof(XDocument), "Simple.xml", SaveOptions.OmitDuplicateNamespaces }, Priority = 0 } });
                    this.AddChild(new TestVariation(AnnotationOnParent1) { Attribute = new VariationAttribute("Annotation on the parent nodes, XDocument") { Params = new object[] { typeof(XDocument), "simple.xml" }, Priority = 0 } });
                    this.AddChild(new TestVariation(AnnotationOnParent1) { Attribute = new VariationAttribute("Annotation on the parent nodes, XElement") { Params = new object[] { typeof(XElement), "simple.xml" }, Priority = 0 } });
                    this.AddChild(new TestVariation(MultipleAnnotationsInTree) { Attribute = new VariationAttribute("Multiple annotations in the tree - both up - XDocument") { Param = typeof(XDocument), Priority = 0 } });
                    this.AddChild(new TestVariation(MultipleAnnotationsInTree) { Attribute = new VariationAttribute("Multiple annotations in the tree - both up - XElement") { Param = typeof(XElement), Priority = 0 } });
                    this.AddChild(new TestVariation(MultipleAnnotationsInTree2) { Attribute = new VariationAttribute("Multiple annotations in the tree - up/down - XDocument") { Param = typeof(XDocument), Priority = 0 } });
                    this.AddChild(new TestVariation(MultipleAnnotationsInTree2) { Attribute = new VariationAttribute("Multiple annotations in the tree - up/down - XElement") { Param = typeof(XElement), Priority = 0 } });
                    this.AddChild(new TestVariation(MultipleAnnotationsOnElement) { Attribute = new VariationAttribute("Multiple annotations on node - XElement") { Param = typeof(XElement), Priority = 0 } });
                    this.AddChild(new TestVariation(MultipleAnnotationsOnElement) { Attribute = new VariationAttribute("Multiple annotations on node - XDocument") { Param = typeof(XDocument), Priority = 0 } });
                    this.AddChild(new TestVariation(OnOtherNodesAttrs) { Attribute = new VariationAttribute("On other node types - attributes") { Param = typeof(XElement), Priority = 2 } });
                    this.AddChild(new TestVariation(SimulateVb1) { Attribute = new VariationAttribute("Simulate the VB behavior - Save") { Priority = 0 } });
                    this.AddChild(new TestVariation(SimulateVb2) { Attribute = new VariationAttribute("Simulate the VB behavior - Reader") { Priority = 0 } });
                    this.AddChild(new TestVariation(LocalOverride) { Attribute = new VariationAttribute("Local settings override annotation") { Priority = 0 } });
                    this.AddChild(new TestVariation(ReaderOptionsSmoke) { Attribute = new VariationAttribute("XElement - ReaderOptions.None") { Params = new object[] { typeof(XElement), "simple.xml", ReaderOptions.None }, Priority = 0 } });
                    this.AddChild(new TestVariation(ReaderOptionsSmoke) { Attribute = new VariationAttribute("XElement - ReaderOptions.OmitDuplicateNamespaces") { Params = new object[] { typeof(XElement), "simple.xml", ReaderOptions.OmitDuplicateNamespaces }, Priority = 0 } });
                    this.AddChild(new TestVariation(ReaderOptionsSmoke) { Attribute = new VariationAttribute("XDocument - ReaderOptions.None") { Params = new object[] { typeof(XDocument), "simple.xml", ReaderOptions.None }, Priority = 0 } });
                    this.AddChild(new TestVariation(ReaderOptionsSmoke) { Attribute = new VariationAttribute("XDocument - ReaderOptions.OmitDuplicateNamespaces") { Params = new object[] { typeof(XDocument), "simple.xml", ReaderOptions.OmitDuplicateNamespaces }, Priority = 0 } });
                }
            }
            public partial class NamespacehandlingSaveOptions : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+NamespacehandlingSaveOptions
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("2 levels down") { Params = new object[] { "<p:A xmlns:p='nsp'><B><p:C xmlns:p='nsp'/></B></p:A>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("2 levels down III.") { Params = new object[] { "<A xmlns:p='nsp'><B><p:C xmlns:p='nsp'/></B></A>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Siblings") { Params = new object[] { "<A xmlns:p='nsp'><p:B xmlns:p='nsp'/><C xmlns:p='nsp'/><p:C xmlns:p='nsp'/></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Xml namespace II.") { Params = new object[] { "<p:A xmlns:p='nsp'><p:B xmlns:p='nsp'><p:C xmlns:p='nsp' xmlns:xml='http://www.w3.org/XML/1998/namespace'/></p:B></p:A>" }, Priority = 3 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("1 level down II.") { Params = new object[] { "<A><p:B xmlns:p='nsp'><p:C xmlns:p='nsp'/></p:B></A>" }, Priority = 0 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("2 levels down II.") { Params = new object[] { "<p:A xmlns:p='nsp'><B><C xmlns:p='nsp'/></B></p:A>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Children") { Params = new object[] { "<A xmlns:p='nsp'><p:B xmlns:p='nsp'><C xmlns:p='nsp'><p:C xmlns:p='nsp'/></C></p:B></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Xml namespace I.") { Params = new object[] { "<A xmlns:xml='http://www.w3.org/XML/1998/namespace'/>" }, Priority = 3 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("1 level down") { Params = new object[] { "<p:A xmlns:p='nsp'><p:B xmlns:p='nsp'><p:C xmlns:p='nsp'/></p:B></p:A>" }, Priority = 0 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Xml namespace III.") { Params = new object[] { "<p:A xmlns:p='nsp'><p:B xmlns:xml='http://www.w3.org/XML/1998/namespace' xmlns:p='nsp'><p:C xmlns:p='nsp' xmlns:xml='http://www.w3.org/XML/1998/namespace'/></p:B></p:A>" }, Priority = 3 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Default namespaces") { Params = new object[] { "<A xmlns='nsp'><p:B xmlns:p='nsp'><C xmlns='nsp' /></p:B></A>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("Not used NS declarations") { Params = new object[] { "<A xmlns='nsp' xmlns:u='not-used'><p:B xmlns:p='nsp'><C xmlns:u='not-used' xmlns='nsp' /></p:B></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeSimple) { Attribute = new VariationAttribute("SameNS, different prefix") { Params = new object[] { "<p:A xmlns:p='nsp'><B xmlns:q='nsp'><p:C xmlns:p='nsp'/></B></p:A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(testFromTheRootNodeTricky) { Attribute = new VariationAttribute("Default ns parent autogenerated") { Priority = 1 } });
                    this.AddChild(new TestVariation(testFromChildNode1) { Attribute = new VariationAttribute("Not from root") { Priority = 1 } });
                    this.AddChild(new TestVariation(testFromChildNode2) { Attribute = new VariationAttribute("Not from root II.") { Priority = 1 } });
                    this.AddChild(new TestVariation(testFromChildNode3) { Attribute = new VariationAttribute("Not from root III.") { Priority = 2 } });
                    this.AddChild(new TestVariation(testFromChildNode4) { Attribute = new VariationAttribute("Not from root IV.") { Priority = 2 } });
                }
            }
            public partial class Writer_Settings : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+Writer_Settings
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(var_1) { Attribute = new VariationAttribute("XDocument: Settings before Close()") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_2) { Attribute = new VariationAttribute("XElement: Settings before Close()") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_3) { Attribute = new VariationAttribute("XDocument: Settings after Close()") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_4) { Attribute = new VariationAttribute("XElement: Settings after Close()") { Priority = 1 } });
                }
            }
            public partial class TCCheckChars : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCCheckChars
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(checkChars_1) { Attribute = new VariationAttribute("CheckChars=true, invalid XML test WriteEntityRef") { Param = "EntityRef", Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_1) { Attribute = new VariationAttribute("CheckChars=true, invalid XML test WriteWhitespace") { Param = "Whitespace", Id = 3, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_1) { Attribute = new VariationAttribute("CheckChars=true, invalid name chars WriteDocType(name)") { Param = "WriteDocTypeName", Id = 4, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_1) { Attribute = new VariationAttribute("CheckChars=true, invalid XML test WriteSurrogateCharEntity") { Param = "SurrogateCharEntity", Id = 2, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_4) { Attribute = new VariationAttribute("CheckChars=true, invalid XML characters in WriteEntityRef should error") { Params = new object[] { "EntityRef", true }, Id = 46, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_4) { Attribute = new VariationAttribute("CheckChars=false, invalid XML characters in WriteSurrogateCharEntity should error") { Params = new object[] { "Surrogate", false }, Id = 41, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_4) { Attribute = new VariationAttribute("CheckChars=true, invalid XML characters in WriteQualifiedName should error") { Params = new object[] { "QName", true }, Id = 47, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_4) { Attribute = new VariationAttribute("CheckChars=true, invalid XML characters in WriteSurrogateCharEntity should error") { Params = new object[] { "Surrogate", true }, Id = 45, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_4) { Attribute = new VariationAttribute("CheckChars=false, invalid XML characters in WriteWhitespace should error") { Params = new object[] { "Whitespace", false }, Id = 40, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_4) { Attribute = new VariationAttribute("CheckChars=false, invalid XML characters in WriteEntityRef should error") { Params = new object[] { "EntityRef", false }, Id = 42, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_4) { Attribute = new VariationAttribute("CheckChars=false, invalid XML characters in WriteQualifiedName should error") { Params = new object[] { "QName", false }, Id = 43, Priority = 1 } });
                    this.AddChild(new TestVariation(checkChars_4) { Attribute = new VariationAttribute("CheckChars=true, invalid XML characters in WriteWhitespace should error") { Params = new object[] { "Whitespace", true }, Id = 44, Priority = 1 } });
                }
            }
            public partial class TCNewLineHandling : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCNewLineHandling
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(NewLineHandling_7) { Attribute = new VariationAttribute("Test for CR (xD) inside attr when NewLineHandling = Replace") { Id = 7, Priority = 0 } });
                    this.AddChild(new TestVariation(NewLineHandling_8) { Attribute = new VariationAttribute("Test for LF (xA) inside attr when NewLineHandling = Replace") { Id = 8, Priority = 0 } });
                    this.AddChild(new TestVariation(NewLineHandling_9) { Attribute = new VariationAttribute("Test for CR LF (xD xA) inside attr when NewLineHandling = Replace") { Id = 9, Priority = 0 } });
                    this.AddChild(new TestVariation(NewLineHandling_10) { Attribute = new VariationAttribute("Test for CR (xD) inside attr when NewLineHandling = Entitize") { Id = 10, Priority = 0 } });
                    this.AddChild(new TestVariation(NewLineHandling_11) { Attribute = new VariationAttribute("Test for LF (xA) inside attr when NewLineHandling = Entitize") { Id = 11, Priority = 0 } });
                    this.AddChild(new TestVariation(NewLineHandling_12) { Attribute = new VariationAttribute("Test for CR LF (xD xA) inside attr when NewLineHandling = Entitize") { Id = 12, Priority = 0 } });
                    this.AddChild(new TestVariation(NewLineHandling_13) { Attribute = new VariationAttribute("Factory-created writers do not entitize 0xD character in text content when NewLineHandling=Entitize") { Id = 13 } });
                }
            }
            public partial class TCIndent : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCIndent
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(indent_1) { Attribute = new VariationAttribute("Simple test when false") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(indent_2) { Attribute = new VariationAttribute("Simple test when true") { Id = 2, Priority = 0 } });
                    this.AddChild(new TestVariation(indent_3) { Attribute = new VariationAttribute("Indent = false, element content is empty") { Id = 3, Priority = 0 } });
                    this.AddChild(new TestVariation(indent_5) { Attribute = new VariationAttribute("Indent = false, element content is empty, FullEndElement") { Id = 5, Priority = 0 } });
                    this.AddChild(new TestVariation(indent_6) { Attribute = new VariationAttribute("Indent = true, element content is empty, FullEndElement") { Id = 6, Priority = 0 } });
                    this.AddChild(new TestVariation(indent_7) { Attribute = new VariationAttribute("Indent = true, mixed content") { Id = 7, Priority = 0 } });
                    this.AddChild(new TestVariation(indent_8) { Attribute = new VariationAttribute("Indent = true, mixed content, FullEndElement") { Id = 8, Priority = 0 } });
                }
            }
            public partial class TCNewLineOnAttributes : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCNewLineOnAttributes
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(NewLineOnAttributes_1) { Attribute = new VariationAttribute("Make sure the setting has no effect when Indent is false") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(NewLineOnAttributes_3) { Attribute = new VariationAttribute("Attributes of nested elements") { Id = 3, Priority = 1 } });
                }
            }
            public partial class TCStandAlone : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCStandAlone
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(standalone_1) { Attribute = new VariationAttribute("StartDocument(bool standalone = true)") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(standalone_2) { Attribute = new VariationAttribute("StartDocument(bool standalone = false)") { Id = 2, Priority = 0 } });
                }
            }
            public partial class TCFragmentCL : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCFragmentCL
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(frag_1) { Attribute = new VariationAttribute("WriteDocType should error when CL=fragment") { Id = 1, Priority = 1 } });
                    this.AddChild(new TestVariation(frag_2) { Attribute = new VariationAttribute("WriteStartDocument() should error when CL=fragment") { Id = 2, Priority = 1 } });
                }
            }
            public partial class TCAutoCL : BridgeHelpers
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+XNodeBuilderTests+TCAutoCL
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(auto_1) { Attribute = new VariationAttribute("Change to CL Document after WriteStartDocument()") { Id = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(auto_2) { Attribute = new VariationAttribute("Change to CL Document after WriteStartDocument(standalone = true)") { Param = "true", Id = 2, Priority = 0 } });
                    this.AddChild(new TestVariation(auto_2) { Attribute = new VariationAttribute("Change to CL Document after WriteStartDocument(standalone = false)") { Param = "false", Id = 3, Priority = 0 } });
                    this.AddChild(new TestVariation(auto_3) { Attribute = new VariationAttribute("Change to CL Document when you write DocType decl") { Id = 4, Priority = 0 } });
                    this.AddChild(new TestVariation(auto_4) { Attribute = new VariationAttribute("Change to CL Fragment when you write a root element") { Id = 5, Priority = 1 } });
                    this.AddChild(new TestVariation(auto_5) { Attribute = new VariationAttribute("Change to CL Fragment for WriteCData at top level") { Param = "CData", Id = 7, Priority = 1 } });
                    this.AddChild(new TestVariation(auto_5) { Attribute = new VariationAttribute("Change to CL Fragment for WriteCharEntity at top level") { Param = "CharEntity", Id = 9, Priority = 1 } });
                    this.AddChild(new TestVariation(auto_5) { Attribute = new VariationAttribute("Change to CL Fragment for WriteSurrogateCharEntity at top level") { Param = "SurrogateCharEntity", Id = 10, Priority = 1 } });
                    this.AddChild(new TestVariation(auto_5) { Attribute = new VariationAttribute("Change to CL Fragment for WriteString at top level") { Param = "String", Id = 6, Priority = 1 } });
                    this.AddChild(new TestVariation(auto_5) { Attribute = new VariationAttribute("Change to CL Fragment for WriteRaw at top level") { Param = "Raw", Id = 12, Priority = 1 } });
                    this.AddChild(new TestVariation(auto_5) { Attribute = new VariationAttribute("Change to CL Fragment for WriteBinHex at top level") { Param = "BinHex", Id = 14, Priority = 1 } });
                    this.AddChild(new TestVariation(auto_5) { Attribute = new VariationAttribute("Change to CL Fragment for WriteChars at top level") { Param = "Chars", Id = 11, Priority = 1 } });
                    this.AddChild(new TestVariation(auto_6) { Attribute = new VariationAttribute("WritePI at top level, followed by DTD, expected CL = Document") { Param = "PI", Id = 15, Priority = 2 } });
                    this.AddChild(new TestVariation(auto_6) { Attribute = new VariationAttribute("WriteWhitespace at top level, followed by DTD, expected CL = Document") { Param = "WS", Id = 17, Priority = 2 } });
                    this.AddChild(new TestVariation(auto_6) { Attribute = new VariationAttribute("WriteComment at top level, followed by DTD, expected CL = Document") { Param = "Comment", Id = 16, Priority = 2 } });
                    this.AddChild(new TestVariation(auto_7) { Attribute = new VariationAttribute("WriteComment at top level, followed by text, expected CL = Fragment") { Param = "Comment", Id = 19, Priority = 2 } });
                    this.AddChild(new TestVariation(auto_7) { Attribute = new VariationAttribute("WriteWhitespace at top level, followed by text, expected CL = Fragment") { Param = "WS", Id = 20, Priority = 2 } });
                    this.AddChild(new TestVariation(auto_7) { Attribute = new VariationAttribute("WritePI at top level, followed by text, expected CL = Fragment") { Param = "PI", Id = 18, Priority = 2 } });
                    this.AddChild(new TestVariation(auto_8) { Attribute = new VariationAttribute("WriteNode(XmlReader) when reader positioned on DocType node, expected CL = Document") { Id = 21, Priority = 2 } });
                    this.AddChild(new TestVariation(auto_10) { Attribute = new VariationAttribute("WriteNode(XmlReader) when reader positioned on text node, expected CL = Fragment") { Id = 22, Priority = 2 } });
                }
            }
        }
        #endregion
    }
}
