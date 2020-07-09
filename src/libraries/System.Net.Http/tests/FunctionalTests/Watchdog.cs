// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    /// <summary>
    /// This is using similar trick to GetStateMachineData
    /// If marked test runs for more than 60s it will print machine state and make sure it fails
    /// Usage (await MUST be run directly in the test, should not be called from other async method):
    ///     using (await Watchdog.CreateAsync())
    ///     {
    ///         // test code
    ///     }
    /// </summary>
    internal class Watchdog : ICriticalNotifyCompletion
    {
        private object _box;

        private Watchdog() { }

        public static Watchdog CreateAsync()
            => new Watchdog();

        public IDisposable GetResult()
            => new WatchdogImpl(_box);

        public Watchdog GetAwaiter() => this;
        public bool IsCompleted => false;
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation)
        {
            _box = continuation.Target;
            Task.Run(continuation);
        }

        private class WatchdogImpl : IDisposable
        {
            private bool _passed = true;
            private Timer _timer;

            public WatchdogImpl(object stateMachineData)
            {
                _timer = new Timer(s =>
                    {
                        _passed = false;
                        Console.WriteLine(GetStateMachineData.Describe(s));
                    },
                    stateMachineData,
                    60_000,
                    60_000);
            }

            public void Dispose()
            {
                _timer.Dispose();
                Assert.True(_passed);
            }
        }
    }
}
