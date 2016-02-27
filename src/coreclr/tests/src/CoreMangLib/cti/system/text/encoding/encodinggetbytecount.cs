// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using TestLibrary;

class EncodingGetByteCount
{
    static int Main()
    {
        EncodingGetByteCount test = new EncodingGetByteCount();

        TestFramework.BeginTestCase("Encoding.GetByteCount");

        if (test.RunTests())
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("FAIL");
            return 0;
        }

    }

    public bool RunTests()
    {
        bool ret = true;

        // Positive Tests
        ret &= PositiveTestString(Encoding.UTF8, "TestString", 10, "00A");
        ret &= PositiveTestString(Encoding.UTF8, "", 0, "00B");
        ret &= PositiveTestString(Encoding.UTF8, "FooBA\u0400R", 8, "00C");
        ret &= PositiveTestString(Encoding.UTF8, "\u00C0nima\u0300l", 9, "00D");
        ret &= PositiveTestString(Encoding.UTF8, "Test\uD803\uDD75Test", 12, "00E");
        ret &= PositiveTestString(Encoding.UTF8, "Test\uD803Test", 11, "00F");
        ret &= PositiveTestString(Encoding.UTF8, "Test\uDD75Test", 11, "00G");
        ret &= PositiveTestString(Encoding.UTF8, "TestTest\uDD75", 11, "00H");
        ret &= PositiveTestString(Encoding.UTF8, "TestTest\uD803", 11, "00I");
        ret &= PositiveTestString(Encoding.UTF8, "\uDD75", 3, "00J");
        ret &= PositiveTestString(Encoding.UTF8, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", 12, "00K");
        ret &= PositiveTestString(Encoding.UTF8, "\u0130", 2, "00L");

        ret &= PositiveTestString(Encoding.Unicode, "TestString", 20, "00A3");
        ret &= PositiveTestString(Encoding.Unicode, "", 0, "00B3");
        ret &= PositiveTestString(Encoding.Unicode, "FooBA\u0400R", 14, "00C3");
        ret &= PositiveTestString(Encoding.Unicode, "\u00C0nima\u0300l", 14, "00D3");
        ret &= PositiveTestString(Encoding.Unicode, "Test\uD803\uDD75Test", 20, "00E3");
        ret &= PositiveTestString(Encoding.Unicode, "Test\uD803Test", 18, "00F3");
        ret &= PositiveTestString(Encoding.Unicode, "Test\uDD75Test", 18, "00G3");
        ret &= PositiveTestString(Encoding.Unicode, "TestTest\uDD75", 18, "00H3");
        ret &= PositiveTestString(Encoding.Unicode, "TestTest\uD803", 18, "00I3");
        ret &= PositiveTestString(Encoding.Unicode, "\uDD75", 2, "00J3");
        ret &= PositiveTestString(Encoding.Unicode, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", 12, "00K3");
        ret &= PositiveTestString(Encoding.Unicode, "\u0130", 2, "00L3");

        ret &= PositiveTestString(Encoding.BigEndianUnicode, "TestString", 20, "00A4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "", 0, "00B4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "FooBA\u0400R", 14, "00C4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "\u00C0nima\u0300l", 14, "00D4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "Test\uD803\uDD75Test", 20, "00E4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "Test\uD803Test", 18, "00F4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "Test\uDD75Test", 18, "00G4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "TestTest\uDD75", 18, "00H4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "TestTest\uD803", 18, "00I4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "\uDD75", 2, "00J4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", 12, "00K4");
        ret &= PositiveTestString(Encoding.BigEndianUnicode, "\u0130", 2, "00L4");

        ret &= PositiveTestChars(Encoding.UTF8, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, 10, "00M");
        ret &= PositiveTestChars(Encoding.Unicode, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, 20, "00M3");
        ret &= PositiveTestChars(Encoding.BigEndianUnicode, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, 20, "00M4");

        // Negative Tests
        ret &= NegativeTestString(new UTF8Encoding(), null, typeof(ArgumentNullException), "00N");
        ret &= NegativeTestString(new UnicodeEncoding(), null, typeof(ArgumentNullException), "00N3");
        ret &= NegativeTestString(new UnicodeEncoding(true, false), null, typeof(ArgumentNullException), "00N4");

        ret &= NegativeTestChars(new UTF8Encoding(), null, typeof(ArgumentNullException), "00O");
        ret &= NegativeTestChars(new UnicodeEncoding(), null, typeof(ArgumentNullException), "00O3");
        ret &= NegativeTestChars(new UnicodeEncoding(true, false), null, typeof(ArgumentNullException), "00O4");

        ret &= NegativeTestChars2(new UTF8Encoding(), null, 0, 0, typeof(ArgumentNullException), "00P");
        ret &= NegativeTestChars2(new UTF8Encoding(), new char[] { 't' }, -1, 1, typeof(ArgumentOutOfRangeException), "00P");
        ret &= NegativeTestChars2(new UTF8Encoding(), new char[] { 't' }, 1, -1, typeof(ArgumentOutOfRangeException), "00Q");
        ret &= NegativeTestChars2(new UTF8Encoding(), new char[] { 't' }, 0, 10, typeof(ArgumentOutOfRangeException), "00R");
        ret &= NegativeTestChars2(new UTF8Encoding(), new char[] { 't' }, 2, 0, typeof(ArgumentOutOfRangeException), "00S");

        ret &= NegativeTestChars2(new UnicodeEncoding(), null, 0, 0, typeof(ArgumentNullException), "00P3");
        ret &= NegativeTestChars2(new UnicodeEncoding(), new char[] { 't' }, -1, 1, typeof(ArgumentOutOfRangeException), "00P3");
        ret &= NegativeTestChars2(new UnicodeEncoding(), new char[] { 't' }, 1, -1, typeof(ArgumentOutOfRangeException), "00Q3");
        ret &= NegativeTestChars2(new UnicodeEncoding(), new char[] { 't' }, 0, 10, typeof(ArgumentOutOfRangeException), "00R3");
        ret &= NegativeTestChars2(new UnicodeEncoding(), new char[] { 't' }, 2, 0, typeof(ArgumentOutOfRangeException), "00S3");

        ret &= NegativeTestChars2(new UnicodeEncoding(true, false), null, 0, 0, typeof(ArgumentNullException), "00P4");
        ret &= NegativeTestChars2(new UnicodeEncoding(true, false), new char[] { 't' }, -1, 1, typeof(ArgumentOutOfRangeException), "00P4");
        ret &= NegativeTestChars2(new UnicodeEncoding(true, false), new char[] { 't' }, 1, -1, typeof(ArgumentOutOfRangeException), "00Q4");
        ret &= NegativeTestChars2(new UnicodeEncoding(true, false), new char[] { 't' }, 0, 10, typeof(ArgumentOutOfRangeException), "00R4");
        ret &= NegativeTestChars2(new UnicodeEncoding(true, false), new char[] { 't' }, 2, 0, typeof(ArgumentOutOfRangeException), "00S4");

        return ret;
    }

    public bool PositiveTestString(Encoding enc, string str, int expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting byte count for " + str + " with encoding " + enc.WebName);
        try
        {
            int output = enc.GetByteCount(str);
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("001", "Error in " + id + ", unexpected comparison result. Actual byte count " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool NegativeTestString(Encoding enc, string str, Type excType, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting byte count with encoding " + enc.WebName);
        try
        {
            int output = enc.GetByteCount(str);
            result = false;
            TestFramework.LogError("005", "Error in " + id + ", Expected exception not thrown. Actual byte count " + output + ", Expected exception type: " + excType.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != excType)
            {
                result = false;
                TestFramework.LogError("006", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
            }
        }
        return result;
    }

    public bool PositiveTestChars(Encoding enc, char[] chars, int expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting byte count for " + new string(chars) + " with encoding " + enc.WebName);
        try
        {
            int output = enc.GetByteCount(chars);
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("003", "Error in " + id + ", unexpected comparison result. Actual byte count " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("004", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool NegativeTestChars(Encoding enc, char[] str, Type excType, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting byte count with encoding " + enc.WebName);
        try
        {
            int output = enc.GetByteCount(str);
            result = false;
            TestFramework.LogError("007", "Error in " + id + ", Expected exception not thrown. Actual byte count " + output + ", Expected exception type: " + excType.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != excType)
            {
                result = false;
                TestFramework.LogError("008", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
            }
        }
        return result;
    }

    public bool NegativeTestChars2(Encoding enc, char[] str, int index, int count, Type excType, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting byte count with encoding " + enc.WebName);
        try
        {
            int output = enc.GetByteCount(str, index, count);
            result = false;
            TestFramework.LogError("009", "Error in " + id + ", Expected exception not thrown. Actual byte count " + output + ", Expected exception type: " + excType.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != excType)
            {
                result = false;
                TestFramework.LogError("010", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
            }
        }
        return result;
    }
}
