// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Trivial implementation of Monitor lock for singlethreaded environment
using System.Runtime;

namespace System.Threading
{
    public static partial class Monitor
    {
        private static readonly IntPtr s_thread = InternalCalls.RhCurrentNativeThreadId();

        public static void Enter(object obj)
        {
            if (s_thread != InternalCalls.RhCurrentNativeThreadId())
                throw new InvalidOperationException();
        }

        public static void Enter(object obj, ref bool lockTaken)
        {
            if (lockTaken)
                throw new InvalidOperationException();

            Enter(obj);
            lockTaken = true;
        }

        public static void Exit(object obj)
        {
            if (s_thread != InternalCalls.RhCurrentNativeThreadId())
                throw new InvalidOperationException();
        }
    }
}
