// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal ServicePoint(Uri address)
        {
            Debug.Assert(address != null);
            Address = address;
            ConnectionName = address.Scheme;
        }

        public BindIPEndPoint BindIPEndPointDelegate { get; set; }

        public int ConnectionLeaseTimeout
        {
            get { return _connectionLeaseTimeout; }
            set
            {
                if (value < Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _connectionLeaseTimeout = value;
            }
        }

        public Uri Address { get; }

        public int MaxIdleTime
        {
            get { return _maxIdleTime; }
            set
            {
                if (value < Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _maxIdleTime = value;
            }
        }

        public bool UseNagleAlgorithm { get; set; }

        public int ReceiveBufferSize
        {
            get { return _receiveBufferSize; }
            set
            {
                if (value < -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
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
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _connectionLimit = value;
            }
        }

        public int CurrentConnections => 0;

        public X509Certificate Certificate { get; internal set; }

        public X509Certificate ClientCertificate { get; internal set; }

        public bool SupportsPipelining { get; internal set; } = true;

        public void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval)
        {
            if (enabled)
            {
                if (keepAliveTime <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(keepAliveTime));
                }
                if (keepAliveInterval <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(keepAliveInterval));
                }
            }
        }
    }
}
