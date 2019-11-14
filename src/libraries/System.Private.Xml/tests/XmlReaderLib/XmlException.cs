// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;

namespace System.Xml.Tests
{
    [InheritRequired()]
    public abstract partial class TCXMLException : TCXMLReaderBaseGeneral
    {
        private void CompareException(XmlException e, string ec, Exception inner, int ln, int lp)
        {
            CError.WriteLine(e);
            CError.Compare((object)e.InnerException, (object)inner, "InnerException");
            CError.Compare(e.LineNumber, ln, "LineNumber");
            CError.Compare(e.LinePosition, lp, "LinePosition");
            string s1 = e.StackTrace;
            Type t = e.GetType();
            Exception e2 = e.GetBaseException();
            int i = e.GetHashCode();
            CError.Compare(!string.IsNullOrEmpty(e.Message), "Message");
            string s3 = e.ToString();
        }

        [Variation("XmlException()")]
        public int v0()
        {
            XmlException e = new XmlException();
            CompareException(e, "Xml_DefaultException", null, 0, 0);
            return TEST_PASS;
        }

        [Variation("XmlException(message)")]
        public int v1()
        {
            XmlException e = new XmlException("error");
            CompareException(e, "Xml_UserException", null, 0, 0);
            return TEST_PASS;
        }

        [Variation("XmlException(message, innerException) ")]
        public int v2()
        {
            Exception inner = new Exception();
            XmlException e = new XmlException("error", inner);
            CompareException(e, "Xml_UserException", inner, 0, 0);
            return TEST_PASS;
        }

        [Variation("XmlException(message, innerException, line, position)")]
        public int v3()
        {
            Exception inner = new Exception();
            XmlException e = new XmlException("error", inner, 14, 36);
            CompareException(e, "Xml_UserException", inner, 14, 36);
            return TEST_PASS;
        }

        [Variation("Read an element with duplicate attributes")]
        public int ReadingElementWithDuplicateAttributesThrowsException()
        {
            string strXml = "<a b='c' b='d'/>";
            try
            {
                ReloadSourceStr(strXml);
                while (DataReader.Read()) ;
            }
            catch (XmlException e)
            {
                CompareException(e, "Xml_UserException", null, 1, 10);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("Read an element with xmlns:xml namespace")]
        public int ReadingForbiddenXmlNamespaceAliasThrows()
        {
            string strXml = "<a xmlns:xml='xyz'/>";
            try
            {
                ReloadSourceStr(strXml);
                while (DataReader.Read()) ;
            }
            catch (XmlException e)
            {
                CompareException(e, "Xml_UserException", null, 1, 4);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("Inner exception on non-supported encoding")]
        public int ReadingNonSupportedEncodingDoesntThrow()
        {
            string filename = Path.Combine(TestData, "Common", "Bug81489.xml");
            ReloadSource(filename);
            while (DataReader.Read()) ;
            return TEST_PASS;
        }

        [Variation("Read an XML element whose name starts with 1")]
        public int ReadingXmlElementWithNameStartingWithNumberThrows()
        {
            try
            {
                ReloadSourceStr("<1/>");
                DataReader.Read();
            }
            catch (XmlException e)
            {
                CompareException(e, "Xml_UserException", null, 1, 2);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("XmlReader: scanner error on invalid character for an encoding give pointer to scanner buffer, not file position")]
        public int InvalidEncodingCharacterThrowsExceptionWithCorrectLineNumberAndPosition()
        {
            string filename = Path.Combine(TestData, "Common", "Bug61321.xml");
            ReloadSource(filename);

            try
            {
                while (DataReader.Read()) ;
            }
            catch (System.Xml.XmlException e)
            {
                CompareException(e, "Xml_UserException", null, 101, 69);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("XmlDocument.Load: XmlException contains ambiguous error when finding unexpected token")]
        public int UnexpectedTokenThrowsErrorWithCorrectPositions()
        {
            string filename = Path.Combine(TestData, "Common", "Bug95253.xml");
            ReloadSource(filename);
            try
            {
                while (DataReader.Read()) ;
            }
            catch (System.Xml.XmlException e)
            {
                CompareException(e, "Xml_UserException", null, 3, 5);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("Check to see if SourceUri1 is set correctly")]
        public int SourceUri1()
        {
            string filename = Path.Combine(TestData, "Common", "Bug95253.xml");
            ReloadSource(filename);

            try
            {
                while (DataReader.Read()) ;
            }
            catch (System.Xml.XmlException e)
            {
                CompareException(e, "Xml_UserException", null, 3, 5);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }
    }
}
