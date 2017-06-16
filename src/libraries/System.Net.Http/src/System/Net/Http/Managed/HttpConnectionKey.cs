// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http.Managed
{
    internal struct HttpConnectionKey : IEquatable<HttpConnectionKey>
    {
        public readonly bool UsingSSL;
        public readonly string Host;
        public readonly int Port;

        public HttpConnectionKey(Uri uri)
        {
            if (uri.Scheme == "http")
            {
                UsingSSL = false;
            }
            else if (uri.Scheme == "https")
            {
                UsingSSL = true;
            }
            else
            {
                throw new ArgumentException("Invalid Uri scheme", nameof(uri));
            }

            Host = uri.Host;
            Port = uri.Port;
        }

        public override int GetHashCode()
        {
            return UsingSSL.GetHashCode() ^ Host.GetHashCode() ^ Port.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(HttpConnectionKey))
            {
                return false;
            }

            return Equals((HttpConnectionKey)obj);
        }

        public bool Equals(HttpConnectionKey other)
        {
            return (UsingSSL == other.UsingSSL && Host == other.Host && Port == other.Port);
        }

        public static bool operator ==(HttpConnectionKey key1, HttpConnectionKey key2)
        {
            return key1.Equals(key2);
        }

        public static bool operator !=(HttpConnectionKey key1, HttpConnectionKey key2)
        {
            return !key1.Equals(key2);
        }
    }
}
