// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal class ListenerClientCertAsyncResult : IAsyncResult
    {
        private readonly X509Certificate2 _certificate;
        internal AsyncCallback _callback;
        internal object _state;

        public ListenerClientCertAsyncResult(X509Certificate2 certificate, AsyncCallback callback, object state)
        {
            _certificate = certificate;
            _callback = callback;
            _state = state;
        }

        public void Complete()
        {
            if (_callback != null)
                _callback(this);
        }

        public object AsyncState
        {
            get { return _state; }
        }

        public X509Certificate2 Certificate => _certificate;

        public WaitHandle AsyncWaitHandle => new ManualResetEvent(true);

        public bool CompletedSynchronously => true;

        public bool IsCompleted => true;
    }
}
