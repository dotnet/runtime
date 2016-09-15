// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Net.Http;

namespace System.Net
{
    /// <summary>Provides a stream that notifies an event when the Close method is called.</summary>
    internal class ClosableStream : DelegatingStream
    {
        private readonly EventHandler _onClose;
        private int _closed;

        internal ClosableStream(Stream stream, EventHandler onClose) : base(stream)
        {
            _onClose = onClose;
        }

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Increment(ref _closed) == 1)
            {
                _onClose?.Invoke(this, new EventArgs());
            }

            base.Dispose(disposing);
        }
    }
}
