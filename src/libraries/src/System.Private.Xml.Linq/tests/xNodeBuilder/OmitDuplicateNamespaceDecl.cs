// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;
using System.IO;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class XNodeBuilderTests : XLinqTestCase
        {
            public partial class NamespacehandlingWriterSanity : XLinqTestCase
            {
                #region helpers
                private string SaveXElementUsingXmlWriter(XElement elem, NamespaceHandling nsHandling)
                {
                    StringWriter sw = new StringWriter();
                    using (XmlWriter w = XmlWriter.Create(sw, new XmlWriterSettings() { NamespaceHandling = nsHandling, OmitXmlDeclaration = true }))
                    {
                        elem.WriteTo(w);
                    }
                    sw.Dispose();
                    return sw.ToString();
                }

                #endregion
                //[Variation(Desc = "1 level down", Priority = 0, Params = new object[] { "<p:A xmlns:p='nsp'><p:B xmlns:p='nsp'><p:C xmlns:p='nsp'/></p:B></p:A>" })]
                //[Variation(Desc = "1 level down II.", Priority = 0, Params = new object[] { "<A><p:B xmlns:p='nsp'><p:C xmlns:p='nsp'/></p:B></A>" })]  // start at not root node
                //[Variation(Desc = "2 levels down", Priority = 1, Params = new object[] { "<p:A xmlns:p='nsp'><B><p:C xmlns:p='nsp'/></B></p:A>" })]
                //[Variation(Desc = "2 levels down II.", Priority = 1, Params = new object[] { "<p:A xmlns:p='nsp'><B><C xmlns:p='nsp'/></B></p:A>" })]
                //[Variation(Desc = "2 levels down III.", Priority = 1, Params = new object[] { "<A xmlns:p='nsp'><B><p:C xmlns:p='nsp'/></B></A>" })]
                //[Variation(Desc = "Siblings", Priority = 2, Params = new object[] { "<A xmlns:p='nsp'><p:B xmlns:p='nsp'/><C xmlns:p='nsp'/><p:C xmlns:p='nsp'/></A>" })]
                //[Variation(Desc = "Children", Priority = 2, Params = new object[] { "<A xmlns:p='nsp'><p:B xmlns:p='nsp'><C xmlns:p='nsp'><p:C xmlns:p='nsp'/></C></p:B></A>" })]
                //[Variation(Desc = "Xml namespace I.", Priority = 3, Params = new object[] { "<A xmlns:xml='http://www.w3.org/XML/1998/namespace'/>" })]
                //[Variation(Desc = "Xml namespace II.", Priority = 3, Params = new object[] { "<p:A xmlns:p='nsp'><p:B xmlns:p='nsp'><p:C xmlns:p='nsp' xmlns:xml='http://www.w3.org/XML/1998/namespace'/></p:B></p:A>" })]
                //[Variation(Desc = "Xml namespace III.", Priority = 3, Params = new object[] { "<p:A xmlns:p='nsp'><p:B xmlns:xml='http://www.w3.org/XML/1998/namespace' xmlns:p='nsp'><p:C xmlns:p='nsp' xmlns:xml='http://www.w3.org/XML/1998/namespace'/></p:B></p:A>" })]
                //[Variation(Desc = "Default namespaces", Priority = 1, Params = new object[] { "<A xmlns='nsp'><p:B xmlns:p='nsp'><C xmlns='nsp' /></p:B></A>" })]
                //[Variation(Desc = "Not used NS declarations", Priority = 2, Params = new object[] { "<A xmlns='nsp' xmlns:u='not-used'><p:B xmlns:p='nsp'><C xmlns:u='not-used' xmlns='nsp' /></p:B></A>" })]
                //[Variation(Desc = "SameNS, different prefix", Priority = 2, Params = new object[] { "<p:A xmlns:p='nsp'><B xmlns:q='nsp'><p:C xmlns:p='nsp'/></B></p:A>" })]     
                public void testFromTheRootNodeSimple()
                {
                    string xml = CurrentChild.Params[0] as string;
                    XElement elem = XElement.Parse(xml);

                    // Write using XmlWriter in duplicate namespace decl. removal mode
                    string removedByWriter = SaveXElementUsingXmlWriter(elem, NamespaceHandling.OmitDuplicates);

                    // Remove the namespace decl. duplicates from the Xlinq tree
                    (from a in elem.DescendantsAndSelf().Attributes()
                     where a.IsNamespaceDeclaration && ((a.Name.LocalName == "xml" && (string)a == XNamespace.Xml.NamespaceName) ||
                                                         (from parentDecls in a.Parent.Ancestors().Attributes(a.Name)
                                                          where parentDecls.IsNamespaceDeclaration && (string)parentDecls == (string)a
                                                          select parentDecls).Any()
                                                       )
                     select a).ToList().Remove();

                    // Write XElement using XmlWriter without omitting
                    string removedByManual = SaveXElementUsingXmlWriter(elem, NamespaceHandling.Default);

                    ReaderDiff.Compare(removedByWriter, removedByManual);
                }

                //[Variation(Desc = "Default ns parent autogenerated", Priority = 1)]
                public void testFromTheRootNodeTricky()
                {
                    XElement e = new XElement("{nsp}A", new XElement("{nsp}B", new XAttribute("xmlns", "nsp")));
                    ReaderDiff.Compare(SaveXElementUsingXmlWriter(e, NamespaceHandling.OmitDuplicates), "<A xmlns='nsp'><B/></A>");
                }

                //[Variation(Desc = "Conflicts: NS redefinition", Priority = 2, Params = new object[] {   "<p:A xmlns:p='nsp'><p:B xmlns:p='ns-other'><p:C xmlns:p='nsp'><D xmlns:p='nsp'/></p:C></p:B></p:A>",
                //                                                                                        "<p:A xmlns:p='nsp'><p:B xmlns:p='ns-other'><p:C xmlns:p='nsp'><D/></p:C></p:B></p:A>" })]
                //[Variation(Desc = "Conflicts: NS redefinition, default NS", Priority = 2, Params = new object[] {   "<A xmlns='nsp'><B xmlns='ns-other'><C xmlns='nsp'><D xmlns='nsp'/></C></B></A>", 
                //                                                                                                    "<A xmlns='nsp'><B xmlns='ns-other'><C xmlns='nsp'><D/></C></B></A>" })]
                //[Variation(Desc = "Conflicts: NS redefinition, default NS II.", Priority = 2, Params = new object[] {   "<A xmlns=''><B xmlns='ns-other'><C xmlns=''><D xmlns=''/></C></B></A>", 
                //                                                                                                        "<A><B xmlns='ns-other'><C xmlns=''><D/></C></B></A>" })]
                //[Variation(Desc = "Conflicts: NS undeclaration, default NS", Priority = 2, Params = new object[] {  "<A xmlns='nsp'><B xmlns=''><C xmlns='nsp'><D xmlns='nsp'/></C></B></A>", 
                //                                                                                                    "<A xmlns='nsp'><B xmlns=''><C xmlns='nsp'><D/></C></B></A>" })]
                public void testConflicts()
                {
                    XElement e1 = XElement.Parse(CurrentChild.Params[0] as string);
                    ReaderDiff.Compare(SaveXElementUsingXmlWriter(e1, NamespaceHandling.OmitDuplicates), CurrentChild.Params[1] as string);
                }

                //[Variation(Desc = "Not from root", Priority = 1)]
                public void testFromChildNode1()
                {
                    XElement e = new XElement("root",
                        new XAttribute(XNamespace.Xmlns + "p1", "nsp"),
                        new XElement("{nsp}A",
                            new XElement("{nsp}B",
                                new XAttribute("xmlns", "nsp"))));
                    ReaderDiff.Compare(SaveXElementUsingXmlWriter(e.Element("{nsp}A"), NamespaceHandling.OmitDuplicates), "<p1:A xmlns:p1='nsp'><B xmlns='nsp'/></p1:A>");
                }

                //[Variation(Desc = "Not from root II.", Priority = 1)]
                public void testFromChildNode2()
                {
                    XElement e = new XElement("root",
                        new XAttribute(XNamespace.Xmlns + "p1", "nsp"),
                        new XElement("{nsp}A",
                            new XElement("{nsp}B",
                                new XAttribute(XNamespace.Xmlns + "p1", "nsp"))));
                    ReaderDiff.Compare(SaveXElementUsingXmlWriter(e.Element("{nsp}A"), NamespaceHandling.OmitDuplicates), "<p1:A xmlns:p1='nsp'><p1:B/></p1:A>");
                }

                //[Variation(Desc = "Not from root III.", Priority = 2)]
                public void testFromChildNode3()
                {
                    XElement e = new XElement("root",
                        new XAttribute(XNamespace.Xmlns + "p1", "nsp"),
                        new XElement("{nsp}A",
                            new XElement("{nsp}B",
                                new XAttribute(XNamespace.Xmlns + "p1", "nsp"))));
                    ReaderDiff.Compare(SaveXElementUsingXmlWriter(e.Descendants("{nsp}B").FirstOrDefault(), NamespaceHandling.OmitDuplicates), "<p1:B xmlns:p1='nsp'/>");
                }

                //[Variation(Desc = "Not from root IV.", Priority = 2)]
                public void testFromChildNode4()
                {
                    XElement e = new XElement("root",
                        new XAttribute(XNamespace.Xmlns + "p1", "nsp"),
                        new XElement("{nsp}A",
                            new XElement("{nsp}B")));
                    ReaderDiff.Compare(SaveXElementUsingXmlWriter(e.Descendants("{nsp}B").FirstOrDefault(), NamespaceHandling.OmitDuplicates), "<p1:B xmlns:p1='nsp'/>");
                }

                //[Variation(Desc = "Write into used reader I.", Priority = 0, Params = new object[] { "<A xmlns:p1='nsp'/>", "<p1:root xmlns:p1='nsp'><A/></p1:root>" })]
                //[Variation(Desc = "Write into used reader II.", Priority = 2, Params = new object[] { "<p1:A xmlns:p1='nsp'/>", "<p1:root xmlns:p1='nsp'><p1:A/></p1:root>" })]
                //[Variation(Desc = "Write into used reader III.", Priority = 2, Params = new object[] { "<p1:A xmlns:p1='nsp'><B xmlns:p1='nsp'/></p1:A>", "<p1:root xmlns:p1='nsp'><p1:A><B/></p1:A></p1:root>" })]
                public void testIntoOpenedWriter()
                {

                    XElement e = XElement.Parse(CurrentChild.Params[0] as string);

                    StringWriter sw = new StringWriter();
                    using (XmlWriter w = XmlWriter.Create(sw, new XmlWriterSettings() { NamespaceHandling = NamespaceHandling.OmitDuplicates, OmitXmlDeclaration = true }))
                    {
                        // prepare writer
                        w.WriteStartDocument();
                        w.WriteStartElement("p1", "root", "nsp");
                        // write xelement
                        e.WriteTo(w);
                        // close the prep. lines
                        w.WriteEndElement();
                        w.WriteEndDocument();
                    }
                    sw.Dispose();

                    ReaderDiff.Compare(sw.ToString(), CurrentChild.Params[1] as string);
                }

                //[Variation(Desc = "Write into used reader I. (def. ns.)", Priority = 0, Params = new object[] { "<A xmlns='nsp'/>", "<root xmlns='nsp'><A/></root>" })]
                //[Variation(Desc = "Write into used reader II. (def. ns.)", Priority = 2, Params = new object[] { "<A xmlns='ns-other'><B xmlns='nsp'><C xmlns='nsp'/></B></A>", 
                //                                                                                                 "<root xmlns='nsp'><A xmlns='ns-other'><B xmlns='nsp'><C/></B></A></root>" })]
                public void testIntoOpenedWriterDefaultNS()
                {
                    XElement e = XElement.Parse(CurrentChild.Params[0] as string);

                    StringWriter sw = new StringWriter();
                    using (XmlWriter w = XmlWriter.Create(sw, new XmlWriterSettings() { NamespaceHandling = NamespaceHandling.OmitDuplicates, OmitXmlDeclaration = true }))
                    {
                        // prepare writer
                        w.WriteStartDocument();
                        w.WriteStartElement("", "root", "nsp");
                        // write xelement
                        e.WriteTo(w);
                        // close the prep. lines
                        w.WriteEndElement();
                        w.WriteEndDocument();
                    }
                    sw.Dispose();

                    ReaderDiff.Compare(sw.ToString(), CurrentChild.Params[1] as string);
                }


                //[Variation(Desc = "Write into used reader (Xlinq lookup + existing hint in the Writer; different prefix)", 
                //          Priority = 2, 
                //           Params = new object[] { "<p1:root xmlns:p1='nsp'><p2:B xmlns:p2='nsp'/></p1:root>" })]
                public void testIntoOpenedWriterXlinqLookup1()
                {
                    XElement e = new XElement("A",
                                    new XAttribute(XNamespace.Xmlns + "p2", "nsp"),
                                    new XElement("{nsp}B"));

                    StringWriter sw = new StringWriter();
                    using (XmlWriter w = XmlWriter.Create(sw, new XmlWriterSettings() { NamespaceHandling = NamespaceHandling.OmitDuplicates, OmitXmlDeclaration = true }))
                    {
                        // prepare writer
                        w.WriteStartDocument();
                        w.WriteStartElement("p1", "root", "nsp");
                        // write xelement
                        e.Element("{nsp}B").WriteTo(w);
                        // close the prep. lines
                        w.WriteEndElement();
                        w.WriteEndDocument();
                    }
                    sw.Dispose();

                    ReaderDiff.Compare(sw.ToString(), CurrentChild.Params[0] as string);
                }

                //[Variation(Desc = "Write into used reader (Xlinq lookup + existing hint in the Writer; same prefix)",
                //   Priority = 2,
                //   Params = new object[] { "<p1:root xmlns:p1='nsp'><p1:B /></p1:root>" })]
                public void testIntoOpenedWriterXlinqLookup2()
                {
                    XElement e = new XElement("A",
                                    new XAttribute(XNamespace.Xmlns + "p1", "nsp"),
                                    new XElement("{nsp}B"));

                    StringWriter sw = new StringWriter();
                    using (XmlWriter w = XmlWriter.Create(sw, new XmlWriterSettings() { NamespaceHandling = NamespaceHandling.OmitDuplicates, OmitXmlDeclaration = true }))
                    {
                        // prepare writer
                        w.WriteStartDocument();
                        w.WriteStartElement("p1", "root", "nsp");
                        // write xelement
                        e.Element("{nsp}B").WriteTo(w);
                        // close the prep. lines
                        w.WriteEndElement();
                        w.WriteEndDocument();
                    }
                    sw.Dispose();

                    ReaderDiff.Compare(sw.ToString(), CurrentChild.Params[0] as string);
                }
            }
        }
    }
}
