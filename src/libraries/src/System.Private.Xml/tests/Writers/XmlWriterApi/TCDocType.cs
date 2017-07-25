// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    //[TestCase(Name = "WriteDocType")]
    public class TCDocType : XmlWriterTestCaseBase
    {
        //[Variation(id = 1, Desc = "Sanity test", Pri = 1)]
        [Fact]
        public void docType_1()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteDocType("ROOT", "publicid", "sysid", "<!ENTITY e 'abc'>");
                w.WriteStartElement("ROOT");
                w.WriteEndElement();
            }

            string exp = IsIndent() ?
                "<!DOCTYPE ROOT PUBLIC \"publicid\" \"sysid\"[<!ENTITY e 'abc'>]>" + Environment.NewLine + "<ROOT />" :
                "<!DOCTYPE ROOT PUBLIC \"publicid\" \"sysid\"[<!ENTITY e 'abc'>]><ROOT />";
            Assert.True(CompareString(exp));
        }

        //[Variation(id = 2, Desc = "WriteDocType pubid = null and sysid = null", Pri = 1)]
        [Fact]
        public void docType_2()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteDocType("test", null, null, "<!ENTITY e 'abc'>");
                w.WriteStartElement("Root");
                w.WriteEndElement();
            }
            string exp = IsIndent() ?
                "<!DOCTYPE test [<!ENTITY e 'abc'>]>" + Environment.NewLine + "<Root />" :
                "<!DOCTYPE test [<!ENTITY e 'abc'>]><Root />";
            Assert.True(CompareString(exp));
        }

        //[Variation(id = 3, Desc = "Call WriteDocType twice", Pri = 1)]
        [Fact]
        public void docType_3()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteDocType("doc1", null, null, "<!ENTITY e 'abc'>");
                    w.WriteDocType("doc2", null, null, "<!ENTITY f 'abc'>");
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        //[Variation(id = 4, Desc = "WriteDocType with name value = String.Empty", Param = "String.Empty", Pri = 1)]
        //[Variation(id = 5, Desc = "WriteDocType with name value = null", Param = "null", Pri = 1)]
        [Fact]
        public void docType_4()
        {
            String docName = "";
            if (CurVariation.Param.ToString() == "String.Empty")
                docName = String.Empty;
            else if (CurVariation.Param.ToString() == "null")
                docName = null;
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteDocType(docName, null, null, "test1");
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Start : WriteState.Error, "WriteState should be Error");
                    return;
                }
                catch (NullReferenceException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Start : WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        //[Variation(id = 6, Desc = "WriteDocType with DocType end tag in the value", Pri = 1)]
        [Fact]
        public void docType_5()
        {
            using (XmlWriter w = CreateWriter())
            {
                String docName = "Root";
                String docValue = "]>";
                w.WriteDocType(docName, null, null, docValue);
                w.WriteStartElement("Root");
                w.WriteEndElement();
            }
            string exp = IsIndent() ? "<!DOCTYPE Root []>]>" + Environment.NewLine + "<Root />" : "<!DOCTYPE Root []>]><Root />";
            Assert.True(CompareString(exp));
        }

        //[Variation(id = 7, Desc = "Call WriteDocType in the root element", Pri = 1)]
        [Fact]
        public void docType_6()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteDocType("doc1", null, null, "test1");
                    w.WriteEndElement();
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        //[Variation(id = 8, Desc = "Call WriteDocType following root element", Pri = 1)]
        [Fact]
        public void docType_7()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteDocType("doc1", null, null, "test1");
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }
    }
}
