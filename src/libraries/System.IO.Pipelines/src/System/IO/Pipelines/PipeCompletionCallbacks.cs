// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace System.IO.Pipelines
{
    internal sealed class PipeCompletionCallbacks
    {
        private readonly List<PipeCompletionCallback> _callbacks;
        private readonly Exception? _exception;

        public PipeCompletionCallbacks(List<PipeCompletionCallback> callbacks, ExceptionDispatchInfo? edi)
        {
            _callbacks = callbacks;
            _exception = edi?.SourceException;
        }

        public void Execute()
        {
            var count = _callbacks.Count;
            if (count == 0)
            {
                return;
            }

            List<Exception>? exceptions = null;

            if (_callbacks != null)
            {
                for (int i = 0; i < count; i++)
                {
                    var callback = _callbacks[i];
                    Execute(callback, ref exceptions);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }

        private void Execute(PipeCompletionCallback callback, ref List<Exception>? exceptions)
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
