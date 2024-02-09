// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net
{
    public class ServicePoint
    {
        private int _connectionLeaseTimeout = -1;
        private int _maxIdleTime = 100 * 1000;
        private int _receiveBufferSize = -1;
        private int _connectionLimit;

        internal TcpKeepAlive? KeepAlive { get; set; }

        internal int CurrentAddressIndex { get; set; }

        internal DateTime LastDnsResolve { get; set; }

        internal bool NeedDnsResolve => LastDnsResolve
            .CompareTo(DateTime.Now.AddMilliseconds(-ServicePointManager.DnsRefreshTimeout)) >= 0;

        internal IPAddress[]? CachedAddresses { get; set; }

        internal ServicePoint(Uri address)
        {
            Debug.Assert(address != null);
            Address = address;
            ConnectionName = address.Scheme;
        }

        public BindIPEndPoint? BindIPEndPointDelegate { get; set; }

        public int ConnectionLeaseTimeout
        {
            get { return _connectionLeaseTimeout; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, Timeout.Infinite);
                _connectionLeaseTimeout = value;
            }
        }

        public Uri Address { get; }

        public int MaxIdleTime
        {
            get { return _maxIdleTime; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, Timeout.Infinite);
                _maxIdleTime = value;
            }
        }

        public bool UseNagleAlgorithm { get; set; }

        public int ReceiveBufferSize
        {
            get { return _receiveBufferSize; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, -1);
                _receiveBufferSize = value;
            }
        }

        public bool Expect100Continue { get; set; }

        public DateTime IdleSince { get; internal set; }

        public virtual Version ProtocolVersion { get; internal set; } = new Version(1, 1);

        public string ConnectionName { get; }

        public bool CloseConnectionGroup(string connectionGroupName) => true;

        public int ConnectionLimit
        {
            get { return _connectionLimit; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
                _connectionLimit = value;
            }
        }

        public int CurrentConnections => 0;

        public X509Certificate? Certificate { get; internal set; }

        public X509Certificate? ClientCertificate { get; internal set; }

        public bool SupportsPipelining { get; internal set; } = true;

        public void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval)
        {
            if (!enabled)
            {
                KeepAlive = null;
                return;
            }

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keepAliveTime);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keepAliveInterval);

            KeepAlive = new TcpKeepAlive
            {
                Time = keepAliveTime,
                Interval = keepAliveInterval
            };
        }
    }
}
