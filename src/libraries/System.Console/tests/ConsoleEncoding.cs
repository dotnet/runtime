// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

public class ConsoleEncoding : RemoteExecutorTestBase
{
    public static IEnumerable<object[]> InputData()
    {
        yield return new object[] { "This is Ascii string" };
        yield return new object[] { "This is string have surrogates \uD800\uDC00" };
        yield return new object[] { "This string has non ascii characters \u03b1\u00df\u0393\u03c0\u03a3\u03c3\u00b5" };
        yield return new object[] { "This string has invalid surrogates \uD800\uD800\uD800\uD800\uD800\uD800" };
        yield return new object[] { "\uD800" };
    }

    [Theory]
    [MemberData(nameof(InputData))]
    public void TestEncoding(string inputString)
    {
        TextWriter outConsoleStream = Console.Out;
        TextReader inConsoleStream = Console.In;

        try
        {
            byte [] inputBytes;
            byte [] inputBytesNoBom = Console.OutputEncoding.GetBytes(inputString);
            byte [] bom = Console.OutputEncoding.GetPreamble();

            if (bom.Length > 0)
            {
                inputBytes = new byte[inputBytesNoBom.Length + bom.Length];
                Array.Copy(bom, inputBytes, bom.Length);
                Array.Copy(inputBytesNoBom, 0, inputBytes, bom.Length, inputBytesNoBom.Length);
            }
            else
            {
                inputBytes = inputBytesNoBom;
            }

            byte[] outBytes = new byte[inputBytes.Length];
            using (MemoryStream ms = new MemoryStream(outBytes, true))
            {
                using (StreamWriter sw = new StreamWriter(ms, Console.OutputEncoding))
                {
                    Console.SetOut(sw);
                    Console.Write(inputString);
                }
            }

            Assert.Equal(inputBytes, outBytes);

            string inString = new String(Console.InputEncoding.GetChars(inputBytesNoBom));

            string outString;
            using (MemoryStream ms = new MemoryStream(inputBytesNoBom, false))
            {
                using (StreamReader sr = new StreamReader(ms, Console.InputEncoding))
                {
                    Console.SetIn(sr);
                    outString = Console.In.ReadToEnd();
                }
            }

            Assert.True(inString.Equals(outString), $"Encoding: {Console.InputEncoding}, Codepage: {Console.InputEncoding.CodePage}, Expected: {inString}, Actual: {outString} ");
        }
        finally
        {
            Console.SetOut(outConsoleStream);
            Console.SetIn(inConsoleStream);
        }
    }

    public class NoSuchCodePage : Encoding
    {
        public override int CodePage => int.MinValue;

        public override int GetByteCount(char[] chars, int index, int count) => 0;
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) => 0;

        public override int GetCharCount(byte[] bytes, int index, int count) => 0;
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) => 0;

        public override int GetMaxByteCount(int charCount) => 0;
        public override int GetMaxCharCount(int byteCount) => 0;
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetDefaultEncoding_Success()
    {
        RemoteInvoke(() =>
        {
            Encoding encoding = Encoding.GetEncoding(0);
            Console.InputEncoding = encoding;
            Assert.Equal(encoding, Console.InputEncoding);
            Assert.Equal((uint)encoding.CodePage, GetConsoleCP());

            return SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetUnicodeEncoding_SilentlyIgnoredInternally()
    {
        RemoteInvoke(() =>
        {
            Encoding unicodeEncoding = Encoding.Unicode;
            Encoding oldEncoding = Console.InputEncoding;
            Assert.NotEqual(unicodeEncoding.CodePage, oldEncoding.CodePage);

            Console.InputEncoding = unicodeEncoding;
            Assert.Equal(unicodeEncoding, Console.InputEncoding);
            Assert.Equal((uint)oldEncoding.CodePage, GetConsoleCP());

            return SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    public void InputEncoding_SetWithInInitialized_ResetsIn()
    {
        RemoteInvoke(() =>
        {
            // Initialize Console.In
            TextReader inReader = Console.In;
            Assert.NotNull(inReader);
            Assert.Same(inReader, Console.In);

            // Chang the InputEncoding
            Console.InputEncoding = Encoding.ASCII;
            Assert.Equal(Encoding.ASCII, Console.InputEncoding);

            Assert.NotSame(inReader, Console.In);

            return SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    public void InputEncoding_SetNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("value", () => Console.InputEncoding = null);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetEncodingWithInvalidCodePage_ThrowsIOException()
    {
        NoSuchCodePage invalidEncoding = new NoSuchCodePage();
        Assert.Throws<IOException>(() => Console.InputEncoding = invalidEncoding);
        Assert.NotEqual(invalidEncoding, Console.InputEncoding);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetDefaultEncoding_Success()
    {
        RemoteInvoke(() =>
        {
            Encoding encoding = Encoding.GetEncoding(0);
            Console.OutputEncoding = encoding;
            Assert.Equal(encoding, Console.OutputEncoding);
            Assert.Equal((uint)encoding.CodePage, GetConsoleOutputCP());

            return SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetUnicodeEncoding_SilentlyIgnoredInternally()
    {
        RemoteInvoke(() =>
        {
            Encoding unicodeEncoding = Encoding.Unicode;
            Encoding oldEncoding = Console.OutputEncoding;
            Assert.NotEqual(unicodeEncoding.CodePage, oldEncoding.CodePage);
            Console.OutputEncoding = unicodeEncoding;
            Assert.Equal(unicodeEncoding, Console.OutputEncoding);

            Assert.Equal((uint)oldEncoding.CodePage, GetConsoleOutputCP());

            return SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    public void OutputEncoding_SetWithErrorAndOutputInitialized_ResetsErrorAndOutput()
    {
        RemoteInvoke(() =>
        {
            // Initialize Console.Error
            TextWriter errorWriter = Console.Error;
            Assert.NotNull(errorWriter);
            Assert.Same(errorWriter, Console.Error);

            // Initialize Console.Out
            TextWriter outWriter = Console.Out;
            Assert.NotNull(outWriter);
            Assert.Same(outWriter, Console.Out);

            // Chang the OutputEncoding
            Console.OutputEncoding = Encoding.ASCII;
            Assert.Equal(Encoding.ASCII, Console.OutputEncoding);

            Assert.NotSame(errorWriter, Console.Error);
            Assert.NotSame(outWriter, Console.Out);

            return SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    public void OutputEncoding_SetNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("value", () => Console.OutputEncoding = null);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetEncodingWithInvalidCodePage_ThrowsIOException()
    {
        NoSuchCodePage invalidEncoding = new NoSuchCodePage();
        Assert.Throws<IOException>(() => Console.OutputEncoding = invalidEncoding);
        Assert.NotEqual(invalidEncoding, Console.OutputEncoding);
    }

    [DllImport("kernel32.dll")]
    public extern static uint GetConsoleCP();

    [DllImport("kernel32.dll")]
    public extern static uint GetConsoleOutputCP();
}
