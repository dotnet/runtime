// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public class ReadKey : RemoteExecutorTestBase
{
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

    private static void RunRemote(Func<int> func, ProcessStartInfo psi = null)
    {
        using (var remote = RemoteInvoke(func, true, psi)) { }
    }
}
