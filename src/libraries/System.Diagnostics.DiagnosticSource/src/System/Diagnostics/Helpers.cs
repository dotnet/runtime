// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics
{
    internal static class Helpers
    {
        internal static string FormatTags(IEnumerable<KeyValuePair<string, object?>>? tags)
        {
            if (tags is null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(',');
                }

                sb.Append(tag.Key).Append('=');

                if (tag.Value is not null)
                {
                    sb.Append(tag.Value.ToString());
                }
            }
            return sb.ToString();
        }

        internal static string FormatTags(KeyValuePair<string, string>[] labels)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < labels.Length; i++)
            {
                sb.Append(labels[i].Key).Append('=').Append(labels[i].Value);
                if (i != labels.Length - 1)
                {
                    sb.Append(',');
                }
            }
            return sb.ToString();
        }

        internal static string FormatObjectHash(object? obj) =>
            obj is null ? string.Empty : RuntimeHelpers.GetHashCode(obj).ToString(CultureInfo.InvariantCulture);
    }
}
