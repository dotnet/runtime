// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    public static class HttpRequestOptionsExtensions
    {
        private static readonly HttpRequestOptionsKey<ICollection<KeyValuePair<string, object?>>> s_CustomMetricsTagsKey = new("CustomMetricsTags");

        public static ICollection<KeyValuePair<string, object?>> GetCustomMetricsTags(this HttpRequestOptions options)
        {
            ICollection<KeyValuePair<string, object?>>? tags;
            if (options.TryGetValue(s_CustomMetricsTagsKey, out tags))
            {
                if (tags.IsReadOnly)
                {
                    throw new Exception("A readonly collection has been assigned previously for the CustomMetricsTags key.");
                }
            }
            else
            {
                tags = default(TagList);
                options.Set(s_CustomMetricsTagsKey, tags);
            }
            return tags;
        }

        internal static bool TryGetCustomMetricsTags(this HttpRequestOptions options, [MaybeNullWhen(false)] out ICollection<KeyValuePair<string, object?>>? tags) =>
            options.TryGetValue(s_CustomMetricsTagsKey, out tags);
    }
}
