// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

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
    /// > The <see cref="HttpMetricsEnrichmentContext"/> instance must not be used from outside of the enrichment callbacks.
    /// </remarks>
    public sealed class HttpMetricsEnrichmentContext
    {
        private static readonly HttpRequestOptionsKey<List<Action<HttpMetricsEnrichmentContext>>> s_optionsKeyForCallbacks = new(nameof(HttpMetricsEnrichmentContext));

        private HttpRequestMessage? _request;
        private HttpResponseMessage? _response;
        private Exception? _exception;
        private TagList _tags;

        private HttpMetricsEnrichmentContext() { } // Hide the default parameterless constructor.

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
        public void AddCustomTag(string name, object? value) => _tags.Add(name, value);

        /// <summary>
        /// Adds a callback to register custom tags for request metrics `http-client-request-duration` and `http-client-failed-requests`.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to apply enrichment to.</param>
        /// <param name="callback">The callback responsible to add custom tags via <see cref="AddCustomTag(string, object?)"/>.</param>
        public static void AddCallback(HttpRequestMessage request, Action<HttpMetricsEnrichmentContext> callback)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(callback);

            HttpRequestOptions options = request.Options;

            if (options.TryGetValue(s_optionsKeyForCallbacks, out List<Action<HttpMetricsEnrichmentContext>>? callbacks))
            {
                callbacks.Add(callback);
            }
            else
            {
                options.Set(s_optionsKeyForCallbacks, [callback]);
            }
        }

        internal static List<Action<HttpMetricsEnrichmentContext>>? GetEnrichmentCallbacksForRequest(HttpRequestMessage request)
        {
            if (request._options is HttpRequestOptions options &&
                options.Remove(s_optionsKeyForCallbacks.Key, out object? callbacks))
            {
                return (List<Action<HttpMetricsEnrichmentContext>>)callbacks!;
            }

            return null;
        }

        internal static void RecordDurationWithEnrichment(
            List<Action<HttpMetricsEnrichmentContext>> callbacks,
            HttpRequestMessage request,
            HttpResponseMessage? response,
            Exception? exception,
            TimeSpan durationTime,
            in TagList commonTags,
            Histogram<double> requestDuration)
        {
            var context = new HttpMetricsEnrichmentContext
            {
                _request = request,
                _response = response,
                _exception = exception,
                _tags = commonTags,
            };

            foreach (Action<HttpMetricsEnrichmentContext> callback in callbacks)
            {
                callback(context);
            }

            requestDuration.Record(durationTime.TotalSeconds, context._tags);
        }
    }
}
