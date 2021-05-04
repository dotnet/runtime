// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Xunit;

[SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on Browser, iOS, or tvOS.")]
public class SyncTextReader
{
    // NOTE: These tests test the underlying SyncTextReader by
    // accessing the Console.In TextReader (which in fact is a SyncTextReader).

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

    private static void Test(string content, Action action)
    {
        TextWriter savedStandardOutput = Console.Out;
        TextReader savedStandardInput = Console.In;

        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                // Write the content, but leave the stream open.
                using (StreamWriter sw = new StreamWriter(memStream, Encoding.Unicode, 1024, true))
                {
                    sw.Write(content);
                    sw.Flush();
                }

                memStream.Seek(0, SeekOrigin.Begin);

                using (StreamReader sr = new StreamReader(memStream))
                {
                    Console.SetIn(sr);
                    action();
                }

            }
        }
        finally
        {
            TextWriter oldWriter = Console.Out;
            Console.SetOut(savedStandardOutput);
            oldWriter.Dispose();

            TextReader oldReader = Console.In;
            Console.SetIn(savedStandardInput);
            oldReader.Dispose();
        }
    }

    [Fact]
    public void ReadToEnd()
    {
        var expected = string.Join(Environment.NewLine, s_testLines);
        Test(expected, () => {
            // Given, When
            var result = Console.In.ReadToEnd();

            // Then
            Assert.Equal(expected, result);
            Assert.Equal(-1, Console.Read()); // We should be at EOF now.
        });
    }

    [Fact]
    public void ReadBlock()
    {
        var expected = new[] { 'H', 'e', 'l', 'l', 'o' };
        Test(new string(expected), () => {
            // Given
            var buffer = new char[expected.Length];

            // When
            var result = Console.In.ReadBlock(buffer, 0, 5);

            // Then
            Assert.Equal(5, result);
            Assert.Equal(expected, buffer);
            Assert.Equal(-1, Console.Read()); // We should be at EOF now.
        });
    }

    [Fact]
    public void Read()
    {
        var expected = new[] { 'H', 'e', 'l', 'l', 'o' };
        Test(new string(expected), () => {
            // Given
            var buffer = new char[expected.Length];

            // When
            var result = Console.In.Read(buffer, 0, 5);

            // Then
            Assert.Equal(5, result);
            Assert.Equal(expected, buffer);
            Assert.Equal(-1, Console.Read()); // We should be at EOF now.
        });
    }

    [Fact]
    public void Peek()
    {
        const string expected = "ABC";
        Test(expected, () => {
            foreach (char expectedChar in expected)
            {
                Assert.Equal(expectedChar, Console.In.Peek());
                Assert.Equal(expectedChar, Console.In.Read());
            }
        });
    }

    [Fact]
    public void ReadToEndAsync()
    {
        var expected = string.Join(Environment.NewLine, s_testLines);
        Test(expected, () => {
            // Given, When
            var result = Console.In.ReadToEndAsync().Result;

            // Then
            Assert.Equal(expected, result);
            Assert.Equal(-1, Console.Read()); // We should be at EOF now.
        });
    }

    [Fact]
    public void ReadBlockAsync()
    {
        var expected = new[] { 'H', 'e', 'l', 'l', 'o' };
        Test(new string(expected), () => {
            // Given
            var buffer = new char[expected.Length];

            // When
            var result = Console.In.ReadBlockAsync(buffer, 0, 5).Result;

            // Then
            Assert.Equal(5, result);
            Assert.Equal(expected, buffer);
            Assert.Equal(-1, Console.Read()); // We should be at EOF now.

            // Invalid args
            Assert.Throws<ArgumentNullException>(() => { Console.In.ReadBlockAsync(null, 0, 0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Console.In.ReadBlockAsync(new char[1], -1, 0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Console.In.ReadBlockAsync(new char[1], 0, -1); });
            AssertExtensions.Throws<ArgumentException>(null, () => { Console.In.ReadBlockAsync(new char[1], 1, 1); });
        });
    }

    [Fact]
    public void ReadAsync()
    {
        var expected = new[] { 'H', 'e', 'l', 'l', 'o' };
        Test(new string(expected), () => {
            // Given
            var buffer = new char[expected.Length];

            // When
            var result = Console.In.ReadAsync(buffer, 0, 5).Result;

            // Then
            Assert.Equal(5, result);
            Assert.Equal(expected, buffer);
            Assert.Equal(-1, Console.Read()); // We should be at EOF now.

            // Invalid args
            Assert.Throws<ArgumentNullException>(() => { Console.In.ReadAsync(null, 0, 0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Console.In.ReadAsync(new char[1], -1, 0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Console.In.ReadAsync(new char[1], 0, -1); });
            AssertExtensions.Throws<ArgumentException>(null, () => { Console.In.ReadAsync(new char[1], 1, 1); });
        });
    }

    [Fact]
    public void ReadLineAsync()
    {
        var expected = string.Join(Environment.NewLine, s_testLines);
        Test(expected, () => {
            for (int i = 0; i < s_testLines.Length; i++)
            {
                // Given, When
                var result = Console.In.ReadLineAsync().Result;

                // Then
                Assert.Equal(s_testLines[i], result);
            }
            Assert.Equal(-1, Console.Read());
        });
    }
}
