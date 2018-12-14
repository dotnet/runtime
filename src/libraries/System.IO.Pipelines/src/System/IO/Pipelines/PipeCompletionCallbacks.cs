// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;

namespace System.IO.Pipelines
{
    internal sealed class PipeCompletionCallbacks
    {
        private readonly ArrayPool<PipeCompletionCallback> _pool;
        private readonly int _count;
        private readonly Exception _exception;
        private readonly PipeCompletionCallback _firstCallback;
        private readonly PipeCompletionCallback[] _callbacks;

        public PipeCompletionCallbacks(ArrayPool<PipeCompletionCallback> pool, int count, Exception exception, PipeCompletionCallback firstCallback, PipeCompletionCallback[] callbacks)
        {
            _pool = pool;
            _count = count;
            _exception = exception;
            _firstCallback = firstCallback;
            _callbacks = callbacks;
        }

        public void Execute()
        {
            if (_count == 0)
            {
                return;
            }

            List<Exception> exceptions = null;

            Execute(_firstCallback, ref exceptions);

            if (_callbacks != null)
            {
                try
                {
                    for (var i = 0; i < _count - 1; i++)
                    {
                        var callback = _callbacks[i];
                        Execute(callback, ref exceptions);
                    }
                }
                finally
                {
                    _pool.Return(_callbacks, clearArray: true);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }

        private void Execute(PipeCompletionCallback callback, ref List<Exception> exceptions)
        {
            try
            {
                callback.Callback(_exception, callback.State);
            }
            catch (Exception ex)
            {
                if (exceptions == null)
                {
                    exceptions = new List<Exception>();
                }

                exceptions.Add(ex);
            }
        }
    }
}
