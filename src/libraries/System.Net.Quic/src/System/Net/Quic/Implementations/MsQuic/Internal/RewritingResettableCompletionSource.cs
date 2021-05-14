// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    /// <summary>
    /// Same as <see cref="ResettableCompletionSource{T}" /> with the difference that
    /// <see cref="Complete" /> can be called multiple times (<see cref="CompleteException"/> only once) before the task is consumed.
    /// The last value (or the exception) will be the one reported.
    /// </summary>
    internal sealed class RewritingResettableCompletionSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private bool _lastValueSet;
        private T? _lastValue;
        private ManualResetValueTaskSourceCore<T> _valueTaskSource;

        public RewritingResettableCompletionSource()
        {
            _valueTaskSource.RunContinuationsAsynchronously = true;
        }

        public ValueTask<T> GetValueTask()
        {
            return new ValueTask<T>(this, _valueTaskSource.Version);
        }

        public ValueTask GetTypelessValueTask()
        {
            return new ValueTask(this, _valueTaskSource.Version);
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return _valueTaskSource.GetStatus(token);
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _valueTaskSource.OnCompleted(continuation, state, token, flags);
        }

        public void Complete(T result)
        {
            if (!_lastValueSet)
            {
                _valueTaskSource.SetResult(result);
            }
            _lastValue = result;
            _lastValueSet = true;
        }

        public void CompleteException(Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }

        public T GetResult(short token)
        {
            bool isValid = token == _valueTaskSource.Version;
            try
            {
                T result =  _valueTaskSource.GetResult(token);
                return _lastValue ?? result;
            }
            finally
            {
                if (isValid)
                {
                    _valueTaskSource.Reset();
                    _lastValue = default;
                    _lastValueSet = false;
                }
            }
        }

        void IValueTaskSource.GetResult(short token)
        {
            bool isValid = token == _valueTaskSource.Version;
            try
            {
                _valueTaskSource.GetResult(token);
            }
            finally
            {
                if (isValid)
                {
                    _valueTaskSource.Reset();
                    _lastValue = default;
                    _lastValueSet = false;
                }
            }
        }
    }
 }
