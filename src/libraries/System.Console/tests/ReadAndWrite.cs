// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
    public static void WriteToOutputStream_EmptyArray()
    {
        Stream outStream = Console.OpenStandardOutput();
        outStream.Write(new byte[] { }, 0, 0);
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
        Assert.Equal(Environment.NewLine, Console.Out.NewLine);
        Console.Out.NewLine = "abcd";
        Assert.Equal("abcd", Console.Out.NewLine);
        Console.Out.NewLine = Environment.NewLine;

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

    [Fact]
    public static async Task OutWriteAndWriteLineOverloads()
    {
        TextWriter savedStandardOutput = Console.Out;
        try
        {
            using (var sw = new StreamWriter(new MemoryStream()))
            {
                Console.SetOut(sw);
                TextWriter writer = Console.Out;
                Assert.NotNull(writer);
                Assert.NotEqual(writer, sw); // the writer we provide gets wrapped

                // We just want to ensure none of these throw exceptions, we don't actually validate
                // what was written.

                writer.Write("{0}", 32);
                writer.Write("{0} {1}", 32, "Hello");
                writer.Write("{0} {1} {2}", 32, "Hello", (uint)50);
                writer.Write("{0} {1} {2} {3}", 32, "Hello", (uint)50, (ulong)5);
                writer.Write("{0} {1} {2} {3} {4}", 32, "Hello", (uint)50, (ulong)5, 'a');
                writer.Write(true);
                writer.Write('a');
                writer.Write(new char[] { 'a', 'b', 'c', 'd', });
                writer.Write(new char[] { 'a', 'b', 'c', 'd', }, 1, 2);
                writer.Write(1.23d);
                writer.Write(123.456M);
                writer.Write(1.234f);
                writer.Write(39);
                writer.Write(50u);
                writer.Write(50L);
                writer.Write(50UL);
                writer.Write(new object());
                writer.Write("Hello World");

                writer.Flush();

                await writer.WriteAsync('c');
                await writer.WriteAsync(new char[] { 'a', 'b', 'c', 'd' });
                await writer.WriteAsync(new char[] { 'a', 'b', 'c', 'd' }, 1, 2);
                await writer.WriteAsync("Hello World");

                await writer.WriteLineAsync('c');
                await writer.WriteLineAsync(new char[] { 'a', 'b', 'c', 'd' });
                await writer.WriteLineAsync(new char[] { 'a', 'b', 'c', 'd' }, 1, 2);
                await writer.WriteLineAsync("Hello World");

                await writer.FlushAsync();
            }
        }
        finally
        {
            Console.SetOut(savedStandardOutput);
        }
    }

    private static unsafe void ValidateConsoleEncoding(Encoding encoding)
    {
        Assert.NotNull(encoding);

        // There's not much validation we can do, but we can at least invoke members
        // to ensure they don't throw exceptions as they delegate to the underlying
        // encoding wrapped by ConsoleEncoding.

        Assert.False(string.IsNullOrWhiteSpace(encoding.EncodingName));
        Assert.False(string.IsNullOrWhiteSpace(encoding.WebName));
        Assert.True(encoding.CodePage >= 0);
        bool ignored = encoding.IsSingleByte;

        // And we can validate that the encoding is self-consistent by roundtripping
        // data between chars and bytes.

        string str = "This is the input string.";
        char[] strAsChars = str.ToCharArray();
        byte[] strAsBytes = encoding.GetBytes(str);
        Assert.Equal(strAsBytes.Length, encoding.GetByteCount(str));
        Assert.True(encoding.GetMaxByteCount(str.Length) >= strAsBytes.Length);

        Assert.Equal(str, encoding.GetString(strAsBytes));
        Assert.Equal(str, encoding.GetString(strAsBytes, 0, strAsBytes.Length));
        Assert.Equal(str, new string(encoding.GetChars(strAsBytes)));
        Assert.Equal(str, new string(encoding.GetChars(strAsBytes, 0, strAsBytes.Length)));
        fixed (byte* bytesPtr = strAsBytes)
        {
            char[] outputArr = new char[encoding.GetMaxCharCount(strAsBytes.Length)];

            int len = encoding.GetChars(strAsBytes, 0, strAsBytes.Length, outputArr, 0);
            Assert.Equal(str, new string(outputArr, 0, len));
            Assert.Equal(len, encoding.GetCharCount(strAsBytes));
            Assert.Equal(len, encoding.GetCharCount(strAsBytes, 0, strAsBytes.Length));

            fixed (char* charsPtr = outputArr)
            {
                len = encoding.GetChars(bytesPtr, strAsBytes.Length, charsPtr, outputArr.Length);
                Assert.Equal(str, new string(charsPtr, 0, len));
                Assert.Equal(len, encoding.GetCharCount(bytesPtr, strAsBytes.Length));
            }

            Assert.Equal(str, encoding.GetString(bytesPtr, strAsBytes.Length));
        }

        Assert.Equal(strAsBytes, encoding.GetBytes(strAsChars));
        Assert.Equal(strAsBytes, encoding.GetBytes(strAsChars, 0, strAsChars.Length));
        Assert.Equal(strAsBytes.Length, encoding.GetByteCount(strAsChars));
        Assert.Equal(strAsBytes.Length, encoding.GetByteCount(strAsChars, 0, strAsChars.Length));
        fixed (char* charsPtr = strAsChars)
        {
            Assert.Equal(strAsBytes.Length, encoding.GetByteCount(charsPtr, strAsChars.Length));

            byte[] outputArr = new byte[encoding.GetMaxByteCount(strAsChars.Length)];
            Assert.Equal(strAsBytes.Length, encoding.GetBytes(strAsChars, 0, strAsChars.Length, outputArr, 0));
            fixed (byte* bytesPtr = outputArr)
            {
                Assert.Equal(strAsBytes.Length, encoding.GetBytes(charsPtr, strAsChars.Length, bytesPtr, outputArr.Length));
            }
            Assert.Equal(strAsBytes.Length, encoding.GetBytes(str, 0, str.Length, outputArr, 0));
        }
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on Browser, iOS, or tvOS.")]
    public static unsafe void OutputEncodingPreamble()
    {
        Encoding curEncoding = Console.OutputEncoding;

        try
        {
            Encoding encoding = Console.Out.Encoding;
            // The primary purpose of ConsoleEncoding is to return an empty preamble.
            Assert.Equal(Array.Empty<byte>(), encoding.GetPreamble());

            // Try setting the ConsoleEncoding to something else and see if it works.
            Console.OutputEncoding = Encoding.Unicode;
            // The primary purpose of ConsoleEncoding is to return an empty preamble.
            Assert.Equal(Array.Empty<byte>(), Console.Out.Encoding.GetPreamble());
        }
        finally
        {
            Console.OutputEncoding = curEncoding;
        }
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on Browser, iOS, or tvOS.")]
    public static unsafe void OutputEncoding()
    {
        Encoding curEncoding = Console.OutputEncoding;

        try
        {
            Assert.Same(Console.Out, Console.Out);

            Encoding encoding = Console.Out.Encoding;
            Assert.NotNull(encoding);
            Assert.Same(encoding, Console.Out.Encoding);
            ValidateConsoleEncoding(encoding);

            // Try setting the ConsoleEncoding to something else and see if it works.
            Console.OutputEncoding = Encoding.Unicode;
            Assert.Equal(Console.OutputEncoding.CodePage, Encoding.Unicode.CodePage);
            ValidateConsoleEncoding(Console.Out.Encoding);
        }
        finally
        {
            Console.OutputEncoding = curEncoding;
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public static void OutputEncoding_Getter_Returns_Unicode()
    {
        Encoding curEncoding = Console.OutputEncoding;
        Assert.Equal(Encoding.UTF8, curEncoding);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public static void InputEncoding_Getter_Throws_PlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.InputEncoding);
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
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on Browser, iOS, or tvOS.")]
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

    [Fact]
    public static void OpenStandardInput_NegativeBufferSize_ThrowsArgumentOutOfRangeException()
    {
        AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => Console.OpenStandardInput(-1));
    }

    [Fact]
    public static void OpenStandardOutput_NegativeBufferSize_ThrowsArgumentOutOfRangeException()
    {
        AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => Console.OpenStandardOutput(-1));
    }

    [Fact]
    public static void OpenStandardError_NegativeBufferSize_ThrowsArgumentOutOfRangeException()
    {
        AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => Console.OpenStandardError(-1));
    }
}
