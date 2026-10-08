// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WasiHttpWorld;
using WasiHttpWorld.wit.Imports.wasi.http.v0_2_8;
using WasiHttpWorld.wit.Imports.wasi.io.v0_2_8;
using static WasiHttpWorld.wit.Imports.wasi.http.v0_2_8.ITypesImports;
using static WasiHttpWorld.wit.Imports.wasi.io.v0_2_8.IStreamsImports;

namespace System.Net.Http
{
    internal static class WasiHttpInterop
    {
        public static Task RegisterWasiPollable(IPollImports.Pollable pollable, CancellationToken cancellationToken)
        {
            var handle = pollable.Handle;

            // this will effectively neutralize Dispose() of the Pollable()
            // because in the CoreLib we create another instance, which will dispose it
            pollable.Handle = 0;
            GC.SuppressFinalize(pollable);

            return CallRegisterWasiPollableHandle((Thread)null!, handle, true, cancellationToken);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "RegisterWasiPollableHandle")]
            static extern Task CallRegisterWasiPollableHandle(Thread t, int handle, bool ownsPollable, CancellationToken cancellationToken);
        }

        public static Method ConvertMethod(HttpMethod requestMethod)
        {
            Method method;
            switch (requestMethod.Method)
            {
                case "":
                case "GET":
                    method = Method.Get();
                    break;
                case "HEAD":
                    method = Method.Head();
                    break;
                case "POST":
                    method = Method.Post();
                    break;
                case "PUT":
                    method = Method.Put();
                    break;
                case "DELETE":
                    method = Method.Delete();
                    break;
                case "CONNECT":
                    method = Method.Connect();
                    break;
                case "OPTIONS":
                    method = Method.Options();
                    break;
                case "TRACE":
                    method = Method.Trace();
                    break;
                case "PATCH":
                    method = Method.Patch();
                    break;
                default:
                    method = Method.Other(requestMethod.Method);
                    break;
            }
            return method;
        }

        public static Scheme ConvertScheme(Uri uri)
        {
            Scheme scheme;
            switch (uri.Scheme)
            {
                case "":
                case "http":
                    scheme = Scheme.Http();
                    break;
                case "https":
                    scheme = Scheme.Https();
                    break;
                default:
                    scheme = Scheme.Other(uri.Scheme);
                    break;
            }
            return scheme;
        }

        public static string ConvertAuthority(Uri uri)
        {
            // `wasi:http/outgoing-handler` requires a non-empty authority,
            // so we set one here:
            if (string.IsNullOrEmpty(uri.Authority))
            {
                if (uri.Scheme == "https")
                {
                    return ":443";
                }
                else
                {
                    return ":80";
                }
            }
            else
            {
                return uri.Authority;
            }
        }

        public static Fields ConvertRequestHeaders(HttpRequestMessage request)
        {
            var headers = new List<(string, byte[])>();
            foreach (var pair in request.Headers)
            {
                foreach (var value in pair.Value)
                {
                    headers.Add((pair.Key, Encoding.UTF8.GetBytes(value)));
                }
            }
            if (request.Content is not null)
            {
                foreach (var pair in request.Content.Headers)
                {
                    foreach (var value in pair.Value)
                    {
                        headers.Add((pair.Key, Encoding.UTF8.GetBytes(value)));
                    }
                }
            }
            try
            {
                return Fields.FromList(headers);
            }
            catch (WitException e)
            {
                var error = HeaderErrorToString((HeaderError)e.Value);
                throw new HttpRequestException($"Header validation error: {error}");
            }
        }

        private static string HeaderErrorToString(HeaderError error)
        {
            switch (error.Tag)
            {
                case HeaderError.Tags.InvalidSyntax:
                    return "INVALID_SYNTAX";
                case HeaderError.Tags.Forbidden:
                    return "FORBIDDEN";
                case HeaderError.Tags.Immutable:
                    return "IMMUTABLE";
                default:
                    return $"{error.Tag}";
            }
        }

        public static void ConvertResponseHeaders(IncomingResponse incomingResponse, HttpResponseMessage response)
        {
            using var headers = incomingResponse.Headers();
            foreach ((var key, var value) in headers.Entries())
            {
                var valueString = Encoding.UTF8.GetString(value);
                if (IsContentHeader(key))
                {
                    response.Content.Headers.Add(key, valueString);
                }
                else
                {
                    response.Headers.Add(key, valueString);
                }
            }
        }

        private static bool IsContentHeader(string headerName)
        {
            return HeaderDescriptor.TryGet(headerName, out HeaderDescriptor descriptor) && (descriptor.HeaderType & HttpHeaderType.Content) != 0;
        }

        public static string ErrorCodeToString(ErrorCode code)
        {
            // TODO: include payload data in result where applicable
            switch (code.Tag)
            {
                case ErrorCode.Tags.DnsTimeout:
                    return "DNS_TIMEOUT";

                case ErrorCode.Tags.DnsError:
                    return "DNS_ERROR";

                case ErrorCode.Tags.DestinationNotFound:
                    return "DESTINATION_NOT_FOUND";

                case ErrorCode.Tags.DestinationUnavailable:
                    return "DESTINATION_UNAVAILABLE";

                case ErrorCode.Tags.DestinationIpProhibited:
                    return "DESTINATION_IP_PROHIBITED";

                case ErrorCode.Tags.DestinationIpUnroutable:
                    return "DESTINATION_IP_UNROUTABLE";

                case ErrorCode.Tags.ConnectionRefused:
                    return "CONNECTION_REFUSED";

                case ErrorCode.Tags.ConnectionTerminated:
                    return "CONNECTION_TERMINATED";

                case ErrorCode.Tags.ConnectionTimeout:
                    return "CONNECTION_TIMEOUT";

                case ErrorCode.Tags.ConnectionReadTimeout:
                    return "CONNECTION_READ_TIMEOUT";

                case ErrorCode.Tags.ConnectionWriteTimeout:
                    return "CONNECTION_WRITE_TIMEOUT";

                case ErrorCode.Tags.ConnectionLimitReached:
                    return "CONNECTION_LIMIT_REACHED";

                case ErrorCode.Tags.TlsProtocolError:
                    return "TLS_PROTOCOL_ERROR";

                case ErrorCode.Tags.TlsCertificateError:
                    return "TLS_CERTIFICATE_ERROR";

                case ErrorCode.Tags.TlsAlertReceived:
                    return "TLS_ALERT_RECEIVED";

                case ErrorCode.Tags.HttpRequestDenied:
                    return "HTTP_REQUEST_DENIED";

                case ErrorCode.Tags.HttpRequestLengthRequired:
                    return "HTTP_REQUEST_LENGTH_REQUIRED";

                case ErrorCode.Tags.HttpRequestBodySize:
                    return "HTTP_REQUEST_BODY_SIZE";

                case ErrorCode.Tags.HttpRequestMethodInvalid:
                    return "HTTP_REQUEST_METHOD_INVALID";

                case ErrorCode.Tags.HttpRequestUriInvalid:
                    return "HTTP_REQUEST_URI_INVALID";

                case ErrorCode.Tags.HttpRequestUriTooLong:
                    return "HTTP_REQUEST_URI_TOO_LONG";

                case ErrorCode.Tags.HttpRequestHeaderSectionSize:
                    return "HTTP_REQUEST_HEADER_SECTION_SIZE";

                case ErrorCode.Tags.HttpRequestHeaderSize:
                    return "HTTP_REQUEST_HEADER_SIZE";

                case ErrorCode.Tags.HttpRequestTrailerSectionSize:
                    return "HTTP_REQUEST_TRAILER_SECTION_SIZE";

                case ErrorCode.Tags.HttpRequestTrailerSize:
                    return "HTTP_REQUEST_TRAILER_SIZE";

                case ErrorCode.Tags.HttpResponseIncomplete:
                    return "HTTP_RESPONSE_INCOMPLETE";

                case ErrorCode.Tags.HttpResponseHeaderSectionSize:
                    return "HTTP_RESPONSE_HEADER_SECTION_SIZE";

                case ErrorCode.Tags.HttpResponseHeaderSize:
                    return "HTTP_RESPONSE_HEADER_SIZE";

                case ErrorCode.Tags.HttpResponseBodySize:
                    return "HTTP_RESPONSE_BODY_SIZE";

                case ErrorCode.Tags.HttpResponseTrailerSectionSize:
                    return "HTTP_RESPONSE_TRAILER_SECTION_SIZE";

                case ErrorCode.Tags.HttpResponseTrailerSize:
                    return "HTTP_RESPONSE_TRAILER_SIZE";

                case ErrorCode.Tags.HttpResponseTransferCoding:
                    return "HTTP_RESPONSE_TRANSFER_CODING";

                case ErrorCode.Tags.HttpResponseContentCoding:
                    return "HTTP_RESPONSE_CONTENT_CODING";

                case ErrorCode.Tags.HttpResponseTimeout:
                    return "HTTP_RESPONSE_TIMEOUT";

                case ErrorCode.Tags.HttpUpgradeFailed:
                    return "HTTP_UPGRADE_FAILED";

                case ErrorCode.Tags.HttpProtocolError:
                    return "HTTP_PROTOCOL_ERROR";

                case ErrorCode.Tags.LoopDetected:
                    return "LOOP_DETECTED";

                case ErrorCode.Tags.ConfigurationError:
                    return "CONFIGURATION_ERROR";

                case ErrorCode.Tags.InternalError:
                    return "INTERNAL_ERROR";

                default:
                    return $"{code.Tag}";
            }
        }
    }
}
