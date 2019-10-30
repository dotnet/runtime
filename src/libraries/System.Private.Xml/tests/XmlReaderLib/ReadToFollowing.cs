// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;

namespace System.Xml.Tests
{
    /////////////////////////////////////////////////////////////////////////
    // TestCase ReadOuterXml
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCReadToFollowing : TCXMLReaderBaseGeneral
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

        //[Variation("Simple positive test", Pri = 0, Params = new object[] { "NNS" })]
        //[Variation("Simple positive test", Pri = 0, Params = new object[] { "DNS" })]
        //[Variation("Simple positive test", Pri = 0, Params = new object[] { "NS" })]
        public int v1()
        {
            string type = CurVariation.Params[0].ToString();
            CError.WriteLine("Test Type : " + type);

            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            switch (type)
            {
                case "NNS":
                    DataReader.ReadToFollowing("elem");
                    CError.Compare(DataReader.HasAttributes, false, "Positioned on wrong element");
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;

                case "DNS":
                    DataReader.ReadToFollowing("elem", "elem");
                    if (DataReader.HasAttributes)
                        CError.Compare(DataReader.GetAttribute("xmlns") != null, "Positioned on wrong element");
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;

                case "NS":
                    DataReader.ReadToFollowing("e:elem");
                    if (DataReader.HasAttributes)
                        CError.Compare(DataReader.GetAttribute("xmlns:e") != null, "Positioned on wrong element");
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;
                default:
                    throw new CTestFailedException("Error in Test type");
            }
        }

        //[Variation("Read on following with same names", Pri = 1, Params = new object[] { "NNS" })]
        //[Variation("Read on following with same names", Pri = 1, Params = new object[] { "DNS" })]
        //[Variation("Read on following with same names", Pri = 1, Params = new object[] { "NS" })]
        public int v2()
        {
            string type = CurVariation.Params[0].ToString();
            CError.WriteLine("Test Type : " + type);

            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            //Doing a sequential read.
            switch (type)
            {
                case "NNS":
                    DataReader.ReadToFollowing("elem");
                    int depth = DataReader.Depth;
                    CError.Compare(DataReader.HasAttributes, false, "Positioned on wrong element");
                    CError.Compare(DataReader.ReadToFollowing("elem"), true, "There are no more elem nodes");
                    CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Wrong node type");
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;

                case "DNS":
                    DataReader.ReadToFollowing("elem", "elem");
                    if (DataReader.HasAttributes)
                        CError.Compare(DataReader.GetAttribute("xmlns") != null, "Positioned on wrong element, not on NS");
                    CError.Compare(DataReader.ReadToFollowing("elem", "elem"), true, "There are no more elem,elem nodes");
                    CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Wrong node type");
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;

                case "NS":
                    DataReader.ReadToFollowing("e:elem");
                    if (DataReader.HasAttributes)
                        CError.Compare(DataReader.GetAttribute("xmlns:e") != null, "Positioned on wrong element, not on NS");
                    CError.Compare(DataReader.ReadToFollowing("e:elem"), true, "There are no more descendants");
                    CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Wrong node type");
                    while (DataReader.Read()) ;
                    DataReader.Close();

                    return TEST_PASS;

                default:
                    throw new CTestFailedException("Error in Test type");
            }
        }

        [Variation("If name not found, go to eof", Pri = 1)]
        public int v4()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("elem");

