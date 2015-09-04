// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using System.Text;
using Microsoft.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests
        // Test Module
        [Fact]
        [OuterLoop]
        public static void RunTests()
        {
            TestInput.CommandLine = "";
            FunctionalTests module = new FunctionalTests();
            module.Init();

            {
                module.AddChild(new StreamingTests() { Attribute = new TestCaseAttribute() { Name = "Streaming", Desc = "XLinq Streaming Tests" } });
            }
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }

        #region Code
        public partial class StreamingTests : XLinqTestCase
        {
            // Type is CoreXml.Test.XLinq.FunctionalTests+StreamingTests
            // Test Case
            public override void AddChildren()
            {
                this.AddChild(new XStreamingElementAPI() { Attribute = new TestCaseAttribute() { Name = "XStreamingElement API" } });
            }
            public partial class XStreamingElementAPI : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+StreamingTests+XStreamingElementAPI
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(XNameAsNullConstructor) { Attribute = new VariationAttribute("Constructor - XStreamingElement(null)") { Priority = 1 } });
                    this.AddChild(new TestVariation(XNameAsEmptyStringConstructor) { Attribute = new VariationAttribute("Constructor - XStreamingElement('')") { Priority = 1 } });
                    this.AddChild(new TestVariation(XNameConstructor) { Attribute = new VariationAttribute("Constructor - XStreamingElement(XName)") { Priority = 0 } });
                    this.AddChild(new TestVariation(XNameWithNamespaceConstructor) { Attribute = new VariationAttribute("Constructor - XStreamingElement(XName with Namespace)") { Priority = 0 } });
                    this.AddChild(new TestVariation(XNameAndNullObjectConstructor) { Attribute = new VariationAttribute("Constructor - XStreamingElement(XName, object as null)") { Priority = 1 } });
                    this.AddChild(new TestVariation(XNameAndXElementObjectConstructor) { Attribute = new VariationAttribute("Constructor - XStreamingElement(XName, XElement)") { Priority = 0 } });
                    this.AddChild(new TestVariation(XNameAndEmptyStringConstructor) { Attribute = new VariationAttribute("Constructor - XStreamingElement(XName, Empty String)") { Priority = 1 } });
                    this.AddChild(new TestVariation(XDocTypeInXStreamingElement) { Attribute = new VariationAttribute("Constructor - XStreamingElement(XName, XDocumentType)") { Priority = 0 } });
                    this.AddChild(new TestVariation(XmlDeclInXStreamingElement) { Attribute = new VariationAttribute("Constructor - XStreamingElement(XName, XDeclaration)") { Priority = 0 } });
                    this.AddChild(new TestVariation(XCDataInXStreamingElement) { Attribute = new VariationAttribute("Constructor - XStreamingElement(XName, XCDATA)") { Priority = 0 } });
                    this.AddChild(new TestVariation(XCommentInXStreamingElement) { Attribute = new VariationAttribute("Constructore - XStreamingElement(XName, XComment)") { Priority = 0 } });
                    this.AddChild(new TestVariation(XDocInXStreamingElement) { Attribute = new VariationAttribute("Constructore - XStreamingElement(XName, XDocument)") { Priority = 0 } });
                    this.AddChild(new TestVariation(XNameAndCollectionObjectConstructor) { Attribute = new VariationAttribute("Constructor - XStreamingElement(XName, object as List<object>)") { Priority = 1 } });
                    this.AddChild(new TestVariation(XNameAndObjectArrayConstructor) { Attribute = new VariationAttribute("Constructor - XStreamingElement(XName, object[])") { Priority = 0 } });
                    this.AddChild(new TestVariation(NamePropertyGet) { Attribute = new VariationAttribute("Name Property - Get") { Priority = 0 } });
                    this.AddChild(new TestVariation(NamePropertySet) { Attribute = new VariationAttribute("Name Property - Set") { Priority = 0 } });
                    this.AddChild(new TestVariation(NamePropertySetInvalid) { Attribute = new VariationAttribute("Name Property - Set(InvalIdName)") { Priority = 1 } });
                    this.AddChild(new TestVariation(XMLPropertyGet) { Attribute = new VariationAttribute("XML Property") { Priority = 0 } });
                    this.AddChild(new TestVariation(AddWithNull) { Attribute = new VariationAttribute("Add(null)") { Priority = 1 } });
                    this.AddChild(new TestVariation(AddObject) { Attribute = new VariationAttribute("Add(Int)") { Params = new object[] { 9255550134 }, Priority = 0 } });
                    this.AddChild(new TestVariation(AddObject) { Attribute = new VariationAttribute("Add(String)") { Params = new object[] { "9255550134" }, Priority = 0 } });
                    this.AddChild(new TestVariation(AddObject) { Attribute = new VariationAttribute("Add(Double)") { Params = new object[] { 9255550134 }, Priority = 0 } });
                    this.AddChild(new TestVariation(AddTimeSpanObject) { Attribute = new VariationAttribute("Add(TimeSpan)") { Priority = 1 } });
                    this.AddChild(new TestVariation(AddAttribute) { Attribute = new VariationAttribute("Add(XAttribute)") { Priority = 0 } });
                    this.AddChild(new TestVariation(AddAttributeAfterContent) { Attribute = new VariationAttribute("Add(XAttribute) After Content is Added)") { Priority = 1 } });
                    this.AddChild(new TestVariation(AddIEnumerableOfNulls) { Attribute = new VariationAttribute("Add(IEnumerable of Nulls)") { Priority = 1 } });
                    this.AddChild(new TestVariation(AddIEnumerableOfXNodes) { Attribute = new VariationAttribute("Add(IEnumerable of XNodes)") { Priority = 0 } });
                    this.AddChild(new TestVariation(AddIEnumerableOfMixedNodes) { Attribute = new VariationAttribute("Add(IEnumerable of Mixed Nodes)") { Priority = 1 } });
                    this.AddChild(new TestVariation(AddIEnumerableOfXNodesPlusString) { Attribute = new VariationAttribute("Add(IEnumerable of XNodes + string)") { Priority = 0 } });
                    this.AddChild(new TestVariation(AddIEnumerableOfXNodesPlusAttribute) { Attribute = new VariationAttribute("Add(XAttribute + IEnumerable of XNodes)") { Priority = 0 } });
                    this.AddChild(new TestVariation(SaveWithNull) { Attribute = new VariationAttribute("Save(null)") { Priority = 0 } });
                    this.AddChild(new TestVariation(SaveWithXmlTextWriter) { Attribute = new VariationAttribute("Save(TextWriter)") { Priority = 0 } });
                    this.AddChild(new TestVariation(SaveTwice) { Attribute = new VariationAttribute("Save Twice") { Priority = 0 } });
                    this.AddChild(new TestVariation(WriteToWithNull) { Attribute = new VariationAttribute("WriteTo(null)") { Priority = 1 } });
                    this.AddChild(new TestVariation(ModifyOriginalElement) { Attribute = new VariationAttribute("Modify Original Elements") { Priority = 1 } });
                    this.AddChild(new TestVariation(NestedXStreamingElement) { Attribute = new VariationAttribute("Nested XStreamingElements") { Priority = 0 } });
                    this.AddChild(new TestVariation(NestedXStreamingElementPlusIEnumerable) { Attribute = new VariationAttribute("Nested XStreamingElements + IEnumerable") { Priority = 0 } });
                    this.AddChild(new TestVariation(IEnumerableLazinessTest1) { Attribute = new VariationAttribute("Laziness of IEnumerables - Modify IEnumerable after adding") { Priority = 1 } });
                    this.AddChild(new TestVariation(IEnumerableLazinessTest2) { Attribute = new VariationAttribute("Laziness of IEnumerables - Make Sure IEnumerable is walked after Save") { Priority = 1 } });
                    this.AddChild(new TestVariation(XStreamingElementInXElement) { Attribute = new VariationAttribute("XStreamingElement in XElement") { Priority = 0 } });
                    this.AddChild(new TestVariation(XStreamingElementInXDocument) { Attribute = new VariationAttribute("XStreamingElement in XDocument") { Priority = 0 } });
                }
            }
        }
        #endregion
    }
}
