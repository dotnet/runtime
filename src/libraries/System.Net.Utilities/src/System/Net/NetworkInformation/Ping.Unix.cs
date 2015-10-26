// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Threading;

namespace System.Net.NetworkInformation
{
    public partial class Ping
    {
        private void InternalSendAsync(IPAddress address, byte[] buffer, int timeout, PingOptions options)
        {
            AsyncOperation asyncOp = _asyncOp;
            SendOrPostCallback callback = _onPingCompletedDelegate;

            // TODO: Implement this (#2487)

            PingException pe = new PingException(SR.net_ping, new PlatformNotSupportedException());
            var ea = new PingCompletedEventArgs(
                new PingReply(address, default(PingOptions), default(IPStatus), default(long), buffer), 
                pe, 
                false, 
                asyncOp.UserSuppliedState);

            Finish();
            asyncOp.PostOperationCompleted(callback, ea);
        }

        private void InternalDisposeCore()
        {
            // TODO: Implement this (#2487)
        }
    }
}
