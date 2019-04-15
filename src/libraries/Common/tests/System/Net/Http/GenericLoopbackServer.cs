// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    // Loopback server abstraction.
    // Tests that want to run over both HTTP/1.1 and HTTP/2 should use this instead of the protocol-specific loopback servers.

    public abstract class LoopbackServerFactory
    {
        public abstract Task CreateServerAsync(Func<GenericLoopbackServer, Uri, Task> funcAsync, int millisecondsTimeout = 30_000);

        public abstract bool IsHttp11 { get; }
        public abstract bool IsHttp2 { get; }

        // Common helper methods

        public Task CreateClientAndServerAsync(Func<Uri, Task> clientFunc, Func<GenericLoopbackServer, Task> serverFunc, int millisecondsTimeout = 30_000)
        {
            return CreateServerAsync(async (server, uri) =>
            {
                Task clientTask = clientFunc(uri);
                Task serverTask = serverFunc(server);

                await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed();
            }).TimeoutAfter(millisecondsTimeout);
        }
    }

    public abstract class GenericLoopbackServer : IDisposable
    {
        // Accept a new connection, process a single request and send the specified response, and gracefully close the connection.
        public abstract Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = null);

        public abstract void Dispose();
    }

    public struct HttpHeaderData
    {
        public string Name { get; }
        public string Value { get; }

        public HttpHeaderData(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public class HttpRequestData
    {
        public byte[] Body;
        public string Method;
        public string Path;
        public List<HttpHeaderData> Headers { get; }

        public HttpRequestData()
        {
            Headers = new List<HttpHeaderData>();
        }

        public string[] GetHeaderValues(string headerName)
        {
            return Headers.Where(h => h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                    .Select(h => h.Value)
                    .ToArray();
        }

        public string GetSingleHeaderValue(string headerName)
        {
            string[] values = GetHeaderValues(headerName);
            if (values.Length != 1)
            {
                throw new Exception(
                    $"Expected single value for {headerName} header, actual count: {values.Length}{Environment.NewLine}" +
                    $"{"\t"}{string.Join(Environment.NewLine + "\t", values)}");
            }

            return values[0];
        }

        public int GetHeaderValueCount(string headerName)
        {
            return Headers.Where(h => h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase)).Count();
        }
    }
}
