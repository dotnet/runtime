// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http.Headers;
using System.Net.Test.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    public class JsonContentTests_Sync : JsonContentTestsBase
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request) => Task.Run(() => client.Send(request));

        [Fact]
        public void JsonContent_CopyTo_Succeeds()
        {
            var person = Person.Create();
            using var content = JsonContent.Create(person);
            using var stream = new MemoryStream();
            content.CopyTo(stream, null, default);
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            Assert.Equal(person.Serialize(JsonOptions.DefaultSerializerOptions), json);
        }
    }
}
