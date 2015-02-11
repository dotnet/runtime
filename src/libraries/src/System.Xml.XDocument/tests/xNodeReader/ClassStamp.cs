// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.IO;
using Microsoft.Test.ModuleCore;
using System.Xml.Linq;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public class FunctionalTests_XNodeReaderTests_TCRegressions1
    {
        [Fact]
        [ActiveIssue(641)]
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
            XmlReader DataReader = XDocument.Load(tr).CreateReader();
            DataReader.ReadToFollowing("text");
            Assert.True(DataReader.MoveToNextAttribute());
            Assert.True(BridgeHelpers.VerifyNode(DataReader, XmlNodeType.Attribute, "id1", "a 123 b"));
            Assert.True(DataReader.ReadAttributeValue());
            Assert.True(BridgeHelpers.VerifyNode(DataReader, XmlNodeType.Text, "", "a "));
            Assert.True(DataReader.MoveToNextAttribute());
            Assert.True(BridgeHelpers.VerifyNode(DataReader, XmlNodeType.Attribute, "id2", "2"));
            Assert.True(DataReader.ReadAttributeValue());
            Assert.True(BridgeHelpers.VerifyNode(DataReader, XmlNodeType.Text, "", "2"));
            Assert.False(DataReader.MoveToNextAttribute());
            Assert.True(BridgeHelpers.VerifyNode(DataReader, XmlNodeType.Text, "", "2"));
            Assert.False(DataReader.ReadAttributeValue());
        }
    }
}