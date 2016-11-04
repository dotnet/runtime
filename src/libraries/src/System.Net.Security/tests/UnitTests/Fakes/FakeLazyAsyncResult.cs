// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Net.Security
{
    internal class LazyAsyncResult : IAsyncResult
    {
        public LazyAsyncResult(SslState sslState, object asyncState, AsyncCallback asyncCallback)
        {
            AsyncState = asyncState;
            asyncCallback?.Invoke(this);
        }

        public object AsyncState { get; }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return true;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return true;
            }
        }
    }
}
