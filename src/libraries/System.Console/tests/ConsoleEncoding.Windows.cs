// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
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
    public void InputEncoding_SetEncodingWhenDetached_ErrorIsSilentlyIgnored()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding encoding = Encoding.GetEncoding(0);
            Assert.NotEqual(encoding.CodePage, Console.InputEncoding.CodePage);

            // use FreeConsole to detach the current console - simulating a process started with the "DETACHED_PROCESS" flag
            FreeConsole();

            // Setting the input encoding should not throw an exception
            Console.InputEncoding = encoding;
            // The internal state of Console should have updated, despite the failure to change the console's input encoding
            Assert.Equal(encoding, Console.InputEncoding);
            // Operations on the console are no longer valid - GetConsoleCP fails.
            Assert.Equal(0u, GetConsoleCP());
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

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetEncodingWhenDetached_ErrorIsSilentlyIgnored()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding encoding = Encoding.GetEncoding(0);
            Assert.NotEqual(encoding.CodePage, Console.OutputEncoding.CodePage);

            // use FreeConsole to detach the current console - simulating a process started with the "DETACHED_PROCESS" flag
            FreeConsole();

            // Setting the output encoding should not throw an exception
            Console.OutputEncoding = encoding;
            // The internal state of Console should have updated, despite the failure to change the console's output encoding
            Assert.Equal(encoding, Console.OutputEncoding);
            // Operations on the console are no longer valid - GetConsoleOutputCP fails.
            Assert.Equal(0u, GetConsoleOutputCP());
        }).Dispose();
    }

    [LibraryImport("kernel32.dll")]
    public static partial uint GetConsoleCP();

    [LibraryImport("kernel32.dll")]
    public static partial uint GetConsoleOutputCP();

    [LibraryImport("kernel32.dll")]
    public static partial int FreeConsole();
}
