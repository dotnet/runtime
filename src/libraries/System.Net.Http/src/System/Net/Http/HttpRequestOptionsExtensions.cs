// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    public static class HttpRequestOptionsExtensions
    {
        private static readonly HttpRequestOptionsKey<IReadOnlyCollection<KeyValuePair<string, object?>>> s_CustomMetricsTagsKey = new("CustomMetricsTags");

        public static void SetCustomMetricsTags(this HttpRequestOptions options, IReadOnlyCollection<KeyValuePair<string, object?>> tags)
        {
            options.Set(s_CustomMetricsTagsKey, tags);
        }

        internal static bool TryGetCustomMetricsTags(this HttpRequestOptions options, [MaybeNullWhen(false)] out IReadOnlyCollection<KeyValuePair<string, object?>>? tags) =>
            options.TryGetValue(s_CustomMetricsTagsKey, out tags);
    }
}
