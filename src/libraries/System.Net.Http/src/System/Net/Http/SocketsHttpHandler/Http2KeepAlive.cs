// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        internal enum KeepAliveState
        {
            None,
            PingSent
        }

        internal class Http2KeepAlive
        {
            private long _pingPayload;
            private readonly TimeSpan _keepAlivePingDelay;
            private readonly TimeSpan _keepAlivePingTimeout;
            private long _nextPingRequestTimestamp;
            private long _pingTimeoutTimestamp;
            private HttpKeepAlivePingPolicy _keepAlivePingPolicy;
            private volatile KeepAliveState _state;
            private Http2Connection _connection;

            public Http2KeepAlive(Http2Connection connection, HttpConnectionSettings settings)
            {
                _keepAlivePingDelay = settings._keepAlivePingDelay;
                _keepAlivePingTimeout = settings._keepAlivePingTimeout;
                _nextPingRequestTimestamp = Environment.TickCount64 + (long)settings._keepAlivePingDelay.TotalMilliseconds;
                _keepAlivePingPolicy = settings._keepAlivePingPolicy;
                _connection = connection;
            }

            public void RefreshTimestamp()
            {
                _nextPingRequestTimestamp = Environment.TickCount64 + (long)_keepAlivePingDelay.TotalMilliseconds;
            }

            public bool ProcessPingAck(long payload)
            {
                if (_state != KeepAliveState.PingSent)
                    return false;
                if (Interlocked.Read(ref _pingPayload) != payload)
                    return false;
                _state = KeepAliveState.None;
                RefreshTimestamp();
                return true;
            }

            public void VerifyKeepAlive()
            {
                if (_keepAlivePingPolicy == HttpKeepAlivePingPolicy.WithActiveRequests && _connection._httpStreams.Count == 0)
                    return;

                var now = Environment.TickCount64;
                switch (_state)
                {
                    case KeepAliveState.None:
                        // Check whether keep alive delay has passed since last frame received
                        if (_keepAlivePingDelay != Timeout.InfiniteTimeSpan && now > _nextPingRequestTimestamp)
                        {
                            // Set the status directly to ping sent and set the timestamp
                            _state = KeepAliveState.PingSent;
                            _pingTimeoutTimestamp = now + (long)_keepAlivePingTimeout.TotalMilliseconds;

                            long pingPayload = Interlocked.Increment(ref _pingPayload);
                            _connection.SendPingAsync(pingPayload);
                            return;
                        }
                        break;
                    case KeepAliveState.PingSent:
                        if (now > _pingTimeoutTimestamp)
                            Http2Connection.ThrowProtocolError();

                        break;
                }
            }
        }
    }
}
