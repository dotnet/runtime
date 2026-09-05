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
        // Tag lists are flattened into a single "key=value,key=value" string. Because tag keys
        // and values are arbitrary strings that may themselves contain the ',' pair separator or
        // the '=' key/value separator, each key and value is escaped so the string can be decoded
        // without ambiguity. The escaping rules are:
        //   '\' => "\\"   ','  => "\,"   '='  => "\="
        // The ',' between pairs and the '=' between a key and its value are emitted literally
        // (unescaped) as delimiters.
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

                AppendEscaped(sb, tag.Key);
                sb.Append('=');
                AppendEscaped(sb, tag.Value?.ToString());
            }
            return sb.ToString();
        }

        internal static string FormatTags(KeyValuePair<string, string>[] labels)
        {
            if (labels is null || labels.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < labels.Length; i++)
            {
                AppendEscaped(sb, labels[i].Key);
                sb.Append('=');
                AppendEscaped(sb, labels[i].Value);
                if (i != labels.Length - 1)
                {
                    sb.Append(',');
                }
            }
            return sb.ToString();
        }

        // Escapes the '\', ',' and '=' characters in a tag key or value so the flattened
        // "key=value,key=value" representation produced by FormatTags can be decoded without
        // ambiguity. See the comment on FormatTags for the encoding details.
        private static void AppendEscaped(StringBuilder sb, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            foreach (char c in value)
            {
                if (c is '\\' or ',' or '=')
                {
                    sb.Append('\\');
                }

                sb.Append(c);
            }
        }

        internal static string FormatObjectHash(object? obj) =>
            obj is null ? string.Empty : RuntimeHelpers.GetHashCode(obj).ToString(CultureInfo.InvariantCulture);
    }
}
