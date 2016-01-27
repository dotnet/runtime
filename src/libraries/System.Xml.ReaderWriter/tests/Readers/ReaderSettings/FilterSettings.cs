// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [TestCase(Name = "Filter Settings", Desc = "Filter Settings")]
    public partial class TCFilterSettings : TCXMLReaderBaseGeneral
    {
        [Variation("FilterSettings Default Values", Pri = 0)]
        public int pi00()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            if (settings.IgnoreProcessingInstructions == true)
            {
                CError.WriteLineIgnore("RS default value = true");
                return TEST_FAIL;
            }
            if (settings.IgnoreComments == true)
            {
                CError.WriteLineIgnore("RS Comm default value = true");
                return TEST_FAIL;
            }
            if (settings.IgnoreWhitespace == true)
            {
                CError.WriteLineIgnore("RS WS default value = true");
                return TEST_FAIL;
            }

            return TEST_PASS;
        }


        private int _commentCount = 0;
        private int _piCount = 0;
        private int _wsCount = 0;

        public bool CheckIgnores(XmlReader reader, bool pi, bool comm, bool ws)
        {
            if (reader == null)
                return false;

            _commentCount = 0;
            _piCount = 0;
            _wsCount = 0;

            while (reader.Read())
            {
                if (XmlNodeType.ProcessingInstruction == reader.NodeType)
                {
                    ++_piCount;
                }
                if (XmlNodeType.SignificantWhitespace == reader.NodeType)
                {
                    ++_wsCount;
                }
                if (XmlNodeType.Comment == reader.NodeType)
                {
                    ++_commentCount;
                }
            }

            if (pi && _piCount > 0)
            {
                CError.WriteLineIgnore("ProcessingInstruction Found");
                CError.WriteLineIgnore(reader.Name);
                CError.WriteLineIgnore(">" + reader.Value + "<");
                return false;
            }
            if (comm && _commentCount > 0)
            {
                CError.WriteLineIgnore("Comment Found");
                CError.WriteLineIgnore(reader.Name);
                CError.WriteLineIgnore(">" + reader.Value + "<");
                return false;
            }
            if (ws && _wsCount > 0)
            {
                CError.WriteLineIgnore("Significant Whitespace Found");
                CError.WriteLineIgnore(reader.Name);
                CError.WriteLineIgnore(">" + reader.Value + "<");
                return false;
            }
            return true;
        }

        [Variation("IgnorePI with setting true and false", Pri = 0)]
        public int pi01()
        {
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutPattern("XWEAWA/PWWPWWE/WWCCCCPPPee");
            CError.WriteLineIgnore(mn.GetNodes());
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader reader = null;

            rs.IgnoreProcessingInstructions = true;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            if (!CheckIgnores(reader, true, false, false))
                return TEST_FAIL;

            rs.IgnoreProcessingInstructions = false;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            if (!CheckIgnores(reader, false, false, false))
                return TEST_FAIL;

            return TEST_PASS;
        }

        [Variation("IgnorePI and invalid PI in XML", Pri = 0)]
        public int pi02()
        {
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutPattern("XEPAA/M");
            CError.WriteLineIgnore(mn.GetNodes());
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader reader = null;

            rs.IgnoreProcessingInstructions = true;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            bool thrown = false;
            try
            {
                CheckIgnores(reader, true, false, false);
            }
            catch (XmlException xe)
            {
                CError.WriteLineIgnore(xe.Message);
                thrown = true;
            }

            if (!thrown)
                return TEST_FAIL;

            return TEST_PASS;
        }

        [Variation("IgnorePI and XmlDecl", Pri = 2)]
        public int pi03()
        {
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutPattern("XEEAA/MTe");
            CError.WriteLineIgnore(mn.GetNodes());
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader reader = null;

            rs.IgnoreProcessingInstructions = true;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            reader.Read();

            CError.Compare(reader.NodeType, XmlNodeType.XmlDeclaration, "XmlDecl should not be ignored");

            return TEST_PASS;
        }

        [Variation("IgnoreComments and escaped end comment", Pri = 2)]
        public int c03()
        {
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutPattern("XE/");
            mn.PutText("--&gt;");
            mn.PutPattern("e");

            CError.WriteLineIgnore(mn.GetNodes());
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader reader = null;

            rs.IgnoreComments = true;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);

            while (reader.Read()) ;
            reader.Dispose();

            return TEST_PASS;
        }

        [Variation("IgnoreComments with setting true and false", Pri = 0)]
        public int c01()
        {
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutPattern("XWEAWA/PWWPWWE/WWCCCCPPPee");
            CError.WriteLineIgnore(mn.GetNodes());
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader reader = null;

            rs.IgnoreComments = true;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            if (!CheckIgnores(reader, false, true, false))
                return TEST_FAIL;

            rs.IgnoreComments = false;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            if (!CheckIgnores(reader, false, false, false))
                return TEST_FAIL;

            return TEST_PASS;
        }


        [Variation("IgnoreComments and invalid comment in XML", Pri = 0)]
        public int c02()
        {
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutPattern("CXEAA/M");
            CError.WriteLineIgnore(mn.GetNodes());
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader reader = null;

            rs.IgnoreComments = true;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            bool thrown = false;
            try
            {
                CheckIgnores(reader, false, true, false);
            }
            catch (XmlException xe)
            {
                CError.WriteLineIgnore(xe.Message);
                thrown = true;
            }

            if (!thrown)
                return TEST_FAIL;

            return TEST_PASS;
        }

        [Variation("IgnoreWhitespace with setting true and false", Pri = 0)]
        public int w01()
        {
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutPattern("XWEAWA/PWWPWWE/WWCCCCPPPee");
            CError.WriteLineIgnore(mn.GetNodes());
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader reader = null;

            rs.IgnoreWhitespace = true;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            if (!CheckIgnores(reader, false, false, true))
                return TEST_FAIL;

            rs.IgnoreWhitespace = false;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            if (!CheckIgnores(reader, false, false, false))
                return TEST_FAIL;

            return TEST_PASS;
        }


        [Variation("IgnoreWhitespace and Xml:Space=preserve", Pri = 0)]
        public int w02()
        {
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutPattern("XEA");
            mn.PutAttribute("xml:space", "preserve");
            mn.PutPattern("/PWPWE/WCCCCPPPee");

            CError.WriteLineIgnore(mn.GetNodes());
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader reader = null;

            rs.IgnoreWhitespace = true;

            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            CheckIgnores(reader, true, false, false);
            if (_wsCount != 3)
            {
                CError.WriteLineIgnore("Ws = " + _wsCount);
                return TEST_FAIL;
            }

            rs.IgnoreWhitespace = false;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            CheckIgnores(reader, false, false, false);
            if (_wsCount != 3)
            {
                CError.WriteLineIgnore("Ws = " + _wsCount);
                return TEST_FAIL;
            }

            return TEST_PASS;
        }


        [Variation("IgnoreWhitespace and invalid whitespace in XML", Pri = 0)]
        public int w03()
        {
            ManagedNodeWriter mn = new ManagedNodeWriter();
            mn.PutPattern("WXEAA/M");
            CError.WriteLineIgnore(mn.GetNodes());
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader reader = null;

            rs.IgnoreComments = true;
            reader = ReaderHelper.Create(new StringReader(mn.GetNodes()), rs, (string)null);
            bool thrown = false;
            try
            {
                CheckIgnores(reader, true, false, false);
            }
            catch (XmlException xe)
            {
                CError.WriteLineIgnore(xe.Message);
                thrown = true;
            }

            if (!thrown)
                return TEST_FAIL;

            return TEST_PASS;
        }
    }
}
