// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace System.Net.NetworkInformation
{
    public delegate void PingCompletedEventHandler(object sender, PingCompletedEventArgs e);

    public class PingCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly PingReply _reply;

        internal PingCompletedEventArgs(PingReply reply, Exception error, bool cancelled, object userToken) : 
            base(error, cancelled, userToken)
        {
            _reply = reply;
        }

        public PingReply Reply { get { return _reply; } }
    }
}
