// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System.Net.Http
{
    internal sealed class CachedCertificateValue(byte[] rawCertificateData, long lastUsedTime)
    {
        public byte[] RawCertificateData { get; } = rawCertificateData;
        public long LastUsedTime { get; set; } = lastUsedTime;
    }

    internal readonly struct CachedCertificateKey : IEquatable<CachedCertificateKey>
    {
        public CachedCertificateKey(IPAddress address, HttpRequestMessage message)
        {
            Debug.Assert(message.RequestUri != null);
            Address = address;
            Host = message.Headers.Host ?? message.RequestUri.Host;
        }
        public IPAddress Address { get; }
        public string Host { get; }

        public bool Equals(CachedCertificateKey other) =>
            Address.Equals(other.Address) &&
            Host == other.Host;

        public override bool Equals(object? obj)
        {
            throw new Exception("Unreachable");
        }

        public override int GetHashCode() => HashCode.Combine(Address, Host);
    }
}
