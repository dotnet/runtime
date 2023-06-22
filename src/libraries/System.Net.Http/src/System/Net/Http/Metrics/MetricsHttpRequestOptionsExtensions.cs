// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net.Http.Metrics;

public static class MetricsHttpRequestOptionsExtensions
{
    // TODO: To eliminate the extra allocation of List<T> for common cases, define a custom collection that embeds callback instances up to a certain number.
    private static readonly HttpRequestOptionsKey<List<Action<HttpMetricsEnrichmentContext>>> s_callbackCollectionKey = new("MetricsEnrichmentCallbackCollection");

    public static void AddMetricsEnrichmentCallback(this HttpRequestOptions options, Action<HttpMetricsEnrichmentContext> callback)
    {
        if (!options.TryGetValue(s_callbackCollectionKey, out List<Action<HttpMetricsEnrichmentContext>>? callbackCollection))
        {
            callbackCollection = new List<Action<HttpMetricsEnrichmentContext>>();
            options.Set(s_callbackCollectionKey, callbackCollection);
        }
        callbackCollection.Add(callback);
    }

    internal static bool TryGetMetricsEnrichmentCallbackCollection(this HttpRequestOptions options, out List<Action<HttpMetricsEnrichmentContext>>? callbackCollection)
    {
        return options.TryGetValue(s_callbackCollectionKey, out callbackCollection);
    }
}
