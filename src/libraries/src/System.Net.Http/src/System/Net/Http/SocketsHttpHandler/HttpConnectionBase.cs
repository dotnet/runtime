// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal abstract class HttpConnectionBase : IHttpTrace
    {
        public abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
        public abstract void Trace(string message, [CallerMemberName] string memberName = null);

        protected void TraceConnection(Stream stream)
        {
            if (stream is SslStream sslStream)
            {
                Trace(
                    $"{this}. " +
                    $"SslProtocol:{sslStream.SslProtocol}, NegotiatedApplicationProtocol:{sslStream.NegotiatedApplicationProtocol}, " +
                    $"NegotiatedCipherSuite:{sslStream.NegotiatedCipherSuite}, CipherAlgorithm:{sslStream.CipherAlgorithm}, CipherStrength:{sslStream.CipherStrength}, " +
                    $"HashAlgorithm:{sslStream.HashAlgorithm}, HashStrength:{sslStream.HashStrength}, " +
                    $"KeyExchangeAlgorithm:{sslStream.KeyExchangeAlgorithm}, KeyExchangeStrength:{sslStream.KeyExchangeStrength}, " +
                    $"LocalCertificate:{sslStream.LocalCertificate}, RemoteCertificate:{sslStream.RemoteCertificate}");
            }
            else
            {
                Trace($"{this}");
            }
        }

        private long CreationTickCount { get; } = Environment.TickCount64;

        // Check if lifetime expired on connection.
        public bool LifetimeExpired(long nowTicks, TimeSpan lifetime)
        {
            bool expired =
                lifetime != Timeout.InfiniteTimeSpan &&
                (lifetime == TimeSpan.Zero || (nowTicks - CreationTickCount) > lifetime.TotalMilliseconds);

            if (expired && NetEventSource.IsEnabled) Trace($"Connection no longer usable. Alive {TimeSpan.FromMilliseconds((nowTicks - CreationTickCount))} > {lifetime}.");
            return expired;
        }

        internal static HttpRequestException CreateRetryException()
        {
            // This is an exception that's thrown during request processing to indicate that the 
            // attempt to send the request failed in such a manner that the server is guaranteed to not have 
            // processed the request in any way, and thus the request can be retried.
            // This will be caught in HttpConnectionPool.SendWithRetryAsync and the retry logic will kick in.
            // The user should never see this exception. 
            throw new HttpRequestException(null, null, allowRetry: true);
        }

        internal static bool IsDigit(byte c) => (uint)(c - '0') <= '9' - '0';

        /// <summary>Awaits a task, ignoring any resulting exceptions.</summary>
        internal static void IgnoreExceptions(ValueTask<int> task)
        {
            _ = IgnoreExceptionsAsync(task);

            async Task IgnoreExceptionsAsync(ValueTask<int> task)
            {
                try { await task.ConfigureAwait(false); } catch { }
            }
        }

        /// <summary>Awaits a task, ignoring any resulting exceptions.</summary>
        internal static void IgnoreExceptions(Task task)
        {
            _ = IgnoreExceptionsAsync(task);

            async Task IgnoreExceptionsAsync(Task task)
            {
                try { await task.ConfigureAwait(false); } catch { }
            }
        }

        /// <summary>Awaits a task, logging any resulting exceptions (which are otherwise ignored).</summary>
        internal void LogExceptions(Task task)
        {
            _ = LogExceptionsAsync(task);

            async Task LogExceptionsAsync(Task task)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    if (NetEventSource.IsEnabled) Trace($"Exception from asynchronous processing: {e}");
                }
            }
        }
    }
}
