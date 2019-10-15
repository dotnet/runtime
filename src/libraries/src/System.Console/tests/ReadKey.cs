// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public class ReadKey
{
    [Fact]
    public static void KeyAvailable()
    {
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

    [Fact]
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
