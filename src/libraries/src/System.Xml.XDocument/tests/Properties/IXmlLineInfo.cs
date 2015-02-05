// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;
using XmlCoreTest.Common;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class PropertiesTests : XLinqTestCase
        {
            //[TestCase(Name = "IXmlLineInfo & BaseURI")]
            public partial class ILineInfoTests : XLinqTestCase
            {
                public static string[] skipFiles =  {
                                                    @"p15pass1.xml",
                                                    @"ibm14v03.xml",
                                                    @"ibm66v01.xml",
                                                    @"vs162.xml",
                                                    @"vx027.xml",
                                                    @"XLinq_Perf_Plan.xml"
                                                 };

                //[Variation(Priority = 0, Desc = "Cast to Interface")]
                public void CastToInterface()
                {
                    XDocument doc = new XDocument();
                    TestLog.Compare(doc is IXmlLineInfo, "doc is IXmlLineInfo");
                    TestLog.Compare(doc.CreateReader() is IXmlLineInfo, "doc.CreateReader() is IXmlLineInfo");
                }

                protected override void DetermineChildren()
                {
                    base.DetermineChildren();
                }

                public TestVariation CreateNewVariation(string descPrefix, string uri)
                {
                    TestVariation variation = new TestVariation(CompareReaders, descPrefix);
                    variation.Attribute = new VariationAttribute(descPrefix + " :: " + uri, new object[] { uri });
                    return variation;
                }

                public void CompareReaders()
                {
                    string uri = CurrentChild.Params[0] as string;

                    string[] tempFile = uri.Split('/', '\\');
                    if (skipFiles.Contains(tempFile[tempFile.Count() - 1]))
                        throw new TestSkippedException("Filtered out ...");


                    XmlReaderSettings rs = new XmlReaderSettings();
                    rs.DtdProcessing = DtdProcessing.Ignore;
                    XmlNodeType[] textNodeTypes = { XmlNodeType.Text, XmlNodeType.Whitespace, XmlNodeType.SignificantWhitespace };

                    XDocument doc;

                    using (XmlReader rIn = XmlReader.Create(uri, rs))
                    {
                        doc = XDocument.Load(rIn, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
                    }

                    using (XmlReader rXN = doc.CreateReader())
                    using (XmlReader rIn = XmlReader.Create(uri, rs))
                    {
                        // Check baseUri for the readers in the Initial state
                        TestLog.Compare(rXN.BaseURI, rIn.BaseURI, "(Initial) baseURI ::: " + uri);

                        // Following is the trusted parallel navigation : assumed that the reader wont go out of sync
                        while (rXN.Read() && rIn.Read())
                        {
                            #region asserts
                            if (rIn.NodeType == XmlNodeType.XmlDeclaration) rIn.Read();

                            if (rXN.NodeType != rIn.NodeType && !textNodeTypes.Contains(rXN.NodeType))
                            {
                                continue;
                            }
                            #endregion

                            TestLog.Compare(rXN.BaseURI, rIn.BaseURI, "baseURI ::: " + uri);
                            VerifyLineInfo((IXmlLineInfo)rXN, (IXmlLineInfo)rIn, uri);
                            while (rXN.MoveToNextAttribute() && rIn.MoveToNextAttribute())
                            {
                                TestLog.Compare(rXN.BaseURI == rIn.BaseURI, "baseURI ::: " + uri);
                                VerifyLineInfo((IXmlLineInfo)rXN, (IXmlLineInfo)rIn, uri);
                            }
                        }
                    }
                }

                //[Variation(Priority = 0, Desc = "XDocument/XElement - BaseUri, Reader in Initial state", Param = @"TestData\XLinq\config.xml")]
                public void BaseUriInitial()
                {
                    string fileName = CurrentChild.Param as string;
                    XDocument doc = XDocument.Load(FilePathUtil.getStream(fileName));
                    TestLog.Compare(doc.BaseUri, doc.CreateReader().BaseURI, "XDocument BaseUri");

                    XElement elem = XElement.Load(FilePathUtil.getStream(fileName));
                    TestLog.Compare(elem.BaseUri, elem.CreateReader().BaseURI, "XElement BaseUri");
                }

                //[Variation(Priority = 0, Desc = "XElement - BaseUri, Reader in Initial state, all nodes", Param = @"TestData\XLinq\IXmlLineInfoTests\company-data.xml")]
                public void AllNodesTests()
                {
                    string fileName = CurrentChild.Param as string;
                    using (XmlReader r = XmlReader.Create(FilePathUtil.getStream(fileName), new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore }))
                    {
                        XDocument doc = XDocument.Load(r, LoadOptions.SetBaseUri);
                        foreach (XNode node in doc.DescendantNodes())
                        {
                            using (XmlReader testReader = node.CreateReader())
                            {
                                TestLog.Compare(node.BaseUri, testReader.BaseURI, "Wrong baseURI in intial state");
                            }
                        }
                    }
                }

                private void VerifyLineInfo(IXmlLineInfo l1, IXmlLineInfo l2, string uri)
                {
                    TestLog.Equals(l1.HasLineInfo(), l2.HasLineInfo(), "HasLineInfo ::: " + uri);
                    TestLog.Equals(l1.LineNumber, l2.LineNumber, "LineNumber ::: " + uri);
                    TestLog.Equals(l1.LinePosition, l2.LinePosition, "LinePosition ::: " + uri);
                }

                private void VerifyNSData(Dictionary<string, IXmlLineInfo> d1, Dictionary<string, IXmlLineInfo> d2, string uri)
                {
                    if (d1 == null && d2 == null) return;
                    TestLog.Compare(!(d1 == null || d2 == null), "Test failed: Ns navigation problem");
                    Debug.Assert(d1.Count == d2.Count);
                    foreach (string name in d1.Keys)
                    {
                        VerifyLineInfo(d1[name], d2[name], uri);
                    }
                }
            }
        }
    }
}
