// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Generic;
using Xunit;

namespace System.Threading.Tests
{
    internal sealed class TrackingSynchronizationContext : SynchronizationContext
    {
        public readonly List<string> CallStacks = new List<string>();

        public override void OperationStarted() => CallStacks.Add(Environment.StackTrace);
        public override void OperationCompleted() => CallStacks.Add(Environment.StackTrace);

        public override void Post(SendOrPostCallback d, object state)
        {
            CallStacks.Add(Environment.StackTrace);
            ThreadPool.QueueUserWorkItem(delegate
            {
                SetSynchronizationContext(this);
                d(state);
            });
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            CallStacks.Add(Environment.StackTrace);
            ThreadPool.QueueUserWorkItem(delegate
            {
                SynchronizationContext orig = SynchronizationContext.Current;
                try
                {
                    SetSynchronizationContext(this);
                    d(state);
                }
                finally
                {
                    SetSynchronizationContext(orig);
                }
            });
        }
    }
}
