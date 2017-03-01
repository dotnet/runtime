// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.ModuleCore;
using System.Xml.Linq;
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
                this.AddChild(new XElementName() { Attribute = new TestCaseAttribute() { Name = "XElement.Name with Events", Params = new object[] { true } } });
                this.AddChild(new XElementName() { Attribute = new TestCaseAttribute() { Name = "XElement.Name", Params = new object[] { false } } });
                this.AddChild(new XElementValue() { Attribute = new TestCaseAttribute() { Name = "XElement.Value", Params = new object[] { false } } });
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
                    this.AddChild(new TestVariation(SmokeTest) { Attribute = new VariationAttribute("GET: Mixed content - whitespace") { Params = new object[] { "<X>t0<A>t1\n<B/><B xmlns='a'>\t<![CDATA[]]> </B>\n<C></C></A>t00</X>" }, Priority = 1 } });
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
