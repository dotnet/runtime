// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Http.Metrics
{
    /// <summary>
    /// Provides functionality for enriching request metrics `http-client-request-duration` and `http-client-failed-requests`.
    /// </summary>
    /// <remarks>
    /// Enrichment is done on per-request basis by callbacks registered with <see cref="AddCallback(HttpRequestMessage, Action{HttpMetricsEnrichmentContext})"/>.
    /// The callbacks are responsible for adding custom tags via <see cref="AddCustomTag(string, object?)"/> for which they can use the request, response and error
    /// information exposed on the <see cref="HttpMetricsEnrichmentContext"/> instance.
    ///
    /// > [!IMPORTANT]
    /// > The <see cref="HttpMetricsEnrichmentContext"/> intance must not be used from outside of the enrichment callbacks.
    /// </remarks>
    public sealed class HttpMetricsEnrichmentContext
    {
        private static readonly HttpRequestOptionsKey<HttpMetricsEnrichmentContext> s_optionsKeyForContext = new(nameof(HttpMetricsEnrichmentContext));
        private static readonly ConcurrentQueue<HttpMetricsEnrichmentContext> s_pool = new();
        private static int s_poolItemCount;
        private const int PoolCapacity = 1024;

        private readonly List<Action<HttpMetricsEnrichmentContext>> _callbacks = new();
        private HttpRequestMessage? _request;
        private HttpResponseMessage? _response;
        private Exception? _exception;
        private List<KeyValuePair<string, object?>> _tags = new(capacity: 16);

        internal HttpMetricsEnrichmentContext() { } // Hide the default parameterless constructor.

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/> that has been sent.
        /// </summary>
        /// <remarks>
        /// This property must not be used from outside of the enrichment callbacks.
        /// </remarks>
        public HttpRequestMessage Request => _request!;

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/> received from the server or <see langword="null"/> if the request failed.
        /// </summary>
        /// <remarks>
        /// This property must not be used from outside of the enrichment callbacks.
        /// </remarks>
        public HttpResponseMessage? Response => _response;

        /// <summary>
        /// Gets the exception that occurred or <see langword="null"/> if there was no error.
        /// </summary>
        /// <remarks>
        /// This property must not be used from outside of the enrichment callbacks.
        /// </remarks>
        public Exception? Exception => _exception;

        /// <summary>
        /// Appends a custom tag to the list of tags to be recorded with the request metrics `http-client-request-duration` and `http-client-failed-requests`.
        /// </summary>
        /// <param name="name">The name of the tag.</param>
        /// <param name="value">The value of the tag.</param>
        /// <remarks>
        /// This method must not be used from outside of the enrichment callbacks.
        /// </remarks>
        public void AddCustomTag(string name, object? value) => _tags.Add(new KeyValuePair<string, object?>(name, value));

        /// <summary>
        /// Adds a callback to register custom tags for request metrics `http-client-request-duration` and `http-client-failed-requests`.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to apply enrichment to.</param>
        /// <param name="callback">The callback responsible to add custom tags via <see cref="AddCustomTag(string, object?)"/>.</param>
        public static void AddCallback(HttpRequestMessage request, Action<HttpMetricsEnrichmentContext> callback)
        {
            HttpRequestOptions options = request.Options;

            // We associate an HttpMetricsEnrichmentContext with the request on the first call to AddCallback(),
            // and store the callbacks in the context. This allows us to cache all the enrichment objects together.
            if (!options.TryGetValue(s_optionsKeyForContext, out HttpMetricsEnrichmentContext? context))
            {
                if (s_pool.TryDequeue(out context))
                {
                    Debug.Assert(context._callbacks.Count == 0);
                    Interlocked.Decrement(ref s_poolItemCount);
                }
                else
                {
                    context = new HttpMetricsEnrichmentContext();
                }

                options.Set(s_optionsKeyForContext, context);
            }
            context._callbacks.Add(callback);
        }

        internal static HttpMetricsEnrichmentContext? GetEnrichmentContextForRequest(HttpRequestMessage request)
        {
            if (request._options is null)
            {
                return null;
            }
            request._options.TryGetValue(s_optionsKeyForContext, out HttpMetricsEnrichmentContext? context);
            return context;
        }

        internal void RecordDurationWithEnrichment(HttpRequestMessage request,
            HttpResponseMessage? response,
            Exception? exception,
            TimeSpan durationTime,
            in TagList commonTags,
            Histogram<double> requestDuration)
        {
            _request = request;
            _response = response;
            _exception = exception;

            Debug.Assert(_tags.Count == 0);

            // Adding the enrichment tags to the TagList would likely exceed its' on-stack capacity, leading to an allocation.
            // To avoid this, we add all the tags to a List<T> which is cached together with HttpMetricsEnrichmentContext.
            // Use a for loop to iterate over the TagList, since TagList.GetEnumerator() allocates, see
            // https://github.com/dotnet/runtime/issues/87022.
            for (int i = 0; i < commonTags.Count; i++)
            {
                _tags.Add(commonTags[i]);
            }

            try
            {
                foreach (Action<HttpMetricsEnrichmentContext> callback in _callbacks)
                {
                    callback(this);
                }

                requestDuration.Record(durationTime.TotalSeconds, CollectionsMarshal.AsSpan(_tags));
            }
            finally
            {
                _request = null;
                _response = null;
                _exception = null;
                _callbacks.Clear();
                _tags.Clear();

                if (Interlocked.Increment(ref s_poolItemCount) <= PoolCapacity)
                {
                    s_pool.Enqueue(this);
                }
                else
                {
                    Interlocked.Decrement(ref s_poolItemCount);
                }
            }
        }
    }
}
