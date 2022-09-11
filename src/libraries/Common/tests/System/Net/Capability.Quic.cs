// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic;
using System.Threading;

namespace System.Net.Test.Common
{
    public static partial class Capability
    {
        public static bool IsQuicSupported = GetIsQuicSupported();
        private static bool GetIsQuicSupported()
        {
            return QuicConnection.IsSupported && QuicListener.IsSupported;
        }
    }
}
