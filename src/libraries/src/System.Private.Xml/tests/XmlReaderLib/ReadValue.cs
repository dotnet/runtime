// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    /////////////////////////////////////////////////////////////////////////
    // TestCase ReadValue
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCReadValue : TCXMLReaderBaseGeneral
    {
        public const String ST_TEST_NAME = "CHARS1";
        public const String ST_GEN_ENT_NAME = "e1";
        public const String ST_GEN_ENT_VALUE = "e1foo";

        private bool VerifyInvalidReadValue(int iBufferSize, int iIndex, int iCount, Type exceptionType)
        {
            bool bPassed = false;
            Char[] buffer = new Char[iBufferSize];

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_NAME);
            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return bPassed;
                }
                catch (NotSupportedException)
                {
                    return true;
                }
            }
            try
            {
                DataReader.ReadValueChunk(buffer, iIndex, iCount);
            }
            catch (Exception e)
            {
                CError.WriteLine("Actual   exception:{0}", e.GetType().ToString());
                CError.WriteLine("Expected exception:{0}", exceptionType.ToString());
                bPassed = (e.GetType().ToString() == exceptionType.ToString());
            }

            return bPassed;
        }

        [Variation("ReadValue", Pri = 0)]
        public int TestReadValuePri0()
        {
            char[] buffer = new char[5];
            ReloadSource(new StringReader("<root>value</root>"));
            DataReader.PositionOnElement("root");
            DataReader.Read(); //This takes to text node.

            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read 5 chars");
            CError.Compare("value", new string(buffer), "Strings don't match");

            return TEST_PASS;
        }

        [Variation("ReadValue on Element", Pri = 0)]
        public int TestReadValuePri0onElement()
        {
            char[] buffer = new char[5];
            ReloadSource(new StringReader("<root>value</root>"));
            DataReader.PositionOnElement("root");

            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            try
            {
                DataReader.ReadValueChunk(buffer, 0, 5);
            }
            catch (InvalidOperationException)
            {
                return TEST_PASS;
            }

            throw new CTestFailedException("ReadValue didn't throw expected exception");
        }

        [Variation("ReadValue on Attribute", Pri = 0)]
        public int TestReadValueOnAttribute0()
        {
            char[] buffer = new char[5];
            ReloadSource(new StringReader("<root name=\"value\">value</root>"));
            DataReader.PositionOnElement("root");
            DataReader.MoveToNextAttribute(); //This takes to text node of attribute.

            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read 5 chars");
            CError.Compare("value", new string(buffer), "Strings don't match");
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 0, "Did read 5 chars");

            return TEST_PASS;
        }

        [Variation("ReadValue on Attribute after ReadAttributeValue", Pri = 2)]
        public int TestReadValueOnAttribute1()
        {
            char[] buffer = new char[5];
            ReloadSource(new StringReader("<root name=\"value\">value</root>"));
            DataReader.PositionOnElement("root");
            DataReader.MoveToNextAttribute(); //This takes to text node of attribute.

            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }
            CError.Compare(DataReader.ReadAttributeValue(), true, "Didn't read attribute value");
            CError.Compare(DataReader.Value, "value", "Didn't read correct attribute value");
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read 5 chars");
            CError.Compare("value", new string(buffer), "Strings don't match");
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 0, "Did read 5 chars");
            CError.WriteLineIgnore(DataReader.MoveToElement() + "");
            DataReader.Read();
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read 5 chars on text node");
            CError.Compare("value", new string(buffer), "Strings don't match");
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 0, "Did read 5 chars on text node");

            return TEST_PASS;
        }

        [Variation("ReadValue on empty buffer", Pri = 0)]
        public int TestReadValue2Pri0()
        {
            char[] buffer = new char[0];
            ReloadSource(new StringReader("<root>value</root>"));
            DataReader.PositionOnElement("root");
            DataReader.Read();

            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            try
            {
                DataReader.ReadValueChunk(buffer, 0, 5);
            }
            catch (ArgumentException)
            {
                return TEST_PASS;
            }

            throw new CTestFailedException("ReadValue didn't throw expected exception");
        }

        [Variation("ReadValue on negative count", Pri = 0)]
        public int TestReadValue3Pri0()
        {
            char[] buffer = new char[5];
            ReloadSource(new StringReader("<root>value</root>"));
            DataReader.PositionOnElement("root");
            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, -1);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            try
            {
                DataReader.ReadValueChunk(buffer, 0, -1);
            }
            catch (ArgumentOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestFailedException("ReadValue didn't throw expected exception");
        }

        [Variation("ReadValue on negative offset", Pri = 0)]
        public int TestReadValue4Pri0()
        {
            char[] buffer = new char[5];
            ReloadSource(new StringReader("<root>value</root>"));
            DataReader.PositionOnElement("root");
            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, -1, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }
            try
            {
                DataReader.ReadValueChunk(buffer, -1, 5);
            }
            catch (ArgumentOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestFailedException("ReadValue didn't throw expected exception");
        }

        [Variation("ReadValue with buffer = element content / 2", Pri = 0)]
        public int TestReadValue1()
        {
            Char[] buffer = new Char[5];

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_NAME);
            DataReader.Read();

            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read first 5");
            CError.Compare("01234", new string(buffer), "First strings don't match");

            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read second 5 chars");
            CError.Compare("56789", new string(buffer), "Second strings don't match");

            return TEST_PASS;
        }

        [Variation("ReadValue entire value in one call", Pri = 0)]
        public int TestReadValue2()
        {
            Char[] buffer = new Char[10];

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_NAME);
            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 10), 10, "Didn't read 10");
            CError.Compare("0123456789", new string(buffer), "Strings don't match");

            return TEST_PASS;
        }

        [Variation("ReadValue bit by bit", Pri = 0)]
        public int TestReadValue3()
        {
            Char[] buffer = new Char[10];

            ReloadSource();
            DataReader.PositionOnElement(ST_TEST_NAME);
            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            int index = 0;

            for (index = 0; index < buffer.Length; index++)
            {
                CError.Compare(DataReader.ReadValueChunk(buffer, index, 1), 1, "Read " + index);
            }

            CError.Compare("0123456789", new string(buffer), "Strings don't match");
            return TEST_PASS;
        }

        [Variation("ReadValue for value more than 4K", Pri = 0)]
        public int TestReadValue4()
        {
            int size = 8192;
            Char[] buffer = new Char[size];

            string val = new string('x', size);

            ReloadSource(new StringReader("<root>" + val + "</root>"));
            DataReader.PositionOnElement("root");
            DataReader.Read();

            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }
            int index = 0;
            for (index = 0; index < buffer.Length; index++)
            {
                CError.Compare(DataReader.ReadValueChunk(buffer, index, 1), 1, "Read " + index);
            }

            CError.Compare(val, new string(buffer), "Strings don't match");
            return TEST_PASS;
        }

        [Variation("ReadValue for value more than 4K and invalid element", Pri = 1)]
        public int TestReadValue5()
        {
            int size = 8192;
            Char[] buffer = new Char[size];

            string val = new string('x', size);

            if (IsRoundTrippedReader())
                return TEST_SKIPPED;
            ReloadSource(new StringReader("<root>" + val + "</notroot>"));
            DataReader.PositionOnElement("root");
            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            int index = 0;
            for (index = 0; index < buffer.Length; index++)
            {
                CError.Compare(DataReader.ReadValueChunk(buffer, index, 1), 1, "Read " + index);
            }

            CError.Compare(val, new string(buffer), "Strings don't match");
            try
            {
                DataReader.Read();
                return TEST_FAIL;
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
        }

        [Variation("ReadValue with count > buffer size")]
        public int TestReadValue7()
        {
            return BoolToLTMResult(VerifyInvalidReadValue(5, 0, 6, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadValue with index > buffer size")]
        public int TestReadValue8()
        {
            return BoolToLTMResult(VerifyInvalidReadValue(5, 5, 1, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadValue with index + count exceeds buffer")]
        public int TestReadValue10()
        {
            return BoolToLTMResult(VerifyInvalidReadValue(5, 2, 5, typeof(ArgumentOutOfRangeException)));
        }

        [Variation("ReadValue with combination Text, CDATA and Whitespace")]
        public int TestReadChar11()
        {
            string strExpected = "AB";
            ReloadSource();

            DataReader.PositionOnElement("CAT");
            DataReader.Read();

            char[] buffer = new char[strExpected.Length];
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            CError.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue");
            CError.Compare(new string(buffer), strExpected, "str");

            return TEST_PASS;
        }

        [Variation("ReadValue with combination Text, CDATA and SignificantWhitespace")]
        public int TestReadChar12()
        {
            string strExpected = "AB";
            ReloadSource();

            DataReader.PositionOnElement("CATMIXED");
            DataReader.Read();
            char[] buffer = new char[strExpected.Length];
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            CError.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue");
            CError.Compare(new string(buffer), strExpected, "str");

            return TEST_PASS;
        }

        [Variation("ReadValue with buffer == null")]
        public int TestReadChar13()
        {
            ReloadSource();

            DataReader.PositionOnElement("CHARS1");
            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(null, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            try
            {
                DataReader.ReadValueChunk(null, 0, 0);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }

            return TEST_FAIL;
        }

        [Variation("ReadValue with multiple different inner nodes")]
        public int TestReadChar14()
        {
            string strExpected = "somevalue";
            char[] buffer = new char[strExpected.Length];
            string strxml = "<ROOT>somevalue<![CDATA[somevalue]]>somevalue</ROOT>";
            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("ROOT");

            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue1");
            CError.Compare(new string(buffer), strExpected, "str1");

            DataReader.Read();//Now on CDATA.
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue2");
            CError.Compare(new string(buffer), strExpected, "str2");

            DataReader.Read();//Now back on Text
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue3");
            CError.Compare(new string(buffer), strExpected, "str3");

            return TEST_PASS;
        }

        [Variation("ReadValue after failed ReadValue")]
        public int TestReadChar15()
        {
            string strExpected = "somevalue";
            char[] buffer = new char[strExpected.Length];
            string strxml = "<ROOT>somevalue</ROOT>";
            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("ROOT");
            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            int nChars;
            try
            {
                nChars = DataReader.ReadValueChunk(buffer, strExpected.Length, 3);
            }
            catch (ArgumentException)
            {
                CError.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue Count");
                CError.Compare(new string(buffer), strExpected, "str");
                return TEST_PASS;
            }

            CError.WriteLine("Couldn't read after ArgumentException");
            return TEST_FAIL;
        }

        [Variation("Read after partial ReadValue")]
        public int TestReadChar16()
        {
            string strExpected = "somevalue";
            char[] buffer = new char[strExpected.Length];
            string strxml = "<ROOT>somevalue</ROOT>";
            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("ROOT");
            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            int nChars = DataReader.ReadValueChunk(buffer, 0, 2);
            CError.Compare(nChars, 2, "Read 2");

            DataReader.Read();

            CError.Compare(DataReader.VerifyNode(XmlNodeType.EndElement, "ROOT", String.Empty), "1vn");

            return TEST_PASS;
        }

        [Variation("Test error after successful ReadValue")]
        public int TestReadChar19()
        {
            if (IsRoundTrippedReader() || IsSubtreeReader()) return TEST_SKIPPED;

            Char[] buffer = new Char[9];

            ReloadSource(new StringReader("<root>somevalue</root></root>"));
            DataReader.PositionOnElement("root");
            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            int index = 0;

            for (index = 0; index < buffer.Length; index++)
            {
                CError.Compare(DataReader.ReadValueChunk(buffer, index, 1), 1, "Read " + index);
            }

            CError.Compare("somevalue", new string(buffer), "Strings don't match");

            try
            {
                while (DataReader.Read()) ;
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        //[Variation("Call on invalid element content after 4k boundary", Pri = 1, Params = new object[] { false, 1024 * 4})]
        //[Variation("Call on invalid element content after 64k boundary for Async", Pri = 1, Params = new object[] { true, 1024 * 64 })]
        public int TestReadChar21()
        {
            if (IsRoundTrippedReader()) return TEST_SKIPPED;
            bool forAsync = (bool)CurVariation.Params[0];
            if (forAsync != AsyncUtil.IsAsyncEnabled)
                return TEST_SKIPPED;

            int size = (int)CurVariation.Params[1];

            string somechar = new string('x', size);
            string strxml = String.Format("<ROOT>a" + somechar + "{0}c</ROOT>", Convert.ToChar(0));
            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("ROOT");
            char[] buffer = new char[1];
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }
            DataReader.Read();
            try
            {
                while (DataReader.ReadValueChunk(buffer, 0, 1) > 0) ;
            }
            catch (XmlException xe)
            {
                CError.WriteLineIgnore(xe.ToString());
                return TEST_PASS;
            }

            return TEST_FAIL;
        }

        [Variation("ReadValue with whitespace")]
        public int TestTextReadValue25()
        {
            string strExpected = "somevalue";
            char[] buffer = new char[strExpected.Length];
            string strxml = "<ROOT>somevalue<![CDATA[somevalue]]><test1/>    <test2/></ROOT>";
            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("ROOT");

            DataReader.Read();
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue1");
            CError.Compare(new string(buffer), strExpected, "str1");

            DataReader.Read();//Now on CDATA.
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue2");
            CError.Compare(new string(buffer), strExpected, "str2");

            DataReader.Read();//Now on test

            char[] spaces = new char[4];
            DataReader.Read();//Now on whitespace.
            CError.Compare(DataReader.ReadValueChunk(spaces, 0, spaces.Length), spaces.Length, "ReadValue3");
            CError.Compare(new string(spaces), "    ", "str3");

            return TEST_PASS;
        }

        [Variation("ReadValue when end tag doesn't exist")]
        public int TestTextReadValue26()
        {
            if (IsRoundTrippedReader()) return TEST_SKIPPED;

            char[] buffer = new char[5];
            ReloadSource(new StringReader("<root>value</notroot>"));
            DataReader.PositionOnElement("root");
            DataReader.Read(); //This takes to text node.
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read 5 chars");
            CError.Compare("value", new string(buffer), "Strings don't match");

            try
            {
                DataReader.Read();
                return TEST_FAIL;
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
        }

        [Variation("Testing with character entities")]
        public int TestCharEntities0()
        {
            char[] buffer = new char[1];
            ReloadSource(new StringReader("<root>va&lt;/root&gt;lue</root>"));
            DataReader.PositionOnElement("root");
            DataReader.Read(); //This takes to text node.
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            while (DataReader.ReadValueChunk(buffer, 0, 1) > 0) ;

            DataReader.Read();//This takes you to end element
            DataReader.Read();//This finishes the reading and sets nodetype to none.

            CError.Compare(DataReader.NodeType, XmlNodeType.None, "Not on End");

            return TEST_PASS;
        }

        [Variation("Testing with character entities when value more than 4k")]
        public int TestCharEntities1()
        {
            char[] buffer = new char[1];
            ReloadSource(new StringReader("<root>va" + new string('x', 5000) + "l&lt;/root&gt;ue</root>"));
            DataReader.PositionOnElement("root");
            DataReader.Read(); //This takes to text node.
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            while (DataReader.ReadValueChunk(buffer, 0, 1) > 0) ;

            DataReader.Read();//This takes you to end element
            DataReader.Read();//This finishes the reading and sets nodetype to none.

            CError.Compare(DataReader.NodeType, XmlNodeType.None, "Not on End");

            return TEST_PASS;
        }

        [Variation("Testing with character entities with another pattern")]
        public int TestCharEntities2()
        {
            char[] buffer = new char[1];
            ReloadSource(new StringReader("<!DOCTYPE root[<!ENTITY x \"somevalue\"><!ELEMENT root ANY>]><root>value&amp;x;</root>"));
            DataReader.PositionOnElement("root");
            DataReader.Read(); //This takes to text node.
            if (!DataReader.CanReadValueChunk)
            {
                try
                {
                    DataReader.ReadValueChunk(buffer, 0, 5);
                    return TEST_FAIL;
                }
                catch (NotSupportedException)
                {
                    return TEST_PASS;
                }
            }

            while (DataReader.ReadValueChunk(buffer, 0, 1) > 0) ;

            DataReader.Read();//This takes you to end element
            DataReader.Read();//This finishes the reading and sets nodetype to none.

            CError.Compare(DataReader.NodeType, XmlNodeType.None, "Not on End");

            return TEST_PASS;
        }

        [Variation("Testing a use case pattern with large file")]
        public int TestReadValueOnBig()
        {
            ReloadSource();

            char[] buffer = new char[1];
            while (DataReader.Read())
            {
                if (DataReader.HasValue && DataReader.CanReadValueChunk)
                {
                    Random rand = new Random();

                    if (rand.Next(1) == 1)
                    {
                        CError.WriteLineIgnore(DataReader.Value);
                    }

                    int count;
                    do
                    {
                        count = rand.Next(4) + 1;
                        buffer = new char[count];
                        if (rand.Next(1) == 1)
                        {
                            CError.WriteLineIgnore(DataReader.Value);
                            break;
                        }
                    }
                    while (DataReader.ReadValueChunk(buffer, 0, count) > 0);

                    CError.WriteLineIgnore(DataReader.Value);
                }
                else
                {
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            buffer = new char[1];
                            DataReader.ReadValueChunk(buffer, 0, 1);
                        }
                        catch (NotSupportedException)
                        {
                        }
                    }
                    else
                    {
                        try
                        {
                            buffer = new char[1];
                            DataReader.ReadValueChunk(buffer, 0, 1);
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }
            }
            return TEST_PASS;
        }

        [Variation("ReadValue on Comments with IgnoreComments")]
        public int TestReadValueOnComments0()
        {
            if (!IsFactoryReader())
                return TEST_SKIPPED;

            char[] buffer = null;

            buffer = new char[3];

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;

            MyDict<string, object> ht = new MyDict<string, object>();
            ht[ReaderFactory.HT_CURDESC] = GetDescription().ToLowerInvariant();
            ht[ReaderFactory.HT_CURVAR] = CurVariation.Desc.ToLowerInvariant();
            ht[ReaderFactory.HT_STRINGREADER] = new StringReader("<root>val<!--Comment-->ue</root>");
            ht[ReaderFactory.HT_READERSETTINGS] = settings;

            ReloadSource(ht);
            DataReader.PositionOnElement("root");
            DataReader.Read(); //This takes to text node.

            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 3), 3, "Didn't read 3 chars");
            CError.Compare("val", new string(buffer), "Strings don't match");

            buffer = new char[2];
            DataReader.Read(); //This takes to text node.
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 2), 2, "Didn't read 2 chars");
            CError.Compare("ue", new string(buffer), "Strings don't match");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("ReadValue on PI with IgnorePI")]
        public int TestReadValueOnPIs0()
        {
            if (!IsFactoryReader())
                return TEST_SKIPPED;

            char[] buffer = null;

            buffer = new char[3];

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreProcessingInstructions = true;

            MyDict<string, object> ht = new MyDict<string, object>();
            ht[ReaderFactory.HT_CURDESC] = GetDescription().ToLowerInvariant();
            ht[ReaderFactory.HT_CURVAR] = CurVariation.Desc.ToLowerInvariant();
            ht[ReaderFactory.HT_STRINGREADER] = new StringReader("<root>val<?pi target?>ue</root>");
            ht[ReaderFactory.HT_READERSETTINGS] = settings;

            ReloadSource(ht);
            DataReader.PositionOnElement("root");
            DataReader.Read(); //This takes to text node.

            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 3), 3, "Didn't read 3 chars");
            CError.Compare("val", new string(buffer), "Strings don't match");

            buffer = new char[2];
            DataReader.Read(); //This takes to text node.
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 2), 2, "Didn't read 2 chars");
            CError.Compare("ue", new string(buffer), "Strings don't match");

            while (DataReader.Read()) ;
            DataReader.Close();

            return TEST_PASS;
        }

        [Variation("Skip after ReadAttributeValue/ReadValueChunk")]
        public int SkipAfterReadAttributeValueAndReadValueChunkDoesNotThrow()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Ignore;

            XmlReader reader = XmlReader.Create(FilePathUtil.getStream(this.StandardPath + @"\XML10\ms_xml\vs084.xml"), settings);
            reader.ReadToFollowing("a");
            reader.MoveToNextAttribute();
            reader.ReadAttributeValue();
            reader.ReadValueChunk(new char[3], 0, 3); //<< removing this line works fine.
            reader.Skip();
            CError.Compare(reader.NodeType, XmlNodeType.Whitespace, "NT");
            reader.Read();
            CError.Compare(reader.NodeType, XmlNodeType.Element, "NT1");
            CError.Compare(reader.Name, "a", "Name");

            return TEST_PASS;
        }

        [Variation("ReadValueChunk - doesn't work correctly on attributes")]
        public int ReadValueChunkDoesWorkProperlyOnAttributes()
        {
            if (!IsFactoryReader())
                return TEST_SKIPPED;

            string xml = @"<root a1='12345' a2='value'/>";
            ReloadSource(new StringReader(xml));
            Char[] buffer = new Char[10];

            CError.Compare(DataReader.Read(), "Read");
            CError.Compare(DataReader.MoveToNextAttribute(), "MoveToNextAttribute");
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 3), 3, "Error");
            CError.Compare(buffer[0].ToString(), "1", "buffer1");
            CError.Compare(buffer[1].ToString(), "2", "buffer1");
            CError.Compare(buffer[2].ToString(), "3", "buffer1");
            CError.WriteLine("LineNumber" + DataReader.LineNumber);
            CError.WriteLine("LinePosition" + DataReader.LinePosition);
            CError.Compare(DataReader.MoveToNextAttribute(), "MoveToNextAttribute");
            CError.Compare(DataReader.MoveToFirstAttribute(), "MoveToFirstAttribute");
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 3), 3, "Error");
            CError.Compare(buffer[0].ToString(), "1", "buffer2");
            CError.Compare(buffer[1].ToString(), "2", "buffer2");
            CError.Compare(buffer[2].ToString(), "3", "buffer2");
            CError.WriteLine("LineNumber" + DataReader.LineNumber);
            CError.WriteLine("LinePosition" + DataReader.LinePosition);
            return TEST_PASS;
        }

        [Variation("ReadValueChunk - doesn't work correctly on special attributes")]
        public int ReadValueChunkDoesWorkProperlyOnSpecialAttributes()
        {
            if (!IsFactoryReader())
                return TEST_SKIPPED;
            string xml = @"<?xml version='1.0'?><root/>";
            ReloadSource(new StringReader(xml));
            Char[] buffer = new Char[10];

            CError.Compare(DataReader.Read(), "Read");
            CError.Compare(DataReader.MoveToFirstAttribute(), "MoveToFirstAttribute");
            CError.Compare(DataReader.Name, "version", "Name");
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 3), 3, "Error");
            CError.Compare(buffer[0].ToString(), "1", "buffer1");
            CError.Compare(buffer[1].ToString(), ".", "buffer1");
            CError.Compare(buffer[2].ToString(), "0", "buffer1");
            CError.WriteLine("LineNumber" + DataReader.LineNumber);
            CError.WriteLine("LinePosition" + DataReader.LinePosition);
            CError.Compare(DataReader.MoveToElement(), "MoveToElement");
            CError.Compare(DataReader.MoveToFirstAttribute(), "MoveToFirstAttribute");
            CError.Compare(DataReader.ReadValueChunk(buffer, 0, 3), 3, "Error");
            CError.Compare(buffer[0].ToString(), "1", "buffer2");
            CError.Compare(buffer[1].ToString(), ".", "buffer2");
            CError.Compare(buffer[2].ToString(), "0", "buffer2");
            CError.WriteLine("LineNumber" + DataReader.LineNumber);
            CError.WriteLine("LinePosition" + DataReader.LinePosition);
            return TEST_PASS;
        }

        [Variation("SubtreeReader inserted attributes don't work with ReadValueChunk")]
        public int ReadValueChunkWorksProperlyWithSubtreeReaderInsertedAttributes()
        {
            if (!IsFactoryReader())
                return TEST_SKIPPED;

            string xml = "<root xmlns='foo'><bar/></root>";
            ReloadSource(new StringReader(xml));
            DataReader.Read();
            DataReader.Read();

            using (XmlReader sr = DataReader.ReadSubtree())
            {
                sr.Read();
                sr.MoveToFirstAttribute();
                CError.WriteLine("Value: " + sr.Value);

                sr.MoveToFirstAttribute();
                char[] chars = new char[100];
                int i = sr.ReadValueChunk(chars, 0, 100);
                CError.WriteLine("ReadValueChunk: length = " + i + " value = '" + new string(chars, 0, i) + "'");
            }
            return TEST_PASS;
        }

        [Variation("goto to text node, ask got.Value, ReadValueChunk")]
        public int TestReadValue_1()
        {
            string xml = "<elem0>123</elem0>";
            ReloadSource(new StringReader(xml));
            char[] chars = new char[100];

            DataReader.Read();
            DataReader.Read();
            CError.Compare(DataReader.Value, "123", "value");
            try
            {
                CError.Compare(DataReader.ReadValueChunk(chars, 0, 1), 1, "size");
            }
            catch (NotSupportedException e) { CError.WriteLine(e); }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("goto to text node, ReadValueChunk, ask got.Value")]
        public int TestReadValue_2()
        {
            string xml = "<elem0>123</elem0>";
            ReloadSource(new StringReader(xml));
            char[] chars = new char[100];

            DataReader.Read();
            DataReader.Read();
            try
            {
                CError.Compare(DataReader.ReadValueChunk(chars, 0, 1), 1, "size");
                CError.Compare(DataReader.Value, "23", "value");
            }
            catch (NotSupportedException e) { CError.WriteLine(e); }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("goto to huge text node, read several chars with ReadValueChank and Move forward with .Read()")]
        public int TestReadValue_3()
        {
            string xml = "<elem0>123 $^ 56789 abcdefg hij klmn opqrst  12345 uvw xy ^ z</elem0>";
            ReloadSource(new StringReader(xml));
            char[] chars = new char[100];

            DataReader.Read();
            DataReader.Read();
            try
            {
                CError.Compare(DataReader.ReadValueChunk(chars, 0, 5), 5, "size");
            }
            catch (NotSupportedException e) { CError.WriteLine(e); }
            DataReader.Read();
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("goto to huge text node with invalid chars, read several chars with ReadValueChank and Move forward with .Read()")]
        public int TestReadValue_4()
        {
            string xml = "<elem0>123 $^ 56789 abcdefg hij klmn opqrst  12345 uvw xy ^ z</elem0>";
            ReloadSource(new StringReader(xml));
            char[] chars = new char[100];

            DataReader.Read();
            DataReader.Read();
            try
            {
                CError.Compare(DataReader.ReadValueChunk(chars, 0, 5), 5, "size");
                DataReader.Read();
            }
            catch (XmlException e) { CError.WriteLine(e); }
            catch (NotSupportedException e) { CError.WriteLine(e); }
            finally
            {
                DataReader.Close();
            }
            return TEST_PASS;
        }

        [Variation("Call ReadValueChunk on two or more nodes")]
        public int TestReadValue_5()
        {
            string xml = "<elem0>123<elem1>123<elem2>123</elem2>123</elem1>123</elem0>";
            ReloadSource(new StringReader(xml));

            char[] chars = new char[100];
            int startPos = 0;
            int readSize = 3;
            int currentSize = 0;

            while (DataReader.Read())
            {
                DataReader.Read();
                if (DataReader.NodeType == XmlNodeType.Text || DataReader.NodeType == XmlNodeType.None)
                    continue;
                currentSize = DataReader.ReadValueChunk(chars, startPos, readSize);
                CError.Equals(currentSize, 3, "size");
                CError.Equals(chars[0].ToString(), "1", "buffer1");
                CError.Equals(chars[1].ToString(), "2", "buffer2");
                CError.Equals(chars[2].ToString(), "3", "buffer3");
                CError.Equals(DataReader.LineNumber, 0, "LineNumber");
                CError.Equals(DataReader.LinePosition, 0, "LinePosition");
            }
            DataReader.Close();
            return TEST_PASS;
        }

        //[Variation("ReadValueChunk on an xmlns attribute", Param = "<foo xmlns='default'> <bar > id='1'/> </foo>")]
        //[Variation("ReadValueChunk on an xmlns:k attribute", Param = "<k:foo xmlns:k='default'> <k:bar id='1'/> </k:foo>")]
        //[Variation("ReadValueChunk on an xml:space attribute", Param = "<foo xml:space='default'> <bar > id='1'/> </foo>")]
        //[Variation("ReadValueChunk on an xml:lang attribute", Param = "<foo xml:lang='default'> <bar > id='1'/> </foo>")]
        public int TestReadValue_6()
        {
            string xml = (string)this.CurVariation.Param;
            ReloadSource(new StringReader(xml));
            char[] chars = new char[8];

            DataReader.Read();
            DataReader.MoveToAttribute(0);
            try
            {
                CError.Equals(DataReader.Value, "default", "value");
                CError.Equals(DataReader.ReadValueChunk(chars, 0, 8), 7, "size");
            }
            catch (NotSupportedException e) { CError.WriteLine(e); }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("Call ReadValueChunk on two or more nodes and whitespace")]
        public int TestReadValue_7()
        {
            string xml = @"<elem0>   123" + "\n" + @" <elem1>" + "\r" + @"123 
<elem2>
123  </elem2>" + "\r\n" + @"  123</elem1>          123           </elem0>";
            ReloadSource(new StringReader(xml));

            char[] chars = new char[100];
            int startPos = 0;
            int readSize = 3;
            int currentSize = 0;
            try
            {
                while (DataReader.Read())
                {
                    DataReader.Read();
                    if (DataReader.NodeType == XmlNodeType.Text || DataReader.NodeType == XmlNodeType.None)
                        continue;
                    currentSize = DataReader.ReadValueChunk(chars, startPos, readSize);
                    CError.Equals(currentSize, 3, "size");
                    CError.Equals(DataReader.LineNumber, 0, "LineNumber");
                    CError.Equals(DataReader.LinePosition, 0, "LinePosition");
                }
            }
            catch (NotSupportedException e) { CError.WriteLine(e); }
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("Call ReadValueChunk on two or more nodes and whitespace after call Value")]
        public int TestReadValue_8()
        {
            string xml = @"<elem0>   123" + "\n" + @" <elem1>" + "\r" + @"123 
<elem2>
123  </elem2>" + "\r\n" + @"  123</elem1>          123           </elem0>";
            ReloadSource(new StringReader(xml));

            char[] chars = new char[100];
            int startPos = 0;
            int readSize = 3;
            int currentSize = 0;

            try
            {
                while (DataReader.Read())
                {
                    DataReader.Read();
                    if (DataReader.NodeType == XmlNodeType.Text || DataReader.NodeType == XmlNodeType.None)
                        continue;
                    CError.Equals(DataReader.Value.Contains("123"), "Value");
                    currentSize = DataReader.ReadValueChunk(chars, startPos, readSize);
                    CError.Equals(currentSize, 3, "size");
                    CError.Equals(DataReader.LineNumber, 0, "LineNumber");
                    CError.Equals(DataReader.LinePosition, 0, "LinePosition");
                }
            }
            catch (NotSupportedException e) { CError.WriteLine(e); }
            DataReader.Close();
            return TEST_PASS;
        }
    }
}
