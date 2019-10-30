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
    public abstract partial class TCReadToDescendant : TCXMLReaderBaseGeneral
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
                    if (DataReader.HasAttributes)
                    {
                        CError.WriteLine("Positioned on wrong element");
                        CError.WriteIgnore(DataReader.ReadInnerXml() + "\n");
                        return TEST_FAIL;
                    }
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;

                case "DNS":
                    DataReader.ReadToDescendant("elem", "elem");
                    if (DataReader.HasAttributes)
                    {
                        if (DataReader.GetAttribute("xmlns") == null)
                        {
                            CError.WriteLine("Positioned on wrong element, not on DNS");
                            return TEST_FAIL;
                        }
                    }
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;

                case "NS":
                    DataReader.ReadToDescendant("e:elem");
                    if (DataReader.HasAttributes)
                    {
                        if (DataReader.GetAttribute("xmlns:e") == null)
                        {
                            CError.WriteLine("Positioned on wrong element, not on NS");
                            return TEST_FAIL;
                        }
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
            }
            while (mnw.GetNodes().Length < 4096);
            mnw.PutText("<a/>");
            mnw.Finish();
            CError.WriteIgnore(mnw.GetNodes() + "\n");


            ReloadSource(new StringReader(mnw.GetNodes()));
            DataReader.PositionOnElement("ELEMENT_1");
            CError.WriteLine("Reading to : " + "a");
            DataReader.ReadToDescendant("a");

            CError.Compare(DataReader.Depth, count, "Depth is not correct");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Nodetype is not correct");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }
        [Variation("Read on a deep tree atleast more than 65535 boundary", Pri = 2)]
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
            mnw.PutText("<a/>");
            mnw.Finish();

            ReloadSource(new StringReader(mnw.GetNodes()));
            DataReader.PositionOnElement("ELEMENT_1");

            CError.WriteLine("Reading to : a");
            DataReader.ReadToDescendant("a");

            CError.Compare(DataReader.Depth, 65536, "Depth is not correct");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Nodetype is not correct");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        //[Variation("Read on descendant with same names", Pri = 1, Params = new object[] { "NNS" })]
        //[Variation("Read on descendant with same names", Pri = 1, Params = new object[] { "DNS" })]
        //[Variation("Read on descendant with same names", Pri = 1, Params = new object[] { "NS" })]
        public int v3()
        {
            string type = CurVariation.Params[0].ToString();
            CError.WriteLine("Test Type : " + type);

            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            //Doing a sequential read.
            switch (type)
            {
                case "NNS":
                    DataReader.ReadToDescendant("elem");
                    int depth = DataReader.Depth;
                    if (DataReader.HasAttributes)
                    {
                        CError.WriteLine("Positioned on wrong element");
                        return TEST_FAIL;
                    }
                    CError.Compare(DataReader.ReadToDescendant("elem"), false, "There are no more descendants");
                    CError.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");
                    while (DataReader.Read()) ;
                    DataReader.Close();

                    return TEST_PASS;

                case "DNS":
                    DataReader.ReadToDescendant("elem", "elem");
                    if (DataReader.HasAttributes)
                    {
                        if (DataReader.GetAttribute("xmlns") == null)
                        {
                            CError.WriteLine("Positioned on wrong element, not on DNS");
                            return TEST_FAIL;
                        }
                    }
                    CError.Compare(DataReader.ReadToDescendant("elem", "elem"), false, "There are no more descendants");
                    CError.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");
                    while (DataReader.Read()) ;
                    DataReader.Close();

                    return TEST_PASS;

                case "NS":
                    DataReader.ReadToDescendant("e:elem");
                    if (DataReader.HasAttributes)
                    {
                        if (DataReader.GetAttribute("xmlns:e") == null)
                        {
                            CError.WriteLine("Positioned on wrong element, not on DNS");
                            return TEST_FAIL;
                        }
                    }
                    CError.Compare(DataReader.ReadToDescendant("e:elem"), false, "There are no more descendants");
                    CError.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");
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

            CError.Compare(DataReader.ReadToDescendant("abc"), false, "Reader returned true for an invalid name");
            CError.Compare(DataReader.NodeType, XmlNodeType.EndElement, "Wrong node type");

            DataReader.Read();
            CError.Compare(DataReader.ReadToDescendant("abc", "elem"), false, "reader returned true for an invalid name,ns combination");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Positioning on a level and try to find the name which is on a level higher", Pri = 1)]
        public int v5()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("child3");

            CError.Compare(DataReader.ReadToDescendant("child1"), false, "Reader returned true for an invalid name");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Wrong node type");
            CError.Compare(DataReader.LocalName, "child3", "Wrong name");

            DataReader.PositionOnElement("child3");

            CError.Compare(DataReader.ReadToDescendant("child2", "child2"), false, "Reader returned true for an invalid name,ns");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Wrong node type for name,ns");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Read to Descendant on one level and again to level below it", Pri = 1)]
        public int v6()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToDescendant("elem"), true, "Cant find elem");
            CError.Compare(DataReader.ReadToDescendant("child1"), true, "Cant find child1");
            CError.Compare(DataReader.ReadToDescendant("child2"), true, "Cant find child2");
            CError.Compare(DataReader.ReadToDescendant("child3"), true, "Cant find child3");
            CError.Compare(DataReader.ReadToDescendant("child4"), false, "Shouldn't find child4");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on EndElement");
            DataReader.Read();
            CError.Compare(DataReader.NodeType, XmlNodeType.Text, "Not on Element");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Read to Descendant on one level and again to level below it, with namespace", Pri = 1)]
        public int v7()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToDescendant("elem", "elem"), true, "Cant find elem");
            CError.Compare(DataReader.ReadToDescendant("child1", "elem"), true, "Cant find child1");
            CError.Compare(DataReader.ReadToDescendant("child2", "child2"), true, "Cant find child2");
            CError.Compare(DataReader.ReadToDescendant("child3", "child2"), true, "Cant find child3");
            CError.Compare(DataReader.ReadToDescendant("child4", "child2"), false, "Shouldn't find child4");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on EndElement");
            DataReader.Read();
            CError.Compare(DataReader.NodeType, XmlNodeType.Text, "Not on Element");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Read to Descendant on one level and again to level below it, with prefix", Pri = 1)]
        public int v8()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToDescendant("e:elem"), true, "Cant find elem");
            CError.Compare(DataReader.ReadToDescendant("e:child1"), true, "Cant find child1");
            CError.Compare(DataReader.ReadToDescendant("e:child2"), true, "Cant find child2");
            CError.Compare(DataReader.ReadToDescendant("e:child3"), true, "Cant find child3");
            CError.Compare(DataReader.ReadToDescendant("e:child4"), false, "Shouldn't find child4");
            CError.Compare(DataReader.NodeType, XmlNodeType.Element, "Not on EndElement");
            DataReader.Read();
            CError.Compare(DataReader.NodeType, XmlNodeType.Text, "Not on Element");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Multiple Reads to children and then next siblings, NNS", Pri = 2)]
        public int v9()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToDescendant("elem"), true, "Read fails elem");
            CError.Compare(DataReader.ReadToDescendant("child3"), true, "Read fails child3");
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

            CError.Compare(DataReader.ReadToDescendant("elem", "elem"), true, "Read fails elem");
            CError.Compare(DataReader.ReadToDescendant("child3", "child2"), true, "Read fails child3");
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

            CError.Compare(DataReader.ReadToDescendant("e:elem"), true, "Read fails elem");
            CError.Compare(DataReader.ReadToDescendant("e:child3"), true, "Read fails child3");
            CError.Compare(DataReader.ReadToNextSibling("e:child4"), true, "Read fails child4");

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
                    CError.Compare(DataReader.ReadToDescendant("child1"), false, "Fails on node");
                }
                else
                {
                    if (DataReader.HasAttributes)
                    {
                        while (DataReader.MoveToNextAttribute())
                        {
                            CError.Compare(DataReader.ReadToDescendant("abc"), false, "Fails on attribute node");
                        }
                    }
                }
            }

            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Interaction with MoveToContent", Pri = 2)]
        public int v13()
        {
            return TEST_SKIPPED;
        }

        [Variation("Only child has namespaces and read to it", Pri = 2)]
        public int v14()
        {
            ReloadSource(new StringReader(_xmlStr));
            DataReader.PositionOnElement("root");

            CError.Compare(DataReader.ReadToDescendant("child2", "child2"), true, "Fails on attribute node");

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
                DataReader.ReadToDescendant(null);
            }
            catch (ArgumentNullException)
            {
                CError.WriteLine("Caught for single param");
            }

            try
            {
                DataReader.ReadToDescendant("b", null);
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

            DataReader.ReadToDescendant("child1", "bar");
            CError.Compare(DataReader.IsEmptyElement, false, "Not on the correct node");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }


        //[Variation("On Root Node", Pri = 0, Params = new object[] { "NNS" })]
        //[Variation("On Root Node", Pri = 0, Params = new object[] { "DNS" })]
        //[Variation("On Root Node", Pri = 0, Params = new object[] { "NS" })]
        public int v18()
        {
            string type = CurVariation.Params[0].ToString();
            CError.WriteLine("Test Type : " + type);

            ReloadSource(new StringReader(_xmlStr));
            switch (type)
            {
                case "NNS":
                    DataReader.ReadToDescendant("elem");
                    if (DataReader.HasAttributes)
                    {
                        CError.WriteLine("Positioned on wrong element");
                        CError.WriteIgnore(DataReader.ReadInnerXml() + "\n");
                        return TEST_FAIL;
                    }
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;

                case "DNS":
                    DataReader.ReadToDescendant("elem", "elem");
                    if (DataReader.HasAttributes)
                    {
                        if (DataReader.GetAttribute("xmlns") == null)
                        {
                            CError.WriteLine("Positioned on wrong element, not on DNS");
                            return TEST_FAIL;
                        }
                    }
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;

                case "NS":
                    DataReader.ReadToDescendant("e:elem");
                    if (DataReader.HasAttributes)
                    {
                        if (DataReader.GetAttribute("xmlns:e") == null)
                        {
                            CError.WriteLine("Positioned on wrong element, not on NS");
                            return TEST_FAIL;
                        }
                    }
                    while (DataReader.Read()) ;
                    DataReader.Close();
                    return TEST_PASS;
                default:
                    throw new CTestFailedException("Error in Test type");
            }
        }

        [Variation("Assertion failed when call XmlReader.ReadToDescendant() for non-existing node", Pri = 1)]
        public int v19()
        {
            ReloadSource(new StringReader("<a>b</a>"));
            CError.Compare(DataReader.ReadToDescendant("foo"), false, "Should fail without assert");

            DataReader.Close();
            return TEST_PASS;
        }
    }
}
