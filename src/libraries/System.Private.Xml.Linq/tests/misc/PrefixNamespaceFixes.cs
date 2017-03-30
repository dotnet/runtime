// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class MiscTests : XLinqTestCase
        {
            public partial class PrefixImprovements : XLinqTestCase
            {
                void CompareStringThroughXmlReader(string xml1, string xml2)
                {
                    XmlReaderSettings rs = new XmlReaderSettings();
                    rs.IgnoreWhitespace = true;

                    using (StringReader sr1 = new StringReader(xml1))
                    using (StringReader sr2 = new StringReader(xml2))
                    using (XmlReader r1 = XmlReader.Create(sr1))
                    using (XmlReader r2 = XmlReader.Create(sr2))
                    {
                        ReaderDiff.Compare(r1, r2);
                    }
                }

                //[Variation(Priority = 0, Desc = "Smoke test")]
                public void var_1()
                {
                    XElement e = new XElement("A", new XAttribute(XNamespace.Xmlns + "p", "p"),
                                                    new XElement("B", new XAttribute(XNamespace.Xmlns + "q", "p"),
                                                            new XElement("{p}C", new XAttribute(XNamespace.Xmlns + "q", "q"))));
                    CompareStringThroughXmlReader(e.ToString(SaveOptions.DisableFormatting), @"<A xmlns:p='p'><B xmlns:q='p'><p:C xmlns:q='q' /></B></A>");
                }

                //[Variation(Priority = 1, Desc = "Smoke test with attributes.")]
                public void var_1a()
                {
                    XElement e = new XElement("A", new XAttribute(XNamespace.Xmlns + "p", "p"),
                                                    new XElement("B", new XAttribute(XNamespace.Xmlns + "q", "p"), new XAttribute("{p}a", "a"),
                                                            new XElement("{q}C", new XAttribute(XNamespace.Xmlns + "q", "q"), new XAttribute("{p}a", "a"))));
                    CompareStringThroughXmlReader(e.ToString(SaveOptions.DisableFormatting), @"<A xmlns:p='p'><B xmlns:q='p' q:a='a'><q:C xmlns:q='q' p:a='a' /></B></A>");
                }


                //[Variation(Priority = 1, Desc = "Default namespace I.")]
                public void var_2()
                {
                    XElement e = new XElement("A", new XAttribute(XNamespace.Xmlns + "p", "p"),
                                                    new XElement("{p}B", new XAttribute("xmlns", "p"),
                                                            new XElement("{p}C", new XAttribute("xmlns", "q"))));
                    CompareStringThroughXmlReader(e.ToString(SaveOptions.DisableFormatting), @"<A xmlns:p='p'><B xmlns='p'><p:C xmlns='q' /></B></A>");
                }

                //[Variation(Priority = 1, Desc = "Default namespace I. (attributes)")]
                public void var_2a()
                {
                    XElement e = new XElement("A", new XAttribute(XNamespace.Xmlns + "p", "p"),
                                                    new XElement("{p}B", new XAttribute("xmlns", "p"), new XAttribute("{p}a", "a"),
                                                            new XElement("{q}C", new XAttribute("xmlns", "q"), new XAttribute("{p}a", "a"))));
                    CompareStringThroughXmlReader(e.ToString(SaveOptions.DisableFormatting), @"<A xmlns:p='p'><B xmlns='p' p:a='a'><C xmlns='q' p:a='a' /></B></A>");
                }


                //[Variation(Priority = 1, Desc = "Default namespace II.")]
                public void var_3()
                {
                    XElement e = new XElement("{p}A", new XAttribute("xmlns", "p"),
                                                                    new XElement("{p}B", new XAttribute(XNamespace.Xmlns + "p", "p"),
                                                                            new XElement("{p}C", new XAttribute(XNamespace.Xmlns + "p", "q"))));
                    CompareStringThroughXmlReader(e.ToString(SaveOptions.DisableFormatting), @"<A xmlns='p'><p:B xmlns:p='p'><C xmlns:p='q' /></p:B></A>");
                }

                //[Variation(Priority = 1, Desc = "Default namespace II. (attributes)")]
                public void var_3a()
                {
                    XElement e = new XElement("{p}A", new XAttribute("xmlns", "p"),
                                                                    new XElement("{p}B", new XAttribute(XNamespace.Xmlns + "p", "p"), new XAttribute("{p}a", "a"),
                                                                            new XElement("{q}C", new XAttribute(XNamespace.Xmlns + "p", "q"), new XAttribute("{p}a", "a"))));
                    CompareStringThroughXmlReader(e.ToString(SaveOptions.DisableFormatting), @"<A xmlns='p'><p:B xmlns:p='p' p:a='a'><p:C xmlns:p='q' p3:a='a' xmlns:p3='p' /></p:B></A>");
                }


                //[Variation(Priority = 2, Desc = "Extended tree")]
                public void var_4()
                {
                    XElement e = new XElement("{p}A", new XAttribute("xmlns", "p"),
                                                                             new XElement("{p}B", new XAttribute(XNamespace.Xmlns + "p", "p"),
                                                                                     new XElement("{p}C", new XAttribute(XNamespace.Xmlns + "p", "q"),
                                                                                             new XElement("{p}X", new XAttribute(XNamespace.Xmlns + "p", "p"),
                                                                                                     new XElement("{p}Y")
                                                                                             )
                                                                                     )
                                                                             )
                                                                );
                    CompareStringThroughXmlReader(e.ToString(SaveOptions.DisableFormatting), @"<A xmlns='p'><p:B xmlns:p='p'><C xmlns:p='q'><p:X xmlns:p='p'><p:Y /></p:X></C></p:B></A>");
                }

                //[Variation(Priority = 1, Desc = "Attribute and element in default NS.")]
                public void var_5()
                {
                    XElement e = new XElement("A", new XAttribute(XNamespace.Xmlns + "p", "p"),
                                                                                     new XElement("{p}B", new XAttribute("xmlns", "p"), new XAttribute("{p}a", "a")));
                    CompareStringThroughXmlReader(e.ToString(SaveOptions.DisableFormatting), @"<A xmlns:p='p'><B xmlns='p' p:a='a' /></A>");
                }

                //[Variation(Priority = 1, Desc = "Attribute and element in default NS II.")]
                public void var_5a()
                {
                    XElement e = new XElement("A", new XAttribute(XNamespace.Xmlns + "p", "p"),
                                                                                     new XElement("{p}B", new XAttribute("xmlns", "p"), new XAttribute("{p}a", "a"),
                                                                                             new XElement("{p}C", new XAttribute("{p}c", "c"))));
                    CompareStringThroughXmlReader(e.ToString(SaveOptions.DisableFormatting), @"<A xmlns:p='p'><B xmlns='p' p:a='a' ><C p:c='c' /></B></A>");
                }

                //[Variation(Priority = 1, Desc = "Attribute and element in default NS (mix of all)")]
                public void var_5b()
                {
                    XElement e = new XElement("A", new XAttribute(XNamespace.Xmlns + "q", "q"), new XAttribute(XNamespace.Xmlns + "top", "p"),
                                                    new XElement("{q}B", new XAttribute(XNamespace.Xmlns + "p", "p"), new XAttribute("xmlns", "q"),
                                                                                     new XElement("{q}C", new XAttribute("xmlns", "p"), new XAttribute("{p}a", "a"), new XAttribute("{q}a", "a"), new XAttribute("a", "a")),
                                                                                     new XElement("{q}D", new XAttribute("{p}a", "a"), new XAttribute("{q}a", "a"), new XAttribute("a", "a")),  // picks the default namespace
                                                                                     new XElement("{p}E", new XAttribute("{p}a", "a"), new XAttribute("{q}a", "a"), new XAttribute("a", "a")),
                                                                                     new XElement("F", new XAttribute("{p}a", "a"), new XAttribute("{q}a", "a"), new XAttribute("a", "a")), // autogenerate xmlns=''
                                                                                     new XElement("{g}G", new XAttribute("xmlns", "g"), new XAttribute("{g}g", "givemeprefix"))));  // autogenerate new prefix

                    string expResult = @"<A xmlns:q='q' xmlns:top='p'>
                                              <B xmlns:p='p' xmlns='q'>
                                                <q:C xmlns='p' p:a='a' q:a='a' a='a' />
                                                <D p:a='a' q:a='a' a='a' />
                                                <p:E p:a='a' q:a='a' a='a' />
                                                <F p:a='a' q:a='a' a='a' xmlns='' />
                                                <G xmlns='g' p6:g='givemeprefix' xmlns:p6='g' />
                                              </B>
                                            </A>";
                    CompareStringThroughXmlReader(e.ToString(), expResult);
                }

                //[Variation(Priority = 1, Desc = "In depth ++")]
                public void var_6()
                {
                    XElement root = new XElement("A", new XAttribute(XNamespace.Xmlns + "a", "a"));
                    XElement e = root;
                    for (char c = 'b'; c < 'e'; c++)
                    {
                        string newPref = new string(c, 1);
                        e.Add(new XElement("B", new XAttribute(XNamespace.Xmlns + newPref, "a")));
                        e = e.FirstNode as XElement;
                    }

                    e.Add(new XElement("{a}C"));
                    e = e.FirstNode as XElement;

                    for (char c = 'd'; c > 'a'; c--)
                    {
                        string newPref = new string(c, 1);
                        e.Add(new XAttribute(XNamespace.Xmlns + newPref, "b"));
                    }

                    CompareStringThroughXmlReader(root.ToString(SaveOptions.DisableFormatting), @"<A xmlns:a='a'><B xmlns:b='a'><B xmlns:c='a'><B xmlns:d='a'><a:C xmlns:d='b' xmlns:c='b' xmlns:b='b' /></B></B></B></A>");
                }

                //[Variation(Priority = 1, Desc = "In depth --")]
                public void var_6b()
                {
                    XElement root = new XElement("A", new XAttribute(XNamespace.Xmlns + "a", "a"));
                    XElement e = root;
                    for (char c = 'b'; c < 'e'; c++)
                    {
                        string newPref = new string(c, 1);
                        e.Add(new XElement("B", new XAttribute(XNamespace.Xmlns + newPref, "a")));
                        e = e.FirstNode as XElement;
                    }

                    e.Add(new XElement("{a}C"));
                    e = e.FirstNode as XElement;

                    for (char c = 'b'; c < 'e'; c++)
                    {
                        string newPref = new string(c, 1);
                        e.Add(new XAttribute(XNamespace.Xmlns + newPref, "b"));
                    }

                    CompareStringThroughXmlReader(root.ToString(SaveOptions.DisableFormatting), @"<A xmlns:a='a'><B xmlns:b='a'><B xmlns:c='a'><B xmlns:d='a'><a:C xmlns:b='b' xmlns:c='b' xmlns:d='b' /></B></B></B></A>");
                }

                //[Variation(Priority = 1, Desc = "XmlWriter interference")]
                public void var_7()
                {
                    // Element "D" should use the default namespace defined in ancestor element "A" but it does not
                    // since the auto generated default name space in parent element "C".  So a new default namespace is added for
                    // "D". 
                    XElement e = new XElement("A", new XAttribute(XNamespace.Xmlns + "a", "a"),
                                                                                new XElement("{a}B", new XAttribute("xmlns", "a"),
                                                                                        new XElement("{ii}C", new XAttribute(XNamespace.Xmlns + "b", "a"),   // This one auto generates xmlns='ii'"
                                                                                                new XElement("{a}D", new XAttribute(XNamespace.Xmlns + "b", "X")))));
                    string expected = @"<A xmlns:a='a'><B xmlns='a'><C xmlns:b='a' xmlns='ii'><a:D xmlns:b='X' /></C></B></A>";
                    try
                    {
                        // This Compare should always fail for this test since this is how our product behaves right now.
                        // If compare passes then we have a regression.
                        CompareStringThroughXmlReader(e.ToString(SaveOptions.DisableFormatting), expected);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    throw new TestFailedException("Expected comparison mismatch and an exception.");
                }
            }
        }
    }
}
