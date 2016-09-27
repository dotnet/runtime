// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Internal.Runtime.Augments
{
    /// <summary>For internal use only.  Exposes runtime functionality to the Thread implementation in corefx.</summary>
    public static class RuntimeThreadAugments
    {
        public static IRuntimeThread CurrentThread => Thread.CurrentThread;

        public static IRuntimeThread Create(ThreadStart start) => new Thread(start);
        public static IRuntimeThread Create(ThreadStart start, int maxStackSize) => new Thread(start, maxStackSize);
        public static IRuntimeThread Create(ParameterizedThreadStart start) => new Thread(start);
        public static IRuntimeThread Create(ParameterizedThreadStart start, int maxStackSize)
            => new Thread(start, maxStackSize);
        public static void SpinWait(int iterations) => Thread.SpinWait(iterations);
    }
}
