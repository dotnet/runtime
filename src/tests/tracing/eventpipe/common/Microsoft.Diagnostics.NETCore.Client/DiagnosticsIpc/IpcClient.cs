// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal class IpcClient
    {
        // The amount of time to wait for a stream to be available for consumption by the Connect method.
        // Normally expect the runtime to respond quickly but resource constrained machines may take longer.
        internal static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Sends a single DiagnosticsIpc Message to the dotnet process associated with the <paramref name="endpoint"/>.
        /// </summary>
        /// <param name="endpoint">An endpoint that provides a diagnostics connection to a runtime instance.</param>
        /// <param name="message">The DiagnosticsIpc Message to be sent</param>
        /// <returns>An <see cref="IpcMessage"/> that is the response message.</returns>
        public static IpcMessage SendMessage(IpcEndpoint endpoint, IpcMessage message)
        {
            using IpcResponse response = SendMessageGetContinuation(endpoint, message);
            return response.Message;
        }

        /// <summary>
        /// Sends a single DiagnosticsIpc Message to the dotnet process associated with the <paramref name="endpoint"/>.
        /// </summary>
        /// <param name="endpoint">An endpoint that provides a diagnostics connection to a runtime instance.</param>
        /// <param name="message">The DiagnosticsIpc Message to be sent</param>
        /// <returns>An <see cref="IpcResponse"/> containing the response message and continuation stream.</returns>
        public static IpcResponse SendMessageGetContinuation(IpcEndpoint endpoint, IpcMessage message)
        {
            Stream stream = null;
            try
            {
                stream = endpoint.Connect(ConnectTimeout);

                Write(stream, message);

                IpcMessage response = Read(stream);

                return new IpcResponse(response, Release(ref stream));
            }
            finally
            {
                stream?.Dispose();
            }
        }

        /// <summary>
        /// Sends a single DiagnosticsIpc Message to the dotnet process associated with the <paramref name="endpoint"/>.
        /// </summary>
        /// <param name="endpoint">An endpoint that provides a diagnostics connection to a runtime instance.</param>
        /// <param name="message">The DiagnosticsIpc Message to be sent</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>An <see cref="IpcMessage"/> that is the response message.</returns>
        public static async Task<IpcMessage> SendMessageAsync(IpcEndpoint endpoint, IpcMessage message, CancellationToken cancellationToken)
        {
            using IpcResponse response = await SendMessageGetContinuationAsync(endpoint, message, cancellationToken).ConfigureAwait(false);
            return response.Message;
        }

        /// <summary>
        /// Sends a single DiagnosticsIpc Message to the dotnet process associated with the <paramref name="endpoint"/>.
        /// </summary>
        /// <param name="endpoint">An endpoint that provides a diagnostics connection to a runtime instance.</param>
        /// <param name="message">The DiagnosticsIpc Message to be sent</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>An <see cref="IpcResponse"/> containing the response message and continuation stream.</returns>
        public static async Task<IpcResponse> SendMessageGetContinuationAsync(IpcEndpoint endpoint, IpcMessage message, CancellationToken cancellationToken)
        {
            Stream stream = null;
            try
            {
                stream = await endpoint.ConnectAsync(cancellationToken).ConfigureAwait(false);

                await WriteAsync(stream, message, cancellationToken).ConfigureAwait(false);

                IpcMessage response = await ReadAsync(stream, cancellationToken).ConfigureAwait(false);

                return new IpcResponse(response, Release(ref stream));
            }
            finally
            {
                stream?.Dispose();
            }
        }

        private static void Write(Stream stream, IpcMessage message)
        {
            byte[] buffer = message.Serialize();
            stream.Write(buffer, 0, buffer.Length);
        }

        private static Task WriteAsync(Stream stream, IpcMessage message, CancellationToken cancellationToken)
        {
            byte[] buffer = message.Serialize();
            return stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        private static IpcMessage Read(Stream stream)
        {
            return IpcMessage.Parse(stream);
        }

        private static Task<IpcMessage> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            return IpcMessage.ParseAsync(stream, cancellationToken);
        }

        private static Stream Release(ref Stream stream1)
        {
            Stream intermediate = stream1;
            stream1 = null;
            return intermediate;
        }
    }
}
