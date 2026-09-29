// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;

namespace System.Net.Http
{
    internal static class DiagnosticsHelper
    {
        // OTel bucket boundary recommendation for 'http.request.duration':
        // https://github.com/open-telemetry/semantic-conventions/blob/release/v1.23.x/docs/http/http-metrics.md#metric-httpclientrequestduration
        // We are using the same boundaries for durations which are not expected to be longer than an HTTP request.
        public static InstrumentAdvice<double> ShortHistogramAdvice { get; } = new()
        {
            HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
        };

        internal static KeyValuePair<string, object?> GetMethodTag(HttpMethod method, out bool isUnknownMethod)
        {
            // Return canonical names for known methods and "_OTHER" for unknown ones.
            HttpMethod? known = HttpMethod.GetKnownMethod(method.Method);
            isUnknownMethod = known is null;
            return new KeyValuePair<string, object?>("http.request.method", isUnknownMethod ? "_OTHER" : known!.Method);
        }

        internal static string GetProtocolVersionString(Version httpVersion) => (httpVersion.Major, httpVersion.Minor) switch
        {
            (1, 0) => "1.0",
            (1, 1) => "1.1",
            (2, 0) => "2",
            (3, 0) => "3",
            _ => httpVersion.ToString()
        };

        // Picks the value of the 'server.address' tag following rules specified in
        // https://github.com/open-telemetry/semantic-conventions/blob/728e5d1/docs/http/http-spans.md#http-client-span
        // When there is no proxy, we need to prioritize the contents of the Host header.
        // Note that this is a best-effort guess, e.g. we are not checking if proxy.GetProxy(uri) returns null.
        public static string GetServerAddress(HttpRequestMessage request, IWebProxy? proxy)
        {
            Debug.Assert(request.RequestUri is not null);
            if ((proxy is null || proxy.IsBypassed(request.RequestUri)) && request.HasHeaders && request.Headers.Host is string hostHeader)
            {
                return HttpUtilities.ParseHostNameFromHeader(hostHeader);
            }

            return request.RequestUri.IdnHost;
        }

        public static bool TryGetErrorType(HttpResponseMessage? response, Exception? exception, out string? errorType)
        {
            if (response is not null)
            {
                int statusCode = (int)response.StatusCode;

                // In case the status code indicates a client or a server error, return the string representation of the status code.
                // See the paragraph Status and the definition of 'error.type' in
                // https://github.com/open-telemetry/semantic-conventions/blob/release/v1.23.x/docs/http/http-spans.md#Status
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

#pragma warning disable CA1859 // we explicitly box here
        // Returns a pooled object if 'value' is between 0-512,
        // saving allocations for standard HTTP status codes and small port tag values.
        public static object GetBoxedInt32(int value)
        {
            object[] boxes = LazyInitializer.EnsureInitialized(ref s_boxedStatusCodes, static () => new object[512]);

            return (uint)value < (uint)boxes.Length
                ? boxes[value] ??= value
                : value;
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
