// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class PropertiesTests : XLinqTestCase
        {
            public partial class NamespaceAccessors : XLinqTestCase
            {
                // GetDefaultNamespace ()
                //  ~ No namespace def
                //  ~ Default namespace
                //      ~ in scope
                //      ~ local
                //      ~ redefined
                //      ~ undefined
                //  ~ sanity - DML: adding/removing xmlns attribute

                //[Variation(Priority = 0, Desc = "GetDefaultNamespace - local def", Params = new object[] { "<A><B xmlns='nsa'/></A>", "nsa" })]
                //[Variation(Priority = 0, Desc = "GetDefaultNamespace - in scope", Params = new object[] { "<A xmlns='nsa'><B/></A>", "nsa" })]
                //[Variation(Priority = 2, Desc = "GetDefaultNamespace - redefined", Params = new object[] { "<A xmlns='nsb'><B xmlns='nsa'/></A>", "nsa" })]
                //[Variation(Priority = 2, Desc = "GetDefaultNamespace - undefined", Params = new object[] { "<A xmlns='nsb'><B xmlns=''/></A>", "" })]
                //[Variation(Priority = 1, Desc = "GetDefaultNamespace - no default ns", Params = new object[] { "<A><B/></A>", "" })]
                //[Variation(Priority = 1, Desc = "GetDefaultNamespace - no default ns II", Params = new object[] { "<A><C xmlns='nsc'/><B/></A>", "" })]
                public void GetDefaultNamespace()
                {
                    string xml = Variation.Params[0] as string;
                    string defNSString = Variation.Params[1] as string;
                    XNamespace defNS = defNSString == null ? null : XNamespace.Get(defNSString);
                    XElement e = XElement.Parse(xml);
                    XNamespace df = (e.LastNode as XElement).GetDefaultNamespace();
                    TestLog.Compare(object.ReferenceEquals(df, defNS), "def namespace");
                }

                //[Variation(Priority = 0, Desc = "GetDefaultNamespace - DML Sanity")]
                public void GetDefaultNamespaceDMLSanity()
                {
                    XElement e = new XElement("A", new XAttribute("xmlns", "nsA"),
                                    new XElement("B", new XAttribute("xmlns", "nsB1")),
                                    new XElement("B", new XAttribute("xmlns", "nsB2")));

                    XElement x = e.LastNode as XElement;
                    TestLog.Compare(x.GetDefaultNamespace(), XNamespace.Get("nsB2"), "start : nsB2");

                    x.FirstAttribute.Value = "nsX1";
                    TestLog.Compare(x.GetDefaultNamespace(), XNamespace.Get("nsX1"), "set value :  nsX1");

                    x.FirstAttribute.Remove();
                    TestLog.Compare(x.GetDefaultNamespace(), XNamespace.Get("nsA"), "Remove :  nsA");

                    x.Parent.FirstAttribute.Value = "nsA1";
                    TestLog.Compare(x.GetDefaultNamespace(), XNamespace.Get("nsA1"), "set value on parent:  nsA1");

                    x.Add(new XAttribute("xmlns", "nsX2"));
                    TestLog.Compare(x.GetDefaultNamespace(), XNamespace.Get("nsX2"), "Add :  nsX2");
                }

                // GetNamespaceOfPrefix ()
                //  ~ Not declared prefix
                //      ~ Not existing
                //      ~ Existing but not in the context
                //  ~ Declared
                //      ~ used, not used
                //      ~ Locally
                //      ~ On parent
                //          ~ Definition
                //          ~ Redefinition
                //  ~ Null, String.Empty

                //[Variation(Priority = 0, Desc = "GetNamespaceOfPrefix - local, used", Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc" })]
                //[Variation(Priority = 0, Desc = "GetNamespaceOfPrefix - case sensitive", Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", "P", null })]
                //[Variation(Priority = 1, Desc = "GetNamespaceOfPrefix - local, not used", Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc" })]
                //[Variation(Priority = 0, Desc = "GetNamespaceOfPrefix - in scope, used", Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S/></p:C><B/></A>", "p", "nsc" })]
                //[Variation(Priority = 1, Desc = "GetNamespaceOfPrefix - in scope, not used I.", Params = new object[] { "<A xmlns:p='nsc'><C xmlns:a='a'/><B/></A>", "p", "nsc" })]
                //[Variation(Priority = 1, Desc = "GetNamespaceOfPrefix - in scope, not used II.", Params = new object[] { "<p:A xmlns:p='nsc' xmlns:a='a'><C/><B/></p:A>", "p", "nsc" })]
                //[Variation(Priority = 1, Desc = "GetNamespaceOfPrefix - redefinition, used", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc" })]
                //[Variation(Priority = 1, Desc = "GetNamespaceOfPrefix - redefinition, not used", Params = new object[] { "<a:A xmlns:a='a' xmlns:p='redefme'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc" })]
                //[Variation(Priority = 2, Desc = "GetNamespaceOfPrefix - nonexisting", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "X", null })]
                //[Variation(Priority = 2, Desc = "GetNamespaceOfPrefix - xml", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xml", "http://www.w3.org/XML/1998/namespace" })]
                //[Variation(Priority = 2, Desc = "GetNamespaceOfPrefix - xmlns", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xmlns", "http://www.w3.org/2000/xmlns/" })]
                //[Variation(Priority = 2, Desc = "GetNamespaceOfPrefix - nonexisting in context", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", "X", null })]
                public void NamespaceForPrefix()
                {
                    string xml = Variation.Params[0] as string;
                    string prefix = Variation.Params[1] as string;
                    string ns = Variation.Params[2] as string;

                    XNamespace NS = ns == null ? null : XNamespace.Get(ns);
                    XElement e = XElement.Parse(xml);
                    XNamespace df = (e.FirstNode as XElement).GetNamespaceOfPrefix(prefix);
                    TestLog.Compare(object.ReferenceEquals(df, NS), "namespace for prefix");
                }


                //[Variation(Priority = 2, Desc = "GetNamespaceOfPrefix - Null", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", null, null })]
                //[Variation(Priority = 2, Desc = "GetNamespaceOfPrefix - Empty string", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", "", null })]
                public void NamespaceForPrefixNull()
                {
                    string xml = Variation.Params[0] as string;
                    string prefix = Variation.Params[1] as string;
                    string ns = Variation.Params[2] as string;

                    XElement e = XElement.Parse(xml);
                    try
                    {
                        XNamespace df = (e.FirstNode as XElement).GetNamespaceOfPrefix(prefix);
                        TestLog.WriteLine("Exception was expected here");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (ArgumentException) { }
                }

                //[Variation(Priority = 0, Desc = "GetNamespaceOfPrefix - DML Sanity")]
                public void NamespaceForPrefixDMLSanity()
                {
                    XElement e = new XElement("A", new XAttribute(XNamespace.Xmlns + "b", "nsA"),
                                    new XElement("{nsA}B", new XAttribute(XNamespace.Xmlns + "b", "nsB1")),
                                    new XElement("{nsB2}B", new XAttribute(XNamespace.Xmlns + "b", "nsB2")));

                    XElement b1 = e.FirstNode as XElement;
                    XElement b2 = e.LastNode as XElement;

                    TestLog.Compare(b2.GetNamespaceOfPrefix("b").NamespaceName, "nsB2", "original value");

                    b2.Attribute(XNamespace.Xmlns + "b").Value = "nsNEW";
                    TestLog.Compare(b2.GetNamespaceOfPrefix("b").NamespaceName, "nsNEW", "updated value");

                    b2.Attribute(XNamespace.Xmlns + "b").Remove();
                    TestLog.Compare(b2.GetNamespaceOfPrefix("b").NamespaceName, "nsA", "removed value, take one from parent");

                    b2.Add(new XAttribute(XNamespace.Xmlns + "p", "nsP"));
                    TestLog.Compare(b2.GetNamespaceOfPrefix("p").NamespaceName, "nsP", "New add - brand new");

                    b2.Add(new XAttribute(XNamespace.Xmlns + "b", "nsADD"));
                    TestLog.Compare(b2.GetNamespaceOfPrefix("b").NamespaceName, "nsADD", "New add - redefine");
                }


                // GetPrefixForNamespace ()
                //  ~ Not declared namespace
                //  ~ Default namespace
                //  ~ Namespace defined
                //      ~ localy
                //      ~ on parent (in scope)
                //      ~ redefined
                //  ~ Null, String.Empty

                //[Variation(Priority = 0, Desc = "GetPrefixOfNamespace - local, used", Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc" })]
                //[Variation(Priority = 0, Desc = "GetPrefixOfNamespace - case sensitive", Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", null, "NSC" })]
                //[Variation(Priority = 1, Desc = "GetPrefixOfNamespace - local, not used", Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc" })]
                //[Variation(Priority = 0, Desc = "GetPrefixOfNamespace - in scope, used", Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S/></p:C><B/></A>", "p", "nsc" })]
                //[Variation(Priority = 1, Desc = "GetPrefixOfNamespace - in scope, not used I.", Params = new object[] { "<A xmlns:p='nsc'><C xmlns:a='a'/><B/></A>", "p", "nsc" })]
                //[Variation(Priority = 1, Desc = "GetPrefixOfNamespace - in scope, not used II.", Params = new object[] { "<p:A xmlns:p='nsc' xmlns:a='a'><C/><B/></p:A>", "p", "nsc" })]
                //[Variation(Priority = 1, Desc = "GetPrefixOfNamespace - redefinition, used", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc" })]
                //[Variation(Priority = 1, Desc = "GetPrefixOfNamespace - redefinition, not used", Params = new object[] { "<a:A xmlns:a='a' xmlns:p='redefme'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc" })]
                //[Variation(Priority = 2, Desc = "GetPrefixOfNamespace - default namespace", Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", null, "x" })]
                //[Variation(Priority = 2, Desc = "GetPrefixOfNamespace - blank", Params = new object[] { "<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", null, "" })]
                //[Variation(Priority = 2, Desc = "GetPrefixOfNamespace - nonexisting", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", null, "nonexisting" })]
                //[Variation(Priority = 2, Desc = "GetPrefixOfNamespace - xml", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xml", "http://www.w3.org/XML/1998/namespace" })]
                //[Variation(Priority = 2, Desc = "GetPrefixOfNamespace - xmlns", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xmlns", "http://www.w3.org/2000/xmlns/" })]
                //[Variation(Priority = 2, Desc = "GetPrefixOfNamespace - nonexisting in context", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", null, "nonexisting" })]
                public void PrefixOfNamespace()
                {
                    string xml = Variation.Params[0] as string;
                    string prefix = Variation.Params[1] as string;
                    string ns = Variation.Params[2] as string;

                    XElement e = XElement.Parse(xml);
                    string pref = (e.FirstNode as XElement).GetPrefixOfNamespace(XNamespace.Get(ns));
                    TestLog.Compare(pref, prefix, "namespace for prefix");
                }

                //[Variation(Priority = 2, Desc = "GetPrefixOfNamespace - Null", Params = new object[] { "<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", null, null })]
                public void PrefixOfNamespaceNull()
                {
                    string xml = Variation.Params[0] as string;
                    string prefix = Variation.Params[1] as string;
                    string ns = Variation.Params[2] as string;

                    XElement e = XElement.Parse(xml);
                    try
                    {
                        string pref = (e.FirstNode as XElement).GetPrefixOfNamespace(ns);
                        TestLog.WriteLine("Exception was expected here");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (ArgumentException) { }
                }

                //[Variation(Priority = 0, Desc = "GetPrefixOfNamespace - DML Sanity")]
                public void PrefixOfNamespaceDMLSanity()
                {
                    XElement e = new XElement("A", new XAttribute(XNamespace.Xmlns + "p", "nsA"),
                                    new XElement("{nsA}B", new XAttribute(XNamespace.Xmlns + "b", "nsA")),
                                    new XElement("{nsB2}B", new XAttribute(XNamespace.Xmlns + "b", "nsA")));

                    XElement b1 = e.FirstNode as XElement;
                    XElement b2 = e.LastNode as XElement;

                    TestLog.Compare(b2.GetPrefixOfNamespace("nsA"), "b", "original value");

                    b2.Attribute(XNamespace.Xmlns + "b").Value = "nsNEW";
                    TestLog.Compare(b2.GetPrefixOfNamespace("nsNEW"), "b", "value channge");

                    b2.Attribute(XNamespace.Xmlns + "b").Remove();
                    TestLog.Compare(b2.GetPrefixOfNamespace("nsA"), "p", "removed value, take one from parent");

                    b2.Add(new XAttribute(XNamespace.Xmlns + "nn", "nsQ"));
                    TestLog.Compare(b2.GetPrefixOfNamespace("nsQ"), "nn", "New add - brand new");

                    b2.Add(new XAttribute(XNamespace.Xmlns + "n1", "nsA"));
                    TestLog.Compare(b2.GetPrefixOfNamespace("nsA"), "n1", "New add - redefine");
                }
            }
        }
    }
}