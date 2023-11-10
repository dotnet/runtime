// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;

namespace Sample
{
    public partial class Test
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"smoke: TestCanStartThread 0 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            var are = new AutoResetEvent(false);
            /*
            var t = new Thread(() =>
            {
                Console.WriteLine($"smoke: TestCanStartThread 2 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                are.Set();
                Console.WriteLine($"smoke: TestCanStartThread 3 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            });
            */
            /*
            var factory = new TaskFactory();
            await factory.StartNew(() =>
            {
                Console.WriteLine($"smoke: TestCanStartThread 2 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                are.Set();
                Console.WriteLine($"smoke: TestCanStartThread 3 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            }, TaskCreationOptions.LongRunning);
            */
            var p = Task.Run(async ()=>{
                Console.WriteLine($"smoke: TestCanStartThread 1 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                await Task.Delay(1000);
                Console.WriteLine($"smoke: TestCanStartThread 2 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                are.Set();
                Console.WriteLine($"smoke: TestCanStartThread 3 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            });

            Console.WriteLine($"smoke: TestCanStartThread 4 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");

            are.WaitOne();

            Console.WriteLine($"smoke: TestCanStartThread 5 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");

            await p;

            return 0;
        }
    }
}
