// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public partial class ConsoleEncoding
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
    [ActiveIssue("https://github.com/dotnet/corefx/issues/18220", TargetFrameworkMonikers.UapAot)]
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

            string inString = new string(Console.InputEncoding.GetChars(inputBytesNoBom));

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

    [Fact]
    public void TestValidEncodings()
    {
        Action<Encoding> check = encoding =>
        {
            Console.OutputEncoding = encoding;
            Assert.Equal(encoding, Console.OutputEncoding);
            Console.InputEncoding = encoding;
            Assert.Equal(encoding, Console.InputEncoding);
        };

        // These seem to always be available
        check(Encoding.UTF8);
        check(Encoding.Unicode);

        // On full Windows, ASCII is available also
        if (!PlatformDetection.IsWindowsNanoServer)
        {
            check(Encoding.ASCII);
        }

    }

    public class NonexistentCodePageEncoding : Encoding
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
    [ActiveIssue("https://github.com/dotnet/corefx/issues/18220", TargetFrameworkMonikers.UapAot)]
    public void InputEncoding_SetWithInInitialized_ResetsIn()
    {
        RemoteExecutor.Invoke(() =>
        {
            // Initialize Console.In
            TextReader inReader = Console.In;
            Assert.NotNull(inReader);
            Assert.Same(inReader, Console.In);

            // Change the InputEncoding
            Console.InputEncoding = Encoding.Unicode; // Not ASCII: not supported by Windows Nano
            Assert.Equal(Encoding.Unicode, Console.InputEncoding);

            if (PlatformDetection.IsWindows)
            {
                // Console.set_InputEncoding is effectively a nop on Unix,
                // so although the reader accessed by Console.In will be reset,
                // it'll be re-initialized on re-access to the same singleton,
                // (assuming input isn't redirected).
                Assert.NotSame(inReader, Console.In);
            }

            return RemoteExecutor.SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    public void InputEncoding_SetNull_ThrowsArgumentNullException()
    {
        AssertExtensions.Throws<ArgumentNullException>("value", () => Console.InputEncoding = null);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetEncodingWithInvalidCodePage_ThrowsIOException()
    {
        NonexistentCodePageEncoding invalidEncoding = new NonexistentCodePageEncoding();
        Assert.Throws<IOException>(() => Console.InputEncoding = invalidEncoding);
        Assert.NotSame(invalidEncoding, Console.InputEncoding);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/corefx/issues/18220", TargetFrameworkMonikers.UapAot)]
    public void OutputEncoding_SetWithErrorAndOutputInitialized_ResetsErrorAndOutput()
    {
        RemoteExecutor.Invoke(() =>
        {
            // Initialize Console.Error
            TextWriter errorWriter = Console.Error;
            Assert.NotNull(errorWriter);
            Assert.Same(errorWriter, Console.Error);

            // Initialize Console.Out
            TextWriter outWriter = Console.Out;
            Assert.NotNull(outWriter);
            Assert.Same(outWriter, Console.Out);

            // Change the OutputEncoding
            Console.OutputEncoding = Encoding.Unicode; // Not ASCII: not supported by Windows Nano
            Assert.Equal(Encoding.Unicode, Console.OutputEncoding);

            Assert.NotSame(errorWriter, Console.Error);
            Assert.NotSame(outWriter, Console.Out);

            return RemoteExecutor.SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    public void OutputEncoding_SetNull_ThrowsArgumentNullException()
    {
        AssertExtensions.Throws<ArgumentNullException>("value", () => Console.OutputEncoding = null);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/corefx/issues/18220", TargetFrameworkMonikers.UapAot)]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetEncodingWithInvalidCodePage_ThrowsIOException()
    {
        NonexistentCodePageEncoding invalidEncoding = new NonexistentCodePageEncoding();
        Assert.Throws<IOException>(() => Console.OutputEncoding = invalidEncoding);
        Assert.NotSame(invalidEncoding, Console.OutputEncoding);
    }
}
