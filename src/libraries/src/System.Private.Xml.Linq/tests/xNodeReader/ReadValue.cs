// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using Microsoft.Test.ModuleCore;
using XmlCoreTest.Common;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class XNodeReaderTests : XLinqTestCase
        {
            //[TestCase(Name = "ReadValue", Desc = "ReadValue")]
            public partial class TCReadValue : BridgeHelpers
            {
                private bool VerifyInvalidReadValue(int iBufferSize, int iIndex, int iCount, Type exceptionType)
                {
                    bool bPassed = false;
                    Char[] buffer = new Char[iBufferSize];

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_NAME);
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
                        bPassed = (e.GetType().ToString() == exceptionType.ToString());
                        if (!bPassed)
                        {
                            TestLog.WriteLine("Actual   exception:{0}", e.GetType().ToString());
                            TestLog.WriteLine("Expected exception:{0}", exceptionType.ToString());
                        }
                    }

                    return bPassed;
                }

                //[Variation("ReadValue", Priority = 0)]
                public void TestReadValuePri0()
                {
                    char[] buffer = new char[5];
                    XmlReader DataReader = GetReader(new StringReader("<root>value</root>"));
                    PositionOnElement(DataReader, "root");
                    DataReader.Read();

                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read 5 chars");
                    TestLog.Compare("value", new string(buffer), "Strings don't match");
                }

                //[Variation("ReadValue on Element", Priority = 0)]
                public void TestReadValuePri0onElement()
                {
                    char[] buffer = new char[5];
                    XmlReader DataReader = GetReader(new StringReader("<root>value</root>"));
                    PositionOnElement(DataReader, "root");

                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    try
                    {
                        DataReader.ReadValueChunk(buffer, 0, 5);
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }

                    throw new TestFailedException("ReadValue didn't throw expected exception");
                }

                //[Variation("ReadValue on Attribute", Priority = 0)]
                public void TestReadValueOnAttribute0()
                {
                    char[] buffer = new char[5];
                    XmlReader DataReader = GetReader(new StringReader("<root name=\"value\">value</root>"));
                    PositionOnElement(DataReader, "root");
                    DataReader.MoveToNextAttribute();

                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read 5 chars");
                    TestLog.Compare("value", new string(buffer), "Strings don't match");
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 0, "Did read 5 chars");
                }

                //[Variation("ReadValue on Attribute after ReadAttributeValue", Priority = 2)]
                public void TestReadValueOnAttribute1()
                {
                    char[] buffer = new char[5];
                    XmlReader DataReader = GetReader(new StringReader("<root name=\"value\">value</root>"));
                    PositionOnElement(DataReader, "root");
                    // This takes to text node of attribute.
                    DataReader.MoveToNextAttribute();

                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }
                    TestLog.Compare(DataReader.ReadAttributeValue(), true, "Didn't read attribute value");
                    TestLog.Compare(DataReader.Value, "value", "Didn't read correct attribute value");
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read 5 chars");
                    TestLog.Compare("value", new string(buffer), "Strings don't match");
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 0, "Did read 5 chars");
                    DataReader.MoveToElement();
                    DataReader.Read();
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read 5 chars on text node");
                    TestLog.Compare("value", new string(buffer), "Strings don't match");
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 0, "Did read 5 chars on text node");
                }

                //[Variation("ReadValue on empty buffer", Priority = 0)]
                public void TestReadValue2Pri0()
                {
                    char[] buffer = new char[0];
                    XmlReader DataReader = GetReader(new StringReader("<root>value</root>"));
                    PositionOnElement(DataReader, "root");
                    DataReader.Read();

                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    try
                    {
                        DataReader.ReadValueChunk(buffer, 0, 5);
                    }
                    catch (ArgumentException)
                    {
                        return;
                    }

                    throw new TestFailedException("ReadValue didn't throw expected exception");
                }

                //[Variation("ReadValue on negative count", Priority = 0)]
                public void TestReadValue3Pri0()
                {
                    char[] buffer = new char[5];
                    XmlReader DataReader = GetReader(new StringReader("<root>value</root>"));
                    PositionOnElement(DataReader, "root");
                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, -1);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    try
                    {
                        DataReader.ReadValueChunk(buffer, 0, -1);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return;
                    }

                    throw new TestFailedException("ReadValue didn't throw expected exception");
                }

                //[Variation("ReadValue on negative offset", Priority = 0)]
                public void TestReadValue4Pri0()
                {
                    char[] buffer = new char[5];
                    XmlReader DataReader = GetReader(new StringReader("<root>value</root>"));
                    PositionOnElement(DataReader, "root");
                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, -1, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }
                    try
                    {
                        DataReader.ReadValueChunk(buffer, -1, 5);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return;
                    }

                    throw new TestFailedException("ReadValue didn't throw expected exception");
                }

                //[Variation("ReadValue with buffer = element content / 2", Priority = 0)]
                public void TestReadValue1()
                {
                    Char[] buffer = new Char[5];

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_NAME);
                    DataReader.Read();

                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read first 5");
                    TestLog.Compare("01234", new string(buffer), "First strings don't match");

                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read second 5 chars");
                    TestLog.Compare("56789", new string(buffer), "Second strings don't match");
                }

                //[Variation("ReadValue entire value in one call", Priority = 0)]
                public void TestReadValue2()
                {
                    Char[] buffer = new Char[10];

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_NAME);
                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 10), 10, "Didn't read 10");
                    TestLog.Compare("0123456789", new string(buffer), "Strings don't match");
                }

                //[Variation("ReadValue bit by bit", Priority = 0)]
                public void TestReadValue3()
                {
                    Char[] buffer = new Char[10];

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_TEST_NAME);
                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    int index = 0;

                    for (index = 0; index < buffer.Length; index++)
                    {
                        TestLog.Compare(DataReader.ReadValueChunk(buffer, index, 1), 1, "Read " + index);
                    }

                    TestLog.Compare("0123456789", new string(buffer), "Strings don't match");
                }

                //[Variation("ReadValue for value more than 4K", Priority = 0)]
                public void TestReadValue4()
                {
                    int size = 8192;
                    Char[] buffer = new Char[size];

                    string val = new string('x', size);

                    XmlReader DataReader = GetReader(new StringReader("<root>" + val + "</root>"));
                    PositionOnElement(DataReader, "root");
                    DataReader.Read();

                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }
                    int index = 0;
                    for (index = 0; index < buffer.Length; index++)
                    {
                        TestLog.Compare(DataReader.ReadValueChunk(buffer, index, 1), 1, "Read " + index);
                    }

                    TestLog.Compare(val, new string(buffer), "Strings don't match");
                }

                //[Variation("ReadValue for value more than 4K and invalid element", Priority = 1)]
                public void TestReadValue5()
                {
                    int size = 8192;
                    Char[] buffer = new Char[size];
                    string val = new string('x', size);
                    try
                    {
                        XmlReader DataReader = GetReader(new StringReader("<root>" + val + "</notroot>"));
                        PositionOnElement(DataReader, "root");
                        DataReader.Read();
                        if (!DataReader.CanReadValueChunk)
                        {
                            try
                            {
                                DataReader.ReadValueChunk(buffer, 0, 5);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (NotSupportedException)
                            {
                                return;
                            }
                        }
                        int index = 0;
                        for (index = 0; index < buffer.Length; index++)
                        {
                            TestLog.Compare(DataReader.ReadValueChunk(buffer, index, 1), 1, "Read " + index);
                        }
                        TestLog.Compare(val, new string(buffer), "Strings don't match");
                        DataReader.Read();
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                }

                //[Variation("ReadValue with Entity Reference, EntityHandling = ExpandEntities")]
                public void TestReadValue6()
                {
                    string strExpected = ST_IGNORE_ENTITIES;
                    Char[] buffer = new Char[strExpected.Length];

                    XmlReader DataReader = GetReader();
                    PositionOnElement(DataReader, ST_ENTTEST_NAME);
                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, strExpected.Length), strExpected.Length, "ReadValue1");
                    TestLog.Compare(new string(buffer), strExpected, "Str1");
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 1), 0, "ReadValue2");
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.Element, "ENTITY2", String.Empty), "Verify");
                }

                //[Variation("ReadValue with count > buffer size")]
                public void TestReadValue7()
                {
                    BoolToLTMResult(VerifyInvalidReadValue(5, 0, 6, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadValue with index > buffer size")]
                public void TestReadValue8()
                {
                    BoolToLTMResult(VerifyInvalidReadValue(5, 5, 1, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadValue with index + count exceeds buffer")]
                public void TestReadValue10()
                {
                    BoolToLTMResult(VerifyInvalidReadValue(5, 2, 5, typeof(ArgumentOutOfRangeException)));
                }

                //[Variation("ReadValue with combination Text, CDATA and Whitespace")]
                public void TestReadChar11()
                {
                    string strExpected = "AB";
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, "CAT");
                    DataReader.Read();

                    char[] buffer = new char[strExpected.Length];
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue");
                    TestLog.Compare(new string(buffer), strExpected, "str");
                }

                //[Variation("ReadValue with combination Text, CDATA and SignificantWhitespace")]
                public void TestReadChar12()
                {
                    string strExpected = "AB";
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, "CATMIXED");
                    DataReader.Read();
                    char[] buffer = new char[strExpected.Length];
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue");
                    TestLog.Compare(new string(buffer), strExpected, "str");
                }

                //[Variation("ReadValue with buffer == null")]
                public void TestReadChar13()
                {
                    XmlReader DataReader = GetReader();

                    PositionOnElement(DataReader, "CHARS1");
                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(null, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    try
                    {
                        DataReader.ReadValueChunk(null, 0, 0);
                    }
                    catch (ArgumentNullException)
                    {
                        return;
                    }

                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadValue with multiple different inner nodes")]
                public void TestReadChar14()
                {
                    string strExpected = "somevalue";
                    char[] buffer = new char[strExpected.Length];
                    string strxml = "<ROOT>somevalue<![CDATA[somevalue]]>somevalue</ROOT>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "ROOT");

                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue1");
                    TestLog.Compare(new string(buffer), strExpected, "str1");

                    // Now on CDATA.
                    DataReader.Read();
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue2");
                    TestLog.Compare(new string(buffer), strExpected, "str2");

                    // Now back on Text
                    DataReader.Read();
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue3");
                    TestLog.Compare(new string(buffer), strExpected, "str3");
                }

                //[Variation("ReadValue after failed ReadValue")]
                public void TestReadChar15()
                {
                    string strExpected = "somevalue";
                    char[] buffer = new char[strExpected.Length];
                    string strxml = "<ROOT>somevalue</ROOT>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "ROOT");
                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    int nChars;
                    try
                    {
                        nChars = DataReader.ReadValueChunk(buffer, strExpected.Length, 3);
                    }
                    catch (ArgumentException)
                    {
                        TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue Count");
                        TestLog.Compare(new string(buffer), strExpected, "str");
                        return;
                    }

                    TestLog.WriteLine("Couldn't read after ArgumentException");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("Read after partial ReadValue")]
                public void TestReadChar16()
                {
                    string strExpected = "somevalue";
                    char[] buffer = new char[strExpected.Length];
                    string strxml = "<ROOT>somevalue</ROOT>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "ROOT");
                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    int nChars = DataReader.ReadValueChunk(buffer, 0, 2);
                    TestLog.Compare(nChars, 2, "Read 2");
                    DataReader.Read();
                    TestLog.Compare(VerifyNode(DataReader, XmlNodeType.EndElement, "ROOT", String.Empty), "1vn");
                }

                //[Variation("Test error after successful ReadValue")]
                public void TestReadChar19()
                {
                    Char[] buffer = new Char[9];
                    try
                    {
                        XmlReader DataReader = GetReaderStr("<root>somevalue</root></root>");
                        PositionOnElement(DataReader, "root");
                        DataReader.Read();
                        if (!DataReader.CanReadValueChunk)
                        {
                            try
                            {
                                DataReader.ReadValueChunk(buffer, 0, 5);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (NotSupportedException)
                            {
                                return;
                            }
                        }
                        int index = 0;
                        for (index = 0; index < buffer.Length; index++)
                        {
                            TestLog.Compare(DataReader.ReadValueChunk(buffer, index, 1), 1, "Read " + index);
                        }
                        TestLog.Compare("somevalue", new string(buffer), "Strings don't match");
                        while (DataReader.Read()) ;
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("Call on invalid element content after 4k boundary", Priority = 1)]
                public void TestReadChar21()
                {
                    string somechar = new string('x', 5000);
                    string strxml = String.Format("<ROOT>a" + somechar + "{0}c</ROOT>", Convert.ToChar(0));
                    try
                    {
                        XmlReader DataReader = GetReaderStr(strxml);
                        PositionOnElement(DataReader, "ROOT");
                        char[] buffer = new char[1];
                        if (!DataReader.CanReadValueChunk)
                        {
                            try
                            {
                                DataReader.ReadValueChunk(buffer, 0, 5);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (NotSupportedException)
                            {
                                return;
                            }
                        }
                        DataReader.Read();
                        while (DataReader.ReadValueChunk(buffer, 0, 1) > 0) ;
                    }
                    catch (XmlException)
                    {
                        return;
                    }

                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation("ReadValue with whitespace")]
                public void TestTextReadValue25()
                {
                    string strExpected = "somevalue";
                    char[] buffer = new char[strExpected.Length];
                    string strxml = "<ROOT>somevalue<![CDATA[somevalue]]><test1/>    <test2/></ROOT>";
                    XmlReader DataReader = GetReaderStr(strxml);
                    PositionOnElement(DataReader, "ROOT");

                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue1");
                    TestLog.Compare(new string(buffer), strExpected, "str1");

                    // Now on CDATA.
                    DataReader.Read();
                    TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, buffer.Length), strExpected.Length, "ReadValue2");
                    TestLog.Compare(new string(buffer), strExpected, "str2");

                    // Now on test
                    DataReader.Read();

                    char[] spaces = new char[4];
                    // Now on whitespace.
                    DataReader.Read();
                    TestLog.Compare(DataReader.ReadValueChunk(spaces, 0, spaces.Length), spaces.Length, "ReadValue3");
                    TestLog.Compare(new string(spaces), "    ", "str3");
                }

                //[Variation("ReadValue when end tag doesn't exist")]
                public void TestTextReadValue26()
                {
                    char[] buffer = new char[5];
                    try
                    {
                        XmlReader DataReader = GetReaderStr("<root>value</notroot>");
                        PositionOnElement(DataReader, "root");
                        DataReader.Read();
                        if (!DataReader.CanReadValueChunk)
                        {
                            try
                            {
                                DataReader.ReadValueChunk(buffer, 0, 5);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (NotSupportedException)
                            {
                                return;
                            }
                        }
                        TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 5), 5, "Didn't read 5 chars");
                        TestLog.Compare("value", new string(buffer), "Strings don't match");
                        DataReader.Read();
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                }

                //[Variation("Testing with character entities")]
                public void TestCharEntities0()
                {
                    char[] buffer = new char[1];
                    XmlReader DataReader = GetReaderStr("<root>va&lt;/root&gt;lue</root>");
                    PositionOnElement(DataReader, "root");
                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    while (DataReader.ReadValueChunk(buffer, 0, 1) > 0) ;

                    DataReader.Read();
                    DataReader.Read();

                    TestLog.Compare(DataReader.NodeType, XmlNodeType.None, "Not on End");
                }

                //[Variation("Testing with character entities when value more than 4k")]
                public void TestCharEntities1()
                {
                    char[] buffer = new char[1];
                    XmlReader DataReader = GetReaderStr("<root>va" + new string('x', 5000) + "l&lt;/root&gt;ue</root>");

                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    while (DataReader.ReadValueChunk(buffer, 0, 1) > 0) ;

                    DataReader.Read();
                    DataReader.Read();

                    TestLog.Compare(DataReader.NodeType, XmlNodeType.None, "Not on End");
                }

                //[Variation("Testing with character entities with another pattern")]
                public void TestCharEntities2()
                {
                    char[] buffer = new char[1];
                    XmlReader DataReader = GetReaderStr("<!DOCTYPE root[<!ENTITY x \"somevalue\"><!ELEMENT root ANY>]><root>value&amp;x;</root>");

                    DataReader.Read();
                    if (!DataReader.CanReadValueChunk)
                    {
                        try
                        {
                            DataReader.ReadValueChunk(buffer, 0, 5);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (NotSupportedException)
                        {
                            return;
                        }
                    }

                    while (DataReader.ReadValueChunk(buffer, 0, 1) > 0) ;

                    DataReader.Read();
                    DataReader.Read();

                    TestLog.Compare(DataReader.NodeType, XmlNodeType.None, "Not on End");
                }

                //[Variation("Testing a use case pattern with large file")]
                public void TestReadValueOnBig()
                {
                    XmlReader DataReader = GetReader();

                    char[] buffer = new char[1];
                    while (DataReader.Read())
                    {
                        if (DataReader.HasValue && DataReader.CanReadValueChunk)
                        {
                            Random rand = new Random();

                            int count;
                            do
                            {
                                count = rand.Next(4) + 1;
                                buffer = new char[count];
                                if (rand.Next(1) == 1)
                                {
                                    break;
                                }
                            }
                            while (DataReader.ReadValueChunk(buffer, 0, count) > 0);
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
                }

                //[Variation("ReadValue on Comments with IgnoreComments")]
                public void TestReadValueOnComments0()
                {
                    char[] buffer = null;
                    buffer = new char[3];
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.IgnoreComments = true;
                    XmlReader DataReader = GetReaderStr("<root>val<!--Comment-->ue</root>");

                    DataReader.Read();
                    try
                    {
                        TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 3), 3, "Didn't read 3 chars");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (NotSupportedException) { }

                    buffer = new char[2];
                    DataReader.Read();
                    try
                    {
                        TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 2), 2, "Didn't read 2 chars");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (NotSupportedException) { }

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("ReadValue on PI with IgnorePI")]
                public void TestReadValueOnPIs0()
                {
                    char[] buffer = null;
                    buffer = new char[3];

                    XmlReader DataReader = GetReaderStr("<root>val<?pi target?>ue</root>");
                    DataReader.Read();
                    try
                    {
                        TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 3), 3, "Didn't read 3 chars");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (NotSupportedException) { }

                    buffer = new char[2];
                    DataReader.Read();
                    try
                    {
                        TestLog.Compare(DataReader.ReadValueChunk(buffer, 0, 2), 2, "Didn't read 2 chars");
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (NotSupportedException) { }

                    while (DataReader.Read()) ;
                    DataReader.Dispose();
                }

                //[Variation("Skip after ReadAttributeValue/ReadValueChunk")]
                public void bug340158()
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.DtdProcessing = DtdProcessing.Ignore;
                    XmlReader r = XmlReader.Create(FilePathUtil.getStream(Path.Combine("StandardTests", "XML10", "ms_xml", "vs084.xml")), settings);
                    XmlReader reader = GetReader(r);
                    reader.ReadToFollowing("a");
                    reader.MoveToNextAttribute();
                    reader.ReadAttributeValue();
                    try
                    {
                        reader.ReadValueChunk(new char[3], 0, 3);
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (NotSupportedException) { }
                    reader.Skip();
                    TestLog.Compare(reader.NodeType, XmlNodeType.Text, "NT");
                    reader.Read();
                    TestLog.Compare(reader.NodeType, XmlNodeType.Element, "NT1");
                    TestLog.Compare(reader.Name, "a", "Name");
                }
            }
        }
    }
}
