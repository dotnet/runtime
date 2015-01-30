// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

public class ReadAndWrite
{
    [Fact]
    public static void Write01()
    {
        Console.Write("{0}", 32);
    }

    [Fact]
    public static void Write02()
    {
        bool success = false;
        try
        {
            Console.Write("{0}", null);
            success = true;
        }
        catch (ArgumentNullException) { }

        Assert.True(success, "ArgumentNullException is thrown: Console.Write(\"{0}\", null)");
    }

    [Fact]
    public static void Write03()
    {
        // Tests Console.Write(String, Object, Object)
        Console.Write("{0} {1}", 32, "Hello");
    }

    [Fact]
    public static void Write04()
    {
        Console.Write("{0}", null, null);
    }

    [Fact]
    public static void Write05()
    {
        // Tests Console.Write(String, Object, Object, Object)
        Console.Write("{0} {1} {2}", 32, "Hello", (uint)50);
    }

    [Fact]
    public static void Write06()
    {
        Console.Write("{0}", null, null, null);
    }

    [Fact]
    public static void Write07()
    {
        // Tests Console.Write(String, Object, Object, Object, Object)
        Console.Write("{0} {1} {2} {3}", 32, "Hello", (uint)50, (ulong)5);
    }

    [Fact]
    public static void Write08()
    {
        Console.Write("{0}", null, null, null, null);
    }

    [Fact]
    public static void Write09()
    {
        // Tests Console.Write(String, Object[])
        Console.Write("{0} {1} {2} {3} {4}", 32, "Hello", (uint)50, (ulong)5, 'a');
    }

    [Fact]
    public static void Write10()
    {
        Console.Write("{0}", null, null, null, null, null);
    }

    [Fact]
    public static void Write11()
    {
        // Tests Console.Write(Boolean)
        bool fValue = true;
        Console.Write(fValue);
    }

    [Fact]
    public static void Write12()
    {
        // Tests Console.Write(Char)
        char chValue = 'a';
        Console.Write(chValue);
    }

    [Fact]
    public static void Write13()
    {
        // Tests Console.Write(Char[])
        char[] chArrValue = new Char[] { 'a', 'b', 'c', 'd', };
        Console.Write(chArrValue);
    }

    [Fact]
    public static void Write14()
    {
        // Tests Console.Write(Char[], Int32, Int32)
        char[] chArrValue = new Char[] { 'a', 'b', 'c', 'd', };
        Console.Write(chArrValue, 1, 2);
    }

    [Fact]
    public static void Write15()
    {
        // Tests Console.Write(Double)
        double dValue = 1.23;
        Console.Write(dValue);
    }

    [Fact]
    public static void Write16()
    {
        // Tests Console.Write(Decimal)
        decimal decValue = 123.456M;
        Console.Write(decValue);
    }

    [Fact]
    public static void Write17()
    {
        // Tests Console.Write(Single)
        Single sglValue = 1.234f;
        Console.Write(sglValue);
    }

    [Fact]
    public static void Write18()
    {
        // Tests Console.Write(Int32)
        int iValue = 39;
        Console.Write(iValue);
    }

    [Fact]
    public static void Write19()
    {
        // Tests Console.Write(UInt32)
        UInt32 uiValue = 50;
        Console.Write(uiValue);
    }

    [Fact]
    public static void Write20()
    {
        // Tests Console.Write(Int64)
        UInt64 lValue = 50;
        Console.Write(lValue);
    }

    [Fact]
    public static void Write21()
    {
        // Tests Console.Write(UInt64)
        UInt64 ulValue = 50;
        Console.Write(ulValue);
    }

    [Fact]
    public static void Write22()
    {
        // Tests Console.Write(Object)
        Object objValue = new Object();
        Console.Write(objValue);
    }

    [Fact]
    public static void Write23()
    {
        // Tests Console.Write(String)
        String strValue = "Hello World";
        Console.Write(strValue);
    }

    [Fact]
    public static void Write24()
    {
        // Tests WriteLine()
        Console.WriteLine();
    }

