// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Sample;

public partial class Test
{
    private static int _animationCounter = 0;
    private static int _callCounter = 0;
    private static bool _isRunning = false;
    private static Task later;
    private static readonly IReadOnlyList<string> _animations = new string[] { "\u2680", "\u2681", "\u2682", "\u2683", "\u2684", "\u2685" };

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        later = Task.Delay(200); // this will create Timer thread
        await updateProgress2();
        return 0;
    }

    [JSImport("globalThis.console.log")]
    public static partial void ConsoleLog(string status);

    [JSImport("Sample.Test.updateProgress", "main.js")]
    private static partial Task updateProgress(string status);

    [JSImport("Sample.Test.updateProgress2", "main.js")]
    private static partial Task updateProgress2();

    [JSExport]
    public static void Progress2()
    {
        // both calls here are sync POSIX calls dispatched to UI thread, which is already blocked because this is synchronous method on deputy thread
        // it should not deadlock anyway, see also invoke_later_when_on_ui_thread_sync and emscripten_yield
        var cwd = Directory.GetCurrentDirectory();
        Console.WriteLine("Progress2 A " + cwd); 

        // below is blocking call, which means that UI will spin-lock little longer
        // it will warn about blocking wait because of jsThreadBlockingMode: "WarnWhenBlockingWait"
        // but it will not deadlock because underlying task chain is not JS promise
        later.Wait();

        Console.WriteLine("Progress2 B"); 
    }

    [JSExport]
    public static bool Progress()
    {
        updateProgress(""+_animations[_animationCounter++]);
        if (_animationCounter >= _animations.Count)
        {
            _animationCounter = 0;
        }
        return _isRunning;
    }

    [JSExport]
    [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
    public static Task<long> Fib(int n)
    {
        return Task.Run(()=>{
            _isRunning = true;
            var res = FibImpl(n);
            _isRunning = false;
            return Task.FromResult(res);
        });
    }

    private static long FibImpl(int n)
    {
        _callCounter++;
        // make some garbage every 1000 calls
        if (_callCounter % 1000 == 0)
        {
            AllocateGarbage();
        }
        // and collect it every once in a while
        if (_callCounter % 500000 == 0)
            GC.Collect();

        if (n < 2)
            return n;
        return FibImpl(n - 1) + FibImpl(n - 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AllocateGarbage()
    {
        object[] garbage = new object[200];
        garbage[12] = new object();
        garbage[197] = garbage;
    }
}
