// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>A base class for a blocked or waiting reader or writer.</summary>
    /// <typeparam name="T">Specifies the type of data passed to the reader or writer.</typeparam>
    internal abstract class Interactor<T> : TaskCompletionSource<T>
    {
        /// <summary>Initializes the interactor.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        protected Interactor(bool runContinuationsAsynchronously) :
            base(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None) { }

        /// <summary>Completes the interactor with a success state and the specified result.</summary>
        /// <param name="item">The result value.</param>
        /// <returns>true if the interactor could be successfully transitioned to a completed state; false if it was already completed.</returns>
        internal bool Success(T item)
        {
            UnregisterCancellation();
            return TrySetResult(item);
        }

        /// <summary>Completes the interactor with a failed state and the specified error.</summary>
        /// <param name="exception">The error.</param>
        /// <returns>true if the interactor could be successfully transitioned to a completed state; false if it was already completed.</returns>
        internal bool Fail(Exception exception)
        {
            UnregisterCancellation();
            return TrySetException(exception);
        }

        /// <summary>Unregister cancellation in case cancellation was registered.</summary>
        internal virtual void UnregisterCancellation() { }
    }

    /// <summary>A blocked or waiting reader.</summary>
    /// <typeparam name="T">Specifies the type of data being read.</typeparam>
    internal class ReaderInteractor<T> : Interactor<T>
    {
        /// <summary>Initializes the reader.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        protected ReaderInteractor(bool runContinuationsAsynchronously) : base(runContinuationsAsynchronously) { }

        /// <summary>Creates a reader.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        /// <returns>The reader.</returns>
        public static ReaderInteractor<T> Create(bool runContinuationsAsynchronously) =>
            new ReaderInteractor<T>(runContinuationsAsynchronously);

        /// <summary>Creates a reader.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the read operation.</param>
        /// <returns>The reader.</returns>
        public static ReaderInteractor<T> Create(bool runContinuationsAsynchronously, CancellationToken cancellationToken) =>
            cancellationToken.CanBeCanceled ?
                new CancelableReaderInteractor<T>(runContinuationsAsynchronously, cancellationToken) :
                new ReaderInteractor<T>(runContinuationsAsynchronously);
    }

    /// <summary>A blocked or waiting writer.</summary>
    /// <typeparam name="T">Specifies the type of data being written.</typeparam>
    internal class WriterInteractor<T> : Interactor<VoidResult>
    {
        /// <summary>Initializes the writer.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        protected WriterInteractor(bool runContinuationsAsynchronously) : base(runContinuationsAsynchronously) { }

        /// <summary>The item being written.</summary>
        internal T Item { get; private set; }

        /// <summary>Creates a writer.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        /// <param name="item">The item being written.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the read operation.</param>
        /// <returns>The reader.</returns>
        public static WriterInteractor<T> Create(bool runContinuationsAsynchronously, T item, CancellationToken cancellationToken)
        {
            WriterInteractor<T> w = cancellationToken.CanBeCanceled ?
                new CancelableWriter<T>(runContinuationsAsynchronously, cancellationToken) :
                new WriterInteractor<T>(runContinuationsAsynchronously);
            w.Item = item;
            return w;
        }
    }

    /// <summary>A blocked or waiting reader where the read can be canceled.</summary>
    /// <typeparam name="T">Specifies the type of data being read.</typeparam>
    internal sealed class CancelableReaderInteractor<T> : ReaderInteractor<T>
    {
        /// <summary>The token used for cancellation.</summary>
        private readonly CancellationToken _token;
        /// <summary>Registration in <see cref="_token"/> that should be disposed of when the operation has completed.</summary>
        private CancellationTokenRegistration _registration;

        /// <summary>Initializes the cancelable reader.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the read operation.</param>
        internal CancelableReaderInteractor(bool runContinuationsAsynchronously, CancellationToken cancellationToken) : base(runContinuationsAsynchronously)
        {
            _token = cancellationToken;
            _registration = cancellationToken.Register(s =>
            {
                var thisRef = (CancelableReaderInteractor<T>)s;
                thisRef.TrySetCanceled(thisRef._token);
            }, this);
        }

        /// <summary>Unregister cancellation in case cancellation was registered.</summary>
        internal override void UnregisterCancellation()
        {
            _registration.Dispose();
            _registration = default;
        }
    }

    /// <summary>A blocked or waiting reader where the read can be canceled.</summary>
    /// <typeparam name="T">Specifies the type of data being read.</typeparam>
    internal sealed class CancelableWriter<T> : WriterInteractor<T>
    {
        /// <summary>The token used for cancellation.</summary>
        private CancellationToken _token;
        /// <summary>Registration in <see cref="_token"/> that should be disposed of when the operation has completed.</summary>
        private CancellationTokenRegistration _registration;

        /// <summary>Initializes the cancelable writer.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the read operation.</param>
        internal CancelableWriter(bool runContinuationsAsynchronously, CancellationToken cancellationToken) : base(runContinuationsAsynchronously)
        {
            _token = cancellationToken;
            _registration = cancellationToken.Register(s =>
            {
                var thisRef = (CancelableWriter<T>)s;
                thisRef.TrySetCanceled(thisRef._token);
            }, this);
        }

        /// <summary>Unregister cancellation in case cancellation was registered.</summary>
        internal override void UnregisterCancellation()
        {
            _registration.Dispose();
            _registration = default;
        }
    }
}
