// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Collections.Generic;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    /////////////////////////////////////////////////////////////////////////
    // TestCase ReadOuterXml
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCReadSubtree : TCXMLReaderBaseGeneral
    {
        [Variation("ReadSubtree only works on Element Node")]
        public int ReadSubtreeWorksOnlyOnElementNode()
        {
            ReloadSource();

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
                        CError.WriteLine("ReadSubtree doesn't throw InvalidOp Exception on NodeType : " + nodeType);
                        return TEST_FAIL;
                    }

                    //now try next read
                    try
                    {
                        DataReader.Read();
                    }
                    catch (XmlException)
                    {
                        CError.WriteLine("Cannot Read after an invalid operation exception");
                        return TEST_FAIL;
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
                            CError.WriteLine("ReadSubtree doesn't throw InvalidOp Exception on Attribute Node Type");
                            return TEST_FAIL;
                        }

                        //now try next read.
                        try
                        {
                            DataReader.Read();
                        }
                        catch (XmlException)
                        {
                            CError.WriteLine("Cannot Read after an invalid operation exception");
                            return TEST_FAIL;
                        }
                    }
                }
            }//end while
            return TEST_PASS;
        }

        private string _xml = "<root><elem1><elempi/><?pi target?><elem2 xmlns='xyz'><elem/><!--Comment--><x:elem3 xmlns:x='pqr'><elem4 attr4='4'/></x:elem3></elem2></elem1><elem5/><elem6/></root>";

        //[Variation("ReadSubtree Test on Root", Pri = 0, Params = new object[]{"root", "", "ELEMENT", "", "", "NONE" })]
        //[Variation("ReadSubtree Test depth=1", Pri = 0, Params = new object[] { "elem1", "", "ELEMENT", "elem5", "", "ELEMENT" })]
        //[Variation("ReadSubtree Test depth=2", Pri = 0, Params = new object[] { "elem2", "", "ELEMENT", "elem1", "", "ENDELEMENT" })]
        //[Variation("ReadSubtree Test depth=3", Pri = 0, Params = new object[] { "x:elem3", "", "ELEMENT", "elem2", "", "ENDELEMENT" })]
        //[Variation("ReadSubtree Test depth=4", Pri = 0, Params = new object[] { "elem4", "", "ELEMENT", "x:elem3", "", "ENDELEMENT" })]
        //[Variation("ReadSubtree Test empty element", Pri = 0, Params = new object[] { "elem5", "", "ELEMENT", "elem6", "", "ELEMENT" })]
        //[Variation("ReadSubtree Test empty element before root", Pri = 0, Params = new object[] { "elem6", "", "ELEMENT", "root", "", "ENDELEMENT" })]
        //[Variation("ReadSubtree Test PI after element", Pri = 0, Params = new object[] { "elempi", "", "ELEMENT", "pi", "target", "PROCESSINGINSTRUCTION" })]
        //[Variation("ReadSubtree Test Comment after element", Pri = 0, Params = new object[] { "elem", "", "ELEMENT", "", "Comment", "COMMENT" })]
        public int v2()
        {
            int count = 0;
            string name = CurVariation.Params[count++].ToString();
            string value = CurVariation.Params[count++].ToString();
            string type = CurVariation.Params[count++].ToString();

            string oname = CurVariation.Params[count++].ToString();
            string ovalue = CurVariation.Params[count++].ToString();
            string otype = CurVariation.Params[count++].ToString();

            ReloadSource(new StringReader(_xml));
            DataReader.PositionOnElement(name);

            XmlReader r = DataReader.ReadSubtree();
            CError.Compare(r.ReadState, ReadState.Initial, "Reader state is not Initial");
            CError.Compare(r.Name, String.Empty, "Name is not empty");
            CError.Compare(r.NodeType, XmlNodeType.None, "Nodetype is not empty");
            CError.Compare(r.Depth, 0, "Depth is not zero");

            r.Read();

            CError.Compare(r.ReadState, ReadState.Interactive, "Reader state is not Interactive");
            CError.Compare(r.Name, name, "Subreader name doesn't match");
            CError.Compare(r.Value, value, "Subreader value doesn't match");
            CError.Compare(r.NodeType.ToString().ToUpperInvariant(), type, "Subreader nodetype doesn't match");
            CError.Compare(r.Depth, 0, "Subreader Depth is not zero");

            while (r.Read()) ;
            r.Dispose();

            CError.Compare(r.ReadState, ReadState.Closed, "Reader state is not Initial");
            CError.Compare(r.Name, String.Empty, "Name is not empty");
            CError.Compare(r.NodeType, XmlNodeType.None, "Nodetype is not empty");

            DataReader.Read();

            CError.Compare(DataReader.Name, oname, "Main name doesn't match");
            CError.Compare(DataReader.Value, ovalue, "Main value doesn't match");
            CError.Compare(DataReader.NodeType.ToString().ToUpperInvariant(), otype, "Main nodetype doesn't match");

            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("Read with entities", Pri = 1)]
        public int v3()
        {
            ReloadSource();
            DataReader.PositionOnElement("PLAY");

            XmlReader r = DataReader.ReadSubtree();

            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.EntityReference)
                {
                    if (r.CanResolveEntity)
                        r.ResolveEntity();
                }
            }

            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("Inner XML on Subtree reader", Pri = 1)]
        public int v4()
        {
            string xmlStr = "<elem1><elem2/></elem1>";
            ReloadSource(new StringReader(xmlStr));
            DataReader.PositionOnElement("elem1");
            XmlReader r = DataReader.ReadSubtree();
            r.Read();

            CError.Compare(r.ReadInnerXml(), "<elem2 />", "Inner Xml Fails");
            CError.Compare(r.Read(), false, "Read returns false");

            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("Outer XML on Subtree reader", Pri = 1)]
        public int v5()
        {
            string xmlStr = "<elem1><elem2/></elem1>";
            ReloadSource(new StringReader(xmlStr));
            DataReader.PositionOnElement("elem1");
            XmlReader r = DataReader.ReadSubtree();
            r.Read();

            CError.Compare(r.ReadOuterXml(), "<elem1><elem2 /></elem1>", "Outer Xml Fails");
            CError.Compare(r.Read(), false, "Read returns true");

            DataReader.Close();
            return TEST_PASS;
        }

        //[Variation("Close on inner reader with CloseInput should not close the outer reader", Pri = 1, Params = new object[] { "true" })]
        //[Variation("Close on inner reader with CloseInput should not close the outer reader", Pri = 1, Params = new object[] { "false" })]

        public int v7()
        {
            if (!(IsFactoryTextReader() || IsBinaryReader() || IsFactoryValidatingReader()))
            {
                return TEST_SKIPPED;
            }

            string fileName = GetTestFileName(EREADER_TYPE.GENERIC);
            bool ci = Boolean.Parse(CurVariation.Params[0].ToString());
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.CloseInput = ci;
            CError.WriteLine(ci);
            MyDict<string, object> options = new MyDict<string, object>();
            options.Add(ReaderFactory.HT_FILENAME, fileName);
            options.Add(ReaderFactory.HT_READERSETTINGS, settings);

            ReloadSource(options);

            DataReader.PositionOnElement("elem2");
            XmlReader r = DataReader.ReadSubtree();

            CError.Compare(DataReader.ReadState, ReadState.Interactive, "ReadState not interactive");

            DataReader.Close();
            return TEST_PASS;
        }

        private XmlReader NestRead(XmlReader r)
        {
            r.Read();
            r.Read();
            if (!(r.Name == "elem0" && r.NodeType == XmlNodeType.Element))
            {
                CError.WriteLine(r.Name);
                NestRead(r.ReadSubtree());
            }
            r.Dispose();
            return r;
        }

        [Variation("Nested Subtree reader calls", Pri = 2)]
        public int v8()
        {
            string xmlStr = "<elem1><elem2><elem3><elem4><elem5><elem6><elem7><elem8><elem9><elem0></elem0></elem9></elem8></elem7></elem6></elem5></elem4></elem3></elem2></elem1>";
            ReloadSource(new StringReader(xmlStr));
            XmlReader r = DataReader.Internal;

            NestRead(r);

            CError.Compare(r.ReadState, ReadState.Closed, "Reader Read State is not closed");
            return TEST_PASS;
        }

        [Variation("ReadSubtree for element depth more than 4K chars", Pri = 2)]
        public int v100()
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

            ReloadSource(new StringReader(mnw.GetNodes()));

            DataReader.PositionOnElement("ELEMENT_2");

            XmlReader r = DataReader.ReadSubtree();
            while (r.Read()) ;

            DataReader.Read();

            CError.Compare(DataReader.Name, "ELEMENT_1", "Main name doesn't match");
            CError.Compare(DataReader.Value, "", "Main value doesn't match");
            CError.Compare(DataReader.NodeType.ToString().ToUpperInvariant(), "ENDELEMENT", "Main nodetype doesn't match");

            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("Multiple Namespaces on Subtree reader", Pri = 1)]
        public int SubtreeReaderCanDealWithMultipleNamespaces()
        {
            string xmlStr = "<root xmlns:p1='a' xmlns:p2='b'><e p1:a='' p2:a=''></e></root>";
            ReloadSource(new StringReader(xmlStr));
            DataReader.PositionOnElement("e");
            XmlReader r = DataReader.ReadSubtree();
            while (r.Read()) ;
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("Subtree Reader caches the NodeType and reports node type of Attribute on subsequent reads.", Pri = 1)]
        public int SubtreeReaderReadsProperlyNodeTypeOfAttributes()
        {
            string xmlStr = "<root xmlns='foo'><b blah='blah'/><b/></root>";
            ReloadSource(new StringReader(xmlStr));
            DataReader.PositionOnElement("root");
            XmlReader xxr = DataReader.ReadSubtree();

            xxr.Read(); //Now on root.
            CError.Compare(xxr.Name, "root", "Root Elem");
            CError.Compare(xxr.MoveToNextAttribute(), true, "MTNA 1");

            CError.Compare(xxr.NodeType, XmlNodeType.Attribute, "XMLNS NT");
            CError.Compare(xxr.Name, "xmlns", "XMLNS Attr");
            CError.Compare(xxr.Value, "foo", "XMLNS Value");
            CError.Compare(xxr.MoveToNextAttribute(), false, "MTNA 2");

            xxr.Read(); //Now on b.
            CError.Compare(xxr.Name, "b", "b Elem");
            CError.Compare(xxr.MoveToNextAttribute(), true, "MTNA 3");

            CError.Compare(xxr.NodeType, XmlNodeType.Attribute, "blah NT");
            CError.Compare(xxr.Name, "blah", "blah Attr");
            CError.Compare(xxr.Value, "blah", "blah Value");
            CError.Compare(xxr.MoveToNextAttribute(), false, "MTNA 4");

            xxr.Read(); //Now on /b.
            CError.Compare(xxr.Name, "b", "b EndElem");
            CError.Compare(xxr.NodeType, XmlNodeType.Element, "b Elem NT");
            CError.Compare(xxr.MoveToNextAttribute(), false, "MTNA 5");

            xxr.Read(); //Now on /root.
            CError.Compare(xxr.Name, "root", "root EndElem");
            CError.Compare(xxr.NodeType, XmlNodeType.EndElement, "root EndElem NT");
            CError.Compare(xxr.MoveToNextAttribute(), false, "MTNA 6");

            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("XmlSubtreeReader add duplicate namespace declaration")]
        public int XmlSubtreeReaderDoesntDuplicateLocalNames()
        {
            Dictionary<string, object> localNames = new Dictionary<string, object>();

            string xml = "<?xml version='1.0' encoding='utf-8'?>" +
                "<IXmlSerializable z:CLRType='A' z:ClrAssembly='test, " +
                "Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' " +
                "xmlns='http://schemas.datacontract.org' xmlns:z='http://schemas.microsoft.com' >" +
                "<WriteAttributeString p3:attributeName3='attributeValue3' " +
                "abc:attributeName='attributeValue' attributeName2='attributeValue2' " +
                "xmlns:abc='myNameSpace' xmlns:p3='myNameSpace3' /></IXmlSerializable>";

            ReloadSourceStr(xml);

            DataReader.MoveToContent();
            XmlReader reader = DataReader.ReadSubtree();

            reader.ReadToDescendant("WriteAttributeString");

            while (reader.MoveToNextAttribute())
            {
                if (localNames.ContainsKey(reader.LocalName))
                {
                    CError.WriteLine("Duplicated LocalName: {0}", reader.LocalName);
                    return TEST_FAIL;
                }
                localNames.Add(reader.LocalName, null);
            }
            return TEST_PASS;
        }

        [Variation("XmlSubtreeReader adds duplicate namespace declaration")]
        public int XmlSubtreeReaderDoesntAddMultipleNamespaceDeclarations()
        {
            ReloadSource(new StringReader("<r xmlns:a='X'><a:e/></r>"));
            DataReader.Read();
            DataReader.Read();
            if (IsBinaryReader())
                DataReader.Read();

            XmlReader r1 = DataReader.ReadSubtree();
            r1.Read();
            XmlReader r2 = r1.ReadSubtree();
            r2.Read();
            string xml = r2.ReadOuterXml();
            CError.Compare(xml, "<a:e xmlns:a=\"X\" />", "Mismatch");
            return TEST_PASS;
        }

        [Variation("XmlSubtreeReader.Dispose disposes the main reader")]
        public int XmlReaderDisposeDoesntDisposeMainReader()
        {
            ReloadSource(new StringReader("<a><b></b></a>"));
            DataReader.PositionOnElement("b");

            using (XmlReader subtreeReader = DataReader.ReadSubtree()) { }

            if (DataReader.NodeType.ToString() == "EndElement" && DataReader.Name.ToString() == "b" && DataReader.Read() == true)
                return TEST_PASS;

            return TEST_FAIL;
        }

        private string[] _s = new string[] {
                "<root xmlns:p1='a' xmlns:p2='b'><e p1:a='' p2:a=''></e></root>",
                "<root xmlns:p1='a' xmlns:p2='b'><e p1:a='' p2:a='' xmlns:p2='b' ></e></root>",
                "<root xmlns:p1='a' xmlns:p2='b'><e xmlns:p2='b' p1:a='' p2:a='' ></e></root>",
                "<root xmlns:p1='a' ><e p1:a='' p2:a='' xmlns:p2='b' ></e></root>",
                "<root xmlns:p2='b'><e xmlns:p1='a' p1:a='' p2:a=''></e></root>",
                "<root xmlns:p1='a' xmlns:p2='b'><e p1:a='' p2:a='' xmlns:p1='a' xmlns:p2='b'></e></root>"
            };
        private string[][] _exp = {
            new string[] {"p1:a", "p2:a", "xmlns:p1", "xmlns:p2"},
            new string[] {"p1:a", "p2:a", "xmlns:p2", "xmlns:p1"},
            new string[] {"xmlns:p2", "p1:a", "p2:a", "xmlns:p1" },
            new string[] {"p1:a", "p2:a", "xmlns:p2", "xmlns:p1"},
            new string[] {"xmlns:p1", "p1:a", "p2:a", "xmlns:p2"},
            new string[] {"p1:a", "p2:a", "xmlns:p1", "xmlns:p2"},
        };

        private string[][] _expXpath = {
            new string[] {"p1:a", "p2:a", "xmlns:p1", "xmlns:p2"},
            new string[] {"xmlns:p2", "p1:a", "p2:a", "xmlns:p1"},
            new string[] {"xmlns:p2", "p1:a", "p2:a", "xmlns:p1" },
            new string[] {"xmlns:p2", "p1:a", "p2:a", "xmlns:p1"},
            new string[] {"xmlns:p1", "p1:a", "p2:a", "xmlns:p2"},
            new string[] {"xmlns:p1", "xmlns:p2", "p1:a", "p2:a"},
        };

        private string[][] _expXslt = {
            new string[] {"p1:a", "p2:a", "xmlns:p1", "xmlns:p2"},
            new string[] {"p1:a", "p2:a", "xmlns:p1", "xmlns:p2"},
            new string[] {"p1:a", "p2:a", "xmlns:p1", "xmlns:p2" },
            new string[] {"p1:a", "p2:a", "xmlns:p2", "xmlns:p1"},
            new string[] {"p1:a", "p2:a", "xmlns:p1", "xmlns:p2"},
            new string[] {"p1:a", "p2:a", "xmlns:p1", "xmlns:p2"},
        };

        //[Variation("0. XmlReader.Name inconsistent when reading namespace node attribute", Param = 0)]
        //[Variation("1. XmlReader.Name inconsistent when reading namespace node attribute", Param = 1)]
        //[Variation("2. XmlReader.Name inconsistent when reading namespace node attribute", Param = 2)]
        //[Variation("3. XmlReader.Name inconsistent when reading namespace node attribute", Param = 3)]
        //[Variation("4. XmlReader.Name inconsistent when reading namespace node attribute", Param = 4)]
        //[Variation("5. XmlReader.Name inconsistent when reading namespace node attribute", Param = 5)]
        public int XmlReaderNameIsConsistentWhenReadingNamespaceNodeAttribute()
        {
            int param = (int)CurVariation.Param;

            ReloadSource(new StringReader(_s[param]));
            DataReader.PositionOnElement("e");
            using (XmlReader r = DataReader.ReadSubtree())
            {
                while (r.Read())
                {
                    for (int i = 0; i < r.AttributeCount; i++)
                    {
                        r.MoveToAttribute(i);
                        if (IsXPathNavigatorReader())
                            CError.Compare(r.Name, _expXpath[param][i], "Error");
                        else if (IsXsltReader())
                            CError.Compare(r.Name, _expXslt[param][i], "Error");
                        else
                            CError.Compare(r.Name, _exp[param][i], "Error");
                    }
                }
            }
            return TEST_PASS;
        }

        [Variation("Indexing methods cause infinite recursion & stack overflow")]
        public int IndexingMethodsWorksProperly()
        {
            string xml = "<e1  a='a1' b='b1'> 123 <e2 a='a2' b='b2'> abc</e2><e3 b='b3'/></e1>";
            ReloadSourceStr(xml);
            DataReader.Read();
            XmlReader r2 = DataReader.ReadSubtree();
            r2.Read();
            CError.Compare(r2[0], "a1", "Error 1");
            CError.Compare(r2["b"], "b1", "Error 2");
            CError.Compare(r2["a", null], "a1", "Error 3");
            return TEST_PASS;
        }

        //[Variation("1. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 1)]
        //[Variation("2. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 2)]
        //[Variation("3. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 3)]
        //[Variation("4. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 4)]
        //[Variation("5. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 5)]
        //[Variation("6. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 6)]
        //[Variation("7. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 7)]
        //[Variation("8. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 8)]
        //[Variation("9. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 9)]
        //[Variation("10. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 10)]
        //[Variation("11. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 11)]
        //[Variation("12. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 12)]
        //[Variation("13. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 13)]
        //[Variation("14. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 14)]
        //[Variation("15. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 15)]
        //[Variation("16. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 16)]
        //[Variation("17. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 17)]
        //[Variation("18. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 18)]
        //[Variation("19. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 19)]
        //[Variation("20. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 20)]
        //[Variation("21. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 21)]
        //[Variation("22. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 22)]
        //[Variation("23. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 23)]
        //[Variation("24. Close on a subtree reader that is in error state gets doesn't it into in infinite loop", Param = 24)]
        public int DisposingSubtreeReaderThatIsInErrorStateWorksProperly()
        {
            int param = (int)CurVariation.Param;
            byte[] b = new byte[4];
            string xml = "<Report><Account><Balance>-4,095,783.00" +
                       "</Balance><LastActivity>2006/01/05</LastActivity>" +
                       "</Account></Report>";
            ReloadSourceStr(xml);
            while (DataReader.Name != "Account")
                DataReader.Read();
            XmlReader sub = DataReader.ReadSubtree();

            while (sub.Read())
            {
                if (sub.Name == "Balance")
                {
                    try
                    {
                        switch (param)
                        {
                            case 1: decimal num1 = sub.ReadElementContentAsDecimal(); break;
                            case 2: object num2 = sub.ReadElementContentAs(typeof(float), null); break;
                            case 3: bool num3 = sub.ReadElementContentAsBoolean(); break;
                            case 5: float num5 = sub.ReadElementContentAsFloat(); break;
                            case 6: double num6 = sub.ReadElementContentAsDouble(); break;
                            case 7: int num7 = sub.ReadElementContentAsInt(); break;
                            case 8: long num8 = sub.ReadElementContentAsLong(); break;
                            case 9: object num9 = sub.ReadElementContentAs(typeof(double), null); break;
                            case 10: object num10 = sub.ReadElementContentAs(typeof(decimal), null); break;
                            case 11: sub.Read(); decimal num11 = sub.ReadContentAsDecimal(); break;
                            case 12: sub.Read(); object num12 = sub.ReadContentAs(typeof(float), null); break;
                            case 13: sub.Read(); bool num13 = sub.ReadContentAsBoolean(); break;
                            case 15: sub.Read(); float num15 = sub.ReadContentAsFloat(); break;
                            case 16: sub.Read(); double num16 = sub.ReadContentAsDouble(); break;
                            case 17: sub.Read(); int num17 = sub.ReadContentAsInt(); break;
                            case 18: sub.Read(); long num18 = sub.ReadContentAsLong(); break;
                            case 19: sub.Read(); object num19 = sub.ReadContentAs(typeof(double), null); break;
                            case 20: sub.Read(); object num20 = sub.ReadContentAs(typeof(decimal), null); break;
                            case 21: object num21 = sub.ReadElementContentAsBase64(b, 0, 2); break;
                            case 22: object num22 = sub.ReadElementContentAsBinHex(b, 0, 2); break;
                            case 23: sub.Read(); object num23 = sub.ReadContentAsBase64(b, 0, 2); break;
                            case 24: sub.Read(); object num24 = sub.ReadContentAsBinHex(b, 0, 2); break;
                        }
                    }
                    catch (XmlException)
                    {
                        try
                        {
                            switch (param)
                            {
                                case 1: decimal num1 = sub.ReadElementContentAsDecimal(); break;
                                case 2: object num2 = sub.ReadElementContentAs(typeof(float), null); break;
                                case 3: bool num3 = sub.ReadElementContentAsBoolean(); break;
                                case 5: float num5 = sub.ReadElementContentAsFloat(); break;
                                case 6: double num6 = sub.ReadElementContentAsDouble(); break;
                                case 7: int num7 = sub.ReadElementContentAsInt(); break;
                                case 8: long num8 = sub.ReadElementContentAsLong(); break;
                                case 9: object num9 = sub.ReadElementContentAs(typeof(double), null); break;
                                case 10: object num10 = sub.ReadElementContentAs(typeof(decimal), null); break;
                                case 11: sub.Read(); decimal num11 = sub.ReadContentAsDecimal(); break;
                                case 12: sub.Read(); object num12 = sub.ReadContentAs(typeof(float), null); break;
                                case 13: sub.Read(); bool num13 = sub.ReadContentAsBoolean(); break;
                                case 15: sub.Read(); float num15 = sub.ReadContentAsFloat(); break;
                                case 16: sub.Read(); double num16 = sub.ReadContentAsDouble(); break;
                                case 17: sub.Read(); int num17 = sub.ReadContentAsInt(); break;
                                case 18: sub.Read(); long num18 = sub.ReadContentAsLong(); break;
                                case 19: sub.Read(); object num19 = sub.ReadContentAs(typeof(double), null); break;
                                case 20: sub.Read(); object num20 = sub.ReadContentAs(typeof(decimal), null); break;
                                case 21: object num21 = sub.ReadElementContentAsBase64(b, 0, 2); break;
                                case 22: object num22 = sub.ReadElementContentAsBinHex(b, 0, 2); break;
                                case 23: sub.Read(); object num23 = sub.ReadContentAsBase64(b, 0, 2); break;
                                case 24: sub.Read(); object num24 = sub.ReadContentAsBinHex(b, 0, 2); break;
                            }
                        }
                        catch (InvalidOperationException) { return TEST_PASS; }
                        catch (XmlException) { return TEST_PASS; }
                        if (param == 24 || param == 23) return TEST_PASS;
                    }
                    catch (NotSupportedException) { return TEST_PASS; }
                }
            }
            return TEST_FAIL;
        }

        [Variation("SubtreeReader has empty namespace")]
        public int v101()
        {
            string xml = @"<a xmlns:f='urn:foobar' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" +
               "<b><c xsi:type='f:mytype'>some content</c></b></a>";
            ReloadSourceStr(xml);

            DataReader.Read(); CError.Compare(DataReader.Name, "a", "a");
            DataReader.Read(); CError.Compare(DataReader.Name, "b", "b");
            using (XmlReader subtree = DataReader.ReadSubtree())
            {
                subtree.Read(); CError.Compare(subtree.Name, "b", "b2");
                subtree.Read(); CError.Compare(subtree.Name, "c", "c");
                subtree.MoveToAttribute("type", "http://www.w3.org/2001/XMLSchema-instance");
                CError.Compare(subtree.Value, "f:mytype", "value");
                string ns = subtree.LookupNamespace("f");
                if (ns == null) { return TEST_PASS; }
            }
            return TEST_FAIL;
        }

        [Variation("ReadValueChunk on an xmlns attribute that has been added by the subtree reader")]
        public int v102()
        {
            char[] c = new char[10];
            string xml = @"<a xmlns:f='urn:foobar' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" +
                       "<b><c xsi:type='f:mytype'>some content</c></b></a>";
            ReloadSourceStr(xml);
            DataReader.Read();
            using (XmlReader subtree = DataReader.ReadSubtree())
            {
                subtree.Read();
                CError.Compare(subtree.Name, "a", "a");
                string s = subtree[0];
                CError.Compare(s, "urn:foobar", "urn:foobar");
                CError.Compare(subtree.LookupNamespace("xmlns"), "http://www.w3.org/2000/xmlns/", "xmlns");
                CError.Compare(subtree.MoveToFirstAttribute(), "True");
                try
                {
                    CError.Compare(subtree.ReadValueChunk(c, 0, 10), 10, "ReadValueChunk");
                    CError.Compare(c[0].ToString(), "u", "u");
                    CError.Compare(c[9].ToString(), "r", "r");
                }
                catch (NotSupportedException) { if (IsCustomReader() || IsCharCheckingReader()) return TEST_PASS; }
            }
            return TEST_PASS;
        }
    }
}
