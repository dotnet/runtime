// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    ////[TestCase(Name = "Auto-completion of tokens")]
    public class TCAutoComplete : XmlWriterTestCaseBase
    {
        //[Variation(id = 1, Desc = "Missing EndAttr, followed by element", Pri = 1)]
        [Fact]
        public void var_1()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr");
                w.WriteStartElement("child");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            Assert.True(CompareReader("<Root attr=''><child /></Root>"));
        }

        //[Variation(id = 2, Desc = "Missing EndAttr, followed by comment", Pri = 1)]
        [Fact]
        public void var_2()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr");
                w.WriteComment("This text is a comment");
                w.WriteEndElement();
            }
            Assert.True(CompareReader("<Root attr=''><!--This text is a comment--></Root>"));
        }

        //[Variation(id = 3, Desc = "Write EndDocument with unclosed element tag", Pri = 1)]
        [Fact]
        public void var_3()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartDocument();
                w.WriteStartElement("Root");
                w.WriteEndDocument();
            }
            Assert.True(CompareReader("<Root />"));
        }

        //[Variation(id = 4, Desc = "WriteStartDocument - WriteEndDocument", Pri = 1)]
        [Fact]
        public void var_4()
        {
            using (XmlWriter w = CreateWriter())
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

        //[Variation(id = 5, Desc = "WriteEndElement without WriteStartElement", Pri = 1)]
        [Fact]
        public void var_5()
        {
            using (XmlWriter w = CreateWriter())
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

        //[Variation(id = 6, Desc = "WriteFullEndElement without WriteStartElement", Pri = 1)]
        [Fact]
        public void var_6()
        {
            using (XmlWriter w = CreateWriter())
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
