// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class SimpleObjectsCreation : XLinqTestCase
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+SimpleObjectsCreation
        // Test Case

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(CreateXDocument) { Attribute = new VariationAttribute("Empty XDocument") { Priority = 0 } });
            AddChild(new TestVariation(CreateXDocument_Decl) { Attribute = new VariationAttribute("Empty XDocument + XmlDecl") { Param = true, Priority = 0 } });
            AddChild(new TestVariation(CreateXDocument_Decl) { Attribute = new VariationAttribute("Empty XDocument + XmlDecl=null") { Param = false, Priority = 0 } });
            AddChild(new TestVariation(CreateXDocument2) { Attribute = new VariationAttribute("Copy Empty XDocument") { Priority = 1 } });
            AddChild(new TestVariation(CreateXDocument3) { Attribute = new VariationAttribute("Copy NonEmpty XDocument") { Priority = 1 } });
            AddChild(new TestVariation(CreateXDocument4) { Attribute = new VariationAttribute("XDocument - null test") { Priority = 1 } });
            AddChild(new TestVariation(CreateXElement1) { Attribute = new VariationAttribute("Empty XElement - Xname") { Priority = 0 } });
            AddChild(new TestVariation(CreateXElement2) { Attribute = new VariationAttribute("Empty XElement - Xname - noNS") { Priority = 2 } });
            AddChild(new TestVariation(CreateXElement3) { Attribute = new VariationAttribute("XElement - Xname - null") { Priority = 2 } });
            AddChild(new TestVariation(CreateXElement31) { Attribute = new VariationAttribute("XElement - Xname - empty string") { Priority = 2 } });
            AddChild(new TestVariation(CreateXElement4) { Attribute = new VariationAttribute("Empty XElement - string") { Priority = 0 } });
            AddChild(new TestVariation(CreateXElement5) { Attribute = new VariationAttribute("Empty XElement - string - noNS") { Priority = 2 } });
            AddChild(new TestVariation(CreateXElement6) { Attribute = new VariationAttribute("XElement - string - null") { Priority = 2 } });
            AddChild(new TestVariation(CreateXElement6_1) { Attribute = new VariationAttribute("XElement - XStreamingElement - null") { Priority = 2 } });
            AddChild(new TestVariation(CreateXElement7) { Attribute = new VariationAttribute("XElement - copy empty") { Priority = 0 } });
            AddChild(new TestVariation(CreateXElement8) { Attribute = new VariationAttribute("XElement - copy: connected, in XDocument") { Params = new object[] { true, true }, Priority = 0 } });
            AddChild(new TestVariation(CreateXElement8) { Attribute = new VariationAttribute("XElement - copy: not connected, in XDocument") { Params = new object[] { false, true }, Priority = 0 } });
            AddChild(new TestVariation(CreateXElement8) { Attribute = new VariationAttribute("XElement - copy: connected, not in XDocument") { Params = new object[] { true, false }, Priority = 0 } });
            AddChild(new TestVariation(CreateXElement8) { Attribute = new VariationAttribute("XElement - copy: not connected, not in XDocument") { Params = new object[] { false, false }, Priority = 0 } });
            AddChild(new TestVariation(CreateXElementFromReader1) { Attribute = new VariationAttribute("XElement - from reader - from root element") { Priority = 0 } });
            AddChild(new TestVariation(CreateXElementFromReader2) { Attribute = new VariationAttribute("XElement - from reader - from inner element") { Priority = 0 } });
            AddChild(new TestVariation(CreateXElementFromReaderEmptyElem) { Attribute = new VariationAttribute("XElement - from reader - from inner empty element") { Priority = 0 } });
            AddChild(new TestVariation(CreateXElementFromReader3) { Attribute = new VariationAttribute("XElement - from reader - from inner element, namespaces") { Priority = 0 } });
            AddChild(new TestVariation(NotAllowedNodesRead) { Attribute = new VariationAttribute("XElement - from reader - not allowed nodes - endElement") { Param = 4, Priority = 2 } });
            AddChild(new TestVariation(NotAllowedNodesRead) { Attribute = new VariationAttribute("XElement - from reader - not allowed nodes - initial") { Param = 0, Priority = 2 } });
            AddChild(new TestVariation(NotAllowedNodesRead) { Attribute = new VariationAttribute("XElement - from reader - not allowed nodes - decl") { Param = 1, Priority = 2 } });
            AddChild(new TestVariation(NotAllowedNodesAttr) { Attribute = new VariationAttribute("XElement - from reader - not allowed nodes - attribute") { Param = false, Priority = 2 } });
            AddChild(new TestVariation(CreateXAttribute) { Attribute = new VariationAttribute("XAttribute - xname - not namespace decl") { Priority = 0 } });
            AddChild(new TestVariation(CreateXAttribute1) { Attribute = new VariationAttribute("XAttribute - xname - namespace decl - default") { Param = "xmlns", Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute2) { Attribute = new VariationAttribute("XAttribute - xname - namespace decl - normal") { Param = "p", Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute3) { Attribute = new VariationAttribute("XAttribute - copy - not namespace decl - connected") { Param = true, Priority = 0 } });
            AddChild(new TestVariation(CreateXAttribute3) { Attribute = new VariationAttribute("XAttribute - copy - not namespace decl - not connected") { Param = false, Priority = 0 } });
            AddChild(new TestVariation(CreateXAttribute4) { Attribute = new VariationAttribute("XAttribute - copy - default namespace decl - connected") { Params = new object[] { true, "xmlns" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute4) { Attribute = new VariationAttribute("XAttribute - copy - default namespace decl - not connected") { Params = new object[] { false, "xmlns" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute5) { Attribute = new VariationAttribute("XAttribute - copy - namespace decl - connected") { Params = new object[] { true, "p" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute5) { Attribute = new VariationAttribute("XAttribute - copy - namespace decl - not connected") { Params = new object[] { false, "p" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute6) { Attribute = new VariationAttribute("XAttribute - default namespace decl & empty value") { Params = new object[] { false, "xmlns" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute7) { Attribute = new VariationAttribute("XAttribute - prefixed namespace decl & empty value") { Params = new object[] { true, "p" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute8) { Attribute = new VariationAttribute("XAttribute - xml namespace - redeclared (positive).") { Params = new object[] { "xml", "http://www.w3.org/XML/1998/namespace", false }, Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute8) { Attribute = new VariationAttribute("XAttribute - xml namespace - prefix redef") { Params = new object[] { "xml", "another", true }, Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute8) { Attribute = new VariationAttribute("XAttribute - xml namespace - bound to diff. prefix") { Params = new object[] { "p", "http://www.w3.org/XML/1998/namespace", true }, Priority = 1 } });
            AddChild(new TestVariation(CreateXAttribute8) { Attribute = new VariationAttribute("XAttribute - xmlns namespace - bound to diff. prefix") { Params = new object[] { "p", "http://www.w3.org/2000/xmlns/", true }, Priority = 1 } });
            AddChild(new TestVariation(CreateXPI1) { Attribute = new VariationAttribute("XProcessingInstruction - target & data") { Priority = 1 } });
            AddChild(new TestVariation(CreateXPI2) { Attribute = new VariationAttribute("XProcessingInstruction - target only") { Priority = 1 } });
            AddChild(new TestVariation(CreateXPINegative) { Attribute = new VariationAttribute("XProcessingInstruction - invalid name (invalid char)") { Param = "?>", Priority = 1 } });
            AddChild(new TestVariation(CreateXPINegative2) { Attribute = new VariationAttribute("XProcessingInstruction - invalid name (XmL)") { Param = "XmL", Priority = 2 } });
            AddChild(new TestVariation(CreateXPINegative2) { Attribute = new VariationAttribute("XProcessingInstruction - invalid name (XmL)") { Param = "XmL", Priority = 2 } });
            AddChild(new TestVariation(CreateXPINegative2) { Attribute = new VariationAttribute("XProcessingInstruction - invalid name (xML)") { Param = "xML", Priority = 2 } });
            AddChild(new TestVariation(CreateXPINegative2) { Attribute = new VariationAttribute("XProcessingInstruction - invalid name (xmL)") { Param = "xmL", Priority = 2 } });
            AddChild(new TestVariation(CreateXPINegative2) { Attribute = new VariationAttribute("XProcessingInstruction - invalid name (Xml)") { Param = "Xml", Priority = 2 } });
            AddChild(new TestVariation(CreateXPINegative2) { Attribute = new VariationAttribute("XProcessingInstruction - invalid name (xmL)") { Param = "xmL", Priority = 2 } });
            AddChild(new TestVariation(CreateXPINegative2) { Attribute = new VariationAttribute("XProcessingInstruction - invalid name (XML)") { Param = "XML", Priority = 2 } });
            AddChild(new TestVariation(CreateXPINegative2) { Attribute = new VariationAttribute("XProcessingInstruction - invalid name (empty string)") { Param = "", Priority = 2 } });
            AddChild(new TestVariation(CreateXPINegative2) { Attribute = new VariationAttribute("XProcessingInstruction - invalid name (xml)") { Param = "xml", Priority = 2 } });
            AddChild(new TestVariation(CreateXPI3) { Attribute = new VariationAttribute("XProcessingInstruction - from reader - target & data") { Param = 2, Priority = 1 } });
            AddChild(new TestVariation(CreateXPI3) { Attribute = new VariationAttribute("XProcessingInstruction - from reader - target only") { Param = 4, Priority = 1 } });
            AddChild(new TestVariation(CreateXComment1) { Attribute = new VariationAttribute("XComment") { Priority = 1 } });
            AddChild(new TestVariation(CreateXComment2) { Attribute = new VariationAttribute("XComment - empty") { Priority = 3 } });
            AddChild(new TestVariation(CreateXComment3) { Attribute = new VariationAttribute("XComment - from reader") { Priority = 1 } });
            AddChild(new TestVariation(CreateXText0) { Attribute = new VariationAttribute("XText - from string") { Priority = 0 } });
            AddChild(new TestVariation(CreateXText01) { Attribute = new VariationAttribute("XText - copy") { Priority = 0 } });
            AddChild(new TestVariation(CreateXText1) { Attribute = new VariationAttribute("XText - CDATA") { Params = new object[] { XmlNodeType.CDATA, "<![CDATA[MY_TEXT]]>" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXText1) { Attribute = new VariationAttribute("XText - text") { Params = new object[] { XmlNodeType.Text, "MY_TEXT" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXText2) { Attribute = new VariationAttribute("XText - Text - empty") { Params = new object[] { XmlNodeType.Text, "" }, Priority = 3 } });
            AddChild(new TestVariation(CreateXText2) { Attribute = new VariationAttribute("XText - CData - empty") { Params = new object[] { XmlNodeType.CDATA, "<![CDATA[]]>" }, Priority = 3 } });
            AddChild(new TestVariation(CreateXTextReader) { Attribute = new VariationAttribute("XText - Text - from reader") { Params = new object[] { 4, XmlNodeType.Text, "MY_TEXT", "<A><![CDATA[MY_TEXT]]><B/>MY_TEXT<C/>\t<D/></A>" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXTextReader) { Attribute = new VariationAttribute("XText - Text - from reader/ significant whitespace") { Params = new object[] { 2, XmlNodeType.Text, "\t", "<a xml:space='preserve'>\t<X/></a>" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXTextReader) { Attribute = new VariationAttribute("XText - Text - from reader/ whitespace") { Params = new object[] { 6, XmlNodeType.Text, "\t", "<A><![CDATA[MY_TEXT]]><B/>MY_TEXT<C/>\t<D/></A>" }, Priority = 1 } });
            AddChild(new TestVariation(CreateXTextReader) { Attribute = new VariationAttribute("XText - CDATA - from reader") { Params = new object[] { 2, XmlNodeType.CDATA, "MY_TEXT", "<A><![CDATA[MY_TEXT]]><B/>MY_TEXT<C/>\t<D/></A>" }, Priority = 1 } });
            AddChild(new TestVariation(DTDConstruct) { Attribute = new VariationAttribute("DTD - all nulls") { Params = new object[] { new[] { "root", null, null, null }, "<!DOCTYPE root >" }, Priority = 1 } });
            AddChild(new TestVariation(DTDConstruct) { Attribute = new VariationAttribute("DTD - no publicId") { Params = new object[] { new[] { "root", null, "a2", "a3" }, "<!DOCTYPE root SYSTEM \"a2\"[a3]>" }, Priority = 1 } });
            AddChild(new TestVariation(DTDConstruct) { Attribute = new VariationAttribute("DTD - no systemId") { Params = new object[] { new[] { "root", "a1", null, "a3" }, "<!DOCTYPE root PUBLIC \"a1\" \"\"[a3]>" }, Priority = 1 } });
            AddChild(new TestVariation(DTDConstruct) { Attribute = new VariationAttribute("DTD - no publicId, no systemId") { Params = new object[] { new[] { "root", null, null, "a3" }, "<!DOCTYPE root [a3]>" }, Priority = 1 } });
            AddChild(new TestVariation(DTDConstruct) { Attribute = new VariationAttribute("DTD - normal") { Params = new object[] { new[] { "root", "a1", "a2", "a3" }, "<!DOCTYPE root PUBLIC \"a1\" \"a2\"[a3]>" }, Priority = 0 } });
        }

        // XDocument constructors
        //[Variation(Priority = 0, Desc = "Empty XDocument")]

        public void CreateXAttribute()
        {
            XName name = "{ns1}id";
            var attr = new XAttribute(name, "value");
            TestLog.Compare(attr != null, "attr!=null");
            TestLog.Compare(attr.Name, name, "Name");
            TestLog.Compare(attr.Value, "value", "value");
            TestLog.Compare(attr.Parent == null, "Parent");
            TestLog.Compare(!attr.IsNamespaceDeclaration, "IsNamespaceDeclaration");
            attr.Verify();
        }

        //[Variation(Priority = 1, Desc = "XAttribute - xname - namespace decl - default", Param = "xmlns")]
        public void CreateXAttribute1()
        {
            var prefix = (string)Variation.Param;
            XName name = prefix;
            var attr = new XAttribute(name, "value");
            TestLog.Compare(attr != null, "attr!=null");
            TestLog.Compare(attr.Name, name, "Name");
            TestLog.Compare(attr.Value, "value", "value");
            TestLog.Compare(attr.Parent == null, "Parent");
            TestLog.Compare(attr.IsNamespaceDeclaration, "IsNamespaceDeclaration");
            attr.Verify();
        }

        //[Variation(Priority = 1, Desc = "XAttribute - xname - namespace decl - normal", Param = "p")]
        public void CreateXAttribute2()
        {
            var prefix = (string)Variation.Param;
            XName name = XNamespace.Xmlns + prefix;
            var attr = new XAttribute(name, "value");
            TestLog.Compare(attr != null, "attr!=null");
            TestLog.Compare(attr.Name, name, "Name");
            TestLog.Compare(attr.Value, "value", "value");
            TestLog.Compare(attr.Parent == null, "Parent");
            TestLog.Compare(attr.IsNamespaceDeclaration, "IsNamespaceDeclaration");
            attr.Verify();
        }

        //[Variation(Priority = 0, Desc = "XAttribute - copy - not namespace decl - connected", Param = true)]
        //[Variation(Priority = 0, Desc = "XAttribute - copy - not namespace decl - not connected", Param = false)]
        public void CreateXAttribute3()
        {
            var isConnected = (bool)Variation.Param;

            XName name = "{ns1}id";
            var attr1 = new XAttribute(name, "value");

            if (isConnected)
            {
                var x = new XElement("parent", attr1);
            }

            var attr = new XAttribute(attr1);

            TestLog.Compare(attr != null, "attr!=null");
            TestLog.Compare(attr1 != attr, "(object)attr1!=(object)attr");
            TestLog.Compare(attr1.Value.Equals(attr.Value), "attr1.Value.Equals(attr.Value)");
            TestLog.Compare(attr.Value.Equals(attr1.Value), "attr.Equals(attr1)");
            TestLog.Compare(attr.Name, name, "Name");
            TestLog.Compare(attr.Value, "value", "value");
            TestLog.Compare(attr.Parent == null, "Parent");
            TestLog.Compare(!attr.IsNamespaceDeclaration, "IsNamespaceDeclaration");
            attr.Verify();
        }

        //[Variation(Priority = 1, Desc = "XAttribute - copy - default namespace decl - connected", Params = new object[] { true, "xmlns" })]
        //[Variation(Priority = 1, Desc = "XAttribute - copy - default namespace decl - not connected", Params = new object[] { false, "xmlns" })]
        public void CreateXAttribute4()
        {
            var isConnected = (bool)Variation.Params[0];
            var prefix = (string)Variation.Params[1];
            XName name = prefix;
            var attr1 = new XAttribute(name, "value");

            if (isConnected)
            {
                var dummmy = new XElement("dummy", attr1);
            }

            var attr = new XAttribute(attr1);
            TestLog.Compare(attr != null, "attr!=null");
            TestLog.Compare(attr1 != attr, "(object)attr1!=(object)attr");
            TestLog.Compare(attr.Value.Equals(attr1.Value), "attr.Value.Equals(attr1.Value)");
            TestLog.Compare(attr.Name, name, "Name");
            TestLog.Compare(attr.Value, "value", "value");
            TestLog.Compare(attr.Parent == null, "Parent");
            TestLog.Compare(attr.IsNamespaceDeclaration, "IsNamespaceDeclaration");
            attr.Verify();
        }

        //[Variation(Priority = 1, Desc = "XAttribute - copy - namespace decl - connected", Params = new object[] { true, "p" })]
        //[Variation(Priority = 1, Desc = "XAttribute - copy - namespace decl - not connected", Params = new object[] { false, "p" })]
        public void CreateXAttribute5()
        {
            var isConnected = (bool)Variation.Params[0];
            var prefix = (string)Variation.Params[1];
            XName name = XNamespace.Xmlns + prefix;
            var attr1 = new XAttribute(name, "value");

            if (isConnected)
            {
                var dummmy = new XElement("dummy", attr1);
            }

            var attr = new XAttribute(attr1);
            TestLog.Compare(attr != null, "attr!=null");
            TestLog.Compare(attr1 != attr, "(object)attr1!=(object)attr");
            TestLog.Compare(attr.Value.Equals(attr1.Value), "attr.Value.Equals(attr1.Value)");
            TestLog.Compare(attr.Name, name, "Name");
            TestLog.Compare(attr.Value, "value", "value");
            TestLog.Compare(attr.Parent == null, "Parent");
            TestLog.Compare(attr.IsNamespaceDeclaration, "IsNamespaceDeclaration");
            attr.Verify();
        }

        // 21.	The empty string cannot be used as a namespace name (exception is default ns redef.)  
        //[Variation(Priority = 1, Desc = "XAttribute - default namespace decl & empty value", Params = new object[] { false, "xmlns" })]
        public void CreateXAttribute6()
        {
            var shouldFail = (bool)Variation.Params[0];
            var prefix = (string)Variation.Params[1];

            try
            {
                var a = new XAttribute(prefix, "");
                TestLog.Compare(!shouldFail, "Should fail");
                a.Verify();
            }
            catch (ArgumentException)
            {
                TestLog.Compare(shouldFail, "NOT EXPECTED EXCEPTION");
            }
        }

        // 21.	The empty string cannot be used as a namespace name (exception is default ns redef.)  
        //[Variation(Priority = 1, Desc = "XAttribute - prefixed namespace decl & empty value", Params = new object[] { true, "p" })]
        public void CreateXAttribute7()
        {
            var shouldFail = (bool)Variation.Params[0];
            var prefix = (string)Variation.Params[1];

            try
            {
                var a = new XAttribute(XNamespace.Xmlns + prefix, "");
                TestLog.Compare(!shouldFail, "Should fail");
                a.Verify();
            }
            catch (ArgumentException)
            {
                TestLog.Compare(shouldFail, "NOT EXPECTED EXCEPTION");
            }
        }

        // 22.	Xml prefix:  Enforce
        //    �	http://www.w3.org/XML/1998/namespace 
        //    �	Must not be bound to any other namespace name.
        //    �	No other prefix may be bound to this namespace name.
        // 23.	Xmlns prefix:  Enforce, but some concern about performance
        //    �	http://www.w3.org/2000/xmlns/ 
        //    �	It must not be declared
        //    �	No other prefix may be bound to this namespace name. 
        //[Variation(Priority = 1, Desc = "XAttribute - xml namespace - prefix redef", Params = new object[] { "xml", "another", true })]
        //[Variation(Priority = 1, Desc = "XAttribute - xml namespace - bound to diff. prefix", Params = new object[] { "p", "http://www.w3.org/XML/1998/namespace", true })]
        //[Variation(Priority = 1, Desc = "XAttribute - xml namespace - redeclared (positive).", Params = new object[] { "xml", "http://www.w3.org/XML/1998/namespace", false })]
        //[Variation(Priority = 1, Desc = "XAttribute - xmlns namespace - bound to diff. prefix", Params = new object[] { "p", "http://www.w3.org/2000/xmlns/", true })]
        public void CreateXAttribute8()
        {
            var prefix = (string)Variation.Params[0];
            var value = (string)Variation.Params[1];
            var shouldFail = (bool)Variation.Params[2];

            try
            {
                var a = new XAttribute(XNamespace.Xmlns + prefix, value);
                TestLog.Compare(!shouldFail, "Should fail");
                a.Verify();
            }
            catch (ArgumentException)
            {
                TestLog.Compare(shouldFail, "NOT EXPECTED EXCEPTION");
            }
        }

        public void CreateXComment1()
        {
            var comment = new XComment("value");
            TestLog.Compare(comment != null, "pi != null");
            TestLog.Compare(comment.NodeType, XmlNodeType.Comment, "NodeType");
            TestLog.Compare(comment.Value, "value", "Value");
            TestLog.Compare(comment.Document == null, "Document");
            TestLog.Compare(comment.Parent == null, "Parent");
            TestLog.Compare(comment.ToString(SaveOptions.DisableFormatting), @"<!--value-->", "Xml");
        }

        //[Variation(Priority = 3, Desc = "XComment - empty")]
        public void CreateXComment2()
        {
            var comment = new XComment("");
            TestLog.Compare(comment != null, "pi != null");
            TestLog.Compare(comment.NodeType, XmlNodeType.Comment, "NodeType");
            TestLog.Compare(comment.Value, "", "Value");
            TestLog.Compare(comment.Document == null, "Document");
            TestLog.Compare(comment.Parent == null, "Parent");
            TestLog.Compare(comment.ToString(SaveOptions.DisableFormatting), @"<!---->", "Xml");
        }

        //[Variation(Priority = 1, Desc = "XComment - from reader")]
        public void CreateXComment3()
        {
            string xml = @"<!--value--><A/>";

            using (XmlReader r = XmlReader.Create(new StringReader(xml)))
            {
                r.Read();
                var comment = (XComment)XNode.ReadFrom(r);

                TestLog.Compare(comment != null, "pi != null");
                TestLog.Compare(comment.NodeType, XmlNodeType.Comment, "NodeType");
                TestLog.Compare(comment.Value, "value", "Value");
                TestLog.Compare(comment.Document == null, "Document");
                TestLog.Compare(comment.Parent == null, "Parent");
                TestLog.Compare(comment.ToString(SaveOptions.DisableFormatting), @"<!--value-->", "Xml");

                TestLog.Compare(r.ReadState, ReadState.Interactive, "reader state ");
                TestLog.Compare(r.NodeType, XmlNodeType.Element, "reader position");
            }
        }

        public void CreateXDocument()
        {
            var doc = new XDocument();
            TestLog.Compare(doc != null, "doc!=null");
            TestLog.Compare(doc.FirstNode == null, "doc is empty");
            TestLog.Compare(doc.Root == null, "doc.Root == null");
            TestLog.Compare(doc.Declaration == null, "doc.Declaration == null");
            TestLog.Compare(doc.DocumentType == null, "doc.DocumentType == null");
            TestLog.Compare(doc.Document == doc, "doc.Document == doc");
        }

        //[Variation(Priority = 0, Desc = "Empty XDocument + XmlDecl", Param = true)]
        //[Variation(Priority = 0, Desc = "Empty XDocument + XmlDecl=null", Param = false)]

        //[Variation(Priority = 1, Desc = "Copy Empty XDocument")]
        public void CreateXDocument2()
        {
            var doc1 = new XDocument();
            var doc = new XDocument(doc1);
            TestLog.Compare(doc != null, "doc!=null");
            TestLog.Compare(doc != doc1, "(object)doc!=(object)doc1");
            TestLog.Compare(doc.FirstNode == null, "doc is empty");
            TestLog.Compare(doc.Root == null, "doc.Root == null");
            TestLog.Compare(doc.Declaration == null, "doc.Declaration == null");
            TestLog.Compare(doc.DocumentType == null, "doc.DocumentType == null");
            TestLog.Compare(doc.Document == doc, "doc.Document == doc");
            TestLog.Compare(doc1.Document == doc1, "doc1.Document == doc1");
        }

        //[Variation(Priority = 1, Desc = "Copy NonEmpty XDocument")]
        public void CreateXDocument3()
        {
            var doc1 = new XDocument(new XDeclaration("1.0", "UTF8", "true"));
            doc1.Add(new XElement("root", new XAttribute("id", "a1"), "text"));

            var doc = new XDocument(doc1);
            TestLog.Compare(doc != null, "doc!=null");
            TestLog.Compare(doc != doc1, "(object)doc!=(object)doc1");
            TestLog.Compare(XNode.DeepEquals(doc.Root, doc1.Root), "XNode.DeepEquals(doc.Root, doc1.Root)");
            TestLog.Compare(doc.Root != doc1.Root, "(object)doc.Root == (object)doc1.Root");
            TestLog.Compare(doc.Declaration.Version, doc1.Declaration.Version, "Declaration.Version");
            TestLog.Compare(doc.Declaration.Encoding, doc1.Declaration.Encoding, "Declaration.Encoding");
            TestLog.Compare(doc.Declaration.Standalone, doc1.Declaration.Standalone, "Declaration.Standalone");
            TestLog.Compare(doc.Declaration != doc1.Declaration, "(object)doc.Declaration != (object)doc1.Declaration");
            TestLog.Compare(doc.DocumentType == null, "doc.DocumentType == null");
            TestLog.Compare(doc.Document == doc, "doc.Document == doc");
            TestLog.Compare(doc1.Document == doc1, "doc1.Document == doc1");
        }

        //[Variation(Priority = 1, Desc = "XDocument - null test")]
        public void CreateXDocument4()
        {
            try
            {
                var doc = new XDocument((XDocument)null);
            }
            catch (ArgumentException)
            {
                return;
            }
            TestLog.WriteLine("Exception was expected here!");
            throw new TestException(TestResult.Failed, "");
        }

        public void CreateXDocument_Decl()
        {
            XDeclaration decl = (bool)Variation.Param ? new XDeclaration("1.0", "UTF8", "True") : null;
            var doc = new XDocument(decl, null);
            TestLog.Compare(doc != null, "doc!=null");
            TestLog.Compare(doc.FirstNode == null, "doc is empty");
            TestLog.Compare(doc.Root == null, "doc.Root == null");
            TestLog.Compare(doc.Declaration == decl, "doc.Declaration == decl");
            TestLog.Compare(doc.DocumentType == null, "doc.DocumentType == null");
            TestLog.Compare(doc.Document == doc, "doc.Document == doc");
        }

        // XElement constructors

        //[Variation(Priority = 0, Desc = "Empty XElement - Xname")]
        public void CreateXElement1()
        {
            XNamespace ns1 = XNamespace.Get("http://myNS");
            XName xname = ns1 + "elem1";
            var elem = new XElement(xname);
            TestLog.Compare(elem.Document == null, "elem.Document");
            TestLog.Compare(!elem.HasAttributes, "HasAttributes");
            TestLog.Compare(!elem.HasElements, "HasElements");
            TestLog.Compare(elem.IsEmpty, "IsEmpty");
            TestLog.Compare(elem.FirstNode == null, "query for content");
            TestLog.Compare(elem.NodeType, XmlNodeType.Element, "NodeType");
            TestLog.Compare(elem.Parent == null, "Parent");
            TestLog.Compare(elem.Value, "", "Value");
            TestLog.Compare(elem.Name, xname, "Name");
            TestLog.Compare(elem.Name.LocalName, "elem1", "LocalName");
            TestLog.Compare(elem.Name.Namespace, ns1, "Namespace");
            elem.Verify();
        }

        //[Variation(Priority = 2, Desc = "Empty XElement - Xname - noNS")]
        public void CreateXElement2()
        {
            XName xname = "elem1";
            var elem = new XElement(xname);
            TestLog.Compare(elem.Document == null, "elem.Document");
            TestLog.Compare(!elem.HasAttributes, "HasAttributes");
            TestLog.Compare(!elem.HasElements, "HasElements");
            TestLog.Compare(elem.IsEmpty, "IsEmpty");
            TestLog.Compare(elem.FirstNode == null, "query for content");
            TestLog.Compare(elem.NodeType, XmlNodeType.Element, "NodeType");
            TestLog.Compare(elem.Parent == null, "Parent");
            TestLog.Compare(elem.Value, "", "Value");
            TestLog.Compare(elem.Name, xname, "Name");
            TestLog.Compare(elem.Name.LocalName, "elem1", "LocalName");
            elem.Verify();
        }

        //[Variation(Priority = 2, Desc = "XElement - Xname - null")]
        public void CreateXElement3()
        {
            try
            {
                var elem = new XElement((XName)null);
            }
            catch (ArgumentException)
            {
                return;
            }
            throw new TestException(TestResult.Failed, "");
        }

        //[Variation(Priority = 2, Desc = "XElement - Xname - empty string")]
        public void CreateXElement31()
        {
            try
            {
                var elem = new XElement("");
            }
            catch (ArgumentException)
            {
                return;
            }
            throw new TestException(TestResult.Failed, "");
        }

        //[Variation(Priority = 0, Desc = "Empty XElement - string")]
        public void CreateXElement4()
        {
            XNamespace ns1 = XNamespace.Get("http://myNS");
            XName xname = ns1 + "elem1";
            var elem = new XElement("{http://myNS}elem1");
            TestLog.Compare(elem.Document == null, "elem.Document");
            TestLog.Compare(!elem.HasAttributes, "HasAttributes");
            TestLog.Compare(!elem.HasElements, "HasElements");
            TestLog.Compare(elem.IsEmpty, "IsEmpty");
            TestLog.Compare(elem.FirstNode == null, "query for content");
            TestLog.Compare(elem.NodeType, XmlNodeType.Element, "NodeType");
            TestLog.Compare(elem.Parent == null, "Parent");
            TestLog.Compare(elem.Value, "", "Value");
            TestLog.Compare(elem.Name, xname, "Name");
            TestLog.Compare(elem.Name.LocalName, "elem1", "LocalName");
            TestLog.Compare(elem.Name.Namespace, ns1, "Namespace");
            elem.Verify();
        }

        //[Variation(Priority = 2, Desc = "Empty XElement - string - noNS")]
        public void CreateXElement5()
        {
            XName xname = "elem1";
            XNamespace ns = XNamespace.Get("");
            var elem = new XElement("elem1");
            TestLog.Compare(elem.Document == null, "elem.Document");
            TestLog.Compare(!elem.HasAttributes, "HasAttributes");
            TestLog.Compare(!elem.HasElements, "HasElements");
            TestLog.Compare(elem.IsEmpty, "IsEmpty");
            TestLog.Compare(elem.FirstNode == null, "query for content");
            TestLog.Compare(elem.NodeType, XmlNodeType.Element, "NodeType");
            TestLog.Compare(elem.Parent == null, "Parent");
            TestLog.Compare(elem.Value, "", "Value");
            TestLog.Compare(elem.Name, xname, "Name");
            TestLog.Compare(elem.Name.LocalName, "elem1", "LocalName");
            TestLog.Compare(elem.Name.Namespace, ns, "namespace");
            elem.Verify();
        }

        //[Variation(Priority = 2, Desc = "XElement - string - null")]
        public void CreateXElement6()
        {
            try
            {
                var elem = new XElement((string)null);
            }
            catch (ArgumentException)
            {
                return;
            }
            throw new TestException(TestResult.Failed, "");
        }

        //[Variation(Priority = 2, Desc = "XElement - XStreamingElement - null")]
        public void CreateXElement6_1()
        {
            try
            {
                var elem = new XElement((XStreamingElement)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            throw new TestException(TestResult.Failed, "");
        }

        //[Variation(Priority = 0, Desc = "XElement - copy empty")]
        public void CreateXElement7()
        {
            XNamespace ns1 = XNamespace.Get("http://myNS");
            XName xname = ns1 + "elem1";
            var elem = new XElement(xname);
            var elem1 = new XElement(elem);

            TestLog.Compare(elem1 != null, "elem1!=null");
            TestLog.Compare(elem1 != elem, "(object)elem1!=(object)elem");
            TestLog.Compare(XNode.DeepEquals(elem1, elem), "XNode.DeepEquals(elem1,elem)");
            TestLog.Compare(elem1.Document == null, "elem1.Document");
            TestLog.Compare(!elem1.HasAttributes, "HasAttributes");
            TestLog.Compare(!elem1.HasElements, "HasElements");
            TestLog.Compare(elem1.IsEmpty, "IsEmpty");
            TestLog.Compare(elem1.FirstNode == null, "query for content");
            TestLog.Compare(elem1.NodeType, XmlNodeType.Element, "NodeType");
            TestLog.Compare(elem1.Parent == null, "Parent");
            TestLog.Compare(elem1.Value, "", "Value");
            TestLog.Compare(elem1.Name, xname, "Name");
            TestLog.Compare(elem1.Name.LocalName, "elem1", "LocalName");
            TestLog.Compare(elem1.Name.Namespace, ns1, "Namespace");
            elem1.Verify();
        }

        //[Variation(Priority = 0, Desc = "XElement - copy: connected, in XDocument", Params = new object[] { true, true })]
        //[Variation(Priority = 0, Desc = "XElement - copy: connected, not in XDocument", Params = new object[] { true, false })]
        //[Variation(Priority = 0, Desc = "XElement - copy: not connected, in XDocument", Params = new object[] { false, true })]
        //[Variation(Priority = 0, Desc = "XElement - copy: not connected, not in XDocument", Params = new object[] { false, false })]
        public void CreateXElement8()
        {
            var isConnected = (bool)Variation.Params[0];
            var isInDocument = (bool)Variation.Params[1];

            XNamespace ns1 = XNamespace.Get("http://myNS");
            XName xname = ns1 + "elem1";
            var elem1 = new XElement(xname, "text", new XElement("inner"), new XAttribute("id", "a1"));

            if (isConnected)
            {
                var parent = new XElement("parent", elem1);
                if (isInDocument)
                {
                    var doc = new XDocument(parent);
                }
            }
            else
            {
                if (isInDocument)
                {
                    var doc = new XDocument(elem1);
                }
            }

            var elem = new XElement(elem1);

            TestLog.Compare(elem1 != null, "elem1!=null");
            TestLog.Compare(elem1 != elem, "(object)elem1!=(object)elem");
            TestLog.Compare(XNode.DeepEquals(elem1, elem), "XNode.DeepEquals(elem1,elem)");
            TestLog.Compare(elem.Document == null, "elem.Document");
            elem.Verify();

            // compare attributes           
            TestLog.Compare(elem.Attribute("id") != null, "attributes null");
            TestLog.Compare(elem.Attribute("id") != elem1.Attribute("id"), "attribute ==");
            TestLog.Compare(elem.Attribute("id").Value.Equals(elem1.Attribute("id").Value), "attribute Equals");

            // compare children
            TestLog.Compare(elem.Element("inner") != null, "element null");
            TestLog.Compare(elem.Element("inner") != elem1.Element("inner"), "element ==");
            TestLog.Compare(XNode.DeepEquals(elem.Element("inner"), elem1.Element("inner")), "element Equals");

            TestLog.Compare(elem.NodeType, XmlNodeType.Element, "NodeType");
            TestLog.Compare(elem.Parent == null, "Parent");
            TestLog.Compare(elem.Value, elem1.Value, "Value");

            TestLog.Compare(elem1.Name, elem.Name, "Name");
            TestLog.Compare(elem1.Name.LocalName, elem.Name.LocalName, "LocalName");
            TestLog.Compare(elem1.Name.Namespace, elem.Name.Namespace, "Namespace");
        }

        //[Variation(Priority = 0, Desc = "XElement - from reader - from root element")]
        public void CreateXElementFromReader1()
        {
            string xml = "<A id='a' xmlns='ns1'><B/></A><?PI targ?>";
            using (XmlReader r = XmlReader.Create(new StringReader(xml)))
            {
                r.Read();
                var elem = (XElement)XNode.ReadFrom(r);

                // element sanity
                TestLog.Compare(elem.Name.LocalName, "A", "LocalName");
                TestLog.Compare(elem.Name.Namespace, XNamespace.Get("ns1"), "Namespace");
                TestLog.Compare(elem.Document == null, "Document");
                TestLog.Compare(elem.Element("{ns1}B") != null, "Element");
                TestLog.Compare(elem.Attribute("id") != null, "Attribute");
                elem.Verify();

                // reader sanity
                TestLog.Compare(r.ReadState, ReadState.Interactive, "ReadState");
                TestLog.Compare(r.NodeType, XmlNodeType.ProcessingInstruction, "r.NodeType");
            }
        }

        //[Variation(Priority = 0, Desc = "XElement - from reader - from inner element")]
        public void CreateXElementFromReader2()
        {
            string xml = "<X><A id='a' xmlns='ns1'><B/></A><?PI targ?></X>";
            using (XmlReader r = XmlReader.Create(new StringReader(xml)))
            {
                r.Read();
                r.Read();
                var elem = (XElement)XNode.ReadFrom(r);

                // element sanity
                TestLog.Compare(elem.Name.LocalName, "A", "LocalName");
                TestLog.Compare(elem.Name.Namespace, XNamespace.Get("ns1"), "Namespace");
                TestLog.Compare(elem.Document == null, "Document");
                TestLog.Compare(elem.Element("{ns1}B") != null, "Element");
                TestLog.Compare(elem.Attribute("id") != null, "Attribute");
                elem.Verify();

                // reader sanity
                TestLog.Compare(r.ReadState, ReadState.Interactive, "ReadState");
                TestLog.Compare(r.NodeType, XmlNodeType.ProcessingInstruction, "r.NodeType");
            }
        }

        //[Variation(Priority = 0, Desc = "XElement - from reader - from inner empty element")]

        //[Variation(Priority = 0, Desc = "XElement - from reader - from inner element, namespaces")]
        public void CreateXElementFromReader3()
        {
            string xml = "<X xmlns='ns1' xmlns:p='ns2'><A p:id='a' xmlns:q='ns3'><q:B/></A><?PI targ?></X>";
            using (XmlReader r = XmlReader.Create(new StringReader(xml)))
            {
                r.Read();
                r.Read();
                var elem = (XElement)XNode.ReadFrom(r);

                // element sanity
                TestLog.Compare(elem.Name.LocalName, "A", "LocalName");
                TestLog.Compare(elem.Name.Namespace, XNamespace.Get("ns1"), "Namespace");
                TestLog.Compare(elem.Document == null, "Document");
                TestLog.Compare(elem.Element("{ns3}B") != null, "Element");
                TestLog.Compare(elem.Attribute("{ns2}id") != null, "Attribute");
                elem.Verify();

                // reader sanity
                TestLog.Compare(r.ReadState, ReadState.Interactive, "ReadState");
                TestLog.Compare(r.NodeType, XmlNodeType.ProcessingInstruction, "r.NodeType");
            }
        }

        public void CreateXElementFromReaderEmptyElem()
        {
            string[] xmls = { "<X><A/><B/><?PI targ?></X>", "<X><A id='a'/><B/><?PI targ?></X>", "<X><A xmlms='nas'/><B/><?PI targ?></X>", "<X><A></A><B/><?PI targ?></X>", "<X><A id='a'></A><B/><?PI targ?></X>" };

            foreach (string xml in xmls)
            {
                using (XmlReader r = XmlReader.Create(new StringReader(xml)))
                {
                    r.Read();
                    r.Read();
                    bool isEmpty = r.IsEmptyElement;
                    bool hasAttributes = r.HasAttributes;
                    string namespaceURI = r.NamespaceURI;
                    string localName = r.LocalName;

                    var elem = (XElement)XNode.ReadFrom(r);

                    // element sanity
                    TestLog.Compare(elem.Name.LocalName, "A", "LocalName");
                    TestLog.Compare(elem.Name.LocalName, localName, "LocalName");
                    TestLog.Compare(elem.Name.Namespace.NamespaceName, namespaceURI, "LocalName");
                    TestLog.Compare(elem.HasAttributes, hasAttributes, "hasAttributes");
                    TestLog.Compare(elem.Document == null, "Document");
                    TestLog.Compare(elem.IsEmpty, isEmpty, "isEmpty");
                    elem.Verify();

                    // reader sanity
                    TestLog.Compare(r.ReadState, ReadState.Interactive, "ReadState");
                    TestLog.Compare(r.NodeType, XmlNodeType.Element, "r.NodeType");
                    TestLog.Compare(r.LocalName, "B", "Next element name");
                }
            }
        }

        //[Variation(Priority = 2, Desc = "XElement - from reader - not allowed nodes - initial", Param = 0)]
        //[Variation(Priority = 2, Desc = "XElement - from reader - not allowed nodes - decl", Param = 1)]
        //[Variation(Priority = 2, Desc = "XElement - from reader - not allowed nodes - endElement", Param = 4)]
        //[Variation(Priority = 2, Desc = "XElement - from reader - not allowed nodes - PI", Param = 5)]
        //[Variation(Priority = 2, Desc = "XElement - from reader - not allowed nodes - Comment", Param = 6)]
        //[Variation(Priority = 2, Desc = "XElement - from reader - not allowed nodes - text", Param = 7)]

        // XPI
        //[Variation(Priority = 1, Desc = "XProcessingInstruction - target & data")]
        public void CreateXPI1()
        {
            var pi = new XProcessingInstruction("Target", "data");
            TestLog.Compare(pi != null, "pi != null");
            TestLog.Compare(pi.NodeType, XmlNodeType.ProcessingInstruction, "NodeType");
            TestLog.Compare(pi.Target, "Target", "Target");
            TestLog.Compare(pi.Data, "data", "Data");
            TestLog.Compare(pi.Document == null, "Document");
            TestLog.Compare(pi.Parent == null, "Parent");
            TestLog.Compare(pi.ToString(SaveOptions.DisableFormatting), @"<?Target data?>", "Xml");
        }

        //[Variation(Priority = 1, Desc = "XProcessingInstruction - target only")]
        public void CreateXPI2()
        {
            var pi = new XProcessingInstruction("Target", "");
            TestLog.Compare(pi != null, "pi != null");
            TestLog.Compare(pi.NodeType, XmlNodeType.ProcessingInstruction, "NodeType");
            TestLog.Compare(pi.Target, "Target", "Target");
            TestLog.Compare(pi.Data, "", "Data");
            TestLog.Compare(pi.Document == null, "Document");
            TestLog.Compare(pi.Parent == null, "Parent");
            TestLog.Compare(pi.ToString(SaveOptions.DisableFormatting), @"<?Target?>", "Xml");
        }

        public void CreateXPI3()
        {
            var readCount = (int)Variation.Param;
            string xml = @"<A><?Target data?><B/><?Target?><C/></A>";

            using (XmlReader r = XmlReader.Create(new StringReader(xml)))
            {
                for (int i = 0; i < readCount; i++)
                {
                    r.Read();
                }
                var pi = (XProcessingInstruction)XNode.ReadFrom(r);

                TestLog.Compare(pi != null, "pi != null");
                TestLog.Compare(pi.NodeType, XmlNodeType.ProcessingInstruction, "NodeType");
                TestLog.Compare(pi.Target, "Target", "Target");
                TestLog.Compare(pi.Data, readCount == 2 ? "data" : "", "Data");
                TestLog.Compare(pi.Document == null, "Document");
                TestLog.Compare(pi.Parent == null, "Parent");

                TestLog.Compare(r.ReadState, ReadState.Interactive, "reader state ");
                TestLog.Compare(r.NodeType, XmlNodeType.Element, "reader position");
            }
        }

        //[Variation(Priority = 1, Desc = "XProcessingInstruction - invalid name (invalid char)", Param = "?>")]
        public void CreateXPINegative()
        {
            var name = Variation.Param as string;
            try
            {
                var pi = new XProcessingInstruction(name, "");
                TestLog.Compare(false, "Should fail; name = " + name);
            }
            catch (XmlException)
            {
            }
        }

        //[Variation(Priority = 2, Desc = "XProcessingInstruction - invalid name (empty string)", Param = "")]
        //[Variation(Priority = 2, Desc = "XProcessingInstruction - invalid name (xml)", Param = "xml")]
        //[Variation(Priority = 2, Desc = "XProcessingInstruction - invalid name (xmL)", Param = "xmL")]
        //[Variation(Priority = 2, Desc = "XProcessingInstruction - invalid name (xmL)", Param = "xmL")]
        //[Variation(Priority = 2, Desc = "XProcessingInstruction - invalid name (xML)", Param = "xML")]
        //[Variation(Priority = 2, Desc = "XProcessingInstruction - invalid name (Xml)", Param = "Xml")]
        //[Variation(Priority = 2, Desc = "XProcessingInstruction - invalid name (XmL)", Param = "XmL")]
        //[Variation(Priority = 2, Desc = "XProcessingInstruction - invalid name (XmL)", Param = "XmL")]
        //[Variation(Priority = 2, Desc = "XProcessingInstruction - invalid name (XML)", Param = "XML")]
        public void CreateXPINegative2()
        {
            var name = Variation.Param as string;
            try
            {
                var pi = new XProcessingInstruction(name, "");
                TestLog.Compare(false, "Should fail; name = " + name);
            }
            catch (ArgumentException)
            {
            }
        }

        //[Variation(Priority = 1, Desc = "XProcessingInstruction - from reader - target & data", Param = 2)]
        //[Variation(Priority = 1, Desc = "XProcessingInstruction - from reader - target only", Param = 4)]

        // XText & CData
        //[Variation(Priority = 0, Desc = "XText - from string")]
        public void CreateXText0()
        {
            var text = new XText("MY_TEXT");
            TestLog.Compare(text != null, "text != null");
            TestLog.Compare(text.NodeType, XmlNodeType.Text, "NodeType");
            TestLog.Compare(text.Value, "MY_TEXT", "Value");
            TestLog.Compare(text.Document == null, "Document");
            TestLog.Compare(text.Parent == null, "Parent");
            TestLog.Compare(text.ToString(SaveOptions.DisableFormatting), "MY_TEXT", "Xml");
        }

        //[Variation(Priority = 0, Desc = "XText - copy")]
        public void CreateXText01()
        {
            var text1 = new XText("MY_TEXT");
            var text = new XText(text1);

            TestLog.Compare(text != null, "text != null");
            TestLog.Compare(text != text1, "text != text1");
            TestLog.Compare(XNode.DeepEquals(text, text1), "text.Equals (text1)");
            TestLog.Compare(text.NodeType, XmlNodeType.Text, "NodeType");
            TestLog.Compare(text.Value, "MY_TEXT", "Value");
            TestLog.Compare(text.Document == null, "Document");
            TestLog.Compare(text.Parent == null, "Parent");
            TestLog.Compare(text.ToString(SaveOptions.DisableFormatting), "MY_TEXT", "Xml");
        }

        //[Variation(Priority = 1, Desc = "XText - text", Params = new object[] { XmlNodeType.Text, @"MY_TEXT" })]
        //[Variation(Priority = 1, Desc = "XText - CDATA", Params = new object[] { XmlNodeType.CDATA, @"<![CDATA[MY_TEXT]]>" })]
        public void CreateXText1()
        {
            var nodeType = (XmlNodeType)Variation.Params[0];
            var expectedXml = (string)Variation.Params[1];

            XText cdata = nodeType == XmlNodeType.CDATA ? new XCData("MY_TEXT") : new XText("MY_TEXT");
            TestLog.Compare(cdata != null, "cdata != null");
            TestLog.Compare(cdata.NodeType, nodeType, "NodeType");
            TestLog.Compare(cdata.Value, "MY_TEXT", "Value");
            TestLog.Compare(cdata.Document == null, "Document");
            TestLog.Compare(cdata.Parent == null, "Parent");
            TestLog.Compare(cdata.ToString(SaveOptions.DisableFormatting), expectedXml, "Xml");
        }

        //[Variation(Priority = 3, Desc = "XText - Text - empty", Params = new object[] { XmlNodeType.Text, "" })]
        //[Variation(Priority = 3, Desc = "XText - CData - empty", Params = new object[] { XmlNodeType.CDATA, @"<![CDATA[]]>" })]
        public void CreateXText2()
        {
            var nodeType = (XmlNodeType)Variation.Params[0];
            var expectedXml = (string)Variation.Params[1];
            XText cdata = nodeType == XmlNodeType.CDATA ? new XCData("") : new XText("");
            TestLog.Compare(cdata != null, "cdata != null");
            TestLog.Compare(cdata.NodeType, nodeType, "NodeType");
            TestLog.Compare(cdata.Value, "", "Value");
            TestLog.Compare(cdata.Document == null, "Document");
            TestLog.Compare(cdata.Parent == null, "Parent");
            TestLog.Compare(cdata.ToString(SaveOptions.DisableFormatting), expectedXml, "Xml");
        }

        //[Variation(Priority = 1, Desc = "XText - CDATA - from reader", Params = new object[] { 2, XmlNodeType.CDATA, "MY_TEXT", "<A><![CDATA[MY_TEXT]]><B/>MY_TEXT<C/>\t<D/></A>" })]
        //[Variation(Priority = 1, Desc = "XText - Text - from reader", Params = new object[] { 4, XmlNodeType.Text, "MY_TEXT", "<A><![CDATA[MY_TEXT]]><B/>MY_TEXT<C/>\t<D/></A>" })]
        //[Variation(Priority = 1, Desc = "XText - Text - from reader/ whitespace", Params = new object[] { 6, XmlNodeType.Text, "\t", "<A><![CDATA[MY_TEXT]]><B/>MY_TEXT<C/>\t<D/></A>" })]
        //[Variation(Priority = 1, Desc = "XText - Text - from reader/ significant whitespace", Params = new object[] { 2, XmlNodeType.Text, "\t", "<a xml:space='preserve'>\t<X/></a>" })]
        public void CreateXTextReader()
        {
            var readCount = (int)Variation.Params[0];
            var nodeType = (XmlNodeType)Variation.Params[1];
            var expValue = (string)Variation.Params[2];
            var xml = (string)Variation.Params[3];

            var rs = new XmlReaderSettings();
            rs.IgnoreWhitespace = false;

            using (XmlReader r = XmlReader.Create(new StringReader(xml), rs))
            {
                for (int i = 0; i < readCount; i++)
                {
                    r.Read();
                }
                XText cdata = nodeType == XmlNodeType.CDATA ? (XCData)XNode.ReadFrom(r) : (XText)XNode.ReadFrom(r);
                TestLog.Compare(cdata != null, "cdata != null");
                TestLog.Compare(cdata.NodeType, nodeType, "NodeType");
                TestLog.Compare(cdata.Value, expValue, "Value");
                TestLog.Compare(cdata.Document == null, "Document");
                TestLog.Compare(cdata.Parent == null, "Parent");

                TestLog.Compare(r.ReadState, ReadState.Interactive, "reader state ");
                TestLog.Compare(r.NodeType, XmlNodeType.Element, "reader position");
            }
        }

        // DTD
        //[Variation(Priority = 0, Desc = "DTD - normal", Params = new object[] { new string[] { "root", "a1", "a2", "a3", }, "<!DOCTYPE root PUBLIC \"a1\" \"a2\"[a3]>" })]
        //[Variation(Priority = 1, Desc = "DTD - no systemId", Params = new object[] { new string[] { "root", "a1", null, "a3" }, "<!DOCTYPE root PUBLIC \"a1\" \"\"[a3]>" })]
        //[Variation(Priority = 1, Desc = "DTD - no publicId", Params = new object[] { new string[] { "root", null, "a2", "a3" }, "<!DOCTYPE root SYSTEM \"a2\"[a3]>" })]
        //[Variation(Priority = 1, Desc = "DTD - no publicId, no systemId", Params = new object[] { new string[] { "root", null, null, "a3" }, "<!DOCTYPE root [a3]>" })]
        //[Variation(Priority = 1, Desc = "DTD - all nulls", Params = new object[] { new string[] { "root", null, null, null }, "<!DOCTYPE root >" })]
        public void DTDConstruct()
        {
            var data = Variation.Params[0] as string[];
            var serial = Variation.Params[1] as string;

            var dtd = new XDocumentType(data[0], data[1], data[2], data[3]);
            TestLog.Compare(dtd.Name, data[0], "dtd.Name, data[0]");
            TestLog.Compare(dtd.PublicId, data[1], "dtd.SystemId, data[1]");
            TestLog.Compare(dtd.SystemId, data[2], "dtd.PublicId, data[2]");
            TestLog.Compare(dtd.InternalSubset, data[3], "dtd.InternalSubset, data[3]");
            TestLog.Compare(dtd.NodeType, XmlNodeType.DocumentType, "nodetype");
            TestLog.Compare(dtd.ToString(), serial, "DTD construction");
        }

        public void NotAllowedNodesAttr()
        {
            string xml = "<A id='abc'/>";
            var attributeValue = (bool)Variation.Param;

            using (XmlReader r = XmlReader.Create(new StringReader(xml)))
            {
                r.Read();
                r.MoveToFirstAttribute();
                if (attributeValue)
                {
                    r.ReadAttributeValue();
                }

                try
                {
                    var e = (XElement)XNode.ReadFrom(r);
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }
            throw new TestException(TestResult.Failed, "");
        }

        public void NotAllowedNodesRead()
        {
            string xml = "<?xml version='1.0'?><X><A></A><?PI click?><!--hele-->text</X>";
            var readCount = (int)Variation.Param;

            using (XmlReader r = XmlReader.Create(new StringReader(xml)))
            {
                for (int i = 0; i < readCount; i++)
                {
                    r.Read();
                }
                try
                {
                    var e = (XElement)XNode.ReadFrom(r);
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }
            throw new TestException(TestResult.Failed, "");
        }
        #endregion
    }
}
