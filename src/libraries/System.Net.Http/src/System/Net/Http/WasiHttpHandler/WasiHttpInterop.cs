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
using WasiHttpWorld.wit.imports.wasi.http.v0_2_1;
using WasiHttpWorld.wit.imports.wasi.io.v0_2_1;
using static WasiHttpWorld.wit.imports.wasi.http.v0_2_1.ITypes;
using static WasiHttpWorld.wit.imports.wasi.io.v0_2_1.IStreams;

namespace System.Net.Http
{
    internal static class WasiHttpInterop
    {
        public static Task RegisterWasiPollable(IPoll.Pollable pollable, CancellationToken cancellationToken)
        {
            var handle = pollable.Handle;

            // this will effectively neutralize Dispose() of the Pollable()
            // because in the CoreLib we create another instance, which will dispose it
            pollable.Handle = 0;
            GC.SuppressFinalize(pollable);

            return CallRegisterWasiPollableHandle((Thread)null!, handle, cancellationToken);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "RegisterWasiPollableHandle")]
            static extern Task CallRegisterWasiPollableHandle(Thread t, int handle, CancellationToken cancellationToken);
        }

        public static Method ConvertMethod(HttpMethod requestMethod)
        {
            Method method;
            switch (requestMethod.Method)
            {
                case "":
                case "GET":
                    method = Method.get();
                    break;
                case "HEAD":
                    method = Method.head();
                    break;
                case "POST":
                    method = Method.post();
                    break;
                case "PUT":
                    method = Method.put();
                    break;
                case "DELETE":
                    method = Method.delete();
                    break;
                case "CONNECT":
                    method = Method.connect();
                    break;
                case "OPTIONS":
                    method = Method.options();
                    break;
                case "TRACE":
                    method = Method.trace();
                    break;
                case "PATCH":
                    method = Method.patch();
                    break;
                default:
                    method = Method.other(requestMethod.Method);
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
                    scheme = Scheme.http();
                    break;
                case "https":
                    scheme = Scheme.https();
                    break;
                default:
                    scheme = Scheme.other(uri.Scheme);
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
            return Fields.FromList(headers);
        }

        public static void ConvertResponseHeaders(IncomingResponse incomingResponse, HttpResponseMessage response)
        {
            var headers = incomingResponse.Headers();
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
            headers.Dispose();
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
                case ErrorCode.DNS_TIMEOUT:
                    return "DNS_TIMEOUT";

                case ErrorCode.DNS_ERROR:
                    return "DNS_ERROR";

                case ErrorCode.DESTINATION_NOT_FOUND:
                    return "DESTINATION_NOT_FOUND";

                case ErrorCode.DESTINATION_UNAVAILABLE:
                    return "DESTINATION_UNAVAILABLE";

                case ErrorCode.DESTINATION_IP_PROHIBITED:
                    return "DESTINATION_IP_PROHIBITED";

                case ErrorCode.DESTINATION_IP_UNROUTABLE:
                    return "DESTINATION_IP_UNROUTABLE";

                case ErrorCode.CONNECTION_REFUSED:
                    return "CONNECTION_REFUSED";

                case ErrorCode.CONNECTION_TERMINATED:
                    return "CONNECTION_TERMINATED";

                case ErrorCode.CONNECTION_TIMEOUT:
                    return "CONNECTION_TIMEOUT";

                case ErrorCode.CONNECTION_READ_TIMEOUT:
                    return "CONNECTION_READ_TIMEOUT";

                case ErrorCode.CONNECTION_WRITE_TIMEOUT:
                    return "CONNECTION_WRITE_TIMEOUT";

                case ErrorCode.CONNECTION_LIMIT_REACHED:
                    return "CONNECTION_LIMIT_REACHED";

                case ErrorCode.TLS_PROTOCOL_ERROR:
                    return "TLS_PROTOCOL_ERROR";

                case ErrorCode.TLS_CERTIFICATE_ERROR:
                    return "TLS_CERTIFICATE_ERROR";

                case ErrorCode.TLS_ALERT_RECEIVED:
                    return "TLS_ALERT_RECEIVED";

                case ErrorCode.HTTP_REQUEST_DENIED:
                    return "HTTP_REQUEST_DENIED";

                case ErrorCode.HTTP_REQUEST_LENGTH_REQUIRED:
                    return "HTTP_REQUEST_LENGTH_REQUIRED";

                case ErrorCode.HTTP_REQUEST_BODY_SIZE:
                    return "HTTP_REQUEST_BODY_SIZE";

                case ErrorCode.HTTP_REQUEST_METHOD_INVALID:
                    return "HTTP_REQUEST_METHOD_INVALID";

                case ErrorCode.HTTP_REQUEST_URI_INVALID:
                    return "HTTP_REQUEST_URI_INVALID";

                case ErrorCode.HTTP_REQUEST_URI_TOO_LONG:
                    return "HTTP_REQUEST_URI_TOO_LONG";

                case ErrorCode.HTTP_REQUEST_HEADER_SECTION_SIZE:
                    return "HTTP_REQUEST_HEADER_SECTION_SIZE";

                case ErrorCode.HTTP_REQUEST_HEADER_SIZE:
                    return "HTTP_REQUEST_HEADER_SIZE";

                case ErrorCode.HTTP_REQUEST_TRAILER_SECTION_SIZE:
                    return "HTTP_REQUEST_TRAILER_SECTION_SIZE";

                case ErrorCode.HTTP_REQUEST_TRAILER_SIZE:
                    return "HTTP_REQUEST_TRAILER_SIZE";

                case ErrorCode.HTTP_RESPONSE_INCOMPLETE:
                    return "HTTP_RESPONSE_INCOMPLETE";

                case ErrorCode.HTTP_RESPONSE_HEADER_SECTION_SIZE:
                    return "HTTP_RESPONSE_HEADER_SECTION_SIZE";

                case ErrorCode.HTTP_RESPONSE_HEADER_SIZE:
                    return "HTTP_RESPONSE_HEADER_SIZE";

                case ErrorCode.HTTP_RESPONSE_BODY_SIZE:
                    return "HTTP_RESPONSE_BODY_SIZE";

                case ErrorCode.HTTP_RESPONSE_TRAILER_SECTION_SIZE:
                    return "HTTP_RESPONSE_TRAILER_SECTION_SIZE";

                case ErrorCode.HTTP_RESPONSE_TRAILER_SIZE:
                    return "HTTP_RESPONSE_TRAILER_SIZE";

                case ErrorCode.HTTP_RESPONSE_TRANSFER_CODING:
                    return "HTTP_RESPONSE_TRANSFER_CODING";

                case ErrorCode.HTTP_RESPONSE_CONTENT_CODING:
                    return "HTTP_RESPONSE_CONTENT_CODING";

                case ErrorCode.HTTP_RESPONSE_TIMEOUT:
                    return "HTTP_RESPONSE_TIMEOUT";

                case ErrorCode.HTTP_UPGRADE_FAILED:
                    return "HTTP_UPGRADE_FAILED";

                case ErrorCode.HTTP_PROTOCOL_ERROR:
                    return "HTTP_PROTOCOL_ERROR";

                case ErrorCode.LOOP_DETECTED:
                    return "LOOP_DETECTED";

                case ErrorCode.CONFIGURATION_ERROR:
                    return "CONFIGURATION_ERROR";

                case ErrorCode.INTERNAL_ERROR:
                    return "INTERNAL_ERROR";

                default:
                    return $"{code.Tag}";
            }
        }
    }
}
