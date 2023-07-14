// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Http.Metrics
{
    internal sealed class ConnectionMetricsInfo
    {
        private readonly SocketsHttpHandlerMetrics _metrics;
        private readonly bool _currentConnectionsEnabled;
        private readonly bool _idleConnectionsEnabled;
        private readonly object _protocolTag;
        private readonly object _schemeTag;
        private readonly object _hostTag;
        private readonly object? _portTag;
        private bool _currentlyIdle;

        public ConnectionMetricsInfo(SocketsHttpHandlerMetrics metrics, string protocol, string scheme, string host, int? port)
        {
            _metrics = metrics;
            _currentConnectionsEnabled = _metrics.CurrentConnections.Enabled;
            _idleConnectionsEnabled = _metrics.IdleConnections.Enabled;
            _protocolTag = protocol;
            _schemeTag = scheme;
            _hostTag = host;
            _portTag = port;
        }

        private TagList GetTags()
        {
            TagList tags = default;

            tags.Add("protocol", _protocolTag);
            tags.Add("scheme", _schemeTag);
            tags.Add("host", _hostTag);

            if (_portTag is not null)
            {
                tags.Add("port", _portTag);
            }

            return tags;
        }

        public static ConnectionMetricsInfo? TryCreate(SocketsHttpHandlerMetrics metrics, string protocol, string scheme, string host, int? port)
        {
            if (metrics.CurrentConnections.Enabled ||
                metrics.IdleConnections.Enabled ||
                metrics.ConnectionDuration.Enabled)
            {
                return new ConnectionMetricsInfo(metrics, protocol, scheme, host, port);
            }

            return null;
        }

        public void ConnectionEstablished()
        {
            if (_currentConnectionsEnabled)
            {
                _metrics.CurrentConnections.Add(1, GetTags());
            }
        }

        public void ConnectionClosed(long durationMs)
        {
            MarkAsNotIdle();

            if (_currentConnectionsEnabled)
            {
                _metrics.CurrentConnections.Add(-1, GetTags());
            }

            if (_metrics.ConnectionDuration.Enabled)
            {
                _metrics.ConnectionDuration.Record(durationMs / 1000d, GetTags());
            }
        }

        public void MarkAsIdle()
        {
            if (_idleConnectionsEnabled && !_currentlyIdle)
            {
                _currentlyIdle = true;
                _metrics.IdleConnections.Add(1, GetTags());
            }
        }

        public void MarkAsNotIdle()
        {
            if (_idleConnectionsEnabled && _currentlyIdle)
            {
                _currentlyIdle = false;
                _metrics.IdleConnections.Add(-1, GetTags());
            }
        }
    }
}
