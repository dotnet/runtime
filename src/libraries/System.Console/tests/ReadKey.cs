// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

public class ReadKey
{
    private readonly ITestOutputHelper _output;

    public ReadKey(ITestOutputHelper output)
    {
        _output = output;
    }

    private bool IsHandleRedirected(IntPtr handle, string desc)
    {
        // If handle is not to a character device, we must be redirected:
        uint fileType = Interop.Kernel32.GetFileType(handle);
        if ((fileType & Interop.Kernel32.FileTypes.FILE_TYPE_CHAR) != Interop.Kernel32.FileTypes.FILE_TYPE_CHAR)
            return true;
        
        _output.WriteLine($"fileTypeOutput '{desc}': {fileType}");

        // We are on a char device if GetConsoleMode succeeds and so we are not redirected.
        return (!Interop.Kernel32.IsGetConsoleModeCallSuccessful(handle));
    }

    [Fact]
    public void KeyAvailable()
    {
        if (PlatformDetection.IsWindows)
        {
            IntPtr inputHandle = Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_INPUT_HANDLE);
            _output.WriteLine("IsInputHandleRedirected: " + IsHandleRedirected(inputHandle, "input"));
            IntPtr OutputHandle = Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_OUTPUT_HANDLE);
            _output.WriteLine("IsOutputHandleRedirected: " + IsHandleRedirected(OutputHandle, "output"));
        }

        if (Console.IsInputRedirected)
        {
            Assert.Throws<InvalidOperationException>(() => Console.KeyAvailable);
        }
        else
        {
            // Nothing to assert; just validate we can call it.
            bool available = Console.KeyAvailable;
        }
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    public static void RedirectedConsole_ReadKey()
    {
        RunRemote(() => { Assert.Throws<InvalidOperationException>(() => Console.ReadKey()); return 42; }, new ProcessStartInfo() { RedirectStandardInput = true });
    }

    [Fact]
    public static void ConsoleKeyValueCheck()
    {
        ConsoleKeyInfo info;
        info = new ConsoleKeyInfo('\0', (ConsoleKey)0, false, false, false);
        info = new ConsoleKeyInfo('\0', (ConsoleKey)255, false, false, false);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleKeyInfo('\0', (ConsoleKey)256, false, false, false));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public void NumberLock_GetUnix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.NumberLock);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public void CapsLock_GetUnix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.CapsLock);
    }

    private static void RunRemote(Func<int> func, ProcessStartInfo psi = null)
    {
        var options = new RemoteInvokeOptions();
        if (psi != null)
        {
            options.StartInfo = psi;
        }

        RemoteExecutor.Invoke(func, options).Dispose();
    }
}
