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
        private readonly PipeCompletionCallback[] _callbacks;

        public PipeCompletionCallbacks(ArrayPool<PipeCompletionCallback> pool, int count, Exception exception, PipeCompletionCallback[] callbacks)
        {
            _pool = pool;
            _count = count;
            _exception = exception;
            _callbacks = callbacks;
        }

        public void Execute()
        {
            if (_callbacks == null || _count == 0)
            {
                return;
            }

            try
            {
                List<Exception> exceptions = null;

                for (int i = 0; i < _count; i++)
                {
                    PipeCompletionCallback callback = _callbacks[i];
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

                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
                }
            }
            finally
            {
                _pool.Return(_callbacks, clearArray: true);
            }
        }
    }
}
