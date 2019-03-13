// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Internal.Runtime.Augments
{
    public class RuntimeThread : CriticalFinalizerObject
    {
        private Thread _thread;
        [ThreadStatic]
        private static RuntimeThread t_currentThread;

        private RuntimeThread(Thread thread)
        {
            _thread = thread;
        }

        public static RuntimeThread Create(ThreadStart start) => new RuntimeThread(new Thread(start));
        public static RuntimeThread Create(ThreadStart start, int maxStackSize) => new RuntimeThread(new Thread(start, maxStackSize));
        public static RuntimeThread Create(ParameterizedThreadStart start) => new RuntimeThread(new Thread(start));
        public static RuntimeThread Create(ParameterizedThreadStart start, int maxStackSize) => new RuntimeThread(new Thread(start, maxStackSize));

        public static RuntimeThread CurrentThread => t_currentThread ?? (t_currentThread = new RuntimeThread(Thread.CurrentThread));

        public bool IsAlive => _thread.IsAlive;
        public bool IsBackground { get => _thread.IsBackground; set => _thread.IsBackground = value; }
        public bool IsThreadPoolThread => _thread.IsThreadPoolThread;
        public int ManagedThreadId => _thread.ManagedThreadId;
        public string Name { get => _thread.Name; set => _thread.Name = value; }
        public ThreadPriority Priority { get => _thread.Priority; set => _thread.Priority = value; }
        public ThreadState ThreadState => _thread.ThreadState;

        public ApartmentState GetApartmentState() => _thread.GetApartmentState();
        public bool TrySetApartmentState(ApartmentState state) => _thread.TrySetApartmentState(state);
        public void DisableComObjectEagerCleanup() => _thread.DisableComObjectEagerCleanup();
        public void Interrupt() => _thread.Interrupt();
        public void Join() => _thread.Join();
        public bool Join(int millisecondsTimeout) => _thread.Join(millisecondsTimeout);

        public static void Sleep(int millisecondsTimeout) => Thread.Sleep(millisecondsTimeout);
        public static int GetCurrentProcessorId() => Thread.GetCurrentProcessorId();
        public static void SpinWait(int iterations) => Thread.SpinWait(iterations);
        public static bool Yield() => Thread.Yield();

        public void Start() => _thread.Start();
        public void Start(object parameter) => _thread.Start(parameter);
    }
}
