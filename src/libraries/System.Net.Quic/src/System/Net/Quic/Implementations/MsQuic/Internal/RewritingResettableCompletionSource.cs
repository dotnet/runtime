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
    internal sealed class RewritingResettableCompletionSource<T> : ResettableCompletionSource<T>
    {
        private bool _exceptionSet;

        public override void Complete(T result)
        {
            if (_exceptionSet)
            {
                throw new InvalidOperationException();
            }

            Reset();
            base.Complete(result);
        }
        public override void CompleteException(Exception ex)
        {
            if (_exceptionSet)
            {
                throw new InvalidOperationException();
            }

            Reset();
            base.CompleteException(ex);
            _exceptionSet = true;
        }
    }
 }
