// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnectionPool
    {
        private static bool IsHttp3Enabled => false;

        private ValueTask<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>?
            GetHttp3ConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken) => null;

        private bool IsAltSvcBlocked(HttpAuthority authority) => false;

        private bool ProcessAltSvc(HttpResponseMessage response, HttpConnectionBase? connection) => false;

        public void OnNetworkChanged()
        {
        }
    }
}
