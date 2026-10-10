// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class UnixHandleAsyncContext
    {
        /// <summary>
        /// An <see cref="Operation"/> that delegates to caller-supplied callbacks.
        /// This enables libraries that cannot subclass <see cref="Operation"/> directly
        /// (e.g. out-of-band packages) to implement operations.
        /// </summary>
        public sealed class DelegateOperation : Operation
        {
            private readonly Func<SafeHandle, bool> _tryComplete;
            private readonly Action<int> _onCompleted;

            /// <summary>
            /// Creates a new <see cref="DelegateOperation"/> with the specified callbacks.
            /// </summary>
            /// <param name="tryComplete">
            /// Performs the I/O operation. Returns <see langword="true"/> if completed,
            /// <see langword="false"/> if pending (EWOULDBLOCK).
            /// </param>
            /// <param name="onCompleted">
            /// Called when the operation completes asynchronously.
            /// The argument is the <see cref="OnCompletedResult"/> value cast to <see cref="int"/>.
            /// </param>
            public DelegateOperation(Func<SafeHandle, bool> tryComplete, Action<int> onCompleted)
            {
                ArgumentNullException.ThrowIfNull(tryComplete);
                ArgumentNullException.ThrowIfNull(onCompleted);

                _tryComplete = tryComplete;
                _onCompleted = onCompleted;
            }

            protected internal override bool TryCompleteOperation(SafeHandle handle)
                => _tryComplete(handle);

            protected internal override void OnCompleted(OnCompletedResult result)
                => _onCompleted((int)result);
        }
    }
}
