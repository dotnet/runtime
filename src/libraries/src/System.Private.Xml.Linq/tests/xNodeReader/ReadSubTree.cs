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
            public partial class TCReadSubtree : BridgeHelpers
            {
                //[Variation("ReadSubtree only works on Element Node")]
                public void ReadSubtreeOnlyWorksOnElementNode()
                {
                    XmlReader DataReader = GetReader();

                    while (DataReader.Read())
                    {
                        if (DataReader.NodeType != XmlNodeType.Element)
                        {
                            string nodeType = DataReader.NodeType.ToString();
                            bool flag = true;
                            try
                            {
                                DataReader.ReadSubtree();
                            }
                            catch (InvalidOperationException)
                            {
                                flag = false;
                            }

                            if (flag)
                            {
                                TestLog.WriteLine("ReadSubtree doesn't throw InvalidOp Exception on NodeType : " + nodeType);
                                throw new TestException(TestResult.Failed, "");
                            }

                            // now try next read
                            try
                            {
                                DataReader.Read();
                            }
                            catch (XmlException)
                            {
                                TestLog.WriteLine("Cannot Read after an invalid operation exception");
                                throw new TestException(TestResult.Failed, "");
                            }
                        }
                        else
                        {
                            if (DataReader.HasAttributes)
                            {
                                bool flag = true;
                                DataReader.MoveToFirstAttribute();
                                try
                                {
                                    DataReader.ReadSubtree();
                                }
                                catch (InvalidOperationException)
                                {
                                    flag = false;
                                }
                                if (flag)
                                {
                                    TestLog.WriteLine("ReadSubtree doesn't throw InvalidOp Exception on Attribute Node Type");
                                    throw new TestException(TestResult.Failed, "");
                                }

                                //now try next read.
                                try
                                {
                                    DataReader.Read();
                                }
                                catch (XmlException)
                                {
                                    TestLog.WriteLine("Cannot Read after an invalid operation exception");
                                    throw new TestException(TestResult.Failed, "");
                                }
                            }
                        }
                    }//end while
                }

                private string _xml = "<root><elem1><elempi/><?pi target?><elem2 xmlns='xyz'><elem/><!--Comment--><x:elem3 xmlns:x='pqr'><elem4 attr4='4'/></x:elem3></elem2></elem1><elem5/><elem6/></root>";

                //[Variation("ReadSubtree Test on Root", Priority = 0, Params = new object[] { "root", "", "ELEMENT", "", "", "NONE" })]
                //[Variation("ReadSubtree Test depth=1", Priority = 0, Params = new object[] { "elem1", "", "ELEMENT", "elem5", "", "ELEMENT" })]
                //[Variation("ReadSubtree Test depth=2", Priority = 0, Params = new object[] { "elem2", "", "ELEMENT", "elem1", "", "ENDELEMENT" })]
                //[Variation("ReadSubtree Test depth=3", Priority = 0, Params = new object[] { "x:elem3", "", "ELEMENT", "elem2", "", "ENDELEMENT" })]
                //[Variation("ReadSubtree Test depth=4", Priority = 0, Params = new object[] { "elem4", "", "ELEMENT", "x:elem3", "", "ENDELEMENT" })]
                //[Variation("ReadSubtree Test empty element", Priority = 0, Params = new object[] { "elem5", "", "ELEMENT", "elem6", "", "ELEMENT" })]
                //[Variation("ReadSubtree Test empty element before root", Priority = 0, Params = new object[] { "elem6", "", "ELEMENT", "root", "", "ENDELEMENT" })]
                //[Variation("ReadSubtree Test PI after element", Priority = 0, Params = new object[] { "elempi", "", "ELEMENT", "pi", "target", "PROCESSINGINSTRUCTION" })]
                //[Variation("ReadSubtree Test Comment after element", Priority = 0, Params = new object[] { "elem", "", "ELEMENT", "", "Comment", "COMMENT" })]
                public void v2()
                {
                    int count = 0;
                    string name = Variation.Params[count++].ToString();
                    string value = Variation.Params[count++].ToString();
                    string type = Variation.Params[count++].ToString();

                    string oname = Variation.Params[count++].ToString();
                    string ovalue = Variation.Params[count++].ToString();
                    string otype = Variation.Params[count++].ToString();

                    XmlReader DataReader = GetReader(new StringReader(_xml));
                    PositionOnElement(DataReader, name);

                    XmlReader r = DataReader.ReadSubtree();
                    TestLog.Compare(r.ReadState, ReadState.Initial, "Reader state is not Initial");
                    TestLog.Compare(r.Name, String.Empty, "Name is not empty");
                    TestLog.Compare(r.NodeType, XmlNodeType.None, "Nodetype is not empty");
                    TestLog.Compare(r.Depth, 0, "Depth is not zero");

                    r.Read();

                    TestLog.Compare(r.ReadState, ReadState.Interactive, "Reader state is not Interactive");
                    TestLog.Compare(r.Name, name, "Subreader name doesn't match");
                    TestLog.Compare(r.Value, value, "Subreader value doesn't match");
                    TestLog.Compare(r.NodeType.ToString().ToUpperInvariant(), type, "Subreader nodetype doesn't match");
                    TestLog.Compare(r.Depth, 0, "Subreader Depth is not zero");

                    while (r.Read()) ;
                    r.Dispose();

                    TestLog.Compare(r.ReadState, ReadState.Closed, "Reader state is not Initial");
                    TestLog.Compare(r.Name, String.Empty, "Name is not empty");
                    TestLog.Compare(r.NodeType, XmlNodeType.None, "Nodetype is not empty");

                    DataReader.Read();

                    TestLog.Compare(DataReader.Name, oname, "Main name doesn't match");
                    TestLog.Compare(DataReader.Value, ovalue, "Main value doesn't match");
                    TestLog.Compare(DataReader.NodeType.ToString().ToUpperInvariant(), otype, "Main nodetype doesn't match");

                    DataReader.Dispose();
                }

                //[Variation("Read with entities", Priority = 1)]
                public void v3()
                {
                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, "PLAY");

                    XmlReader r = DataReader.ReadSubtree();

                    while (r.Read())
                    {
                        if (r.NodeType == XmlNodeType.EntityReference)
                        {
                            if (r.CanResolveEntity)
                                r.ResolveEntity();
                        }
                    }

                    r.Dispose();
                    DataReader.Dispose();
                }

                //[Variation("Inner XML on Subtree reader", Priority = 1)]
                public void v4()
                {
                    string xmlStr = "<elem1><elem2/></elem1>";
                    XmlReader DataReader = GetReaderStr(xmlStr);
                    PositionOnElement(DataReader, "elem1");
                    XmlReader r = DataReader.ReadSubtree();
                    r.Read();
                    TestLog.Compare(r.ReadInnerXml(), "<elem2 />", "Inner Xml Fails");
                    TestLog.Compare(r.Read(), false, "Read returns false");
                    r.Dispose();
                    DataReader.Dispose();
                }

                //[Variation("Outer XML on Subtree reader", Priority = 1)]
                public void v5()
                {
                    string xmlStr = "<elem1><elem2/></elem1>";
                    XmlReader DataReader = GetReaderStr(xmlStr);
                    PositionOnElement(DataReader, "elem1");
                    XmlReader r = DataReader.ReadSubtree();
                    r.Read();
                    TestLog.Compare(r.ReadOuterXml(), "<elem1><elem2 /></elem1>", "Outer Xml Fails");
                    TestLog.Compare(r.Read(), false, "Read returns true");
                    r.Dispose();
                    DataReader.Dispose();
                }

                //[Variation("ReadString on Subtree reader", Priority = 1)]
                public void v6()
                {
                    string xmlStr = "<elem1><elem2/></elem1>";
                    XmlReader DataReader = GetReaderStr(xmlStr);
                    PositionOnElement(DataReader, "elem1");
                    XmlReader r = DataReader.ReadSubtree();
                    r.Read();
                    TestLog.Compare(r.Read(), true, "Read returns false");

                    r.Dispose();
                    DataReader.Dispose();
                }

                //[Variation("Close on inner reader with CloseInput should not close the outer reader", Priority = 1, Params = new object[] { "true" })]
                //[Variation("Close on inner reader with CloseInput should not close the outer reader", Priority = 1, Params = new object[] { "false" })]

                public void v7()
                {
                    XmlReader DataReader = GetReader();
                    bool ci = Boolean.Parse(Variation.Params[0].ToString());
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.CloseInput = ci;

                    PositionOnElement(DataReader, "elem2");
                    XmlReader r = DataReader.ReadSubtree();
                    r.Dispose();

                    TestLog.Compare(DataReader.ReadState, ReadState.Interactive, "ReadState not interactive");

                    DataReader.Dispose();
                }

                private XmlReader NestRead(XmlReader r)
                {
                    r.Read();
                    r.Read();
                    if (!(r.Name == "elem0" && r.NodeType == XmlNodeType.Element))
                    {
                        NestRead(r.ReadSubtree());
                    }
                    r.Dispose();
                    return r;
                }

                //[Variation("Nested Subtree reader calls", Priority = 2)]
                public void v8()
                {
                    string xmlStr = "<elem1><elem2><elem3><elem4><elem5><elem6><elem7><elem8><elem9><elem0></elem0></elem9></elem8></elem7></elem6></elem5></elem4></elem3></elem2></elem1>";
                    XmlReader r = GetReader(new StringReader(xmlStr));

                    NestRead(r);
                    TestLog.Compare(r.ReadState, ReadState.Closed, "Reader Read State is not closed");
                }

                //[Variation("ReadSubtree for element depth more than 4K chars", Priority = 2)]
                public void v100()
                {
                    ManagedNodeWriter mnw = new ManagedNodeWriter();
                    mnw.PutPattern("X");
                    do
                    {
                        mnw.OpenElement();
                        mnw.CloseElement();
                    }
                    while (mnw.GetNodes().Length < 4096);

                    mnw.Finish();

                    XmlReader DataReader = GetReader(new StringReader(mnw.GetNodes()));

                    PositionOnElement(DataReader, "ELEMENT_2");

                    XmlReader r = DataReader.ReadSubtree();
                    while (r.Read()) ;
                    r.Dispose();

                    DataReader.Read();

                    TestLog.Compare(DataReader.Name, "ELEMENT_1", "Main name doesn't match");
                    TestLog.Compare(DataReader.Value, "", "Main value doesn't match");
                    TestLog.Compare(DataReader.NodeType.ToString().ToUpperInvariant(), "ENDELEMENT", "Main nodetype doesn't match");

                    DataReader.Dispose();
                }

                //[Variation("Multiple Namespaces on Subtree reader", Priority = 1)]
                public void MultipleNamespacesOnSubtreeReader()
                {
                    string xmlStr = "<root xmlns:p1='a' xmlns:p2='b'><e p1:a='' p2:a=''></e></root>";
                    XmlReader DataReader = GetReader(new StringReader(xmlStr));
                    PositionOnElement(DataReader, "e");
                    XmlReader r = DataReader.ReadSubtree();
                    while (r.Read()) ;
                    r.Dispose();
                    DataReader.Dispose();
                }

                //[Variation("Subtree Reader caches the NodeType and reports node type of Attribute on subsequent reads.", Priority = 1)]
                public void SubtreeReaderCachesNodeTypeAndReportsNodeTypeOfAttributeOnSubsequentReads()
                {
                    string xmlStr = "<root xmlns='foo'><b blah='blah'/><b/></root>";
                    XmlReader DataReader = GetReader(new StringReader(xmlStr));
                    PositionOnElement(DataReader, "root");
                    XmlReader xxr = DataReader.ReadSubtree();

                    //Now on root.
                    xxr.Read();
                    TestLog.Compare(xxr.Name, "root", "Root Elem");
                    TestLog.Compare(xxr.MoveToNextAttribute(), true, "MTNA 1");

                    TestLog.Compare(xxr.NodeType, XmlNodeType.Attribute, "XMLNS NT");
                    TestLog.Compare(xxr.Name, "xmlns", "XMLNS Attr");
                    TestLog.Compare(xxr.Value, "foo", "XMLNS Value");
                    TestLog.Compare(xxr.MoveToNextAttribute(), false, "MTNA 2");

                    //Now on b.
                    xxr.Read();
                    TestLog.Compare(xxr.Name, "b", "b Elem");
                    TestLog.Compare(xxr.MoveToNextAttribute(), true, "MTNA 3");

                    TestLog.Compare(xxr.NodeType, XmlNodeType.Attribute, "blah NT");
                    TestLog.Compare(xxr.Name, "blah", "blah Attr");
                    TestLog.Compare(xxr.Value, "blah", "blah Value");
                    TestLog.Compare(xxr.MoveToNextAttribute(), false, "MTNA 4");

                    // Now on /b.
                    xxr.Read();
                    TestLog.Compare(xxr.Name, "b", "b EndElem");
                    TestLog.Compare(xxr.NodeType, XmlNodeType.Element, "b Elem NT");
                    TestLog.Compare(xxr.MoveToNextAttribute(), false, "MTNA 5");

                    xxr.Read();
                    TestLog.Compare(xxr.Name, "root", "root EndElem");
                    TestLog.Compare(xxr.NodeType, XmlNodeType.EndElement, "root EndElem NT");
                    TestLog.Compare(xxr.MoveToNextAttribute(), false, "MTNA 6");

                    xxr.Dispose();

                    DataReader.Dispose();
                }
            }
        }
    }
}
