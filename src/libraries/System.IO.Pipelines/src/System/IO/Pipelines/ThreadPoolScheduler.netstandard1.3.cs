// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    internal sealed class ThreadPoolScheduler : PipeScheduler
    {
        public override void Schedule<TState>(Action<TState> action, TState state)
        {
            Task.Factory.StartNew(s =>
            {
                var tuple = (Tuple<Action<TState>, TState>)s;
                tuple.Item1(tuple.Item2);
            }, 
            Tuple.Create(action, state), 
            CancellationToken.None, 
            TaskCreationOptions.DenyChildAttach, 
            TaskScheduler.Default);
        }
    }
}
