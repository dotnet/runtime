// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.WinHttpHandlerTests
{
    public class SendAsyncWinHttpMockHandler : WinHttpHandler
    {
        public Task<HttpResponseMessage> SendAsyncPublic(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }
    }
}
