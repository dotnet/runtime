// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class RuntimeAsync
{
    public const int Pass = 100;
    public const int Fail = -1;

    internal static int Run()
    {
        if (TaskReturningAsyncTest.Run() != Pass)
            return Fail;

        if (ValueTaskReturningAsyncTest.Run() != Pass)
            return Fail;

        if (TaskWithResultTest.Run() != Pass)
            return Fail;

        if (AwaitCompletedTaskTest.Run() != Pass)
            return Fail;

        return Pass;
    }
}

// Test basic Task-returning async method
class TaskReturningAsyncTest
{
    [RuntimeAsyncMethodGeneration(false)]
    static async Task SimpleAsyncMethod()
    {
        await Task.CompletedTask;
    }

    public static int Run()
    {
        try
        {
            var task = SimpleAsyncMethod();
            task.Wait();
            Console.WriteLine("TaskReturningAsyncTest passed");
            return RuntimeAsync.Pass;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TaskReturningAsyncTest failed: {ex}");
            return RuntimeAsync.Fail;
        }
    }
}

// Test ValueTask-returning async method
class ValueTaskReturningAsyncTest
{
    [RuntimeAsyncMethodGeneration(false)]
    static async ValueTask SimpleValueTaskAsync()
    {
        await Task.CompletedTask;
    }

    public static int Run()
    {
        try
        {
            var task = SimpleValueTaskAsync();
            task.AsTask().Wait();
            Console.WriteLine("ValueTaskReturningAsyncTest passed");
            return RuntimeAsync.Pass;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ValueTaskReturningAsyncTest failed: {ex}");
            return RuntimeAsync.Fail;
        }
    }
}

// Test Task<T> returning async method with result
class TaskWithResultTest
{
    [RuntimeAsyncMethodGeneration(false)]
    static async Task<int> ComputeAsync()
    {
        await Task.CompletedTask;
        return 42;
    }

    public static int Run()
    {
        try
        {
            var task = ComputeAsync();
            int result = task.Result;
            if (result == 42)
            {
                Console.WriteLine("TaskWithResultTest passed");
                return RuntimeAsync.Pass;
            }
            else
            {
                Console.WriteLine($"TaskWithResultTest failed: expected 42, got {result}");
                return RuntimeAsync.Fail;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TaskWithResultTest failed: {ex}");
            return RuntimeAsync.Fail;
        }
    }
}

// Test awaiting a completed task (optimized fast path)
class AwaitCompletedTaskTest
{
    [RuntimeAsyncMethodGeneration(false)]
    static async Task<string> GetMessageAsync()
    {
        await Task.CompletedTask;
        return "Hello";
    }

    [RuntimeAsyncMethodGeneration(false)]
    static async Task<string> CombineAsync()
    {
        string msg1 = await GetMessageAsync();
        string msg2 = await GetMessageAsync();
        return msg1 + " " + msg2;
    }

    public static int Run()
    {
        try
        {
            var task = CombineAsync();
            string result = task.Result;
            if (result == "Hello Hello")
            {
                Console.WriteLine("AwaitCompletedTaskTest passed");
                return RuntimeAsync.Pass;
            }
            else
            {
                Console.WriteLine($"AwaitCompletedTaskTest failed: expected 'Hello Hello', got '{result}'");
                return RuntimeAsync.Fail;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AwaitCompletedTaskTest failed: {ex}");
            return RuntimeAsync.Fail;
        }
    }
}
