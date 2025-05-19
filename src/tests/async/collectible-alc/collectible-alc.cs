// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Xunit;

public class Async2CollectibleAlc
{
    [Fact]
    public static void TestEntryPoint()
    {
        AsyncEntryPoint().Wait();
    }

    private static async Task AsyncEntryPoint()
    {
        WeakReference wr = await CallFooAsyncAndUnload();

        for (int i = 0; i < 10 && wr.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(wr.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> CallFooAsyncAndUnload()
    {
        TaskCompletionSource tcs = new();
        (Task<string> task, WeakReference wr) = CallFooAsyncInCollectibleALC(tcs.Task);
        for (int i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.True(wr.IsAlive);

        tcs.SetResult();
        string result = await task;
        Assert.Equal("done", result);
        return wr;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Task<string>, WeakReference) CallFooAsyncInCollectibleALC(Task task)
    {
        CollectibleALC alc = new CollectibleALC();
        Assembly asm = alc.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);

        MethodInfo[] mis = asm.GetType(nameof(Async2CollectibleAlc)).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo mi = mis.Single(mi => mi.Name == "FooAsync" && mi.ReturnType == typeof(Task<string>));
        Task<string> resultTask = (Task<string>)mi.Invoke(null, new object[] { new Task[] { task } });
        alc.Unload();
        return (resultTask, new WeakReference(alc, trackResurrection: true));
    }

    // Task[] to work around a compiler bug
    private static async Task<string> FooAsync(Task[] t)
    {
        await t[0];
        return "done";
    }

    private class CollectibleALC : AssemblyLoadContext
    {
        public CollectibleALC() : base(true)
        {
        }
    }
}
