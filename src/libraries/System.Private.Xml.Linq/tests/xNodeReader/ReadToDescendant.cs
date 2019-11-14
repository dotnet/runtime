// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeReaderFunctionalTests : TestModule
    {

        public partial class XNodeReaderTests : XLinqTestCase
        {
            //[TestCase(Name = "ReadToDescendant", Desc = "ReadToDescendant")]
            public partial class TCReadToDescendant : BridgeHelpers
            {
                #region XMLSTR
                private string _xmlStr = @"<?xml version='1.0'?>
													<root><!--Comment-->
														<elem><!-- Comment -->
															<child1 att='1'><?pi target?>
																<child2 xmlns='child2'>
																	<child3/>
																	blahblahblah<![CDATA[ blah ]]>
																	<child4/>
																</child2>
															<?pi target1?>
															</child1>
														</elem>
														<elem att='1'>
															<child1 att='1'>
																<child2 xmlns='child2'>
																	<child3/>
																	blahblahblah
																	<child4/>
																</child2>
															<?pi target1?>
															</child1>
														</elem>
														<elem xmlns='elem'>
															<child1 att='1'>
																<child2 xmlns='child2'>
																	<child3/>
																	blahblahblah2
																	<child4/>
																</child2>
															</child1>
														</elem>
														<elem xmlns='elem' att='1'>
															<child1 att='1'>
																<child2 xmlns='child2'>
																	<child3/>
																	blahblahblah2
																	<child4/>
																</child2>
															</child1>
														</elem>
														<e:elem xmlns:e='elem2'>
															<e:child1 att='1'>
																<e:child2 xmlns='child2'>
																	<e:child3/>
																	blahblahblah2
																	<e:child4/>
																</e:child2>
															</e:child1>
														</e:elem>
														<e:elem xmlns:e='elem2' att='1'>
															<e:child1 att='1'>
																<e:child2 xmlns='child2'>
																	<e:child3/>
																	blahblahblah2
																	<e:child4/>
																</e:child2>
															</e:child1>
														</e:elem>
													</root>";

                #endregion

                //[Variation("Simple positive test", Priority = 0, Params = new object[] { "NNS" })]
                //[Variation("Simple positive test", Priority = 0, Params = new object[] { "DNS" })]
                //[Variation("Simple positive test", Priority = 0, Params = new object[] { "NS" })]
                public void v()
                {
                    string type = Variation.Params[0].ToString();

                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    switch (type)
                    {
                        case "NNS":
                            DataReader.ReadToDescendant("elem");
                            if (DataReader.HasAttributes)
                            {
                                TestLog.WriteLine("Positioned on wrong element");
                                TestLog.WriteIgnore(DataReader.ReadInnerXml() + "\n");
                                throw new TestException(TestResult.Failed, "");
                            }
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;

                        case "DNS":
                            DataReader.ReadToDescendant("elem", "elem");
                            if (DataReader.HasAttributes)
                            {
                                if (DataReader.GetAttribute("xmlns") == null)
                                {
                                    TestLog.WriteLine("Positioned on wrong element, not on DNS");
                                    throw new TestException(TestResult.Failed, "");
                                }
                            }
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;

                        case "NS":
                            DataReader.ReadToDescendant("e:elem");
                            if (DataReader.HasAttributes)
                            {
                                if (DataReader.GetAttribute("xmlns:e") == null)
                                {
                                    TestLog.WriteLine("Positioned on wrong element, not on NS");
                                    throw new TestException(TestResult.Failed, "");
                                }
                            }
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;
                        default:
                            throw new TestFailedException("Error in Test type");
                    }
                }

                //[Variation("Read on a deep tree at least more than 4K boundary", Priority = 2)]
                public void v2()
                {
                    ManagedNodeWriter mnw = new ManagedNodeWriter();
                    mnw.PutPattern("X");

                    int count = 0;
                    do
                    {
                        mnw.PutPattern("E/");
                        count++;
                    }
                    while (mnw.GetNodes().Length < 4096);
                    mnw.PutText("<a/>");
                    mnw.Finish();

                    XmlReader DataReader = GetReader(new StringReader(mnw.GetNodes()));

                    PositionOnElement(DataReader, "ELEMENT_1");
                    DataReader.ReadToDescendant("a");

                    TestLog.Compare(DataReader.Depth, count, "Depth is not correct");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Nodetype is not correct");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Read on descendant with same names", Priority = 1, Params = new object[] { "NNS" })]
                //[Variation("Read on descendant with same names", Priority = 1, Params = new object[] { "DNS" })]
                //[Variation("Read on descendant with same names", Priority = 1, Params = new object[] { "NS" })]
                public void v3()
                {
                    string type = Variation.Params[0].ToString();

                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    // Doing a sequential read.
                    switch (type)
                    {
                        case "NNS":
                            DataReader.ReadToDescendant("elem");
                            int depth = DataReader.Depth;
                            if (DataReader.HasAttributes)
                            {
                                TestLog.WriteLine("Positioned on wrong element");
                                throw new TestException(TestResult.Failed, "");
                            }
                            TestLog.Compare(DataReader.ReadToDescendant("elem"), false, "There are no more descendants");
                            TestLog.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");
                            while (DataReader.Read()) ;
                            DataReader.Dispose();

                            return;

                        case "DNS":
                            DataReader.ReadToDescendant("elem", "elem");
                            if (DataReader.HasAttributes)
                            {
                                if (DataReader.GetAttribute("xmlns") == null)
                                {
                                    TestLog.WriteLine("Positioned on wrong element, not on DNS");
                                    throw new TestException(TestResult.Failed, "");
                                }
                            }
                            TestLog.Compare(DataReader.ReadToDescendant("elem", "elem"), false, "There are no more descendants");
                            TestLog.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");
                            while (DataReader.Read()) ;
                            DataReader.Dispose();

                            return;

                        case "NS":
                            DataReader.ReadToDescendant("e:elem");
                            if (DataReader.HasAttributes)
                            {
                                if (DataReader.GetAttribute("xmlns:e") == null)
                                {
                                    TestLog.WriteLine("Positioned on wrong element, not on DNS");
                                    throw new TestException(TestResult.Failed, "");
                                }
                            }
                            TestLog.Compare(DataReader.ReadToDescendant("e:elem"), false, "There are no more descendants");
                            TestLog.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");
                            while (DataReader.Read()) ;
                            DataReader.Dispose();

                            return;

                        default:
                            throw new TestFailedException("Error in Test type");
                    }
                }

                //[Variation("If name not found, stop at end element of the subtree", Priority = 1)]
                public void v4()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "elem");

                    TestLog.Compare(DataReader.ReadToDescendant("abc"), false, "Reader returned true for an invalid name");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");

                    DataReader.Read();
                    TestLog.Compare(DataReader.ReadToDescendant("abc", "elem"), false, "reader returned true for an invalid name,ns combination");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Positioning on a level and try to find the name which is on a level higher", Priority = 1)]
                public void v5()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "child3");

                    TestLog.Compare(DataReader.ReadToDescendant("child1"), false, "Reader returned true for an invalid name");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Wrong node type");
                    TestLog.Compare(DataReader.LocalName, "child3", "Wrong name");

                    PositionOnElement(DataReader, "child3");

                    TestLog.Compare(DataReader.ReadToDescendant("child2", "child2"), false, "Reader returned true for an invalid name,ns");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Wrong node type for name,ns");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Read to Descendant on one level and again to level below it", Priority = 1)]
                public void v6()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToDescendant("elem"), true, "Cant find elem");
                    TestLog.Compare(DataReader.ReadToDescendant("child1"), true, "Cant find child1");
                    TestLog.Compare(DataReader.ReadToDescendant("child2"), true, "Cant find child2");
                    TestLog.Compare(DataReader.ReadToDescendant("child3"), true, "Cant find child3");
                    TestLog.Compare(DataReader.ReadToDescendant("child4"), false, "Shouldn't find child4");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on EndElement");
                    DataReader.Read();
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Text, "Not on Element");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Read to Descendant on one level and again to level below it, with namespace", Priority = 1)]
                public void v7()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToDescendant("elem", "elem"), true, "Cant find elem");
                    TestLog.Compare(DataReader.ReadToDescendant("child1", "elem"), true, "Cant find child1");
                    TestLog.Compare(DataReader.ReadToDescendant("child2", "child2"), true, "Cant find child2");
                    TestLog.Compare(DataReader.ReadToDescendant("child3", "child2"), true, "Cant find child3");
                    TestLog.Compare(DataReader.ReadToDescendant("child4", "child2"), false, "Shouldn't find child4");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on EndElement");
                    DataReader.Read();
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Text, "Not on Element");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Read to Descendant on one level and again to level below it, with prefix", Priority = 1)]
                public void v8()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToDescendant("e:elem"), true, "Cant find elem");
                    TestLog.Compare(DataReader.ReadToDescendant("e:child1"), true, "Cant find child1");
                    TestLog.Compare(DataReader.ReadToDescendant("e:child2"), true, "Cant find child2");
                    TestLog.Compare(DataReader.ReadToDescendant("e:child3"), true, "Cant find child3");
                    TestLog.Compare(DataReader.ReadToDescendant("e:child4"), false, "Shouldn't find child4");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on EndElement");
                    DataReader.Read();
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Text, "Not on Element");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Multiple Reads to children and then next siblings, NNS", Priority = 2)]
                public void v9()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToDescendant("elem"), true, "Read fails elem");
                    TestLog.Compare(DataReader.ReadToDescendant("child3"), true, "Read fails child3");
                    TestLog.Compare(DataReader.ReadToNextSibling("child4"), true, "Read fails child4");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Multiple Reads to children and then next siblings, DNS", Priority = 2)]
                public void v10()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToDescendant("elem", "elem"), true, "Read fails elem");
                    TestLog.Compare(DataReader.ReadToDescendant("child3", "child2"), true, "Read fails child3");
                    TestLog.Compare(DataReader.ReadToNextSibling("child4", "child2"), true, "Read fails child4");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Multiple Reads to children and then next siblings, NS", Priority = 2)]
                public void v11()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToDescendant("e:elem"), true, "Read fails elem");
                    TestLog.Compare(DataReader.ReadToDescendant("e:child3"), true, "Read fails child3");
                    TestLog.Compare(DataReader.ReadToNextSibling("e:child4"), true, "Read fails child4");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Call from different nodetypes", Priority = 1)]
                public void v12()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));

                    while (DataReader.Read())
                    {
                        if (DataReader.NodeType != XmlNodeType.Element)
                        {
                            TestLog.Compare(DataReader.ReadToDescendant("child1"), false, "Fails on node");
                        }
                        else
                        {
                            if (DataReader.HasAttributes)
                            {
                                while (DataReader.MoveToNextAttribute())
                                {
                                    TestLog.Compare(DataReader.ReadToDescendant("abc"), false, "Fails on attribute node");
                                }
                            }
                        }
                    }

                    DataReader.Dispose();
                }

                //[Variation("Only child has namespaces and read to it", Priority = 2)]
                public void v14()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToDescendant("child2", "child2"), true, "Fails on attribute node");

                    DataReader.Dispose();
                }

                //[Variation("Pass null to both arguments throws ArgumentException", Priority = 2)]
                public void v15()
                {
                    XmlReader DataReader = GetReader(new StringReader("<root><b/></root>"));
                    DataReader.Read();
                    try
                    {
                        DataReader.ReadToDescendant(null);
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        DataReader.ReadToDescendant("b", null);
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Different names, same uri works correctly", Priority = 2)]
                public void v17()
                {
                    XmlReader DataReader = GetReader(new StringReader("<root><child1 xmlns='foo'/>blah<child1 xmlns='bar'>blah</child1></root>"));
                    DataReader.Read();

                    DataReader.ReadToDescendant("child1", "bar");
                    TestLog.Compare(DataReader.IsEmptyElement, false, "Not on the correct node");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }


                //[Variation("On Root Node", Priority = 0, Params = new object[] { "NNS" })]
                //[Variation("On Root Node", Priority = 0, Params = new object[] { "DNS" })]
                //[Variation("On Root Node", Priority = 0, Params = new object[] { "NS" })]
                public void v18()
                {
                    string type = Variation.Params[0].ToString();

                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    switch (type)
                    {
                        case "NNS":
                            DataReader.ReadToDescendant("elem");
                            if (DataReader.HasAttributes)
                            {
                                TestLog.WriteLine("Positioned on wrong element");
                                TestLog.WriteIgnore(DataReader.ReadInnerXml() + "\n");
                                throw new TestException(TestResult.Failed, "");
                            }
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;

                        case "DNS":
                            DataReader.ReadToDescendant("elem", "elem");
                            if (DataReader.HasAttributes)
                            {
                                if (DataReader.GetAttribute("xmlns") == null)
                                {
                                    TestLog.WriteLine("Positioned on wrong element, not on DNS");
                                    throw new TestException(TestResult.Failed, "");
                                }
                            }
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;

                        case "NS":
                            DataReader.ReadToDescendant("e:elem");
                            if (DataReader.HasAttributes)
                            {
                                if (DataReader.GetAttribute("xmlns:e") == null)
                                {
                                    TestLog.WriteLine("Positioned on wrong element, not on NS");
                                    throw new TestException(TestResult.Failed, "");
                                }
                            }
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;
                        default:
                            throw new TestFailedException("Error in Test type");
                    }
                }

                //[Variation("427176	Assertion failed when call XmlReader.ReadToDescendant() for non-existing node", Priority = 1)]
                public void v19()
                {
                    XmlReader DataReader = GetReader(new StringReader("<a>b</a>"));
                    TestLog.Compare(DataReader.ReadToDescendant("foo"), false, "Should fail without assert");

                    DataReader.Dispose();
                }
            }
        }
    }
}
