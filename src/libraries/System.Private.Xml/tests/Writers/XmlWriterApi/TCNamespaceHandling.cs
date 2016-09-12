// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCNamespaceHandling : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCNamespaceHandling
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter || WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent)
            {
                return;
            }
            // for function NS_Handling_1
            {
                this.AddChild(new CVariation(NS_Handling_1) { Attribute = new Variation("NamespaceHandling Default value - NamespaceHandling.Default") });
            }


            // for function NS_Handling_2
            {
                this.AddChild(new CVariation(NS_Handling_2) { Attribute = new Variation("XmlWriter creation with NamespaceHandling.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_2) { Attribute = new Variation("XmlWriter creation with NamespaceHandling.Default") { Param = 0 } });
            }


            // for function NS_Handling_2a
            {
                this.AddChild(new CVariation(NS_Handling_2a) { Attribute = new Variation("NamespaceHandling = NamespaceHandling.Default | NamespaceHandling.OmitDuplicates") });
            }


            // for function NS_Handling_3
            {
                this.AddChild(new CVariation(NS_Handling_3) { Attribute = new Variation("NamespaceHandling override with Default.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_3) { Attribute = new Variation("NamespaceHandling override with Default.Default") { Param = 0 } });
            }


            // for function NS_Handling_3a
            {
                this.AddChild(new CVariation(NS_Handling_3a) { Attribute = new Variation("NamespaceHandling override with negativeVal.Default") { Param = 0 } });
                this.AddChild(new CVariation(NS_Handling_3a) { Attribute = new Variation("NamespaceHandling override with negativeVal.OmitDuplicates") { Param = 1 } });
            }


            // for function NS_Handling_3b
            {
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("1.NamespaceHandling.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<root><p:foo xmlns:p=\"uri\"><a xmlns:p=\"uri\" /></p:foo></root>", "<root><p:foo xmlns:p=\"uri\"><a /></p:foo></root>" } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("9.Unused namespace declarations.Default") { Params = new object[] { NamespaceHandling.Default, "<root> <elem1 xmlns=\"urn:namespace\" att1=\"foo\" /></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("9.Unused namespace declarations.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<root> <elem1 xmlns=\"urn:namespace\" att1=\"foo\" /></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("10.NamespaceHandling.Same NS.Diff prefix.Default") { Params = new object[] { NamespaceHandling.Default, "<a xmlns:p=\"p1\"><b xmlns:a=\"p1\"><c xmlns:b=\"p1\" /></b></a>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("8.NamespaceHandling.Default NS Redefinition.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<a xmlns=\"p1\"><b xmlns=\"p2\"><c xmlns=\"p1\" /></b><d xmlns=\"\"><e xmlns=\"p1\"><f xmlns=\"\" /></e></d></a>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("10.NamespaceHandling.Same NS.Diff prefix.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<a xmlns:p=\"p1\"><b xmlns:a=\"p1\"><c xmlns:b=\"p1\" /></b></a>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("11.NamespaceHandling.Diff NS.Same prefix.Default") { Params = new object[] { NamespaceHandling.Default, "<a xmlns:p=\"p1\"><b xmlns:p=\"p2\"><c xmlns:p=\"p3\" /></b></a>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("11.NamespaceHandling.Diff NS.Same prefix.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<a xmlns:p=\"p1\"><b xmlns:p=\"p2\"><c xmlns:p=\"p3\" /></b></a>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("1.NamespaceHandling.Default") { Params = new object[] { NamespaceHandling.Default, "<root><p:foo xmlns:p=\"uri\"><a xmlns:p=\"uri\" /></p:foo></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("12.NamespaceHandling.NS Redefinition.Unused NS.Default") { Params = new object[] { NamespaceHandling.Default, "<a xmlns:p=\"p1\"><b xmlns:p=\"p2\"><c xmlns:p=\"p1\" /></b></a>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("2.NamespaceHandling.Default") { Params = new object[] { NamespaceHandling.Default, "<root><foo xmlns=\"uri\"><a xmlns=\"uri\" /></foo></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("2.NamespaceHandling.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<root><foo xmlns=\"uri\"><a xmlns=\"uri\" /></foo></root>", "<root><foo xmlns=\"uri\"><a /></foo></root>" } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("3.NamespaceHandling.Default") { Params = new object[] { NamespaceHandling.Default, "<root><p:foo xmlns:p=\"uri\"><a xmlns:p=\"uriOther\" /></p:foo></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("3.NamespaceHandling.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<root><p:foo xmlns:p=\"uri\"><a xmlns:p=\"uriOther\" /></p:foo></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("12.NamespaceHandling.NS Redefinition.Unused NS.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<a xmlns:p=\"p1\"><b xmlns:p=\"p2\"><c xmlns:p=\"p1\" /></b></a>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("4.NamespaceHandling.Default") { Params = new object[] { NamespaceHandling.Default, "<root><p:foo xmlns:p=\"uri\"><a xmlns:pOther=\"uri\" /></p:foo></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("4.NamespaceHandling.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<root><p:foo xmlns:p=\"uri\"><a xmlns:pOther=\"uri\" /></p:foo></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("5.NamespaceHandling.Default") { Params = new object[] { NamespaceHandling.Default, "<root xmlns:p=\"uri\"><p:foo><p:a xmlns:p=\"uri\" /></p:foo></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("5.NamespaceHandling.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<root xmlns:p=\"uri\"><p:foo><p:a xmlns:p=\"uri\" /></p:foo></root>", "<root xmlns:p=\"uri\"><p:foo><p:a /></p:foo></root>" } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("6.NamespaceHandling.Default") { Params = new object[] { NamespaceHandling.Default, "<root xmlns:p=\"uri\"><p:foo><a xmlns:p=\"uri\" /></p:foo></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("6.NamespaceHandling.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<root xmlns:p=\"uri\"><p:foo><a xmlns:p=\"uri\" /></p:foo></root>", "<root xmlns:p=\"uri\"><p:foo><a /></p:foo></root>" } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("7.NamespaceHandling.Default") { Params = new object[] { NamespaceHandling.Default, "<root><p xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" /></root>", null } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("7.NamespaceHandling.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "<root><p xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" /></root>", "<root><p /></root>" } } });
                this.AddChild(new CVariation(NS_Handling_3b) { Attribute = new Variation("8.NamespaceHandling.Default NS Redefinition.Default") { Params = new object[] { NamespaceHandling.Default, "<a xmlns=\"p1\"><b xmlns=\"p2\"><c xmlns=\"p1\" /></b><d xmlns=\"\"><e xmlns=\"p1\"><f xmlns=\"\" /></e></d></a>", null } } });
            }


            // for function NS_Handling_4a
            {
                this.AddChild(new CVariation(NS_Handling_4a) { Attribute = new Variation("NamespaceHandling wrap with Default.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_4a) { Attribute = new Variation("NamespaceHandling wrap with Default.Default") { Param = 0 } });
            }


            // for function NS_Handling_4b
            {
                this.AddChild(new CVariation(NS_Handling_4b) { Attribute = new Variation("NamespaceHandling wrap with OmitDuplicates.Default") { Param = 0 } });
                this.AddChild(new CVariation(NS_Handling_4b) { Attribute = new Variation("NamespaceHandling wrap with OmitDuplicates.OmitDuplicates") { Param = 1 } });
            }


            // for function NS_Handling_5
            {
                this.AddChild(new CVariation(NS_Handling_5) { Attribute = new Variation("XmlWriter.WriteStartElement() should inspect attributes before emitting the element tag.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_5) { Attribute = new Variation("XmlWriter.WriteStartElement() should inspect attributes before emitting the element tag.Default") { Param = 0 } });
            }


            // for function NS_Handling_6
            {
                this.AddChild(new CVariation(NS_Handling_6) { Attribute = new Variation("WriteStartElement,AttrString with null prefix.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_6) { Attribute = new Variation("WriteStartElement,AttrString with null prefix.Default") { Param = 0 } });
            }


            // for function NS_Handling_7
            {
                this.AddChild(new CVariation(NS_Handling_7) { Attribute = new Variation("WriteStartElement,AttrString with empty prefix.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_7) { Attribute = new Variation("WriteStartElement,AttrString with empty prefix.Default") { Param = 0 } });
            }


            // for function NS_Handling_8
            {
                this.AddChild(new CVariation(NS_Handling_8) { Attribute = new Variation("WriteStartElement,AttrString with not null prefix and namespace.Default") { Param = 0 } });
                this.AddChild(new CVariation(NS_Handling_8) { Attribute = new Variation("WriteStartElement,AttrString with not null prefix and namespace.OmitDuplicates") { Param = 1 } });
            }


            // for function NS_Handling_9
            {
                this.AddChild(new CVariation(NS_Handling_9) { Attribute = new Variation("WriteStartElement,AttrString without prefix.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_9) { Attribute = new Variation("WriteStartElement,AttrString without prefix.Default") { Param = 0 } });
            }


            // for function NS_Handling_10
            {
                this.AddChild(new CVariation(NS_Handling_10) { Attribute = new Variation("WriteStartElement,AttrString with null namespace,prefix.Default") { Param = 0 } });
                this.AddChild(new CVariation(NS_Handling_10) { Attribute = new Variation("WriteStartElement,AttrString with null namespace,prefix.OmitDuplicates") { Param = 1 } });
            }


            // for function NS_Handling_11
            {
                this.AddChild(new CVariation(NS_Handling_11) { Attribute = new Variation("WriteStartElement,AttrString with empty namespace,prefix.Default") { Param = 0 } });
                this.AddChild(new CVariation(NS_Handling_11) { Attribute = new Variation("WriteStartElement,AttrString with empty namespace,prefix.OmitDuplicates") { Param = 1 } });
            }


            // for function NS_Handling_12
            {
                this.AddChild(new CVariation(NS_Handling_12) { Attribute = new Variation("WriteStartElement,AttrString without namespace,prefix.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_12) { Attribute = new Variation("WriteStartElement,AttrString without namespace,prefix.Default") { Param = 0 } });
            }


            // for function NS_Handling_16
            {
                this.AddChild(new CVariation(NS_Handling_16) { Attribute = new Variation("LookupPrefix.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_16) { Attribute = new Variation("LookupPrefix.Default") { Param = 0 } });
            }


            // for function NS_Handling_17
            {
                this.AddChild(new CVariation(NS_Handling_17) { Attribute = new Variation("WriteAttributeString with dup.namespace,w/o prefix.Default") { Param = 0 } });
                this.AddChild(new CVariation(NS_Handling_17) { Attribute = new Variation("WriteAttributeString with dup.namespace,w/o prefix.OmitDuplicates") { Param = 1 } });
            }


            // for function NS_Handling_17a
            {
                this.AddChild(new CVariation(NS_Handling_17a) { Attribute = new Variation("WriteElementString with prefix bind to the same ns.Default") { Params = new object[] { NamespaceHandling.Default, false } } });
                this.AddChild(new CVariation(NS_Handling_17a) { Attribute = new Variation("WriteAttributeString with prefix bind to the same ns.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, true } } });
                this.AddChild(new CVariation(NS_Handling_17a) { Attribute = new Variation("WriteElementString with prefix bind to the same ns.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, false } } });
                this.AddChild(new CVariation(NS_Handling_17a) { Attribute = new Variation("WriteAttributeString with prefix bind to the same ns.Default") { Params = new object[] { NamespaceHandling.Default, true } } });
            }


            // for function NS_Handling_17b
            {
                this.AddChild(new CVariation(NS_Handling_17b) { Attribute = new Variation("WriteAttributeString with prefix bind to default ns.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, true } } });
                this.AddChild(new CVariation(NS_Handling_17b) { Attribute = new Variation("WriteElementString with prefix bind to default ns.Default") { Params = new object[] { NamespaceHandling.Default, false } } });
                this.AddChild(new CVariation(NS_Handling_17b) { Attribute = new Variation("WriteElementString with prefix bind to default ns.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, false } } });
                this.AddChild(new CVariation(NS_Handling_17b) { Attribute = new Variation("WriteAttributeString with prefix bind to default ns.Default") { Params = new object[] { NamespaceHandling.Default, true } } });
            }


            // for function NS_Handling_17c
            {
                this.AddChild(new CVariation(NS_Handling_17c) { Attribute = new Variation("WriteElementString with prefix bind to non-default ns.dup.namespace.Default") { Params = new object[] { NamespaceHandling.Default, false } } });
                this.AddChild(new CVariation(NS_Handling_17c) { Attribute = new Variation("WriteAttributeString with prefix bind to non-default ns.dup.namespace.Default") { Params = new object[] { NamespaceHandling.Default, true } } });
                this.AddChild(new CVariation(NS_Handling_17c) { Attribute = new Variation("WriteElementString with prefix bind to non-default ns.dup.namespace.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, false } } });
                this.AddChild(new CVariation(NS_Handling_17c) { Attribute = new Variation("WriteAttributeString with prefix bind to non-default ns.dup.namespace.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, true } } });
            }


            // for function NS_Handling_17d
            {
                this.AddChild(new CVariation(NS_Handling_17d) { Attribute = new Variation("WriteElementString with prefix bind to ns uri.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, false } } });
                this.AddChild(new CVariation(NS_Handling_17d) { Attribute = new Variation("WriteElementString with prefix bind to ns uri.Default") { Params = new object[] { NamespaceHandling.Default, false } } });
                this.AddChild(new CVariation(NS_Handling_17d) { Attribute = new Variation("WriteAttributeString with prefix bind to ns uri.Default") { Params = new object[] { NamespaceHandling.Default, true } } });
                this.AddChild(new CVariation(NS_Handling_17d) { Attribute = new Variation("WriteAttributeString with prefix bind to ns uri.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, true } } });
            }


            // for function NS_Handling_17e
            {
                this.AddChild(new CVariation(NS_Handling_17e) { Attribute = new Variation("WriteAttributeString with ns uri.Default") { Params = new object[] { NamespaceHandling.Default, true } } });
                this.AddChild(new CVariation(NS_Handling_17e) { Attribute = new Variation("WriteElementString with ns uri.Default") { Params = new object[] { NamespaceHandling.Default, false } } });
                this.AddChild(new CVariation(NS_Handling_17e) { Attribute = new Variation("WriteElementString with ns uri.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, false } } });
                this.AddChild(new CVariation(NS_Handling_17e) { Attribute = new Variation("WriteAttributeString with ns uri.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, true } } });
            }


            // for function NS_Handling_18
            {
                this.AddChild(new CVariation(NS_Handling_18) { Attribute = new Variation("WriteAttribute/ElemString with prefixes.namespace.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_18) { Attribute = new Variation("WriteAttribute/ElemString with prefixes.namespace.Default") { Param = 0 } });
            }


            // for function NS_Handling_19
            {
                this.AddChild(new CVariation(NS_Handling_19) { Attribute = new Variation("WriteAttribute/ElemString with xmlns:xml,space,lang.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_19) { Attribute = new Variation("WriteAttribute/ElemString with xmlns:xml,space,lang.Default") { Param = 0 } });
            }


            // for function NS_Handling_19a
            {
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.attr=xmlns:lang.Default") { Params = new object[] { NamespaceHandling.Default, "xmlns", "lang", true } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.attr=xmlns:lang.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "xmlns", "lang", true } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.attr=xmlns:xml.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "xmlns", "xml", true } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.elem=xmlns:xml.Default") { Params = new object[] { NamespaceHandling.Default, "xmlns", "xml", false } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.elem=xmlns:xml.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "xmlns", "xml", false } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.attr=xml:space.Default") { Params = new object[] { NamespaceHandling.Default, "xml", "space", true } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.attr=xml:space.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "xml", "space", true } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.elem=xmlns:space.Default") { Params = new object[] { NamespaceHandling.Default, "xmlns", "space", false } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.elem=xmlns:space.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "xmlns", "space", false } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.attr=xmlns:xml.Default") { Params = new object[] { NamespaceHandling.Default, "xmlns", "xml", true } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.elem=xmlns:lang.Default") { Params = new object[] { NamespaceHandling.Default, "xmlns", "lang", false } } });
                this.AddChild(new CVariation(NS_Handling_19a) { Attribute = new Variation("WriteAttributeString with null val,ns.elem=xmlns:lang.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, "xmlns", "lang", false } } });
            }


            // for function NS_Handling_19b
            {
                this.AddChild(new CVariation(NS_Handling_19b) { Attribute = new Variation("WriteAttributeString in error state.Default") { Params = new object[] { NamespaceHandling.Default, true } } });
                this.AddChild(new CVariation(NS_Handling_19b) { Attribute = new Variation("WriteElementString in error state.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, false } } });
                this.AddChild(new CVariation(NS_Handling_19b) { Attribute = new Variation("WriteElementString in error state.Default") { Params = new object[] { NamespaceHandling.Default, false } } });
                this.AddChild(new CVariation(NS_Handling_19b) { Attribute = new Variation("WriteAttributeString in error state.OmitDuplicates") { Params = new object[] { NamespaceHandling.OmitDuplicates, true } } });
            }


            // for function NS_Handling_20
            {
                this.AddChild(new CVariation(NS_Handling_20) { Attribute = new Variation("WriteAttributeString,StartElement with hello:worldl.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_20) { Attribute = new Variation("WriteAttributeString,StartElement with hello:world.Default") { Param = 0 } });
            }


            // for function NS_Handling_21
            {
                this.AddChild(new CVariation(NS_Handling_21) { Attribute = new Variation("WriteStartAttribute(xml:lang),WriteRaw(0,0).OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_21) { Attribute = new Variation("WriteStartAttribute(xml:lang),WriteRaw(0,0).Default") { Param = 0 } });
            }


            // for function NS_Handling_22
            {
                this.AddChild(new CVariation(NS_Handling_22) { Attribute = new Variation("WriteStartAttribute(xml:lang),WriteBinHex(0,0).Default") { Param = 0 } });
                this.AddChild(new CVariation(NS_Handling_22) { Attribute = new Variation("WriteStartAttribute(xml:lang),WriteBinHex(0,0).OmitDuplicates") { Param = 1 } });
            }


            // for function NS_Handling_23
            {
                this.AddChild(new CVariation(NS_Handling_23) { Attribute = new Variation("WriteStartAttribute(xml:lang),WriteBase64(0,0).Default") { Param = 0 } });
                this.AddChild(new CVariation(NS_Handling_23) { Attribute = new Variation("WriteStartAttribute(xml:lang),WriteBase64(0,0).OmitDuplicates") { Param = 1 } });
            }


            // for function NS_Handling_24
            {
                this.AddChild(new CVariation(NS_Handling_24) { Attribute = new Variation("Duplicate attribute conflict when the namespace decl. is being omitted.Default") { Param = 0 } });
                this.AddChild(new CVariation(NS_Handling_24) { Attribute = new Variation("Duplicate attribute conflict when the namespace decl. is being omitted.OmitDuplicates") { Param = 1 } });
            }


            // for function NS_Handling_25
            {
                this.AddChild(new CVariation(NS_Handling_25) { Attribute = new Variation("1.Namespace redefinition.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_25) { Attribute = new Variation("1.Namespace redefinition.Default") { Param = 0 } });
            }


            // for function NS_Handling_25a
            {
                this.AddChild(new CVariation(NS_Handling_25a) { Attribute = new Variation("2.Default Namespace redefinition.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_25a) { Attribute = new Variation("2.Default Namespace redefinition.Default") { Param = 0 } });
            }


            // for function NS_Handling_26
            {
                this.AddChild(new CVariation(NS_Handling_26) { Attribute = new Variation("Namespaces with the same prefix different NS value.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_26) { Attribute = new Variation("Namespaces with the same prefix different NS value.Default") { Param = 0 } });
            }


            // for function NS_Handling_27
            {
                this.AddChild(new CVariation(NS_Handling_27) { Attribute = new Variation("Namespaces with the same NS value but different prefixes.Default") { Param = 0 } });
                this.AddChild(new CVariation(NS_Handling_27) { Attribute = new Variation("Namespaces with the same NS value but different prefixes.OmitDuplicates") { Param = 1 } });
            }


            // for function NS_Handling_31
            {
                this.AddChild(new CVariation(NS_Handling_31) { Attribute = new Variation("XmlTextWriter : Wrong prefix management if the same prefix is used at inner level.OmitDuplicates") { Param = 1 } });
                this.AddChild(new CVariation(NS_Handling_31) { Attribute = new Variation("XmlTextWriter : Wrong prefix management if the same prefix is used at inner level.Default") { Param = 0 } });
            }
        }
    }
}
