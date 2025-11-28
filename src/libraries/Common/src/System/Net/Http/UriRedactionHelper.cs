// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    internal static class UriRedactionHelper
    {
        public static bool IsDisabled { get; } = GetDisableUriRedactionSettingValue();

        private static bool GetDisableUriRedactionSettingValue()
        {
            if (AppContext.TryGetSwitch("System.Net.Http.DisableUriRedaction", out bool value))
            {
                return value;
            }

            string? envVar = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_DISABLEURIREDACTION");

            if (bool.TryParse(envVar, out value))
            {
                return value;
            }
            else if (uint.TryParse(envVar, out uint intVal))
            {
                return intVal != 0;
            }

            return false;
        }

        public static string GetRedactedPathAndQuery(string pathAndQuery)
        {
            Debug.Assert(pathAndQuery is not null);

            if (!IsDisabled)
            {
                int queryIndex = pathAndQuery.IndexOf('?');
                if (queryIndex >= 0 && queryIndex < (pathAndQuery.Length - 1))
                {
                    pathAndQuery = $"{Slice(pathAndQuery, 0, queryIndex + 1)}*";
                }
            }

            return pathAndQuery;
        }

        [return: NotNullIfNotNull(nameof(uri))]
        public static string? GetRedactedUriString(Uri? uri)
        {
            if (uri is null)
            {
                return null;
            }

            if (IsDisabled)
            {
                return uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString();
            }

            if (!uri.IsAbsoluteUri)
            {
                // We cannot guarantee the redaction of UserInfo for relative Uris without implementing some subset of Uri parsing.
                // To avoid this, we redact the whole Uri. Seeing a relative Uri here requires a custom handler chain with
                // custom expansion logic implemented by the user's HttpMessageHandler.
                // In such advanced scenarios we recommend users to log the Uri in their handler.
                return "*";
            }

            string pathAndQuery = uri.PathAndQuery;
            int queryIndex = pathAndQuery.IndexOf('?');

            bool redactQuery = queryIndex >= 0 && // Query is present.
                queryIndex < pathAndQuery.Length - 1; // Query is not empty.

            return (redactQuery, uri.IsDefaultPort) switch
            {
                (true, true) => $"{uri.Scheme}://{uri.Host}{Slice(pathAndQuery, 0, queryIndex + 1)}*",
                (true, false) => $"{uri.Scheme}://{uri.Host}:{uri.Port}{Slice(pathAndQuery, 0, queryIndex + 1)}*",
                (false, true) => $"{uri.Scheme}://{uri.Host}{pathAndQuery}",
                (false, false) => $"{uri.Scheme}://{uri.Host}:{uri.Port}{pathAndQuery}"
            };
        }

#if NET
        private static ReadOnlySpan<char> Slice(string text, int startIndex, int length) => text.AsSpan(startIndex, length);
#else
        private static string Slice(string text, int startIndex, int length) => text.Substring(startIndex, length);
#endif
    }
}
