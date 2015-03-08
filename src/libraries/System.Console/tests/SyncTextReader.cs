// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public class SyncTextReaderTests
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

    private static void Test(string content, Action<StreamReader> action)
    {
        TextWriter savedStandardOutput = Console.Out;
        TextReader savedStandardInput = Console.In;

        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(memStream))
                {
                    sw.Write(content);
                    sw.Flush();

                    memStream.Seek(0, SeekOrigin.Begin);

                    using (StreamReader sr = new StreamReader(memStream))
                    {
                        Console.SetIn(sr);
                        action(sr);
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
    public void ReadToEnd()
    {
        var expected = string.Join(Environment.NewLine, s_testLines);
        Test(expected, reader => {
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
        Test(new string(expected), reader => {
            // Given
            var buffer = new char[5];

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
        Test(new string(expected), reader => {
            // Given
            var buffer = new char[5];

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
        Test("ABC", reader => {
            // Given, When
            var first = Console.In.Peek();
            var second = Console.In.Read();

            // Then
            Assert.Equal('A', (char)first);
            Assert.Equal('A', (char)second);
        });
    }

    [Fact]
    public void ReadToEndAsync()
    {
        var expected = string.Join(Environment.NewLine, s_testLines);
        Test(expected, reader => {
            // Given, When
            var task = Console.In.ReadToEndAsync();
            task.Wait(TimeSpan.FromSeconds(1));
            var result = task.Result;

            // Then
            Assert.Equal(expected, result);
            Assert.Equal(-1, Console.Read()); // We should be at EOF now.
        });
    }

    [Fact]
    public void ReadBlockAsync()
    {
        var expected = new[] { 'H', 'e', 'l', 'l', 'o' };
        Test(new string(expected), reader => {
            // Given
            var buffer = new char[5];

            // When
            var task = Console.In.ReadBlockAsync(buffer, 0, 5);
            task.Wait(TimeSpan.FromSeconds(1));
            var result = task.Result;

            // Then
            Assert.Equal(5, result);
            Assert.Equal(expected, buffer);
            Assert.Equal(-1, Console.Read()); // We should be at EOF now. 
        });
    }

    [Fact]
    public void ReadAsync()
    {
        var expected = new[] { 'H', 'e', 'l', 'l', 'o' };
        Test(new string(expected), reader => {
            // Given
            var buffer = new char[5];

            // When
            var task = Console.In.ReadAsync(buffer, 0, 5);
            task.Wait(TimeSpan.FromSeconds(1));
            var result = task.Result;

            // Then
            Assert.Equal(5, result);
            Assert.Equal(expected, buffer);
            Assert.Equal(-1, Console.Read()); // We should be at EOF now.
        });
    }

    [Fact]
    public void ReadLineAsync()
    {
        var expected = string.Join(Environment.NewLine, s_testLines);
        Test(expected, reader => {
            for (int i = 0; i < s_testLines.Length; i++)
            {
                var task = Console.In.ReadLineAsync();
                task.Wait(TimeSpan.FromSeconds(1));

                Assert.Equal(s_testLines[i], task.Result);
            }
            Assert.Equal(-1, Console.Read());
        });
    }
}