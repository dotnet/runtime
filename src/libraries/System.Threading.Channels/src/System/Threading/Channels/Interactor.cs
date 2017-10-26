// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Threading.Channels
{
    internal abstract class Interactor<T> : TaskCompletionSource<T>
    {
        protected Interactor(bool runContinuationsAsynchronously) :
            base(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None) { }

        internal bool Success(T item)
        {
            UnregisterCancellation();
            return TrySetResult(item);
        }

        internal bool Fail(Exception exception)
        {
            UnregisterCancellation();
            return TrySetException(exception);
        }

        internal virtual void UnregisterCancellation() { }
    }

    internal class ReaderInteractor<T> : Interactor<T>
    {
        protected ReaderInteractor(bool runContinuationsAsynchronously) : base(runContinuationsAsynchronously) { }

        public static ReaderInteractor<T> Create(bool runContinuationsAsynchronously) =>
            new ReaderInteractor<T>(runContinuationsAsynchronously);

        public static ReaderInteractor<T> Create(bool runContinuationsAsynchronously, CancellationToken cancellationToken) =>
            cancellationToken.CanBeCanceled ?
                new CancelableReaderInteractor<T>(runContinuationsAsynchronously, cancellationToken) :
                new ReaderInteractor<T>(runContinuationsAsynchronously);
    }

    internal class WriterInteractor<T> : Interactor<VoidResult>
    {
        protected WriterInteractor(bool runContinuationsAsynchronously) : base(runContinuationsAsynchronously) { }

        internal T Item { get; private set; }

        public static WriterInteractor<T> Create(bool runContinuationsAsynchronously, CancellationToken cancellationToken, T item)
        {
            WriterInteractor<T> w = cancellationToken.CanBeCanceled ?
                new CancelableWriter<T>(runContinuationsAsynchronously, cancellationToken) :
                new WriterInteractor<T>(runContinuationsAsynchronously);
            w.Item = item;
            return w;
        }
    }

    internal sealed class CancelableReaderInteractor<T> : ReaderInteractor<T>
    {
        private CancellationToken _token;
        private CancellationTokenRegistration _registration;

        internal CancelableReaderInteractor(bool runContinuationsAsynchronously, CancellationToken cancellationToken) : base(runContinuationsAsynchronously)
        {
            _token = cancellationToken;
            _registration = cancellationToken.Register(s =>
            {
                var thisRef = (CancelableReaderInteractor<T>)s;
                thisRef.TrySetCanceled(thisRef._token);
            }, this);
        }

        internal override void UnregisterCancellation()
        {
            _registration.Dispose();
            _registration = default(CancellationTokenRegistration);
        }
    }

    internal sealed class CancelableWriter<T> : WriterInteractor<T>
    {
        private CancellationToken _token;
        private CancellationTokenRegistration _registration;

        internal CancelableWriter(bool runContinuationsAsynchronously, CancellationToken cancellationToken) : base(runContinuationsAsynchronously)
        {
            _token = cancellationToken;
            _registration = cancellationToken.Register(s =>
            {
                var thisRef = (CancelableWriter<T>)s;
                thisRef.TrySetCanceled(thisRef._token);
            }, this);
        }

        internal override void UnregisterCancellation()
        {
            _registration.Dispose();
            _registration = default(CancellationTokenRegistration);
        }
    }
}
