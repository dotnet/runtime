// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeReaderFunctionalTests : TestModule
    {

        public partial class XNodeReaderTests : XLinqTestCase
        {
            public partial class TCDispose : BridgeHelpers
            {
                //[Variation("Test Integrity of all values after Dispose")]
                public void Variation1()
                {
                    XmlReader DataReader = GetReader();
                    ((IDisposable)DataReader).Dispose();

                    TestLog.Compare(DataReader.Name, string.Empty, "Name");
                    TestLog.Compare(DataReader.NodeType, XmlNodeType.None, "NodeType");
                    TestLog.Compare(DataReader.ReadState, ReadState.Closed, "ReadState");
                    TestLog.Compare(DataReader.AttributeCount, 0, "Attrib Count");
                    TestLog.Compare(DataReader.XmlLang, string.Empty, "XML Lang");
                    TestLog.Compare(DataReader.XmlSpace, XmlSpace.None, "Space");
                    TestLog.Compare(DataReader.BaseURI, string.Empty, "BaseUri");
                    TestLog.Compare(DataReader.Depth, 0, "Depth");
                    TestLog.Compare(DataReader.EOF, false, "EOF");
                    TestLog.Compare(DataReader.HasAttributes, false, "HasAttr");
                    TestLog.Compare(DataReader.HasValue, false, "HasValue");
                    TestLog.Compare(DataReader.IsDefault, false, "IsDefault");
                    TestLog.Compare(DataReader.LocalName, string.Empty, "LocalName");
                    try
                    {
                        TestLog.Compare(((IXmlLineInfo)DataReader).LineNumber, 0, "Line number");
                    }
                    catch (InvalidCastException) { }
                    try
                    {
                        TestLog.Compare(((IXmlLineInfo)DataReader).LinePosition, 0, "Line Position");
                    }
                    catch (InvalidCastException) { }
                    TestLog.Compare(DataReader.IsEmptyElement, false, "IsEmptyElement");
                    TestLog.Compare(DataReader.Read(), false, "Read");
                    TestLog.Compare(DataReader.ReadAttributeValue(), false, "ReadAV");
                    TestLog.Compare(DataReader.ReadInnerXml(), string.Empty, "ReadIX");
                    TestLog.Compare(DataReader.ReadOuterXml(), string.Empty, "ReadOX");
                }

                //[Variation("Call Dispose Multiple(3) Times")]
                public void Variation2()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        XmlReader r = n.CreateReader();
                        ((IDisposable)r).Dispose();
                        ((IDisposable)r).Dispose();
                        ((IDisposable)r).Dispose();
                    }
                }

                //GetXNode List
                public List<XNode> GetXNodeR()
                {
                    List<XNode> xNode = new List<XNode>();

                    xNode.Add(new XDocument(new XDocumentType("root", "", "", "<!ELEMENT root ANY>"), new XElement("root")));
                    xNode.Add(new XElement("elem1"));
                    xNode.Add(new XText("text1"));
                    xNode.Add(new XComment("comment1"));
                    xNode.Add(new XProcessingInstruction("pi1", "pi1pi1pi1pi1pi1"));
                    xNode.Add(new XCData("cdata cdata"));
                    xNode.Add(new XDocumentType("dtd1", "dtd1dtd1dtd1", "dtd1dtd1", "dtd1dtd1dtd1dtd1"));
                    return xNode;
                }
            }
        }
    }
}
