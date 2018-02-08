// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    internal readonly struct HttpConnectionKey : IEquatable<HttpConnectionKey>
    {
        public readonly string Host;
        public readonly int Port;
        public readonly string SslHostName;     // null if not SSL

        public HttpConnectionKey(string host, int port, string sslHostName)
        {
            Host = host;
            Port = port;
            SslHostName = sslHostName;
        }

        // The common cases here are SslHostName == null or SslHostName == Host
        public override int GetHashCode() =>
            SslHostName == null ? Host.GetHashCode() ^ Port.GetHashCode() :
            SslHostName == Host ? Host.GetHashCode() ^ Port.GetHashCode() ^ 0x1 :
            Host.GetHashCode() ^ Port.GetHashCode() ^ SslHostName.GetHashCode();

        public override bool Equals(object obj) =>
            obj != null &&
            obj is HttpConnectionKey &&
            Equals((HttpConnectionKey)obj);

        public bool Equals(HttpConnectionKey other) =>
            Host == other.Host &&
            Port == other.Port &&
            SslHostName == other.SslHostName;

        public static bool operator ==(HttpConnectionKey key1, HttpConnectionKey key2) => key1.Equals(key2);
        public static bool operator !=(HttpConnectionKey key1, HttpConnectionKey key2) => !key1.Equals(key2);

        public bool IsSecure => SslHostName != null;

        public override string ToString() =>
            SslHostName == null ?
            $"{Host}:{Port}" :
            $"{Host}:{Port} SslHostName: {SslHostName}";
    }
}
