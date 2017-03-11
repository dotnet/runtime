// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {

        public partial class XNodeReaderTests : XLinqTestCase
        {
            //[TestCase(Name = "ReadToFollowing", Desc = "ReadToFollowing")]
            public partial class TCReadToFollowing : BridgeHelpers
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
                public void v1()
                {
                    string type = Variation.Params[0].ToString();

                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    switch (type)
                    {
                        case "NNS":
                            DataReader.ReadToFollowing("elem");
                            TestLog.Compare(DataReader.HasAttributes, false, "Positioned on wrong element");
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;

                        case "DNS":
                            DataReader.ReadToFollowing("elem", "elem");
                            if (DataReader.HasAttributes)
                                TestLog.Compare(DataReader.GetAttribute("xmlns") != null, "Positioned on wrong element");
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;

                        case "NS":
                            DataReader.ReadToFollowing("e:elem");
                            if (DataReader.HasAttributes)
                                TestLog.Compare(DataReader.GetAttribute("xmlns:e") != null, "Positioned on wrong element");
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;
                        default:
                            throw new TestFailedException("Error in Test type");
                    }
                }

                //[Variation("Read on following with same names", Priority = 1, Params = new object[] { "NNS" })]
                //[Variation("Read on following with same names", Priority = 1, Params = new object[] { "DNS" })]
                //[Variation("Read on following with same names", Priority = 1, Params = new object[] { "NS" })]
                public void v2()
                {
                    string type = Variation.Params[0].ToString();

                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    // Doing a sequential read.
                    switch (type)
                    {
                        case "NNS":
                            DataReader.ReadToFollowing("elem");
                            int depth = DataReader.Depth;
                            TestLog.Compare(DataReader.HasAttributes, false, "Positioned on wrong element");
                            TestLog.Compare(DataReader.ReadToFollowing("elem"), true, "There are no more elem nodes");
                            TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Wrong node type");
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;

                        case "DNS":
                            DataReader.ReadToFollowing("elem", "elem");
                            if (DataReader.HasAttributes)
                                TestLog.Compare(DataReader.GetAttribute("xmlns") != null, "Positioned on wrong element, not on NS");
                            TestLog.Compare(DataReader.ReadToFollowing("elem", "elem"), true, "There are no more elem,elem nodes");
                            TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Wrong node type");
                            while (DataReader.Read()) ;
                            DataReader.Dispose();
                            return;

                        case "NS":
                            DataReader.ReadToFollowing("e:elem");
                            if (DataReader.HasAttributes)
                                TestLog.Compare(DataReader.GetAttribute("xmlns:e") != null, "Positioned on wrong element, not on NS");
                            TestLog.Compare(DataReader.ReadToFollowing("e:elem"), true, "There are no more descendants");
                            TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Wrong node type");
                            while (DataReader.Read()) ;
                            DataReader.Dispose();

                            return;

                        default:
                            throw new TestFailedException("Error in Test type");
                    }
                }

                //[Variation("If name not found, go to eof", Priority = 1)]
                public void v4()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "elem");

                    TestLog.Compare(DataReader.ReadToFollowing("abc"), false, "Reader returned true for an invalid name");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.None, "Wrong node type");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("If localname not found go to eof", Priority = 1)]
                public void v5_1()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "elem");

                    TestLog.Compare(DataReader.ReadToFollowing("abc", "elem"), false, "Reader returned true for an invalid localname");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.None, "Wrong node type");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("If uri not found, go to eof", Priority = 1)]
                public void v5_2()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));

                    TestLog.Compare(DataReader.ReadToFollowing("elem", "abc"), false, "Reader returned true for an invalid uri");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.None, "Wrong node type");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Read to Following on one level and again to level below it", Priority = 1)]
                public void v6()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToFollowing("elem"), true, "Cant find elem");
                    TestLog.Compare(DataReader.ReadToFollowing("child1"), true, "Cant find child1");
                    TestLog.Compare(DataReader.ReadToFollowing("child2"), true, "Cant find child2");
                    TestLog.Compare(DataReader.ReadToFollowing("child3"), true, "Cant find child3");
                    TestLog.Compare(DataReader.ReadToFollowing("child4"), true, "Shouldn't find child4");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on Element");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Read to Following on one level and again to level below it, with namespace", Priority = 1)]
                public void v7()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToFollowing("elem", "elem"), true, "Cant find elem");
                    TestLog.Compare(DataReader.ReadToFollowing("child1", "elem"), true, "Cant find child1");
                    TestLog.Compare(DataReader.ReadToFollowing("child2", "child2"), true, "Cant find child2");
                    TestLog.Compare(DataReader.ReadToFollowing("child3", "child2"), true, "Cant find child3");
                    TestLog.Compare(DataReader.ReadToFollowing("child4", "child2"), true, "Cant find child4");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on Element");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Read to Following on one level and again to level below it, with prefix", Priority = 1)]
                public void v8()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToFollowing("e:elem"), true, "Cant find elem");
                    TestLog.Compare(DataReader.ReadToFollowing("e:child1"), true, "Cant find child1");
                    TestLog.Compare(DataReader.ReadToFollowing("e:child2"), true, "Cant find child2");
                    TestLog.Compare(DataReader.ReadToFollowing("e:child3"), true, "Cant find child3");
                    TestLog.Compare(DataReader.ReadToFollowing("e:child4"), true, "Cant fine child4");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on Element");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Multiple Reads to children and then next siblings, NNS", Priority = 2)]
                public void v9()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToFollowing("elem"), true, "Read fails elem");
                    TestLog.Compare(DataReader.ReadToFollowing("child3"), true, "Read fails child3");
                    TestLog.Compare(DataReader.ReadToNextSibling("child4"), true, "Read fails child4");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Multiple Reads to children and then next siblings, DNS", Priority = 2)]
                public void v10()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToFollowing("elem", "elem"), true, "Read fails elem");
                    TestLog.Compare(DataReader.ReadToFollowing("child3", "child2"), true, "Read fails child3");
                    TestLog.Compare(DataReader.ReadToNextSibling("child4", "child2"), true, "Read fails child4");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Multiple Reads to children and then next siblings, NS", Priority = 2)]
                public void v11()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToFollowing("e:elem"), true, "Read fails elem");
                    TestLog.Compare(DataReader.ReadToFollowing("e:child3"), true, "Read fails child3");
                    TestLog.Compare(DataReader.ReadToNextSibling("e:child4"), true, "Read fails child4");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Only child has namespaces and read to it", Priority = 2)]
                public void v14()
                {
                    XmlReader DataReader = GetReader(new StringReader(_xmlStr));
                    PositionOnElement(DataReader, "root");

                    TestLog.Compare(DataReader.ReadToFollowing("child2", "child2"), true, "Fails on child2");

                    DataReader.Dispose();
                }

                //[Variation("Pass null to both arguments throws ArgumentException", Priority = 2)]
                public void v15()
                {
                    XmlReader DataReader = GetReader(new StringReader("<root><b/></root>"));
                    DataReader.Read();
                    try
                    {
                        DataReader.ReadToFollowing(null);
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        DataReader.ReadToFollowing("b", null);
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

                    DataReader.ReadToFollowing("child1", "bar");
                    TestLog.Compare(DataReader.IsEmptyElement, false, "Not on the correct node");

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }
            }
        }
    }
}
