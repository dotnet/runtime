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
            //[TestCase(Name = "ReadToNextSibling", Desc = "ReadToNextSibling")]
            public partial class TCReadToNextSibling : BridgeHelpers
            {
                #region XMLSTR
                private string _xmlStr = @"<?xml version='1.0'?>
													<root><!--Comment-->
														<elem>
															<child1 att='1'>
																<child2 xmlns='child2'>
																	<child3/>
																	blahblahblah
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

                //[Variation("Simple positive test 1", Priority = 0, Params = new object[] { "NNS" })]
                //[Variation("Simple positive test 2", Priority = 0, Params = new object[] { "DNS" })]
                //[Variation("Simple positive test 3", Priority = 0, Params = new object[] { "NS" })]
                public void v()
                {
                    string type = Variation.Params[0].ToString();

                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    switch (type)
                    {
                        case "NNS":
                            DataReader.ReadToDescendant("elem");
                            DataReader.ReadToNextSibling("elem");

                            if (DataReader.HasAttributes)
                            {
                                TestLog.Compare(DataReader.GetAttribute("att"), "1", "Not the expected attribute");
                            }
                            else
                            {
                                TestLog.WriteLine("Positioned on wrong element");
                                throw new TestException(TestResult.Failed, "");
                            }
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;

                        case "DNS":
                            DataReader.ReadToDescendant("elem", "elem");
                            DataReader.ReadToNextSibling("elem", "elem");
                            if (DataReader.HasAttributes)
                            {
                                if (DataReader.GetAttribute("att") == null)
                                {
                                    TestLog.WriteLine("Positioned on wrong element, not on DNS");
                                    throw new TestException(TestResult.Failed, "");
                                }
                            }
                            else
                            {
                                TestLog.WriteLine("Positioned on wrong element");
                                throw new TestException(TestResult.Failed, "");
                            }
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;

                        case "NS":
                            DataReader.ReadToDescendant("e:elem");
                            DataReader.ReadToNextSibling("e:elem");

                            if (DataReader.HasAttributes)
                            {
                                if (DataReader.GetAttribute("xmlns:e") == null)
                                {
                                    TestLog.WriteLine("Positioned on wrong element, not on DNS");
                                    throw new TestException(TestResult.Failed, "");
                                }
                            }
                            else
                            {
                                TestLog.WriteLine("Positioned on wrong element");
                                throw new TestException(TestResult.Failed, "");
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
                    } while (mnw.GetNodes().Length < 4096);
                    mnw.PutText("<a/><b/>");
                    mnw.Finish();

                    XmlReader DataReader = GetReader(new StringReader(mnw.GetNodes()));
                    PositionOnElement(DataReader, "ELEMENT_1");

                    TestLog.Compare(DataReader.ReadToDescendant("a"), true, "Couldn't go to Descendant");
                    int depth = DataReader.Depth;
                    TestLog.Compare(DataReader.ReadToNextSibling("b"), true, "Couldn't go to NextSibling");

                    TestLog.Compare(DataReader.Depth, depth, "Depth is not correct");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Nodetype is not correct");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Read to next sibling with same names 1", Priority = 1, Params = new object[] { "NNS", "<root><a att='1'/><a att='2'/><a att='3'/></root>" })]
                //[Variation("Read on next sibling with same names 2", Priority = 1, Params = new object[] { "DNS", "<root xmlns='a'><a att='1'/><a att='2'/><a att='3'/></root>" })]
                //[Variation("Read on next sibling with same names 3", Priority = 1, Params = new object[] { "NS", "<root xmlns:a='a'><a:a att='1'/><a:a att='2'/><a:a att='3'/></root>" })]
                public void v3()
                {
                    string type = Variation.Params[0].ToString();
                    string xml = Variation.Params[1].ToString();

                    XmlReader DataReader = GetReader(new StringReader(xml));
                    DataReader.Read();

                    // Doing a sequential read.
                    switch (type)
                    {
                        case "NNS":
                            DataReader.ReadToDescendant("a");
                            DataReader.ReadToNextSibling("a");
                            DataReader.ReadToNextSibling("a");
                            TestLog.Compare(DataReader.GetAttribute("att"), "3", "Wrong node");

                            while (DataReader.Read()) ;
                            DataReader.Dispose();

                            return;

                        case "DNS":
                            DataReader.ReadToDescendant("a", "a");
                            DataReader.ReadToNextSibling("a", "a");
                            DataReader.ReadToNextSibling("a", "a");
                            TestLog.Compare(DataReader.GetAttribute("att"), "3", "Wrong node");

                            while (DataReader.Read()) ;
                            DataReader.Dispose();

                            return;

                        case "NS":
                            DataReader.ReadToDescendant("a:a");
                            DataReader.ReadToNextSibling("a:a");
                            DataReader.ReadToNextSibling("a:a");
                            TestLog.Compare(DataReader.GetAttribute("att"), "3", "Wrong node");

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
                    int depth = DataReader.Depth;

                    TestLog.Compare(DataReader.ReadToNextSibling("abc"), false, "Reader returned true for an invalid name");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");
                    TestLog.Compare(DataReader.Depth, depth - 1, "Wrong Depth");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Positioning on a level and try to find the name which is on a level higher", Priority = 1)]
                public void v5()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "child3");

                    TestLog.Compare(DataReader.ReadToNextSibling("child1"), false, "Reader returned true for an invalid name");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");

                    PositionOnElement(DataReader, "child3");

                    TestLog.Compare(DataReader.ReadToNextSibling("child2", "child2"), false, "Reader returned true for an invalid name,ns");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type for name,ns");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Read to next sibling on one level and again to level below it", Priority = 1)]
                public void v6()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToDescendant("elem"), true, "Cant find elem");
                    TestLog.Compare(DataReader.ReadToNextSibling("elem", "elem"), true, "Cant find elem,elem");
                    TestLog.Compare(DataReader.ReadToNextSibling("e:elem"), true, "Cant find e:elem");

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
                            TestLog.Compare(DataReader.ReadToNextSibling("abc"), false, "Fails on node");
                        }
                        else
                        {
                            if (DataReader.HasAttributes)
                            {
                                while (DataReader.MoveToNextAttribute())
                                {
                                    TestLog.Compare(DataReader.ReadToNextSibling("abc"), false, "Fails on attribute node");
                                }
                            }
                        }
                    }

                    DataReader.Dispose();
                }

                //[Variation("Pass null to both arguments throws ArgumentException", Priority = 2)]
                public void v16()
                {
                    XmlReader DataReader = GetReader(new StringReader("<root><e/></root>"));
                    DataReader.Read();
                    try
                    {
                        DataReader.ReadToNextSibling(null);
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        DataReader.ReadToNextSibling("e", null);
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

                    DataReader.ReadToDescendant("child1", "foo");
                    DataReader.ReadToNextSibling("child1", "bar");
                    TestLog.Compare(DataReader.IsEmptyElement, false, "Not on the correct node");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }
            }
        }
    }
}
