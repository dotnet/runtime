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
    public sealed class HttpMetricsEnrichmentContext
    {
        private static readonly HttpRequestOptionsKey<HttpMetricsEnrichmentContext> s_optionsKeyForContext = new("HttpMetricsEnrichmentContext");
        private static readonly ConcurrentQueue<HttpMetricsEnrichmentContext> s_contextCache = new();
        private static int s_contextCacheItemCount;
        private const int ContextCacheCapacity = 1024;

        private readonly List<Action<HttpMetricsEnrichmentContext>> _callbacks = new();
        private HttpRequestMessage? _request;
        private HttpResponseMessage? _response;
        private Exception? _exception;
        private List<KeyValuePair<string, object?>> _tags = new(capacity: 16);

        public HttpRequestMessage Request => _request!;

        public HttpResponseMessage? Response => _response;

        public Exception? Exception => _exception;

        public void AddCustomTag(string name, object? value) => _tags.Add(new KeyValuePair<string, object?>(name, value));

        /// <summary>
        /// Adds a callback to register enrichment for request metrics instruments that support it.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to apply enrichment to.</param>
        /// <param name="callback">The callback responsible to add custom tags.</param>
        public static void AddCallback(HttpRequestMessage request, Action<HttpMetricsEnrichmentContext> callback)
        {
            HttpRequestOptions options = request.Options;
            if (!options.TryGetValue(s_optionsKeyForContext, out HttpMetricsEnrichmentContext? context))
            {
                if (s_contextCache.TryDequeue(out context))
                {
                    Interlocked.Decrement(ref s_contextCacheItemCount);
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

        internal void RecordWithEnrichment(HttpRequestMessage request,
            HttpResponseMessage? response,
            Exception? exception,
            long startTimestamp,
            in TagList commonTags,
            bool recordRequestDuration,
            bool recordFailedRequests,
            Histogram<double> requestDuration,
            Counter<long> failedRequests)
        {
            _request = request;
            _response = response;
            _exception = exception;

            Debug.Assert(_tags.Count == 0);

            // Adding the enrichment tags to TagList would likely exceed its' on-stack capacity, leading to an allocation.
            // To avoid this, we copy all the tags to a List<T> which is cached together with HttpMetricsEnrichmentContext.
            // Use a for loop to iterate over TagList, since TagList.GetEnumerator() allocates.
            // https://github.com/dotnet/runtime/issues/87022
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

                if (recordRequestDuration)
                {
                    TimeSpan duration = Stopwatch.GetElapsedTime(startTimestamp, Stopwatch.GetTimestamp());
                    requestDuration.Record(duration.TotalSeconds, CollectionsMarshal.AsSpan(_tags));
                }

                if (recordFailedRequests)
                {
                    failedRequests.Add(1, CollectionsMarshal.AsSpan(_tags));
                }
            }
            finally
            {
                _request = null;
                _response = null;
                _exception = null;
                _callbacks.Clear();
                _tags.Clear();

                if (Interlocked.Increment(ref s_contextCacheItemCount) <= ContextCacheCapacity)
                {
                    s_contextCache.Enqueue(this);
                }
                else
                {
                    Interlocked.Decrement(ref s_contextCacheItemCount);
                }
            }
        }
    }
}
