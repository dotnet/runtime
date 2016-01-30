// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
