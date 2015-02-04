// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.IO;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {

        public partial class XNodeReaderTests : XLinqTestCase
        {
            public partial class TCRegressions1 : BridgeHelpers
            {
                //[Variation("Incorrect behavior of ReadAttributeValue ")]
                public void IncorrentBehaviorOfReadAttributeValue()
                {
                    string xml = @"<?xml version='1.0' encoding='utf-8' ?>
											<!DOCTYPE text [
												<!ATTLIST book id CDATA #REQUIRED>
												<!ENTITY a '123'>
											]>
											<text id1='a 123 b' id2='2'/>";
                    XmlReaderSettings rs = new XmlReaderSettings();
                    rs.DtdProcessing = DtdProcessing.Ignore;
                    XmlReader tr = XmlReader.Create(new StringReader(xml), rs);
                    XmlReader DataReader = GetReader(tr);
                    DataReader.ReadToFollowing("text");
                    TestLog.Compare(DataReader.MoveToNextAttribute(), true, "MTNA1");
                    VerifyNode(DataReader, XmlNodeType.Attribute, "id1", "a 123 b");
                    TestLog.Compare(DataReader.ReadAttributeValue(), true, "RAV1");
                    VerifyNode(DataReader, XmlNodeType.Text, "", "a ");
                    TestLog.Compare(DataReader.MoveToNextAttribute(), true, "MTNA2");
                    VerifyNode(DataReader, XmlNodeType.Attribute, "id2", "2");
                    TestLog.Compare(DataReader.ReadAttributeValue(), true, "RAV2");
                    VerifyNode(DataReader, XmlNodeType.Text, "", "2");
                    TestLog.Compare(DataReader.MoveToNextAttribute(), false, "MTNA3");
                    VerifyNode(DataReader, XmlNodeType.Text, "", "2");
                    TestLog.Compare(DataReader.ReadAttributeValue(), false, "RAV3");
                }
            }
        }
    }
}