// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static partial class BrowserHttpInterop
    {
        [JSImport("INTERNAL.http_wasm_supports_streaming_request")]
        public static partial bool SupportsStreamingRequest();

        [JSImport("INTERNAL.http_wasm_supports_streaming_response")]
        public static partial bool SupportsStreamingResponse();

        [JSImport("INTERNAL.http_wasm_create_controller")]
        public static partial JSObject CreateController();

        [JSImport("INTERNAL.http_wasm_abort_request")]
        public static partial void AbortRequest(
            JSObject httpController);

        [JSImport("INTERNAL.http_wasm_abort_response")]
        public static partial void AbortResponse(
            JSObject httpController);

        [JSImport("INTERNAL.http_wasm_transform_stream_write")]
        public static partial Task TransformStreamWrite(
            JSObject httpController,
            IntPtr bufferPtr,
            int bufferLength);

        public static unsafe Task TransformStreamWriteUnsafe(JSObject httpController, ReadOnlyMemory<byte> buffer, Buffers.MemoryHandle handle)
            => TransformStreamWrite(httpController, (nint)handle.Pointer, buffer.Length);

        [JSImport("INTERNAL.http_wasm_transform_stream_close")]
        public static partial Task TransformStreamClose(
            JSObject httpController);

        [JSImport("INTERNAL.http_wasm_get_response_header_names")]
        private static partial string[] _GetResponseHeaderNames(
            JSObject httpController);

        [JSImport("INTERNAL.http_wasm_get_response_header_values")]
        private static partial string[] _GetResponseHeaderValues(
            JSObject httpController);

        [JSImport("INTERNAL.http_wasm_get_response_status")]
        public static partial int GetResponseStatus(
            JSObject httpController);

        [JSImport("INTERNAL.http_wasm_get_response_type")]
        public static partial string GetResponseType(
            JSObject httpController);

        public static void GetResponseHeaders(JSObject httpController, HttpHeaders resposeHeaders, HttpHeaders contentHeaders)
        {
            string[] headerNames = _GetResponseHeaderNames(httpController);
            string[] headerValues = _GetResponseHeaderValues(httpController);

            // Some of the headers may not even be valid header types in .NET thus we use TryAddWithoutValidation
            // CORS will only allow access to certain headers on browser.
            for (int i = 0; i < headerNames.Length; i++)
            {
                if (!resposeHeaders.TryAddWithoutValidation(headerNames[i], headerValues[i]))
                {
                    contentHeaders.TryAddWithoutValidation(headerNames[i], headerValues[i]);
                }
            }
        }

        [JSImport("INTERNAL.http_wasm_fetch")]
        public static partial Task Fetch(
            JSObject httpController,
            string uri,
            string[] headerNames,
            string[] headerValues,
            string[] optionNames,
            [JSMarshalAs<JSType.Array<JSType.Any>>] object?[] optionValues);

        [JSImport("INTERNAL.http_wasm_fetch_stream")]
        public static partial Task FetchStream(
            JSObject httpController,
            string uri,
            string[] headerNames,
            string[] headerValues,
            string[] optionNames,
            [JSMarshalAs<JSType.Array<JSType.Any>>] object?[] optionValues);

        [JSImport("INTERNAL.http_wasm_fetch_bytes")]
        private static partial Task FetchBytes(
            JSObject httpController,
            string uri,
            string[] headerNames,
            string[] headerValues,
            string[] optionNames,
            [JSMarshalAs<JSType.Array<JSType.Any>>] object?[] optionValues,
            IntPtr bodyPtr,
            int bodyLength);

        public static unsafe Task FetchBytes(JSObject httpController, string uri, string[] headerNames, string[] headerValues, string[] optionNames, object?[] optionValues, MemoryHandle pinBuffer, int bodyLength)
        {
            return FetchBytes(httpController, uri, headerNames, headerValues, optionNames, optionValues, (IntPtr)pinBuffer.Pointer, bodyLength);
        }

        [JSImport("INTERNAL.http_wasm_get_streamed_response_bytes")]
        public static partial Task<int> GetStreamedResponseBytes(
            JSObject fetchResponse,
            IntPtr bufferPtr,
            int bufferLength);

        public static unsafe Task<int> GetStreamedResponseBytesUnsafe(JSObject jsController, Memory<byte> buffer, MemoryHandle handle)
            => GetStreamedResponseBytes(jsController, (IntPtr)handle.Pointer, buffer.Length);


        [JSImport("INTERNAL.http_wasm_get_response_length")]
        public static partial Task<int> GetResponseLength(
            JSObject fetchResponse);

        [JSImport("INTERNAL.http_wasm_get_response_bytes")]
        public static partial int GetResponseBytes(
            JSObject fetchResponse,
            [JSMarshalAs<JSType.MemoryView>] Span<byte> buffer);


        public static async Task CancellationHelper(Task promise, CancellationToken cancellationToken, JSObject jsController)
        {
            Http.CancellationHelper.ThrowIfCancellationRequested(cancellationToken);

            if (promise.IsCompletedSuccessfully)
            {
                return;
            }
            try
            {
                using (var operationRegistration = cancellationToken.Register(static s =>
                {
                    (Task _promise, JSObject _jsController) = ((Task, JSObject))s!;
                    CancelablePromise.CancelPromise(_promise);
                    if (!_jsController.IsDisposed)
                    {
                        AbortResponse(_jsController);
                    }
                }, (promise, jsController)))
                {
                    await promise.ConfigureAwait(true);
                }
            }
            catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested)
            {
                Http.CancellationHelper.ThrowIfCancellationRequested(oce, cancellationToken);
            }
            catch (JSException jse)
            {
                if (jse.Message.StartsWith("AbortError", StringComparison.Ordinal))
                {
                    throw Http.CancellationHelper.CreateOperationCanceledException(jse, CancellationToken.None);
                }
                Http.CancellationHelper.ThrowIfCancellationRequested(jse, cancellationToken);
                throw new HttpRequestException(jse.Message, jse);
            }
        }

        public static async Task<T> CancellationHelper<T>(Task<T> promise, CancellationToken cancellationToken, JSObject jsController)
        {
            Http.CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
            if (promise.IsCompletedSuccessfully)
            {
                return promise.Result;
            }
            await CancellationHelper((Task)promise, cancellationToken, jsController).ConfigureAwait(false);
            return promise.Result;
        }
    }
}
