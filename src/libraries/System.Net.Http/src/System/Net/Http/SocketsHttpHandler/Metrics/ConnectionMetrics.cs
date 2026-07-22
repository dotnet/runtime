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
        private readonly string _protocolVersionTag;
        private readonly string _schemeTag;
        private readonly string _hostTag;
        private readonly int _portTag;
        private readonly string? _peerAddressTag;
        private bool _currentlyIdle;

        public ConnectionMetrics(SocketsHttpHandlerMetrics metrics, string protocolVersion, string scheme, string host, int port, string? peerAddress)
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
            tags.Add("server.port", DiagnosticsHelper.GetBoxedInt32(_portTag));

            if (_peerAddressTag is not null)
            {
                tags.Add("network.peer.address", _peerAddressTag);
            }

            return tags;
        }

        private OpenConnectionsTagKey CreateTagKey(bool idle) =>
            new OpenConnectionsTagKey(_protocolVersionTag, _schemeTag, _hostTag, _portTag, idle, _peerAddressTag);

        public void ConnectionEstablished()
        {
            if (_openConnectionsEnabled)
            {
                _currentlyIdle = true;
                _metrics.OpenConnectionsTracker.Increment(CreateTagKey(idle: true));
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
                _metrics.OpenConnectionsTracker.Decrement(CreateTagKey(idle: _currentlyIdle));
            }
        }

        public void IdleStateChanged(bool idle)
        {
            if (_openConnectionsEnabled && _currentlyIdle != idle)
            {
                _currentlyIdle = idle;
                _metrics.OpenConnectionsTracker.Decrement(CreateTagKey(idle: !idle));
                _metrics.OpenConnectionsTracker.Increment(CreateTagKey(idle: idle));
            }
        }
    }
}
