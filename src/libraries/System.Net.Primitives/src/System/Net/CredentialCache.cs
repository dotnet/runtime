// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.Net
{
    // More sophisticated password cache that stores multiple
    // name-password pairs and associates these with host/realm.
    public class CredentialCache : ICredentials, ICredentialsByHost, IEnumerable
    {
        private readonly Dictionary<CredentialKey, NetworkCredential> _cache = new Dictionary<CredentialKey, NetworkCredential>();
        private readonly Dictionary<CredentialHostKey, NetworkCredential> _cacheForHosts = new Dictionary<CredentialHostKey, NetworkCredential>();
        internal int _version;

        private int _numbDefaultCredInCache = 0;

        // [thread token optimization] The resulting counter of default credential resided in the cache.
        internal bool IsDefaultInCache
        {
            get
            {
                return _numbDefaultCredInCache != 0;
            }
        }

        /// <devdoc>
        ///  <para>
        ///    Initializes a new instance of the <see cref='System.Net.CredentialCache'/> class.
        ///  </para>
        /// </devdoc>
        public CredentialCache()
        {
        }

        /// <devdoc>
        ///  <para>
        ///    Adds a <see cref='System.Net.NetworkCredential'/> instance to the credential cache.
        ///  </para>
        /// </devdoc>
        public void Add(Uri uriPrefix, string authenticationType, NetworkCredential credential)
        {
            // Parameter validation
            if (uriPrefix == null)
            {
                throw new ArgumentNullException(nameof(uriPrefix));
            }
            if (authenticationType == null)
            {
                throw new ArgumentNullException(nameof(authenticationType));
            }

            ++_version;

            CredentialKey key = new CredentialKey(uriPrefix, authenticationType);

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialCache::Add() Adding key:[" + key.ToString() + "], cred:[" + credential.Domain + "],[" + credential.UserName + "]");
            }

            _cache.Add(key, credential);
            if (credential is SystemNetworkCredential)
            {
                ++_numbDefaultCredInCache;
            }
        }


        public void Add(string host, int port, string authenticationType, NetworkCredential credential)
        {
            // Parameter validation
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (authenticationType == null)
            {
                throw new ArgumentNullException(nameof(authenticationType));
            }

            if (host.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, "host"));
            }

            if (port < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            ++_version;

            CredentialHostKey key = new CredentialHostKey(host, port, authenticationType);

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialCache::Add() Adding key:[" + key.ToString() + "], cred:[" + credential.Domain + "],[" + credential.UserName + "]");
            }

            _cacheForHosts.Add(key, credential);
            if (credential is SystemNetworkCredential)
            {
                ++_numbDefaultCredInCache;
            }
        }


        /// <devdoc>
        ///  <para>
        ///    Removes a <see cref='System.Net.NetworkCredential'/> instance from the credential cache.
        ///  </para>
        /// </devdoc>
        public void Remove(Uri uriPrefix, string authenticationType)
        {
            if (uriPrefix == null || authenticationType == null)
            {
                // These couldn't possibly have been inserted into
                // the cache because of the test in Add().
                return;
            }

            ++_version;

            CredentialKey key = new CredentialKey(uriPrefix, authenticationType);

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialCache::Remove() Removing key:[" + key.ToString() + "]");
            }

            NetworkCredential value;
            if (_cache.TryGetValue(key, out value))
            {
                if (value is SystemNetworkCredential)
                {
                    --_numbDefaultCredInCache;
                }
                _cache.Remove(key);
            }
        }


        public void Remove(string host, int port, string authenticationType)
        {
            if (host == null || authenticationType == null)
            {
                // These couldn't possibly have been inserted into
                // the cache because of the test in Add().
                return;
            }

            if (port < 0)
            {
                return;
            }

            ++_version;

            CredentialHostKey key = new CredentialHostKey(host, port, authenticationType);

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialCache::Remove() Removing key:[" + key.ToString() + "]");
            }

            NetworkCredential value;
            if (_cacheForHosts.TryGetValue(key, out value))
            {
                if (value is SystemNetworkCredential)
                {
                    --_numbDefaultCredInCache;
                }
                _cacheForHosts.Remove(key);
            }
        }

        /// <devdoc>
        ///  <para>
        ///    Returns the <see cref='System.Net.NetworkCredential'/>
        ///    instance associated with the supplied Uri and
        ///    authentication type.
        ///  </para>
        /// </devdoc>
        public NetworkCredential GetCredential(Uri uriPrefix, string authenticationType)
        {
            if (uriPrefix == null)
            {
                throw new ArgumentNullException(nameof(uriPrefix));
            }
            if (authenticationType == null)
            {
                throw new ArgumentNullException(nameof(authenticationType));
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialCache::GetCredential(uriPrefix=\"" + uriPrefix + "\", authType=\"" + authenticationType + "\")");
            }

            int longestMatchPrefix = -1;
            NetworkCredential mostSpecificMatch = null;

            // Enumerate through every credential in the cache
            foreach (KeyValuePair<CredentialKey, NetworkCredential> pair in _cache)
            {
                CredentialKey key = pair.Key;

                // Determine if this credential is applicable to the current Uri/AuthType
                if (key.Match(uriPrefix, authenticationType))
                {
                    int prefixLen = key.UriPrefixLength;

                    // Check if the match is better than the current-most-specific match
                    if (prefixLen > longestMatchPrefix)
                    {
                        // Yes: update the information about currently preferred match
                        longestMatchPrefix = prefixLen;
                        mostSpecificMatch = pair.Value;
                    }
                }
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialCache::GetCredential returning " + ((mostSpecificMatch == null) ? "null" : "(" + mostSpecificMatch.UserName + ":" + mostSpecificMatch.Domain + ")"));
            }
            return mostSpecificMatch;
        }


        public NetworkCredential GetCredential(string host, int port, string authenticationType)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (authenticationType == null)
            {
                throw new ArgumentNullException(nameof(authenticationType));
            }
            if (host.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, "host"));
            }
            if (port < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialCache::GetCredential(host=\"" + host + ":" + port.ToString() + "\", authenticationType=\"" + authenticationType + "\")");
            }

            NetworkCredential match = null;

            // Enumerate through every credential in the cache
            foreach (KeyValuePair<CredentialHostKey, NetworkCredential> pair in _cacheForHosts)
            {
                CredentialHostKey key = pair.Key;

                // Determine if this credential is applicable to the current Uri/AuthType
                if (key.Match(host, port, authenticationType))
                {
                    match = pair.Value;
                }
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialCache::GetCredential returning " + ((match == null) ? "null" : "(" + match.UserName + ":" + match.Domain + ")"));
            }
            return match;
        }

        public IEnumerator GetEnumerator()
        {
            return new CredentialEnumerator(this, _cache, _cacheForHosts, _version);
        }

        /// <devdoc>
        ///  <para>
        ///    Gets the default system credentials from the <see cref='System.Net.CredentialCache'/>.
        ///  </para>
        /// </devdoc>
        public static ICredentials DefaultCredentials
        {
            get
            {
                return SystemNetworkCredential.s_defaultCredential;
            }
        }

        public static NetworkCredential DefaultNetworkCredentials
        {
            get
            {
                return SystemNetworkCredential.s_defaultCredential;
            }
        }

        private class CredentialEnumerator : IEnumerator
        {
            private CredentialCache _cache;
            private ICredentials[] _array;
            private int _index = -1;
            private int _version;

            internal CredentialEnumerator(CredentialCache cache, Dictionary<CredentialKey, NetworkCredential> table, Dictionary<CredentialHostKey, NetworkCredential> hostTable, int version)
            {
                _cache = cache;
                _array = new ICredentials[table.Count + hostTable.Count];
                ((ICollection)table.Values).CopyTo(_array, 0);
                ((ICollection)hostTable.Values).CopyTo(_array, table.Count);
                _version = version;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_index < 0 || _index >= _array.Length)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
                    if (_version != _cache._version)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    }
                    return _array[_index];
                }
            }

            bool IEnumerator.MoveNext()
            {
                if (_version != _cache._version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }
                if (++_index < _array.Length)
                {
                    return true;
                }
                _index = _array.Length;
                return false;
            }

            void IEnumerator.Reset()
            {
                _index = -1;
            }
        }
    }

    // Abstraction for credentials in password-based
    // authentication schemes (basic, digest, NTLM, Kerberos).
    //
    // Note that this is not applicable to public-key based
    // systems such as SSL client authentication.
    //
    // "Password" here may be the clear text password or it
    // could be a one-way hash that is sufficient to
    // authenticate, as in HTTP/1.1 digest.
    internal class SystemNetworkCredential : NetworkCredential
    {
        internal static readonly SystemNetworkCredential s_defaultCredential = new SystemNetworkCredential();

        // We want reference equality to work. Making this private is a good way to guarantee that.
        private SystemNetworkCredential() :
            base(string.Empty, string.Empty, string.Empty)
        {
        }
    }

    internal class CredentialHostKey : IEquatable<CredentialHostKey>
    {
        public readonly string Host;
        public readonly string AuthenticationType;
        public readonly int Port;

        internal CredentialHostKey(string host, int port, string authenticationType)
        {
            Host = host;
            Port = port;
            AuthenticationType = authenticationType;
        }

        internal bool Match(string host, int port, string authenticationType)
        {
            if (host == null || authenticationType == null)
            {
                return false;
            }

            // If the protocols don't match, this credential is not applicable for the given Uri.
            if (!string.Equals(authenticationType, AuthenticationType, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Host, host, StringComparison.OrdinalIgnoreCase) ||
                port != Port)
            {
                return false;
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialKey::Match(" + Host.ToString() + ":" + Port.ToString() + " & " + host.ToString() + ":" + port.ToString() + ")");
            }
            return true;
        }

        private int _hashCode = 0;
        private bool _computedHashCode = false;
        public override int GetHashCode()
        {
            if (!_computedHashCode)
            {
                // Compute HashCode on demand
                _hashCode = AuthenticationType.ToUpperInvariant().GetHashCode() + Host.ToUpperInvariant().GetHashCode() + Port.GetHashCode();
                _computedHashCode = true;
            }
            return _hashCode;
        }

        public bool Equals(CredentialHostKey other)
        {
            if (other == null)
            {
                return false;
            }

            bool equals =
                string.Equals(AuthenticationType, other.AuthenticationType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase) &&
                Port == other.Port;

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialKey::Equals(" + ToString() + ", " + other.ToString() + ") returns " + equals.ToString());
            }
            return equals;
        }

        public override bool Equals(object comparand)
        {
            return Equals(comparand as CredentialHostKey);
        }

        public override string ToString()
        {
            return "[" + Host.Length.ToString(NumberFormatInfo.InvariantInfo) + "]:" + Host + ":" + Port.ToString(NumberFormatInfo.InvariantInfo) + ":" + LoggingHash.ObjectToString(AuthenticationType);
        }
    }

    internal class CredentialKey : IEquatable<CredentialKey>
    {
        public readonly Uri UriPrefix;
        public readonly int UriPrefixLength = -1;
        public readonly string AuthenticationType;
        private int _hashCode = 0;
        private bool _computedHashCode = false;

        internal CredentialKey(Uri uriPrefix, string authenticationType)
        {
            UriPrefix = uriPrefix;
            UriPrefixLength = UriPrefix.ToString().Length;
            AuthenticationType = authenticationType;
        }

        internal bool Match(Uri uri, string authenticationType)
        {
            if (uri == null || authenticationType == null)
            {
                return false;
            }

            // If the protocols don't match, this credential is not applicable for the given Uri.
            if (!string.Equals(authenticationType, AuthenticationType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialKey::Match(" + UriPrefix.ToString() + " & " + uri.ToString() + ")");
            }

            return IsPrefix(uri, UriPrefix);
        }

        // IsPrefix (Uri)
        //
        // Determines whether <prefixUri> is a prefix of this URI. A prefix
        // match is defined as:
        //
        //     scheme match
        //     + host match
        //     + port match, if any
        //     + <prefix> path is a prefix of <URI> path, if any
        //
        // Returns:
        // True if <prefixUri> is a prefix of this URI
        internal bool IsPrefix(Uri uri, Uri prefixUri)
        {
            if (prefixUri.Scheme != uri.Scheme || prefixUri.Host != uri.Host || prefixUri.Port != uri.Port)
            {
                return false;
            }

            int prefixLen = prefixUri.AbsolutePath.LastIndexOf('/');
            if (prefixLen > uri.AbsolutePath.LastIndexOf('/'))
            {
                return false;
            }

            return String.Compare(uri.AbsolutePath, 0, prefixUri.AbsolutePath, 0, prefixLen, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public override int GetHashCode()
        {
            if (!_computedHashCode)
            {
                // Compute HashCode on demand
                _hashCode = AuthenticationType.ToUpperInvariant().GetHashCode() + UriPrefixLength + UriPrefix.GetHashCode();
                _computedHashCode = true;
            }
            return _hashCode;
        }

        public bool Equals(CredentialKey other)
        {
            if (other == null)
            {
                return false;
            }

            bool equals =
                string.Equals(AuthenticationType, other.AuthenticationType, StringComparison.OrdinalIgnoreCase) &&
                UriPrefix.Equals(other.UriPrefix);

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("CredentialKey::Equals(" + ToString() + ", " + other.ToString() + ") returns " + equals.ToString());
            }
            return equals;
        }

        public override bool Equals(object comparand)
        {
            return Equals(comparand as CredentialKey);
        }

        public override string ToString()
        {
            return "[" + UriPrefixLength.ToString(NumberFormatInfo.InvariantInfo) + "]:" + LoggingHash.ObjectToString(UriPrefix) + ":" + LoggingHash.ObjectToString(AuthenticationType);
        }
    }
}
