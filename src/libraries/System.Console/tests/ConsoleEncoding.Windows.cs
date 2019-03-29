// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public partial class ConsoleEncoding
{
    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetDefaultEncoding_Success()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding encoding = Encoding.GetEncoding(0);
            Console.InputEncoding = encoding;
            Assert.Equal(encoding, Console.InputEncoding);
            Assert.Equal((uint)encoding.CodePage, GetConsoleCP());

            return RemoteExecutor.SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetUnicodeEncoding_SilentlyIgnoredInternally()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding unicodeEncoding = Encoding.Unicode;
            Encoding oldEncoding = Console.InputEncoding;
            Assert.NotEqual(unicodeEncoding.CodePage, oldEncoding.CodePage);

            Console.InputEncoding = unicodeEncoding;
            Assert.Equal(unicodeEncoding, Console.InputEncoding);
            Assert.Equal((uint)oldEncoding.CodePage, GetConsoleCP());

            return RemoteExecutor.SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetDefaultEncoding_Success()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding encoding = Encoding.GetEncoding(0);
            Console.OutputEncoding = encoding;
            Assert.Equal(encoding, Console.OutputEncoding);
            Assert.Equal((uint)encoding.CodePage, GetConsoleOutputCP());

            return RemoteExecutor.SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetUnicodeEncoding_SilentlyIgnoredInternally()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding unicodeEncoding = Encoding.Unicode;
            Encoding oldEncoding = Console.OutputEncoding;
            Assert.NotEqual(unicodeEncoding.CodePage, oldEncoding.CodePage);
            Console.OutputEncoding = unicodeEncoding;
            Assert.Equal(unicodeEncoding, Console.OutputEncoding);

            Assert.Equal((uint)oldEncoding.CodePage, GetConsoleOutputCP());

            return RemoteExecutor.SuccessExitCode;
        }).Dispose();
    }

    [DllImport("kernel32.dll")]
    public extern static uint GetConsoleCP();

    [DllImport("kernel32.dll")]
    public extern static uint GetConsoleOutputCP();
}
