// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Http.Metrics
{
    internal sealed class ConnectionMetrics
    {
        private readonly SocketsHttpHandlerMetrics _metrics;
        private readonly bool _openConnectionsEnabled;
        private readonly object _protocolVersionTag;
        private readonly object _schemeTag;
        private readonly object _hostTag;
        private readonly object? _portTag;
        private readonly object? _peerAddressTag;
        private bool _currentlyIdle;

        public ConnectionMetrics(SocketsHttpHandlerMetrics metrics, string protocolVersion, string scheme, string host, int? port, string? peerAddress)
        {
            _metrics = metrics;
            _openConnectionsEnabled = _metrics.OpenConnections.Enabled;
            _protocolVersionTag = protocolVersion;
            _schemeTag = scheme;
            _hostTag = host;
            _portTag = port;
            _peerAddressTag = peerAddress;
        }

        // TagList is a huge struct, so we avoid storing it in a field to reduce the amount we allocate on the heap.
        private TagList GetTags()
        {
            TagList tags = default;

            tags.Add("network.protocol.version", _protocolVersionTag);
            tags.Add("url.scheme", _schemeTag);
            tags.Add("server.address", _hostTag);

            if (_portTag is not null)
            {
                tags.Add("server.port", _portTag);
            }

            if (_peerAddressTag is not null)
            {
                tags.Add("network.peer.address", _peerAddressTag);
            }

            return tags;
        }

        private static KeyValuePair<string, object?> GetStateTag(bool idle) => new KeyValuePair<string, object?>("http.connection.state", idle ? "idle" : "active");

        public void ConnectionEstablished()
        {
            if (_openConnectionsEnabled)
            {
                _currentlyIdle = true;
                TagList tags = GetTags();
                tags.Add(GetStateTag(idle: true));
                _metrics.OpenConnections.Add(1, tags);
            }
        }

        public void ConnectionClosed(long durationMs)
        {
            TagList tags = GetTags();

            if (_metrics.ConnectionDuration.Enabled)
            {
                _metrics.ConnectionDuration.Record(durationMs / 1000d, tags);
            }

            if (_openConnectionsEnabled)
            {
                tags.Add(GetStateTag(idle: _currentlyIdle));
                _metrics.OpenConnections.Add(-1, tags);
            }
        }

        public void IdleStateChanged(bool idle)
        {
            if (_openConnectionsEnabled && _currentlyIdle != idle)
            {
                _currentlyIdle = idle;
                TagList tags = GetTags();
                tags.Add(GetStateTag(idle: !idle));
                _metrics.OpenConnections.Add(-1, tags);
                tags[tags.Count - 1] = GetStateTag(idle: idle);
                _metrics.OpenConnections.Add(1, tags);
            }
        }
    }
}
