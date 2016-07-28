// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCWriteEndDocumentOnCloseTest : XmlWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCWriteEndDocumentOnCloseTest
        // Test Case
        public override void AddChildren()
        {
            // for function TestWriteEndDocumentOnCoseForOneElementwithText
            {
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElementwithText) { Attribute = new Variation("write element with text but without end element when the WriteEndDocumentOnClose = true") { Params = new object[] { true, "<root>text</root>" }, Pri = 1 } });
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElementwithText) { Attribute = new Variation("write element with text but without end element when the WriteEndDocumentOnClose = false") { Params = new object[] { false, "<root>text" }, Pri = 1 } });
            }


            // for function TestWriteEndDocumentOnCoseForOneElement
            {
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElement) { Attribute = new Variation("write start element and then close when the WriteEndDocumentOnClose = true") { Params = new object[] { true, "<root />" }, Pri = 1 } });
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElement) { Attribute = new Variation("write start element and then close when the WriteEndDocumentOnClose = false") { Params = new object[] { false, "<root>" }, Pri = 1 } });
            }


            // for function TestWriteEndDocumentOnCoseForOneElementwithAttribute
            {
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElementwithAttribute) { Attribute = new Variation("write start element and with attribute then close when the WriteEndDocumentOnClose = false") { Params = new object[] { false, "<root att=\"\">" }, Pri = 1 } });
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElementwithAttribute) { Attribute = new Variation("write start element and with attribute then close when the WriteEndDocumentOnClose = true") { Params = new object[] { true, "<root att=\"\" />" }, Pri = 1 } });
            }


            // for function TestWriteEndDocumentOnCoseForOneElementwithAttributeValue
            {
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElementwithAttributeValue) { Attribute = new Variation("write start element and with attribute value then close when the WriteEndDocumentOnClose = true") { Params = new object[] { true, "<root att=\"value\" />" }, Pri = 1 } });
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElementwithAttributeValue) { Attribute = new Variation("write start element and with attribute value then close when the WriteEndDocumentOnClose = false") { Params = new object[] { false, "<root att=\"value\">" }, Pri = 1 } });
            }


            // for function TestWriteEndDocumentOnCoseForOneElementwithNamespace
            {
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElementwithNamespace) { Attribute = new Variation("write start element and with namespace then close when the WriteEndDocumentOnClose = true") { Params = new object[] { true, "<root xmlns=\"testns\" />" }, Pri = 1 } });
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElementwithNamespace) { Attribute = new Variation("write start element and with namespace then close when the WriteEndDocumentOnClose = false") { Params = new object[] { false, "<root xmlns=\"testns\">" }, Pri = 1 } });
            }


            // for function TestWriteEndDocumentOnCoseForMultiElements
            {
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForMultiElements) { Attribute = new Variation("write multi start elements then close when the WriteEndDocumentOnClose = false") { Params = new object[] { false, "<root><sub1><sub2>" }, Pri = 1 } });
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForMultiElements) { Attribute = new Variation("write multi start elements then close when the WriteEndDocumentOnClose = true") { Params = new object[] { true, "<root><sub1><sub2 /></sub1></root>" }, Pri = 1 } });
            }


            // for function TestWriteEndDocumentOnCoseForElementsWithOneEndElement
            {
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForElementsWithOneEndElement) { Attribute = new Variation("Write two elements only one with end elment when the WriteEndDocumentOnClose = false") { Params = new object[] { false, "<root><sub1 />" }, Pri = 1 } });
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForElementsWithOneEndElement) { Attribute = new Variation("Write two elements only one with end elment when the WriteEndDocumentOnClose = true") { Params = new object[] { true, "<root><sub1 /></root>" }, Pri = 1 } });
            }


            // for function TestWriteEndDocumentOnCoseForOneElementWithEndElement
            {
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElementWithEndElement) { Attribute = new Variation("Write one element with end elment when the WriteEndDocumentOnClose = false") { Params = new object[] { false, "<root />" }, Pri = 1 } });
                this.AddChild(new CVariation(TestWriteEndDocumentOnCoseForOneElementWithEndElement) { Attribute = new Variation("Write one element with end elment when the WriteEndDocumentOnClose = true") { Params = new object[] { true, "<root />" }, Pri = 1 } });
            }
        }
    }
}