    [Fact]
    public static void Write25()
    {
        // Tests Console.WriteLine(String, Object)
        Console.WriteLine("{0}", 32);
    }

    [Fact]
    public static void Write26()
    {
        // if it is executed without any exception, it writes a new line
        bool newLineWritten = true;
        try
        {
            Console.WriteLine("{0}", null);
        }
        catch (ArgumentNullException) { }

        Assert.True(newLineWritten, "ArgumentNullException is thrown: Console.WriteLine(\"{0}\", null)");
    }

    [Fact]
    public static void Write27()
    {
        // Tests Console.WriteLine(String, Object, Object)
        Console.WriteLine("{0} {1}", 32, "Hello");
    }

    [Fact]
    public static void Write28()
    {
        Console.WriteLine("{0}", null, null);
    }

    [Fact]
    public static void Write29()
    {
        // Tests Console.WriteLine(String, Object, Object, Object)
        Console.WriteLine("{0} {1} {2}", 32, "Hello", (uint)50);
    }

    [Fact]
    public static void Write30()
    {
        Console.WriteLine("{0}", null, null, null);
    }

    [Fact]
    public static void Write31()
    {
        // Tests Console.WriteLine(String, Object, Object, Object, Object)
        Console.WriteLine("{0} {1} {2} {3}", 32, "Hello", (uint)50, (ulong)5);
    }

    [Fact]
    public static void Write32()
    {
        Console.WriteLine("{0}", null, null, null, null);
    }

    [Fact]
    public static void Write33()
    {
        // Tests Console.WriteLine(String, Object[])
        Console.WriteLine("{0} {1} {2} {3} {4}", 32, "Hello", (uint)50, (ulong)5, 'a');
    }

    [Fact]
    public static void Write34()
    {
        Console.WriteLine("{0}", null, null, null, null, null);
    }

    [Fact]
    public static void Write35()
    {
        // Tests Console.WriteLine(Boolean)
        bool fValue = true;
        Console.WriteLine(fValue);
    }

    [Fact]
    public static void Write36()
    {
        // Tests Console.WriteLine(Char)
        Char chValue = 'a';
        Console.WriteLine(chValue);
    }

    [Fact]
    public static void Write37()
    {
        // Tests Console.WriteLine(Char[])
        Char[] chArrValue = new Char[] { 'a', 'b', 'c', 'd', };
        Console.WriteLine(chArrValue);
    }

    [Fact]
    public static void Write38()
    {
        // Tests Console.WriteLine(Char[], Int32, Int32)
        Char[] chArrValue = new Char[] { 'a', 'b', 'c', 'd', };
        Console.WriteLine(chArrValue, 1, 2);
    }

    [Fact]
    public static void Write39()
    {
        // Tests Console.WriteLine(Double)
        Double dValue = 1.23;
        Console.WriteLine(dValue);
    }

    [Fact]
    public static void Write40()
    {
        // Tests Console.WriteLine(Decimal)
        Decimal decValue = 123.456M;
        Console.WriteLine(decValue);
    }

    [Fact]
    public static void Write41()
    {
        // Tests Console.WriteLine(Single)
        Single sglValue = 1.234f;
        Console.WriteLine(sglValue);
    }

    [Fact]
    public static void Write42()
    {
        // Tests Console.WriteLine(Int32)
        Int32 iValue = 39;
        Console.WriteLine(iValue);
    }

    [Fact]
    public static void Write43()
    {
        // Tests Console.WriteLine(UInt32)
        UInt32 uiValue = 50;
        Console.WriteLine(uiValue);
    }

    [Fact]
    public static void Write44()
    {
        // Tests Console.WriteLine(Int64)
        Int64 lValue = 50;
        Console.WriteLine(lValue);
    }

    [Fact]
    public static void Write45()
    {
        // Tests Console.WriteLine(UInt64)
        UInt64 ulValue = 50;
        Console.WriteLine(ulValue);
    }

