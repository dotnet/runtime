// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    /////////////////////////////////////////////////////////////////////////
    // TestCase ReadOuterXml
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCReadToNextSibling : TCXMLReaderBaseGeneral
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

        //[Variation("Simple positive test 1", Pri = 0, Params = new object[] { "NNS" })]
        //[Variation("Simple positive test 2", Pri = 0, Params = new object[] { "DNS" })]
        //[Variation("Simple positive test 3", Pri = 0, Params = new object[] { "NS" })]
        public int v()
        {
            string type = CurVariation.Params[0].ToString();
            CError.WriteLine("Test Type : " + type);

            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            switch (type)
            {
                case "NNS":
                    DataReader.ReadToDescendant("elem");
                    DataReader.ReadToNextSibling("elem");

                    if (DataReader.HasAttributes)
                    {
                        CError.Compare(DataReader.GetAttribute("att"), "1", "Not the expected attribute");
                    }
                    else
                    {
                        CError.WriteLine("Positioned on wrong element");
                        DumpStat();
                        return TEST_FAIL;
                    }
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;

                case "DNS":
                    DataReader.ReadToDescendant("elem", "elem");
                    DataReader.ReadToNextSibling("elem", "elem");
                    if (DataReader.HasAttributes)
                    {
                        if (DataReader.GetAttribute("att") == null)
                        {
                            CError.WriteLine("Positioned on wrong element, not on DNS");
                            return TEST_FAIL;
                        }
                    }
                    else
                    {
                        CError.WriteLine("Positioned on wrong element");
                        DumpStat();
                        return TEST_FAIL;
                    }
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;

                case "NS":
                    DataReader.ReadToDescendant("e:elem");
                    DataReader.ReadToNextSibling("e:elem");

                    if (DataReader.HasAttributes)
                    {
                        if (DataReader.GetAttribute("xmlns:e") == null)
                        {
                            CError.WriteLine("Positioned on wrong element, not on DNS");
                            return TEST_FAIL;
                        }
                    }
                    else
                    {
                        CError.WriteLine("Positioned on wrong element");
                        DumpStat();
                        return TEST_FAIL;
                    }

                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;
                default:
                    throw new CTestFailedException("Error in Test type");
            }
        }

        [Variation("Read on a deep tree atleast more than 4K boundary", Pri = 2)]
        public int v2()
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
            CError.WriteIgnore(mnw.GetNodes() + "\n");


            ReloadSource(new StringReader(mnw.GetNodes()));
            DataReader.PositionOnElement("ELEMENT_1");

            CError.Compare(DataReader.ReadToDescendant("a"), true, "Couldn't go to Descendant");
            int depth = DataReader.Depth;
            CError.Compare(DataReader.ReadToNextSibling("b"), true, "Couldn't go to NextSibling");

            CError.Compare(DataReader.Depth, depth, "Depth is not correct");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Nodetype is not correct");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Read on a deep tree with more than 65536 elems", Pri = 2)]
        public int v2_1()
        {
            ManagedNodeWriter mnw = new ManagedNodeWriter();
            mnw.PutPattern("X");

            int count = 0;
            do
            {
                mnw.PutPattern("E/");
                count++;
            } while (count < 65536);
            mnw.PutText("<a/><b/>");
            mnw.Finish();

            CError.WriteIgnore(mnw.GetNodes() + "\n");

            ReloadSource(new StringReader(mnw.GetNodes()));
            DataReader.PositionOnElement("ELEMENT_0");

            CError.Compare(DataReader.ReadToDescendant("a"), "Couldn't go to Descendant");
            int depth = DataReader.Depth;

            CError.Compare(DataReader.ReadToNextSibling("b"), "Couldn't go to NextSibling");


            CError.Compare(DataReader.Depth, depth, "Depth is not correct");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Nodetype is not correct");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        //[Variation("Read to next sibling with same names 1", Pri = 1, Params = new object[] { "NNS", "<root><a att='1'/><a att='2'/><a att='3'/></root>" })]
        //[Variation("Read on next sibling with same names 2", Pri = 1, Params = new object[] { "DNS", "<root xmlns='a'><a att='1'/><a att='2'/><a att='3'/></root>" })]
        //[Variation("Read on next sibling with same names 3", Pri = 1, Params = new object[] { "NS", "<root xmlns:a='a'><a:a att='1'/><a:a att='2'/><a:a att='3'/></root>" })]
        public int v3()
        {
            string type = CurVariation.Params[0].ToString();
            string xml = CurVariation.Params[1].ToString();

            CError.WriteLine("Test Type : " + type);

            ReloadSource(new StringReader(xml));
            DataReader.Read();
            if (IsBinaryReader())
                DataReader.Read();

            //Doing a sequential read.
            switch (type)
            {
                case "NNS":
                    DataReader.ReadToDescendant("a");
                    DataReader.ReadToNextSibling("a");
                    DataReader.ReadToNextSibling("a");
                    CError.Compare(DataReader.GetAttribute("att"), "3", "Wrong node");

                    while (DataReader.Read()) ;
                    DataReader.Close();

                    return TEST_PASS;

                case "DNS":
                    DataReader.ReadToDescendant("a", "a");
                    DataReader.ReadToNextSibling("a", "a");
                    DataReader.ReadToNextSibling("a", "a");
                    CError.Compare(DataReader.GetAttribute("att"), "3", "Wrong node");

                    while (DataReader.Read()) ;
                    DataReader.Close();

                    return TEST_PASS;

                case "NS":
                    DataReader.ReadToDescendant("a:a");
                    DataReader.ReadToNextSibling("a:a");
                    DataReader.ReadToNextSibling("a:a");
                    CError.Compare(DataReader.GetAttribute("att"), "3", "Wrong node");

                    while (DataReader.Read()) ;
                    DataReader.Close();

                    return TEST_PASS;

                default:
                    throw new CTestFailedException("Error in Test type");
            }
        }

        [Variation("If name not found, stop at end element of the subtree", Pri = 1)]
        public int v4()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("elem");
            int depth = DataReader.Depth;

            CError.Compare(DataReader.ReadToNextSibling("abc"), false, "Reader returned true for an invalid name");
            CError.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");
            CError.Compare(DataReader.Depth, depth - 1, "Wrong Depth");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Positioning on a level and try to find the name which is on a level higher", Pri = 1)]
        public int v5()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("child3");

            CError.Compare(DataReader.ReadToNextSibling("child1"), false, "Reader returned true for an invalid name");
            CError.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");

            DataReader.PositionOnElement("child3");

            CError.Compare(DataReader.ReadToNextSibling("child2", "child2"), false, "Reader returned true for an invalid name,ns");
            CError.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type for name,ns");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Read to next sibling on one level and again to level below it", Pri = 1)]
        public int v6()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToDescendant("elem"), true, "Cant find elem");
            CError.Compare(DataReader.ReadToNextSibling("elem", "elem"), true, "Cant find elem,elem");
            CError.Compare(DataReader.ReadToNextSibling("e:elem"), true, "Cant find e:elem");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Call from different nodetypes", Pri = 1)]
        public int v12()
        {
            ReloadSource(new StringReader(_xmlStr));

            while (DataReader.Read())
            {
                if (DataReader.NodeType != XmlNodeType.Element)
                {
                    CError.WriteLine(DataReader.NodeType.ToString());
                    CError.Compare(DataReader.ReadToNextSibling("abc"), false, "Fails on node");
                }
                else
                {
                    if (DataReader.HasAttributes)
                    {
                        while (DataReader.MoveToNextAttribute())
                        {
                            CError.Compare(DataReader.ReadToNextSibling("abc"), false, "Fails on attribute node");
                        }
                    }
                }
            }

            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Pass null to both arguments throws ArgumentException", Pri = 2)]
        public int v16()
        {
            ReloadSource(new StringReader("<root><e/></root>"));
            DataReader.Read();
            try
            {
                DataReader.ReadToNextSibling(null);
            }
            catch (ArgumentNullException)
            {
                CError.WriteLine("Caught for single param");
            }

            try
            {
                DataReader.ReadToNextSibling("e", null);
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

            DataReader.ReadToDescendant("child1", "foo");
            DataReader.ReadToNextSibling("child1", "bar");
            CError.Compare(DataReader.IsEmptyElement, false, "Not on the correct node");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }
    }
}
