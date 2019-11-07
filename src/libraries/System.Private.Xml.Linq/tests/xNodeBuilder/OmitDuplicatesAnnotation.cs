// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Test.ModuleCore;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Threading;
using XmlCoreTest.Common;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeBuilderFunctionalTests : TestModule
    {
        public partial class XNodeBuilderTests : XLinqTestCase
        {
            public partial class OmitAnotation : XLinqTestCase
            {
                private static string s_MyPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"Xlinq\DuplicateNamespaces");

                private static XContainer GetContainer(string filename, Type type)
                {
                    switch (type.Name)
                    {
                        case "XElement":
                            return XElement.Load(FilePathUtil.getStream(filename));
                        case "XDocument":
                            return XDocument.Load(FilePathUtil.getStream(filename));
                        default:
                            throw new TestFailedException("Type not recognized");
                    }
                }

                private static XContainer GetContainer(Stream stream, Type type)
                {
                    switch (type.Name)
                    {
                        case "XElement":
                            return XElement.Load(stream);
                        case "XDocument":
                            return XDocument.Load(stream);
                        default:
                            throw new TestFailedException("Type not recognized");
                    }
                }


                //[Variation(Priority = 0, Desc = "No annotation - element", Params = new object[] { typeof(XElement), "Simple.xml" })]
                //[Variation(Priority = 0, Desc = "No annotation - document", Params = new object[] { typeof(XDocument), "Simple.xml" })]
                public void NoAnnotation()
                {
                    Type t = CurrentChild.Params[0] as Type;
                    string fileName = Path.Combine(s_MyPath, CurrentChild.Params[1] as string);
                    // create normal reader for comparison
                    using (XmlReader r1 = XmlReader.Create(FilePathUtil.getStream(fileName), new XmlReaderSettings() { IgnoreWhitespace = true, DtdProcessing = DtdProcessing.Ignore }))
                    {
                        // create reader from Xlinq
                        XContainer c = GetContainer(fileName, t);
                        using (XmlReader r2 = c.CreateReader())
                        {
                            ReaderDiff.Compare(r1, r2);
                        }
                    }
                }

                //[Variation(Priority = 2, Desc = "Annotation on document without omit - None", Params = new object[] { typeof(XDocument), "Simple.xml", SaveOptions.None })]
                //[Variation(Priority = 2, Desc = "Annotation on document without omit - DisableFormating", Params = new object[] { typeof(XDocument), "Simple.xml", SaveOptions.DisableFormatting })]
                //[Variation(Priority = 2, Desc = "Annotation on element without omit - None", Params = new object[] { typeof(XElement), "Simple.xml", SaveOptions.None })]
                //[Variation(Priority = 2, Desc = "Annotation on element without omit - DisableFormating", Params = new object[] { typeof(XElement), "Simple.xml", SaveOptions.DisableFormatting })]
                public void AnnotationWithoutTheOmitDuplicates()
                {
                    Type t = CurrentChild.Params[0] as Type;
                    string fileName = Path.Combine(s_MyPath, CurrentChild.Params[1] as string);
                    SaveOptions so = (SaveOptions)CurrentChild.Params[2];

                    using (XmlReader r1 = XmlReader.Create(FilePathUtil.getStream(fileName), new XmlReaderSettings() { IgnoreWhitespace = true, DtdProcessing = DtdProcessing.Ignore }))
                    {
                        XContainer doc = GetContainer(fileName, t);
                        doc.AddAnnotation(so);
                        using (XmlReader r2 = doc.CreateReader())
                        {
                            ReaderDiff.Compare(r1, r2);
                        }
                    }
                }

                //[Variation(Priority = 0, Desc = "Annotation on document - Omit", Params = new object[] { typeof(XDocument), "Simple.xml", SaveOptions.OmitDuplicateNamespaces })]
                //[Variation(Priority = 1, Desc = "Annotation on document - Omit + Disable", Params = new object[] { typeof(XDocument), "Simple.xml", SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting })]
                //[Variation(Priority = 0, Desc = "Annotation on element - Omit", Params = new object[] { typeof(XElement), "Simple.xml", SaveOptions.OmitDuplicateNamespaces })]
                //[Variation(Priority = 1, Desc = "Annotation on element - Omit + Disable", Params = new object[] { typeof(XElement), "Simple.xml", SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting })]
                public void XDocAnnotation()
                {
                    Type t = CurrentChild.Params[0] as Type;
                    string fileName = Path.Combine(s_MyPath, CurrentChild.Params[1] as string);
                    SaveOptions so = (SaveOptions)CurrentChild.Params[2];

                    using (MemoryStream ms = new MemoryStream())
                    {
                        XDocument toClean = XDocument.Load(FilePathUtil.getStream(fileName));
                        using (XmlWriter w = XmlWriter.Create(ms, new XmlWriterSettings() { OmitXmlDeclaration = true, NamespaceHandling = NamespaceHandling.OmitDuplicates }))
                        {
                            toClean.Save(w);
                        }
                        ms.Position = 0;

                        using (XmlReader r1 = XmlReader.Create(ms, new XmlReaderSettings() { IgnoreWhitespace = true, DtdProcessing = DtdProcessing.Ignore }))
                        {
                            XContainer doc = GetContainer(fileName, t);
                            doc.AddAnnotation(so);
                            using (XmlReader r2 = doc.CreateReader())
                            {
                                ReaderDiff.Compare(r1, r2);
                            }
                        }
                    }
                }

                //[Variation(Priority = 0, Desc = "Annotation on the parent nodes, XElement", Params = new object[] { typeof(XElement), "simple.xml" })]
                //[Variation(Priority = 0, Desc = "Annotation on the parent nodes, XDocument", Params = new object[] { typeof(XDocument), "simple.xml" })]
                public void AnnotationOnParent1()
                {
                    Type t = CurrentChild.Params[0] as Type;
                    string fileName = Path.Combine(s_MyPath, CurrentChild.Params[1] as string);

                    var orig = GetContainer(fileName, t).DescendantNodes().ToArray();
                    var test = GetContainer(fileName, t).DescendantNodes().ToArray();

                    for (int i = 0; i < test.Count(); i++)
                    {
                        XNode n = test[i];
                        XNode parent = n;  // verify parent and self
                        while (parent != null)
                        {  // for all parents
                            // verify original version
                            TestLog.Compare(n.ToString(), n.ToString(SaveOptions.None), "Initial value");
                            TestLog.Compare(n.ToString(), orig[i].ToString(), "Initial value, via orig");
                            ReaderDiff.Compare(orig[i].CreateReader(), n.CreateReader());
                            // add annotation on parent
                            parent.AddAnnotation(SaveOptions.OmitDuplicateNamespaces);
                            // verify with annotation
                            TestLog.Compare(n.ToString(), n.ToString(SaveOptions.OmitDuplicateNamespaces), "with the annotation, normal");
                            ReaderDiffNSAware.CompareNamespaceAware(orig[i].CreateReader(), n.CreateReader());
                            // removeannotation
                            parent.RemoveAnnotations(typeof(SaveOptions));
                            // verify after removal
                            TestLog.Compare(n.ToString(), n.ToString(SaveOptions.None), "after removed annotation value");
                            TestLog.Compare(n.ToString(), orig[i].ToString(), "after removed annotation, via orig");
                            ReaderDiff.Compare(orig[i].CreateReader(), n.CreateReader());
                            // move parent
                            if (parent is XDocument) break;
                            parent = parent.Parent ?? parent.Document as XNode;
                        }
                    }
                }

                //[Variation(Priority = 0, Desc = "Multiple annotations in the tree - both up - XElement", Param = typeof(XElement))]
                //[Variation(Priority = 0, Desc = "Multiple annotations in the tree - both up - XDocument", Param = typeof(XDocument))]
                public void MultipleAnnotationsInTree()
                {
                    Type t = CurrentChild.Param as Type;
                    string xml = @"<A xmlns:p='a1'><B xmlns:q='a2'><C xmlns:p='a1'><D xmlns:q='a2' ><E xmlns:p='a1' /></D></C></B></A>";
                    XContainer reF = (t == typeof(XElement) ? XElement.Parse(xml) as XContainer : XDocument.Parse(xml) as XContainer);  // I want dynamics!!!
                    SaveOptions[] options = new SaveOptions[] { SaveOptions.None, SaveOptions.DisableFormatting, SaveOptions.OmitDuplicateNamespaces, SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces };

                    foreach (SaveOptions[] opts in Tuples2(options))
                    {
                        XContainer gp = (t == typeof(XElement) ? XElement.Parse(xml) as XContainer : XDocument.Parse(xml) as XContainer);
                        gp.AddAnnotation(opts[0]);
                        gp.FirstNode.AddAnnotation(opts[1]);

                        TestLog.Compare(reF.Descendants("C").First().ToString(opts[1]), gp.Descendants("C").First().ToString(), "On C - ToString()");
                        ReaderDiffNSAware.CompareNamespaceAware(opts[1], reF.Descendants("C").First().CreateReader(), gp.Descendants("C").First().CreateReader());

                        TestLog.Compare(reF.Descendants("B").First().ToString(opts[1]), gp.Descendants("B").First().ToString(), "On C - ToString()");
                        ReaderDiffNSAware.CompareNamespaceAware(opts[1], reF.Descendants("B").First().CreateReader(), gp.Descendants("B").First().CreateReader());
                    }
                }

                //[Variation(Priority = 0, Desc = "Multiple annotations in the tree - up/down - XElement", Param = typeof(XElement))]
                //[Variation(Priority = 0, Desc = "Multiple annotations in the tree - up/down - XDocument", Param = typeof(XDocument))]
                public void MultipleAnnotationsInTree2()
                {
                    Type t = CurrentChild.Param as Type;
                    string xml = @"<A xmlns:p='a1'><B xmlns:q='a2'><C xmlns:p='a1'><D xmlns:q='a2' ><E xmlns:p='a1' /></D></C></B></A>";
                    XContainer reF = (t == typeof(XElement) ? XElement.Parse(xml) as XContainer : XDocument.Parse(xml) as XContainer);  // I want dynamics!!!
                    SaveOptions[] options = new SaveOptions[] { SaveOptions.None, SaveOptions.DisableFormatting, SaveOptions.OmitDuplicateNamespaces, SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces };

                    foreach (SaveOptions[] opts in Tuples2(options))
                    {
                        XContainer gp = (t == typeof(XElement) ? XElement.Parse(xml) as XContainer : XDocument.Parse(xml) as XContainer);
                        gp.AddAnnotation(opts[0]);
                        gp.Descendants("C").First().AddAnnotation(opts[1]);

                        TestLog.Compare(reF.ToString(opts[0]), gp.ToString(), "On root - ToString()");
                        ReaderDiffNSAware.CompareNamespaceAware(opts[0], reF.CreateReader(), gp.CreateReader());

                        TestLog.Compare(reF.Descendants("B").First().ToString(opts[0]), gp.Descendants("B").First().ToString(), "On C - ToString()");
                        ReaderDiffNSAware.CompareNamespaceAware(opts[0], reF.Descendants("B").First().CreateReader(), gp.Descendants("B").First().CreateReader());
                    }
                }

                //[Variation(Priority = 0, Desc = "Multiple annotations on node - XDocument", Param = typeof(XDocument))]
                //[Variation(Priority = 0, Desc = "Multiple annotations on node - XElement", Param = typeof(XElement))]
                public void MultipleAnnotationsOnElement()
                {
                    Type t = CurrentChild.Param as Type;
                    string xml = @"<A xmlns:p='a1'><B xmlns:q='a2'><C xmlns:p='a1'><D xmlns:q='a2' ><E xmlns:p='a1' /></D></C></B></A>";
                    XContainer reF = (t == typeof(XElement) ? XElement.Parse(xml) as XContainer : XDocument.Parse(xml) as XContainer);  // I want dynamics!!!
                    SaveOptions[] options = new SaveOptions[] { SaveOptions.None, SaveOptions.DisableFormatting, SaveOptions.OmitDuplicateNamespaces, SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces };

                    foreach (SaveOptions[] opts in Tuples2(options))
                    {
                        XContainer gp = (t == typeof(XElement) ? XElement.Parse(xml) as XContainer : XDocument.Parse(xml) as XContainer);
                        foreach (SaveOptions o in opts)
                        {
                            gp.AddAnnotation(o);
                        }
                        TestLog.Compare(reF.ToString(opts[0]), gp.ToString(), "On root - ToString()");
                        ReaderDiffNSAware.CompareNamespaceAware(opts[0], reF.CreateReader(), gp.CreateReader());
                    }
                }

                static IEnumerable<T[]> Tuples2<T>(T[] array)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        for (int j = 0; j < array.Length; j++)
                        {
                            if (i != j) yield return new T[] { array[i], array[j] };
                        }
                    }
                }

                //[Variation(Priority = 2, Desc = "On other node types - attributes", Param = typeof(XElement))]
                public void OnOtherNodesAttrs()
                {
                    string fileName = Path.Combine(s_MyPath, "attributes.xml");
                    TestLog.WriteLineIgnore("Loading: .... " + fileName);
                    XElement reF = XElement.Load(FilePathUtil.getStream(fileName));
                    XElement e = XElement.Load(FilePathUtil.getStream(fileName));

                    e.AddAnnotation(SaveOptions.OmitDuplicateNamespaces);

                    XAttribute[] refAttrs = reF.DescendantsAndSelf().Attributes().ToArray();
                    XAttribute[] eAttrs = e.DescendantsAndSelf().Attributes().ToArray();

                    for (int i = 0; i < refAttrs.Length; i++)
                    {
                        TestLog.Compare(refAttrs[i].ToString(), eAttrs[i].ToString(), "without annotation on attribute");
                        eAttrs[i].AddAnnotation(SaveOptions.OmitDuplicateNamespaces);
                        TestLog.Compare(refAttrs[i].ToString(), eAttrs[i].ToString(), "with annotation on attribute");
                    }
                }

                private static XElement CreateVBSample()
                {
                    XElement e1 = new XElement("{ns1}A", new XAttribute("xmlns", "ns1"));
                    XElement c1 = new XElement("{ns1}B", new XAttribute("xmlns", "ns1"));
                    XElement c2 = new XElement("{ns1}C", new XAttribute("xmlns", "ns1"));
                    e1.Add(c1, c2);
                    e1.AddAnnotation(SaveOptions.OmitDuplicateNamespaces);
                    return e1;
                }

                //[Variation(Priority = 0, Desc = "Simulate the VB behavior - Save")]
                public void SimulateVb1()
                {
                    string expected = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\n<A xmlns=\"ns1\">\n  <B />\n  <C />\n</A>";
                    XElement e1 = CreateVBSample();
                    StringBuilder sb = new StringBuilder();
                    e1.Save(new StringWriter(sb));
                    ReaderDiff.Compare(sb.ToString(), expected);
                }

                //[Variation(Priority = 0, Desc = "Simulate the VB behavior - Reader")]
                public void SimulateVb2()
                {
                    string expected = "<A xmlns=\"ns1\"><B /><C /></A>";
                    XElement e1 = CreateVBSample();

                    using (XmlReader r1 = XmlReader.Create(new StringReader(expected)))
                    {
                        using (XmlReader r2 = e1.CreateReader())
                        {
                            ReaderDiff.Compare(r1, r2);
                        }
                    }
                }

                //[Variation(Priority = 0, Desc = "Local settings override annotation")]
                public void LocalOverride()
                {
                    string expected = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\n<A xmlns=\"ns1\">\n  <B xmlns=\"ns1\"/>\n  <C xmlns=\"ns1\"/>\n</A>";
                    XElement e1 = CreateVBSample();
                    StringBuilder sb = new StringBuilder();
                    e1.Save(new StringWriter(sb), SaveOptions.None);
                    ReaderDiff.Compare(sb.ToString(), expected);
                }

                //[Variation(Priority = 0, Desc = "XDocument - ReaderOptions.None", Params = new object[] { typeof(XDocument), "simple.xml", ReaderOptions.None })]
                //[Variation(Priority = 0, Desc = "XDocument - ReaderOptions.OmitDuplicateNamespaces", Params = new object[] { typeof(XDocument), "simple.xml", ReaderOptions.OmitDuplicateNamespaces })]
                //[Variation(Priority = 0, Desc = "XElement - ReaderOptions.None", Params= new object [] {typeof(XElement), "simple.xml", ReaderOptions.None })]
                //[Variation(Priority = 0, Desc = "XElement - ReaderOptions.OmitDuplicateNamespaces", Params = new object[] { typeof(XElement), "simple.xml", ReaderOptions.OmitDuplicateNamespaces })]
                public void ReaderOptionsSmoke()
                {
                    Type t = CurrentChild.Params[0] as Type;
                    string fileName = Path.Combine(s_MyPath, CurrentChild.Params[1] as string);
                    ReaderOptions ro = (ReaderOptions)CurrentChild.Params[2];

                    var original = GetContainer(fileName, t).DescendantNodes().ToArray();
                    var clone = GetContainer(fileName, t).DescendantNodes().ToArray();

                    TestLog.Compare(original.Length, clone.Length, "original.Length != clone.Length"); // assert
                    Action<XmlReader, XmlReader> compareDelegate = ro == ReaderOptions.None ? (Action<XmlReader, XmlReader>)ReaderDiff.Compare : (Action<XmlReader, XmlReader>)ReaderDiffNSAware.CompareNamespaceAware;

                    foreach (int i in Enumerable.Range(0, original.Length))
                    {
                        // no annotation
                        compareDelegate(original[i].CreateReader(), clone[i].CreateReader(ro));

                        // annotation on self
                        foreach (SaveOptions so in new SaveOptions[] { SaveOptions.None, SaveOptions.OmitDuplicateNamespaces })
                        {
                            clone[i].AddAnnotation(so);
                            compareDelegate(original[i].CreateReader(), clone[i].CreateReader(ro));
                            clone[i].RemoveAnnotations(typeof(object));
                        }

                        // annotation on parents
                        foreach (SaveOptions so in new SaveOptions[] { SaveOptions.None, SaveOptions.OmitDuplicateNamespaces })
                        {
                            foreach (XNode anc in clone[i].Ancestors())
                            {
                                anc.AddAnnotation(so);
                                compareDelegate(original[i].CreateReader(), clone[i].CreateReader(ro));
                                anc.RemoveAnnotations(typeof(object));
                            }
                        }
                    }
                }
            }
        }
    }
}
