// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Internal.Runtime.Augments
{
    public interface IRuntimeThread
    {
        ExecutionContext ExecutionContext { get; }
        bool IsAlive { get; }
        bool IsBackground { get; set; }
        bool IsThreadPoolThread { get; }
        int ManagedThreadId { get; }
        string Name { get; set; }
        ThreadPriority Priority { get; set; }
        ThreadState ThreadState { get; }

        ApartmentState GetApartmentState();
        bool TrySetApartmentState(ApartmentState state);

        void Interrupt();
    }
}
