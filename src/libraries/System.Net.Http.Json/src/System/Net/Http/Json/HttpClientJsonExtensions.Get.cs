// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    /// <summary>
    /// Contains the extensions methods for using JSON as the content-type in HttpClient.
    /// </summary>
    public static partial class HttpClientJsonExtensions
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestUri"></param>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<object?> GetFromJsonAsync(
            this HttpClient client,
            string requestUri,
            Type type,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, cancellationToken);
            return ProcessTaskResponseAsync(taskResponse, type, options, cancellationToken);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestUri"></param>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<object?> GetFromJsonAsync(
            this HttpClient client,
            Uri requestUri,
            Type type,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, cancellationToken);
            return ProcessTaskResponseAsync(taskResponse, type, options, cancellationToken);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="requestUri"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<T> GetFromJsonAsync<T>(
            this HttpClient client,
            string requestUri,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
             Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, cancellationToken);
            return ProcessTaskResponseAsync<T>(taskResponse, options, cancellationToken);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="requestUri"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<T> GetFromJsonAsync<T>(
            this HttpClient client,
            Uri requestUri,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, cancellationToken);
            return ProcessTaskResponseAsync<T>(taskResponse, options, cancellationToken);
        }

        private static async Task<TValue> ProcessTaskResponseAsync<TValue>(
            Task<HttpResponseMessage> taskResponse,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Stream jsonStream = await GetUtf8StreamFromResponseAsync(taskResponse).ConfigureAwait(false);
            TValue value = await JsonSerializer.DeserializeAsync<TValue>(jsonStream, options, cancellationToken);
            return value;
        }

        private static async Task<object?> ProcessTaskResponseAsync(
            Task<HttpResponseMessage> taskResponse,
            Type type,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Stream jsonStream = await GetUtf8StreamFromResponseAsync(taskResponse).ConfigureAwait(false);
            object? value = await JsonSerializer.DeserializeAsync(jsonStream, type, options, cancellationToken);
            return value;
        }

        private static async Task<Stream> GetUtf8StreamFromResponseAsync(Task<HttpResponseMessage> taskResponse)
        {
            // Is CofigureAwait the right thing here?
            HttpResponseMessage response = await taskResponse.ConfigureAwait(false);

            // TODO: Is there any other validation that we should do?
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception();
            }

            // TODO: Validate that this is Utf8 or plain text.

            return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }
    }
}
