// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    //[TestCase(Name = "Invalid State Combinations", Pri = 1)]
    public class TCErrorState
    {
        // EntityRef after Document should error - PROLOG
        [Theory]
        [XmlWriterInlineData]
        public void state_1(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteEntityRef("ent");
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

        // EntityRef after Document should error - EPILOG
        [Theory]
        [XmlWriterInlineData]
        public void state_2(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteEntityRef("ent");
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

        // CharEntity after Document should error - PROLOG
        [Theory]
        [XmlWriterInlineData]
        public void state_3(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteCharEntity('\uD23E');
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

        // CharEntity after Document should error - EPILOG
        [Theory]
        [XmlWriterInlineData]
        public void state_4(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteCharEntity('\uD23E');
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

        // SurrogateCharEntity after Document should error - PROLOG
        [Theory]
        [XmlWriterInlineData]
        public void state_5(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteSurrogateCharEntity('\uDF41', '\uD920');
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

        // SurrogateCharEntity after Document should error - EPILOG
        [Theory]
        [XmlWriterInlineData]
        public void state_6(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteSurrogateCharEntity('\uDF41', '\uD920');
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

        // Attribute after Document should error - PROLOG
        [Theory]
        [XmlWriterInlineData]
        public void state_7(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartAttribute("attr", "");
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

        // Attribute after Document should error - EPILOG
        [Theory]
        [XmlWriterInlineData]
        public void state_8(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteStartAttribute("attr", "");
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

        // CDATA after Document should error - PROLOG
        [Theory]
        [XmlWriterInlineData]
        public void state_9(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteCData("Invalid");
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            return;
        }

        // CDATA after Document should error - EPILOG
        [Theory]
        [XmlWriterInlineData]
        public void state_10(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteCData("Invalid");
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

        // Element followed by Document should error
        [Theory]
        [XmlWriterInlineData]
        public void state_11(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
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

        // Element followed by DocType should error
        [Theory]
        [XmlWriterInlineData]
        public void state_12(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteDocType("Test", null, null, "");
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
