// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    internal struct HttpConnectionKey : IEquatable<HttpConnectionKey>
    {
        public readonly bool UsingSSL;
        public readonly string Host;
        public readonly int Port;

        public HttpConnectionKey(Uri uri)
        {
            UsingSSL = 
                uri.Scheme == "http" ? false :
                uri.Scheme == "https" ? true :
                throw new ArgumentException("Invalid Uri scheme", nameof(uri));

            Host = uri.Host;
            Port = uri.Port;
        }

        public override int GetHashCode() =>
            UsingSSL.GetHashCode() ^ Host.GetHashCode() ^ Port.GetHashCode();

        public override bool Equals(object obj) =>
            obj != null &&
            obj is HttpConnectionKey &&
            Equals((HttpConnectionKey)obj);

        public bool Equals(HttpConnectionKey other) =>
            UsingSSL == other.UsingSSL &&
            Host == other.Host &&
            Port == other.Port;

        public static bool operator ==(HttpConnectionKey key1, HttpConnectionKey key2) => key1.Equals(key2);
        public static bool operator !=(HttpConnectionKey key1, HttpConnectionKey key2) => !key1.Equals(key2);
    }
}
