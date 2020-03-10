// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    public class HttpContentJsonExtensionsTests
    {
        public async Task HttpContentGetPersonAsync()
        {
            HttpContent content = null;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, $"getPerson");
                        var response = await client.SendAsync(request);
                        Assert.True(response.StatusCode == HttpStatusCode.OK);

                        content = response.Content;
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync(content: Person.Create().Serialize());
                });

            object obj = await content.ReadFromJsonAsync(typeof(Person));
            Person per = Assert.IsType<Person>(obj);
            per.Validate();

            per = await content.ReadFromJsonAsync<Person>();
            per.Validate();
        }

        public async Task HttpContentTypeIsNull()
        {
            HttpContent content = null;
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, "getPerson");
                        var response = await client.SendAsync(request);
                        Assert.True(response.StatusCode == HttpStatusCode.OK);

                         content = response.Content;
                    }
                },
                async server => {
                    HttpRequestData req = await server.HandleRequestAsync(content: "null");
                });

            object obj = await content.ReadFromJsonAsync(typeof(Person));
            Assert.Null(obj);

            Person per = await content.ReadFromJsonAsync<Person>();
            Assert.Null(per);
        }
    }
}
