// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            Person person = Person.Create();
            using JsonContent content = JsonContent.Create(person);
            using MemoryStream stream = new MemoryStream();
            // HttpContent.CopyTo internally calls overridden JsonContent.SerializeToStream, which is the targeted method of this test.
            content.CopyTo(stream, context: null, cancellationToken: default);
            stream.Seek(0, SeekOrigin.Begin);
            using StreamReader reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            Assert.Equal(person.Serialize(JsonOptions.DefaultSerializerOptions), json);
        }
    }
}
