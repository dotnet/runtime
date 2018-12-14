// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace System.IO.Pipelines
{
    [DebuggerDisplay("IsCompleted: {" + nameof(IsCompleted) + "}")]
    internal struct PipeCompletion
    {
        private static readonly ArrayPool<PipeCompletionCallback> s_completionCallbackPool = ArrayPool<PipeCompletionCallback>.Shared;

        private const int InitialCallbacksSize = 1;

        private bool _isCompleted;
        private ExceptionDispatchInfo _exceptionInfo;

        private PipeCompletionCallback _firstCallback;
        private PipeCompletionCallback[] _callbacks;
        private int _callbackCount;

        public bool IsCompleted => _isCompleted;

        public bool IsFaulted => _exceptionInfo != null;

        public PipeCompletionCallbacks TryComplete(Exception exception = null)
        {
            if (!_isCompleted)
            {
                _isCompleted = true;
                if (exception != null)
                {
                    _exceptionInfo = ExceptionDispatchInfo.Capture(exception);
                }
            }
            return GetCallbacks();
        }

        public PipeCompletionCallbacks AddCallback(Action<Exception, object> callback, object state)
        {
            if (_callbackCount == 0)
            {
                _firstCallback = new PipeCompletionCallback(callback, state);
                _callbackCount++;
            }
            else
            {
                EnsureSpace();

                // -1 to adjust for _firstCallback
                var callbackIndex = _callbackCount - 1;
                _callbackCount++;
                _callbacks[callbackIndex] = new PipeCompletionCallback(callback, state);
            }

            if (IsCompleted)
            {
                return GetCallbacks();
            }

            return null;
        }

        private void EnsureSpace()
        {
            if (_callbacks == null)
            {
                _callbacks = s_completionCallbackPool.Rent(InitialCallbacksSize);
            }

            int newLength = _callbackCount - 1;

            if (newLength == _callbacks.Length)
            {
                PipeCompletionCallback[] newArray = s_completionCallbackPool.Rent(_callbacks.Length * 2);
                Array.Copy(_callbacks, newArray, _callbacks.Length);
                s_completionCallbackPool.Return(_callbacks, clearArray: true);
                _callbacks = newArray;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCompletedOrThrow()
        {
            if (!_isCompleted)
            {
                return false;
            }

            if (_exceptionInfo != null)
            {
                ThrowLatchedException();
            }

            return true;
        }

        private PipeCompletionCallbacks GetCallbacks()
        {
            Debug.Assert(IsCompleted);
            if (_callbackCount == 0)
            {
                return null;
            }

            var callbacks = new PipeCompletionCallbacks(s_completionCallbackPool,
                _callbackCount,
                _exceptionInfo?.SourceException,
                _firstCallback,
                _callbacks);

            _firstCallback = default;
            _callbacks = null;
            _callbackCount = 0;
            return callbacks;
        }

        public void Reset()
        {
            Debug.Assert(IsCompleted);
            Debug.Assert(_callbacks == null);
            _isCompleted = false;
            _exceptionInfo = null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowLatchedException()
        {
            _exceptionInfo.Throw();
        }

        public override string ToString()
        {
            return $"{nameof(IsCompleted)}: {IsCompleted}";
        }
    }
}
