using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System.Net.Test.Common;
using System.Text.Json;

namespace System.Net.Http.Json.Functional.Tests
{
    public class HttpClientJsonExtensionsTests
    {
        [Fact]
        public async Task TestGetFromJsonAsync()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var deserializedObj = await client.GetFromJsonAsync(uri, typeof(object));
                        Assert.NotNull(deserializedObj);
                        Assert.IsType<JsonElement>(deserializedObj);
                    }
                },
                server => server.HandleRequestAsync(content: "{}"));
        }
    }
}
