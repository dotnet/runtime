// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    ////[TestCase(Name = "Auto-completion of tokens")]
    public class TCAutoComplete
    {
        // Missing EndAttr, followed by element
        [Theory]
        [XmlWriterInlineData]
        public void var_1(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr");
                w.WriteStartElement("child");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root attr=''><child /></Root>"));
        }

        // Missing EndAttr, followed by comment
        [Theory]
        [XmlWriterInlineData]
        public void var_2(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr");
                w.WriteComment("This text is a comment");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root attr=''><!--This text is a comment--></Root>"));
        }

        // Write EndDocument with unclosed element tag
        [Theory]
        [XmlWriterInlineData]
        public void var_3(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartDocument();
                w.WriteStartElement("Root");
                w.WriteEndDocument();
            }
            Assert.True(utils.CompareReader("<Root />"));
        }

        // WriteStartDocument - WriteEndDocument
        [Theory]
        [XmlWriterInlineData]
        public void var_4(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteEndDocument();
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());

                    Assert.True(false);
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

        // WriteEndElement without WriteStartElement
        [Theory]
        [XmlWriterInlineData]
        public void var_5(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteEndElement();
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

        // WriteFullEndElement without WriteStartElement
        [Theory]
        [XmlWriterInlineData]
        public void var_6(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteFullEndElement();
                    w.WriteFullEndElement();
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
