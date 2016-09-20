// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Net.Mime
{
    internal sealed class MultiAsyncResult : LazyAsyncResult
    {
        private readonly object _context;
        private int _outstanding;

        internal MultiAsyncResult(object context, AsyncCallback callback, object state) : base(context, state, callback)
        {
            _context = context;
        }

        internal object Context => _context;

        internal void Enter() => Increment();

        internal void Leave() => Decrement();

        internal void Leave(object result)
        {
            Result = result;
            Decrement();
        }

        private void Decrement()
        {
            if (Interlocked.Decrement(ref _outstanding) == -1)
            {
                InvokeCallback(Result);
            }
        }

        private void Increment() => Interlocked.Increment(ref _outstanding);

        internal void CompleteSequence() => Decrement();

        internal static object End(IAsyncResult result)
        {
            MultiAsyncResult thisPtr = (MultiAsyncResult)result;
            thisPtr.InternalWaitForCompletion();
            return thisPtr.Result;
        }
    }
}
