// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net.Connections;

namespace System.Net.Http
{
    // Passed to a connection factory, merges allocations for the DnsEndPoint and connection properties.
    internal sealed class DnsEndPointWithProperties : DnsEndPoint, IConnectionProperties
    {
        public HttpRequestMessage InitialRequest { get; }

        public DnsEndPointWithProperties(string host, int port, HttpRequestMessage initialRequest) : base(host, port)
        {
            InitialRequest = initialRequest;
        }

        bool IConnectionProperties.TryGet(Type propertyKey, [NotNullWhen(true)] out object? property)
        {
            if (propertyKey == typeof(DnsEndPointWithProperties))
            {
                property = this;
                return true;
            }

            property = null;
            return false;
        }
    }
}
