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
    public static void WriteOverloads()
    {
        TextWriter savedStandardOutput = Console.Out;
        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(memStream))
                {
                    Console.SetOut(sw);
                    WriteCore();
                }
            }
        }
        finally
        {
            Console.SetOut(savedStandardOutput);
        }
    }

    [Fact]
    [OuterLoop]
    public static void WriteOverloadsToRealConsole()
    {
        WriteCore();
    }

    [Fact]
    public static void WriteLineOverloads()
    {
        TextWriter savedStandardOutput = Console.Out;
        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(memStream))
                {
                    Console.SetOut(sw);
                    WriteLineCore();
                }
            }
        }
        finally
        {
            Console.SetOut(savedStandardOutput);
        }
    }

    [Fact]
    [OuterLoop]
    public static void WriteLineOverloadsToRealConsole()
    {
        WriteLineCore();
    }

    private static void WriteCore()
    {
        // We just want to ensure none of these throw exceptions, we don't actually validate 
        // what was written.

        Console.Write("{0}", 32);
        Console.Write("{0}", null);
        Console.Write("{0} {1}", 32, "Hello");
        Console.Write("{0}", null, null);
        Console.Write("{0} {1} {2}", 32, "Hello", (uint)50);
        Console.Write("{0}", null, null, null);
        Console.Write("{0} {1} {2} {3}", 32, "Hello", (uint)50, (ulong)5);
        Console.Write("{0}", null, null, null, null);
        Console.Write("{0} {1} {2} {3} {4}", 32, "Hello", (uint)50, (ulong)5, 'a');
        Console.Write("{0}", null, null, null, null, null);
        Console.Write(true);
        Console.Write('a');
        Console.Write(new char[] { 'a', 'b', 'c', 'd', });
        Console.Write(new char[] { 'a', 'b', 'c', 'd', }, 1, 2);
        Console.Write(1.23d);
        Console.Write(123.456M);
        Console.Write(1.234f);
        Console.Write(39);
        Console.Write(50u);
        Console.Write(50L);
        Console.Write(50UL);
        Console.Write(new object());
        Console.Write("Hello World");
    }

    private static void WriteLineCore()
    {
        // We just want to ensure none of these throw exceptions, we don't actually validate 
        // what was written.

        Console.WriteLine();
        Console.WriteLine("{0}", 32);
        Console.WriteLine("{0}", null);
        Console.WriteLine("{0} {1}", 32, "Hello");
        Console.WriteLine("{0}", null, null);
        Console.WriteLine("{0} {1} {2}", 32, "Hello", (uint)50);
        Console.WriteLine("{0}", null, null, null);
        Console.WriteLine("{0} {1} {2} {3}", 32, "Hello", (uint)50, (ulong)5);
        Console.WriteLine("{0}", null, null, null, null);
        Console.WriteLine("{0} {1} {2} {3} {4}", 32, "Hello", (uint)50, (ulong)5, 'a');
        Console.WriteLine("{0}", null, null, null, null, null);
        Console.WriteLine(true);
        Console.WriteLine('a');
        Console.WriteLine(new char[] { 'a', 'b', 'c', 'd', });
        Console.WriteLine(new char[] { 'a', 'b', 'c', 'd', }, 1, 2);
        Console.WriteLine(1.23);
        Console.WriteLine(123.456M);
        Console.WriteLine(1.234f);
        Console.WriteLine(39);
        Console.WriteLine(50u);
        Console.WriteLine(50L);
        Console.WriteLine(50UL);
        Console.WriteLine(new object());
        Console.WriteLine("Hello World");
    }

    static readonly string[] s_testLines = new string[] {
        "3232 Hello32 Hello 5032 Hello 50 532 Hello 50 5 aTrueaabcdbc1.23123.4561.23439505050System.ObjectHello World",
        "32",
        "",
        "32 Hello",
        "",
        "32 Hello 50",
        "",
        "32 Hello 50 5",
        "",
        "32 Hello 50 5 a",
        "",
        "True",
        "a",
        "abcd",
        "bc",
        "1.23",
        "123.456",
        "1.234",
        "39",
        "50",
        "50",
        "50",
        "System.Object",
        "Hello World",
    };

    [Fact]
    public static void ReadAndReadLine()
    {
        TextWriter savedStandardOutput = Console.Out;
        TextReader savedStandardInput = Console.In;

        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(memStream))
                {
                    sw.WriteLine(string.Join(Environment.NewLine, s_testLines));
                    sw.Flush();

                    memStream.Seek(0, SeekOrigin.Begin);

                    using (StreamReader sr = new StreamReader(memStream))
                    {
                        Console.SetIn(sr);

                        for (int i = 0; i < s_testLines[0].Length; i++)
                        {
                            Assert.Equal(s_testLines[0][i], Console.Read());
                        }

                        // Read the newline at the end of the first line.
                        Assert.Equal("", Console.ReadLine());

                        for (int i = 1; i < s_testLines.Length; i++)
                        {
                            Assert.Equal(s_testLines[i], sr.ReadLine());
                        }

                        // We should be at EOF now.
                        Assert.Equal(-1, Console.Read());
                    }
                }
            }

        }
        finally
        {
            Console.SetOut(savedStandardOutput);
            Console.SetIn(savedStandardInput);
        }        
    }
}
