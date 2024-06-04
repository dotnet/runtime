// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.XmlWriterApiTests
{
    public class TCFragmentCL
    {
        // Multiple root elements should be allowed
        [Theory]
        [XmlWriterInlineData]
        public void frag_1(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareReader("<Root /><Root />"));
        }

        // Top level text should be allowed - PROLOG
        [Theory]
        [XmlWriterInlineData]
        public void frag_2(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment);
            w.WriteString("Top level text");
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareReader("Top level text<Root />"));
        }

        // Top level text should be allowed - EPILOG
        [Theory]
        [XmlWriterInlineData]
        public void frag_3(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteString("Top level text");
            w.Dispose();
            Assert.True(utils.CompareReader("<Root />Top level text"));
        }

        // Top level atomic value should be allowed - PROLOG
        [Theory]
        [XmlWriterInlineData]
        public void frag_4(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment);
            int i = 1;
            w.WriteValue(i);
            w.WriteElementString("Root", "test");
            w.Dispose();
            Assert.True(utils.CompareReader("1<Root>test</Root>"));
        }

        // Top level atomic value should be allowed - EPILOG
        [Theory]
        [XmlWriterInlineData]
        public void frag_5(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment);
            w.WriteElementString("Root", "test");
            int i = 1;
            w.WriteValue(i);
            w.Dispose();
            Assert.True(utils.CompareReader("<Root>test</Root>1"));
        }

        // Multiple top level atomic values
        [Theory]
        [XmlWriterInlineData]
        public void frag_6(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment);
            int i = 1;
            w.WriteValue(i); w.WriteValue(i); w.WriteValue(i); w.WriteValue(i);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteValue(i); w.WriteValue(i); w.WriteValue(i); w.WriteValue(i);
            w.Dispose();
            Assert.True(utils.CompareReader("1111<Root />1111"));
        }

        // WriteDocType should error when CL=fragment
        [Theory]
        [XmlWriterInlineData]
        public void frag_7(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment))
            {
                try
                {
                    w.WriteDocType("ROOT", "publicid", "sysid", "<!ENTITY e 'abc'>");
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.Fail();
        }

        // WriteStartDocument() should error when CL=fragment
        [Theory]
        [XmlWriterInlineData]
        public void frag_8(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment))
            {
                try
                {
                    w.WriteStartDocument();
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.Fail();
        }
    }
}
