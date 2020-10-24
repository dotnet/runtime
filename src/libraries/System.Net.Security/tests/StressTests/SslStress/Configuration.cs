// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;

namespace SslStress
{
    [Flags]
    public enum RunMode { server = 1, client = 2, both = server | client };

    public class Configuration
    {
        public IPEndPoint ServerEndpoint { get; set; } = new IPEndPoint(IPAddress.Loopback, 0);
        public RunMode RunMode { get; set; }
        public int RandomSeed { get; set; }
        public double CancellationProbability { get; set; }
        public int MaxConnections { get; set; }
        public int MaxBufferLength { get; set; }
        public TimeSpan? MaxExecutionTime { get; set; }
        public TimeSpan DisplayInterval { get; set; }
        public TimeSpan MinConnectionLifetime { get; set; }
        public TimeSpan MaxConnectionLifetime { get; set; }
        public bool LogServer { get; set; }
    }
}
