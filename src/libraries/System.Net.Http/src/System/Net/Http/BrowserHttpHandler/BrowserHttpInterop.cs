// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static partial class BrowserHttpInterop
    {
        [JSImport("INTERNAL.http_wasm_supports_streaming_response")]
        public static partial bool SupportsStreamingResponse();

        [JSImport("INTERNAL.http_wasm_create_abort_controler")]
        public static partial JSObject CreateAbortController();

        [JSImport("INTERNAL.http_wasm_abort_request")]
        public static partial void AbortRequest(
            JSObject abortController);

        [JSImport("INTERNAL.http_wasm_abort_response")]
        public static partial void AbortResponse(
            JSObject fetchResponse);

        [JSImport("INTERNAL.http_wasm_get_response_header_names")]
        private static partial string[] _GetResponseHeaderNames(
            JSObject fetchResponse);

        [JSImport("INTERNAL.http_wasm_get_response_header_values")]
        private static partial string[] _GetResponseHeaderValues(
            JSObject fetchResponse);

        public static void GetResponseHeaders(JSObject fetchResponse, HttpHeaders resposeHeaders, HttpHeaders contentHeaders)
        {
            string[] headerNames = _GetResponseHeaderNames(fetchResponse);
            string[] headerValues = _GetResponseHeaderValues(fetchResponse);

            for (int i = 0; i < headerNames.Length; i++)
            {
                if (!resposeHeaders.TryAddWithoutValidation(headerNames[i], headerValues[i]))
                {
                    contentHeaders.TryAddWithoutValidation(headerNames[i], headerValues[i]);
                }
            }
        }


        [JSImport("INTERNAL.http_wasm_fetch")]
        public static partial Task<JSObject> Fetch(
            string uri,
            string[] headerNames,
            string[] headerValues,
            string[] optionNames,
            [JSMarshalAs<JSType.Array<JSType.Any>>] object?[] optionValues,
            JSObject abortControler,
            string? body = null);

        [JSImport("INTERNAL.http_wasm_fetch_bytes")]
        private static partial Task<JSObject> FetchBytes(
            string uri,
            string[] headerNames,
            string[] headerValues,
            string[] optionNames,
            [JSMarshalAs<JSType.Array<JSType.Any>>] object?[] optionValues,
            JSObject abortControler,
            IntPtr bodyPtr,
            int bodyLength
            );

        public static unsafe Task<JSObject> Fetch(string uri, string[] headerNames, string[] headerValues, string[] optionNames, object?[] optionValues, JSObject abortControler, byte[] body)
        {
            fixed (byte* ptr = body)
            {
                return FetchBytes(uri, headerNames, headerValues, optionNames, optionValues, abortControler, (IntPtr)ptr, body.Length);
            }
        }

        [JSImport("INTERNAL.http_wasm_get_streamed_response_bytes")]
        public static partial Task<int> GetStreamedResponseBytes(
            JSObject fetchResponse,
            IntPtr bufferPtr,
            int bufferLength);

        [JSImport("INTERNAL.http_wasm_get_response_length")]
        public static partial Task<int> GetResponseLength(
            JSObject fetchResponse);

        [JSImport("INTERNAL.http_wasm_get_response_bytes")]
        public static partial int GetResponseBytes(
            JSObject fetchResponse,
            [JSMarshalAs<JSType.MemoryView>] Span<byte> buffer);


        public static async ValueTask<T> CancelationHelper<T>(Task<T> promise, CancellationToken cancellationToken, JSObject? abortController = null, JSObject? fetchResponse = null)
        {
            if (promise.IsCompletedSuccessfully)
            {
                return promise.Result;
            }
            try
            {
                using (var operationRegistration = cancellationToken.Register(() =>
                {
                    CancelablePromise.CancelPromise(promise);
                    if (abortController != null)
                    {
                        AbortRequest(abortController);
                    }
                    if (fetchResponse != null)
                    {
                        AbortResponse(fetchResponse);
                    }

                }))
                {
                    return await promise.ConfigureAwait(true);
                }
            }
            catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested)
            {
                throw CancellationHelper.CreateOperationCanceledException(oce, cancellationToken);
            }
            catch (JSException jse)
            {
                if (jse.Message.StartsWith("AbortError", StringComparison.Ordinal))
                {
                    throw CancellationHelper.CreateOperationCanceledException(jse, CancellationToken.None);
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    throw CancellationHelper.CreateOperationCanceledException(jse, cancellationToken);
                }
                throw new HttpRequestException(jse.Message, jse);
            }
        }
    }

}
