// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            using (XmlReader tr = XmlReader.Create(new StringReader(xml), rs))
            {
                using (XmlReader reader = XDocument.Load(tr).CreateReader())
                {
                    reader.ReadToFollowing("text");

                    Assert.True(reader.MoveToNextAttribute());
                    Assert.Equal(XmlNodeType.Attribute, reader.NodeType);
                    Assert.Equal("id1", reader.Name);
                    Assert.Equal("a 123 b", reader.Value);

                    Assert.True(reader.ReadAttributeValue());
                    Assert.Equal(XmlNodeType.Text, reader.NodeType);
                    Assert.Equal("", reader.Name);
                    Assert.Equal("a 123 b", reader.Value);

                    Assert.True(reader.MoveToNextAttribute());
                    Assert.Equal(XmlNodeType.Attribute, reader.NodeType);
                    Assert.Equal("id2", reader.Name);
                    Assert.Equal("2", reader.Value);

                    Assert.True(reader.ReadAttributeValue());
                    Assert.Equal(XmlNodeType.Text, reader.NodeType);
                    Assert.Equal("", reader.Name);
                    Assert.Equal("2", reader.Value);

                    Assert.False(reader.MoveToNextAttribute());
                    Assert.Equal(XmlNodeType.Text, reader.NodeType);
                    Assert.Equal("", reader.Name);
                    Assert.Equal("2", reader.Value);
                    Assert.False(reader.ReadAttributeValue());
                }
            }
        }

         [Fact]
         public void EnsureReadToFollowingMovesToAttributeAndNotToDtd()
         {
            string xml = @"<?xml version='1.0' encoding='utf-8' ?>
									<!DOCTYPE text [
										<!ATTLIST book id CDATA #REQUIRED>
										<!ENTITY a '123'>
									]>
									<text id1='a 123 b' id2='2'/>";
             XmlReaderSettings rs = new XmlReaderSettings();
             rs.DtdProcessing = DtdProcessing.Ignore;
             using (XmlReader tr = XmlReader.Create(new StringReader(xml), rs))
             {
                 using (XmlReader reader = XDocument.Load(tr).CreateReader())
                 {
                     reader.ReadToFollowing("text");
                     Assert.True(reader.MoveToNextAttribute());
                     Assert.True(reader.ReadAttributeValue());
                     Assert.Equal(XmlNodeType.Text, reader.NodeType);
                     Assert.Equal("", reader.Name);
                     Assert.Equal("a 123 b", reader.Value);
                 }
             }
        }
    }
}
