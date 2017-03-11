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
using System.Reflection;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class XNodeBuilderTests : XLinqTestCase
        {
            public partial class NamespacehandlingSaveOptions : XLinqTestCase
            {
                private string _mode;
                private Type _type;

                public override void Init()
                {
                    _mode = Params[0] as string;
                    _type = Params[1] as Type;
                }

                #region helpers

                private void DumpMethodInfo(MethodInfo mi)
                {
                    TestLog.WriteLineIgnore("Method with the params found: " + mi.Name);
                    foreach (var pi in mi.GetParameters())
                    {
                        TestLog.WriteLineIgnore(" name: " + pi.Name + " Type: " + pi.ParameterType);
                    }
                }

                private string SaveXElement(object elem, SaveOptions so)
                {
                    string retVal = null;
                    switch (_mode)
                    {
                        case "Save":
                            using (StringWriter sw = new StringWriter())
                            {
                                if (_type.Name == "XElement")
                                {
                                    (elem as XElement).Save(sw, so);
                                }
                                else if (_type.Name == "XDocument")
                                {
                                    (elem as XDocument).Save(sw, so);
                                }
                                retVal = sw.ToString();
                            }
                            break;
                        case "ToString":
                            if (_type.Name == "XElement")
                            {
                                retVal = (elem as XElement).ToString(so);
                            }
                            else if (_type.Name == "XDocument")
                            {
                                retVal = (elem as XDocument).ToString(so);
                            }
                            break;
                        default:
                            TestLog.Compare(false, "TEST FAILED: wrong mode");
                            break;
                    }
                    return retVal;
                }

                private string AppendXmlDecl(string xml)
                {
                    return _mode == "Save" ? GetXmlStringWithXmlDecl(xml) : xml;
                }

                private static string GetXmlStringWithXmlDecl(string xml)
                {
                    return @"<?xml version='1.0' encoding='utf-16'?>" + xml;
                }

                private object Parse(string xml)
                {
                    object o = null;
                    if (_type.Name == "XElement")
                    {
                        o = XElement.Parse(xml);
                    }
                    else if (_type.Name == "XDocument")
                    {
                        o = XDocument.Parse(xml);
                    }
                    return o;
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
                    Object elemObj = Parse(xml);

                    // Write using XmlWriter in duplicate namespace decl. removal mode
                    string removedByWriter = SaveXElement(elemObj, SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting);

                    XElement elem = (elemObj is XDocument) ? (elemObj as XDocument).Root : elemObj as XElement;
                    // Remove the namespace decl. duplicates from the Xlinq tree
                    (from a in elem.DescendantsAndSelf().Attributes()
                     where a.IsNamespaceDeclaration && ((a.Name.LocalName == "xml" && (string)a == XNamespace.Xml.NamespaceName) ||
                                                         (from parentDecls in a.Parent.Ancestors().Attributes(a.Name)
                                                          where parentDecls.IsNamespaceDeclaration && (string)parentDecls == (string)a
                                                          select parentDecls).Any()
                                                       )
                     select a).ToList().Remove();

                    // Write XElement using XmlWriter without omitting
                    string removedByManual = SaveXElement(elemObj, SaveOptions.DisableFormatting);

                    ReaderDiff.Compare(removedByWriter, removedByManual);
                }

                //[Variation(Desc = "Default ns parent autogenerated", Priority = 1)]
                public void testFromTheRootNodeTricky()
                {
                    object e = new XElement("{nsp}A", new XElement("{nsp}B", new XAttribute("xmlns", "nsp")));
                    if (_type == typeof(XDocument))
                    {
                        XDocument d = new XDocument();
                        d.Add(e);
                        e = d;
                    }
                    string act = SaveXElement(e, SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting);
                    string exp = AppendXmlDecl("<A xmlns='nsp'><B/></A>");

                    ReaderDiff.Compare(act, exp);
                }

                //[Variation(Desc = "Conflicts: NS redefinition", Priority = 2, Params = new object[] {   "<p:A xmlns:p='nsp'><p:B xmlns:p='ns-other'><p:C xmlns:p='nsp'><D xmlns:p='nsp'/></p:C></p:B></p:A>",
                //                                                                                        "<p:A xmlns:p='nsp'><p:B xmlns:p='ns-other'><p:C xmlns:p='nsp'><D/></p:C></p:B></p:A>" })]
                //[Variation(Desc = "Conflicts: NS redefinition, default NS", Priority = 2, Params = new object[] {   "<A xmlns='nsp'><B xmlns='ns-other'><C xmlns='nsp'><D xmlns='nsp'/></C></B></A>", 
                //                                                                                                    "<A xmlns='nsp'><B xmlns='ns-other'><C xmlns='nsp'><D/></C></B></A>" })]
                //[Variation(Desc = "Conflicts: NS redefinition, default NS II.", Priority = 2, Params = new object[] {   "<A xmlns=''><B xmlns='ns-other'><C xmlns=''><D xmlns=''/></C></B></A>", 
                //                                                                                                        "<A><B xmlns='ns-other'><C xmlns=''><D/></C></B></A>" })]
                //[Variation(Desc = "Conflicts: NS undeclaration, default NS", Priority = 2, Params = new object[] {  "<A xmlns='nsp'><B xmlns=''><C xmlns='nsp'><D xmlns='nsp'/></C></B></A>", 
                //                                                                                                    "<A xmlns='nsp'><B xmlns=''><C xmlns='nsp'><D/></C></B></A>" })]
                public static object[][] ConFlictsNSRedefenitionParams = new object[][] {
                    new object[] {   "<p:A xmlns:p='nsp'><p:B xmlns:p='ns-other'><p:C xmlns:p='nsp'><D xmlns:p='nsp'/></p:C></p:B></p:A>",
                        "<p:A xmlns:p='nsp'><p:B xmlns:p='ns-other'><p:C xmlns:p='nsp'><D/></p:C></p:B></p:A>" },
                    new object[] {   "<A xmlns='nsp'><B xmlns='ns-other'><C xmlns='nsp'><D xmlns='nsp'/></C></B></A>",
                        "<A xmlns='nsp'><B xmlns='ns-other'><C xmlns='nsp'><D/></C></B></A>" },
                    new object[] {   "<A xmlns=''><B xmlns='ns-other'><C xmlns=''><D xmlns=''/></C></B></A>",
                        "<A><B xmlns='ns-other'><C xmlns=''><D/></C></B></A>" },
                    new object[] {  "<A xmlns='nsp'><B xmlns=''><C xmlns='nsp'><D xmlns='nsp'/></C></B></A>",
                        "<A xmlns='nsp'><B xmlns=''><C xmlns='nsp'><D/></C></B></A>" }
                };

                [Theory, MemberData(nameof(ConFlictsNSRedefenitionParams))]
                public void XDocumentConflictsNSRedefinitionSaveToStringWriterAndGetContent(string xml1, string xml2)
                {
                    XDocument doc = XDocument.Parse(xml1);
                    SaveOptions so = SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting;
                    using (StringWriter sw = new StringWriter())
                    {
                        doc.Save(sw, so);
                        ReaderDiff.Compare(GetXmlStringWithXmlDecl(xml2), sw.ToString());
                    }
                }

                [Theory, MemberData(nameof(ConFlictsNSRedefenitionParams))]
                public void XDocumentConflictsNSRedefinitionToString(string xml1, string xml2)
                {
                    XDocument doc = XDocument.Parse(xml1);
                    SaveOptions so = SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting;
                    ReaderDiff.Compare(xml2, doc.ToString(so));
                }

                [Theory, MemberData(nameof(ConFlictsNSRedefenitionParams))]
                public void XElementConflictsNSRedefinitionSaveToStringWriterAndGetContent(string xml1, string xml2)
                {
                    XElement el = XElement.Parse(xml1);
                    SaveOptions so = SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting;
                    using (StringWriter sw = new StringWriter())
                    {
                        el.Save(sw, so);
                        ReaderDiff.Compare(GetXmlStringWithXmlDecl(xml2), sw.ToString());
                    }
                }

                [Theory, MemberData(nameof(ConFlictsNSRedefenitionParams))]
                public void XElementConflictsNSRedefinitionToString(string xml1, string xml2)
                {
                    XElement el = XElement.Parse(xml1);
                    SaveOptions so = SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting;
                    ReaderDiff.Compare(xml2, el.ToString(so));
                }

                //[Variation(Desc = "Not from root", Priority = 1)]
                public void testFromChildNode1()
                {
                    if (_type == typeof(XDocument)) TestLog.Skip("Test not applicable");
                    XElement e = new XElement("root",
                        new XAttribute(XNamespace.Xmlns + "p1", "nsp"),
                        new XElement("{nsp}A",
                            new XElement("{nsp}B",
                                new XAttribute("xmlns", "nsp"))));
                    ReaderDiff.Compare(SaveXElement(e.Element("{nsp}A"), SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting), AppendXmlDecl("<p1:A xmlns:p1='nsp'><B xmlns='nsp'/></p1:A>"));
                }

                //[Variation(Desc = "Not from root II.", Priority = 1)]
                public void testFromChildNode2()
                {
                    if (_type == typeof(XDocument)) TestLog.Skip("Test not applicable");
                    XElement e = new XElement("root",
                        new XAttribute(XNamespace.Xmlns + "p1", "nsp"),
                        new XElement("{nsp}A",
                            new XElement("{nsp}B",
                                new XAttribute(XNamespace.Xmlns + "p1", "nsp"))));
                    ReaderDiff.Compare(SaveXElement(e.Element("{nsp}A"), SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting), AppendXmlDecl("<p1:A xmlns:p1='nsp'><p1:B/></p1:A>"));
                }

                //[Variation(Desc = "Not from root III.", Priority = 2)]
                public void testFromChildNode3()
                {
                    if (_type == typeof(XDocument)) TestLog.Skip("Test not applicable");
                    XElement e = new XElement("root",
                        new XAttribute(XNamespace.Xmlns + "p1", "nsp"),
                        new XElement("{nsp}A",
                            new XElement("{nsp}B",
                                new XAttribute(XNamespace.Xmlns + "p1", "nsp"))));
                    ReaderDiff.Compare(SaveXElement(e.Descendants("{nsp}B").FirstOrDefault(), SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting), AppendXmlDecl("<p1:B xmlns:p1='nsp'/>"));
                }

                //[Variation(Desc = "Not from root IV.", Priority = 2)]
                public void testFromChildNode4()
                {
                    if (_type == typeof(XDocument)) TestLog.Skip("Test not applicable");
                    XElement e = new XElement("root",
                        new XAttribute(XNamespace.Xmlns + "p1", "nsp"),
                        new XElement("{nsp}A",
                            new XElement("{nsp}B")));
                    ReaderDiff.Compare(SaveXElement(e.Descendants("{nsp}B").FirstOrDefault(), SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting), AppendXmlDecl("<p1:B xmlns:p1='nsp'/>"));
                }
            }
        }
    }
}
