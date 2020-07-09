// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public partial class ConsoleEncoding
{
    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetDefaultEncoding_Success()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding encoding = Encoding.GetEncoding(0);
            Console.InputEncoding = encoding;
            Assert.Equal(encoding, Console.InputEncoding);
            Assert.Equal((uint)encoding.CodePage, GetConsoleCP());
        }).Dispose();
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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
        }).Dispose();
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetDefaultEncoding_Success()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding encoding = Encoding.GetEncoding(0);
            Console.OutputEncoding = encoding;
            Assert.Equal(encoding, Console.OutputEncoding);
            Assert.Equal((uint)encoding.CodePage, GetConsoleOutputCP());
        }).Dispose();
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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
        }).Dispose();
    }

    [DllImport("kernel32.dll")]
    public static extern uint GetConsoleCP();

    [DllImport("kernel32.dll")]
    public static extern uint GetConsoleOutputCP();
}
