// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Threading.Tasks
{
    /// <summary>Enables awaiting a Begin/End method pair.</summary>
    internal sealed class BeginEndAwaitableAdapter : RendezvousAwaitable<IAsyncResult>
    {
        public static readonly AsyncCallback Callback = asyncResult =>
        {
            Debug.Assert(asyncResult != null);
            Debug.Assert(asyncResult.IsCompleted);
            Debug.Assert(asyncResult.AsyncState is BeginEndAwaitableAdapter);

            var adapter = (BeginEndAwaitableAdapter)asyncResult.AsyncState;
            adapter.SetResult(asyncResult);
        };

        public BeginEndAwaitableAdapter()
        {
            RunContinuationsAsynchronously = false;
        }
    }
}
