// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Net.NetworkInformation
{
    public delegate void PingCompletedEventHandler(object sender, PingCompletedEventArgs e);

    public class PingCompletedEventArgs : AsyncCompletedEventArgs
    {
        internal PingCompletedEventArgs(PingReply? reply, Exception? error, bool cancelled, object? userToken) : base(error, cancelled, userToken)
        {
            Reply = reply;
        }

        public PingReply? Reply { get; }
    }
}
