// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static class DiagnosticsHelper
    {
        internal static string GetRedactedUriString(Uri uri)
        {
            Debug.Assert(uri.IsAbsoluteUri);
            string uriString = uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.UserInfo, UriFormat.UriEscaped);

            if (GlobalHttpSettings.DiagnosticsHandler.DisableUriQueryRedaction)
            {
                return uriString;
            }

            int fragmentOffset = uriString.IndexOf('#');
            int queryOffset = uriString.IndexOf('?');
            if (fragmentOffset >= 0 && queryOffset > fragmentOffset)
            {
                // Not a query delimiter, but a '?' in the fragment.
                queryOffset = -1;
            }

            if (queryOffset < 0 || queryOffset == uriString.Length - 1 || queryOffset == fragmentOffset - 1)
            {
                // No query or empty query.
                return uriString;
            }

            if (fragmentOffset < 0)
            {
                return $"{uriString.AsSpan(0, queryOffset + 1)}*";
            }
            else
            {
                ReadOnlySpan<char> uriSpan = uriString.AsSpan();
                return $"{uriSpan[..(queryOffset + 1)]}*{uriSpan[fragmentOffset..]}";
            }
        }

        internal static KeyValuePair<string, object?> GetMethodTag(HttpMethod method)
        {
            // Return canonical names for known methods and "_OTHER" for unknown ones.
            HttpMethod? known = HttpMethod.GetKnownMethod(method.Method);
            return new KeyValuePair<string, object?>("http.request.method", known?.Method ?? "_OTHER");
        }

        internal static string GetProtocolVersionString(Version httpVersion) => (httpVersion.Major, httpVersion.Minor) switch
        {
            (1, 0) => "1.0",
            (1, 1) => "1.1",
            (2, 0) => "2",
            (3, 0) => "3",
            _ => httpVersion.ToString()
        };

        public static bool TryGetErrorType(HttpResponseMessage? response, Exception? exception, out string? errorType)
        {
            if (response is not null)
            {
                int statusCode = (int)response.StatusCode;

                // In case the status code indicates a client or a server error, return the string representation of the status code.
                // See the paragraph Status and the definition of 'error.type' in
                // https://github.com/open-telemetry/semantic-conventions/blob/2bad9afad58fbd6b33cc683d1ad1f006e35e4a5d/docs/http/http-spans.md
                if (statusCode >= 400 && statusCode <= 599)
                {
                    errorType = GetErrorStatusCodeString(statusCode);
                    return true;
                }
            }

            if (exception is null)
            {
                errorType = null;
                return false;
            }

            Debug.Assert(Enum.GetValues<HttpRequestError>().Length == 12, "We need to extend the mapping in case new values are added to HttpRequestError.");
            errorType = (exception as HttpRequestException)?.HttpRequestError switch
            {
                HttpRequestError.NameResolutionError => "name_resolution_error",
                HttpRequestError.ConnectionError => "connection_error",
                HttpRequestError.SecureConnectionError => "secure_connection_error",
                HttpRequestError.HttpProtocolError => "http_protocol_error",
                HttpRequestError.ExtendedConnectNotSupported => "extended_connect_not_supported",
                HttpRequestError.VersionNegotiationError => "version_negotiation_error",
                HttpRequestError.UserAuthenticationError => "user_authentication_error",
                HttpRequestError.ProxyTunnelError => "proxy_tunnel_error",
                HttpRequestError.InvalidResponse => "invalid_response",
                HttpRequestError.ResponseEnded => "response_ended",
                HttpRequestError.ConfigurationLimitExceeded => "configuration_limit_exceeded",

                // Fall back to the exception type name in case of HttpRequestError.Unknown or when exception is not an HttpRequestException.
                _ => exception.GetType().FullName!
            };
            return true;
        }

        private static object[]? s_boxedStatusCodes;
        private static string[]? s_statusCodeStrings;

#pragma warning disable CA1859 // we explictly box here
        public static object GetBoxedStatusCode(int statusCode)
        {
            object[] boxes = LazyInitializer.EnsureInitialized(ref s_boxedStatusCodes, static () => new object[512]);

            return (uint)statusCode < (uint)boxes.Length
                ? boxes[statusCode] ??= statusCode
                : statusCode;
        }
#pragma warning restore

        private static string GetErrorStatusCodeString(int statusCode)
        {
            Debug.Assert(statusCode >= 400 && statusCode <= 599);

            string[] strings = LazyInitializer.EnsureInitialized(ref s_statusCodeStrings, static () => new string[200]);
            int index = statusCode - 400;
            return (uint)index < (uint)strings.Length
                ? strings[index] ??= statusCode.ToString()
                : statusCode.ToString();
        }
    }
}