            CError.Compare(DataReader.ReadToFollowing("abc"), false, "Reader returned true for an invalid name");
            CError.Compare(DataReader.NodeType, XmlNodeType.None, "Wrong node type");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("If localname not found go to eof", Pri = 1)]
        public int v5_1()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("elem");

            CError.Compare(DataReader.ReadToFollowing("abc", "elem"), false, "Reader returned true for an invalid localname");
            CError.Compare(DataReader.NodeType, XmlNodeType.None, "Wrong node type");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("If uri not found, go to eof", Pri = 1)]
        public int v5_2()
        {
            ReloadSource(new StringReader(_xmlStr));

            CError.Compare(DataReader.ReadToFollowing("elem", "abc"), false, "Reader returned true for an invalid uri");
            CError.Compare(DataReader.NodeType, XmlNodeType.None, "Wrong node type");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Read to Following on one level and again to level below it", Pri = 1)]
        public int v6()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToFollowing("elem"), true, "Cant find elem");
            CError.Compare(DataReader.ReadToFollowing("child1"), true, "Cant find child1");
            CError.Compare(DataReader.ReadToFollowing("child2"), true, "Cant find child2");
            CError.Compare(DataReader.ReadToFollowing("child3"), true, "Cant find child3");
            CError.Compare(DataReader.ReadToFollowing("child4"), true, "Shouldn't find child4");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on Element");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Read to Following on one level and again to level below it, with namespace", Pri = 1)]
        public int v7()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToFollowing("elem", "elem"), true, "Cant find elem");
            CError.Compare(DataReader.ReadToFollowing("child1", "elem"), true, "Cant find child1");
            CError.Compare(DataReader.ReadToFollowing("child2", "child2"), true, "Cant find child2");
            CError.Compare(DataReader.ReadToFollowing("child3", "child2"), true, "Cant find child3");
            CError.Compare(DataReader.ReadToFollowing("child4", "child2"), true, "Cant find child4");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on Element");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Read to Following on one level and again to level below it, with prefix", Pri = 1)]
        public int v8()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToFollowing("e:elem"), true, "Cant find elem");
            CError.Compare(DataReader.ReadToFollowing("e:child1"), true, "Cant find child1");
            CError.Compare(DataReader.ReadToFollowing("e:child2"), true, "Cant find child2");
            CError.Compare(DataReader.ReadToFollowing("e:child3"), true, "Cant find child3");
            CError.Compare(DataReader.ReadToFollowing("e:child4"), true, "Cant fine child4");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on Element");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Multiple Reads to children and then next siblings, NNS", Pri = 2)]
        public int v9()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToFollowing("elem"), true, "Read fails elem");
            CError.Compare(DataReader.ReadToFollowing("child3"), true, "Read fails child3");
            CError.Compare(DataReader.ReadToNextSibling("child4"), true, "Read fails child4");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Multiple Reads to children and then next siblings, DNS", Pri = 2)]
        public int v10()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToFollowing("elem", "elem"), true, "Read fails elem");
            CError.Compare(DataReader.ReadToFollowing("child3", "child2"), true, "Read fails child3");
            CError.Compare(DataReader.ReadToNextSibling("child4", "child2"), true, "Read fails child4");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Multiple Reads to children and then next siblings, NS", Pri = 2)]
        public int v11()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToFollowing("e:elem"), true, "Read fails elem");
            CError.Compare(DataReader.ReadToFollowing("e:child3"), true, "Read fails child3");
            CError.Compare(DataReader.ReadToNextSibling("e:child4"), true, "Read fails child4");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Only child has namespaces and read to it", Pri = 2)]
        public int v14()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToFollowing("child2", "child2"), true, "Fails on child2");

            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("Pass null to both arguments throws ArgumentException", Pri = 2)]
        public int v15()
        {
            ReloadSource(new StringReader("<root><b/></root>"));
            DataReader.Read();
            if (IsBinaryReader())
                DataReader.Read();

            try
            {
                DataReader.ReadToFollowing(null);
            }
            catch (ArgumentNullException)
            {
                CError.WriteLine("Caught for single param");
            }

            try
            {
                DataReader.ReadToFollowing("b", null);
            }
            catch (ArgumentNullException)
            {
                CError.WriteLine("Caught for single param");
            }

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Different names, same uri works correctly", Pri = 2)]
        public int v17()
        {
            ReloadSource(new StringReader("<root><child1 xmlns='foo'/>blah<child1 xmlns='bar'>blah</child1></root>"));
            DataReader.Read();
            if (IsBinaryReader())
                DataReader.Read();

            DataReader.ReadToFollowing("child1", "bar");
            CError.Compare(DataReader.IsEmptyElement, false, "Not on the correct node");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }
    }
}
