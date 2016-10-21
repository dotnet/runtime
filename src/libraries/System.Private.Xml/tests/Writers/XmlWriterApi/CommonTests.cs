// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    //[TestCase(Name = "Invalid State Combinations", Pri = 1)]
    public partial class TCErrorState : XmlWriterTestCaseBase
    {
        //[Variation(id = 1, Desc = "EntityRef after Document should error - PROLOG", Pri = 1)]
        public int state_1()
        {
            using (XmlWriter w = CreateWriter())
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

                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 2, Desc = "EntityRef after Document should error - EPILOG", Pri = 1)]
        public int state_2()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 3, Desc = "CharEntity after Document should error - PROLOG", Pri = 1)]
        public int state_3()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 4, Desc = "CharEntity after Document should error - EPILOG", Pri = 1)]
        public int state_4()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 5, Desc = "SurrogateCharEntity after Document should error - PROLOG", Pri = 1)]
        public int state_5()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 6, Desc = "SurrogateCharEntity after Document should error - EPILOG", Pri = 1)]
        public int state_6()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 7, Desc = "Attribute after Document should error - PROLOG", Pri = 1)]
        public int state_7()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 8, Desc = "Attribute after Document should error - EPILOG", Pri = 1)]
        public int state_8()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 9, Desc = "CDATA after Document should error - PROLOG", Pri = 1)]
        public int state_9()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_PASS;
        }

        //[Variation(id = 10, Desc = "CDATA after Document should error - EPILOG", Pri = 1)]
        public int state_10()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 11, Desc = "Element followed by Document should error", Pri = 1)]
        public int state_11()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 12, Desc = "Element followed by DocType should error", Pri = 1)]
        public int state_12()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }


        //[Variation(Desc = "1.WriteNode with GB18030 encoding", Param = 1)]
        //[Variation(Desc = "2.WriteNode with GB18030 encoding", Param = 2)]
        public int writeNode_XmlReader36a()
        {
            string path = FilePathUtil.GetStandardPath();
            int param = (int)this.CurVariation.Param;
            string xml = (param == 1) ? path + @"\xml10\ms_xml\bug433100.xml" : path + @"\Globalization\Lang\GB18030\gb18030char.xml";
            CError.WriteLine(xml);

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            using (XmlReader xr = ReaderHelper.Create(xml))
            {
                using (XmlWriter w = CreateWriter(ws))
                {
                    while (!xr.EOF)
                    {
                        w.WriteNode(xr, true);
                    }
                }
            }
            return CompareBaseline2(xml) ? TEST_PASS : TEST_FAIL;
        }
    }

    ////[TestCase(Name = "Auto-completion of tokens")]
    public partial class TCAutoComplete : XmlWriterTestCaseBase
    {
        //[Variation(id = 1, Desc = "Missing EndAttr, followed by element", Pri = 1)]
        public int var_1()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr");
                w.WriteStartElement("child");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            return CompareReader("<Root attr=''><child /></Root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 2, Desc = "Missing EndAttr, followed by comment", Pri = 1)]
        public int var_2()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr");
                w.WriteComment("This text is a comment");
                w.WriteEndElement();
            }
            return CompareReader("<Root attr=''><!--This text is a comment--></Root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 3, Desc = "Write EndDocument with unclosed element tag", Pri = 1)]
        public int var_3()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartDocument();
                w.WriteStartElement("Root");
                w.WriteEndDocument();
            }
            return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 4, Desc = "WriteStartDocument - WriteEndDocument", Pri = 1)]
        public int var_4()
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

                    return TEST_FAIL;
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 5, Desc = "WriteEndElement without WriteStartElement", Pri = 1)]
        public int var_5()
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 6, Desc = "WriteFullEndElement without WriteStartElement", Pri = 1)]
        public int var_6()
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }
    }

    //[TestCase(Name = "WriteStart/EndDocument")]
    public partial class TCDocument : XmlWriterTestCaseBase
    {
        public override int Init(object o)
        {
            int i = base.Init(0);
            return i;
        }

        //[Variation(id = 1, Desc = "StartDocument-EndDocument Sanity Test", Pri = 0)]
        public int document_1()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartDocument();
                w.WriteStartElement("Root");
                w.WriteEndElement();
                w.WriteEndDocument();
            }
            return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 2, Desc = "Multiple StartDocument should error", Pri = 1)]
        public int document_2()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 3, Desc = "Missing StartDocument should be fixed", Pri = 1)]
        public int document_3()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteEndElement();
                w.WriteEndDocument();
            }
            return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
        }


        //[Variation(id = 4, Desc = "Multiple EndDocument should error", Pri = 1)]
        public int document_4()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 5, Desc = "Missing EndDocument should be fixed", Pri = 1)]
        public int document_5()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartDocument();
                w.WriteStartElement("Root");
                w.WriteEndElement();
            }
            return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 6, Desc = "Call Start-EndDocument multiple times, should error", Pri = 2)]
        public int document_6()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 7, Desc = "Multiple root elements should error", Pri = 1)]
        public int document_7()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 8, Desc = "Start-EndDocument without any element should error", Pri = 2)]
        public int document_8()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 9, Desc = "Top level text should error - PROLOG", Pri = 1)]
        public int document_9()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 10, Desc = "Top level text should error - EPILOG", Pri = 1)]
        public int document_10()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }


        //[Variation(id = 11, Desc = "Top level atomic value should error - PROLOG", Pri = 1)]
        public int document_11()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        //[Variation(id = 12, Desc = "Top level atomic value should error - EPILOG", Pri = 1)]
        public int document_12()
        {
            using (XmlWriter w = CreateWriter())
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
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }
    }

    //[TestCase(Name = "WriteDocType")]
    public partial class TCDocType : XmlWriterTestCaseBase
    {
        //[Variation(id = 1, Desc = "Sanity test", Pri = 1)]
        public int docType_1()
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
            return CompareString(exp) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 2, Desc = "WriteDocType pubid = null and sysid = null", Pri = 1)]
        public int docType_2()
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
            return CompareString(exp) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 3, Desc = "Call WriteDocType twice", Pri = 1)]
        public int docType_3()
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 4, Desc = "WriteDocType with name value = String.Empty", Param = "String.Empty", Pri = 1)]
        //[Variation(id = 5, Desc = "WriteDocType with name value = null", Param = "null", Pri = 1)]
        public int docType_4()
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
                    return TEST_PASS;
                }
                catch (NullReferenceException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Start : WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 6, Desc = "WriteDocType with DocType end tag in the value", Pri = 1)]
        public int docType_5()
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
            return CompareString(exp) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 7, Desc = "Call WriteDocType in the root element", Pri = 1)]
        public int docType_6()
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 8, Desc = "Call WriteDocType following root element", Pri = 1)]
        public int docType_7()
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
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }
    }

    //[TestCase(Name = "WriteStart/EndElement")]
    public partial class TCElement : XmlWriterTestCaseBase
    {
        //[Variation(id = 1, Desc = "StartElement-EndElement Sanity Test", Pri = 0)]
        public int element_1()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteEndElement();
            }
            return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 2, Desc = "Sanity test for overload WriteStartElement(string prefix, string name, string ns)", Pri = 0)]
        public int element_2()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("pre1", "Root", "http://my.com");
                w.WriteEndElement();
            }
            return CompareReader("<pre1:Root xmlns:pre1=\"http://my.com\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 3, Desc = "Sanity test for overload WriteStartElement(string name, string ns)", Pri = 0)]
        public int element_3()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root", "http://my.com");
                w.WriteEndElement();
            }
            return CompareReader("<Root xmlns=\"http://my.com\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 4, Desc = "Element name = String.Empty should error", Pri = 1)]
        public int element_4()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement(String.Empty);
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Start : WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 5, Desc = "Element name = null should error", Pri = 1)]
        public int element_5()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement(null);
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Start : WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 6, Desc = "Element NS = String.Empty", Pri = 1)]
        public int element_6()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root", String.Empty);
                w.WriteEndElement();
            }
            return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 7, Desc = "Element NS = null", Pri = 1)]
        public int element_7()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root", null);
                w.WriteEndElement();
            }
            return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "Write 100 nested elements")]
        public int element_8()
        {
            using (XmlWriter w = CreateWriter())
            {
                for (int i = 0; i < 100; i++)
                {
                    string eName = "Node" + i.ToString();
                    w.WriteStartElement(eName);
                }
                for (int i = 0; i < 100; i++)
                    w.WriteEndElement();
            }

            string exp = (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent) ?
                "100ElementsIndent.txt" : "100Elements.txt";
            return CompareBaseline(exp) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 9, Desc = "WriteDecl with start element with prefix and namespace")]
        public int element_9()
        {
            string enc = (WriterType == WriterType.UnicodeWriter || WriterType == WriterType.UnicodeWriterIndent) ? "16" : "8";
            string exp = (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent) ?
                String.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\"?>" + nl + "<a:b xmlns:a=\"c\" />", enc) :
                String.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\"?><a:b xmlns:a=\"c\" />", enc);

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = false;
            using (XmlWriter w = CreateWriter(ws))
            {
                w.WriteStartElement("a", "b", "c");
            }

            return (CompareString(exp)) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "Write many attributes with same names and diff.namespaces", Param = true)]
        //[Variation(Desc = "Write many attributes with same names and diff.namespaces", Param = false)]
        public int element_10()
        {
            string xml = "<a p1:a=\"\" p2:a=\"\" p3:a=\"\" p4:a=\"\" p5:a=\"\" p6:a=\"\" p7:a=\"\" p8:a=\"\" p9:a=\"\" p10:a=\"\" p11:a=\"\" p12:a=\"\" p13:a=\"\" p14:a=\"\" p15:a=\"\" p16:a=\"\" p17:a=\"\" p18:a=\"\" p19:a=\"\" p20:a=\"\" p21:a=\"\" p22:a=\"\" p23:a=\"\" p24:a=\"\" p25:a=\"\" p26:a=\"\" p27:a=\"\" p28:a=\"\" p29:a=\"\" p30:a=\"\" p31:a=\"\" p32:a=\"\" p33:a=\"\" p34:a=\"\" p35:a=\"\" p36:a=\"\" p37:a=\"\" p38:a=\"\" p39:a=\"\" p40:a=\"\" p41:a=\"\" p42:a=\"\" p43:a=\"\" p44:a=\"\" p45:a=\"\" p46:a=\"\" p47:a=\"\" p48:a=\"\" p49:a=\"\" p50:a=\"\" p51:a=\"\" p52:a=\"\" p53:a=\"\" p54:a=\"\" p55:a=\"\" p56:a=\"\" p57:a=\"\" p58:a=\"\" p59:a=\"\" p60:a=\"\" p61:a=\"\" p62:a=\"\" p63:a=\"\" p64:a=\"\" p65:a=\"\" p66:a=\"\" p67:a=\"\" p68:a=\"\" p69:a=\"\" p70:a=\"\" p71:a=\"\" p72:a=\"\" p73:a=\"\" p74:a=\"\" p75:a=\"\" p76:a=\"\" p77:a=\"\" p78:a=\"\" p79:a=\"\" p80:a=\"\" p81:a=\"\" p82:a=\"\" p83:a=\"\" p84:a=\"\" p85:a=\"\" p86:a=\"\" p87:a=\"\" p88:a=\"\" p89:a=\"\" p90:a=\"\" p91:a=\"\" p92:a=\"\" p93:a=\"\" p94:a=\"\" p95:a=\"\" p96:a=\"\" p97:a=\"\" p98:a=\"\" p99:a=\"\" p100:a=\"\" xmlns:p100=\"b99\" xmlns:p99=\"b98\" xmlns:p98=\"b97\" xmlns:p97=\"b96\" xmlns:p96=\"b95\" xmlns:p95=\"b94\" xmlns:p94=\"b93\" xmlns:p93=\"b92\" xmlns:p92=\"b91\" xmlns:p91=\"b90\" xmlns:p90=\"b89\" xmlns:p89=\"b88\" xmlns:p88=\"b87\" xmlns:p87=\"b86\" xmlns:p86=\"b85\" xmlns:p85=\"b84\" xmlns:p84=\"b83\" xmlns:p83=\"b82\" xmlns:p82=\"b81\" xmlns:p81=\"b80\" xmlns:p80=\"b79\" xmlns:p79=\"b78\" xmlns:p78=\"b77\" xmlns:p77=\"b76\" xmlns:p76=\"b75\" xmlns:p75=\"b74\" xmlns:p74=\"b73\" xmlns:p73=\"b72\" xmlns:p72=\"b71\" xmlns:p71=\"b70\" xmlns:p70=\"b69\" xmlns:p69=\"b68\" xmlns:p68=\"b67\" xmlns:p67=\"b66\" xmlns:p66=\"b65\" xmlns:p65=\"b64\" xmlns:p64=\"b63\" xmlns:p63=\"b62\" xmlns:p62=\"b61\" xmlns:p61=\"b60\" xmlns:p60=\"b59\" xmlns:p59=\"b58\" xmlns:p58=\"b57\" xmlns:p57=\"b56\" xmlns:p56=\"b55\" xmlns:p55=\"b54\" xmlns:p54=\"b53\" xmlns:p53=\"b52\" xmlns:p52=\"b51\" xmlns:p51=\"b50\" xmlns:p50=\"b49\" xmlns:p49=\"b48\" xmlns:p48=\"b47\" xmlns:p47=\"b46\" xmlns:p46=\"b45\" xmlns:p45=\"b44\" xmlns:p44=\"b43\" xmlns:p43=\"b42\" xmlns:p42=\"b41\" xmlns:p41=\"b40\" xmlns:p40=\"b39\" xmlns:p39=\"b38\" xmlns:p38=\"b37\" xmlns:p37=\"b36\" xmlns:p36=\"b35\" xmlns:p35=\"b34\" xmlns:p34=\"b33\" xmlns:p33=\"b32\" xmlns:p32=\"b31\" xmlns:p31=\"b30\" xmlns:p30=\"b29\" xmlns:p29=\"b28\" xmlns:p28=\"b27\" xmlns:p27=\"b26\" xmlns:p26=\"b25\" xmlns:p25=\"b24\" xmlns:p24=\"b23\" xmlns:p23=\"b22\" xmlns:p22=\"b21\" xmlns:p21=\"b20\" xmlns:p20=\"b19\" xmlns:p19=\"b18\" xmlns:p18=\"b17\" xmlns:p17=\"b16\" xmlns:p16=\"b15\" xmlns:p15=\"b14\" xmlns:p14=\"b13\" xmlns:p13=\"b12\" xmlns:p12=\"b11\" xmlns:p11=\"b10\" xmlns:p10=\"b9\" xmlns:p9=\"b8\" xmlns:p8=\"b7\" xmlns:p7=\"b6\" xmlns:p6=\"b5\" xmlns:p5=\"b4\" xmlns:p4=\"b3\" xmlns:p3=\"b2\" xmlns:p2=\"b1\" xmlns:p1=\"b0\" />";
            XmlReader r = ReaderHelper.Create(new StringReader(xml));
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(r, (bool)CurVariation.Param);
            }

            return (CompareString(xml)) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "Write many attributes and dup namespace")]
        public int element_10a()
        {
            XmlWriter w = CreateWriter();
            w.WriteDocType("a", null, null, "<!ATTLIST oot a CDATA #IMPLIED>");
            w.WriteStartElement("Root");
            for (int i = 0; i < 200; i++)
            {
                w.WriteAttributeString("a", "n" + i, "val");
            }
            try
            {
                w.WriteAttributeString("a", "n" + 199, "val");
            }
            catch (XmlException) { return TEST_PASS; }
            finally
            {
                w.Dispose();
            }
            return TEST_FAIL;
        }


        //[Variation(Desc = "Write many attributes and dup name")]
        public int element_10b()
        {
            XmlWriter w = CreateWriter();
            w.WriteDocType("a", null, null, "<!ATTLIST Root a CDATA #FIXED \"val\">");
            w.WriteStartElement("Root");
            for (int i = 0; i < 200; i++)
            {
                w.WriteAttributeString("a" + i, "val");
            }
            try
            {
                w.WriteAttributeString("a" + 199, "val");
            }
            catch (XmlException) { return TEST_PASS; }
            finally
            {
                w.Dispose();
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "Write many attributes and dup prefix")]
        public int element_10c()
        {
            XmlWriter w = CreateWriter();
            w.WriteDocType("a", null, null, "<!ATTLIST Root a (val|value) \"val\">");
            w.WriteStartElement("Root");
            for (int i = 0; i < 200; i++)
            {
                w.WriteAttributeString("p", "a", "n" + i, "val");
            }
            try
            {
                w.WriteAttributeString("p", "a", "n" + 199, "val");
            }
            catch (XmlException) { return TEST_PASS; }
            finally
            {
                w.Dispose();
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "Write invalid DOCTYPE with many attributes with prefix")]
        public int element_10d()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteDocType("a", "b", "c", "d");
                    w.WriteStartElement("Root");
                    for (int i = 0; i < 200; i++)
                    {
                        w.WriteAttributeString("p", "a", "n" + i, "val");
                    }
                    w.Dispose();
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    return TEST_FAIL;
                }
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "WriteEntityRef with XmlWellformedWriter for 'apos'", Param = 1)]
        //[Variation(Desc = "WriteEntityRef with XmlWellformedWriter for 'lt'", Param = 2)]
        //[Variation(Desc = "WriteEntityRef with XmlWellformedWriter for 'quot'", Param = 3)]
        public int element_11()
        {
            string exp = "";
            int param = (int)CurVariation.Param;
            bool isIndent = false;
            if (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent)
                isIndent = true;

            using (XmlWriter w = CreateWriter())
            {
                w.WriteDocType("root", null, null, "<!ENTITY e \"en-us\">");
                w.WriteStartElement("root");
                w.WriteStartAttribute("xml", "lang", null);
                switch (param)
                {
                    case 1:
                        w.WriteEntityRef("apos");
                        exp = !isIndent ? "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&apos;&lt;\" />" :
                            "<!DOCTYPE root [<!ENTITY e \"en-us\">]>" + nl + "<root xml:lang=\"&apos;&lt;\" />";
                        break;
                    case 2:
                        w.WriteEntityRef("lt");
                        exp = !isIndent ? "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&lt;&lt;\" />" :
                            "<!DOCTYPE root [<!ENTITY e \"en-us\">]>" + nl + "<root xml:lang=\"&lt;&lt;\" />";
                        break;
                    case 3:
                        w.WriteEntityRef("quot");
                        exp = !isIndent ? "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&quot;&lt;\" />" :
                            "<!DOCTYPE root [<!ENTITY e \"en-us\">]>" + nl + "<root xml:lang=\"&quot;&lt;\" />";
                        break;
                }
                w.WriteString("<");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }
            return (CompareString(exp)) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "WriteValue & WriteWhitespace on a special attribute value – xml:xmlns", Param = 1)]
        //[Variation(Desc = "WriteValue & WriteWhitespace on a special attribute value – xml:space", Param = 2)]
        //[Variation(Desc = "WriteValue & WriteWhitespace on a special attribute value – xml:lang", Param = 3)]
        //[Variation(Desc = "WriteValue & WriteWhitespace on a special attribute value – xmlns", Param = 4)]
        //[Variation(Desc = "WriteValue & WriteWhitespace on a special attribute value – space", Param = 5)]
        //[Variation(Desc = "WriteValue & WriteWhitespace on a special attribute value – lang", Param = 6)]
        public int element_12()
        {
            int param = (int)CurVariation.Param;
            XmlWriter w = CreateWriter();
            w.WriteStartElement("Root");
            string exp = "";
            switch (param)
            {
                case 1: exp = "<Root xml:xmlns=\"default    \" />"; break;
                case 2: exp = "<Root xml:space=\"default\" />"; break;
                case 3: exp = "<Root xml:lang=\"default    \" />"; break;
                case 4: exp = "<Root p1:xml=\"default    \" xmlns:p1=\"xmlns\" />"; break;
                case 5: exp = "<Root p1:xml=\"default    \" xmlns:p1=\"space\" />"; break;
                case 6: exp = "<Root p1:xml=\"default    \" xmlns:p1=\"lang\" />"; break;
            }

            switch (param)
            {
                case 1: w.WriteStartAttribute("xml", "xmlns", null); break;
                case 2: w.WriteStartAttribute("xml", "space", null); break;
                case 3: w.WriteStartAttribute("xml", "lang", null); break;
                case 4: w.WriteStartAttribute("xml", "xmlns"); break;
                case 5: w.WriteStartAttribute("xml", "space"); break;
                case 6: w.WriteStartAttribute("xml", "lang"); break;
            }
            w.WriteValue("default");
            try
            {
                w.WriteWhitespace("    ");
                w.WriteEndAttribute();
                w.WriteEndElement();
                w.Dispose();
            }
            catch (InvalidOperationException e)
            {
                CError.WriteLine(e);
                return TEST_FAIL;
            }
            return CompareString(exp) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "WriteValue element double value", Params = new object[] { false, "<Root>-0</Root>" })]
        //[Variation(Desc = "WriteValue attribute double value", Params = new object[] { true, "<Root b=\"-0\" />" })]
        public int element_13()
        {
            bool isAttr = (bool)CurVariation.Params[0];
            string exp = (string)CurVariation.Params[1];
            double a = 1;
            double b = 0;
            double c = 1;
            double d = -a * b / c;

            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                if (isAttr) w.WriteStartAttribute("b");
                w.WriteValue(d);
                w.WriteEndElement();
            }
            return (CompareString(exp)) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "WriteNode with euc-jp encoding.pr-xml-euc-jp.xml", Param = "pr-xml-euc-jp.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.pr-xml-iso-2022-jp.xml", Param = "pr-xml-iso-2022-jp.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.pr-xml-little-endian.xml", Param = "pr-xml-little-endian.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.pr-xml-shift_jis.xml", Param = "pr-xml-shift_jis.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.pr-xml-utf-8.xml", Param = "pr-xml-utf-8.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.pr-xml-utf-16.xml", Param = "pr-xml-utf-16.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.weekly-euc-jp.xml", Param = "weekly-euc-jp.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.weekly-iso-2022-jp.xml", Param = "weekly-iso-2022-jp.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.weekly-little-endian.xml", Param = "weekly-little-endian.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.weekly-shift_jis.xml", Param = "weekly-shift_jis.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.weekly-utf-8.xml", Param = "weekly-utf-8.xml")]
        //[Variation(Desc = "WriteNode with euc-jp encoding.weekly-utf-16.xml", Param = "weekly-utf-16.xml")]
        public int element_bug480250()
        {
            string path = FilePathUtil.GetStandardPath();
            string xml = (string)CurVariation.Param;
            string uri = path + @"\XML10\xmlconf\japanese\" + xml;
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.IgnoreWhitespace = true;
            using (XmlReader r = ReaderHelper.Create(uri, rs))
            {
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.Encoding = System.Text.Encoding.GetEncoding("euc-jp");
                using (XmlWriter w = WriterHelper.Create(@"out.xml", ws))
                {
                    w.WriteNode(r, true);
                }
            }
            return TEST_PASS;
        }
    }

    //[TestCase(Name = "WriteStart/EndAttribute")]
    public partial class TCAttribute : XmlWriterTestCaseBase
    {
        //[Variation(id = 1, Desc = "Sanity test for WriteAttribute", Pri = 0)]
        public int attribute_1()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr1");
                w.WriteEndAttribute();
                w.WriteAttributeString("attr2", "val2");
                w.WriteEndElement();
            }
            return CompareReader("<Root attr1=\"\" attr2=\"val2\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 2, Desc = "Missing EndAttribute should be fixed", Pri = 0)]
        public int attribute_2()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr1");
                w.WriteEndElement();
            }
            return CompareReader("<Root attr1=\"\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 3, Desc = "WriteStartAttribute followed by WriteStartAttribute", Pri = 0)]
        public int attribute_3()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr1");
                w.WriteStartAttribute("attr2");
                w.WriteEndElement();
            }
            return CompareReader("<Root attr1=\"\" attr2=\"\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 4, Desc = "Multiple WritetAttributeString", Pri = 0)]
        public int attribute_4()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("attr1", "val1");
                w.WriteAttributeString("attr2", "val2");
                w.WriteEndElement();
            }
            return CompareReader("<Root attr1=\"val1\" attr2=\"val2\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 5, Desc = "WriteStartAttribute followed by WriteString", Pri = 0)]
        public int attribute_5()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr1");
                w.WriteString("test");
                w.WriteEndElement();
            }
            return CompareReader("<Root attr1=\"test\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 6, Desc = "Sanity test for overload WriteStartAttribute(name, ns)", Pri = 1)]
        public int attribute_6()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr1", "http://my.com");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }
            return CompareString("<Root ~a p1 a~:attr1=\"\" xmlns:~a p1 A~=\"http://my.com\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 7, Desc = "Sanity test for overload WriteStartAttribute(prefix, name, ns)", Pri = 0)]
        public int attribute_7()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("pre1", "attr1", "http://my.com");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }
            return CompareReader("<Root pre1:attr1=\"\" xmlns:pre1=\"http://my.com\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 8, Desc = "DCR 64183: Duplicate attribute 'attr1'", Pri = 1)]
        public int attribute_8()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr1");
                    w.WriteStartAttribute("attr1");
                }
                catch (XmlException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 9, Desc = "Duplicate attribute 'ns1:attr1'", Pri = 1)]
        public int attribute_9()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("ns1", "attr1", "http://my.com");
                    w.WriteStartAttribute("ns1", "attr1", "http://my.com");
                }
                catch (XmlException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 10, Desc = "Attribute name = String.Empty should error", Pri = 1)]
        public int attribute_10()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute(String.Empty);
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 11, Desc = "Attribute name = null", Pri = 1)]
        public int attribute_11()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute(null);
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 12, Desc = "WriteAttribute with names Foo, fOo, foO, FOO", Pri = 1)]
        public int attribute_12()
        {
            using (XmlWriter w = CreateWriter())
            {
                string[] attrNames = { "Foo", "fOo", "foO", "FOO" };
                w.WriteStartElement("Root");
                for (int i = 0; i < attrNames.Length; i++)
                {
                    w.WriteAttributeString(attrNames[i], "x");
                }
                w.WriteEndElement();
            }
            return CompareReader("<Root Foo=\"x\" fOo=\"x\" foO=\"x\" FOO=\"x\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 13, Desc = "Invalid value of xml:space", Pri = 1)]
        public int attribute_13()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "invalid");
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 14, Desc = "SingleQuote in attribute value should be allowed")]
        public int attribute_14()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", null, "b'c");
                w.WriteEndElement();
            }
            return CompareReader("<Root a=\"b'c\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 15, Desc = "DoubleQuote in attribute value should be escaped")]
        public int attribute_15()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", null, "b\"c");
                w.WriteEndElement();
            }
            return CompareReader("<Root a=\"b&quot;c\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 16, Desc = "WriteAttribute with value = &, #65, #x20", Pri = 1)]
        public int attribute_16()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", "&");
                w.WriteAttributeString("b", "&#65;");
                w.WriteAttributeString("c", "&#x43;");
                w.WriteEndElement();
            }
            return CompareReader("<Root a=\"&amp;\" b=\"&amp;#65;\" c=\"&amp;#x43;\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 17, Desc = "WriteAttributeString followed by WriteString", Pri = 1)]
        public int attribute_17()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", null, "b");
                w.WriteString("test");
                w.WriteEndElement();
            }
            return CompareReader("<Root a=\"b\">test</Root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 18, Desc = "WriteAttribute followed by WriteString", Pri = 1)]
        public int attribute_18()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("a");
                w.WriteString("test");
                w.WriteEndElement();
            }
            return CompareReader("<Root a=\"test\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 19, Desc = "WriteAttribute with all whitespace characters", Pri = 1)]
        public int attribute_19()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", null, "\x20\x9\xD\xA");
                w.WriteEndElement();
            }

            return CompareReader("<Root a=\" &#x9;&#xD;&#xA;\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 20, Desc = "< > & chars should be escaped in attribute value", Pri = 1)]
        public int attribute_20()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", null, "< > &");
                w.WriteEndElement();
            }
            return CompareReader("<Root a=\"&lt; &gt; &amp;\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 21, Desc = "Redefine auto generated prefix n1")]
        public int attribute_21()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("test");
                w.WriteAttributeString("xmlns", "n1", null, "http://testbasens");
                w.WriteStartElement("base");
                w.WriteAttributeString("id", "http://testbasens", "5");
                w.WriteAttributeString("lang", "http://common", "en");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            string exp = IsIndent() ?
                "<test xmlns:n1=\"http://testbasens\">" + Environment.NewLine + "  <base n1:id=\"5\" p4:lang=\"en\" xmlns:p4=\"http://common\" />" + Environment.NewLine + "</test>" :
                "<test xmlns:~f n1 A~=\"http://testbasens\"><base ~f n1 a~:id=\"5\" ~a p4 a~:lang=\"en\" xmlns:~a p4 A~=\"http://common\" /></test>";
            return CompareString(exp) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 22, Desc = "Reuse and redefine existing prefix")]
        public int attribute_22()
        {
            string exp = "<test ~f p a~:a1=\"v\" xmlns:~f p A~=\"ns1\"><base ~f p b~:a2=\"v\" ~a p4 ab~:a3=\"v\" xmlns:~a p4 AB~=\"ns2\" /></test>";

            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("test");
                w.WriteAttributeString("p", "a1", "ns1", "v");
                w.WriteStartElement("base");
                w.WriteAttributeString("a2", "ns1", "v");
                w.WriteAttributeString("p", "a3", "ns2", "v");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            exp = IsIndent() ?
                "<test p:a1=\"v\" xmlns:p=\"ns1\">" + Environment.NewLine + "  <base p:a2=\"v\" p4:a3=\"v\" xmlns:p4=\"ns2\" />" + Environment.NewLine + "</test>" : exp;
            return CompareString(exp) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 23, Desc = "WriteStartAttribute(attr) sanity test")]
        public int attribute_23()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("test");
                w.WriteStartAttribute("attr");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }
            return CompareReader("<test attr=\"\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 24, Desc = "WriteStartAttribute(attr) inside an element with changed default namespace")]
        public int attribute_24()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement(string.Empty, "test", "ns");
                w.WriteStartAttribute("attr");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }
            return CompareReader("<test attr=\"\" xmlns=\"ns\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 25, Desc = "WriteStartAttribute(attr) and duplicate attrs")]
        public int attribute_25()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("test");
                    w.WriteStartAttribute(null, "attr", null);
                    w.WriteStartAttribute("attr");
                }
                catch (XmlException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw error for duplicate attrs");
            return TEST_FAIL;
        }

        //[Variation(id = 26, Desc = "WriteStartAttribute(attr) when element has ns:attr")]
        public int attribute_26()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("pre", "test", "ns");
                w.WriteStartAttribute(null, "attr", "ns");
                w.WriteStartAttribute("attr");
                w.WriteEndElement();
            }
            return CompareReader("<pre:test pre:attr=\"\" attr=\"\" xmlns:pre=\"ns\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 27, Desc = "XmlCharCheckingWriter should not normalize newLines in attribute values when NewLinesHandling = Replace")]
        public int attribute_27()
        {
            XmlWriterSettings s = new XmlWriterSettings();
            s.NewLineHandling = NewLineHandling.Replace;

            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteAttributeString("a", "|\x0D|\x0A|\x0D\x0A|");
                w.WriteEndElement();
            }
            return CompareReader("<root a=\"|&#xD;|&#xA;|&#xD;&#xA;|\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 28, Desc = "Wrapped XmlTextWriter: Invalid replacement of newline characters in text values")]
        public int attribute_28()
        {
            XmlWriterSettings s = new XmlWriterSettings();
            s.NewLineHandling = NewLineHandling.Replace;

            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteAttributeString("a", "|\x0D\x0A|");
                w.WriteElementString("foo", "|\x0D\x0A|");
                w.WriteEndElement();
            }
            return CompareReader("<root a=\"|&#xD;&#xA;|\"><foo>|\x0D\x0A|</foo></root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 29, Desc = "WriteAttributeString doesn't fail on invalid surrogate pair sequences")]
        public int attribute_29()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                try
                {
                    w.WriteAttributeString("attribute", "\ud800\ud800");
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }
    }

    //[TestCase(Name = "WriteAttributes(XmlTextReader)", Param = "XMLTEXTREADER")]
    //[TestCase(Name = "WriteAttributes(XmlDocument NodeReader)", Param = "XMLDOCNODEREADER")]
    //[TestCase(Name = "WriteAttributes(XmlDataDocument NodeReader)", Param = "XMLDATADOCNODEREADER")]
    //[TestCase(Name = "WriteAttributes(XsltReader)", Param = "XSLTREADER")]
    //[TestCase(Name = "WriteAttributes(CoreReader)", Param = "COREREADER")]
    //[TestCase(Name = "WriteAttributes(XPathdocument NavigatorReader)", Param = "XPATHDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteAttributes(XmlDocument NavigatorReader)", Param = "XMLDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteAttributes(XmlDataDocument NavigatorReader)", Param = "XMLDATADOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteAttributes(XmlBinaryReader)", Param = "XMLBINARYREADER")]
    public partial class TCWriteAttributes : ReaderParamTestCase
    {
        //[Variation(id = 1, Desc = "Call WriteAttributes with default DTD attributes = true", Pri = 1)]
        public int writeAttributes_1()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "name")
                        {
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, true);
                    w.WriteEndElement();
                }
            }
            return CompareReader("<Root a=\"b\" FIRST=\"KEVIN\" LAST=\"WHITE\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 2, Desc = "Call WriteAttributes with default DTD attributes = false", Pri = 1)]
        public int writeAttributes_2()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "name")
                        {
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }

            if (IsXPathDataModelReader())
                // allways sees default attributes
                return CompareReader("<Root a=\"b\" FIRST=\"KEVIN\" LAST=\"WHITE\" />") ? TEST_PASS : TEST_FAIL;
            else
                return CompareReader("<Root a=\"b\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 3, Desc = "Call WriteAttributes with XmlReader = null")]
        public int writeAttributes_3()
        {
            using (XmlWriter w = CreateWriter())
            {
                XmlReader xr = null;
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                }
                catch (ArgumentNullException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 4, Desc = "Call WriteAttributes when reader is located on element", Pri = 1)]
        public int writeAttributes_4()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesGeneric")
                        {
                            do { xr.Read(); } while (xr.LocalName != "node");
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Element)
                    {
                        CError.WriteLine("Reader not positioned element");
                        CError.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        return TEST_FAIL;
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            return CompareReader("<Root a=\"b\" c=\"d\" e=\"f\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 5, Desc = "Call WriteAttributes when reader is located in the middle attribute", Pri = 1)]
        public int writeAttributes_5()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesGeneric")
                        {
                            do { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToAttribute(1);
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        CError.WriteLine("Reader not positioned on attribute");
                        CError.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        return TEST_FAIL;
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            return CompareReader("<Root c=\"d\" e=\"f\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 6, Desc = "Call WriteAttributes when reader is located in the last attribute", Pri = 1)]
        public int writeAttributes_6()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesGeneric")
                        {
                            do { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToNextAttribute();
                            xr.MoveToNextAttribute();
                            xr.MoveToNextAttribute();
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        CError.WriteLine("Reader not positioned on attribute");
                        CError.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        return TEST_FAIL;
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            return CompareReader("<Root e=\"f\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 7, Desc = "Call WriteAttributes when reader is located on an attribute with an entity reference in the value", Pri = 1)]
        public int writeAttributes_7()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesEntity")
                        {
                            do { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToNextAttribute();
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        CError.WriteLine("Reader not positioned on attribute");
                        CError.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        return TEST_FAIL;
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            if (!ReaderExpandsEntityRef())
                return CompareString("<Root a=\"&e;\" />") ? TEST_PASS : TEST_FAIL;
            else
                return CompareReader("<Root a=\"Test Entity\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 8, Desc = "Call WriteAttributes with reader on XmlDeclaration", Pri = 1)]
        public int writeAttributes_8()
        {
            if (IsXPathDataModelReader())
            {
                CError.WriteLine("{0} does not support XmlDecl node", readerType);
                return TEST_SKIPPED;
            }
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("Simple.xml"))
                {
                    xr.Read();
                    if (xr.NodeType != XmlNodeType.XmlDeclaration)
                    {
                        CError.WriteLine("Reader not positioned on XmlDeclaration");
                        CError.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        return TEST_FAIL;
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            return CompareReader("<Root version=\"1.0\" standalone=\"yes\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 9, Desc = "Call WriteAttributes with reader on DocType", Pri = 1, Param = "DocumentType")]
        //[Variation(id = 10, Desc = "Call WriteAttributes with reader on CDATA", Pri = 1, Param = "CDATA")]
        //[Variation(id = 11, Desc = "Call WriteAttributes with reader on Text", Pri = 1, Param = "Text")]
        //[Variation(id = 12, Desc = "Call WriteAttributes with reader on PI", Pri = 1, Param = "ProcessingInstruction")]
        //[Variation(id = 13, Desc = "Call WriteAttributes with reader on Comment", Pri = 1, Param = "Comment")]
        //[Variation(id = 14, Desc = "Call WriteAttributes with reader on EntityRef", Pri = 1, Param = "EntityReference")]
        //[Variation(id = 15, Desc = "Call WriteAttributes with reader on Whitespace", Pri = 1, Param = "Whitespace")]
        //[Variation(id = 16, Desc = "Call WriteAttributes with reader on SignificantWhitespace", Pri = 1, Param = "SignificantWhitespace")]
        public int writeAttributes_9()
        {
            string strxml = "";
            switch (CurVariation.Param.ToString())
            {
                case "DocumentType":
                    if (IsXPathDataModelReader())
                    {
                        CError.WriteLine("{0} does not support DocumentType node", readerType);
                        return TEST_SKIPPED;
                    }
                    strxml = "<!DOCTYPE Root[]><Root/>";
                    break;
                case "CDATA":
                    if (IsXPathDataModelReader())
                    {
                        CError.WriteLine("{0} does not support CDATA node", readerType);
                        return TEST_SKIPPED;
                    }
                    strxml = "<root><![CDATA[Test]]></root>";
                    break;
                case "Text":
                    strxml = "<root>Test</root>";
                    break;
                case "ProcessingInstruction":
                    strxml = "<root><?pi test?></root>";
                    break;
                case "Comment":
                    strxml = "<root><!-- comment --></root>";
                    break;
                case "EntityReference":
                    if (!ReaderSupportsEntityRef())
                    {
                        CError.WriteLine("{0} does not support EntityRef node", readerType);
                        return TEST_SKIPPED;
                    }
                    strxml = "<!DOCTYPE root[<!ENTITY e \"Test Entity\"> ]><root>&e;</root>";
                    break;
                case "SignificantWhitespace":
                    strxml = "<root xml:space=\"preserve\">			 </root>";
                    break;
                case "Whitespace":
                    if (ReaderStripsWhitespace())
                    {
                        CError.WriteLine("{0} strips whitespace nodes by default", readerType);
                        return TEST_SKIPPED;
                    }
                    strxml = "<root>			 </root>";
                    break;
            }

            XmlReader xr;
            xr = CreateReader(new StringReader(strxml));

            do
            { xr.Read(); }
            while ((xr.NodeType.ToString() != CurVariation.Param.ToString()) && (xr.ReadState != ReadState.EndOfFile));

            if (xr.ReadState == ReadState.EndOfFile || xr.NodeType.ToString() != CurVariation.Param.ToString())
            {
                xr.Dispose();
                CError.WriteLine("Reader not positioned on correct node");
                CError.WriteLine("ReadState: {0}", xr.ReadState);
                CError.WriteLine("NodeType: {0}", xr.NodeType);
                return TEST_FAIL;
            }

            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    if (CurVariation.Param.ToString() != "DocumentType")
                        w.WriteStartElement("root");
                    w.WriteAttributes(xr, false);
                }
                catch (XmlException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    CError.Compare(w.WriteState, (CurVariation.Param.ToString() == "DocumentType") ? WriteState.Start : WriteState.Element, "WriteState should be Element");
                    return TEST_PASS;
                }
                finally
                {
                    xr.Dispose();
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 17, Desc = "Call WriteAttribute with double quote char in the value", Pri = 1)]
        public int writeAttributes_10()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "QuoteChar")
                        {
                            do { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            return CompareReader("<Root a=\"b&quot;c\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 18, Desc = "Call WriteAttribute with single quote char in the value", Pri = 1)]
        public int writeAttributes_11()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "QuoteChar")
                        {
                            do { xr.Read(); } while (xr.LocalName != "node");
                            do { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            return CompareReader("<Root a=\"b'c\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 19, Desc = "Call WriteAttributes with 100 attributes", Pri = 1)]
        public int writeAttributes_12()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReader("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "OneHundredAttributes")
                        {
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }

                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        CError.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                        xr.Dispose();
                        return TEST_FAIL;
                    }
                    w.WriteStartElement("OneHundredAttributes");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            return CompareBaseline("OneHundredAttributes.xml") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 20, Desc = "WriteAttributes with different builtin entities in attribute value", Pri = 1)]
        public int writeAttributes_13()
        {
            string strxml = "<E a=\"&gt;&lt;&quot;&apos;&amp;\" />";
            using (XmlReader xr = CreateReader(new StringReader(strxml)))
            {
                xr.Read();
                xr.MoveToFirstAttribute();

                if (xr.NodeType != XmlNodeType.Attribute)
                {
                    CError.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                    xr.Dispose();
                    return TEST_FAIL;
                }

                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
            }
            return CompareReader("<Root a=\"&gt;&lt;&quot;&apos;&amp;\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 21, Desc = "WriteAttributes tries to duplicate attribute", Pri = 1)]
        public int writeAttributes_14()
        {
            string strxml = "<root attr='test' />";
            XmlReader xr = CreateReader(new StringReader(strxml));
            xr.Read();
            xr.MoveToFirstAttribute();

            if (xr.NodeType != XmlNodeType.Attribute)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                xr.Dispose();
                return TEST_FAIL;
            }

            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                }
                catch (Exception e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
                finally
                {
                    xr.Dispose();
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }
    }

    //[TestCase(Name = "WriteNode(XmlTextReader)", Param = "XMLTEXTREADER")]
    //[TestCase(Name = "WriteNode(XmlDocument NodeReader)", Param = "XMLDOCNODEREADER")]
    //[TestCase(Name = "WriteNode(XmlDataDocument NodeReader)", Param = "XMLDATADOCNODEREADER")]
    //[TestCase(Name = "WriteNode(XsltReader)", Param = "XSLTREADER")]
    //[TestCase(Name = "WriteNode(CoreReader)", Param = "COREREADER")]
    //[TestCase(Name = "WriteNode(XPathdocument NavigatorReader)", Param = "XPATHDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteNode(XmlDocument NavigatorReader)", Param = "XMLDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteNode(XmlDataDocument NavigatorReader)", Param = "XMLDATADOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteNode(XmlBinaryReader)", Param = "XMLBINARYREADER")]
    public partial class TCWriteNode_XmlReader : ReaderParamTestCase
    {
        //[Variation(id = 1, Desc = "WriteNode with null reader", Pri = 1)]
        public int writeNode_XmlReader1()
        {
            XmlReader xr = null;
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                }
                catch (ArgumentNullException)
                {
                    CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 2, Desc = "WriteNode with reader positioned on attribute, no operation", Pri = 1)]
        public int writeNode_XmlReader2()
        {
            using (XmlWriter w = CreateWriter())
            {
                using (XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml"))
                {
                    while (xr.Read())
                    {
                        if (xr.LocalName == "defattr")
                        {
                            xr.Read();
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }

                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        CError.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                        xr.Dispose();
                        w.Dispose();
                        return TEST_FAIL;
                    }
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                }
            }
            return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 3, Desc = "WriteNode before reader.Read()", Pri = 1)]
        public int writeNode_XmlReader3()
        {
            using (XmlReader xr = CreateReader(new StringReader("<root />")))
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteNode(xr, false);
                }
            }

            return CompareReader("<root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 4, Desc = "WriteNode after first reader.Read()", Pri = 1)]
        public int writeNode_XmlReader4()
        {
            using (XmlReader xr = CreateReader(new StringReader("<root />")))
            {
                xr.Read();
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteNode(xr, false);
                }
            }

            return CompareReader("<root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 5, Desc = "WriteNode when reader is positioned on middle of an element node", Pri = 1)]
        public int writeNode_XmlReader5()
        {
            using (XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml"))
            {
                while (xr.Read())
                {
                    if (xr.LocalName == "Middle")
                    {
                        xr.Read();
                        break;
                    }
                }
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteNode(xr, false);
                }
                CError.Compare(xr.NodeType, XmlNodeType.Comment, "Error");
                CError.Compare(xr.Value, "WriteComment", "Error");
            }
            return CompareReader("<node2>Node Text<node3></node3><?name Instruction?></node2>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 6, Desc = "WriteNode when reader state is EOF", Pri = 1)]
        public int writeNode_XmlReader6()
        {
            using (XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml"))
            {
                while (xr.Read()) { }

                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                }
            }
            return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 7, Desc = "WriteNode when reader state is Closed", Pri = 1)]
        public int writeNode_XmlReader7()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read()) { }
            xr.Dispose();

            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 8, Desc = "WriteNode with reader on empty element node", Pri = 1)]
        public int writeNode_XmlReader8()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "EmptyElement")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            // check reader position
            CError.Compare(xr.NodeType, XmlNodeType.EndElement, "Error");
            CError.Compare(xr.Name, "EmptyElement", "Error");
            xr.Dispose();

            return CompareReader("<node1 />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 9, Desc = "WriteNode with reader on 100 Nodes", Pri = 1)]
        public int writeNode_XmlReader9()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "OneHundredElements")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            return CompareBaseline("100Nodes.txt") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 10, Desc = "WriteNode with reader on node with mixed content", Pri = 1)]
        public int writeNode_XmlReader10()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "MixedContent")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            // check reader position
            CError.Compare(xr.NodeType, XmlNodeType.EndElement, "Error");
            CError.Compare(xr.Name, "MixedContent", "Error");
            xr.Dispose();

            if (IsXPathDataModelReader())
            {
                return CompareReader("<node1><?PI Instruction?><!--Comment-->Textcdata</node1>") ? TEST_PASS : TEST_FAIL;
            }

            return CompareReader("<node1><?PI Instruction?><!--Comment-->Text<![CDATA[cdata]]></node1>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 11, Desc = "WriteNode with reader on node with declared namespace in parent", Pri = 1)]
        public int writeNode_XmlReader11()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "NamespaceNoPrefix")
                {
                    xr.Read();
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            return CompareReader("<node1 xmlns=\"foo\"></node1>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 12, Desc = "WriteNode with reader on node with entity reference included in element", Pri = 1)]
        public int writeNode_XmlReader12()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "EntityRef")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            if (!ReaderExpandsEntityRef())
                return CompareString("<node>&e;</node>") ? TEST_PASS : TEST_FAIL;
            else
                return CompareReader("<node>Test Entity</node>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 14, Desc = "WriteNode with element that has different prefix", Pri = 1)]
        public int writeNode_XmlReader14()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "DiffPrefix")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("x", "bar", "foo");
                w.WriteNode(xr, true);
                w.WriteStartElement("blah", "foo");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            xr.Dispose();

            return CompareReader("<x:bar xmlns:x=\"foo\"><z:node xmlns:z=\"foo\" /><x:blah /></x:bar>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 15, Desc = "Call WriteNode with default attributes = true and DTD", Pri = 1)]
        public int writeNode_XmlReader15()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "DefaultAttributesTrue")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xr, true);
                w.WriteEndElement();
            }
            xr.Dispose();
            if (!ReaderParsesDTD())
                return CompareReader("<Root><name a='b' /></Root>") ? TEST_PASS : TEST_FAIL;
            else
                return CompareReader("<Root><name a='b' FIRST='KEVIN' LAST='WHITE'/></Root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 16, Desc = "Call WriteNode with default attributes = false and DTD", Pri = 1)]
        public int writeNode_XmlReader16()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "DefaultAttributesTrue")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            if (ReaderLoosesDefaultAttrInfo())
                return CompareReader("<Root><name a='b' FIRST='KEVIN' LAST='WHITE'/></Root>") ? TEST_PASS : TEST_FAIL;
            else
                return CompareReader("<Root><name a='b' /></Root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 17, Desc = "WriteNode with reader on empty element with attributes", Pri = 1)]
        public int writeNode_XmlReader17()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "EmptyElementWithAttributes")
                {
                    xr.Read();
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            return CompareReader("<node1 a='foo' />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 18, Desc = "WriteNode with document containing just empty element with attributes", Pri = 1)]
        public int writeNode_XmlReader18()
        {
            string xml = "<Root a=\"foo\"/>";
            XmlReader xr = CreateReader(new StringReader(xml));
            xr.Read();
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            xr.Dispose();
            return CompareReader("<Root a=\"foo\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 19, Desc = "Call WriteNode with special entity references as attribute value", Pri = 1)]
        public int writeNode_XmlReader19()
        {
            using (XmlWriter w = CreateWriter())
            {
                string xml = "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>";
                using (XmlReader xr = CreateReader(new StringReader(xml)))
                {
                    while (xr.Read())
                        w.WriteNode(xr, true);
                }
            }
            return CompareReader("<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 20, Desc = "Call WriteNode with reader on doctype", Pri = 1)]
        public int writeNode_XmlReader20()
        {
            string strxml = "<!DOCTYPE ROOT []><ROOT/>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            using (XmlWriter w = CreateWriter())
            {
                while (xr.NodeType != XmlNodeType.DocumentType)
                    xr.Read();

                w.WriteNode(xr, false);
                w.WriteStartElement("ROOT");
                w.WriteEndElement();
            }
            xr.Dispose();

            return CompareReader("<!DOCTYPE ROOT[]><ROOT />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 21, Desc = "Call WriteNode with full end element", Pri = 1)]
        public int writeNode_XmlReader21()
        {
            string strxml = "<root></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }

            xr.Dispose();
            return CompareReader("<root></root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "Call WriteNode with tag mismatch")]
        public int writeNode_XmlReader21a()
        {
            string strxml = "<a xmlns=\"p1\"><b xmlns=\"p2\"><c xmlns=\"p1\" /></b><d xmlns=\"\"><e xmlns=\"p1\"><f xmlns=\"\" /></d></a>";
            try
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        using (XmlReader xr = CreateReader(new StringReader(strxml)))
                        {
                            w.WriteNode(xr, true);
                            CError.Compare(false, "Failed");
                        }
                    }
                    catch (XmlException xe) { CError.WriteLine(xe.Message); return TEST_PASS; }
                }
            }
            catch (ObjectDisposedException e) { CError.WriteLine(e.Message); return TEST_PASS; }
            return TEST_FAIL;
        }

        //[Variation(Desc = "Call WriteNode with default NS from DTD.UnexpToken")]
        public int writeNode_XmlReader21b()
        {
            string strxml = "<!DOCTYPE doc " +
"[<!ELEMENT doc ANY>" +
"<!ELEMENT test1 (#PCDATA)>" +
"<!ELEMENT test2 ANY>" +
"<!ELEMENT test3 (#PCDATA)>" +
"<!ENTITY e1 \"&e2;\">" +
"<!ENTITY e2 \"xmlns=\"x\"\">" +
"<!ATTLIST test3 a1 CDATA #IMPLIED>" +
"<!ATTLIST test3 a2 CDATA #IMPLIED>" +
"]>" +
"<doc>" +
"    &e2;" +
"    <test1>AA&e2;AA</test1>" +
"    <test2>BB&e1;BB</test2>" +
"    <test3 a1=\"&e2;\" a2=\"&e1;\">World</test3>" +
"</doc>";
            try
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        using (XmlReader xr = CreateReader(new StringReader(strxml)))
                        {
                            w.WriteNode(xr, true);
                            CError.Compare(false, "Failed");
                        }
                    }
                    catch (XmlException xe) { CError.WriteLine(xe.Message); return TEST_PASS; }
                }
            }
            catch (ObjectDisposedException e) { CError.WriteLine(e.Message); return TEST_PASS; }
            return TEST_FAIL;
        }

        //[Variation(id = 22, Desc = "Call WriteNode with reader on element with 100 attributes", Pri = 1)]
        public int writeNode_XmlReader22()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "OneHundredAttributes")
                {
                    break;
                }
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
            }
            xr.Dispose();
            return CompareBaseline("OneHundredAttributes.xml") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 23, Desc = "Call WriteNode with reader on text node", Pri = 1)]
        public int writeNode_XmlReader23()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "Middle")
                {
                    xr.Read();
                    xr.Read();
                    break;
                }
            }
            if (xr.NodeType != XmlNodeType.Text)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType);
                xr.Dispose();
                return TEST_FAIL;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            return CompareReader("<root>Node Text</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 24, Desc = "Call WriteNode with reader on CDATA node", Pri = 1)]
        public int writeNode_XmlReader24()
        {
            if (IsXPathDataModelReader())
            {
                CError.WriteLine("XPath data model does not have CDATA node type, so {0} can not be positioned on CDATA", readerType);
                return TEST_SKIPPED;
            }

            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "CDataNode")
                {
                    xr.Read();
                    break;
                }
            }
            if (xr.NodeType != XmlNodeType.CDATA)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType);
                xr.Dispose();
                return TEST_FAIL;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();

            return CompareReader("<root><![CDATA[cdata content]]></root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 25, Desc = "Call WriteNode with reader on PI node", Pri = 1)]
        public int writeNode_XmlReader25()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "PINode")
                {
                    xr.Read();
                    break;
                }
            }
            if (xr.NodeType != XmlNodeType.ProcessingInstruction)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType);
                xr.Dispose();
                return TEST_FAIL;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            return CompareReader("<root><?PI Text?></root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 26, Desc = "Call WriteNode with reader on Comment node", Pri = 1)]
        public int writeNode_XmlReader26()
        {
            XmlReader xr = CreateReaderIgnoreWS("XmlReader.xml");
            while (xr.Read())
            {
                if (xr.LocalName == "CommentNode")
                {
                    xr.Read();
                    break;
                }
            }
            if (xr.NodeType != XmlNodeType.Comment)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType);
                xr.Dispose();
                return TEST_FAIL;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteNode(xr, false);
                w.WriteEndElement();
            }
            xr.Dispose();
            return CompareReader("<root><!--Comment--></root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "Call WriteNode with reader on XmlDecl (OmitXmlDecl false)", Pri = 1)]
        public int writeNode_XmlReader28()
        {
            string strxml = "<?xml version=\"1.0\" standalone=\"yes\"?><Root />";
            XmlReader xr = CreateReader(new StringReader(strxml));

            xr.Read();
            if (xr.NodeType != XmlNodeType.XmlDeclaration)
            {
                CError.WriteLine("Reader positioned on {0}", xr.NodeType);
                xr.Dispose();
                return TEST_SKIPPED;
            }

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = false;
            XmlWriter w = CreateWriter(ws);
            w.WriteNode(xr, false);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            xr.Dispose();
            w.Dispose();
            strxml = IsIndent() ? "<?xml version=\"1.0\" standalone=\"yes\"?>" + Environment.NewLine + "<Root />" : strxml;
            return CompareString(strxml) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 27, Desc = "WriteNode should only write required namespaces", Pri = 1)]
        public int writeNode_XmlReader27()
        {
            string strxml = @"<root xmlns:p1='p1'><p2:child xmlns:p2='p2' /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            return CompareReader("<p2:child xmlns:p2='p2' />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 28, Desc = "Reader.WriteNode should only write required namespaces, include xmlns:xml", Pri = 1)]
        public int writeNode_XmlReader28b()
        {
            string strxml = @"<root xmlns:p1='p1'><p2:child xmlns:p2='p2' xmlns:xml='http://www.w3.org/XML/1998/namespace' /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            string exp = (WriterType == WriterType.UnicodeWriter) ? "<p2:child xmlns:p2=\"p2\" />" : "<p2:child xmlns:p2=\"p2\" xmlns:xml='http://www.w3.org/XML/1998/namespace' />";
            return CompareReader(exp) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 29, Desc = "WriteNode should only write required namespaces, exclude xmlns:xml", Pri = 1)]
        public int writeNode_XmlReader29()
        {
            string strxml = @"<root xmlns:p1='p1' xmlns:xml='http://www.w3.org/XML/1998/namespace'><p2:child xmlns:p2='p2' /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);

                xr.Dispose();
            }
            return CompareReader("<p2:child xmlns:p2=\"p2\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 30, Desc = "WriteNode should only write required namespaces, change default ns at top level", Pri = 1)]
        public int writeNode_XmlReader30()
        {
            string strxml = @"<root xmlns='p1'><child /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            return CompareReader("<child xmlns='p1' />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 31, Desc = "WriteNode should only write required namespaces, change default ns at same level", Pri = 1)]
        public int writeNode_XmlReader31()
        {
            string strxml = @"<root xmlns:p1='p1'><child xmlns='p2'/></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            return CompareReader("<child xmlns='p2' />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 32, Desc = "WriteNode should only write required namespaces, change default ns at both levels", Pri = 1)]
        public int writeNode_XmlReader32()
        {
            string strxml = @"<root xmlns='p1'><child xmlns='p2'/></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);

                xr.Dispose();
            }
            return CompareReader("<child xmlns='p2' />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 33, Desc = "WriteNode should only write required namespaces, change ns uri for same prefix", Pri = 1)]
        public int writeNode_XmlReader33()
        {
            string strxml = @"<p1:root xmlns:p1='p1'><p1:child xmlns:p1='p2'/></p1:root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            return CompareReader("<p1:child xmlns:p1='p2' />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 34, Desc = "WriteNode should only write required namespaces, reuse prefix from top level", Pri = 1)]
        public int writeNode_XmlReader34()
        {
            string strxml = @"<root xmlns:p1='p1'><p1:child /></root>";
            XmlReader xr = CreateReader(new StringReader(strxml));
            while (xr.Read())
            {
                if (xr.LocalName == "child")
                    break;
            }
            using (XmlWriter w = CreateWriter())
            {
                w.WriteNode(xr, false);
                xr.Dispose();
            }
            return CompareReader("<p1:child xmlns:p1='p1' />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "1. XDocument does not format content while Saving", Param = @"<?xml version='1.0'?><?pi?><?pi?>  <shouldbeindented><a>text</a></shouldbeindented><?pi?>")]
        //[Variation(Desc = "2. XDocument does not format content while Saving", Param = @"<?xml version='1.0'?><?pi?><?pi?>  <shouldbeindented><a>text</a></shouldbeindented><?pi?>")]
        public int writeNode_XmlReader35()
        {
            string strxml = (string)CurVariation.Param;
            CError.WriteLine(strxml);
            XmlReader xr = CreateReader(new StringReader(strxml));
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.ConformanceLevel = (CurVariation.Desc.Contains("1.")) ? ConformanceLevel.Document : ConformanceLevel.Auto;
            ws.Indent = true;
            XmlWriter w = CreateWriter(ws);
            w.WriteNode(xr, false);
            xr.Dispose();
            w.Dispose();
            return CompareReader(strxml) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "1.WriteNode with ascii encoding", Param = true)]
        //[Variation(Desc = "2.WriteNode with ascii encoding", Param = false)]
        public int writeNode_XmlReader36()
        {
            string strxml = "<Ro\u00F6t \u00F6=\"\u00F6\" />";
            string exp = strxml;

            XmlReader xr = CreateReader(new StringReader(strxml));

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            XmlWriter w = CreateWriter(ws);
            while (!xr.EOF)
            {
                w.WriteNode(xr, (bool)CurVariation.Param);
            }
            xr.Dispose();
            w.Dispose();
            return (CompareString(exp)) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "WriteNode DTD PUBLIC with identifier", Param = true)]
        //[Variation(Desc = "WriteNode DTD PUBLIC with identifier", Param = false)]
        public int writeNode_XmlReader37()
        {
            string strxml = "<!DOCTYPE root PUBLIC \"\" \"#\"><root/>";
            string exp = (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent) ?
               "<!DOCTYPE root PUBLIC \"\" \"#\"[]>" + nl + "<root />" :
               "<!DOCTYPE root PUBLIC \"\" \"#\"[]><root />";
            try
            {
                XmlReader xr = CreateReader(new StringReader(strxml));
                using (XmlWriter w = CreateWriter())
                {
                    while (!xr.EOF)
                    {
                        w.WriteNode(xr, (bool)CurVariation.Param);
                    }
                    xr.Dispose();
                }
            }
            catch (FileNotFoundException e) { CError.WriteLine(e); return TEST_PASS; }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        //[Variation(Desc = "WriteNode DTD SYSTEM with identifier", Param = true)]
        //[Variation(Desc = "WriteNode DTD SYSTEM with identifier", Param = false)]
        public int writeNode_XmlReader38()
        {
            string strxml = "<!DOCTYPE root SYSTEM \"#\"><root/>";
            try
            {
                using (XmlReader xr = CreateReader(new StringReader(strxml)))
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteNode(xr, (bool)CurVariation.Param);
                    }
                }
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            return TEST_FAIL;
        }

        //[Variation(Desc = "WriteNode DTD SYSTEM with valid surrogate pair", Param = true)]
        //[Variation(Desc = "WriteNode DTD SYSTEM with valid surrogate pair", Param = false)]
        public int writeNode_XmlReader39()
        {
            string strxml = "<!DOCTYPE root SYSTEM \"\uD812\uDD12\"><root/>";
            string exp = "<!DOCTYPE root SYSTEM \"\uD812\uDD12\"[]><root />";
            try
            {
                using (XmlReader xr = CreateReader(new StringReader(strxml)))
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteNode(xr, (bool)CurVariation.Param);
                    }
                }
            }
            catch (XmlException e) { CError.WriteLine(e); return TEST_PASS; }
            catch (FileNotFoundException e) { CError.WriteLine(e); return TEST_PASS; }
            if (WriterType == WriterType.CharCheckingWriter)
            {
                return (CompareString(exp)) ? TEST_PASS : TEST_FAIL;
            }
            return TEST_FAIL;
        }
    }

    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - COREREADER", Param = "COREREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XMLTEXTREADER", Param = "XMLTEXTREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XMLDOCNODEREADER", Param = "XMLDOCNODEREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XMLDATADOCNODEREADER", Param = "XMLDATADOCNODEREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XPATHDOCNAVIGATORREADER", Param = "XPATHDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XMLDOCNAVIGATORREADER", Param = "XMLDOCNAVIGATORREADER")]
    //[TestCase(Name = "WriteNode with streaming API ReadValueChunk - XMLDATADOCNAVIGATORREADER", Param = "XMLDATADOCNAVIGATORREADER")]
    public partial class TCWriteNode_With_ReadValueChunk : ReaderParamTestCase
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
        public int writeNode_1()
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
                    return CompareBaseline("textnode_1K-1_utf8.xml") ? TEST_PASS : TEST_FAIL;
                case 1024:
                    return CompareBaseline("textnode_1K_utf8.xml") ? TEST_PASS : TEST_FAIL;
                case 1025:
                    return CompareBaseline("textnode_1K+1_utf8.xml") ? TEST_PASS : TEST_FAIL;
                case 2048:
                    return CompareBaseline("textnode_2K_utf8.xml") ? TEST_PASS : TEST_FAIL;
                case 4096:
                    return CompareBaseline("textnode_4K_utf8.xml") ? TEST_PASS : TEST_FAIL;
            }
            CError.WriteLine("Error");
            return TEST_FAIL;
        }

        //[Variation(id = 6, Desc = "Input XML in unicode encoding, text node has 1K-1 chars", Pri = 0, Param = "1023")]
        //[Variation(id = 7, Desc = "Input XML in unicode encoding, text node has 1K chars", Pri = 0, Param = "1024")]
        //[Variation(id = 8, Desc = "Input XML in unicode encoding, text node has 1K+1 chars", Pri = 0, Param = "1025")]
        //[Variation(id = 9, Desc = "Input XML in unicode encoding, text node has 2K chars", Pri = 0, Param = "2048")]
        //[Variation(id = 10, Desc = "Input XML in unicode encoding, text node has 4K chars", Pri = 0, Param = "4096")]
        public int writeNode_2()
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
                    return CompareBaseline("textnode_1K-1_unicode.xml") ? TEST_PASS : TEST_FAIL;
                case 1024:
                    return CompareBaseline("textnode_1K_unicode.xml") ? TEST_PASS : TEST_FAIL;
                case 1025:
                    return CompareBaseline("textnode_1K+1_unicode.xml") ? TEST_PASS : TEST_FAIL;
                case 2048:
                    return CompareBaseline("textnode_2K_unicode.xml") ? TEST_PASS : TEST_FAIL;
                case 4096:
                    return CompareBaseline("textnode_4K_unicode.xml") ? TEST_PASS : TEST_FAIL;
            }
            CError.WriteLine("Error");
            return TEST_FAIL;
        }


        //[Variation(id = 11, Desc = "Trailing surrogate pair", Pri = 1)]
        public int writeNode_3()
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
            return CompareBaseline("trailing_surrogate_1K.xml") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 12, Desc = "Leading surrogate pair", Pri = 1)]
        public int writeNode_4()
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

            return CompareBaseline("leading_surrogate_1K.xml") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 13, Desc = "Split surrogate pair across 1K buffer boundary", Pri = 1)]
        public int writeNode_5()
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

            return CompareBaseline("split_surrogate_1K.xml") ? TEST_PASS : TEST_FAIL;
        }
    }

    //[TestCase(Name = "WriteFullEndElement")]
    public partial class TCFullEndElement : XmlWriterTestCaseBase
    {
        //[Variation(id = 1, Desc = "Sanity test for WriteFullEndElement()", Pri = 0)]
        public int fullEndElement_1()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteFullEndElement();
            }
            return CompareReader("<Root></Root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 2, Desc = "Call WriteFullEndElement before calling WriteStartElement", Pri = 2)]
        public int fullEndElement_2()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteFullEndElement();
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 3, Desc = "Call WriteFullEndElement after WriteEndElement", Pri = 2)]
        public int fullEndElement_3()
        {
            using (XmlWriter w = CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteFullEndElement();
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Did not throw exception");
            return TEST_FAIL;
        }

        //[Variation(id = 4, Desc = "Call WriteFullEndElement without closing attributes", Pri = 1)]
        public int fullEndElement_4()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("a");
                w.WriteString("b");
                w.WriteFullEndElement();
            }
            return CompareReader("<Root a=\"b\"></Root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 5, Desc = "Call WriteFullEndElement after WriteStartAttribute", Pri = 1)]
        public int fullEndElement_5()
        {
            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("a");
                w.WriteFullEndElement();
            }
            return CompareReader("<Root a=\"\"></Root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id = 6, Desc = "WriteFullEndElement for 100 nested elements", Pri = 1)]
        public int fullEndElement_6()
        {
            using (XmlWriter w = CreateWriter())
            {
                for (int i = 0; i < 100; i++)
                {
                    string eName = "Node" + i.ToString();
                    w.WriteStartElement(eName);
                }
                for (int i = 0; i < 100; i++)
                    w.WriteFullEndElement();

                w.Dispose();
                return CompareBaseline("100FullEndElements.txt") ? TEST_PASS : TEST_FAIL;
            }
        }

        //[TestCase(Name = "Element Namespace")]
        public partial class TCElemNamespace : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "Multiple NS decl for same prefix on an element", Pri = 1)]
            public int elemNamespace_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", "x", null, "foo");
                        w.WriteAttributeString("xmlns", "x", null, "bar");
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "Multiple NS decl for same prefix (same NS value) on an element", Pri = 1)]
            public int elemNamespace_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", "x", null, "foo");
                        w.WriteAttributeString("xmlns", "x", null, "foo");
                        w.WriteEndElement();
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "Element and attribute have same prefix, but different namespace value", Pri = 2)]
            public int elemNamespace_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("x", "a", "bar", "b");
                    w.WriteEndElement();
                }
                return CompareString("<~f x a~:Root ~a p1 a~:a=\"b\" xmlns:~a p1 A~=\"bar\" xmlns:~f x A~=\"foo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "Nested elements have same prefix, but different namespace", Pri = 1)]
            public int elemNamespace_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteStartElement("x", "level1", "bar");
                    w.WriteStartElement("x", "level2", "blah");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<x:Root xmlns:x=\"foo\"><x:level1 xmlns:x=\"bar\"><x:level2 xmlns:x=\"blah\" /></x:level1></x:Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 5, Desc = "Mapping reserved prefix xml to invalid namespace", Pri = 1)]
            public int elemNamespace_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("xml", "Root", "blah");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 6, Desc = "Mapping reserved prefix xml to correct namespace", Pri = 1)]
            public int elemNamespace_6()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("xml", "Root", "http://www.w3.org/XML/1998/namespace");
                    w.WriteEndElement();
                }

                return CompareReader("<xml:Root />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 7, Desc = "Write element with prefix beginning with xml", Pri = 1)]
            public int elemNamespace_7()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartElement("xmlA", "elem1", "test");
                    w.WriteEndElement();
                    w.WriteStartElement("xMlB", "elem2", "test");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<Root><xmlA:elem1 xmlns:xmlA=\"test\" /><xMlB:elem2 xmlns:xMlB=\"test\" /></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 8, Desc = "Reuse prefix that refers the same as default namespace", Pri = 2)]
            public int elemNamespace_8()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("x", "foo", "uri-1");
                    w.WriteStartElement("", "bar", "uri-1");
                    w.WriteStartElement("x", "bop", "uri-1");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<x:foo xmlns:x=\"uri-1\"><bar xmlns=\"uri-1\"><x:bop /></bar></x:foo>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 9, Desc = "Should throw error for prefix=xmlns", Pri = 2)]
            public int elemNamespace_9()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("xmlns", "localname", "uri:bogus");
                        w.WriteEndElement();
                    }
                    catch (Exception e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw error");
                return TEST_FAIL;
            }

            //[Variation(id = 10, Desc = "Create nested element without prefix but with namespace of parent element with a defined prefix", Pri = 2)]
            public int elemNamespace_10()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteStartElement("level1", "fo");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<Root xmlns:x=\"fo\"><x:level1 /></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 11, Desc = "Create different prefix for element and attribute that have same namespace", Pri = 2)]
            public int elemNamespace_11()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("y", "attr", "foo", "b");
                    w.WriteEndElement();
                }
                return CompareReader("<x:Root y:attr=\"b\" xmlns:y=\"foo\" xmlns:x=\"foo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 12, Desc = "Create same prefix for element and attribute that have same namespace", Pri = 2)]
            public int elemNamespace_12()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("x", "attr", "foo", "b");
                    w.WriteEndElement();
                }
                return CompareReader("<x:Root x:attr=\"b\" xmlns:x=\"foo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 13, Desc = "Try to re-define NS prefix on attribute which is aleady defined on an element", Pri = 2)]
            public int elemNamespace_13()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("x", "attr", "bar", "test");
                    w.WriteEndElement();
                }
                return CompareString("<~f x a~:Root ~a p1 a~:attr=\"test\" xmlns:~a p1 A~=\"bar\" xmlns:~f x A~=\"foo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 14, Desc = "Namespace string contains surrogates, reuse at different levels", Pri = 1)]
            public int elemNamespace_14()
            {
                string uri = "urn:\uD800\uDC00";

                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteAttributeString("xmlns", "pre", null, uri);
                    w.WriteElementString("elt", uri, "text");
                    w.WriteEndElement();
                }
                string strExpected = String.Format("<root xmlns:pre=\"{0}\"><pre:elt>text</pre:elt></root>", uri);
                return CompareReader(strExpected) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 15, Desc = "Namespace containing entities, use at multiple levels", Pri = 1)]
            public int elemNamespace_15()
            {
                using (XmlWriter w = CreateWriter())
                {
                    string strxml = "<?xml version=\"1.0\" ?><root xmlns:foo=\"urn:&lt;&gt;\"><foo:elt1 /><foo:elt2 /><foo:elt3 /></root>";

                    XmlReader xr = ReaderHelper.Create(new StringReader(strxml));
                    w.WriteNode(xr, false);
                    xr.Dispose();
                }
                return CompareReader("<root xmlns:foo=\"urn:&lt;&gt;\"><foo:elt1 /><foo:elt2 /><foo:elt3 /></root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 16, Desc = "Verify it resets default namespace when redefined earlier in the stack", Pri = 1)]
            public int elemNamespace_16()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("", "x", "foo");
                    w.WriteAttributeString("xmlns", "foo");
                    w.WriteStartElement("", "y", "");
                    w.WriteStartElement("", "z", "foo");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<x xmlns=\"foo\"><y xmlns=\"\"><z xmlns=\"foo\" /></y></x>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 17, Desc = "The default namespace for an element can not be changed once it is written out", Pri = 1)]
            public int elemNamespace_17()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", null, "test");
                        w.WriteEndElement();
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 18, Desc = "Map XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix", Pri = 1)]
            public int elemNamespace_18()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("foo", "bar", "http://www.w3.org/XML/1998/namaespace");
                    w.WriteEndElement();
                }
                return CompareReader("<foo:bar xmlns:foo=\"http://www.w3.org/XML/1998/namaespace\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 19, Desc = "Pass NULL as NS to WriteStartElement", Pri = 1)]
            public int elemNamespace_19()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("foo", "Root", "NS");
                    w.WriteStartElement("bar", null);
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<foo:Root xmlns:foo=\"NS\"><bar /></foo:Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 20, Desc = "Write element in reserved XML namespace, should error", Pri = 1)]
            public int elemNamespace_20()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("foo", "Root", "http://www.w3.org/XML/1998/namespace");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                return TEST_FAIL;
            }

            //[Variation(id = 21, Desc = "Write element in reserved XMLNS namespace, should error", Pri = 1)]
            public int elemNamespace_21()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("foo", "Root", "http://www.w3.org/XML/1998/namespace");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                return TEST_FAIL;
            }

            //[Variation(id = 22, Desc = "Mapping a prefix to empty ns should error", Pri = 1)]
            public int elemNamespace_22()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("pre", "test", string.Empty);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 23, Desc = "Pass null prefix to WriteStartElement()", Pri = 1)]
            public int elemNamespace_23()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement(null, "Root", "ns");
                    w.WriteEndElement();
                }
                return CompareReader("<Root xmlns='ns' />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 24, Desc = "Pass String.Empty prefix to WriteStartElement()", Pri = 1)]
            public int elemNamespace_24()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement(String.Empty, "Root", "ns");
                    w.WriteEndElement();
                }
                return CompareReader("<Root xmlns='ns' />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 25, Desc = "Pass null ns to WriteStartElement()", Pri = 1)]
            public int elemNamespace_25()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root", null);
                    w.WriteEndElement();
                }
                return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 26, Desc = "Pass String.Empty ns to WriteStartElement()", Pri = 1)]
            public int elemNamespace_26()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root", String.Empty);
                    w.WriteEndElement();
                }
                return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 27, Desc = "Pass null prefix to WriteStartElement() when namespace is in scope", Pri = 1)]
            public int elemNamespace_27()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteElementString(null, "child", "ns", "test");
                    w.WriteEndElement();
                }
                return CompareReader("<pre:Root xmlns:pre='ns'><pre:child>test</pre:child></pre:Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 28, Desc = "Pass String.Empty prefix to WriteStartElement() when namespace is in scope", Pri = 1)]
            public int elemNamespace_28()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteElementString(String.Empty, "child", "ns", "test");
                    w.WriteEndElement();
                }
                return CompareReader("<pre:Root xmlns:pre='ns'><child xmlns='ns'>test</child></pre:Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 29, Desc = "Pass null ns to WriteStartElement() when prefix is in scope", Pri = 1)]
            public int elemNamespace_29()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteElementString("pre", "child", null, "test");
                    w.WriteEndElement();
                }
                return CompareReader("<pre:Root xmlns:pre='ns'><pre:child>test</pre:child></pre:Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 30, Desc = "Pass String.Empty ns to WriteStartElement() when prefix is in scope", Pri = 1)]
            public int elemNamespace_30()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("pre", "Root", "ns");
                        w.WriteElementString("pre", "child", String.Empty, "test");
                    }
                    catch (ArgumentException)
                    {
                        return TEST_PASS;
                    }
                }
                return TEST_FAIL;
            }

            //[Variation(id = 31, Desc = "Pass String.Empty ns to WriteStartElement() when prefix is in scope", Pri = 1)]
            public int elemNamespace_31()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("pre", "Root", "ns");
                        w.WriteElementString("pre", "child", String.Empty, "test");
                    }
                    catch (ArgumentException)
                    {
                        return TEST_PASS;
                    }
                }
                return TEST_FAIL;
            }

            //[Variation(id = 31, Desc = "Mapping empty ns uri to a prefix should error", Pri = 1)]
            public int elemNamespace_32()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("prefix", "localname", null);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        return TEST_PASS;
                    }
                }
                return TEST_FAIL;
            }
        }

        //[TestCase(Name = "Attribute Namespace")]
        public partial class TCAttrNamespace : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "Define prefix 'xml' with invalid namespace URI 'foo'", Pri = 1)]
            public int attrNamespace_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", "xml", null, "foo");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "Bind NS prefix 'xml' with valid namespace URI", Pri = 1)]
            public int attrNamespace_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "xml", null, "http://www.w3.org/XML/1998/namespace");
                    w.WriteEndElement();
                }
                string exp = (WriterType == WriterType.UnicodeWriter) ? "<Root />" : "<Root xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" />";
                return CompareReader(exp) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "Bind NS prefix 'xmlA' with namespace URI 'foo'", Pri = 1)]
            public int attrNamespace_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "xmlA", null, "foo");
                    w.WriteEndElement();
                }
                return CompareReader("<Root xmlns:xmlA=\"foo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "Write attribute xml:space with correct namespace", Pri = 1)]
            public int attrNamespace_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "default");
                    w.WriteEndElement();
                }
                return CompareReader("<Root xml:space=\"default\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 5, Desc = "Write attribute xml:space with incorrect namespace", Pri = 1)]
            public int attrNamespace_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "space", "foo", "default");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 6, Desc = "Write attribute xml:lang with incorrect namespace", Pri = 1)]
            public int attrNamespace_6()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "lang", "foo", "EN");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }


            //[Variation(id = 7, Desc = "WriteAttribute, define namespace attribute before value attribute", Pri = 1)]
            public int attrNamespace_7()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteEndElement();
                }
                return CompareReader("<Root xmlns:x=\"fo\" x:a=\"b\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 8, Desc = "WriteAttribute, define namespace attribute after value attribute", Pri = 1)]
            public int attrNamespace_8()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "fo", "b");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteEndElement();
                }
                return CompareReader("<Root x:a=\"b\" xmlns:x=\"fo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 9, Desc = "WriteAttribute, redefine prefix at different scope and use both of them", Pri = 1)]
            public int attrNamespace_9()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("level1");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteStartElement("level2");
                    w.WriteAttributeString("xmlns", "x", null, "bar");
                    w.WriteAttributeString("c", "bar", "d");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<level1 xmlns:x=\"fo\" x:a=\"b\"><level2 xmlns:x=\"bar\" x:c=\"d\" /></level1>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 10, Desc = "WriteAttribute, redefine namespace at different scope and use both of them", Pri = 1)]
            public int attrNamespace_10()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("level1");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteStartElement("level2");
                    w.WriteAttributeString("xmlns", "y", null, "fo");
                    w.WriteAttributeString("c", "fo", "d");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<level1 xmlns:x=\"fo\" x:a=\"b\"><level2 xmlns:y=\"fo\" y:c=\"d\" /></level1>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 11, Desc = "WriteAttribute with colliding prefix with element", Pri = 1)]
            public int attrNamespace_11()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "fo");
                    w.WriteAttributeString("x", "a", "bar", "b");
                    w.WriteEndElement();
                }
                return CompareString("<~f x a~:Root ~a p1 a~:a=\"b\" xmlns:~a p1 A~=\"bar\" xmlns:~f x A~=\"fo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 12, Desc = "WriteAttribute with colliding namespace with element", Pri = 1)]
            public int attrNamespace_12()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "fo");
                    w.WriteAttributeString("y", "a", "fo", "b");
                    w.WriteEndElement();
                }
                return CompareReader("<x:Root y:a=\"b\" xmlns:y=\"fo\" xmlns:x=\"fo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 13, Desc = "WriteAttribute with namespace but no prefix", Pri = 1)]
            public int attrNamespace_13()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteEndElement();
                }
                return CompareString("<Root ~a p1 a~:a=\"b\" xmlns:~a p1 A~=\"fo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 14, Desc = "WriteAttribute for 2 attributes with same prefix but different namespace", Pri = 1)]
            public int attrNamespace_14()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "fo", "b");
                    w.WriteAttributeString("x", "c", "bar", "d");
                    w.WriteEndElement();
                }
                return CompareString("<Root ~f x a~:a=\"b\" ~a p2 a~:c=\"d\" xmlns:~a p2 A~=\"bar\" xmlns:~f x A~=\"fo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 15, Desc = "WriteAttribute with String.Empty and null as namespace and prefix values", Pri = 1)]
            public int attrNamespace_15()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString(null, "a", null, "b");
                    w.WriteAttributeString(String.Empty, "c", String.Empty, "d");
                    w.WriteAttributeString(null, "e", String.Empty, "f");
                    w.WriteAttributeString(String.Empty, "g", null, "h");
                    w.WriteEndElement();
                }
                return CompareReader("<Root a=\"b\" c=\"d\" e=\"f\" g=\"h\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 16, Desc = "WriteAttribute to manually create attribute of xmlns:x", Pri = 1)]
            public int attrNamespace_16()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "test");
                    w.WriteStartElement("x", "level1", null);
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<Root xmlns:x=\"test\"><x:level1 /></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 17, Desc = "WriteAttribute with namespace value = null while a prefix exists", Pri = 1)]
            public int attrNamespace_17()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", null, "b");
                    w.WriteEndElement();
                }
                return CompareReader("<Root a=\"b\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 18, Desc = "WriteAttribute with namespace value = String.Empty while a prefix exists", Pri = 1)]
            public int attrNamespace_18()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", String.Empty, "b");
                    w.WriteEndElement();
                }
                return CompareReader("<Root a=\"b\" />") ? TEST_PASS : TEST_FAIL;
            }


            //[Variation(id = 19, Desc = "WriteAttribe in nested elements with same namespace but different prefix", Pri = 1)]
            public int attrNamespace_19()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "x", "fo", "y");
                    w.WriteAttributeString("xmlns", "a", null, "fo");
                    w.WriteStartElement("level1");
                    w.WriteAttributeString("b", "x", "fo", "y");
                    w.WriteAttributeString("xmlns", "b", null, "fo");
                    w.WriteStartElement("level2");
                    w.WriteAttributeString("c", "x", "fo", "y");
                    w.WriteAttributeString("xmlns", "c", null, "fo");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<Root a:x=\"y\" xmlns:a=\"fo\"><level1 b:x=\"y\" xmlns:b=\"fo\"><level2 c:x=\"y\" xmlns:c=\"fo\" /></level1></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 20, Desc = "WriteAttribute for x:a and xmlns:a diff namespace", Pri = 1)]
            public int attrNamespace_20()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "bar", "b");
                    w.WriteAttributeString("xmlns", "a", null, "foo");
                    w.WriteEndElement();
                }
                return CompareReader("<Root x:a=\"b\" xmlns:a=\"foo\" xmlns:x=\"bar\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 21, Desc = "WriteAttribute for x:a and xmlns:a same namespace", Pri = 1)]
            public int attrNamespace_21()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "foo", "b");
                    w.WriteAttributeString("xmlns", "a", null, "foo");
                    w.WriteEndElement();
                }
                return CompareReader("<Root x:a=\"b\" xmlns:a=\"foo\" xmlns:x=\"foo\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 22, Desc = "WriteAttribute with colliding NS and prefix for 2 attributes", Pri = 1)]
            public int attrNamespace_22()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "foo");
                    w.WriteAttributeString("x", "a", "foo", "b");
                    w.WriteAttributeString("x", "c", "foo", "b");
                    w.WriteEndElement();
                }
                return CompareReader("<Root xmlns:x=\"foo\" x:a=\"b\" x:c=\"b\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 23, Desc = "WriteAttribute with DQ in namespace", Pri = 2)]
            public int attrNamespace_23()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "\"", "b");
                    w.WriteEndElement();
                }
                return CompareString("<Root ~a p1 a~:a=\"b\" xmlns:~a p1 A~=\"&quot;\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 24, Desc = "Attach prefix with empty namespace", Pri = 1)]
            public int attrNamespace_24()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", "foo", "bar", "");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 25, Desc = "Explicitly write namespace attribute that maps XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix", Pri = 1)]
            public int attrNamespace_25()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "foo", "", "http://www.w3.org/XML/1998/namaespace");
                    w.WriteEndElement();
                }
                return CompareReader("<Root xmlns:foo=\"http://www.w3.org/XML/1998/namaespace\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 26, Desc = "Map XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix", Pri = 1)]
            public int attrNamespace_26()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("foo", "bar", "http://www.w3.org/XML/1998/namaespace", "test");
                    w.WriteEndElement();
                }
                return CompareReader("<Root foo:bar=\"test\" xmlns:foo=\"http://www.w3.org/XML/1998/namaespace\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 27, Desc = "Pass empty namespace to WriteAttributeString(prefix, name, ns, value)", Pri = 1)]
            public int attrNamespace_27()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "urn:pre");
                    w.WriteAttributeString("pre", "attr", "", "test");
                    w.WriteEndElement();
                }
                return CompareReader("<pre:Root attr=\"test\" xmlns:pre=\"urn:pre\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 28, Desc = "Write attribute with prefix = xmlns", Pri = 1)]
            public int attrNamespace_28()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", "xmlns", null, "test");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 29, Desc = "Write attribute in reserved XML namespace, should error", Pri = 1)]
            public int attrNamespace_29()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("foo");
                        w.WriteAttributeString("aaa", "bbb", "http://www.w3.org/XML/1998/namespace", "ccc");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                return TEST_FAIL;
            }

            //[Variation(id = 30, Desc = "Write attribute in reserved XMLNS namespace, should error", Pri = 1)]
            public int attrNamespace_30()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("foo");
                        w.WriteStartAttribute("aaa", "bbb", "http://www.w3.org/XML/1998/namespace");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                return TEST_FAIL;
            }

            //[Variation(id = 31, Desc = "WriteAttributeString with no namespace under element with empty prefix", Pri = 1)]
            public int attrNamespace_31()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("d", "Data", "http://example.org/data");
                    w.WriteStartElement("g", "GoodStuff", "http://example.org/data/good");
                    w.WriteAttributeString("hello", "world");
                    w.WriteEndElement();
                    w.WriteStartElement("BadStuff", "http://example.org/data/bad");
                    w.WriteAttributeString("hello", "world");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareReader("<d:Data xmlns:d=\"http://example.org/data\">" +
                                    "<g:GoodStuff hello=\"world\" xmlns:g=\"http://example.org/data/good\" />" +
                                    "<BadStuff hello=\"world\" xmlns=\"http://example.org/data/bad\" />" +
                                    "</d:Data>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 32, Desc = "Pass null prefix to WriteAttributeString()", Pri = 1)]
            public int attrNamespace_32()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString(null, "attr", "ns", "value");
                    w.WriteEndElement();
                }
                return CompareString("<Root ~a p1 a~:attr=\"value\" xmlns:~a p1 A~=\"ns\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 33, Desc = "Pass String.Empty prefix to WriteAttributeString()", Pri = 1)]
            public int attrNamespace_33()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString(String.Empty, "attr", "ns", "value");
                    w.WriteEndElement();
                }
                return CompareString("<Root ~a p1 a~:attr=\"value\" xmlns:~a p1 A~=\"ns\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 34, Desc = "Pass null ns to WriteAttributeString()", Pri = 1)]
            public int attrNamespace_34()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("pre", "attr", null, "value");
                    w.WriteEndElement();
                }
                return CompareReader("<Root attr='value' />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 35, Desc = "Pass String.Empty ns to WriteAttributeString()", Pri = 1)]
            public int attrNamespace_35()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("pre", "attr", String.Empty, "value");
                    w.WriteEndElement();
                }
                return CompareReader("<Root attr='value' />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 36, Desc = "Pass null prefix to WriteAttributeString() when namespace is in scope", Pri = 1)]
            public int attrNamespace_36()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString(null, "child", "ns", "test");
                    w.WriteEndElement();
                }
                return CompareReader("<pre:Root pre:child='test' xmlns:pre='ns' />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 37, Desc = "Pass String.Empty prefix to WriteAttributeString() when namespace is in scope", Pri = 1)]
            public int attrNamespace_37()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString(String.Empty, "child", "ns", "test");
                    w.WriteEndElement();
                }
                return CompareReader("<pre:Root pre:child='test' xmlns:pre='ns' />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 38, Desc = "Pass null ns to WriteAttributeString() when prefix is in scope", Pri = 1)]
            public int attrNamespace_38()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString("pre", "child", null, "test");
                    w.WriteEndElement();
                }
                return CompareReader("<pre:Root pre:child='test' xmlns:pre='ns' />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 39, Desc = "Pass String.Empty ns to WriteAttributeString() when prefix is in scope", Pri = 1)]
            public int attrNamespace_39()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString("pre", "child", String.Empty, "test");
                    w.WriteEndElement();
                }
                return CompareReader("<pre:Root child='test' xmlns:pre='ns' />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 40, Desc = "Mapping empty ns uri to a prefix should error", Pri = 1)]
            public int attrNamespace_40()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteAttributeString("xmlns", null, null, "test");
                        w.WriteEndElement();
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        return TEST_PASS;
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        return TEST_PASS;
                    }
                }
                return TEST_FAIL;
            }

            //[Variation(id = 42, Desc = "WriteStartAttribute with prefix = null, localName = xmlns - case 2", Pri = 1)]
            public int attrNamespace_42()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("pre", "foo", "ns1");
                    w.WriteAttributeString(null, "xmlns", "http://www.w3.org/2000/xmlns/", "ns");
                    w.WriteEndElement();
                }
                return CompareReader("<pre:foo xmlns='ns' xmlns:pre='ns1' />") ? TEST_PASS : TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteCData")]
        public partial class TCCData : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "WriteCData with null", Pri = 1)]
            public int CData_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData(null);
                    w.WriteEndElement();
                }
                return CompareReader("<Root><![CDATA[]]></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "WriteCData with String.Empty", Pri = 1)]
            public int CData_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData(String.Empty);
                    w.WriteEndElement();
                }
                return CompareReader("<Root><![CDATA[]]></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "WriteCData Sanity test", Pri = 0)]
            public int CData_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("This text is in a CDATA section");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><![CDATA[This text is in a CDATA section]]></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "WriteCData with valid surrogate pair", Pri = 1)]
            public int CData_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("\uD812\uDD12");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><![CDATA[\uD812\uDD12]]></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 5, Desc = "WriteCData with ]]>", Pri = 1)]
            public int CData_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("test ]]> test");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><![CDATA[test ]]]]><![CDATA[> test]]></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 6, Desc = "WriteCData with & < > chars, they should not be escaped", Pri = 2)]
            public int CData_6()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("<greeting>Hello World! & Hello XML</greeting>");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><![CDATA[<greeting>Hello World! & Hello XML</greeting>]]></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 7, Desc = "WriteCData with <![CDATA[", Pri = 2)]
            public int CData_7()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("<![CDATA[");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><![CDATA[<![CDATA[]]></Root>") ? TEST_PASS : TEST_FAIL;
            }
            //[Variation(id = 8, Desc = "CData state machine", Pri = 2)]
            public int CData_8()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("]x]>]]x> x]x]x> x]]x]]x>");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><![CDATA[]x]>]]x> x]x]x> x]]x]]x>]]></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 9, Desc = "WriteCData with invalid surrogate pair", Pri = 1)]
            public int CData_9()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteCData("\uD812");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CheckErrorState(w.WriteState);
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 10, Desc = "WriteCData after root element")]
            public int CData_10()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteEndElement();
                        w.WriteCData("foo");
                    }
                    catch (InvalidOperationException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 11, Desc = "Call WriteCData twice - that should write two CData blocks", Pri = 1)]
            public int CData_11()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("foo");
                    w.WriteCData("bar");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><![CDATA[foo]]><![CDATA[bar]]></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 12, Desc = "WriteCData with empty string at the buffer boundary", Pri = 1)]
            public int CData_12()
            {
                // WriteCData with empty string when the write buffer looks like
                // <r>aaaaaaa....   (currently lenght is 2048 * 3 - len("<![CDATA[")
                int buflen = 2048 * 3;
                string xml1 = "<r>";
                string xml3 = "<![CDATA[";
                int padlen = buflen - xml1.Length - xml3.Length;
                string xml2 = new string('a', padlen);
                string xml4 = "]]></r>";
                string expXml = String.Format("{0}{1}{2}{3}", xml1, xml2, xml3, xml4);
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("r");
                    w.WriteRaw(xml2);
                    w.WriteCData("");
                    w.WriteEndElement();
                }

                return CompareReader(expXml) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 13, Desc = "WriteCData with 0x0D with NewLineHandling.Replace", Pri = 1, Params = new Object[] { 0x0d, NewLineHandling.Replace, "<r><![CDATA[\r\n]]></r>" })]
            //[Variation(id = 14, Desc = "WriteCData with 0x0D with NewLineHandling.None", Pri = 1, Params = new Object[] { 0x0d, NewLineHandling.None, "<r><![CDATA[\r]]></r>" })]
            //[Variation(id = 15, Desc = "WriteCData with 0x0D with NewLineHandling.Entitize", Pri = 1, Params = new Object[] { 0x0d, NewLineHandling.Entitize, "<r><![CDATA[\r]]></r>" })]
            //[Variation(id = 16, Desc = "WriteCData with 0x0A with NewLineHandling.Replace", Pri = 1, Params = new Object[] { 0x0a, NewLineHandling.Replace, "<r><![CDATA[\r\n]]></r>" })]
            //[Variation(id = 17, Desc = "WriteCData with 0x0A with NewLineHandling.None", Pri = 1, Params = new Object[] { 0x0a, NewLineHandling.None, "<r><![CDATA[\n]]></r>" })]
            //[Variation(id = 18, Desc = "WriteCData with 0x0A with NewLineHandling.Entitize", Pri = 1, Params = new Object[] { 0x0a, NewLineHandling.Entitize, "<r><![CDATA[\n]]></r>" })]
            public int CData_13()
            {
                char ch = (char)(int)CurVariation.Params[0];
                NewLineHandling nlh = (NewLineHandling)CurVariation.Params[1];
                string expXml = (string)CurVariation.Params[2];

                XmlWriterSettings xws = new XmlWriterSettings();
                xws.OmitXmlDeclaration = true;
                xws.NewLineHandling = nlh;

                using (XmlWriter w = CreateWriter(xws))
                {
                    w.WriteStartElement("r");
                    w.WriteCData(new string(ch, 1));
                    w.WriteEndElement();
                }
                return CompareString(expXml) ? TEST_PASS : TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteComment")]
        public partial class TCComment : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "Sanity test for WriteComment", Pri = 0)]
            public int comment_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteComment("This text is a comment");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><!--This text is a comment--></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "Comment value = String.Empty", Pri = 0)]
            public int comment_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteComment(String.Empty);
                    w.WriteEndElement();
                }
                return CompareReader("<Root><!----></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "Comment value = null", Pri = 0)]
            public int comment_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteComment(null);
                    w.WriteEndElement();
                }
                return CompareReader("<Root><!----></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "WriteComment with valid surrogate pair", Pri = 1)]
            public int comment_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteComment("\uD812\uDD12");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><!--\uD812\uDD12--></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 5, Desc = "WriteComment with invalid surrogate pair", Pri = 1)]
            public int comment_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteComment("\uD812");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CheckErrorState(w.WriteState);
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw error");
                return TEST_FAIL;
            }

            //[Variation(id = 6, Desc = "WriteComment with -- in value", Pri = 1)]
            public int comment_6()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteComment("test --");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><!--test - - --></Root>") ? TEST_PASS : TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteEntityRef")]
        public partial class TCEntityRef : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "WriteEntityRef with value = null", Param = "null", Pri = 1)]
            //[Variation(id = 2, Desc = "WriteEntityRef with value = String.Empty", Param = "String.Empty", Pri = 1)]
            //[Variation(id = 3, Desc = "WriteEntityRef with invalid value <;", Param = "test<test", Pri = 1)]
            //[Variation(id = 4, Desc = "WriteEntityRef with invalid value >", Param = "test>test", Pri = 1)]
            //[Variation(id = 5, Desc = "WriteEntityRef with invalid value &", Param = "test&test", Pri = 1)]
            //[Variation(id = 6, Desc = "WriteEntityRef with invalid value & and ;", Param = "&test;", Pri = 1)]
            //[Variation(id = 7, Desc = "WriteEntityRef with invalid value SQ", Param = "test'test", Pri = 1)]
            //[Variation(id = 8, Desc = "WriteEntityRef with invalid value DQ", Param = "test\"test", Pri = 1)]
            //[Variation(id = 9, Desc = "WriteEntityRef with #xD", Param = "\xD", Pri = 1)]
            //[Variation(id = 10, Desc = "WriteEntityRef with #xA", Param = "\xD", Pri = 1)]
            //[Variation(id = 11, Desc = "WriteEntityRef with #xD#xA", Param = "\xD\xA", Pri = 1)]
            public int entityRef_1()
            {
                string temp = null;
                switch (CurVariation.Param.ToString())
                {
                    case "null":
                        temp = null;
                        break;
                    case "String.Empty":
                        temp = String.Empty;
                        break;
                    default:
                        temp = CurVariation.Param.ToString();
                        break;
                }
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteEntityRef(temp);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                    catch (NullReferenceException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw error");
                return TEST_FAIL;
            }

            //[Variation(id = 12, Desc = "WriteEntityRef with entity defined in doctype", Pri = 1)]
            public int entityRef_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteDocType("Root", null, null, "<!ENTITY e \"test\">");
                    w.WriteStartElement("Root");
                    w.WriteEntityRef("e");
                    w.WriteEndElement();
                }
                return CompareReader("<!DOCTYPE Root [<!ENTITY e \"test\">]><Root>&e;</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 13, Desc = "WriteEntityRef in value for xml:lang attribute", Pri = 1)]
            public int entityRef_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteDocType("root", null, null, "<!ENTITY e \"en-us\">");
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteEntityRef("e");
                    w.WriteString("<");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                }
                return CompareReader("<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&e;&lt;\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 14, Desc = "XmlWriter: Entity Refs are entitized twice in xml:lang attributes", Pri = 1)]
            public int var_14()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteDocType("root", null, null, "<!ENTITY e \"en-us\">");
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteEntityRef("e");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                }
                return CompareReader("<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&e;\" />") ? TEST_PASS : TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteCharEntity")]
        public partial class TCCharEntity : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "WriteCharEntity with valid Unicode character", Pri = 0)]
            public int charEntity_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteCharEntity('\uD23E');
                    w.WriteEndAttribute();
                    w.WriteCharEntity('\uD7FF');
                    w.WriteCharEntity('\uE000');
                    w.WriteEndElement();
                }
                return CompareReader("<Root a=\"&#xD23E;\">&#xD7FF;&#xE000;</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "Call WriteCharEntity after WriteStartElement/WriteEndElement", Pri = 0)]
            public int charEntity_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCharEntity('\uD001');
                    w.WriteStartElement("elem");
                    w.WriteCharEntity('\uF345');
                    w.WriteEndElement();
                    w.WriteCharEntity('\u0048');
                    w.WriteEndElement();
                }
                return CompareReader("<Root>&#xD001;<elem>&#xF345;</elem>&#x48;</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "Call WriteCharEntity after WriteStartAttribute/WriteEndAttribute", Pri = 0)]
            public int charEntity_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteCharEntity('\u1289');
                    w.WriteEndAttribute();
                    w.WriteCharEntity('\u2584');
                    w.WriteEndElement();
                }
                return CompareReader("<Root a=\"&#x1289;\">&#x2584;</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "Character from low surrogate range", Pri = 1)]
            public int charEntity_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteCharEntity('\uDD12');
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 5, Desc = "Character from high surrogate range", Pri = 1)]
            public int charEntity_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteCharEntity('\uD812');
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 7, Desc = "Sanity test, pass 'a'", Pri = 0)]
            public int charEntity_7()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteCharEntity('c');
                    w.WriteEndElement();
                }
                string strExp = "<root>&#x63;</root>";
                return CompareReader(strExp) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 8, Desc = "WriteCharEntity for special attributes", Pri = 1)]
            public int charEntity_8()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteCharEntity('A');
                    w.WriteString("\n");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                }
                return CompareReader("<root xml:lang=\"A&#xA;\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 9, Desc = "XmlWriter generates invalid XML", Pri = 1)]
            public int bug35637()
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                };

                using (XmlWriter xw = CreateWriter())
                {
                    xw.WriteStartElement("root");
                    for (int i = 0; i < 150; i++)
                    {
                        xw.WriteElementString("e", "\u00e6\u00f8\u00e5\u00e9\u00ed\u00e8\u00f9\u00f6\u00f1\u00ea\u00fb\u00ee\u00c2\u00c5\u00d8\u00f5\u00cf");
                    }
                    xw.WriteElementString("end", "end");
                    xw.WriteEndElement();
                }

                using (XmlReader reader = GetReader())
                {
                    reader.ReadToDescendant("end"); // should not throw here
                }

                return TEST_PASS;
            }
        }

        //[TestCase(Name = "WriteSurrogateCharEntity")]
        public partial class TCSurrogateCharEntity : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "SurrogateCharEntity after WriteStartElement/WriteEndElement", Pri = 1)]
            public int surrogateEntity_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                    w.WriteStartElement("Elem");
                    w.WriteSurrogateCharEntity('\uDE44', '\uDAFF');
                    w.WriteEndElement();
                    w.WriteSurrogateCharEntity('\uDC22', '\uD820');
                    w.WriteEndElement();
                }
                return CompareReader("<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "SurrogateCharEntity after WriteStartAttribute/WriteEndAttribute", Pri = 1)]
            public int surrogateEntity_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                    w.WriteEndAttribute();
                    w.WriteSurrogateCharEntity('\uDE44', '\uDAFF');
                    w.WriteEndElement();
                }
                return CompareReader("<Root a=\"&#x58341;\">&#xCFE44;</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "Test with limits of surrogate range", Pri = 1)]
            public int surrogateEntity_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteSurrogateCharEntity('\uDC00', '\uD800');
                    w.WriteEndAttribute();
                    w.WriteSurrogateCharEntity('\uDFFF', '\uD800');
                    w.WriteSurrogateCharEntity('\uDC00', '\uDBFF');
                    w.WriteSurrogateCharEntity('\uDFFF', '\uDBFF');
                    w.WriteEndElement();
                }
                return CompareReader("<Root a=\"&#x10000;\">&#x103FF;&#x10FC00;&#x10FFFF;</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "Middle surrogate character", Pri = 1)]
            public int surrogateEntity_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteSurrogateCharEntity('\uDD12', '\uDA34');
                    w.WriteEndElement();
                }
                return CompareReader("<Root>&#x9D112;</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 5, Desc = "Invalid high surrogate character", Pri = 1)]
            public int surrogateEntity_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteSurrogateCharEntity('\uDD12', '\uDD01');
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 6, Desc = "Invalid low surrogate character", Pri = 1)]
            public int surrogateEntity_6()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteSurrogateCharEntity('\u1025', '\uD900');
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 7, Desc = "Swap high-low surrogate characters", Pri = 1)]
            public int surrogateEntity_7()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteSurrogateCharEntity('\uD9A2', '\uDE34');
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 8, Desc = "WriteSurrogateCharEntity for special attributes", Pri = 1)]
            public int surrogateEntity_8()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteSurrogateCharEntity('\uDC00', '\uDBFF');
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                }
                string strExp = "<root xml:lang=\"&#x10FC00;\" />";
                return CompareReader(strExp) ? TEST_PASS : TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteProcessingInstruction")]
        public partial class TCPI : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "Sanity test for WritePI", Pri = 0)]
            public int pi_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("test", "This text is a PI");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><?test This text is a PI?></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "PI text value = null", Pri = 1)]
            public int pi_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("test", null);
                    w.WriteEndElement();
                }
                return CompareReader("<Root><?test?></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "PI text value = String.Empty", Pri = 1)]
            public int pi_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("test", String.Empty);
                    w.WriteEndElement();
                }
                return CompareReader("<Root><?test?></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "PI name = null should error", Pri = 1)]
            public int pi_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteProcessingInstruction(null, "test");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                    catch (NullReferenceException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element ");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 5, Desc = "PI name = String.Empty should error", Pri = 1)]
            public int pi_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteProcessingInstruction(String.Empty, "test");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 6, Desc = "WritePI with xmlns as the name value")]
            public int pi_6()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("xmlns", "text");
                    w.WriteEndElement();
                }
                return (CompareReader("<Root><?xmlns text?></Root>") ? TEST_PASS : TEST_FAIL);
            }

            //[Variation(id = 7, Desc = "WritePI with XmL as the name value")]
            public int pi_7()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteProcessingInstruction("XmL", "text");
                        w.WriteEndElement();
                        w.Dispose();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 8, Desc = "WritePI before XmlDecl", Pri = 1)]
            public int pi_8()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteProcessingInstruction("pi", "text");
                        w.WriteStartDocument(true);
                    }
                    catch (InvalidOperationException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 9, Desc = "WritePI (after StartDocument) with name = 'xml' text = 'version = 1.0' should error", Pri = 1)]
            public int pi_9()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartDocument();
                        w.WriteProcessingInstruction("xml", "version = \"1.0\"");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 10, Desc = "WritePI (before StartDocument) with name = 'xml' text = 'version = 1.0' should error", Pri = 1)]
            public int pi_10()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteProcessingInstruction("xml", "version = \"1.0\"");
                        w.WriteStartDocument();
                    }
                    catch (InvalidOperationException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 11, Desc = "Include PI end tag ?> as part of the text value", Pri = 1)]
            public int pi_11()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("badpi", "text ?>");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><?badpi text ? >?></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 12, Desc = "WriteProcessingInstruction with valid surrogate pair", Pri = 1)]
            public int pi_12()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("pi", "\uD812\uDD12");
                    w.WriteEndElement();
                }
                return CompareReader("<Root><?pi \uD812\uDD12?></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 13, Desc = "WritePI with invalid surrogate pair", Pri = 1)]
            public int pi_13()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteProcessingInstruction("pi", "\uD812");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CheckErrorState(w.WriteState);
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteNmToken")]
        public partial class TCWriteNmToken : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "Name = null", Param = "null", Pri = 1)]
            //[Variation(id = 2, Desc = "Name = String.Empty", Param = "String.Empty", Pri = 1)]
            public int writeNmToken_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        string temp;
                        if (CurVariation.Param.ToString() == "null")
                            temp = null;
                        else
                            temp = String.Empty;
                        w.WriteNmToken(temp);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CheckElementState(w.WriteState);//by design 396962 
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "Sanity test, Name = foo", Pri = 1)]
            public int writeNmToken_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteNmToken("foo");
                    w.WriteEndElement();
                }
                return CompareReader("<root>foo</root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "Name contains letters, digits, . _ - : chars", Pri = 1)]
            public int writeNmToken_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteNmToken("_foo:1234.bar-");
                    w.WriteEndElement();
                }
                return CompareReader("<root>_foo:1234.bar-</root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "Name contains whitespace char", Param = "test test", Pri = 1)]
            //[Variation(id = 5, Desc = "Name contains ? char", Param = "test?", Pri = 1)]
            //[Variation(id = 6, Desc = "Name contains SQ", Param = "test'", Pri = 1)]
            //[Variation(id = 7, Desc = "Name contains DQ", Param = "\"test", Pri = 1)]
            public int writeNmToken_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteNmToken(CurVariation.Param.ToString());
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CheckElementState(w.WriteState);
                        return TEST_PASS;
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CheckElementState(w.WriteState);
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteName")]
        public partial class TCWriteName : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "Name = null", Param = "null", Pri = 1)]
            //[Variation(id = 2, Desc = "Name = String.Empty", Param = "String.Empty", Pri = 1)]
            public int writeName_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        string temp;
                        if (CurVariation.Param.ToString() == "null")
                            temp = null;
                        else
                            temp = String.Empty;
                        w.WriteName(temp);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CheckElementState(w.WriteState);
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "Sanity test, Name = foo", Pri = 1)]
            public int writeName_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteName("foo");
                    w.WriteEndElement();
                }
                return CompareReader("<root>foo</root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "Sanity test, Name = foo:bar", Pri = 1)]
            public int writeName_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteName("foo:bar");
                    w.WriteEndElement();
                }
                return CompareReader("<root>foo:bar</root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "Name starts with :", Param = ":bar", Pri = 1)]
            //[Variation(id = 5, Desc = "Name contains whitespace char", Param = "foo bar", Pri = 1)]
            public int writeName_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteName(CurVariation.Param.ToString());
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CheckElementState(w.WriteState);
                        return TEST_PASS;
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CheckElementState(w.WriteState);
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteQualifiedName")]
        public partial class TCWriteQName : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "Name = null", Param = "null", Pri = 1)]
            //[Variation(id = 2, Desc = "Name = String.Empty", Param = "String.Empty", Pri = 1)]
            public int writeQName_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteAttributeString("xmlns", "foo", null, "test");
                        string temp;
                        if (CurVariation.Param.ToString() == "null")
                            temp = null;
                        else
                            temp = String.Empty;
                        w.WriteQualifiedName(temp, "test");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                    catch (NullReferenceException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return (WriterType == WriterType.CustomWriter) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "WriteQName with correct NS", Pri = 1)]
            public int writeQName_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteAttributeString("xmlns", "foo", null, "test");
                    w.WriteQualifiedName("bar", "test");
                    w.WriteEndElement();
                }
                return CompareReader("<root xmlns:foo=\"test\">foo:bar</root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "WriteQName when NS is auto-generated", Pri = 1)]
            public int writeQName_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("foo", "root", "test");
                    w.WriteQualifiedName("bar", "test");
                    w.WriteEndElement();
                }
                return CompareReader("<foo:root xmlns:foo=\"test\">foo:bar</foo:root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 5, Desc = "QName = foo:bar when foo is not in scope", Pri = 1)]
            public int writeQName_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteQualifiedName("bar", "foo");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        if (WriterType == WriterType.CustomWriter)
                        {
                            CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                        }
                        else
                        {
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        }
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 6, Desc = "Name starts with :", Param = ":bar", Pri = 1)]
            //[Variation(id = 7, Desc = "Name contains whitespace char", Param = "foo bar", Pri = 1)]
            public int writeQName_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteAttributeString("xmlns", "foo", null, "test");
                        w.WriteQualifiedName(CurVariation.Param.ToString(), "test");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return (WriterType == WriterType.CustomWriter) ? TEST_PASS : TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteChars")]
        public partial class TCWriteChars : TCWriteBuffer
        {
            //[Variation(id = 1, Desc = "WriteChars with valid buffer, number, count", Pri = 0)]
            public int writeChars_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    string s = "test the buffer";
                    char[] buf = s.ToCharArray();
                    w.WriteStartElement("Root");
                    w.WriteChars(buf, 0, 4);
                    w.WriteEndElement();
                }
                return CompareReader("<Root>test</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "WriteChars with & < >", Pri = 1)]
            public int writeChars_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    string s = "&<>theend";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("Root");
                    w.WriteChars(buf, 0, 5);
                    w.WriteEndElement();
                }
                return CompareReader("<Root>&amp;&lt;&gt;th</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "WriteChars following WriteStartAttribute", Pri = 1)]
            public int writeChars_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    string s = "valid";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteChars(buf, 0, 5);
                    w.WriteEndElement();
                }
                return CompareReader("<Root a=\"valid\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "WriteChars with entity ref included", Pri = 1)]
            public int writeChars_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    string s = "this is an entity &foo;";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("Root");
                    w.WriteChars(buf, 0, buf.Length);
                    w.WriteEndElement();
                }
                return CompareReader("<Root>this is an entity &amp;foo;</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 5, Desc = "WriteChars with buffer = null", Pri = 2)]
            public int writeChars_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteChars(null, 0, 0);
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, WriteState.Element, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 6, Desc = "WriteChars with count > buffer size", Pri = 1)]
            public int writeChars_6()
            {
                return VerifyInvalidWrite("WriteChars", 5, 0, 6, typeof(ArgumentOutOfRangeException));
            }

            //[Variation(id = 7, Desc = "WriteChars with count < 0", Pri = 1)]
            public int writeChars_7()
            {
                return VerifyInvalidWrite("WriteChars", 5, 2, -1, typeof(ArgumentOutOfRangeException));
            }

            //[Variation(id = 8, Desc = "WriteChars with index > buffer size", Pri = 1)]
            public int writeChars_8()
            {
                return VerifyInvalidWrite("WriteChars", 5, 6, 1, typeof(ArgumentOutOfRangeException));
            }

            //[Variation(id = 9, Desc = "WriteChars with index < 0", Pri = 1)]
            public int writeChars_9()
            {
                return VerifyInvalidWrite("WriteChars", 5, -1, 1, typeof(ArgumentOutOfRangeException));
            }

            //[Variation(id = 10, Desc = "WriteChars with index + count exceeds buffer", Pri = 1)]
            public int writeChars_10()
            {
                return VerifyInvalidWrite("WriteChars", 5, 2, 5, typeof(ArgumentOutOfRangeException));
            }

            //[Variation(id = 11, Desc = "WriteChars for xml:lang attribute, index = count = 0", Pri = 1)]
            public int writeChars_11()
            {
                using (XmlWriter w = CreateWriter())
                {
                    string s = "en-us;";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteChars(buf, 0, 0);
                    w.WriteEndElement();
                }
                return CompareReader("<root xml:lang=\"\" />") ? TEST_PASS : TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteString")]
        public partial class TCWriteString : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "WriteString(null)", Pri = 0)]
            public int writeString_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString(null);
                    w.WriteEndElement();
                }
                return CompareReader("<Root />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "WriteString(String.Empty)", Pri = 1)]
            public int writeString_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString(String.Empty);
                    w.WriteEndElement();
                }
                return CompareReader("<Root></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "WriteString with valid surrogate pair", Pri = 1)]
            public int writeString_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString("\uD812\uDD12");
                    w.WriteEndElement();
                }
                return CompareReader("<Root>\uD812\uDD12</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "WriteString with invalid surrogate pair", Pri = 1)]
            public int writeString_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteString("\uD812");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CheckErrorState(w.WriteState);
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 5, Desc = "WriteString with entity reference", Pri = 1)]
            public int writeString_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString("&test;");
                    w.WriteEndElement();
                }
                return CompareReader("<Root>&amp;test;</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 6, Desc = "WriteString with single/double quote, &, <, >", Pri = 1)]
            public int writeString_6()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString("' & < > \"");
                    w.WriteEndElement();
                }
                return CompareReader("<Root>&apos; &amp; &lt; &gt; \"</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 9, Desc = "WriteString for value greater than x1F", Pri = 1)]
            public int writeString_9()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString(XmlConvert.ToString('\x21'));
                    w.WriteEndElement();
                }
                return CompareReader("<Root>!</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 10, Desc = "WriteString with CR, LF, CR LF inside element", Pri = 1)]
            public int writeString_10()
            {
                // By default NormalizeNewLines = false and NewLineChars = \r\n
                // So \r, \n or \r\n gets replaces by \r\n in element content
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartElement("ws1");
                    w.WriteString("\r");
                    w.WriteEndElement();
                    w.WriteStartElement("ws2");
                    w.WriteString("\n");
                    w.WriteEndElement();
                    w.WriteStartElement("ws3");
                    w.WriteString("\r\n");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return CompareBaseline("writeStringWhiespaceInElem.txt") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 11, Desc = "WriteString with CR, LF, CR LF inside attribute value", Pri = 1)]
            public int writeString_11()
            {
                // \r, \n and \r\n gets replaced by char entities &#xD; &#xA; and &#xD;&#xA; respectively

                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr1");
                    w.WriteString("\r");
                    w.WriteStartAttribute("attr2");
                    w.WriteString("\n");
                    w.WriteStartAttribute("attr3");
                    w.WriteString("\r\n");
                    w.WriteEndElement();
                }
                return CompareBaseline("writeStringWhiespaceInAttr.txt") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 12, Desc = "Call WriteString for LF inside attribute", Pri = 1)]
            public int writeString_12()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root", "");
                    w.WriteStartAttribute("a1", "");
                    w.WriteString("x\ny");
                    w.WriteEndElement();
                }
                return CompareReader("<Root a1=\"x&#xA;y\" />") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 13, Desc = "Surrogate charaters in text nodes, range limits", Pri = 1)]
            public int writeString_13()
            {
                char[] invalidXML = { '\uD800', '\uDC00', '\uD800', '\uDFFF', '\uDBFF', '\uDC00', '\uDBFF', '\uDFFF' };
                string invXML = new String(invalidXML);

                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString(invXML);
                    w.WriteEndElement();
                }
                return CompareReader("<Root>\uD800\uDC00\uD800\uDFFF\uDBFF\uDC00\uDBFF\uDFFF</Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 14, Desc = "High surrogate on last position", Pri = 1)]
            public int writeString_14()
            {
                char[] invalidXML = { 'a', 'b', '\uDA34' };
                string invXML = new String(invalidXML);

                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteString(invXML);
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CheckErrorState(w.WriteState);
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 15, Desc = "Low surrogate on first position", Pri = 1)]
            public int writeString_15()
            {
                char[] invalidXML = { '\uDF20', 'b', 'c' };
                string invXML = new String(invalidXML);

                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteString(invXML);
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CheckErrorState(w.WriteState);
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }

            //[Variation(id = 16, Desc = "Swap low-high surrogates", Pri = 1)]
            public int writeString_16()
            {
                char[] invalidXML = { 'a', '\uDE40', '\uDA72', 'c' };
                string invXML = new String(invalidXML);

                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteString(invXML);
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CheckErrorState(w.WriteState);
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteWhitespace")]
        public partial class TCWhiteSpace : XmlWriterTestCaseBase
        {
            //[Variation(id = 1, Desc = "WriteWhitespace with values #x20 #x9 #xD #xA", Pri = 1)]
            public int whitespace_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString("text");
                    w.WriteWhitespace("\x20");
                    w.WriteString("text");
                    w.WriteWhitespace("\x9");
                    w.WriteString("text");
                    w.WriteWhitespace("\xD");
                    w.WriteString("text");
                    w.WriteWhitespace("\xA");
                    w.WriteString("text");
                    w.WriteEndElement();
                }
                return CompareBaseline("whitespace1.txt") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 2, Desc = "WriteWhitespace in the middle of text", Pri = 1)]
            public int whitespace_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString("text");
                    w.WriteWhitespace("\xD");
                    w.WriteString("text");
                    w.WriteEndElement();
                }
                return CompareBaseline("whitespace2.txt") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 3, Desc = "WriteWhitespace before and after root element", Pri = 1)]
            public int whitespace_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartDocument();
                    w.WriteWhitespace("\x20");
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteWhitespace("\x20");
                    w.WriteEndDocument();
                }
                return CompareBaseline("whitespace3.txt") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 4, Desc = "WriteWhitespace with null ", Param = "null", Pri = 1)]
            //[Variation(id = 5, Desc = "WriteWhitespace with String.Empty ", Param = "String.Empty", Pri = 1)]
            public int whitespace_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    string temp;
                    if (CurVariation.Param.ToString() == "null")
                        temp = null;
                    else
                        temp = String.Empty;
                    w.WriteStartElement("Root");

                    w.WriteWhitespace(temp);
                    w.WriteEndElement();
                }
                return CompareReader("<Root></Root>") ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(id = 6, Desc = "WriteWhitespace with invalid char", Param = "a", Pri = 1)]
            //[Variation(id = 7, Desc = "WriteWhitespace with invalid char", Param = "\xE", Pri = 1)]
            //[Variation(id = 8, Desc = "WriteWhitespace with invalid char", Param = "\x0", Pri = 1)]
            //[Variation(id = 9, Desc = "WriteWhitespace with invalid char", Param = "\x10", Pri = 1)]
            //[Variation(id = 10, Desc = "WriteWhitespace with invalid char", Param = "\x1F", Pri = 1)]
            public int whitespace_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteWhitespace(CurVariation.Param.ToString());
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, (WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return TEST_PASS;
                    }
                }
                CError.WriteLine("Did not throw exception");
                return TEST_FAIL;
            }
        }

        //[TestCase(Name = "WriteValue")]
        public partial class TCWriteValue : XmlWriterTestCaseBase
        {
            //[Variation(Desc = "Write multiple atomic values inside element", Pri = 1)]
            public int writeValue_1()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteValue((int)2);
                    w.WriteValue((bool)true);
                    w.WriteValue((double)3.14);
                    w.WriteEndElement();
                }
                return (CompareReader("<Root>2true3.14</Root>")) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(Desc = "Write multiple atomic values inside attribute", Pri = 1)]
            public int writeValue_2()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteValue((int)2);
                    w.WriteValue((bool)true);
                    w.WriteValue((double)3.14);
                    w.WriteEndElement();
                }
                return (CompareReader("<Root attr=\"2true3.14\" />")) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(Desc = "Write multiple atomic values inside element, seperate by WriteWhitespace(' ')", Pri = 1)]
            public int writeValue_3()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteValue((int)2);
                    w.WriteWhitespace(" ");
                    w.WriteValue((bool)true);
                    w.WriteWhitespace(" ");
                    w.WriteValue((double)3.14);
                    w.WriteWhitespace(" ");
                    w.WriteEndElement();
                }
                return (CompareReader("<Root>2 true 3.14 </Root>")) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(Desc = "Write multiple atomic values inside element, seperate by WriteString(' ')", Pri = 1)]
            public int writeValue_4()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteValue((int)2);
                    w.WriteString(" ");
                    w.WriteValue((bool)true);
                    w.WriteString(" ");
                    w.WriteValue((double)3.14);
                    w.WriteString(" ");
                    w.WriteEndElement();
                }
                return (CompareReader("<Root>2 true 3.14 </Root>")) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(Desc = "Write multiple atomic values inside attribute, separate by WriteWhitespace(' ')", Pri = 1)]
            public int writeValue_5()
            {
                using (XmlWriter w = CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("attr");
                        w.WriteValue((int)2);
                        w.WriteWhitespace(" ");
                        w.WriteValue((bool)true);
                        w.WriteWhitespace(" ");
                        w.WriteValue((double)3.14);
                        w.WriteWhitespace(" ");
                        w.WriteEndElement();
                    }
                    catch (InvalidOperationException e)
                    {
                        CError.WriteLine(e);
                        return TEST_FAIL;
                    }
                }
                return (CompareReader("<Root attr=\"2 true 3.14 \" />")) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(Desc = "Write multiple atomic values inside attribute, seperate by WriteString(' ')", Pri = 1)]
            public int writeValue_6()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteValue((int)2);
                    w.WriteString(" ");
                    w.WriteValue((bool)true);
                    w.WriteString(" ");
                    w.WriteValue((double)3.14);
                    w.WriteString(" ");
                    w.WriteEndElement();
                }
                return (CompareReader("<Root attr=\"2 true 3.14 \" />")) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(Desc = "WriteValue(long)", Pri = 1)]
            public int writeValue_7()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteValue(long.MaxValue);
                    w.WriteStartElement("child");
                    w.WriteValue(long.MinValue);
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                return (CompareReader("<Root>9223372036854775807<child>-9223372036854775808</child></Root>")) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(Desc = "WriteValue((string)null)", Param = "string", Pri = 1)]
            //[Variation(Desc = "WriteValue((object)null)", Param = "object", Pri = 1)]
            public int writeValue_8()
            {
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("root");
                    switch ((string)CurVariation.Param)
                    {
                        case "string":
                            w.WriteValue((string)null);
                            return TEST_PASS;
                        case "object":
                            try
                            {
                                w.WriteValue((object)null);
                            }
                            catch (ArgumentNullException) { return TEST_PASS; }
                            break;
                    }
                    throw new CTestFailedException("Test failed.");
                }
            }

            public void VerifyValue(Type dest, object expVal, int param)
            {
                object actual;

                using (Stream fsr = FilePathUtil.getStream("writer.out"))
                {
                    using (XmlReader r = ReaderHelper.Create(fsr))
                    {
                        while (r.Read())
                        {
                            if (r.NodeType == XmlNodeType.Element)
                                break;
                        }
                        if (param == 1)
                        {
                            actual = (object)r.ReadElementContentAs(dest, null);
                            if (!actual.Equals(expVal)) CError.Compare(actual.ToString(), expVal.ToString(), "RECA");
                        }
                        else
                        {
                            r.MoveToAttribute("a");
                            actual = (object)r.ReadContentAs(dest, null);
                            if (!actual.Equals(expVal)) CError.Compare(actual.ToString(), expVal.ToString(), "RCA");
                        }
                    }
                }
            }

            public static Dictionary<string, Type> typeMapper;
            public static Dictionary<string, object> value;
            public static Dictionary<string, object> expValue;

            static TCWriteValue()
            {
                if (typeMapper == null)
                {
                    typeMapper = new Dictionary<string, Type>();
                    typeMapper.Add("UInt64", typeof(UInt64));
                    typeMapper.Add("UInt32", typeof(UInt32));
                    typeMapper.Add("UInt16", typeof(UInt16));
                    typeMapper.Add("Int64", typeof(Int64));
                    typeMapper.Add("Int32", typeof(Int32));
                    typeMapper.Add("Int16", typeof(Int16));
                    typeMapper.Add("Byte", typeof(Byte));
                    typeMapper.Add("SByte", typeof(SByte));
                    typeMapper.Add("Decimal", typeof(Decimal));
                    typeMapper.Add("Single", typeof(Single));
                    typeMapper.Add("float", typeof(float));
                    typeMapper.Add("object", typeof(object));
                    typeMapper.Add("bool", typeof(bool));
                    typeMapper.Add("DateTime", typeof(DateTime));
                    typeMapper.Add("DateTimeOffset", typeof(DateTimeOffset));
                    typeMapper.Add("ByteArray", typeof(byte[]));
                    typeMapper.Add("BoolArray", typeof(bool[]));
                    typeMapper.Add("ObjectArray", typeof(Object[]));
                    typeMapper.Add("DecimalArray", typeof(Decimal[]));
                    typeMapper.Add("DoubleArray", typeof(Double[]));
                    typeMapper.Add("DateTimeArray", typeof(DateTime[]));
                    typeMapper.Add("DateTimeOffsetArray", typeof(DateTimeOffset[]));
                    typeMapper.Add("Int16Array", typeof(Int16[]));
                    typeMapper.Add("Int32Array", typeof(Int32[]));
                    typeMapper.Add("Int64Array", typeof(Int64[]));
                    typeMapper.Add("SByteArray", typeof(SByte[]));
                    typeMapper.Add("SingleArray", typeof(Single[]));
                    typeMapper.Add("StringArray", typeof(string[]));
                    typeMapper.Add("TimeSpanArray", typeof(TimeSpan[]));
                    typeMapper.Add("UInt16Array", typeof(UInt16[]));
                    typeMapper.Add("UInt32Array", typeof(UInt32[]));
                    typeMapper.Add("UInt64Array", typeof(UInt64[]));
                    typeMapper.Add("UriArray", typeof(Uri[]));
                    typeMapper.Add("XmlQualifiedNameArray", typeof(XmlQualifiedName[]));
                    typeMapper.Add("List", typeof(List<string>));
                    typeMapper.Add("TimeSpan", typeof(TimeSpan));
                    typeMapper.Add("Double", typeof(Double));
                    typeMapper.Add("Uri", typeof(Uri));
                    typeMapper.Add("XmlQualifiedName", typeof(XmlQualifiedName));
                    typeMapper.Add("Char", typeof(Char));
                    typeMapper.Add("string", typeof(string));
                }
                if (value == null)
                {
                    value = new Dictionary<string, object>();
                    value.Add("UInt64", UInt64.MaxValue);
                    value.Add("UInt32", UInt32.MaxValue);
                    value.Add("UInt16", UInt16.MaxValue);
                    value.Add("Int64", Int64.MaxValue);
                    value.Add("Int32", Int32.MaxValue);
                    value.Add("Int16", Int16.MaxValue);
                    value.Add("Byte", Byte.MaxValue);
                    value.Add("SByte", SByte.MaxValue);
                    value.Add("Decimal", Decimal.MaxValue);
                    value.Add("Single", -4582.24);
                    value.Add("float", -4582.24F);
                    value.Add("object", 0);
                    value.Add("bool", false);
                    value.Add("DateTime", new DateTime(2002, 1, 3, 21, 59, 59, 59));
                    value.Add("DateTimeOffset", new DateTimeOffset(2002, 1, 3, 21, 59, 59, 59, TimeSpan.FromHours(0)));
                    value.Add("ByteArray", new byte[] { 0xd8, 0x7e });
                    value.Add("BoolArray", new bool[] { true, false });
                    value.Add("ObjectArray", new Object[] { 0, 1 });
                    value.Add("DecimalArray", new Decimal[] { 0, 1 });
                    value.Add("DoubleArray", new Double[] { 0, 1 });
                    value.Add("DateTimeArray", new DateTime[] { new DateTime(2002, 12, 30), new DateTime(2, 1, 3, 23, 59, 59, 59) });
                    value.Add("DateTimeOffsetArray", new DateTimeOffset[] { new DateTimeOffset(2002, 12, 30, 0, 0, 0, TimeSpan.FromHours(-8.0)), new DateTimeOffset(2, 1, 3, 23, 59, 59, 59, TimeSpan.FromHours(0)) });
                    value.Add("Int16Array", new Int16[] { 0, 1 });
                    value.Add("Int32Array", new Int32[] { 0, 1 });
                    value.Add("Int64Array", new Int64[] { 0, 1 });
                    value.Add("SByteArray", new SByte[] { 0, 1 });
                    value.Add("SingleArray", new Single[] { 0, 1 });
                    value.Add("StringArray", new string[] { "0", "1" });
                    value.Add("TimeSpanArray", new TimeSpan[] { TimeSpan.MinValue, TimeSpan.MaxValue });
                    value.Add("UInt16Array", new UInt16[] { 0, 1 });
                    value.Add("UInt32Array", new UInt32[] { 0, 1 });
                    value.Add("UInt64Array", new UInt64[] { 0, 1 });
                    value.Add("UriArray", new Uri[] { new Uri("http://wddata", UriKind.Absolute), new Uri("http://webxtest") });
                    value.Add("XmlQualifiedNameArray", new XmlQualifiedName[] { new XmlQualifiedName("a"), new XmlQualifiedName("b", null) });
                    value.Add("List", new List<Guid>[] { });
                    value.Add("TimeSpan", new TimeSpan());
                    value.Add("Double", Double.MaxValue);
                    value.Add("Uri", "http");
                    value.Add("XmlQualifiedName", new XmlQualifiedName("a", null));
                    value.Add("Char", Char.MaxValue);
                    value.Add("string", "123");
                }
            }
            private object[] _dates = new object[]
            {
                new DateTimeOffset(2002,1,3,21,59,59,59, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002,1,3))),
                "2002-01-03T21:59:59.059",
                XmlConvert.ToString(new DateTimeOffset(2002,1,3,21,59,59,59, TimeSpan.FromHours(0)))
            };

            //[Variation(Desc = "elem.WriteValue(UInt64ToString)", Params = new object[] { 1, "UInt64", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToString)", Params = new object[] { 1, "UInt32", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToString)", Params = new object[] { 1, "UInt16", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToString)", Params = new object[] { 1, "Int64", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToString)", Params = new object[] { 1, "Int32", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToString)", Params = new object[] { 1, "Int16", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToString)", Params = new object[] { 1, "Byte", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToString)", Params = new object[] { 1, "SByte", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToString)", Params = new object[] { 1, "Decimal", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(floatToString)", Params = new object[] { 1, "float", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(objectToString)", Params = new object[] { 1, "object", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToString)", Params = new object[] { 1, "bool", "string", true, "false" })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToString)", Params = new object[] { 1, "DateTime", "string", true, 1 })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToString)", Params = new object[] { 1, "DateTimeOffset", "string", true, 2 })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToString)", Params = new object[] { 1, "ByteArray", "string", true, "2H4=" })]
            //[Variation(Desc = "elem.WriteValue(ListToString)", Params = new object[] { 1, "List", "string", true, "" })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToString)", Params = new object[] { 1, "TimeSpan", "string", true, "PT0S" })]
            //[Variation(Desc = "elem.WriteValue(UriToString)", Params = new object[] { 1, "Uri", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToString)", Params = new object[] { 1, "Double", "string", true, "1.7976931348623157E+308" })]
            //[Variation(Desc = "elem.WriteValue(SingleToString)", Params = new object[] { 1, "Single", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToString)", Params = new object[] { 1, "XmlQualifiedName", "string", true, null })]
            //[Variation(Desc = "elem.WriteValue(stringToString)", Params = new object[] { 1, "string", "string", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToUInt64)", Params = new object[] { 1, "UInt64", "UInt64", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToUInt64)", Params = new object[] { 1, "UInt32", "UInt64", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToUInt64)", Params = new object[] { 1, "UInt16", "UInt64", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToUInt64)", Params = new object[] { 1, "Int64", "UInt64", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToUInt64)", Params = new object[] { 1, "Int32", "UInt64", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToUInt64)", Params = new object[] { 1, "Int16", "UInt64", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToUInt64)", Params = new object[] { 1, "Byte", "UInt64", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToUInt64)", Params = new object[] { 1, "SByte", "UInt64", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToUInt64)", Params = new object[] { 1, "Decimal", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToUInt64)", Params = new object[] { 1, "float", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToUInt64)", Params = new object[] { 1, "object", "UInt64", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToUInt64)", Params = new object[] { 1, "bool", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToUInt64)", Params = new object[] { 1, "DateTime", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToUInt64)", Params = new object[] { 1, "DateTimeOffset", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToUInt64)", Params = new object[] { 1, "ByteArray", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToUInt64)", Params = new object[] { 1, "List", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToUInt64)", Params = new object[] { 1, "TimeSpan", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToUInt64)", Params = new object[] { 1, "Uri", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToUInt64)", Params = new object[] { 1, "Double", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToUInt64)", Params = new object[] { 1, "Single", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToUInt64)", Params = new object[] { 1, "XmlQualifiedName", "UInt64", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToUInt64)", Params = new object[] { 1, "string", "UInt64", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToInt64)", Params = new object[] { 1, "UInt64", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToInt64)", Params = new object[] { 1, "UInt32", "Int64", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToInt64)", Params = new object[] { 1, "UInt16", "Int64", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToInt64)", Params = new object[] { 1, "Int64", "Int64", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToInt64)", Params = new object[] { 1, "Int32", "Int64", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToInt64)", Params = new object[] { 1, "Int16", "Int64", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToInt64)", Params = new object[] { 1, "Byte", "Int64", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToInt64)", Params = new object[] { 1, "SByte", "Int64", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToInt64)", Params = new object[] { 1, "Decimal", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToInt64)", Params = new object[] { 1, "float", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToInt64)", Params = new object[] { 1, "object", "Int64", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToInt64)", Params = new object[] { 1, "bool", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToInt64)", Params = new object[] { 1, "DateTime", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToInt64)", Params = new object[] { 1, "DateTimeOffset", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToInt64)", Params = new object[] { 1, "ByteArray", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToInt64)", Params = new object[] { 1, "List", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToInt64)", Params = new object[] { 1, "TimeSpan", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToInt64)", Params = new object[] { 1, "Uri", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToInt64)", Params = new object[] { 1, "Double", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToInt64)", Params = new object[] { 1, "Single", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToInt64)", Params = new object[] { 1, "XmlQualifiedName", "Int64", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToInt64)", Params = new object[] { 1, "string", "Int64", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToUInt32)", Params = new object[] { 1, "UInt64", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToUInt32)", Params = new object[] { 1, "UInt32", "UInt32", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToUInt32)", Params = new object[] { 1, "UInt16", "UInt32", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToUInt32)", Params = new object[] { 1, "Int64", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToUInt32)", Params = new object[] { 1, "Int32", "UInt32", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToUInt32)", Params = new object[] { 1, "Int16", "UInt32", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToUInt32)", Params = new object[] { 1, "Byte", "UInt32", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToUInt32)", Params = new object[] { 1, "SByte", "UInt32", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToUInt32)", Params = new object[] { 1, "Decimal", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToUInt32)", Params = new object[] { 1, "float", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToUInt32)", Params = new object[] { 1, "object", "UInt32", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToUInt32)", Params = new object[] { 1, "bool", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToUInt32)", Params = new object[] { 1, "DateTime", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToUInt32)", Params = new object[] { 1, "DateTimeOffset", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToUInt32)", Params = new object[] { 1, "ByteArray", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToUInt32)", Params = new object[] { 1, "List", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToUInt32)", Params = new object[] { 1, "TimeSpan", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToUInt32)", Params = new object[] { 1, "Uri", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToUInt32)", Params = new object[] { 1, "Double", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToUInt32)", Params = new object[] { 1, "Single", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToUInt32)", Params = new object[] { 1, "XmlQualifiedName", "UInt32", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToUInt32)", Params = new object[] { 1, "string", "UInt32", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToInt32)", Params = new object[] { 1, "UInt64", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToInt32)", Params = new object[] { 1, "UInt32", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToInt32)", Params = new object[] { 1, "UInt16", "Int32", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToInt32)", Params = new object[] { 1, "Int64", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToInt32)", Params = new object[] { 1, "Int32", "Int32", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToInt32)", Params = new object[] { 1, "Int16", "Int32", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToInt32)", Params = new object[] { 1, "Byte", "Int32", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToInt32)", Params = new object[] { 1, "SByte", "Int32", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToInt32)", Params = new object[] { 1, "Decimal", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToInt32)", Params = new object[] { 1, "float", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToInt32)", Params = new object[] { 1, "object", "Int32", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToInt32)", Params = new object[] { 1, "bool", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToInt32)", Params = new object[] { 1, "DateTime", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToInt32)", Params = new object[] { 1, "DateTimeOffset", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToInt32)", Params = new object[] { 1, "ByteArray", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToInt32)", Params = new object[] { 1, "List", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToInt32)", Params = new object[] { 1, "TimeSpan", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToInt32)", Params = new object[] { 1, "Uri", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToInt32)", Params = new object[] { 1, "Double", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToInt32)", Params = new object[] { 1, "Single", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToInt32)", Params = new object[] { 1, "XmlQualifiedName", "Int32", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToInt32)", Params = new object[] { 1, "string", "Int32", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToUInt16)", Params = new object[] { 1, "UInt64", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToUInt16)", Params = new object[] { 1, "UInt32", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToUInt16)", Params = new object[] { 1, "UInt16", "UInt16", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToUInt16)", Params = new object[] { 1, "Int64", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToUInt16)", Params = new object[] { 1, "Int32", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToUInt16)", Params = new object[] { 1, "Int16", "UInt16", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToUInt16)", Params = new object[] { 1, "Byte", "UInt16", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToUInt16)", Params = new object[] { 1, "SByte", "UInt16", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToUInt16)", Params = new object[] { 1, "Decimal", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToUInt16)", Params = new object[] { 1, "float", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToUInt16)", Params = new object[] { 1, "object", "UInt16", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToUInt16)", Params = new object[] { 1, "bool", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToUInt16)", Params = new object[] { 1, "DateTime", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToUInt16)", Params = new object[] { 1, "DateTimeOffset", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToUInt16)", Params = new object[] { 1, "ByteArray", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToUInt16)", Params = new object[] { 1, "List", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToUInt16)", Params = new object[] { 1, "TimeSpan", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToUInt16)", Params = new object[] { 1, "Uri", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToUInt16)", Params = new object[] { 1, "Double", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToUInt16)", Params = new object[] { 1, "Single", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToUInt16)", Params = new object[] { 1, "XmlQualifiedName", "UInt16", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToUInt16)", Params = new object[] { 1, "string", "UInt16", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToInt16)", Params = new object[] { 1, "UInt64", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToInt16)", Params = new object[] { 1, "UInt32", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToInt16)", Params = new object[] { 1, "UInt16", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToInt16)", Params = new object[] { 1, "Int64", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToInt16)", Params = new object[] { 1, "Int32", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToInt16)", Params = new object[] { 1, "Int16", "Int16", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToInt16)", Params = new object[] { 1, "Byte", "Int16", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToInt16)", Params = new object[] { 1, "SByte", "Int16", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToInt16)", Params = new object[] { 1, "Decimal", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToInt16)", Params = new object[] { 1, "float", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToInt16)", Params = new object[] { 1, "object", "Int16", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToInt16)", Params = new object[] { 1, "bool", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToInt16)", Params = new object[] { 1, "DateTime", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToInt16)", Params = new object[] { 1, "DateTimeOffset", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToInt16)", Params = new object[] { 1, "ByteArray", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToInt16)", Params = new object[] { 1, "List", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToInt16)", Params = new object[] { 1, "TimeSpan", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToInt16)", Params = new object[] { 1, "Uri", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToInt16)", Params = new object[] { 1, "Double", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToInt16)", Params = new object[] { 1, "Single", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToInt16)", Params = new object[] { 1, "XmlQualifiedName", "Int16", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToInt16)", Params = new object[] { 1, "string", "Int16", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToByte)", Params = new object[] { 1, "UInt64", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToByte)", Params = new object[] { 1, "UInt32", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToByte)", Params = new object[] { 1, "UInt16", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToByte)", Params = new object[] { 1, "Int64", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToByte)", Params = new object[] { 1, "Int32", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToByte)", Params = new object[] { 1, "Int16", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToByte)", Params = new object[] { 1, "Byte", "Byte", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToByte)", Params = new object[] { 1, "SByte", "Byte", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToByte)", Params = new object[] { 1, "Decimal", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToByte)", Params = new object[] { 1, "float", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToByte)", Params = new object[] { 1, "object", "Byte", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToByte)", Params = new object[] { 1, "bool", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToByte)", Params = new object[] { 1, "DateTime", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToByte)", Params = new object[] { 1, "DateTimeOffset", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToByte)", Params = new object[] { 1, "ByteArray", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToByte)", Params = new object[] { 1, "List", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToByte)", Params = new object[] { 1, "TimeSpan", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToByte)", Params = new object[] { 1, "Uri", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToByte)", Params = new object[] { 1, "Double", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToByte)", Params = new object[] { 1, "Single", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToByte)", Params = new object[] { 1, "XmlQualifiedName", "Byte", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToByte)", Params = new object[] { 1, "string", "Byte", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToSByte)", Params = new object[] { 1, "UInt64", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToSByte)", Params = new object[] { 1, "UInt32", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToSByte)", Params = new object[] { 1, "UInt16", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToSByte)", Params = new object[] { 1, "Int64", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToSByte)", Params = new object[] { 1, "Int32", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToSByte)", Params = new object[] { 1, "Int16", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToSByte)", Params = new object[] { 1, "Byte", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToSByte)", Params = new object[] { 1, "SByte", "SByte", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToSByte)", Params = new object[] { 1, "Decimal", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToSByte)", Params = new object[] { 1, "float", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToSByte)", Params = new object[] { 1, "object", "SByte", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToSByte)", Params = new object[] { 1, "bool", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToSByte)", Params = new object[] { 1, "DateTime", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToSByte)", Params = new object[] { 1, "DateTimeOffset", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToSByte)", Params = new object[] { 1, "ByteArray", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToSByte)", Params = new object[] { 1, "List", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToSByte)", Params = new object[] { 1, "TimeSpan", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToSByte)", Params = new object[] { 1, "Uri", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToSByte)", Params = new object[] { 1, "Double", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToSByte)", Params = new object[] { 1, "Single", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToSByte)", Params = new object[] { 1, "XmlQualifiedName", "SByte", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToSByte)", Params = new object[] { 1, "string", "SByte", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToDecimal)", Params = new object[] { 1, "UInt64", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToDecimal)", Params = new object[] { 1, "UInt32", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToDecimal)", Params = new object[] { 1, "UInt16", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToDecimal)", Params = new object[] { 1, "Int64", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToDecimal)", Params = new object[] { 1, "Int32", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToDecimal)", Params = new object[] { 1, "Int16", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToDecimal)", Params = new object[] { 1, "Byte", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToDecimal)", Params = new object[] { 1, "SByte", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToDecimal)", Params = new object[] { 1, "Decimal", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(floatToDecimal)", Params = new object[] { 1, "float", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(objectToDecimal)", Params = new object[] { 1, "object", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToDecimal)", Params = new object[] { 1, "bool", "Decimal", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToDecimal)", Params = new object[] { 1, "DateTime", "Decimal", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToDecimal)", Params = new object[] { 1, "DateTimeOffset", "Decimal", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToDecimal)", Params = new object[] { 1, "ByteArray", "Decimal", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToDecimal)", Params = new object[] { 1, "List", "Decimal", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToDecimal)", Params = new object[] { 1, "TimeSpan", "Decimal", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToDecimal)", Params = new object[] { 1, "Uri", "Decimal", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToDecimal)", Params = new object[] { 1, "Double", "Decimal", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToDecimal)", Params = new object[] { 1, "Single", "Decimal", true, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToDecimal)", Params = new object[] { 1, "XmlQualifiedName", "Decimal", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToDecimal)", Params = new object[] { 1, "string", "Decimal", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToFloat)", Params = new object[] { 1, "UInt64", "float", true, 1.844674E+19F })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToFloat)", Params = new object[] { 1, "UInt32", "float", true, 4.294967E+09F })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToFloat)", Params = new object[] { 1, "UInt16", "float", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToFloat)", Params = new object[] { 1, "Int64", "float", true, 9.223372E+18F })]
            //[Variation(Desc = "elem.WriteValue(Int32ToFloat)", Params = new object[] { 1, "Int32", "float", true, 2.147484E+09F })]
            //[Variation(Desc = "elem.WriteValue(Int16ToFloat)", Params = new object[] { 1, "Int16", "float", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToFloat)", Params = new object[] { 1, "Byte", "float", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToFloat)", Params = new object[] { 1, "SByte", "float", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToFloat)", Params = new object[] { 1, "Decimal", "float", true, 7.922816E+28F })]
            //[Variation(Desc = "elem.WriteValue(floatToFloat)", Params = new object[] { 1, "float", "float", true, null })]
            //[Variation(Desc = "elem.WriteValue(objectToFloat)", Params = new object[] { 1, "object", "float", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToFloat)", Params = new object[] { 1, "bool", "float", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToFloat)", Params = new object[] { 1, "DateTime", "float", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToFloat)", Params = new object[] { 1, "DateTimeOffset", "float", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToFloat)", Params = new object[] { 1, "ByteArray", "float", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToFloat)", Params = new object[] { 1, "List", "float", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToFloat)", Params = new object[] { 1, "TimeSpan", "float", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToFloat)", Params = new object[] { 1, "Uri", "float", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToFloat)", Params = new object[] { 1, "Double", "float", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleTofloat)", Params = new object[] { 1, "Single", "float", true, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToFloat)", Params = new object[] { 1, "XmlQualifiedName", "float", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToFloat)", Params = new object[] { 1, "string", "float", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToBool)", Params = new object[] { 1, "UInt64", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToBool)", Params = new object[] { 1, "UInt32", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToBool)", Params = new object[] { 1, "UInt16", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToBool)", Params = new object[] { 1, "Int64", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToBool)", Params = new object[] { 1, "Int32", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToBool)", Params = new object[] { 1, "Int16", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToBool)", Params = new object[] { 1, "Byte", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToBool)", Params = new object[] { 1, "SByte", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToBool)", Params = new object[] { 1, "Decimal", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToBool)", Params = new object[] { 1, "float", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToBool)", Params = new object[] { 1, "object", "bool", true, false })]
            //[Variation(Desc = "elem.WriteValue(boolToBool)", Params = new object[] { 1, "bool", "bool", true, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToBool)", Params = new object[] { 1, "DateTime", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToBool)", Params = new object[] { 1, "DateTimeOffset", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToBool)", Params = new object[] { 1, "ByteArray", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToBool)", Params = new object[] { 1, "List", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToBool)", Params = new object[] { 1, "TimeSpan", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToBool)", Params = new object[] { 1, "Uri", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToBool)", Params = new object[] { 1, "Double", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleTobool)", Params = new object[] { 1, "Single", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToBool)", Params = new object[] { 1, "XmlQualifiedName", "bool", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToBool)", Params = new object[] { 1, "string", "bool", false, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToDateTime)", Params = new object[] { 1, "UInt64", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToDateTime)", Params = new object[] { 1, "UInt32", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToDateTime)", Params = new object[] { 1, "UInt16", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToDateTime)", Params = new object[] { 1, "Int64", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToDateTime)", Params = new object[] { 1, "Int32", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToDateTime)", Params = new object[] { 1, "Int16", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToDateTime)", Params = new object[] { 1, "Byte", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToDateTime)", Params = new object[] { 1, "SByte", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToDateTime)", Params = new object[] { 1, "Decimal", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToDateTime)", Params = new object[] { 1, "float", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToDateTime)", Params = new object[] { 1, "object", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(boolToDateTime)", Params = new object[] { 1, "bool", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToDateTime)", Params = new object[] { 1, "DateTime", "DateTime", true, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToDateTime)", Params = new object[] { 1, "DateTimeOffset", "DateTime", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToDateTime)", Params = new object[] { 1, "ByteArray", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToDateTime)", Params = new object[] { 1, "List", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToDateTime)", Params = new object[] { 1, "TimeSpan", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToDateTime)", Params = new object[] { 1, "Uri", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToDateTime)", Params = new object[] { 1, "Double", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToDateTime)", Params = new object[] { 1, "Single", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToDateTime)", Params = new object[] { 1, "XmlQualifiedName", "DateTime", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToDateTime)", Params = new object[] { 1, "string", "DateTime", false, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToDateTimeOffset)", Params = new object[] { 1, "UInt64", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToDateTimeOffset)", Params = new object[] { 1, "UInt32", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToDateTimeOffset)", Params = new object[] { 1, "UInt16", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToDateTimeOffset)", Params = new object[] { 1, "Int64", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToDateTimeOffset)", Params = new object[] { 1, "Int32", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToDateTimeOffset)", Params = new object[] { 1, "Int16", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToDateTimeOffset)", Params = new object[] { 1, "Byte", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToDateTimeOffset)", Params = new object[] { 1, "SByte", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToDateTimeOffset)", Params = new object[] { 1, "Decimal", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToDateTimeOffset)", Params = new object[] { 1, "float", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToDateTimeOffset)", Params = new object[] { 1, "object", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(boolToDateTimeOffset)", Params = new object[] { 1, "bool", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToDateTimeOffset)", Params = new object[] { 1, "DateTime", "DateTimeOffset", true, 0 })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToDateTimeOffset)", Params = new object[] { 1, "DateTimeOffset", "DateTimeOffset", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToDateTimeOffset)", Params = new object[] { 1, "ByteArray", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToDateTimeOffset)", Params = new object[] { 1, "List", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToDateTimeOffset)", Params = new object[] { 1, "TimeSpan", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToDateTimeOffset)", Params = new object[] { 1, "Uri", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToDateTimeOffset)", Params = new object[] { 1, "Double", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToDateTimeOffset)", Params = new object[] { 1, "Single", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToDateTimeOffset)", Params = new object[] { 1, "XmlQualifiedName", "DateTimeOffset", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToDateTimeOffset)", Params = new object[] { 1, "string", "DateTimeOffset", false, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToList)", Params = new object[] { 1, "UInt64", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToList)", Params = new object[] { 1, "UInt32", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToList)", Params = new object[] { 1, "UInt16", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToList)", Params = new object[] { 1, "Int64", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToList)", Params = new object[] { 1, "Int32", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToList)", Params = new object[] { 1, "Int16", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToList)", Params = new object[] { 1, "Byte", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToList)", Params = new object[] { 1, "SByte", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToList)", Params = new object[] { 1, "Decimal", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(floatToList)", Params = new object[] { 1, "float", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(objectToList)", Params = new object[] { 1, "object", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(boolToList)", Params = new object[] { 1, "bool", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToList)", Params = new object[] { 1, "DateTime", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToList)", Params = new object[] { 1, "DateTimeOffset", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToList)", Params = new object[] { 1, "ByteArray", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToList)", Params = new object[] { 1, "List", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToList)", Params = new object[] { 1, "TimeSpan", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToList)", Params = new object[] { 1, "Uri", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToList)", Params = new object[] { 1, "Double", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToList)", Params = new object[] { 1, "Single", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToList)", Params = new object[] { 1, "XmlQualifiedName", "List", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToList)", Params = new object[] { 1, "string", "List", false, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToUri)", Params = new object[] { 1, "UInt64", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToUri)", Params = new object[] { 1, "UInt32", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToUri)", Params = new object[] { 1, "UInt16", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToUri)", Params = new object[] { 1, "Int64", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToUri)", Params = new object[] { 1, "Int32", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToUri)", Params = new object[] { 1, "Int16", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToUri)", Params = new object[] { 1, "Byte", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToUri)", Params = new object[] { 1, "SByte", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToUri)", Params = new object[] { 1, "Decimal", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(floatToUri)", Params = new object[] { 1, "float", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(objectToUri)", Params = new object[] { 1, "object", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToUri)", Params = new object[] { 1, "bool", "Uri", true, "false" })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToUri)", Params = new object[] { 1, "DateTime", "Uri", true, 1 })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToUri)", Params = new object[] { 1, "DateTimeOffset", "Uri", true, 2 })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToUri)", Params = new object[] { 1, "ByteArray", "Uri", true, "2H4=" })]
            //[Variation(Desc = "elem.WriteValue(ListToUri)", Params = new object[] { 1, "List", "Uri", true, "" })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToUri)", Params = new object[] { 1, "TimeSpan", "Uri", true, "PT0S" })]
            //[Variation(Desc = "elem.WriteValue(UriToUri)", Params = new object[] { 1, "Uri", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToUri)", Params = new object[] { 1, "Double", "Uri", true, "1.7976931348623157E+308" })]
            //[Variation(Desc = "elem.WriteValue(SingleToUri)", Params = new object[] { 1, "Single", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToUri)", Params = new object[] { 1, "XmlQualifiedName", "Uri", true, null })]
            //[Variation(Desc = "elem.WriteValue(stringToUri)", Params = new object[] { 1, "string", "Uri", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToDouble)", Params = new object[] { 1, "UInt64", "Double", true, 1.84467440737096E+19D })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToDouble)", Params = new object[] { 1, "UInt32", "Double", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToDouble)", Params = new object[] { 1, "UInt16", "Double", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToDouble)", Params = new object[] { 1, "Int64", "Double", true, 9.22337203685478E+18D })]
            //[Variation(Desc = "elem.WriteValue(Int32ToDouble)", Params = new object[] { 1, "Int32", "Double", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToDouble)", Params = new object[] { 1, "Int16", "Double", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToDouble)", Params = new object[] { 1, "Byte", "Double", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToDouble)", Params = new object[] { 1, "SByte", "Double", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToDouble)", Params = new object[] { 1, "Decimal", "Double", true, 7.92281625142643E+28D })]
            //[Variation(Desc = "elem.WriteValue(floatToDouble)", Params = new object[] { 1, "float", "Double", true, null })]
            //[Variation(Desc = "elem.WriteValue(objectToDouble)", Params = new object[] { 1, "object", "Double", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToDouble)", Params = new object[] { 1, "bool", "Double", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToDouble)", Params = new object[] { 1, "DateTime", "Double", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToDouble)", Params = new object[] { 1, "DateTimeOffset", "Double", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToDouble)", Params = new object[] { 1, "ByteArray", "Double", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToDouble)", Params = new object[] { 1, "List", "Double", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToDouble)", Params = new object[] { 1, "TimeSpan", "Double", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToDouble)", Params = new object[] { 1, "Uri", "Double", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToDouble)", Params = new object[] { 1, "Double", "Double", true, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToDouble)", Params = new object[] { 1, "Single", "Double", true, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToDouble)", Params = new object[] { 1, "XmlQualifiedName", "Double", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToDouble)", Params = new object[] { 1, "string", "Double", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToSingle)", Params = new object[] { 1, "UInt64", "Single", true, 1.844674E+19F })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToSingle)", Params = new object[] { 1, "UInt32", "Single", true, 4.294967E+09F })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToSingle)", Params = new object[] { 1, "UInt16", "Single", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToSingle)", Params = new object[] { 1, "Int64", "Single", true, 9.223372E+18F })]
            //[Variation(Desc = "elem.WriteValue(Int32ToSingle)", Params = new object[] { 1, "Int32", "Single", true, 2.147484E+09F })]
            //[Variation(Desc = "elem.WriteValue(Int16ToSingle)", Params = new object[] { 1, "Int16", "Single", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToSingle)", Params = new object[] { 1, "Byte", "Single", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToSingle)", Params = new object[] { 1, "SByte", "Single", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToSingle)", Params = new object[] { 1, "Decimal", "Single", true, 7.922816E+28F })]
            //[Variation(Desc = "elem.WriteValue(floatToSingle)", Params = new object[] { 1, "float", "Single", true, null })]
            //[Variation(Desc = "elem.WriteValue(objectToSingle)", Params = new object[] { 1, "object", "Single", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToSingle)", Params = new object[] { 1, "bool", "Single", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToSingle)", Params = new object[] { 1, "DateTime", "Single", false, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToSingle)", Params = new object[] { 1, "DateTimeOffset", "Single", false, null })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToSingle)", Params = new object[] { 1, "ByteArray", "Single", false, null })]
            //[Variation(Desc = "elem.WriteValue(ListToSingle)", Params = new object[] { 1, "List", "Single", false, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToSingle)", Params = new object[] { 1, "TimeSpan", "Single", false, null })]
            //[Variation(Desc = "elem.WriteValue(UriToSingle)", Params = new object[] { 1, "Uri", "Single", false, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToSingle)", Params = new object[] { 1, "Double", "Single", false, null })]
            //[Variation(Desc = "elem.WriteValue(SingleToSingle)", Params = new object[] { 1, "Single", "Single", true, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameToSingle)", Params = new object[] { 1, "XmlQualifiedName", "Single", false, null })]
            //[Variation(Desc = "elem.WriteValue(stringToSingle)", Params = new object[] { 1, "string", "Single", true, null })]

            //[Variation(Desc = "elem.WriteValue(UInt64ToObject)", Params = new object[] { 1, "UInt64", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ToObject)", Params = new object[] { 1, "UInt32", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ToObject)", Params = new object[] { 1, "UInt16", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ToObject)", Params = new object[] { 1, "Int64", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ToObject)", Params = new object[] { 1, "Int32", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ToObject)", Params = new object[] { 1, "Int16", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(ByteToObject)", Params = new object[] { 1, "Byte", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteToObject)", Params = new object[] { 1, "SByte", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalToObject)", Params = new object[] { 1, "Decimal", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(floatToObject)", Params = new object[] { 1, "float", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(objectToObject)", Params = new object[] { 1, "object", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(boolToObject)", Params = new object[] { 1, "bool", "object", true, "false" })]
            //[Variation(Desc = "elem.WriteValue(DateTimeToObject)", Params = new object[] { 1, "DateTime", "object", true, 1 })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetToObject)", Params = new object[] { 1, "DateTimeOffset", "object", true, 2 })]
            //[Variation(Desc = "elem.WriteValue(ByteArrayToObject)", Params = new object[] { 1, "ByteArray", "object", true, "2H4=" })]
            //[Variation(Desc = "elem.WriteValue(ListToObject)", Params = new object[] { 1, "List", "object", true, "" })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToObject)", Params = new object[] { 1, "TimeSpan", "object", true, "PT0S" })]
            //[Variation(Desc = "elem.WriteValue(UriToObject)", Params = new object[] { 1, "Uri", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleToObject)", Params = new object[] { 1, "Double", "object", true, "1.7976931348623157E+308" })]
            //[Variation(Desc = "elem.WriteValue(SingleToObject)", Params = new object[] { 1, "Single", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameTypeToObject)", Params = new object[] { 1, "XmlQualifiedName", "object", true, null })]
            //[Variation(Desc = "elem.WriteValue(stringToObject)", Params = new object[] { 1, "string", "object", true, null })]

            //[Variation(Desc = "elem.WriteValue(ByteArrayToByteArray)", Params = new object[] { 1, "ByteArray", "ByteArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(BoolArrayToBoolArray)", Params = new object[] { 1, "BoolArray", "BoolArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(ObjectArrayToObjectArray)", Params = new object[] { 1, "ObjectArray", "ObjectArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeArrayToDateTimeArray)", Params = new object[] { 1, "DateTimeArray", "DateTimeArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(DateTimeOffsetArrayToDateTimeOffsetArray)", Params = new object[] { 1, "DateTimeOffsetArray", "DateTimeOffsetArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(DecimalArrayToDecimalArray)", Params = new object[] { 1, "DecimalArray", "DecimalArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(DoubleArrayToDoubleArray)", Params = new object[] { 1, "DoubleArray", "DoubleArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int16ArrayToInt16Array)", Params = new object[] { 1, "Int16Array", "Int16Array", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int32ArrayToInt32Array)", Params = new object[] { 1, "Int32Array", "Int32Array", true, null })]
            //[Variation(Desc = "elem.WriteValue(Int64ArrayToInt64Array)", Params = new object[] { 1, "Int64Array", "Int64Array", true, null })]
            //[Variation(Desc = "elem.WriteValue(SByteArrayToSByteArray)", Params = new object[] { 1, "SByteArray", "SByteArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(SingleArrayToSingleArray)", Params = new object[] { 1, "SingleArray", "SingleArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(StringArrayToStringArray)", Params = new object[] { 1, "StringArray", "StringArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanArrayToTimeSpanArray)", Params = new object[] { 1, "TimeSpanArray", "TimeSpanArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt16ArrayToUInt16Array)", Params = new object[] { 1, "UInt16Array", "UInt16Array", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt32ArrayToUInt32Array)", Params = new object[] { 1, "UInt32Array", "UInt32Array", true, null })]
            //[Variation(Desc = "elem.WriteValue(UInt64ArrayToUInt64Array)", Params = new object[] { 1, "UInt64Array", "UInt64Array", true, null })]
            //[Variation(Desc = "elem.WriteValue(UriArrayToUriArray)", Params = new object[] { 1, "UriArray", "UriArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameArrayToXmlQualifiedNameArray)", Params = new object[] { 1, "XmlQualifiedNameArray", "XmlQualifiedNameArray", true, null })]
            //[Variation(Desc = "elem.WriteValue(TimeSpanToTimeSpan)", Params = new object[] { 1, "TimeSpan", "TimeSpan", true, null })]
            //[Variation(Desc = "elem.WriteValue(XmlQualifiedNameToXmlQualifiedName)", Params = new object[] { 1, "XmlQualifiedName", "XmlQualifiedName", true, null })]

            //////////attr         
            //[Variation(Desc = "attr.WriteValue(Int16ToString)", Params = new object[] { 2, "Int16", "string", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToString)", Params = new object[] { 2, "Byte", "string", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToString)", Params = new object[] { 2, "SByte", "string", true, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToString)", Params = new object[] { 2, "Decimal", "string", true, null })]
            //[Variation(Desc = "attr.WriteValue(floatToString)", Params = new object[] { 2, "float", "string", true, null })]
            //[Variation(Desc = "attr.WriteValue(objectToString)", Params = new object[] { 2, "object", "string", true, null })]
            //[Variation(Desc = "attr.WriteValue(boolToString)", Params = new object[] { 2, "bool", "string", true, "False" })]
            //[Variation(Desc = "attr.WriteValue(UriToString)", Params = new object[] { 2, "Uri", "string", true, null })]
            //[Variation(Desc = "attr.WriteValue(DoubleToString)", Params = new object[] { 2, "Double", "string", true, null })]
            //[Variation(Desc = "attr.WriteValue(SingleToString)", Params = new object[] { 2, "Single", "string", true, null })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameTypeToString)", Params = new object[] { 2, "XmlQualifiedName", "string", true, null })]
            //[Variation(Desc = "attr.WriteValue(stringToString)", Params = new object[] { 2, "string", "string", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToUInt64)", Params = new object[] { 2, "UInt64", "UInt64", true, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToUInt64)", Params = new object[] { 2, "UInt32", "UInt64", true, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToUInt64)", Params = new object[] { 2, "UInt16", "UInt64", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToUInt64)", Params = new object[] { 2, "Int64", "UInt64", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToUInt64)", Params = new object[] { 2, "Int32", "UInt64", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToUInt64)", Params = new object[] { 2, "Int16", "UInt64", true, null })]
            //[Variation(Desc = "attr.WriteValue(ListToUInt64)", Params = new object[] { 2, "List", "UInt64", false, null })]
            //[Variation(Desc = "attr.WriteValue(TimeSpanToUInt64)", Params = new object[] { 2, "TimeSpan", "UInt64", false, null })]
            //[Variation(Desc = "attr.WriteValue(UriToUInt64)", Params = new object[] { 2, "Uri", "UInt64", false, null })]
            //[Variation(Desc = "attr.WriteValue(DoubleToUInt64)", Params = new object[] { 2, "Double", "UInt64", false, null })]
            //[Variation(Desc = "attr.WriteValue(SingleToUInt64)", Params = new object[] { 2, "Single", "UInt64", false, null })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameTypeToUInt64)", Params = new object[] { 2, "XmlQualifiedName", "UInt64", false, null })]
            //[Variation(Desc = "attr.WriteValue(stringToUInt64)", Params = new object[] { 2, "string", "UInt64", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToInt64)", Params = new object[] { 2, "UInt64", "Int64", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToInt64)", Params = new object[] { 2, "UInt32", "Int64", true, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToInt64)", Params = new object[] { 2, "UInt16", "Int64", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToInt64)", Params = new object[] { 2, "Int64", "Int64", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToInt64)", Params = new object[] { 2, "Int32", "Int64", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToInt64)", Params = new object[] { 2, "Int16", "Int64", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToInt64)", Params = new object[] { 2, "Byte", "Int64", true, null })]
            //[Variation(Desc = "attr.WriteValue(TimeSpanToInt64)", Params = new object[] { 2, "TimeSpan", "Int64", false, null })]
            //[Variation(Desc = "attr.WriteValue(UriToInt64)", Params = new object[] { 2, "Uri", "Int64", false, null })]
            //[Variation(Desc = "attr.WriteValue(DoubleToInt64)", Params = new object[] { 2, "Double", "Int64", false, null })]
            //[Variation(Desc = "attr.WriteValue(SingleToInt64)", Params = new object[] { 2, "Single", "Int64", false, null })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameTypeToInt64)", Params = new object[] { 2, "XmlQualifiedName", "Int64", false, null })]
            //[Variation(Desc = "attr.WriteValue(stringToInt64)", Params = new object[] { 2, "string", "Int64", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToUInt32)", Params = new object[] { 2, "UInt64", "UInt32", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToUInt32)", Params = new object[] { 2, "UInt32", "UInt32", true, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToUInt32)", Params = new object[] { 2, "UInt16", "UInt32", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToUInt32)", Params = new object[] { 2, "Int64", "UInt32", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToUInt32)", Params = new object[] { 2, "Int32", "UInt32", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToUInt32)", Params = new object[] { 2, "Int16", "UInt32", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToUInt32)", Params = new object[] { 2, "Byte", "UInt32", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToUInt32)", Params = new object[] { 2, "SByte", "UInt32", true, null })]
            //[Variation(Desc = "attr.WriteValue(stringToUInt32)", Params = new object[] { 2, "string", "UInt32", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToInt32)", Params = new object[] { 2, "UInt64", "Int32", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToInt32)", Params = new object[] { 2, "UInt32", "Int32", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToInt32)", Params = new object[] { 2, "UInt16", "Int32", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToInt32)", Params = new object[] { 2, "Int64", "Int32", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToInt32)", Params = new object[] { 2, "Int32", "Int32", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToInt32)", Params = new object[] { 2, "Int16", "Int32", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToInt32)", Params = new object[] { 2, "Byte", "Int32", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToInt32)", Params = new object[] { 2, "SByte", "Int32", true, null })]
            //[Variation(Desc = "attr.WriteValue(SingleToInt32)", Params = new object[] { 2, "Single", "Int32", false, null })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameTypeToInt32)", Params = new object[] { 2, "XmlQualifiedName", "Int32", false, null })]
            //[Variation(Desc = "attr.WriteValue(stringToInt32)", Params = new object[] { 2, "string", "Int32", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToUInt16)", Params = new object[] { 2, "UInt64", "UInt16", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToUInt16)", Params = new object[] { 2, "UInt32", "UInt16", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToUInt16)", Params = new object[] { 2, "UInt16", "UInt16", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToUInt16)", Params = new object[] { 2, "Int64", "UInt16", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToUInt16)", Params = new object[] { 2, "Int32", "UInt16", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToUInt16)", Params = new object[] { 2, "Int16", "UInt16", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToUInt16)", Params = new object[] { 2, "Byte", "UInt16", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToUInt16)", Params = new object[] { 2, "SByte", "UInt16", true, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToUInt16)", Params = new object[] { 2, "Decimal", "UInt16", false, null })]
            //[Variation(Desc = "attr.WriteValue(floatToUInt16)", Params = new object[] { 2, "float", "UInt16", false, null })]
            //[Variation(Desc = "attr.WriteValue(objectToUInt16)", Params = new object[] { 2, "object", "UInt16", true, null })]
            //[Variation(Desc = "attr.WriteValue(boolToUInt16)", Params = new object[] { 2, "bool", "UInt16", false, null })]
            //[Variation(Desc = "attr.WriteValue(stringToUInt16)", Params = new object[] { 2, "string", "UInt16", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToInt16)", Params = new object[] { 2, "UInt64", "Int16", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToInt16)", Params = new object[] { 2, "UInt32", "Int16", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToInt16)", Params = new object[] { 2, "UInt16", "Int16", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToInt16)", Params = new object[] { 2, "Int64", "Int16", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToInt16)", Params = new object[] { 2, "Int32", "Int16", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToInt16)", Params = new object[] { 2, "Int16", "Int16", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToInt16)", Params = new object[] { 2, "Byte", "Int16", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToInt16)", Params = new object[] { 2, "SByte", "Int16", true, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToInt16)", Params = new object[] { 2, "Decimal", "Int16", false, null })]
            //[Variation(Desc = "attr.WriteValue(floatToInt16)", Params = new object[] { 2, "float", "Int16", false, null })]
            //[Variation(Desc = "attr.WriteValue(objectToInt16)", Params = new object[] { 2, "object", "Int16", true, null })]
            //[Variation(Desc = "attr.WriteValue(boolToInt16)", Params = new object[] { 2, "bool", "Int16", false, null })]
            //[Variation(Desc = "attr.WriteValue(DateTimeToInt16)", Params = new object[] { 2, "DateTime", "Int16", false, null })]
            //[Variation(Desc = "attr.WriteValue(stringToInt16)", Params = new object[] { 2, "string", "Int16", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToByte)", Params = new object[] { 2, "UInt64", "Byte", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToByte)", Params = new object[] { 2, "UInt32", "Byte", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToByte)", Params = new object[] { 2, "UInt16", "Byte", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToByte)", Params = new object[] { 2, "Int64", "Byte", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToByte)", Params = new object[] { 2, "Int32", "Byte", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToByte)", Params = new object[] { 2, "Int16", "Byte", false, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToByte)", Params = new object[] { 2, "Byte", "Byte", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToByte)", Params = new object[] { 2, "SByte", "Byte", true, null })]
            //[Variation(Desc = "attr.WriteValue(stringToByte)", Params = new object[] { 2, "string", "Byte", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToSByte)", Params = new object[] { 2, "UInt64", "SByte", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToSByte)", Params = new object[] { 2, "UInt32", "SByte", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToSByte)", Params = new object[] { 2, "UInt16", "SByte", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToSByte)", Params = new object[] { 2, "Int64", "SByte", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToSByte)", Params = new object[] { 2, "Int32", "SByte", false, null })]
            //[Variation(Desc = "attr.WriteValue(UriToSByte)", Params = new object[] { 2, "Uri", "SByte", false, null })]
            //[Variation(Desc = "attr.WriteValue(DoubleToSByte)", Params = new object[] { 2, "Double", "SByte", false, null })]
            //[Variation(Desc = "attr.WriteValue(SingleToSByte)", Params = new object[] { 2, "Single", "SByte", false, null })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameTypeToSByte)", Params = new object[] { 2, "XmlQualifiedName", "SByte", false, null })]
            //[Variation(Desc = "attr.WriteValue(stringToSByte)", Params = new object[] { 2, "string", "SByte", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToDecimal)", Params = new object[] { 2, "UInt64", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToDecimal)", Params = new object[] { 2, "UInt32", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToDecimal)", Params = new object[] { 2, "UInt16", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToDecimal)", Params = new object[] { 2, "Int64", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToDecimal)", Params = new object[] { 2, "Int32", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToDecimal)", Params = new object[] { 2, "Int16", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToDecimal)", Params = new object[] { 2, "Byte", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToDecimal)", Params = new object[] { 2, "SByte", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToDecimal)", Params = new object[] { 2, "Decimal", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(floatToDecimal)", Params = new object[] { 2, "float", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(objectToDecimal)", Params = new object[] { 2, "object", "Decimal", true, null })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameTypeToDecimal)", Params = new object[] { 21, "XmlQualifiedName", "Decimal", false, null })]
            //[Variation(Desc = "attr.WriteValue(stringToDecimal)", Params = new object[] { 2, "string", "Decimal", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToFloat)", Params = new object[] { 2, "UInt64", "float", true, 1.844674E+19F })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToFloat)", Params = new object[] { 2, "UInt32", "float", true, 4.294967E+09F })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToFloat)", Params = new object[] { 2, "UInt16", "float", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToFloat)", Params = new object[] { 2, "Int64", "float", true, 9.223372E+18F })]
            //[Variation(Desc = "attr.WriteValue(Int32ToFloat)", Params = new object[] { 2, "Int32", "float", true, 2.147484E+09F })]
            //[Variation(Desc = "attr.WriteValue(Int16ToFloat)", Params = new object[] { 2, "Int16", "float", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToFloat)", Params = new object[] { 2, "Byte", "float", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToFloat)", Params = new object[] { 2, "SByte", "float", true, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToFloat)", Params = new object[] { 2, "Decimal", "float", true, 7.922816E+28F })]
            //[Variation(Desc = "attr.WriteValue(floatToFloat)", Params = new object[] { 2, "float", "float", true, null })]
            //[Variation(Desc = "attr.WriteValue(objectToFloat)", Params = new object[] { 2, "object", "float", true, null })]
            //[Variation(Desc = "attr.WriteValue(boolToFloat)", Params = new object[] { 2, "bool", "float", false, null })]
            //[Variation(Desc = "attr.WriteValue(SingleTofloat)", Params = new object[] { 2, "Single", "float", true, null })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameTypeToFloat)", Params = new object[] { 2, "XmlQualifiedName", "float", false, null })]
            //[Variation(Desc = "attr.WriteValue(stringToFloat)", Params = new object[] { 2, "string", "float", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToBool)", Params = new object[] { 2, "UInt64", "bool", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToBool)", Params = new object[] { 2, "UInt32", "bool", false, null })]
            //[Variation(Desc = "attr.WriteValue(objectToBool)", Params = new object[] { 2, "object", "bool", true, false })]
            //[Variation(Desc = "attr.WriteValue(DateTimeToBool)", Params = new object[] { 2, "DateTime", "bool", false, null })]
            //[Variation(Desc = "attr.WriteValue(DateTimeOffsetToBool)", Params = new object[] { 2, "DateTimeOffset", "bool", false, null })]
            //[Variation(Desc = "attr.WriteValue(ByteArrayToBool)", Params = new object[] { 2, "ByteArray", "bool", false, null })]
            //[Variation(Desc = "attr.WriteValue(ListToBool)", Params = new object[] { 2, "List", "bool", false, null })]
            //[Variation(Desc = "attr.WriteValue(TimeSpanToBool)", Params = new object[] { 2, "TimeSpan", "bool", false, null })]
            //[Variation(Desc = "attr.WriteValue(UriToBool)", Params = new object[] { 2, "Uri", "bool", false, null })]
            //[Variation(Desc = "attr.WriteValue(DoubleToBool)", Params = new object[] { 2, "Double", "bool", false, null })]
            //[Variation(Desc = "attr.WriteValue(SingleTobool)", Params = new object[] { 2, "Single", "bool", false, null })]

            //[Variation(Desc = "attr.WriteValue(floatToDateTime)", Params = new object[] { 2, "float", "DateTime", false, null })]
            //[Variation(Desc = "attr.WriteValue(objectToDateTime)", Params = new object[] { 2, "object", "DateTime", false, null })]
            //[Variation(Desc = "attr.WriteValue(boolToDateTime)", Params = new object[] { 2, "bool", "DateTime", false, null })]
            //[Variation(Desc = "attr.WriteValue(ByteArrayToDateTime)", Params = new object[] { 2, "ByteArray", "DateTime", false, null })]
            //[Variation(Desc = "attr.WriteValue(ListToDateTime)", Params = new object[] { 2, "List", "DateTime", false, null })]
            //[Variation(Desc = "attr.WriteValue(UriToDateTime)", Params = new object[] { 2, "Uri", "DateTime", false, null })]
            //[Variation(Desc = "attr.WriteValue(DoubleToDateTime)", Params = new object[] { 2, "Double", "DateTime", false, null })]
            //[Variation(Desc = "attr.WriteValue(SingleToDateTime)", Params = new object[] { 2, "Single", "DateTime", false, null })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameTypeToDateTime)", Params = new object[] { 2, "XmlQualifiedName", "DateTime", false, null })]
            //[Variation(Desc = "attr.WriteValue(stringToDateTime)", Params = new object[] { 2, "string", "DateTime", false, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToDateTimeOffset)", Params = new object[] { 2, "UInt64", "DateTimeOffset", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToDateTimeOffset)", Params = new object[] { 2, "UInt32", "DateTimeOffset", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToDateTimeOffset)", Params = new object[] { 2, "UInt16", "DateTimeOffset", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToDateTimeOffset)", Params = new object[] { 2, "Int64", "DateTimeOffset", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToDateTimeOffset)", Params = new object[] { 2, "Int32", "DateTimeOffset", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToDateTimeOffset)", Params = new object[] { 2, "Int16", "DateTimeOffset", false, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToDateTimeOffset)", Params = new object[] { 2, "Byte", "DateTimeOffset", false, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToDateTimeOffset)", Params = new object[] { 2, "SByte", "DateTimeOffset", false, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToDateTimeOffset)", Params = new object[] { 2, "Decimal", "DateTimeOffset", false, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToList)", Params = new object[] { 2, "UInt64", "List", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToList)", Params = new object[] { 2, "UInt32", "List", false, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToList)", Params = new object[] { 2, "UInt16", "List", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToList)", Params = new object[] { 2, "Int64", "List", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToList)", Params = new object[] { 2, "Int32", "List", false, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToList)", Params = new object[] { 2, "Int16", "List", false, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToList)", Params = new object[] { 2, "Byte", "List", false, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToList)", Params = new object[] { 2, "SByte", "List", false, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToList)", Params = new object[] { 2, "Decimal", "List", false, null })]
            //[Variation(Desc = "attr.WriteValue(floatToList)", Params = new object[] { 2, "float", "List", false, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToUri)", Params = new object[] { 2, "UInt64", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToUri)", Params = new object[] { 2, "UInt32", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToUri)", Params = new object[] { 2, "UInt16", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToUri)", Params = new object[] { 2, "Int64", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToUri)", Params = new object[] { 2, "Int32", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToUri)", Params = new object[] { 2, "Int16", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToUri)", Params = new object[] { 2, "Byte", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToUri)", Params = new object[] { 2, "SByte", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToUri)", Params = new object[] { 2, "Decimal", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(floatToUri)", Params = new object[] { 2, "float", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(objectToUri)", Params = new object[] { 2, "object", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(boolToUri)", Params = new object[] { 2, "bool", "Uri", true, "False" })]
            //[Variation(Desc = "attr.WriteValue(UriToUri)", Params = new object[] { 2, "Uri", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(DoubleToUri)", Params = new object[] { 2, "Double", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(SingleToUri)", Params = new object[] { 2, "Single", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameTypeToUri)", Params = new object[] { 2, "XmlQualifiedName", "Uri", true, null })]
            //[Variation(Desc = "attr.WriteValue(stringToUri)", Params = new object[] { 2, "string", "Uri", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToDouble)", Params = new object[] { 2, "UInt64", "Double", true, 1.84467440737096E+19D })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToDouble)", Params = new object[] { 2, "UInt32", "Double", true, null })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToDouble)", Params = new object[] { 2, "UInt16", "Double", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToDouble)", Params = new object[] { 2, "Int64", "Double", true, 9.22337203685478E+18D })]
            //[Variation(Desc = "attr.WriteValue(Int32ToDouble)", Params = new object[] { 2, "Int32", "Double", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToDouble)", Params = new object[] { 2, "Int16", "Double", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToDouble)", Params = new object[] { 2, "Byte", "Double", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToDouble)", Params = new object[] { 2, "SByte", "Double", true, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToDouble)", Params = new object[] { 2, "Decimal", "Double", true, 7.92281625142643E+28D })]
            //[Variation(Desc = "attr.WriteValue(floatToDouble)", Params = new object[] { 2, "float", "Double", true, null })]
            //[Variation(Desc = "attr.WriteValue(objectToDouble)", Params = new object[] { 2, "object", "Double", true, null })]
            //[Variation(Desc = "attr.WriteValue(boolToDouble)", Params = new object[] { 2, "bool", "Double", false, null })]
            //[Variation(Desc = "attr.WriteValue(DoubleToDouble)", Params = new object[] { 2, "Double", "Double", false, null })]
            //[Variation(Desc = "attr.WriteValue(SingleToDouble)", Params = new object[] { 2, "Single", "Double", true, null })]
            //[Variation(Desc = "attr.WriteValue(stringToDouble)", Params = new object[] { 2, "string", "Double", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToSingle)", Params = new object[] { 2, "UInt64", "Single", true, 1.844674E+19F })]
            //[Variation(Desc = "attr.WriteValue(UInt32ToSingle)", Params = new object[] { 2, "UInt32", "Single", true, 4.294967E+09F })]
            //[Variation(Desc = "attr.WriteValue(UInt16ToSingle)", Params = new object[] { 2, "UInt16", "Single", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int64ToSingle)", Params = new object[] { 2, "Int64", "Single", true, 9.223372E+18F })]
            //[Variation(Desc = "attr.WriteValue(Int32ToSingle)", Params = new object[] { 2, "Int32", "Single", true, 2.147484E+09F })]
            //[Variation(Desc = "attr.WriteValue(Int16ToSingle)", Params = new object[] { 2, "Int16", "Single", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToSingle)", Params = new object[] { 2, "Byte", "Single", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToSingle)", Params = new object[] { 2, "SByte", "Single", true, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToSingle)", Params = new object[] { 2, "Decimal", "Single", true, 7.922816E+28F })]
            //[Variation(Desc = "attr.WriteValue(floatToSingle)", Params = new object[] { 2, "float", "Single", true, null })]
            //[Variation(Desc = "attr.WriteValue(objectToSingle)", Params = new object[] { 2, "object", "Single", true, null })]
            //[Variation(Desc = "attr.WriteValue(boolToSingle)", Params = new object[] { 2, "bool", "Single", false, null })]
            //[Variation(Desc = "attr.WriteValue(DateTimeOffsetToSingle)", Params = new object[] { 2, "DateTimeOffset", "Single", false, null })]
            //[Variation(Desc = "attr.WriteValue(SingleToSingle)", Params = new object[] { 2, "Single", "Single", true, null })]
            //[Variation(Desc = "attr.WriteValue(stringToSingle)", Params = new object[] { 2, "string", "Single", true, null })]

            //[Variation(Desc = "attr.WriteValue(UInt64ToObject)", Params = new object[] { 2, "UInt64", "object", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int32ToObject)", Params = new object[] { 2, "Int32", "object", true, null })]
            //[Variation(Desc = "attr.WriteValue(Int16ToObject)", Params = new object[] { 2, "Int16", "object", true, null })]
            //[Variation(Desc = "attr.WriteValue(ByteToObject)", Params = new object[] { 2, "Byte", "object", true, null })]
            //[Variation(Desc = "attr.WriteValue(SByteToObject)", Params = new object[] { 2, "SByte", "object", true, null })]
            //[Variation(Desc = "attr.WriteValue(DecimalToObject)", Params = new object[] { 2, "Decimal", "object", true, null })]
            //[Variation(Desc = "attr.WriteValue(floatToObject)", Params = new object[] { 2, "float", "object", true, null })]
            //[Variation(Desc = "attr.WriteValue(objectToObject)", Params = new object[] { 2, "object", "object", true, null })]
            //[Variation(Desc = "attr.WriteValue(boolToObject)", Params = new object[] { 2, "bool", "object", true, "False" })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameTypeToObject)", Params = new object[] { 2, "XmlQualifiedName", "object", true, null })]
            //[Variation(Desc = "attr.WriteValue(stringToObject)", Params = new object[] { 2, "string", "object", true, null })]
            //[Variation(Desc = "attr.WriteValue(ObjectArrayToObjectArray)", Params = new object[] { 2, "ObjectArray", "ObjectArray", true, null })]
            //[Variation(Desc = "attr.WriteValue(StringArrayToStringArray)", Params = new object[] { 2, "StringArray", "StringArray", true, null })]
            //[Variation(Desc = "attr.WriteValue(UriArrayToUriArray)", Params = new object[] { 2, "UriArray", "UriArray", true, null })]
            //[Variation(Desc = "attr.WriteValue(XmlQualifiedNameToXmlQualifiedName)", Params = new object[] { 2, "XmlQualifiedName", "XmlQualifiedName", true, null })]

            public int writeValue_27()
            {
                if (CurVariation.Desc.Equals("elem.WriteValue(new DateTimeOffset)"))
                {
                    Console.WriteLine("elem.WriteValue(new DateTimeOffset)");
                }

                int param = (int)CurVariation.Params[0];
                string sourceStr = (string)CurVariation.Params[1];
                string destStr = (string)CurVariation.Params[2];
                Type source = typeMapper[sourceStr];
                Type dest = typeMapper[destStr];
                bool isValid = (bool)CurVariation.Params[3];
                object expVal = (object)CurVariation.Params[4];

                if (expVal == null && destStr.Contains("DateTime"))
                    expVal = value[destStr];
                else if (expVal != null && sourceStr.Contains("DateTime"))
                    expVal = _dates[(int)expVal];
                else if (sourceStr.Equals("XmlQualifiedName") && (WriterType == WriterType.CustomWriter) && param == 1)
                    expVal = "{}a";
                else if (expVal == null)
                    expVal = value[sourceStr];

                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    if (param == 1)
                        w.WriteValue(value[sourceStr]);
                    else
                        w.WriteAttributeString("a", value[sourceStr].ToString());
                    w.WriteEndElement();
                }
                try
                {
                    VerifyValue(dest, expVal, param);
                }
                catch (XmlException)
                {
                    if (!isValid || (WriterType == WriterType.CustomWriter) && sourceStr.Contains("XmlQualifiedName")) return TEST_PASS;
                    CError.Compare(false, "XmlException");
                }
                catch (OverflowException)
                {
                    if (!isValid) return TEST_PASS;
                    CError.Compare(false, "OverflowException");
                }
                catch (FormatException)
                {
                    if (!isValid) return TEST_PASS;
                    CError.Compare(false, "FormatException");
                }
                catch (ArgumentOutOfRangeException)
                {
                    if (!isValid) return TEST_PASS;
                    CError.Compare(false, "ArgumentOutOfRangeException");
                }
                catch (InvalidCastException)
                {
                    if (!isValid) return TEST_PASS;
                    CError.Compare(false, "ArgumentException");
                }
                return (isValid) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(Desc = "WriteValue(XmlException)", Pri = 2, Param = 1)]
            //[Variation(Desc = "WriteValue(DayOfWeek)", Pri = 2, Param = 2)]
            //[Variation(Desc = "WriteValue(XmlQualifiedName)", Pri = 2, Param = 3)]
            //[Variation(Desc = "WriteValue(Guid)", Pri = 2, Param = 4)]
            //[Variation(Desc = "WriteValue(XmlDateTimeSerializationMode.Local)", Pri = 2, Param = 5)]
            //[Variation(Desc = "WriteValue(NewLineHandling.Entitize)", Pri = 2, Param = 6)]
            //[Variation(Desc = "ConformanceLevel.Auto", Pri = 2, Param = 7)]
            //[Variation(Desc = "TimeZone.CurrentTimeZone", Pri = 2, Param = 8)]
            //[Variation(Desc = "WriteValue(Tuple)", Pri = 2, Param = 9)]
            //[Variation(Desc = "WriteValue(DynamicObject)", Pri = 2, Param = 10)]
            public int writeValue_28()
            {
                int param = (int)CurVariation.Param;

                Tuple<Int32, String, Double> t = Tuple.Create(1, "Melitta", 7.5);

                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    try
                    {
                        switch (param)
                        {
                            case 1: w.WriteValue(new XmlException()); break;
                            case 2: w.WriteValue(DayOfWeek.Friday); break;
                            case 3: w.WriteValue(new XmlQualifiedName("b", "c")); break;
                            case 4: w.WriteValue(new Guid()); break;
                            case 6: w.WriteValue(NewLineHandling.Entitize); break;
                            case 7: w.WriteValue(ConformanceLevel.Auto); break;
                            case 9: w.WriteValue(t); break;
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        CError.WriteLine(e.Message);
                        try
                        {
                            switch (param)
                            {
                                case 1: w.WriteValue(new XmlException()); break;
                                case 2: w.WriteValue(DayOfWeek.Friday); break;
                                case 3: w.WriteValue(new XmlQualifiedName("b", "c")); break;
                                case 4: w.WriteValue(new Guid()); break;
                                case 6: w.WriteValue(NewLineHandling.Entitize); break;
                                case 7: w.WriteValue(ConformanceLevel.Auto); break;
                                case 9: w.WriteValue(t); break;
                            }
                        }
                        catch (InvalidOperationException) { return TEST_PASS; }
                        catch (InvalidCastException) { return TEST_PASS; }
                    }
                }
                return (param == 3 && (WriterType == WriterType.CustomWriter)) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(Desc = "WriteValue(stringToXmlQualifiedName-invalid)", Pri = 1, Param = 1)]
            //[Variation(Desc = "WriteValue(stringToXmlQualifiedName-invalid attr)", Pri = 1, Param = 2)]
            public int writeValue_30()
            {
                int param = (int)CurVariation.Param;
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    if (param == 1)
                        w.WriteValue("p:foo");
                    else
                        w.WriteAttributeString("a", "p:foo");
                    w.WriteEndElement();
                }
                try
                {
                    VerifyValue(typeof(XmlQualifiedName), "p:foo", param);
                }
                catch (XmlException) { return TEST_PASS; }
                catch (InvalidOperationException) { return TEST_PASS; }
                return TEST_FAIL;
            }

            //[Variation(Desc = "1.WriteValue(DateTimeOffset) - valid", Params = new object[] { "2002-12-30T00:00:00-08:00", "<Root>2002-12-30T00:00:00-08:00</Root>" })]
            //[Variation(Desc = "2.WriteValue(DateTimeOffset) - valid", Params = new object[] { "2000-02-29T23:59:59.999999999999-13:60", "<Root>2000-03-01T00:00:00-14:00</Root>" })]
            //[Variation(Desc = "3.WriteValue(DateTimeOffset) - valid", Params = new object[] { "0001-01-01T00:00:00+00:00", "<Root>0001-01-01T00:00:00Z</Root>" })]
            //[Variation(Desc = "4.WriteValue(DateTimeOffset) - valid", Params = new object[] { "0001-01-01T00:00:00.9999999-14:00", "<Root>0001-01-01T00:00:00.9999999-14:00</Root>" })]
            //[Variation(Desc = "5.WriteValue(DateTimeOffset) - valid", Params = new object[] { "9999-12-31T12:59:59.9999999+14:00", "<Root>9999-12-31T12:59:59.9999999+14:00</Root>" })]
            //[Variation(Desc = "6.WriteValue(DateTimeOffset) - valid", Params = new object[] { "9999-12-31T12:59:59-11:00", "<Root>9999-12-31T12:59:59-11:00</Root>" })]
            //[Variation(Desc = "7.WriteValue(DateTimeOffset) - valid", Params = new object[] { "2000-02-29T23:59:59.999999999999+13:60", "<Root>2000-03-01T00:00:00+14:00</Root>" })]
            public int writeValue_31()
            {
                string value = (string)CurVariation.Params[0];
                string expectedValue = (string)CurVariation.Params[1];
                DateTimeOffset a = XmlConvert.ToDateTimeOffset(value);
                using (XmlWriter w = CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteValue(XmlConvert.ToDateTimeOffset(value));
                    w.WriteEndElement();
                }
                return (CompareReader(expectedValue)) ? TEST_PASS : TEST_FAIL;
            }

            //[Variation(Desc = "WriteValue(new DateTimeOffset) - valid", Pri = 2)]
            public int writeValue_32()
            {
                DateTimeOffset actual;
                string expect;
                bool isPassed = true;
                object[] actualArray =
                {
                    new DateTimeOffset(2002,2,1,0,0,0,TimeSpan.FromHours(-8.0)),
                    new DateTimeOffset(9999,1,1,0,0,0,TimeSpan.FromHours(-8.0)),
                    new DateTimeOffset(9999,1,1,0,0,0,TimeSpan.FromHours(0)),
                    new DateTimeOffset(9999,12,31,12,59,59,TimeSpan.FromHours(-11.0)),
                    new DateTimeOffset(9999,12,31,12,59,59,TimeSpan.FromHours(-10) + TimeSpan.FromMinutes(-59)),
                    new DateTimeOffset(9999,12,31,12,59,59,new TimeSpan(13,59,0)),
                    new DateTimeOffset(9999,12,31,23,59,59,TimeSpan.FromHours(0)),
                    new DateTimeOffset(9999,12,31,23,59,59, new TimeSpan(14,0,0)),
                    new DateTimeOffset(9999,12,31,23,59,59, new TimeSpan(13,60,0)),
                    new DateTimeOffset(9999,12,31,23,59,59, new TimeSpan(13,59,60)),
                    new DateTimeOffset(9998,12,31,12,59,59, new TimeSpan(13,60,0)),
                    new DateTimeOffset(9998,12,31,12,59,59,TimeSpan.FromHours(-14.0)),
                    new DateTimeOffset(1,1,1,0,0,0,TimeSpan.FromHours(-8.0)),
                    new DateTimeOffset(1,1,1,0,0,0,TimeSpan.FromHours(-14.0)),
                    new DateTimeOffset(1,1,1,0,0,0,TimeSpan.FromHours(-13) + TimeSpan.FromMinutes(-59)),
                    new DateTimeOffset(1,1,1,0,0,0,TimeSpan.Zero),
                };
                object[] expectArray =
                {
                    "<Root>2002-02-01T00:00:00-08:00</Root>",
                    "<Root>9999-01-01T00:00:00-08:00</Root>",
                    "<Root>9999-01-01T00:00:00Z</Root>",
                    "<Root>9999-12-31T12:59:59-11:00</Root>",
                    "<Root>9999-12-31T12:59:59-10:59</Root>",
                    "<Root>9999-12-31T12:59:59+13:59</Root>",
                    "<Root>9999-12-31T23:59:59Z</Root>",
                    "<Root>9999-12-31T23:59:59+14:00</Root>",
                    "<Root>9999-12-31T23:59:59+14:00</Root>",
                    "<Root>9999-12-31T23:59:59+14:00</Root>",
                    "<Root>9998-12-31T12:59:59+14:00</Root>",
                    "<Root>9998-12-31T12:59:59-14:00</Root>",
                    "<Root>0001-01-01T00:00:00-08:00</Root>",
                    "<Root>0001-01-01T00:00:00-14:00</Root>",
                    "<Root>0001-01-01T00:00:00-13:59</Root>",
                    "<Root>0001-01-01T00:00:00Z</Root>"
                };

                for (int i = 0; i < actualArray.Length; i++)
                {
                    actual = (DateTimeOffset)actualArray[i];
                    expect = (string)expectArray[i];

                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteValue(actual);
                        w.WriteEndElement();
                        w.Dispose();
                        if (!CompareReader((string)expect))
                        {
                            isPassed = false;
                        }
                    }
                }
                return (isPassed) ? TEST_PASS : TEST_FAIL;
            }

            //[TestCase(Name = "LookupPrefix")]
            public partial class TCLookUpPrefix : XmlWriterTestCaseBase
            {
                //[Variation(id = 1, Desc = "LookupPrefix with null", Pri = 2)]
                public int lookupPrefix_1()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            string s = w.LookupPrefix(null);
                            w.Dispose();
                        }
                        catch (ArgumentException e)
                        {
                            CError.WriteLineIgnore("Exception: " + e.ToString());
                            CheckErrorState(w.WriteState);
                            return TEST_PASS;
                        }
                    }
                    CError.WriteLine("Did not throw exception");
                    return TEST_FAIL;
                }

                //[Variation(id = 2, Desc = "LookupPrefix with String.Empty should return String.Empty", Pri = 1)]
                public int lookupPrefix_2()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        string s = w.LookupPrefix(String.Empty);
                        CError.Compare(s, String.Empty, "Error");
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 3, Desc = "LookupPrefix with generated namespace used for attributes", Pri = 1)]
                public int lookupPrefix_3()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("a", "foo", "b");
                        string s = w.LookupPrefix("foo");
                        string exp = "p1";
                        CError.Compare(s, exp, "Error");
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 4, Desc = "LookupPrefix for namespace used with element", Pri = 0)]
                public int lookupPrefix_4()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("ns1", "Root", "foo");
                        string s = w.LookupPrefix("foo");
                        CError.Compare(s, "ns1", "Error");
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 5, Desc = "LookupPrefix for namespace used with attribute", Pri = 0)]
                public int lookupPrefix_5()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("ns1", "attr1", "foo", "val1");
                        string s = w.LookupPrefix("foo");
                        CError.Compare(s, "ns1", "Error");
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 6, Desc = "Lookup prefix for a default namespace", Pri = 1)]
                public int lookupPrefix_6()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root", "foo");
                        w.WriteString("content");
                        string s = w.LookupPrefix("foo");
                        CError.Compare(s, String.Empty, "Error");
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 7, Desc = "Lookup prefix for nested element with same namespace but different prefix", Pri = 1)]
                public int lookupPrefix_7()
                {
                    string s = "";
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("x", "Root", "foo");
                        s = w.LookupPrefix("foo");
                        CError.Compare(s, "x", "Error");

                        w.WriteStartElement("y", "node", "foo");
                        s = w.LookupPrefix("foo");
                        CError.Compare(s, "y", "Error");

                        w.WriteStartElement("z", "node1", "foo");
                        s = w.LookupPrefix("foo");
                        CError.Compare(s, "z", "Error");
                        w.WriteEndElement();

                        s = w.LookupPrefix("foo");
                        CError.Compare(s, "y", "Error");
                        w.WriteEndElement();

                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 8, Desc = "Lookup prefix for multiple prefix associated with the same namespace", Pri = 1)]
                public int lookupPrefix_8()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("x", "Root", "foo");
                        w.WriteAttributeString("y", "a", "foo", "b");
                        string s = w.LookupPrefix("foo");
                        CError.Compare(s, "y", "Error");
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 9, Desc = "Lookup prefix for namespace defined outside the scope of an empty element and also defined in its parent", Pri = 1)]
                public int lookupPrefix_9()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("x", "Root", "foo");
                        w.WriteStartElement("y", "node", "foo");
                        w.WriteEndElement();
                        string s = w.LookupPrefix("foo");
                        CError.Compare(s, "x", "Error");
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 10, Desc = "Bug 53940: Lookup prefix for namespace declared as default and also with a prefix", Pri = 1)]
                public int lookupPrefix_10()
                {
                    string s;
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root", "foo");
                        w.WriteStartElement("x", "node", "foo");
                        s = w.LookupPrefix("foo");
                        CError.Compare(s, "x", "Error in nested element");
                        w.WriteEndElement();
                        s = w.LookupPrefix("foo");
                        CError.Compare(s, String.Empty, "Error in root element");
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }
            }

            //[TestCase(Name = "XmlSpace")]
            public partial class TCXmlSpace : XmlWriterTestCaseBase
            {
                //[Variation(id = 1, Desc = "Verify XmlSpace as Preserve", Pri = 0)]
                public int xmlSpace_1()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "space", null, "preserve");
                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 2, Desc = "Verify XmlSpace as Default", Pri = 0)]
                public int xmlSpace_2()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "space", null, "default");
                        CError.Compare(w.XmlSpace, XmlSpace.Default, "Error");
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 3, Desc = "Verify XmlSpace as None", Pri = 0)]
                public int xmlSpace_3()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        CError.Compare(w.XmlSpace, XmlSpace.None, "Error");
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 4, Desc = "Verify XmlSpace within an empty element", Pri = 1)]
                public int xmlSpace_4()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "space", null, "preserve");
                        w.WriteStartElement("node", null);

                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");

                        w.WriteEndElement();
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 5, Desc = "Verify XmlSpace - scope with nested elements (both PROLOG and EPILOG)", Pri = 1)]
                public int xmlSpace_5()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");

                        w.WriteStartElement("node", null);
                        w.WriteAttributeString("xml", "space", null, "preserve");
                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");

                        w.WriteStartElement("node1");
                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");

                        w.WriteStartElement("node2");
                        w.WriteAttributeString("xml", "space", null, "default");
                        CError.Compare(w.XmlSpace, XmlSpace.Default, "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlSpace, XmlSpace.None, "Error");
                        w.WriteEndElement();
                    }

                    return TEST_PASS;
                }

                //[Variation(id = 6, Desc = "Verify XmlSpace - outside defined scope", Pri = 1)]
                public int xmlSpace_6()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartElement("node", null);
                        w.WriteAttributeString("xml", "space", null, "preserve");
                        w.WriteEndElement();

                        CError.Compare(w.XmlSpace, XmlSpace.None, "Error");
                        w.WriteEndElement();
                    }

                    return TEST_PASS;
                }

                //[Variation(id = 7, Desc = "Verify XmlSpace with invalid space value", Pri = 0)]
                public int xmlSpace_7()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteStartElement("node", null);
                            w.WriteAttributeString("xml", "space", null, "reserve");
                        }
                        catch (ArgumentException e)
                        {
                            CError.WriteLineIgnore(e.ToString());
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return TEST_PASS;
                        }
                    }
                    CError.WriteLine("Exception expected");
                    return TEST_FAIL;
                }

                //[Variation(id = 8, Desc = "Duplicate xml:space attr should error", Pri = 1)]
                public int xmlSpace_8()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "space", null, "preserve");
                            w.WriteAttributeString("xml", "space", null, "default");
                        }
                        catch (XmlException e)
                        {
                            CError.WriteLineIgnore(e.ToString());
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return TEST_PASS;
                        }
                    }
                    CError.WriteLine("Exception expected");
                    return TEST_FAIL;
                }

                //[Variation(id = 9, Desc = "Veify XmlSpace value when received through WriteString", Pri = 1)]
                public int xmlSpace_9()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("xml", "space", null);
                        w.WriteString("default");
                        w.WriteEndAttribute();

                        CError.Compare(w.XmlSpace, XmlSpace.Default, "Error");
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }
            }

            //[TestCase(Name = "XmlLang")]
            public partial class TCXmlLang : XmlWriterTestCaseBase
            {
                //[Variation(id = 1, Desc = "Verify XmlLang sanity test", Pri = 0)]
                public int XmlLang_1()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartElement("node", null);
                        w.WriteAttributeString("xml", "lang", null, "en");

                        CError.Compare(w.XmlLang, "en", "Error");

                        w.WriteEndElement();
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 2, Desc = "Verify that default value of XmlLang is NULL", Pri = 1)]
                public int XmlLang_2()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        if (w.XmlLang != null)
                        {
                            w.Dispose();
                            CError.WriteLine("Default value if no xml:lang attributes are currentlly on the stack should be null");
                            CError.WriteLine("Actual value: {0}", w.XmlLang.ToString());
                            return TEST_FAIL;
                        }
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 3, Desc = "Verify XmlLang scope inside nested elements (both PROLOG and EPILOG)", Pri = 1)]
                public int XmlLang_3()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");

                        w.WriteStartElement("node", null);
                        w.WriteAttributeString("xml", "lang", null, "fr");
                        CError.Compare(w.XmlLang, "fr", "Error");

                        w.WriteStartElement("node1");
                        w.WriteAttributeString("xml", "lang", null, "en-US");
                        CError.Compare(w.XmlLang, "en-US", "Error");

                        w.WriteStartElement("node2");
                        CError.Compare(w.XmlLang, "en-US", "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlLang, "en-US", "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlLang, "fr", "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlLang, null, "Error");
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 4, Desc = "Duplicate xml:lang attr should error", Pri = 1)]
                public int XmlLang_4()
                {
                    /*if (WriterType == WriterType.XmlTextWriter)
                        return TEST_SKIPPED;*/

                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "lang", null, "en-us");
                            w.WriteAttributeString("xml", "lang", null, "ja");
                        }
                        catch (XmlException e)
                        {
                            CError.WriteLineIgnore(e.ToString());
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return TEST_PASS;
                        }
                    }
                    CError.WriteLine("Exception expected");
                    return TEST_FAIL;
                }

                //[Variation(id = 5, Desc = "Veify XmlLang value when received through WriteAttributes", Pri = 1)]
                public int XmlLang_5()
                {
                    XmlReaderSettings xrs = new XmlReaderSettings();
                    xrs.IgnoreWhitespace = true;
                    XmlReader tr = XmlReader.Create(FilePathUtil.getStream(FullPath("XmlReader.xml")), xrs);

                    while (tr.Read())
                    {
                        if (tr.LocalName == "XmlLangNode")
                        {
                            tr.Read();
                            tr.MoveToNextAttribute();
                            break;
                        }
                    }

                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributes(tr, false);

                        CError.Compare(w.XmlLang, "fr", "Error");
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 6, Desc = "Veify XmlLang value when received through WriteString")]
                public int XmlLang_6()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("xml", "lang", null);
                        w.WriteString("en-US");
                        w.WriteEndAttribute();

                        CError.Compare(w.XmlLang, "en-US", "Error");
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 7, Desc = "Should not check XmlLang value", Pri = 2)]
                public int XmlLang_7()
                {
                    string[] langs = new string[] { "en-", "e n", "en", "en-US", "e?", "en*US" };

                    for (int i = 0; i < langs.Length; i++)
                    {
                        using (XmlWriter w = CreateWriter())
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "lang", null, langs[i]);
                            w.WriteEndElement();
                        }

                        string strExp = "<Root xml:lang=\"" + langs[i] + "\" />";
                        if (!CompareReader(strExp))
                            return TEST_FAIL;
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 8, Desc = "More XmlLang with valid sequence", Pri = 1)]
                public int XmlLang_8()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "lang", null, "U.S.A.");
                    }
                    return TEST_PASS;
                }
            }

            //[TestCase(Name = "WriteRaw")]
            public partial class TCWriteRaw : TCWriteBuffer
            {
                //[Variation(id = 1, Desc = "Call both WriteRaw Methods", Pri = 1)]
                public int writeRaw_1()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        string t = "Test Case";
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("a");
                        w.WriteRaw(t);
                        w.WriteStartAttribute("b");
                        w.WriteRaw(t.ToCharArray(), 0, 4);
                        w.WriteEndElement();
                    }
                    return CompareReader("<Root a=\"Test Case\" b=\"Test\" />") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 2, Desc = "WriteRaw with entites and entitized characters", Pri = 1)]
                public int writeRaw_2()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        String t = "<node a=\"&'b\">\" c=\"'d\">&</node>";

                        w.WriteStartElement("Root");
                        w.WriteRaw(t);
                        w.WriteEndElement();
                    }

                    string strExp = "<Root><node a=\"&'b\">\" c=\"'d\">&</node></Root>";

                    return CompareString(strExp) ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 3, Desc = "WriteRaw with entire Xml Document in string", Pri = 1)]
                public int writeRaw_3()
                {
                    XmlWriter w = CreateWriter();
                    String t = "<root><node1></node1><node2></node2></root>";

                    w.WriteRaw(t);

                    w.Dispose();
                    return CompareReader("<root><node1></node1><node2></node2></root>") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 4, Desc = "Call WriteRaw to write the value of xml:space")]
                public int writeRaw_4()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("xml", "space", null);
                        w.WriteRaw("default");
                        w.WriteEndAttribute();
                        w.WriteEndElement();
                    }
                    return CompareReader("<Root xml:space=\"default\" />") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 5, Desc = "Call WriteRaw to write the value of xml:lang", Pri = 1)]
                public int writerRaw_5()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        string strraw = "abc";
                        char[] buffer = strraw.ToCharArray();

                        w.WriteStartElement("root");
                        w.WriteStartAttribute("xml", "lang", null);
                        w.WriteRaw(buffer, 1, 1);
                        w.WriteRaw(buffer, 0, 2);
                        w.WriteEndAttribute();
                        w.WriteEndElement();
                    }
                    return CompareReader("<root xml:lang=\"bab\" />") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 6, Desc = "WriteRaw with count > buffer size", Pri = 1)]
                public int writeRaw_6()
                {
                    return VerifyInvalidWrite("WriteRaw", 5, 0, 6, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                //[Variation(id = 7, Desc = "WriteRaw with count < 0", Pri = 1)]
                public int writeRaw_7()
                {
                    return VerifyInvalidWrite("WriteRaw", 5, 2, -1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(id = 8, Desc = "WriteRaw with index > buffer size", Pri = 1)]
                public int writeRaw_8()
                {
                    return VerifyInvalidWrite("WriteRaw", 5, 6, 1, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                //[Variation(id = 9, Desc = "WriteRaw with index < 0", Pri = 1)]
                public int writeRaw_9()
                {
                    return VerifyInvalidWrite("WriteRaw", 5, -1, 1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(id = 10, Desc = "WriteRaw with index + count exceeds buffer", Pri = 1)]
                public int writeRaw_10()
                {
                    return VerifyInvalidWrite("WriteRaw", 5, 2, 5, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                //[Variation(id = 11, Desc = "WriteRaw with buffer = null", Pri = 1)]
                public int writeRaw_11()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteRaw(null, 0, 0);
                        }
                        catch (ArgumentNullException)
                        {
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return TEST_PASS;
                        }
                    }
                    CError.WriteLine("Did not throw exception");
                    return TEST_FAIL;
                }

                //[Variation(id = 12, Desc = "WriteRaw with valid surrogate pair", Pri = 1)]
                public int writeRaw_12()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");

                        string str = "\uD812\uDD12";
                        char[] chr = str.ToCharArray();

                        w.WriteRaw(str);
                        w.WriteRaw(chr, 0, chr.Length);
                        w.WriteEndElement();
                    }
                    string strExp = "<Root>\uD812\uDD12\uD812\uDD12</Root>";
                    return CompareReader(strExp) ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 13, Desc = "WriteRaw with invalid surrogate pair", Pri = 1)]
                public int writeRaw_13()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteRaw("\uD812");
                        }
                        catch (ArgumentException e)
                        {
                            CError.WriteLineIgnore(e.ToString());
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return TEST_PASS;
                        }
                    }
                    CError.WriteLine("Did not throw exception");
                    return TEST_FAIL;
                }

                //[Variation(id = 14, Desc = "Index = Count = 0", Pri = 1)]
                public int writeRaw_14()
                {
                    string lang = new String('a', 1);
                    char[] buffer = lang.ToCharArray();

                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("root");
                        w.WriteStartAttribute("xml", "lang", null);
                        w.WriteRaw(buffer, 0, 0);
                        w.WriteEndElement();
                    }
                    return CompareReader("<root xml:lang=\"\" />") ? TEST_PASS : TEST_FAIL;
                }
            }

            //[TestCase(Name = "WriteBase64")]
            public partial class TCWriteBase64 : TCWriteBuffer
            {
                // Base64LineSize = 76, test around this boundary size
                //[Variation(id = 10, Desc = "Call WriteBase64 with 75 chars", Pri = 0, Param = "75")]
                //[Variation(id = 11, Desc = "Call WriteBase64 with 76 chars", Pri = 0, Param = "76")]
                //[Variation(id = 12, Desc = "Call WriteBase64 with 77 chars", Pri = 0, Param = "77")]
                //[Variation(id = 13, Desc = "Call WriteBase64 with 1024 chars", Pri = 0, Param = "1024")]
                //[Variation(id = 14, Desc = "Call WriteBase64 with 4*1024 chars", Pri = 0, Param = "4096")]
                public int Base64_1()
                {
                    String strBase64 = String.Empty;
                    int strBase64Len = Int32.Parse(CurVariation.Param.ToString());
                    for (int i = 0; i < strBase64Len; i++)
                    {
                        strBase64 += "A";
                    }

                    byte[] Wbase64 = new byte[strBase64Len * 2];
                    int Wbase64len = 0;

                    for (int i = 0; i < strBase64.Length; i++)
                    {
                        WriteToBuffer(ref Wbase64, ref Wbase64len, System.BitConverter.GetBytes(strBase64[i]));
                    }

                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("root");
                        w.WriteBase64(Wbase64, 0, (int)Wbase64len);
                        w.WriteEndElement();
                    }

                    XmlReader r = GetReader();
                    r.Read();
                    byte[] buffer = new byte[strBase64Len * 2];
                    int nRead = r.ReadElementContentAsBase64(buffer, 0, strBase64Len * 2);
                    r.Dispose();

                    CError.Compare(nRead, strBase64Len * 2, "Read count");

                    string strRes = String.Empty;
                    for (int i = 0; i < nRead; i += 2)
                    {
                        strRes += BitConverter.ToChar(buffer, i);
                    }
                    CError.Compare(strRes, strBase64, "Base64 value");

                    return TEST_PASS;
                }

                //[Variation(id = 20, Desc = "WriteBase64 with count > buffer size", Pri = 1)]
                public int Base64_2()
                {
                    return VerifyInvalidWrite("WriteBase64", 5, 0, 6, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                //[Variation(id = 30, Desc = "WriteBase64 with count < 0", Pri = 1)]
                public int Base64_3()
                {
                    return VerifyInvalidWrite("WriteBase64", 5, 2, -1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(id = 40, Desc = "WriteBase64 with index > buffer size", Pri = 1)]
                public int Base64_4()
                {
                    return VerifyInvalidWrite("WriteBase64", 5, 5, 1, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                //[Variation(id = 50, Desc = "WriteBase64 with index < 0", Pri = 1)]
                public int Base64_5()
                {
                    return VerifyInvalidWrite("WriteBase64", 5, -1, 1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(id = 60, Desc = "WriteBase64 with index + count exceeds buffer", Pri = 1)]
                public int Base64_6()
                {
                    return VerifyInvalidWrite("WriteBase64", 5, 2, 5, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                //[Variation(id = 70, Desc = "WriteBase64 with buffer = null", Pri = 1)]
                public int Base64_7()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteBase64(null, 0, 0);
                        }
                        catch (ArgumentNullException)
                        {
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return TEST_PASS;
                        }
                    }
                    CError.WriteLine("Did not throw exception");
                    return TEST_FAIL;
                }

                //[Variation(id = 80, Desc = "Index = Count = 0", Pri = 1)]
                public int Base64_8()
                {
                    byte[] buffer = new byte[10];

                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("root");
                        w.WriteStartAttribute("foo");
                        w.WriteBase64(buffer, 0, 0);
                        w.WriteEndElement();
                    }
                    return CompareReader("<root foo=\"\" />") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 90, Desc = "Base64 should not be allowed inside xml:lang value", Pri = 1, Param = "lang")]
                //[Variation(id = 91, Desc = "Base64 should not be allowed inside xml:space value", Pri = 1, Param = "space")]
                //[Variation(id = 92, Desc = "Base64 should not be allowed inside namespace decl", Pri = 1, Param = "ns")]
                public int Base64_9()
                {
                    byte[] buffer = new byte[10];

                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            switch (CurVariation.Param.ToString())
                            {
                                case "lang":
                                    w.WriteStartAttribute("xml", "lang", null);
                                    break;
                                case "space":
                                    w.WriteStartAttribute("xml", "space", null);
                                    break;
                                case "ns":
                                    w.WriteStartAttribute("xmlns", "foo", null);
                                    break;
                            }
                            w.WriteBase64(buffer, 0, 5);
                        }
                        catch (InvalidOperationException)
                        {
                            return TEST_PASS;
                        }
                    }

                    CError.WriteLine("Did not throw exception");
                    return TEST_FAIL;
                }

                //[Variation(id = 94, Desc = "WriteBase64 should flush the buffer if WriteString is called", Pri = 1)]
                public int Base64_11()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("fromname");
                        w.WriteString("=?gb2312?B?");
                        w.Flush();
                        byte[] bytesFrom = new byte[] { 1, 2 };
                        w.WriteBase64(bytesFrom, 0, bytesFrom.Length);
                        w.Flush();
                        w.WriteString("?=");
                        w.Flush();
                        w.WriteEndElement();
                    }

                    string strExp = "<fromname>=?gb2312?B?AQI=?=</fromname>";
                    CompareString(strExp);
                    return TEST_PASS;
                }

                //[Variation(id = 95, Desc = "XmlWriter.WriteBase64 inserts new lines where they should not be...", Pri = 1)]
                public int Base64_12()
                {
                    byte[][] byteArrays = new byte[][]
                {
                    new byte[] {0xd8,0x7e,0x8d,0xf9,0x84,0x06,0x4a,0x67,0x93,0xba,0xc1,0x0d,0x16,0x53,0xb2,0xcc,0xbb,0x03,0xe3,0xf9},
                    new byte[] {
                        0xaa,
                        0x48,
                        0x60,
                        0x49,
                        0xa1,
                        0xb4,
                        0xa2,
                        0xe4,
                        0x65,
                        0x74,
                        0x5e,
                        0xc8,
                        0x84,
                        0x33,
                        0xae,
                        0x6a,
                        0xe3,
                        0xb5,
                        0x2f,
                        0x8c,
                    },
                    new byte[] {
                        0x46,
                        0xe4,
                        0xf9,
                        0xb9,
                        0x3e,
                        0xb6,
                        0x6b,
                        0x3f,
                        0xf9,
                        0x01,
                        0x67,
                        0x5b,
                        0xf5,
                        0x2c,
                        0xfd,
                        0xe6,
                        0x8e,
                        0x52,
                        0xc4,
                        0x1b,
                    },
                    new byte[] {
                        0x55,
                        0xca,
                        0x97,
                        0xfb,
                        0xaa,
                        0xc6,
                        0x9a,
                        0x69,
                        0xa0,
                        0x2e,
                        0x1f,
                        0xa7,
                        0xa9,
                        0x3c,
                        0x62,
                        0xe9,
                        0xa1,
                        0xf3,
                        0x0a,
                        0x07,
                    },
                    new byte[] {
                        0x28,
                        0x82,
                        0xb7,
                        0xbe,
                        0x49,
                        0x45,
                        0x37,
                        0x54,
                        0x26,
                        0x31,
                        0xd4,
                        0x24,
                        0xa6,
                        0x5a,
                        0xb6,
                        0x6b,
                        0x37,
                        0xf3,
                        0xaf,
                        0x38,
                    },
                    new byte[] {
                        0xdd,
                        0xbd,
                        0x3f,
                        0x8f,
                        0xd5,
                        0xeb,
                        0x5b,
                        0xcc,
                        0x9d,
                        0xdd,
                        0x00,
                        0xba,
                        0x90,
                        0x76,
                        0x4c,
                        0xcb,
                        0xd3,
                        0xd5,
                        0xfa,
                        0xd2,
                    }
             };

                    XmlWriter writer = CreateWriter();
                    writer.WriteStartElement("Root");
                    for (int i = 0; i < byteArrays.Length; i++)
                    {
                        writer.WriteStartElement("DigestValue");
                        byte[] bytes = byteArrays[i];
                        writer.WriteBase64(bytes, 0, bytes.Length);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.Dispose();

                    return CompareBaseline("bug364698.xml") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 96, Desc = "XmlWriter does not flush Base64 data on the Close", Pri = 1)]
                public int Base64_13()
                {
                    byte[] data = new byte[] { 60, 65, 47, 62 }; // <A/>

                    XmlWriterSettings ws = new XmlWriterSettings();
                    ws.ConformanceLevel = ConformanceLevel.Fragment;

                    StringBuilder sb = new StringBuilder();
                    using (XmlWriter w = WriterHelper.Create(sb, ws))
                    {
                        w.WriteBase64(data, 0, data.Length);
                    }


                    String s = sb.ToString();
                    if (String.Compare(s, "PEEvPg==") != 0)
                    {
                        CError.WriteLine("Unexpected output : {0}", s);
                        return TEST_FAIL;
                    }

                    return TEST_PASS;
                }
            }

            //[TestCase(Name = "WriteBinHex")]
            public partial class TCWriteBinHex : TCWriteBuffer
            {
                //[Variation(id = 1, Desc = "Call WriteBinHex with correct byte, index, and count", Pri = 0)]
                public int BinHex_1()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("root");

                        string str = "abcdefghijk1234567890";
                        byte[] buffer = StringToByteArray(str);
                        w.WriteBinHex(buffer, 0, str.Length * 2);
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 2, Desc = "WriteBinHex with count > buffer size", Pri = 1)]
                public int BinHex_2()
                {
                    return VerifyInvalidWrite("WriteBinHex", 5, 0, 6, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                //[Variation(id = 3, Desc = "WriteBinHex with count < 0", Pri = 1)]
                public int BinHex_3()
                {
                    return VerifyInvalidWrite("WriteBinHex", 5, 2, -1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(id = 4, Desc = "WriteBinHex with index > buffer size", Pri = 1)]
                public int BinHex_4()
                {
                    return VerifyInvalidWrite("WriteBinHex", 5, 5, 1, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                //[Variation(id = 5, Desc = "WriteBinHex with index < 0", Pri = 1)]
                public int BinHex_5()
                {
                    return VerifyInvalidWrite("WriteBinHex", 5, -1, 1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(id = 6, Desc = "WriteBinHex with index + count exceeds buffer", Pri = 1)]
                public int BinHex_6()
                {
                    return VerifyInvalidWrite("WriteBinHex", 5, 2, 5, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                //[Variation(id = 7, Desc = "WriteBinHex with buffer = null", Pri = 1)]
                public int BinHex_7()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteBinHex(null, 0, 0);
                        }
                        catch (ArgumentNullException)
                        {
                            if (WriterType == WriterType.CustomWriter)
                            {
                                CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                            }
                            else
                            {
                                CheckErrorState(w.WriteState);
                            }
                            return TEST_PASS;
                        }
                    }
                    CError.WriteLine("Did not throw exception");
                    return TEST_FAIL;
                }

                //[Variation(id = 8, Desc = "Index = Count = 0", Pri = 1)]
                public int BinHex_8()
                {
                    byte[] buffer = new byte[10];

                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("root");
                        w.WriteStartAttribute("xml", "lang", null);
                        w.WriteBinHex(buffer, 0, 0);
                        w.WriteEndElement();
                    }
                    return CompareReader("<root xml:lang=\"\" />") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 9, Desc = "Call WriteBinHex as an attribute value", Pri = 1)]
                public int BinHex_9()
                {
                    String strBinHex = "abc";
                    byte[] Wbase64 = new byte[2000];
                    int/*uint*/ Wbase64len = 0;

                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("root");
                        w.WriteStartAttribute("a", null);
                        for (int i = 0; i < strBinHex.Length; i++)
                        {
                            WriteToBuffer(ref Wbase64, ref Wbase64len, System.BitConverter.GetBytes(strBinHex[i]));
                        }
                        w.WriteBinHex(Wbase64, 0, (int)Wbase64len);
                        w.WriteEndElement();
                    }
                    return CompareReader("<root a='610062006300' />") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 10, Desc = "Call WriteBinHex and verify results can be read as a string", Pri = 1)]
                public int BinHex_10()
                {
                    String strBinHex = "abc";
                    byte[] Wbase64 = new byte[2000];
                    int/*uint*/ Wbase64len = 0;

                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("root");
                        for (int i = 0; i < strBinHex.Length; i++)
                        {
                            WriteToBuffer(ref Wbase64, ref Wbase64len, System.BitConverter.GetBytes(strBinHex[i]));
                        }
                        w.WriteBinHex(Wbase64, 0, (int)Wbase64len);
                        w.WriteEndElement();
                    }
                    return CompareReader("<root>610062006300</root>") ? TEST_PASS : TEST_FAIL;
                }
            }

            //[TestCase(Name = "WriteState")]
            public partial class TCWriteState : XmlWriterTestCaseBase
            {
                //[Variation(id = 1, Desc = "Verify WriteState.Start when nothing has been written yet", Pri = 0)]
                public int writeState_1()
                {
                    XmlWriter w = CreateWriter();
                    CError.Compare(w.WriteState, WriteState.Start, "Error");
                    try
                    {
                        w.Dispose();
                    }
                    catch (InvalidOperationException)
                    {
                        return TEST_FAIL;
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 2, Desc = "Verify correct state when writing in Prolog", Pri = 1)]
                public int writeState_2()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        CError.Compare(w.WriteState, WriteState.Start, "Error");
                        w.WriteDocType("Root", null, null, "<!ENTITY e \"test\">");
                        CError.Compare(w.WriteState, WriteState.Prolog, "Error");
                        w.WriteStartElement("Root");
                        CError.Compare(w.WriteState, WriteState.Element, "Error");
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 3, Desc = "Verify correct state when writing an attribute", Pri = 1)]
                public int writeState_3()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("a");
                        CError.Compare(w.WriteState, WriteState.Attribute, "Error");
                        w.WriteString("content");
                        w.WriteEndAttribute();
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 4, Desc = "Verify correct state when writing element content", Pri = 1)]
                public int writeState_4()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteString("content");
                        CError.Compare(w.WriteState, WriteState.Content, "Error");
                        w.WriteEndElement();
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 5, Desc = "Verify correct state after Close has been called", Pri = 1)]
                public int writeState_5()
                {
                    XmlWriter w = CreateWriter();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.Dispose();
                    CError.Compare(w.WriteState, WriteState.Closed, "Error");
                    return TEST_PASS;
                }

                //[Variation(id = 6, Desc = "Verify WriteState = Error after an exception", Pri = 1)]
                public int writeState_6()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteStartElement("Root");
                        }
                        catch (InvalidOperationException e)
                        {
                            CError.WriteLineIgnore(e.ToString());
                            CError.Compare(w.WriteState, WriteState.Error, "Error");
                        }
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 7, Desc = "Call WriteStartDocument after WriteState = Error", Pri = 1, Param = "WriteStartDocument")]
                //[Variation(id = 8, Desc = "Call WriteStartElement after WriteState = Error", Pri = 1, Param = "WriteStartElement")]
                //[Variation(id = 9, Desc = "Call WriteEndElement after WriteState = Error", Pri = 1, Param = "WriteEndElement")]
                //[Variation(id = 10, Desc = "Call WriteStartAttribute after WriteState = Error", Pri = 1, Param = "WriteStartAttribute")]
                //[Variation(id = 11, Desc = "Call WriteEndAttribute after WriteState = Error", Pri = 1, Param = "WriteEndAttribute")]
                //[Variation(id = 12, Desc = "Call WriteCData after WriteState = Error", Pri = 1, Param = "WriteCData")]
                //[Variation(id = 13, Desc = "Call WriteComment after WriteState = Error", Pri = 1, Param = "WriteComment")]
                //[Variation(id = 14, Desc = "Call WritePI after WriteState = Error", Pri = 1, Param = "WritePI")]
                //[Variation(id = 15, Desc = "Call WriteEntityRef after WriteState = Error", Pri = 1, Param = "WriteEntityRef")]
                //[Variation(id = 16, Desc = "Call WriteCharEntiry after WriteState = Error", Pri = 1, Param = "WriteCharEntity")]
                //[Variation(id = 17, Desc = "Call WriteSurrogateCharEntity after WriteState = Error", Pri = 1, Param = "WriteSurrogateCharEntity")]
                //[Variation(id = 18, Desc = "Call WriteWhitespace after WriteState = Error", Pri = 1, Param = "WriteWhitespace")]
                //[Variation(id = 19, Desc = "Call WriteString after WriteState = Error", Pri = 1, Param = "WriteString")]
                //[Variation(id = 20, Desc = "Call WriteChars after WriteState = Error", Pri = 1, Param = "WriteChars")]
                //[Variation(id = 21, Desc = "Call WriteRaw after WriteState = Error", Pri = 1, Param = "WriteRaw")]
                //[Variation(id = 22, Desc = "Call WriteBase64 after WriteState = Error", Pri = 1, Param = "WriteBase64")]
                //[Variation(id = 23, Desc = "Call WriteBinHex after WriteState = Error", Pri = 1, Param = "WriteBinHex")]
                //[Variation(id = 24, Desc = "Call LookupPrefix after WriteState = Error", Pri = 1, Param = "LookupPrefix")]
                //[Variation(id = 25, Desc = "Call WriteNmToken after WriteState = Error", Pri = 1, Param = "WriteNmToken")]
                //[Variation(id = 26, Desc = "Call WriteName after WriteState = Error", Pri = 1, Param = "WriteName")]
                //[Variation(id = 27, Desc = "Call WriteQualifiedName after WriteState = Error", Pri = 1, Param = "WriteQualifiedName")]
                //[Variation(id = 28, Desc = "Call WriteValue after WriteState = Error", Pri = 1, Param = "WriteValue")]
                //[Variation(id = 29, Desc = "Call WriteAttributes after WriteState = Error", Pri = 1, Param = "WriteAttributes")]
                //[Variation(id = 30, Desc = "Call WriteNode(nav) after WriteState = Error", Pri = 1, Param = "WriteNodeNavigator")]
                //[Variation(id = 31, Desc = "Call WriteNode(reader) after WriteState = Error", Pri = 1, Param = "WriteNodeReader")]
                //[Variation(id = 32, Desc = "Call Flush after WriteState = Error", Pri = 1, Param = "Flush")]
                public int writeState_7()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            CError.Equals(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                this.InvokeMethod(w, CurVariation.Param.ToString());
                            }
                            catch (InvalidOperationException)
                            {
                                CError.Equals(w.WriteState, WriteState.Error, "Error");
                                try
                                {
                                    this.InvokeMethod(w, CurVariation.Param.ToString());
                                }
                                catch (InvalidOperationException)
                                {
                                    return TEST_PASS;
                                }
                            }
                            catch (ArgumentException)
                            {
                                if (WriterType == WriterType.CustomWriter)
                                {
                                    CError.Equals(w.WriteState, WriteState.Error, "Error");
                                    try
                                    {
                                        this.InvokeMethod(w, CurVariation.Param.ToString());
                                    }
                                    catch (ArgumentException)
                                    {
                                        return TEST_PASS;
                                    }
                                }
                            }
                            // Flush/LookupPrefix is a NOOP
                            if (CurVariation.Param.ToString() == "Flush" || CurVariation.Param.ToString() == "LookupPrefix")
                                return TEST_PASS;
                        }
                    }
                    return TEST_FAIL;
                }

                //[Variation(id = 33, Desc = "XmlSpace property after WriteState = Error", Pri = 1, Param = "XmlSpace")]
                //[Variation(id = 34, Desc = "XmlLang property after WriteState = Error", Pri = 1, Param = "XmlSpace")]
                public int writeState_8()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            CError.Equals(w.WriteState, WriteState.Error, "Error");
                            switch (CurVariation.Param.ToString())
                            {
                                case "XmlSpace":
                                    CError.Equals(w.XmlSpace, XmlSpace.None, "Error");
                                    break;
                                case "XmlLang":
                                    CError.Equals(w.XmlLang, null, "Error");
                                    break;
                            }
                        }
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 6, Desc = "Call WriteStartDocument after Close()", Pri = 1, Param = "WriteStartDocument")]
                //[Variation(id = 7, Desc = "Call WriteStartElement after Close()", Pri = 1, Param = "WriteStartElement")]
                //[Variation(id = 8, Desc = "Call WriteEndElement after Close()", Pri = 1, Param = "WriteEndElement")]
                //[Variation(id = 9, Desc = "Call WriteStartAttribute after Close()", Pri = 1, Param = "WriteStartAttribute")]
                //[Variation(id = 10, Desc = "Call WriteEndAttribute after Close()", Pri = 1, Param = "WriteEndAttribute")]
                //[Variation(id = 11, Desc = "Call WriteCData after Close()", Pri = 1, Param = "WriteCData")]
                //[Variation(id = 12, Desc = "Call WriteComment after Close()", Pri = 1, Param = "WriteComment")]
                //[Variation(id = 13, Desc = "Call WritePI after Close()", Pri = 1, Param = "WritePI")]
                //[Variation(id = 14, Desc = "Call WriteEntityRef after Close()", Pri = 1, Param = "WriteEntityRef")]
                //[Variation(id = 15, Desc = "Call WriteCharEntiry after Close()", Pri = 1, Param = "WriteCharEntity")]
                //[Variation(id = 16, Desc = "Call WriteSurrogateCharEntity after Close()", Pri = 1, Param = "WriteSurrogateCharEntity")]
                //[Variation(id = 17, Desc = "Call WriteWhitespace after Close()", Pri = 1, Param = "WriteWhitespace")]
                //[Variation(id = 18, Desc = "Call WriteString after Close()", Pri = 1, Param = "WriteString")]
                //[Variation(id = 19, Desc = "Call WriteChars after Close()", Pri = 1, Param = "WriteChars")]
                //[Variation(id = 20, Desc = "Call WriteRaw after Close()", Pri = 1, Param = "WriteRaw")]
                //[Variation(id = 21, Desc = "Call WriteBase64 after Close()", Pri = 1, Param = "WriteBase64")]
                //[Variation(id = 22, Desc = "Call WriteBinHex after Close()", Pri = 1, Param = "WriteBinHex")]
                //[Variation(id = 23, Desc = "Call LookupPrefix after Close()", Pri = 1, Param = "LookupPrefix")]
                //[Variation(id = 24, Desc = "Call WriteNmToken after Close()", Pri = 1, Param = "WriteNmToken")]
                //[Variation(id = 25, Desc = "Call WriteName after Close()", Pri = 1, Param = "WriteName")]
                //[Variation(id = 26, Desc = "Call WriteQualifiedName after Close()", Pri = 1, Param = "WriteQualifiedName")]
                //[Variation(id = 27, Desc = "Call WriteValue after Close()", Pri = 1, Param = "WriteValue")]
                //[Variation(id = 28, Desc = "Call WriteAttributes after Close()", Pri = 1, Param = "WriteAttributes")]
                //[Variation(id = 29, Desc = "Call WriteNode(nav) after Close()", Pri = 1, Param = "WriteNodeNavigator")]
                //[Variation(id = 30, Desc = "Call WriteNode(reader) after Close()", Pri = 1, Param = "WriteNodeReader")]
                //[Variation(id = 31, Desc = "Call Flush after Close()", Pri = 1, Param = "Flush")]
                public int writeState_9()
                {
                    XmlWriter w = CreateWriter();
                    w.WriteElementString("root", "");
                    w.Dispose();
                    try
                    {
                        this.InvokeMethod(w, CurVariation.Param.ToString());
                    }
                    catch (InvalidOperationException)
                    {
                        try
                        {
                            this.InvokeMethod(w, CurVariation.Param.ToString());
                        }
                        catch (InvalidOperationException)
                        {
                            return TEST_PASS;
                        }
                    }
                    catch (ArgumentException)
                    {
                        if (WriterType == WriterType.CustomWriter)
                        {
                            try
                            {
                                this.InvokeMethod(w, CurVariation.Param.ToString());
                            }
                            catch (ArgumentException)
                            {
                                return TEST_PASS;
                            }
                        }
                    }
                    // Flush/LookupPrefix is a NOOP
                    if (CurVariation.Param.ToString() == "Flush" || CurVariation.Param.ToString() == "LookupPrefix")
                        return TEST_PASS;

                    return TEST_FAIL;
                }

                private void InvokeMethod(XmlWriter w, string methodName)
                {
                    byte[] buffer = new byte[10];
                    switch (methodName)
                    {
                        case "WriteStartDocument":
                            w.WriteStartDocument();
                            break;
                        case "WriteStartElement":
                            w.WriteStartElement("root");
                            break;
                        case "WriteEndElement":
                            w.WriteEndElement();
                            break;
                        case "WriteStartAttribute":
                            w.WriteStartAttribute("attr");
                            break;
                        case "WriteEndAttribute":
                            w.WriteEndAttribute();
                            break;
                        case "WriteCData":
                            w.WriteCData("test");
                            break;
                        case "WriteComment":
                            w.WriteComment("test");
                            break;
                        case "WritePI":
                            w.WriteProcessingInstruction("name", "test");
                            break;
                        case "WriteEntityRef":
                            w.WriteEntityRef("e");
                            break;
                        case "WriteCharEntity":
                            w.WriteCharEntity('c');
                            break;
                        case "WriteSurrogateCharEntity":
                            w.WriteSurrogateCharEntity('\uDC00', '\uDBFF');
                            break;
                        case "WriteWhitespace":
                            w.WriteWhitespace(" ");
                            break;
                        case "WriteString":
                            w.WriteString("foo");
                            break;
                        case "WriteChars":
                            char[] charArray = new char[] { 'a', 'b', 'c', 'd' };
                            w.WriteChars(charArray, 0, 3);
                            break;
                        case "WriteRaw":
                            w.WriteRaw("<foo>bar</foo>");
                            break;
                        case "WriteBase64":
                            w.WriteBase64(buffer, 0, 9);
                            break;
                        case "WriteBinHex":
                            w.WriteBinHex(buffer, 0, 9);
                            break;
                        case "LookupPrefix":
                            string str = w.LookupPrefix("foo");
                            break;
                        case "WriteNmToken":
                            w.WriteNmToken("foo");
                            break;
                        case "WriteName":
                            w.WriteName("foo");
                            break;
                        case "WriteQualifiedName":
                            w.WriteQualifiedName("foo", "bar");
                            break;
                        case "WriteValue":
                            w.WriteValue(Int32.MaxValue);
                            break;
                        case "WriteAttributes":
                            XmlReader xr1 = ReaderHelper.Create(new StringReader("<root attr='test'/>"));
                            xr1.Read();
                            w.WriteAttributes(xr1, false);
                            break;
                        case "WriteNodeReader":
                            XmlReader xr2 = ReaderHelper.Create(new StringReader("<root/>"));
                            xr2.Read();
                            w.WriteNode(xr2, false);
                            break;
                        case "Flush":
                            w.Flush();
                            break;
                        default:
                            CError.Equals(false, "Unexpected param in testcase: {0}", methodName);
                            break;
                    }
                }
            }

            //[TestCase(Name = "NDP20_NewMethods")]
            public partial class TC_NDP20_NewMethods : XmlWriterTestCaseBase
            {
                //[Variation(id = 1, Desc = "WriteElementString(prefix, name, ns, value) sanity test", Pri = 0)]
                public int var_1()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteElementString("foo", "elem", "bar", "test");
                    }
                    return CompareReader("<foo:elem xmlns:foo=\"bar\">test</foo:elem>") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 2, Desc = "WriteElementString(prefix = xml, ns = XML namespace)", Pri = 1)]
                public int var_2()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteElementString("xml", "elem", "http://www.w3.org/XML/1998/namespace", "test");
                    }
                    return CompareReader("<xml:elem>test</xml:elem>") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 3, Desc = "WriteStartAttribute(string name) sanity test", Pri = 0)]
                public int var_3()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        w.WriteStartElement("elem");
                        w.WriteStartAttribute("attr");
                        w.WriteEndElement();
                    }
                    return CompareReader("<elem attr=\"\" />") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 4, Desc = "WriteElementString followed by attribute should error", Pri = 1)]
                public int var_4()
                {
                    using (XmlWriter w = CreateWriter())
                    {
                        try
                        {
                            w.WriteElementString("foo", "elem", "bar", "test");
                            w.WriteStartAttribute("attr");
                        }
                        catch (InvalidOperationException)
                        {
                            return TEST_PASS;
                        }
                    }

                    return TEST_FAIL;
                }

                //[Variation(id = 5, Desc = "XmlWellformedWriter wrapping another XmlWriter should check the duplicate attributes first", Pri = 1)]
                public int var_5()
                {
                    using (XmlWriter wf = CreateWriter())
                    {
                        using (XmlWriter w = WriterHelper.Create(wf))
                        {
                            w.WriteStartElement("B");
                            w.WriteStartAttribute("aaa");
                            try
                            {
                                w.WriteStartAttribute("aaa");
                            }
                            catch (XmlException)
                            {
                                return TEST_PASS;
                            }
                        }
                    }
                    return TEST_FAIL;
                }

                //[Variation(id = 6, Desc = "XmlWriter::WriteStartDocument(true)", Pri = 1, Param = true)]
                //[Variation(id = 7, Desc = "XmlWriter::WriteStartDocument(false)", Pri = 1, Param = false)]
                public int var_6a()
                {
                    bool standalone = (bool)CurVariation.Param;
                    XmlWriterSettings ws = new XmlWriterSettings();
                    ws.ConformanceLevel = ConformanceLevel.Auto;
                    XmlWriter w = CreateWriter(ws);
                    w.WriteStartDocument(standalone);
                    w.WriteStartElement("a");

                    w.Dispose();
                    string enc = (WriterType == WriterType.UnicodeWriter || WriterType == WriterType.UnicodeWriterIndent) ? "16" : "8";
                    string param = (standalone) ? "yes" : "no";

                    string exp = (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent) ?
                        String.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\" standalone=\"{1}\"?>" + nl + "<a />", enc, param) :
                        String.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\" standalone=\"{1}\"?><a />", enc, param);

                    return (CompareString(exp)) ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 8, Desc = "Wrapped XmlWriter::WriteStartDocument(true) is missing standalone attribute", Pri = 1)]
                public int var_6b()
                {
                    XmlWriterSettings ws = new XmlWriterSettings();
                    ws.ConformanceLevel = ConformanceLevel.Auto;

                    XmlWriter wf = CreateWriter(ws);
                    XmlWriter w = WriterHelper.Create(wf);
                    w.WriteStartDocument(true);
                    w.WriteStartElement("a");

                    w.Dispose();

                    string enc = (WriterType == WriterType.UnicodeWriter || WriterType == WriterType.UnicodeWriterIndent) ? "16" : "8";
                    string exp = (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent) ?
                        String.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\"?>" + nl + "<a />", enc) :
                        String.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\"?><a />", enc);

                    exp = (WriterType == WriterType.CustomWriter) ? "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><a />" : exp;

                    return (CompareString(exp)) ? TEST_PASS : TEST_FAIL;
                }
            }

            //[TestCase(Name = "Globalization")]
            public partial class TCGlobalization : XmlWriterTestCaseBase
            {
                //[Variation(id = 1, Desc = "Characters between 0xdfff and 0xfffe are valid Unicode characters", Pri = 1)]
                public int var_1()
                {
                    string UniStr = "";
                    using (XmlWriter w = CreateWriter())
                    {
                        for (char ch = '\ue000'; ch < '\ufffe'; ch++) UniStr += ch;
                        w.WriteElementString("root", UniStr);
                    }

                    return CompareReader("<root>" + UniStr + "</root>") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 2, Desc = "XmlWriter using UTF-16BE encoding writes out wrong encoding name value in the xml decl", Pri = 1)]
                public int var_2()
                {
                    if (WriterType != WriterType.UnicodeWriter)
                        return TEST_SKIPPED;

                    Encoding enc = Encoding.GetEncoding("UTF-16BE");

                    Stream s = new MemoryStream();
                    byte[] preamble = enc.GetPreamble();
                    s.Write(preamble, 0, preamble.Length);
                    s.Flush();
                    using (StreamWriter sw = new StreamWriter(s, enc, 512, true))
                    {
                        using (XmlWriter xw = WriterHelper.Create(sw))
                        {
                            xw.WriteStartDocument();
                            xw.WriteElementString("A", "value");
                            xw.WriteEndDocument();
                        }
                    }

                    if (s.CanSeek)
                    {
                        s.Position = 0;
                    }
                    StreamReader sr = new StreamReader(s);
                    string str = sr.ReadToEnd();
                    CError.WriteLine(str);
                    return (str.Equals("<?xml version=\"1.0\" encoding=\"utf-16BE\"?><A>value</A>", StringComparison.OrdinalIgnoreCase)) ? TEST_PASS : TEST_FAIL;
                }
            }

            //[TestCase(Name = "Close()")]
            public partial class TCClose : XmlWriterTestCaseBase
            {
                //[Variation(id = 1, Desc = "Closing an XmlWriter should close all opened elements", Pri = 1)]
                public int var_1()
                {
                    using (XmlWriter writer = CreateWriter())
                    {
                        writer.WriteStartElement("Root");
                        writer.WriteStartElement("Nesting");
                        writer.WriteStartElement("SomeDeep");
                    }
                    return CompareReader("<Root><Nesting><SomeDeep /></Nesting></Root>") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 2, Desc = "Disposing an XmlWriter should close all opened elements", Pri = 1)]
                public int var_2()
                {
                    using (XmlWriter writer = CreateWriter())
                    {
                        writer.WriteStartElement("Root");
                        writer.WriteStartElement("Nesting");
                        writer.WriteStartElement("SomeDeep");
                    }
                    return CompareReader("<Root><Nesting><SomeDeep /></Nesting></Root>") ? TEST_PASS : TEST_FAIL;
                }

                //[Variation(id = 3, Desc = "Dispose() shouldn't throw when a tag is not closed and inner stream is closed", Pri = 1)]
                public int var_3()
                {
                    XmlWriter w;
                    StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
                    XmlWriterSettings s = new XmlWriterSettings();


                    switch (WriterType)
                    {
                        case WriterType.UnicodeWriter:
                            s.Encoding = Encoding.Unicode;
                            w = WriterHelper.Create(sw, s);
                            break;
                        case WriterType.UTF8Writer:
                            s.Encoding = Encoding.UTF8;
                            w = WriterHelper.Create(sw, s);
                            break;
                        case WriterType.WrappedWriter:
                            XmlWriter ww = WriterHelper.Create(sw, s);
                            w = WriterHelper.Create(ww, s);
                            break;
                        case WriterType.CharCheckingWriter:
                            s.CheckCharacters = false;
                            XmlWriter w1 = WriterHelper.Create(sw, s);
                            XmlWriterSettings ws2 = new XmlWriterSettings();
                            ws2.CheckCharacters = true;
                            w = WriterHelper.Create(w1, ws2);
                            break;
                        case WriterType.UnicodeWriterIndent:
                            s.Encoding = Encoding.Unicode;
                            s.Indent = true;
                            w = WriterHelper.Create(sw, s);
                            break;
                        case WriterType.UTF8WriterIndent:
                            s.Encoding = Encoding.UTF8;
                            s.Indent = true;
                            w = WriterHelper.Create(sw, s);
                            break;
                        default:
                            return TEST_SKIPPED;
                    }

                    w.WriteStartElement("root");

                    ((IDisposable)sw).Dispose();
                    sw = null;
                    try
                    {
                        ((IDisposable)w).Dispose();
                    }
                    catch (ObjectDisposedException e) { CError.WriteLine(e.Message); return TEST_PASS; }
                    return TEST_FAIL;
                }

                //[Variation(id = 4, Desc = "Close() should be allowed when XML doesn't have content", Pri = 1)]
                public int var_4()
                {
                    XmlWriter w = CreateWriter();
                    w.Dispose();

                    try
                    {
                        CompareReader("");
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLine(e.Message);
                        if (e.Message.EndsWith(".."))
                        {
                            return TEST_FAIL;
                        }
                        return TEST_FAIL;
                    }
                    return TEST_PASS;
                }

                //[Variation(id = 5, Desc = "XmlRawTextWriters need to call steam.Close in a finally block", Pri = 1)]
                public int var_5()
                {
                    int testResult = TEST_FAIL;

                    using (Stream tfs = new MemoryStream())
                    {
                        try
                        {
                            XmlWriterSettings ws = new XmlWriterSettings();
                            ws.CloseOutput = true;
                            XmlWriter w = WriterHelper.Create(tfs, ws);
                            w.WriteElementString("foo", "bar");
                            w.Dispose();

                            CError.WriteLine("expected exception wasn't thrown");
                            return TEST_FAIL;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        testResult = TEST_PASS;
                    }
                    return testResult;
                }

                //[Variation("Change Writer to entitize unencodable characters within raw text", Param = 1)]
                //[Variation("Change Writer to entitize unencodable characters within CDATA", Param = 2)]
                public int Bug384544()
                {
                    int param = (int)CurVariation.Param;
                    XmlWriterSettings settings = new XmlWriterSettings();

                    using (Stream strm = new MemoryStream())
                    {
                        using (XmlWriter writer = WriterHelper.Create(strm, settings))
                        {
                            try
                            {
                                writer.WriteStartElement("foo");
                                if (param == 1)
                                    writer.WriteRaw(string.Concat("218: ", (char)218, ", 32000: ", (char)32000));
                                else
                                    writer.WriteCData(string.Concat("218: ", (char)218, ", 32000: ", (char)32000));
                                writer.WriteEndElement();
                                writer.Dispose();
                            }
                            catch (Exception Ex)
                            {
                                Console.WriteLine(Ex.Message);
                                return TEST_PASS;
                            }
                        }
                    }
                    return TEST_FAIL;
                }

                //[Variation("XmlWriter: Setting Indenting to false still allows indending while writing base64 out")]
                public int SettingIndetingToFalseAllowsIndentingWhileWritingBase64()
                {
                    string base64test = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz";
                    byte[] bytesToWrite = Encoding.Unicode.GetBytes(base64test.ToCharArray());
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    XmlWriter writer = WriterHelper.Create("out.xml", settings);
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Root");
                    writer.WriteStartElement("WB64");
                    writer.WriteBase64(bytesToWrite, 0, bytesToWrite.Length);
                    writer.WriteEndElement();

                    writer.WriteStartElement("WBC64");
                    writer.WriteString(Convert.ToBase64String(bytesToWrite));
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Dispose();


                    XmlReader reader = ReaderHelper.Create("out.xml");
                    reader.ReadToFollowing("WB64");
                    string one = reader.ReadInnerXml();
                    reader.Read();
                    string two = reader.ReadInnerXml();
                    reader.Dispose();

                    return (one == two) ? TEST_PASS : TEST_FAIL;
                }

                //[Variation("WriteState returns Content even though document element has been closed")]
                public int WriteStateReturnsContentAfterDocumentClosed()
                {
                    XmlWriter xw = CreateWriter();
                    xw.WriteStartDocument(false);
                    xw.WriteStartElement("foo");
                    xw.WriteString("bar");
                    xw.WriteEndElement();
                    Console.WriteLine(xw.WriteState);

                    try
                    {
                        xw.WriteStartElement("foo2");
                        xw.Dispose();
                    }
                    catch (System.InvalidOperationException)
                    {
                        return TEST_PASS;
                    }
                    return TEST_FAIL;
                }
            }
        }
    }
}