    [Fact]
    public static void Write46()
    {
        // Tests Console.WriteLine(Object)
        Object objValue = new Object();
        Console.WriteLine(objValue);
    }

    [Fact]
    public static void Write47()
    {
        // Tests Console.WriteLine(String)
        String strValue = "Hello World";
        Console.WriteLine(strValue);
    }

    [Fact]
    public static void Read()
    {
        List<char> listChars = new List<char>();
        List<string> errors = new List<string>();

        TextWriter writer;
        TextReader reader;

        writer = Console.Out;
        reader = Console.In;

        StreamWriter sw = null;
        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                sw = new StreamWriter(memStream);
                sw.Write(GetInputString());

                sw.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (StreamReader sr1 = new StreamReader(memStream))
                {
                    Console.SetIn(sr1);

                    //hardcoded
                    int iCount = 108;

                    // Tests Console.Read()
                    for (int i = 0; i < iCount; i++)
                        listChars.Add((Char)Console.Read());

                    Char[] chars = new Char[listChars.Count];
                    listChars.CopyTo(chars, 0);
                    String result = new String(chars);

                    String expected = @"3232 Hello32 Hello 5032 Hello 50 532 Hello 50 5 aTrueaabcdbc1.23123.4561.23439505050System.ObjectHello World";

                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_974325sdg! Wrong value returned");
                    }

                    // Tests Console.ReadLine()
                    result = Console.ReadLine();

                    expected = String.Empty;
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_9745sg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "32";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_83745sg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = String.Empty;
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_87345nLe! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "32 Hello";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_87345sg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = String.Empty;
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_87345nLe! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "32 Hello 50";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_2975sg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = String.Empty;
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_87345nLe! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "32 Hello 50 5";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_874325sg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = String.Empty;
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_87345nLe! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "32 Hello 50 5 a";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_2085sg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = String.Empty;
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_87345nLe! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "True";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_1075sg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "a";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_93746sdg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "abcd";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_20785sg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "bc";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_9376sg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "1.23";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_7982esgf! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "123.456";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_9376tsg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "1.234";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_9745sg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "39";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_347tg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "50";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_394zg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "50";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_93756fdg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "50";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_376efg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "System.Object";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_275tsg! Wrong value returned <" + result + ">");
                    }

                    result = Console.ReadLine();

                    expected = "Hello World";
                    if (!result.Equals(expected))
                    {
                        errors.Add("Err_394zg! Wrong value returned <" + result + ">");
                    }

                    if (Console.Read() != -1)
                    {
                        errors.Add("Err_89745sgd! EOF file not reached");
                    }
                }
            }

            //revert to the standard output in case we are not the only one using the runtime currently:-)
            Console.SetOut(writer);
            Console.SetIn(reader);

            Assert.True(errors.Count == 0, String.Join(" ", errors));
        }
        finally
        {
            if (sw != null)
            {
                sw.Dispose();
            }
        }
    }

    private static string GetInputString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("3232 Hello32 Hello 5032 Hello 50 532 Hello 50 5 aTrueaabcdbc1.23123.4561.23439505050System.ObjectHello World");
        sb.AppendLine("32");
        sb.AppendLine();
        sb.AppendLine("32 Hello");
        sb.AppendLine();
        sb.AppendLine("32 Hello 50");
        sb.AppendLine();
        sb.AppendLine("32 Hello 50 5");
        sb.AppendLine();
        sb.AppendLine("32 Hello 50 5 a");
        sb.AppendLine();
        sb.AppendLine("True");
        sb.AppendLine("a");
        sb.AppendLine("abcd");
        sb.AppendLine("bc");
        sb.AppendLine("1.23");
        sb.AppendLine("123.456");
        sb.AppendLine("1.234");
        sb.AppendLine("39");
        sb.AppendLine("50");
        sb.AppendLine("50");
        sb.AppendLine("50");
        sb.AppendLine("System.Object");
        sb.AppendLine("Hello World");
        return sb.ToString();
    }
}
