// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

// TODO: #13618
namespace System.Net.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class SimpleHttpTest
    {
        [Fact]
        public static void Supported_True()
        {
            Assert.True(HttpListener.IsSupported);
        }

        [Fact]
        public static async Task SimpleRequest_Succeeds()
        {
            string url = UrlPrefix.CreateLocal();
            const string expectedResponse = "hello from HttpListener";

            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(url);
                listener.Start();

                var serverContextTask = listener.GetContextAsync();

                using (HttpClient client = new HttpClient())
                {
                    var clientTask = client.GetStringAsync(url);

                    var serverContext = await serverContextTask;
                    using (var response = serverContext.Response)
                    {
                        var responseBuffer = Encoding.UTF8.GetBytes(expectedResponse);
                        response.ContentLength64 = responseBuffer.Length;

                        using (var output = response.OutputStream)
                        {
                            await output.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                        }
                    }

                    var clientString = await clientTask;

                    Assert.Equal(expectedResponse, clientString);
                }

                listener.Stop();
            }
        }
    }
}
