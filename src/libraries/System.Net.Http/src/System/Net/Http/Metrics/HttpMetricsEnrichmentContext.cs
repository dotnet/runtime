// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;

namespace System.Net.Http.Metrics
{
    public sealed class HttpMetricsEnrichmentContext
    {
        private static readonly HttpRequestOptionsKey<HttpMetricsEnrichmentContext> s_optionsKeyForContext = new("HttpMetricsEnrichmentContext");

        private readonly List<Action<HttpMetricsEnrichmentContext>> _callbacks = new List<Action<HttpMetricsEnrichmentContext>>();
        private HttpRequestMessage? _request;
        private HttpResponseMessage? _response;
        private Exception? _exception;
        private List<KeyValuePair<string, object?>> _tags = new(capacity: 16);

        public HttpRequestMessage Request
        {
            get
            {
                EnsureRunningCallback();
                return _request!;
            }
        }

        public HttpResponseMessage? Response
        {
            get
            {
                EnsureRunningCallback();
                return _response;
            }
        }

        public Exception? Exception
        {
            get
            {
                EnsureRunningCallback();
                return _exception;
            }
        }

        internal bool InProgress => _request != null;

        public void AddCustomTag(string name, object? value)
        {
            _tags.Add(new KeyValuePair<string, object?>(name, value));
        }

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
                context = new HttpMetricsEnrichmentContext();
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
            in TagList commonTags,
            bool recordRequestDuration,
            bool recordFailedRequests,
            long startTimestamp,
            Histogram<double> requestDuration,
            Counter<long> failedRequests)
        {
            if (!recordRequestDuration && !recordFailedRequests)
            {
                return;
            }

            _request = request;
            _response = response;
            _exception = exception;

            Debug.Assert(_tags.Count == 0);
            // TagList.GetEnumerator() allocates, use a for loop instead.
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
                _tags.Clear();
                _request = null;
                _response = null;
                _exception = null;
            }
        }

        private void EnsureRunningCallback()
        {
            if (_request == null) throw new InvalidOperationException("An attempt has been made to use HttpMetricsEnrichmentContext outside of an enrichment callback.");
        }
    }
}
