// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    //[TestCase(Name = "WriteStart/EndDocument")]
    public class TCDocument
    {
        //[Variation(id = 1, Desc = "StartDocument-EndDocument Sanity Test", Pri = 0)]
        [Theory]
        [XmlWriterInlineData]
        public void document_1(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartDocument();
                w.WriteStartElement("Root");
                w.WriteEndElement();
                w.WriteEndDocument();
            }
            Assert.True(utils.CompareReader("<Root />"));
        }

        //[Variation(id = 2, Desc = "Multiple StartDocument should error", Pri = 1)]
        [Theory]
        [XmlWriterInlineData]
        public void document_2(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
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

        //[Variation(id = 3, Desc = "Missing StartDocument should be fixed", Pri = 1)]
        [Theory]
        [XmlWriterInlineData]
        public void document_3(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteEndElement();
                w.WriteEndDocument();
            }
            Assert.True(utils.CompareReader("<Root />"));
        }


        //[Variation(id = 4, Desc = "Multiple EndDocument should error", Pri = 1)]
        [Theory]
        [XmlWriterInlineData]
        public void document_4(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteEndDocument();
                    w.WriteEndDocument();
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

        //[Variation(id = 5, Desc = "Missing EndDocument should be fixed", Pri = 1)]
        [Theory]
        [XmlWriterInlineData]
        public void document_5(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartDocument();
                w.WriteStartElement("Root");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root />"));
        }

        //[Variation(id = 6, Desc = "Call Start-EndDocument multiple times, should error", Pri = 2)]
        [Theory]
        [XmlWriterInlineData]
        public void document_6(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteEndDocument();

                    w.WriteStartDocument();
                    w.WriteEndDocument();
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

        //[Variation(id = 7, Desc = "Multiple root elements should error", Pri = 1)]
        [Theory]
        [XmlWriterInlineData]
        public void document_7(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteStartElement("Root");
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

        //[Variation(id = 8, Desc = "Start-EndDocument without any element should error", Pri = 2)]
        [Theory]
        [XmlWriterInlineData]
        public void document_8(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteEndDocument();
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

        //[Variation(id = 9, Desc = "Top level text should error - PROLOG", Pri = 1)]
        [Theory]
        [XmlWriterInlineData]
        public void document_9(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteString("Top level text");
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

        //[Variation(id = 10, Desc = "Top level text should error - EPILOG", Pri = 1)]
        [Theory]
        [XmlWriterInlineData]
        public void document_10(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteString("Top level text");
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


        //[Variation(id = 11, Desc = "Top level atomic value should error - PROLOG", Pri = 1)]
        [Theory]
        [XmlWriterInlineData]
        public void document_11(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    int i = 1;
                    w.WriteValue(i);
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            Assert.True(false);
        }

        //[Variation(id = 12, Desc = "Top level atomic value should error - EPILOG", Pri = 1)]
        [Theory]
        [XmlWriterInlineData]
        public void document_12(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    int i = 1;
                    w.WriteValue(i);
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            Assert.True(false);
        }
    }
}
