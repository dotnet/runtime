// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public static class SendRequestHelper
    {
        public static  HttpResponseMessage Send(WinHttpHandler handler, Action setup)
        {
            return Send(handler, setup, TestServer.FakeServerEndpoint);
        }

        public static HttpResponseMessage Send(WinHttpHandler handler, Action setup, string fakeServerEndpoint)
        {
            TestServer.SetResponse(DecompressionMethods.None, TestServer.ExpectedResponseBody);

            setup();

            var invoker = new HttpMessageInvoker(handler, false);
            var request = new HttpRequestMessage(HttpMethod.Get, fakeServerEndpoint);
            Task<HttpResponseMessage> task = invoker.SendAsync(request, CancellationToken.None);
            
            return task.GetAwaiter().GetResult();
        }
    }
}
