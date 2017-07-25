// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Text;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - COREREADER", Param = "COREREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XMLTEXTREADER", Param = "XMLTEXTREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XMLDOCNODEREADER", Param = "XMLDOCNODEREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XMLDATADOCNODEREADER", Param = "XMLDATADOCNODEREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XPATHDOCNAVIGATORREADER", Param = "XPATHDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XMLDOCNAVIGATORREADER", Param = "XMLDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XMLDATADOCNAVIGATORREADER", Param = "XMLDATADOCNAVIGATORREADER")]
    public class TCWriteNode_With_ReadValueChunk : ReaderParamTestCase
    {
        /* Buffer size is 1024 chars in XmlWriter */
        private XmlReader CreateReader(int size)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<root>");
            for (int i = 0; i < size; i++)
            {
                sb.Append("A");
            }
            sb.Append("</root>");

            StringReader sr = new StringReader(sb.ToString());
            XmlReader r = base.CreateReader(sr);
            return r;
        }

        //[Variation(id = 1, Desc = "Input XML in utf8 encoding, text node has 1K-1 chars", Pri = 0, Param = "1023")]
        //[Variation(id = 2, Desc = "Input XML in utf8 encoding, text node has 1K chars", Pri = 0, Param = "1024")]
        //[Variation(id = 3, Desc = "Input XML in utf8 encoding, text node has 1K+1 chars", Pri = 0, Param = "1025")]
        //[Variation(id = 4, Desc = "Input XML in utf8 encoding, text node has 2K chars", Pri = 0, Param = "2048")]
        //[Variation(id = 5, Desc = "Input XML in utf8 encoding, text node has 4K chars", Pri = 0, Param = "4096")]
        [Fact]
        public void writeNode_1()
        {
            int size = Int32.Parse(CurVariation.Param.ToString());
            using (XmlReader r = this.CreateReader(size))
            {
                using (XmlWriter w = CreateWriter())
                {
                    while (r.Read())
                    {
                        w.WriteNode(r, false);
                    }
                }
            }

            switch (size)
            {
                case 1023:
                    Assert.True(CompareBaseline("textnode_1K-1_utf8.xml"));
                    break;
                case 1024:
                    Assert.True(CompareBaseline("textnode_1K_utf8.xml"));
                    break;
                case 1025:
                    Assert.True(CompareBaseline("textnode_1K+1_utf8.xml"));
                    break;
                case 2048:
                    Assert.True(CompareBaseline("textnode_2K_utf8.xml"));
                    break;
                case 4096:
                    Assert.True(CompareBaseline("textnode_4K_utf8.xml"));
                    break;
            }
            CError.WriteLine("Error");
            Assert.True(false);
        }

        //[Variation(id = 6, Desc = "Input XML in unicode encoding, text node has 1K-1 chars", Pri = 0, Param = "1023")]
        //[Variation(id = 7, Desc = "Input XML in unicode encoding, text node has 1K chars", Pri = 0, Param = "1024")]
        //[Variation(id = 8, Desc = "Input XML in unicode encoding, text node has 1K+1 chars", Pri = 0, Param = "1025")]
        //[Variation(id = 9, Desc = "Input XML in unicode encoding, text node has 2K chars", Pri = 0, Param = "2048")]
        //[Variation(id = 10, Desc = "Input XML in unicode encoding, text node has 4K chars", Pri = 0, Param = "4096")]
        [Fact]
        public void writeNode_2()
        {
            int size = Int32.Parse(CurVariation.Param.ToString());
            using (XmlReader r = this.CreateReader(size))
            {
                using (XmlWriter w = CreateWriter())
                {
                    while (r.Read())
                    {
                        w.WriteNode(r, false);
                    }
                }
            }

            switch (size)
            {
                case 1023:
                    Assert.True(CompareBaseline("textnode_1K-1_unicode.xml"));
                    break;
                case 1024:
                    Assert.True(CompareBaseline("textnode_1K_unicode.xml"));
                    break;
                case 1025:
                    Assert.True(CompareBaseline("textnode_1K+1_unicode.xml"));
                    break;
                case 2048:
                    Assert.True(CompareBaseline("textnode_2K_unicode.xml"));
                    break;
                case 4096:
                    Assert.True(CompareBaseline("textnode_4K_unicode.xml"));
                    break;
            }
            CError.WriteLine("Error");
            Assert.True(false);
        }


        //[Variation(id = 11, Desc = "Trailing surrogate pair", Pri = 1)]
        [Fact]
        public void writeNode_3()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<root>");
            for (int i = 0; i < 1022; i++)
            {
                sb.Append("A");
            }
            sb.Append("&#x10000;");
            sb.Append("</root>");

            StringReader sr = new StringReader(sb.ToString());
            using (XmlReader r = ReaderHelper.Create(sr))
            {
                using (XmlWriter w = CreateWriter())
                {
                    while (r.Read())
                    {
                        w.WriteNode(r, false);
                    }
                }
            }
            Assert.True(CompareBaseline("trailing_surrogate_1K.xml"));
        }

        //[Variation(id = 12, Desc = "Leading surrogate pair", Pri = 1)]
        [Fact]
        public void writeNode_4()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<root>");
            sb.Append("&#x10000;");
            for (int i = 0; i < 1022; i++)
            {
                sb.Append("A");
            }
            sb.Append("</root>");

            StringReader sr = new StringReader(sb.ToString());
            XmlReader r = ReaderHelper.Create(sr);
            using (XmlWriter w = CreateWriter())
            {
                while (r.Read())
                {
                    w.WriteNode(r, false);
                }
            }
            r.Dispose();

            Assert.True(CompareBaseline("leading_surrogate_1K.xml"));
        }

        //[Variation(id = 13, Desc = "Split surrogate pair across 1K buffer boundary", Pri = 1)]
        [Fact]
        public void writeNode_5()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<root>");

            for (int i = 0; i < 1023; i++)
            {
                sb.Append("A");
            }
            sb.Append("&#x10000;");
            sb.Append("</root>");

            StringReader sr = new StringReader(sb.ToString());
            XmlReader r = ReaderHelper.Create(sr);
            using (XmlWriter w = CreateWriter())
            {
                while (r.Read())
                {
                    w.WriteNode(r, false);
                }
            }
            r.Dispose();

            Assert.True(CompareBaseline("split_surrogate_1K.xml"));
        }
    }
}
