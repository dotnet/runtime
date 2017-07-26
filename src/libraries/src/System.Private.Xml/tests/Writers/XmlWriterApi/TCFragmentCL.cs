// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public class TCFragmentCL
    {
        //[Variation(id=1, Desc="Multiple root elements should be allowed", Pri=1)]
        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter)]
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

        //[Variation(id=2, Desc="Top level text should be allowed - PROLOG", Pri=1)]
        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter)]
        public void frag_2(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment);
            w.WriteString("Top level text");
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareReader("Top level text<Root />"));
        }

        //[Variation(id=3, Desc="Top level text should be allowed - EPILOG", Pri=1)]
        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter)]
        public void frag_3(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteString("Top level text");
            w.Dispose();
            Assert.True(utils.CompareReader("<Root />Top level text"));
        }

        //[Variation(id=4, Desc="Top level atomic value should be allowed - PROLOG", Pri=1)]
        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter)]
        public void frag_4(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment);
            int i = 1;
            w.WriteValue(i);
            w.WriteElementString("Root", "test");
            w.Dispose();
            Assert.True(utils.CompareReader("1<Root>test</Root>"));
        }

        //[Variation(id=5, Desc="Top level atomic value should be allowed - EPILOG", Pri=1)]
        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter)]
        public void frag_5(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter(ConformanceLevel.Fragment);
            w.WriteElementString("Root", "test");
            int i = 1;
            w.WriteValue(i);
            w.Dispose();
            Assert.True(utils.CompareReader("<Root>test</Root>1"));
        }

        //[Variation(id=6, Desc="Multiple top level atomic values", Pri=1)]
        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter)]
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

        //[Variation(id=7, Desc="WriteDocType should error when CL=fragment", Pri=1)]
        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter)]
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
            Assert.True(false);
        }

        //[Variation(id=8, Desc="WriteStartDocument() should error when CL=fragment", Pri=1)]
        [Theory]
        [XmlWriterInlineData(TestCaseUtilsImplementation.XmlFactoryWriter)]
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
            Assert.True(false);
        }
    }
}
