// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Authentication.ExtendedProtection;
using System.Diagnostics;
using System.Globalization;

namespace System.Net
{
    internal sealed class ServiceNameStore
    {
        private readonly List<string> _serviceNames;
        private ServiceNameCollection? _serviceNameCollection;

        public ServiceNameCollection ServiceNames
        {
            get
            {
                if (_serviceNameCollection == null)
                {
                    _serviceNameCollection = new ServiceNameCollection(_serviceNames);
                }
                return _serviceNameCollection;
            }
        }

        public ServiceNameStore()
        {
            _serviceNames = new List<string>();
            _serviceNameCollection = null; // set only when needed (due to expensive item-by-item copy)
        }

        private static string? NormalizeServiceName(string? inputServiceName)
        {
            if (string.IsNullOrWhiteSpace(inputServiceName))
            {
                return inputServiceName;
            }

            // Separate out the prefix
            int shashIndex = inputServiceName.IndexOf('/');
            if (shashIndex < 0)
            {
                return inputServiceName;
            }
            string prefix = inputServiceName.Substring(0, shashIndex + 1); // Includes slash
            string hostPortAndDistinguisher = inputServiceName.Substring(shashIndex + 1); // Excludes slash

            if (string.IsNullOrWhiteSpace(hostPortAndDistinguisher))
            {
                return inputServiceName;
            }

            string host = hostPortAndDistinguisher;
            string port = string.Empty;
            string distinguisher = string.Empty;

            // Check for the absence of a port or distinguisher.
            UriHostNameType hostType = Uri.CheckHostName(hostPortAndDistinguisher);
            if (hostType == UriHostNameType.Unknown)
            {
                string hostAndPort = hostPortAndDistinguisher;

                // Check for distinguisher
                int nextSlashIndex = hostPortAndDistinguisher.IndexOf('/');
                if (nextSlashIndex >= 0)
                {
                    // host:port/distinguisher or host/distinguisher
                    hostAndPort = hostPortAndDistinguisher.Substring(0, nextSlashIndex); // Excludes Slash
                    distinguisher = hostPortAndDistinguisher.Substring(nextSlashIndex); // Includes Slash
                    host = hostAndPort; // We don't know if there is a port yet.

                    // No need to validate the distinguisher
                }

                // Check for port
                int colonIndex = hostAndPort.LastIndexOf(':'); // Allow IPv6 addresses
                if (colonIndex >= 0)
                {
                    // host:port
                    host = hostAndPort.Substring(0, colonIndex); // Excludes colon
                    port = hostAndPort.Substring(colonIndex + 1); // Excludes colon

                    // Loosely validate the port just to make sure it was a port and not something else
                    if (!ushort.TryParse(port, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    {
                        return inputServiceName;
                    }

                    // Re-include the colon for the final output.  Do not change the port format.
                    port = hostAndPort.Substring(colonIndex);
                }

                hostType = Uri.CheckHostName(host); // Revaidate the host
            }

            if (hostType != UriHostNameType.Dns)
            {
                // UriHostNameType.IPv4, UriHostNameType.IPv6: Do not normalize IPv4/6 hosts.
                // UriHostNameType.Basic: This is never returned by CheckHostName today
                // UriHostNameType.Unknown: Nothing recognizable to normalize
                // default Some new UriHostNameType?
                return inputServiceName;
            }

            // Now we have a valid DNS host, normalize it.

            Uri? constructedUri;
            // This shouldn't fail, but we need to avoid any unexpected exceptions on this code path.
            if (!Uri.TryCreate(Uri.UriSchemeHttp + Uri.SchemeDelimiter + host, UriKind.Absolute, out constructedUri))
            {
                return inputServiceName;
            }

            string normalizedHost = constructedUri.GetComponents(
                UriComponents.NormalizedHost, UriFormat.SafeUnescaped);

            string normalizedServiceName = prefix + normalizedHost + port + distinguisher;

            // Don't return the new one unless we absolutely have to.  It may have only changed casing.
            if (inputServiceName.Equals(normalizedServiceName, StringComparison.OrdinalIgnoreCase))
            {
                return inputServiceName;
            }

            return normalizedServiceName;
        }

        private bool AddSingleServiceName(string spn)
        {
            spn = NormalizeServiceName(spn)!;
            if (Contains(spn))
            {
                return false;
            }
            else
            {
                _serviceNames.Add(spn);
                return true;
            }
        }

        public bool Add(string uriPrefix)
        {
            Debug.Assert(!string.IsNullOrEmpty(uriPrefix));

            string[] newServiceNames = BuildServiceNames(uriPrefix);

            bool addedAny = false;
            foreach (string spn in newServiceNames)
            {
                if (AddSingleServiceName(spn))
                {
                    addedAny = true;

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.Format(SR.net_log_listener_spn_add, spn, uriPrefix));
                }
            }

            if (addedAny)
            {
                _serviceNameCollection = null;
            }
            else if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, SR.Format(SR.net_log_listener_spn_not_add, uriPrefix));
            }

