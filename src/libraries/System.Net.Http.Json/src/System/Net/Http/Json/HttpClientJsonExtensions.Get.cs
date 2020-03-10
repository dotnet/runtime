// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            return GetFromJsonAsyncCore(taskResponse, type, options, cancellationToken);
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
            return GetFromJsonAsyncCore(taskResponse, type, options, cancellationToken);
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
            return GetFromJsonAsyncCore<T>(taskResponse, options, cancellationToken);
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
            return GetFromJsonAsyncCore<T>(taskResponse, options, cancellationToken);
        }

        private static async Task<object?> GetFromJsonAsyncCore(Task<HttpResponseMessage> taskResponse, Type type, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await taskResponse.ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync(type, options, cancellationToken).ConfigureAwait(false);
        }
        private static async Task<T> GetFromJsonAsyncCore<T>(Task<HttpResponseMessage> taskResponse, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await taskResponse.ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(options, cancellationToken).ConfigureAwait(false);
        }
    }
}
