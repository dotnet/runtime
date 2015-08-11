// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using System.Text;
using Microsoft.Test.ModuleCore;
using System.Xml.Linq;
using XmlCoreTest.Common;
using System.IO;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests
        [Fact]
        [OuterLoop]
        public static void RunTests()
        {
            TestInput.CommandLine = "";
            FunctionalTests module = new FunctionalTests();
            module.Init();

            //for class PropertiesTests
            {
                module.AddChild(new PropertiesTests() { Attribute = new TestCaseAttribute() { Name = "Properties", Desc = "XLinq Properties Tests" } });
            }
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }
        #region Class
        public partial class PropertiesTests : XLinqTestCase
        {
            // Type is CoreXml.Test.XLinq.FunctionalTests+PropertiesTests
            // Test Case
            public override void AddChildren()
            {
                this.AddChild(new DeepEqualsTests() { Attribute = new TestCaseAttribute() { Name = "DeepEquals tests" } });
                this.AddChild(new DocOrderComparer() { Attribute = new TestCaseAttribute() { Name = "XNode.DocumentOrderComparer" } });
                this.AddChild(new XElement_Op_Eplicit() { Attribute = new TestCaseAttribute() { Name = "XAttribute - XmlConvert conformance      (SetValue)", Params = new object[] { typeof(XAttribute), ExplicitCastTestType.XmlConvert, NodeCreateType.SetValue } } });
                this.AddChild(new XElement_Op_Eplicit() { Attribute = new TestCaseAttribute() { Name = "XElement - XmlConvert conformance        (constructor)", Params = new object[] { typeof(XElement), ExplicitCastTestType.XmlConvert, NodeCreateType.Constructor } } });
                this.AddChild(new XElement_Op_Eplicit() { Attribute = new TestCaseAttribute() { Name = "XElement - value conversion round trip   (constructor)", Params = new object[] { typeof(XElement), ExplicitCastTestType.RoundTrip, NodeCreateType.Constructor } } });
                this.AddChild(new XElement_Op_Eplicit() { Attribute = new TestCaseAttribute() { Name = "XAttribute - value conversion round trip (constructor)", Params = new object[] { typeof(XAttribute), ExplicitCastTestType.RoundTrip, NodeCreateType.Constructor } } });
                this.AddChild(new XElement_Op_Eplicit() { Attribute = new TestCaseAttribute() { Name = "XElement - value conversion round trip   (SetValue)", Params = new object[] { typeof(XElement), ExplicitCastTestType.RoundTrip, NodeCreateType.SetValue } } });
                this.AddChild(new XElement_Op_Eplicit() { Attribute = new TestCaseAttribute() { Name = "XElement - XmlConvert conformance        (SetValue)", Params = new object[] { typeof(XElement), ExplicitCastTestType.XmlConvert, NodeCreateType.SetValue } } });
                this.AddChild(new XElement_Op_Eplicit() { Attribute = new TestCaseAttribute() { Name = "XAttribute - XmlConvert conformance      (constructor)", Params = new object[] { typeof(XAttribute), ExplicitCastTestType.XmlConvert, NodeCreateType.Constructor } } });
                this.AddChild(new XElement_Op_Eplicit() { Attribute = new TestCaseAttribute() { Name = "XAttribute - value conversion round trip (SetValue)", Params = new object[] { typeof(XAttribute), ExplicitCastTestType.RoundTrip, NodeCreateType.SetValue } } });
                this.AddChild(new XElement_Op_Eplicit_Null() { Attribute = new TestCaseAttribute() { Name = "XElement - explicit cast, null", Params = new object[] { typeof(XElement) } } });
                this.AddChild(new XElement_Op_Eplicit_Null() { Attribute = new TestCaseAttribute() { Name = "XAttribute - explicit cast, null", Params = new object[] { typeof(XAttribute) } } });
                this.AddChild(new ImplicitConversionsElem() { Attribute = new TestCaseAttribute() { Name = "Implicit conversions - XElement" } });
                this.AddChild(new ILineInfoTests() { Attribute = new TestCaseAttribute() { Name = "IXmlLineInfo & BaseURI" } });
                this.AddChild(new NamespaceAccessors() { Attribute = new TestCaseAttribute() { Name = "XElement : Namespace accessors" } });
                this.AddChild(new XElementName() { Attribute = new TestCaseAttribute() { Name = "XElement.Name with Events", Params = new object[] { true } } });
                this.AddChild(new XElementName() { Attribute = new TestCaseAttribute() { Name = "XElement.Name", Params = new object[] { false } } });
                this.AddChild(new XElementValue() { Attribute = new TestCaseAttribute() { Name = "XElement.Value", Params = new object[] { false } } });
            }
            public partial class DeepEqualsTests : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+PropertiesTests+DeepEqualsTests
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(PI) { Attribute = new VariationAttribute("PI data=target, not the same") { Params = new object[] { "PA", "PA", "PI", "PI", false }, Priority = 2 } });
                    this.AddChild(new TestVariation(PI) { Attribute = new VariationAttribute("PI target1!=target2!") { Params = new object[] { "AAAAP", "click", "AAAAQ", "click", false }, Priority = 2 } });
                    this.AddChild(new TestVariation(PI) { Attribute = new VariationAttribute("PI hashconflict I.") { Params = new object[] { "AAAAP", "AAAAQ", "AAAAP", "AAAAQ", true }, Priority = 2 } });
                    this.AddChild(new TestVariation(PI) { Attribute = new VariationAttribute("PI data1!=data2") { Params = new object[] { "PI", "click", "PI", "", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(PI) { Attribute = new VariationAttribute("PI normal") { Params = new object[] { "PI", "click", "PI", "click", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(PI) { Attribute = new VariationAttribute("PI hashconflict II.") { Params = new object[] { "AAAAP", "AAAAQ", "AAAAQ", "AAAAP", false }, Priority = 2 } });
                    this.AddChild(new TestVariation(PI) { Attribute = new VariationAttribute("PI target=data") { Params = new object[] { "PI", "PI", "PI", "PI", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(PI) { Attribute = new VariationAttribute("PI data = ''") { Params = new object[] { "PI", "", "PI", "", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(Comment) { Attribute = new VariationAttribute("XComment - not equals, hashconflict") { Params = new object[] { "AAAAP", "AAAAQ", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(Comment) { Attribute = new VariationAttribute("XComment - equals") { Params = new object[] { "AAAAP", "AAAAP", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(Comment) { Attribute = new VariationAttribute("XComment - Whitespaces (negative)") { Params = new object[] { "  ", " ", false }, Priority = 3 } });
                    this.AddChild(new TestVariation(Comment) { Attribute = new VariationAttribute("XComment - Whitespaces") { Params = new object[] { " ", " ", true }, Priority = 3 } });
                    this.AddChild(new TestVariation(Comment) { Attribute = new VariationAttribute("XComment - Empty") { Params = new object[] { "", "", true }, Priority = 1 } });
                    this.AddChild(new TestVariation(DTD) { Attribute = new VariationAttribute("DTD (negative) : subset diff") { Params = new object[] { new string[] { "A", null, null, "aa" }, new string[] { "A", null, null, "bb" }, false }, Priority = 1 } });
                    this.AddChild(new TestVariation(DTD) { Attribute = new VariationAttribute("DTD : internal subset only") { Params = new object[] { new string[] { "root", null, null, "data" }, new string[] { "root", null, null, "data" }, true }, Priority = 2 } });
                    this.AddChild(new TestVariation(DTD) { Attribute = new VariationAttribute("DTD (negative) : name diff") { Params = new object[] { new string[] { "A", "", "", "" }, new string[] { "B", "", "", "" }, false }, Priority = 0 } });
                    this.AddChild(new TestVariation(DTD) { Attribute = new VariationAttribute("DTD (negative) : null vs. \"\"") { Params = new object[] { new string[] { "A", "", "", "" }, new string[] { "A", null, null, null }, false }, Priority = 2 } });
                    this.AddChild(new TestVariation(DTD) { Attribute = new VariationAttribute("DTD : all nulls") { Params = new object[] { new string[] { "root", null, null, null }, new string[] { "root", null, null, null }, true }, Priority = 1 } });
                    this.AddChild(new TestVariation(DTD) { Attribute = new VariationAttribute("DTD : all field") { Params = new object[] { new string[] { "root", "a", "b", "c" }, new string[] { "root", "a", "b", "c" }, true }, Priority = 0 } });
                    this.AddChild(new TestVariation(Text1) { Attribute = new VariationAttribute("XText - Whitespaces ()") { Params = new object[] { " ", " ", true }, Priority = 2 } });
                    this.AddChild(new TestVariation(Text1) { Attribute = new VariationAttribute("XText - Whitespaces (negative)") { Params = new object[] { "\n", " ", false }, Priority = 2 } });
                    this.AddChild(new TestVariation(Text1) { Attribute = new VariationAttribute("XText - same") { Params = new object[] { "same", "same", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(Text1) { Attribute = new VariationAttribute("XText - different") { Params = new object[] { "same", "different", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(Text1) { Attribute = new VariationAttribute("XText - Empty") { Params = new object[] { "", "", true }, Priority = 2 } });
                    this.AddChild(new TestVariation(CData) { Attribute = new VariationAttribute("XCData - Whitespaces (negative)") { Params = new object[] { "\n", " ", false }, Priority = 2 } });
                    this.AddChild(new TestVariation(CData) { Attribute = new VariationAttribute("XCData - same") { Params = new object[] { "same", "same", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(CData) { Attribute = new VariationAttribute("XCData - Empty") { Params = new object[] { "", "", true }, Priority = 2 } });
                    this.AddChild(new TestVariation(CData) { Attribute = new VariationAttribute("XCData - Whitespaces ()") { Params = new object[] { " ", " ", true }, Priority = 2 } });
                    this.AddChild(new TestVariation(CData) { Attribute = new VariationAttribute("XCData - different") { Params = new object[] { "same", "different", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(TextVsCData) { Attribute = new VariationAttribute("Xtext vs. XCData - Empty") { Params = new object[] { "", "", false }, Priority = 2 } });
                    this.AddChild(new TestVariation(TextVsCData) { Attribute = new VariationAttribute("Xtext vs. XCData - Whitespaces") { Params = new object[] { " ", " ", false }, Priority = 2 } });
                    this.AddChild(new TestVariation(TextVsCData) { Attribute = new VariationAttribute("Xtext vs. XCData - same") { Params = new object[] { "same", "same", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(TextWholeVsConcatenate) { Attribute = new VariationAttribute("XText do not concatenate inside") { Priority = 2 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - atributes (same, same order, different value)") { Params = new object[] { "<A at='1' Id='a'/>", "<A at='1' Id='ab'/>", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - atributes") { Params = new object[] { "<A Id='a'/>", "<A Id='a'/>", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - atributes (same, different order)") { Params = new object[] { "<A at='1' Id='a'/>", "<A Id='a' at='1'/>", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - String + PI content (negative)") { Params = new object[] { "<A>text<?PI click?></A>", "<A><?PI click?>text</A>", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - String + PI content") { Params = new object[] { "<A>text<?PI click?></A>", "<A>text<?PI click?></A>", true }, Priority = 1 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - smoke") { Params = new object[] { "<A/>", "<A></A>", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - atributes (same, same order)") { Params = new object[] { "<A at='1' Id='a'/>", "<A at='1' Id='a'/>", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - atributes (same, same order, namespace decl, different prefix)") { Params = new object[] { "<A p:at='1' xmlns:p='nsp'/>", "<A q:at='1' xmlns:q='nsp'/>", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - String content") { Params = new object[] { "<A>text</A>", "<A>text</A>", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - atributes (same, same order, namespace decl)") { Params = new object[] { "<A p:at='1' xmlns:p='nsp'/>", "<A p:at='1' xmlns:p='nsp'/>", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(Element) { Attribute = new VariationAttribute("XElement - atribute missing") { Params = new object[] { "<A/>", "<A Id='a'/>", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(Element2) { Attribute = new VariationAttribute("XElement - String content vs. text node vs. CData") { Priority = 0 } });
                    this.AddChild(new TestVariation(Element3) { Attribute = new VariationAttribute("XElement - text node concatenations") { Priority = 0 } });
                    this.AddChild(new TestVariation(Element6) { Attribute = new VariationAttribute("XElement - text node incarnation - by adding new node") { Param = 2, Priority = 0 } });
                    this.AddChild(new TestVariation(Element6) { Attribute = new VariationAttribute("XElement - text node incarnation - by touching") { Param = 1, Priority = 0 } });
                    this.AddChild(new TestVariation(Element4) { Attribute = new VariationAttribute("XElement - text node concatenations (negative)") { Priority = 2 } });
                    this.AddChild(new TestVariation(Element5) { Attribute = new VariationAttribute("XElement - namespace prefixes") { Params = new object[] { "<A xmlns='nsa'><B><!--comm--><C xmlns=''/></B></A>", "<A xmlns:p='nsa'><p:B><!--comm--><C xmlns=''/></p:B></A>", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(ElementDynamic) { Attribute = new VariationAttribute("XElement - dynamic") { Priority = 0 } });
                    this.AddChild(new TestVariation(Document1) { Attribute = new VariationAttribute("XDocument : dynamic") { Priority = 0 } });
                    this.AddChild(new TestVariation(Document4) { Attribute = new VariationAttribute("XDocument : DTD") { Param = true, Priority = 2 } });
                    this.AddChild(new TestVariation(Document4) { Attribute = new VariationAttribute("XDocument : DTD (negative)") { Param = false, Priority = 2 } });
                    this.AddChild(new TestVariation(Nulls) { Attribute = new VariationAttribute("Nulls") { Param = true, Priority = 2 } });
                }
            }
            public partial class DocOrderComparer : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+PropertiesTests+DocOrderComparer
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(ConnectedNodes) { Attribute = new VariationAttribute("Connected nodes") { Priority = 0 } });
                    this.AddChild(new TestVariation(StandAloneNodes) { Attribute = new VariationAttribute("Single standalone node (sanity)") { Priority = 3 } });
                    this.AddChild(new TestVariation(AdjacentTextNodes1) { Attribute = new VariationAttribute("Adjacent text nodes I. (sanity)") { Priority = 3 } });
                    this.AddChild(new TestVariation(AdjacentTextNodes2) { Attribute = new VariationAttribute("Adjacent text nodes II. (sanity)") { Priority = 3 } });
                    this.AddChild(new TestVariation(DisconnectedNodes1) { Attribute = new VariationAttribute("Disconnected nodes") { Priority = 2 } });
                    this.AddChild(new TestVariation(NotXNode) { Attribute = new VariationAttribute("Not XNode") { Priority = 2 } });
                    this.AddChild(new TestVariation(Nulls) { Attribute = new VariationAttribute("Nulls") { Priority = 2 } });
                }
            }
            public partial class XElement_Op_Eplicit : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+PropertiesTests+XElement_Op_Eplicit
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(SetValueNull) { Attribute = new VariationAttribute("XElement.SetValue(null)") { Param = null } });
                    this.AddChild(new TestVariation(SetValueNull) { Attribute = new VariationAttribute("XElement.SetValue(null)") { Param = "" } });
                    this.AddChild(new TestVariation(SetValueNull) { Attribute = new VariationAttribute("XElement.SetValue(null)") { Param = "text" } });
                    this.AddChild(new TestVariation(SetValueNull) { Attribute = new VariationAttribute("XElement.SetValue(null)") { Param = typeof(XElement) } });
                    this.AddChild(new TestVariation(SetValueNullAttr) { Attribute = new VariationAttribute("XAttribute.SetValue(null)") });
                    this.AddChild(new TestVariation(ConversionoBool) { Attribute = new VariationAttribute("Conversion to bool overloads (0,False,false)") { Params = new object[] { false, new string[] { "0", "False", "false", "FALSE", " FalsE " } } } });
                    this.AddChild(new TestVariation(ConversionoBool) { Attribute = new VariationAttribute("Conversion to bool overloads (1,True,true)") { Params = new object[] { true, new string[] { "1", "True", "true", "TRUE", " TRue " } } } });
                }
            }
            public partial class XElement_Op_Eplicit_Null : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+PropertiesTests+XElement_Op_Eplicit_Null
                // Test Case
                public override void AddChildren()
                {
                }
            }
            public partial class ImplicitConversionsElem : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+PropertiesTests+ImplicitConversionsElem
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(StringConvert) { Attribute = new VariationAttribute("String - concat from children") { Params = new object[] { "<A><B>text1<D><![CDATA[text2]]></D></B><C>text3</C></A>", "text1text2text3" }, Priority = 0 } });
                    this.AddChild(new TestVariation(StringConvert) { Attribute = new VariationAttribute("String - concat two text nodes") { Params = new object[] { "<A>text1<B/>text2</A>", "text1text2" }, Priority = 0 } });
                    this.AddChild(new TestVariation(StringConvert) { Attribute = new VariationAttribute("String - empty with CDATA") { Params = new object[] { "<A><B><D><![CDATA[]]></D></B><C></C></A>", "" }, Priority = 0 } });
                    this.AddChild(new TestVariation(StringConvert) { Attribute = new VariationAttribute("String - concat text and CData") { Params = new object[] { "<A>text1<B/><![CDATA[text2]]></A>", "text1text2" }, Priority = 0 } });
                    this.AddChild(new TestVariation(StringConvert) { Attribute = new VariationAttribute("String - empty") { Params = new object[] { "<A><B><D></D></B><C></C></A>", "" }, Priority = 0 } });
                    this.AddChild(new TestVariation(StringConvert) { Attribute = new VariationAttribute("String - direct") { Params = new object[] { "<A>text</A>", "text" }, Priority = 0 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - concat from children - true") { Params = new object[] { "<A><B>t<D><![CDATA[ru]]></D></B><C>e</C></A>", true }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - direct - 1") { Params = new object[] { "<A>1</A>", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - direct - false") { Params = new object[] { "<A>false</A>", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - direct - 0 ") { Params = new object[] { "<A>0</A>", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - concat two text nodes - true") { Params = new object[] { "<A>tr<B/>ue</A>", true }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - concat two text nodes - false") { Params = new object[] { "<A>f<B/>alse</A>", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - concat text and CData - true") { Params = new object[] { "<A>tru<B/><![CDATA[e]]></A>", true }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - concat text and CData - false") { Params = new object[] { "<A>fal<B/><![CDATA[se]]></A>", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - direct - true") { Params = new object[] { "<A>true</A>", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - concat from children - 1") { Params = new object[] { "<A> <B><D><![CDATA[1]]></D></B><C> </C></A>", true }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - concat from children - false") { Params = new object[] { "<A><B>fa<D><![CDATA[l]]></D></B><C>se</C></A>", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolConvert) { Attribute = new VariationAttribute("bool - concat from children - 0") { Params = new object[] { "<A><B> <D><![CDATA[ ]]></D></B><C>0</C></A>", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolConvertInvalid) { Attribute = new VariationAttribute("bool - Invalid - empty") { Params = new object[] { "<A></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(BoolConvertInvalid) { Attribute = new VariationAttribute("bool - Invalid - capital T") { Params = new object[] { "<A>True</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(BoolConvertInvalid) { Attribute = new VariationAttribute("bool? - Invalid - some other") { Params = new object[] { "<A>2</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - concat from children - 1") { Params = new object[] { "<A> <B><D><![CDATA[1]]></D></B><C> </C></A>", true }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - direct - 0 ") { Params = new object[] { "<A>0</A>", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - concat two text nodes - true") { Params = new object[] { "<A>tr<B/>ue</A>", true }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - concat two text nodes - false") { Params = new object[] { "<A>f<B/>alse</A>", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - concat text and CData - true") { Params = new object[] { "<A>tru<B/><![CDATA[e]]></A>", true }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - concat text and CData - false") { Params = new object[] { "<A>fal<B/><![CDATA[se]]></A>", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - concat from children - true") { Params = new object[] { "<A><B>t<D><![CDATA[ru]]></D></B><C>e</C></A>", true }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - concat from children - false") { Params = new object[] { "<A><B>fa<D><![CDATA[l]]></D></B><C>se</C></A>", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - concat from children - 0") { Params = new object[] { "<A><B> <D><![CDATA[ ]]></D></B><C>0</C></A>", false }, Priority = 1 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - direct - true") { Params = new object[] { "<A>true</A>", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - direct - 1") { Params = new object[] { "<A>1</A>", true }, Priority = 0 } });
                    this.AddChild(new TestVariation(BoolQConvert) { Attribute = new VariationAttribute("bool? - direct - false") { Params = new object[] { "<A>false</A>", false }, Priority = 0 } });
                    this.AddChild(new TestVariation(BoolQConvertInvalid) { Attribute = new VariationAttribute("bool? - Invalid - capital T") { Params = new object[] { "<A a='1'>True</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(BoolQConvertInvalid) { Attribute = new VariationAttribute("bool? - Invalid - space inside") { Params = new object[] { "<A a='1'>tr<B/> ue</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(BoolQConvertInvalid) { Attribute = new VariationAttribute("bool? - Invalid - some other") { Params = new object[] { "<A a='1'>2</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(BoolQConvertInvalid) { Attribute = new VariationAttribute("bool? - Invalid - empty") { Params = new object[] { "<A a='1'></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(BoolQConvertNull) { Attribute = new VariationAttribute("bool? - Null") { Priority = 2 } });
                    this.AddChild(new TestVariation(IntConvert) { Attribute = new VariationAttribute("int - concat text and CData") { Params = new object[] { "<A a='1'>-<B/><![CDATA[21]]></A>", -21 }, Priority = 0 } });
                    this.AddChild(new TestVariation(IntConvert) { Attribute = new VariationAttribute("int - concat two text nodes") { Params = new object[] { "<A a='1'>1<B/>7</A>", 17 }, Priority = 0 } });
                    this.AddChild(new TestVariation(IntConvert) { Attribute = new VariationAttribute("int - direct") { Params = new object[] { "<A a='1'>10</A>", 10 }, Priority = 0 } });
                    this.AddChild(new TestVariation(IntConvert) { Attribute = new VariationAttribute("int - concat from children") { Params = new object[] { "<A a='1'><B>-<D><![CDATA[12]]></D></B><C>0</C></A>", -120 }, Priority = 0 } });
                    this.AddChild(new TestVariation(IntConvertInvalid) { Attribute = new VariationAttribute("int - Invalid - some other") { Params = new object[] { "<A a='1'>X</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(IntConvertInvalid) { Attribute = new VariationAttribute("int - Invalid - space inside") { Params = new object[] { "<A a='1'>2<B/> 1</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(IntConvertInvalid) { Attribute = new VariationAttribute("int - Invalid - empty") { Params = new object[] { "<A a='1'></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(IntQConvert) { Attribute = new VariationAttribute("int? - concat from children") { Params = new object[] { "<A a='1'><B>-<D><![CDATA[12]]></D></B><C>0</C></A>", -120 }, Priority = 0 } });
                    this.AddChild(new TestVariation(IntQConvert) { Attribute = new VariationAttribute("int? - direct") { Params = new object[] { "<A a='1'>10</A>", 10 }, Priority = 0 } });
                    this.AddChild(new TestVariation(IntQConvert) { Attribute = new VariationAttribute("int? - concat two text nodes") { Params = new object[] { "<A a='1'>1<B/>7</A>", 17 }, Priority = 0 } });
                    this.AddChild(new TestVariation(IntQConvert) { Attribute = new VariationAttribute("int? - concat text and CData") { Params = new object[] { "<A a='1'>-<B/><![CDATA[21]]></A>", -21 }, Priority = 0 } });
                    this.AddChild(new TestVariation(IntQConvertInvalid) { Attribute = new VariationAttribute("int? - Invalid - space inside") { Params = new object[] { "<A a='1'>2<B/> 1</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(IntQConvertInvalid) { Attribute = new VariationAttribute("int? - Invalid - empty") { Params = new object[] { "<A a='1'></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(IntQConvertInvalid) { Attribute = new VariationAttribute("int? - Invalid - some other") { Params = new object[] { "<A a='1'>X</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(IntQConvertNull) { Attribute = new VariationAttribute("int? - Null") { Priority = 2 } });
                    this.AddChild(new TestVariation(UIntConvert) { Attribute = new VariationAttribute("uint - concat two text nodes") { Params = new object[] { "<A a='1'>1<B/>7</A>", 17 }, Priority = 0 } });
                    this.AddChild(new TestVariation(UIntConvert) { Attribute = new VariationAttribute("uint - concat from children") { Params = new object[] { "<A a='1'><B><D><![CDATA[12]]></D></B><C>0</C></A>", 120 }, Priority = 0 } });
                    this.AddChild(new TestVariation(UIntConvert) { Attribute = new VariationAttribute("uint - direct") { Params = new object[] { "<A a='1'>10</A>", 10 }, Priority = 0 } });
                    this.AddChild(new TestVariation(UIntConvert) { Attribute = new VariationAttribute("uint - concat text and CData") { Params = new object[] { "<A a='1'><B/><![CDATA[21]]></A>", 21 }, Priority = 0 } });
                    this.AddChild(new TestVariation(UIntConvertInvalid) { Attribute = new VariationAttribute("uint - Invalid - empty") { Params = new object[] { "<A a='1'></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(UIntConvertInvalid) { Attribute = new VariationAttribute("uint - Invalid - space inside") { Params = new object[] { "<A a='1'>2<B/> 1</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(UIntConvertInvalid) { Attribute = new VariationAttribute("uint - Invalid - negative") { Params = new object[] { "<A a='1'>-<B/>1</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(UIntConvertInvalid) { Attribute = new VariationAttribute("uint - Invalid - some other") { Params = new object[] { "<A a='1'>X</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(UIntQConvert) { Attribute = new VariationAttribute("uint? - direct") { Params = new object[] { "<A a='1'>10</A>", 10 }, Priority = 0 } });
                    this.AddChild(new TestVariation(UIntQConvert) { Attribute = new VariationAttribute("uint? - concat from children") { Params = new object[] { "<A a='1'><B><D><![CDATA[12]]></D></B><C>0</C></A>", 120 }, Priority = 0 } });
                    this.AddChild(new TestVariation(UIntQConvert) { Attribute = new VariationAttribute("uint? - concat two text nodes") { Params = new object[] { "<A a='1'>1<B/>7</A>", 17 }, Priority = 0 } });
                    this.AddChild(new TestVariation(UIntQConvert) { Attribute = new VariationAttribute("uint? - concat text and CData") { Params = new object[] { "<A a='1'><B/><![CDATA[21]]></A>", 21 }, Priority = 0 } });
                    this.AddChild(new TestVariation(UIntQConvertInvalid) { Attribute = new VariationAttribute("uint? - Invalid - some other") { Params = new object[] { "<A a='1'>X</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(UIntQConvertInvalid) { Attribute = new VariationAttribute("uint? - Invalid - negative") { Params = new object[] { "<A a='1'>-<B/>1</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(UIntQConvertInvalid) { Attribute = new VariationAttribute("uint? - Invalid - space inside") { Params = new object[] { "<A a='1'>2<B/> 1</A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(UIntQConvertInvalid) { Attribute = new VariationAttribute("uint? - Invalid - empty") { Params = new object[] { "<A a='1'></A>" }, Priority = 2 } });
                    this.AddChild(new TestVariation(UIntQConvertNull) { Attribute = new VariationAttribute("uint? - Null") { Priority = 2 } });
                }
            }
            public partial class ILineInfoTests : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+PropertiesTests+ILineInfoTests
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(CastToInterface) { Attribute = new VariationAttribute("Cast to Interface") { Priority = 0 } });
                    this.AddChild(new TestVariation(BaseUriInitial) { Attribute = new VariationAttribute("XDocument/XElement - BaseUri, Reader in Initial state") { Param = Path.Combine("TestData", "XLinq", "config.xml"), Priority = 0 } });
                    this.AddChild(new TestVariation(AllNodesTests) { Attribute = new VariationAttribute("XElement - BaseUri, Reader in Initial state, all nodes") { Param = Path.Combine("TestData", "XLinq", "IXmlLineInfoTests", "company-data.xml"), Priority = 0 } });
                }
            }
            public partial class NamespaceAccessors : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+PropertiesTests+NamespaceAccessors
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(GetDefaultNamespace) { Attribute = new VariationAttribute("GetDefaultNamespace - local def") { Params = new object[] { "<A><B xmlns='nsa'/></A>", "nsa" }, Priority = 0 } });
                    this.AddChild(new TestVariation(GetDefaultNamespace) { Attribute = new VariationAttribute("GetDefaultNamespace - undefined") { Params = new object[] { "<A xmlns='nsb'><B xmlns=''/></A>", "" }, Priority = 2 } });
                    this.AddChild(new TestVariation(GetDefaultNamespace) { Attribute = new VariationAttribute("GetDefaultNamespace - no default ns II") { Params = new object[] { "<A><C xmlns='nsc'/><B/></A>", "" }, Priority = 1 } });
                    this.AddChild(new TestVariation(GetDefaultNamespace) { Attribute = new VariationAttribute("GetDefaultNamespace - no default ns") { Params = new object[] { "<A><B/></A>", "" }, Priority = 1 } });
                    this.AddChild(new TestVariation(GetDefaultNamespace) { Attribute = new VariationAttribute("GetDefaultNamespace - in scope") { Params = new object[] { "<A xmlns='nsa'><B/></A>", "nsa" }, Priority = 0 } });
                    this.AddChild(new TestVariation(GetDefaultNamespace) { Attribute = new VariationAttribute("GetDefaultNamespace - redefined") { Params = new object[] { "<A xmlns='nsb'><B xmlns='nsa'/></A>", "nsa" }, Priority = 2 } });
                    this.AddChild(new TestVariation(GetDefaultNamespaceDMLSanity) { Attribute = new VariationAttribute("GetDefaultNamespace - DML Sanity") { Priority = 0 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - case sensitive") { Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", "P", null }, Priority = 0 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - in scope, not used I.") { Params = new object[] { "<A xmlns:p='nsc'><C xmlns:a='a'/><B/></A>", "p", "nsc" }, Priority = 1 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - in scope, not used II.") { Params = new object[] { "<p:A xmlns:p='nsc' xmlns:a='a'><C/><B/></p:A>", "p", "nsc" }, Priority = 1 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - redefinition, used") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc" }, Priority = 1 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - in scope, used") { Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S/></p:C><B/></A>", "p", "nsc" }, Priority = 0 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - nonexisting") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "X", null }, Priority = 2 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - xml") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xml", "http://www.w3.org/XML/1998/namespace" }, Priority = 2 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - xmlns") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xmlns", "http://www.w3.org/2000/xmlns/" }, Priority = 2 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - nonexisting in context") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", "X", null }, Priority = 2 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - redefinition, not used") { Params = new object[] { "<a:A xmlns:a='a' xmlns:p='redefme'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc" }, Priority = 1 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - local, used") { Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc" }, Priority = 0 } });
                    this.AddChild(new TestVariation(NamespaceForPrefix) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - local, not used") { Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc" }, Priority = 1 } });
                    this.AddChild(new TestVariation(NamespaceForPrefixNull) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - Null") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", null, null }, Priority = 2 } });
                    this.AddChild(new TestVariation(NamespaceForPrefixNull) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - Empty string") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", "", null }, Priority = 2 } });
                    this.AddChild(new TestVariation(NamespaceForPrefixDMLSanity) { Attribute = new VariationAttribute("GetNamespaceOfPrefix - DML Sanity") { Priority = 0 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - local, not used") { Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc" }, Priority = 1 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - case sensitive") { Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", null, "NSC" }, Priority = 0 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - in scope, used") { Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S/></p:C><B/></A>", "p", "nsc" }, Priority = 0 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - in scope, not used I.") { Params = new object[] { "<A xmlns:p='nsc'><C xmlns:a='a'/><B/></A>", "p", "nsc" }, Priority = 1 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - in scope, not used II.") { Params = new object[] { "<p:A xmlns:p='nsc' xmlns:a='a'><C/><B/></p:A>", "p", "nsc" }, Priority = 1 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - redefinition, used") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc" }, Priority = 1 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - redefinition, not used") { Params = new object[] { "<a:A xmlns:a='a' xmlns:p='redefme'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc" }, Priority = 1 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - default namespace") { Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", null, "x" }, Priority = 2 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - blank") { Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", null, "" }, Priority = 2 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - nonexisting") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", null, "nonexisting" }, Priority = 2 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - xml") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xml", "http://www.w3.org/XML/1998/namespace" }, Priority = 2 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - xmlns") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xmlns", "http://www.w3.org/2000/xmlns/" }, Priority = 2 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - local, used") { Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc" }, Priority = 0 } });
                    this.AddChild(new TestVariation(PrefixOfNamespace) { Attribute = new VariationAttribute("GetPrefixOfNamespace - nonexisting in context") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", null, "nonexisting" }, Priority = 2 } });
                    this.AddChild(new TestVariation(PrefixOfNamespaceNull) { Attribute = new VariationAttribute("GetPrefixOfNamespace - Null") { Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", null, null }, Priority = 2 } });
                    this.AddChild(new TestVariation(PrefixOfNamespaceDMLSanity) { Attribute = new VariationAttribute("GetPrefixOfNamespace - DML Sanity") { Priority = 0 } });
                }
            }
            public partial class XElementName : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+PropertiesTests+XElementName
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(ValidVariation) { Attribute = new VariationAttribute("XElement - same name") { Params = new object[] { "<element>value</element>", "element" }, Priority = 0 } });
                    this.AddChild(new TestVariation(ValidVariation) { Attribute = new VariationAttribute("XElement - name with namespace") { Params = new object[] { "<element>value</element>", "{a}newElement" }, Priority = 0 } });
                    this.AddChild(new TestVariation(ValidVariation) { Attribute = new VariationAttribute("XElement - name with xml namespace") { Params = new object[] { "<element>value</element>", "{http://www.w3.org/XML/1998/namespace}newElement" }, Priority = 0 } });
                    this.AddChild(new TestVariation(ValidVariation) { Attribute = new VariationAttribute("XElement - element with namespace") { Params = new object[] { "<p:element xmlns:p='mynamespace'><p:child>value</p:child></p:element>", "{a}newElement" }, Priority = 0 } });
                    this.AddChild(new TestVariation(ValidVariation) { Attribute = new VariationAttribute("XElement - different name") { Params = new object[] { "<element>value</element>", "newElement" }, Priority = 0 } });
                    this.AddChild(new TestVariation(InValidVariation) { Attribute = new VariationAttribute("XElement - empty string name") { Params = new object[] { "<element>value</element>", "" }, Priority = 0 } });
                    this.AddChild(new TestVariation(InValidVariation) { Attribute = new VariationAttribute("XElement - null name") { Params = new object[] { "<element>value</element>", null }, Priority = 0 } });
                    this.AddChild(new TestVariation(InValidVariation) { Attribute = new VariationAttribute("XElement - space character name") { Params = new object[] { "<element>value</element>", " " }, Priority = 0 } });
                    this.AddChild(new TestVariation(ValidPIVariation) { Attribute = new VariationAttribute("XProcessingInstruction - Valid Name") { Priority = 0 } });
                    this.AddChild(new TestVariation(InvalidPIVariation) { Attribute = new VariationAttribute("XProcessingInstruction - Invalid Name") { Priority = 0 } });
                    this.AddChild(new TestVariation(ValidDocTypeVariation) { Attribute = new VariationAttribute("XDocumentType - Valid Name") { Priority = 0 } });
                    this.AddChild(new TestVariation(InvalidDocTypeVariation) { Attribute = new VariationAttribute("XDocumentType - Invalid Name") { Priority = 0 } });
                }
            }
            public partial class XElementValue : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+PropertiesTests+XElementValue
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(SmokeTest) { Attribute = new VariationAttribute("GET: String content") { Params = new object[] { "<X>t0<A>truck</A>t00</X>" }, Priority = 0 } });
                    this.AddChild(new TestVariation(SmokeTest) { Attribute = new VariationAttribute("GET: Empty string node") { Params = new object[] { "<X>t0<A/>t00</X>" }, Priority = 0 } });
                    this.AddChild(new TestVariation(SmokeTest) { Attribute = new VariationAttribute("GET: Mixed content") { Params = new object[] { "<X>t0<A>t1<B/><B xmlns='a'><![CDATA[t2]]></B><C>t3</C></A>t00</X>" }, Priority = 0 } });
                    this.AddChild(new TestVariation(SmokeTest) { Attribute = new VariationAttribute("GET: Mixed content - empty CDATA") { Params = new object[] { "<X>t0<A>t1<B/><B xmlns='a'><![CDATA[]]></B><C>t3</C></A>t00</X>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(SmokeTest) { Attribute = new VariationAttribute("GET: Mixed content - empty XText") { Params = new object[] { "<X>t0<A>t1<B/><B xmlns='a'><![CDATA[]]></B><C></C></A>t00</X>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(SmokeTest) { Attribute = new VariationAttribute("GET: Mixed content - whitespaces") { Params = new object[] { "<X>t0<A>t1\n<B/><B xmlns='a'>\t<![CDATA[]]> </B>\n<C></C></A>t00</X>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(SmokeTest) { Attribute = new VariationAttribute("GET: Mixed content - no text nodes") { Params = new object[] { "<X>t0<A Id='a0'><B/><B xmlns='a'><?Pi c?></B><!--commm--><C></C></A>t00</X>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(SmokeTest) { Attribute = new VariationAttribute("GET: Empty string node") { Params = new object[] { "<X>t0<A></A>t00</X>" }, Priority = 0 } });
                    this.AddChild(new TestVariation(APIModified1) { Attribute = new VariationAttribute("GET: Adjacent text nodes II.") { Params = new object[] { "<X>t0<A xmlns:p='p'>truck<p:Y/></A>t00</X>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(APIModified1) { Attribute = new VariationAttribute("GET: Adjacent text nodes I.") { Params = new object[] { "<X>t0<A>truck</A>t00</X>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(APIModified2) { Attribute = new VariationAttribute("GET: Adjacent text nodes III.") { Params = new object[] { "<X>t0<A xmlns:p='p'>truck\n<p:Y/>\nhello</A>t00</X>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(APIModified3) { Attribute = new VariationAttribute("GET: Concatenated text I.") { Params = new object[] { "<X>t0<A>truck</A>t00</X>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(APIModified3) { Attribute = new VariationAttribute("GET: Concatenated text II.") { Params = new object[] { "<X>t0<A xmlns:p='p'>truck<p:Y/></A>t00</X>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(APIModified4) { Attribute = new VariationAttribute("GET: Removed node.") { Params = new object[] { "<X>t0<A xmlns:p='p'>truck\n<p:Y/>\nhello</A>t00</X>" }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set) { Attribute = new VariationAttribute("SET: String content, Empty string content") { Params = new object[] { "<X>t0<A>orig</A>t00</X>", "" }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set) { Attribute = new VariationAttribute("SET: Empty element, String content") { Params = new object[] { "<X>t0<A/>t00</X>", "\nt1 " }, Priority = 0 } });
                    this.AddChild(new TestVariation(Value_Set) { Attribute = new VariationAttribute("SET: Empty element, Empty string content") { Params = new object[] { "<X>t0<A/>t00</X>", "" }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set) { Attribute = new VariationAttribute("SET: Empty string content, String content") { Params = new object[] { "<X>t0<A></A>t00</X>", "\nt1 " }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set) { Attribute = new VariationAttribute("SET: Empty string content, Empty string content") { Params = new object[] { "<X>t0<A></A>t00</X>", "" }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set) { Attribute = new VariationAttribute("SET: String content, String content") { Params = new object[] { "<X>t0<A>orig</A>t00</X>", "\nt1 " }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set_WithNodes) { Attribute = new VariationAttribute("SET:  XText content, string content") { Params = new object[] { "<X>t0<A>orig</A>t00</X>", "\tt1 " }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set_WithNodes) { Attribute = new VariationAttribute("SET:  Mixed content (PI only), string content") { Params = new object[] { "<X>t0<A is='is'><?PI aaa?></A>t00</X>", "\tt1 " }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set_WithNodes) { Attribute = new VariationAttribute("SET:  XText content, Empty string content") { Params = new object[] { "<X>t0<A xml:space='preserve'>orig</A>t00</X>", "" }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set_WithNodes) { Attribute = new VariationAttribute("SET:  CDATA content, Empty string content") { Params = new object[] { "<X>t0<A><![CDATA[cdata]]></A>t00</X>", "" }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set_WithNodes) { Attribute = new VariationAttribute("SET:  CDATA content, string content") { Params = new object[] { "<X>t0<A><![CDATA[cdata]]></A>t00</X>", "\tt1 " }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set_WithNodes) { Attribute = new VariationAttribute("SET:  Mixed content, Empty string content") { Params = new object[] { "<X>t0<A xmlns:p='p'>t1<p:Y/></A>t00</X>", "" }, Priority = 1 } });
                    this.AddChild(new TestVariation(Value_Set_WithNodes) { Attribute = new VariationAttribute("SET:  Mixed content, string content") { Params = new object[] { "<X>t0<A is='is'><![CDATA[cdata]]>orig<C/><!--comment--></A>t00</X>", "\tt1 " }, Priority = 0 } });
                    this.AddChild(new TestVariation(Value_Set_WithNodes) { Attribute = new VariationAttribute("SET:  Mixed content (comment only), string content") { Params = new object[] { "<X>t0<A is='is'><!--comment--></A>t00</X>", "\tt1 " }, Priority = 1 } });
                    this.AddChild(new TestVariation(set_APIModified1) { Attribute = new VariationAttribute("SET: Adjacent text nodes II.") { Params = new object[] { "<X>t0<A xmlns:p='p'>truck<p:Y/></A>t00</X>", "tn\n" }, Priority = 2 } });
                    this.AddChild(new TestVariation(set_APIModified1) { Attribute = new VariationAttribute("SET: Adjacent text nodes I.") { Params = new object[] { "<X>t0<A>truck</A>t00</X>", "tn\n" }, Priority = 2 } });
                    this.AddChild(new TestVariation(set_APIModified2) { Attribute = new VariationAttribute("SET: Adjacent text nodes III.") { Params = new object[] { "<X>t0<A xmlns:p='p'>truck\n<p:Y/>\nhello</A>t00</X>", "tn\n" }, Priority = 2 } });
                    this.AddChild(new TestVariation(set_APIModified3) { Attribute = new VariationAttribute("SET: Concatenated text I.") { Params = new object[] { "<X>t0<A>truck</A>t00</X>", "tn\n" }, Priority = 2 } });
                    this.AddChild(new TestVariation(set_APIModified3) { Attribute = new VariationAttribute("SET: Concatenated text II.") { Params = new object[] { "<X>t0<A xmlns:p='p'>truck<p:Y/></A>t00</X>", "tn\n" }, Priority = 2 } });
                    this.AddChild(new TestVariation(set_APIModified4) { Attribute = new VariationAttribute("SET: Removed node.") { Params = new object[] { "<X>t0<A xmlns:p='p'>truck\n<p:Y/>\nhello</A>t00</X>", "tn\n" }, Priority = 2 } });
                }
            }
        }
        #endregion
    }
}