            return addedAny;
        }

        public bool Remove(string uriPrefix)
        {
            Debug.Assert(!string.IsNullOrEmpty(uriPrefix));

            string? newServiceName = BuildSimpleServiceName(uriPrefix);
            newServiceName = NormalizeServiceName(newServiceName);
            bool needToRemove = Contains(newServiceName);

            if (needToRemove)
            {
                _serviceNames.Remove(newServiceName!);
                _serviceNameCollection = null; //invalidate (readonly) ServiceNameCollection
            }

            if (NetEventSource.Log.IsEnabled())
            {
                if (needToRemove)
                {
                    NetEventSource.Info(this, SR.Format(SR.net_log_listener_spn_remove, newServiceName, uriPrefix));
                }
                else
                {
                    NetEventSource.Info(this, SR.Format(SR.net_log_listener_spn_not_remove, uriPrefix));
                }
            }

            return needToRemove;
        }

        // Assumes already normalized
        private bool Contains(string? newServiceName)
        {
            if (newServiceName == null)
            {
                return false;
            }

            foreach (string serviceName in _serviceNames)
            {
                if (serviceName.Equals(newServiceName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            _serviceNames.Clear();
            _serviceNameCollection = null; //invalidate (readonly) ServiceNameCollection
        }

        private static string? ExtractHostname(string uriPrefix, bool allowInvalidUriStrings)
        {
            if (Uri.IsWellFormedUriString(uriPrefix, UriKind.Absolute))
            {
                Uri hostUri = new Uri(uriPrefix);
                return hostUri.Host;
            }
            else if (allowInvalidUriStrings)
            {
                int i = uriPrefix.IndexOf("://", StringComparison.Ordinal) + 3;
                int j = i;

                bool inSquareBrackets = false;
                while (j < uriPrefix.Length && uriPrefix[j] != '/' && (uriPrefix[j] != ':' || inSquareBrackets))
                {
                    if (uriPrefix[j] == '[')
                    {
                        if (inSquareBrackets)
                        {
                            j = i;
                            break;
                        }
                        inSquareBrackets = true;
                    }
                    if (inSquareBrackets && uriPrefix[j] == ']')
                    {
                        inSquareBrackets = false;
                    }
                    j++;
                }

                return uriPrefix.Substring(i, j - i);
            }

            return null;
        }

        public static string? BuildSimpleServiceName(string uriPrefix)
        {
            string? hostname = ExtractHostname(uriPrefix, false);

            if (hostname != null)
            {
                return "HTTP/" + hostname;
            }
            else
            {
                return null;
            }
        }

        public static string[] BuildServiceNames(string uriPrefix)
        {
            string hostname = ExtractHostname(uriPrefix, true)!;

            if (string.Equals(hostname, "*", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(hostname, "+", StringComparison.OrdinalIgnoreCase) ||
                IPAddress.TryParse(hostname, out _))
            {
                // for a wildcard, register the machine name.  If the caller doesn't have DNS permission
                // or the query fails for some reason, don't add an SPN.
                try
                {
                    string machineName = Dns.GetHostEntry(string.Empty).HostName;
                    return new string[] { "HTTP/" + machineName };
                }
                catch (System.Net.Sockets.SocketException)
                {
                    return Array.Empty<string>();
                }
                catch (System.Security.SecurityException)
                {
                    return Array.Empty<string>();
                }
            }
            else if (!hostname.Contains('.'))
            {
                // for a dotless name, try to resolve the FQDN.  If the caller doesn't have DNS permission
                // or the query fails for some reason, add only the dotless name.
                try
                {
                    string fqdn = Dns.GetHostEntry(hostname).HostName;
                    return new string[] { "HTTP/" + hostname, "HTTP/" + fqdn };
                }
                catch (System.Net.Sockets.SocketException)
                {
                    return new string[] { "HTTP/" + hostname };
                }
                catch (System.Security.SecurityException)
                {
                    return new string[] { "HTTP/" + hostname };
                }
            }
            else
            {
                return new string[] { "HTTP/" + hostname };
            }
        }
    }
}
