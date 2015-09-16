// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Net;

namespace System.Security.Authentication.ExtendedProtection
{
    public class ServiceNameCollection : ReadOnlyCollectionBase
    {
        public ServiceNameCollection(ICollection items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            // Normalize and filter for duplicates.
            foreach (string serviceName in items)
            {
                AddIfNew(InnerList, serviceName);
            }
        }

        public ServiceNameCollection Merge(string serviceName)
        {
            ArrayList newServiceNames = new ArrayList();
            newServiceNames.AddRange(this.InnerList);

            AddIfNew(newServiceNames, serviceName);

            ServiceNameCollection newCollection = new ServiceNameCollection(newServiceNames);
            return newCollection;
        }

        public ServiceNameCollection Merge(IEnumerable serviceNames)
        {
            ArrayList newServiceNames = new ArrayList();
            newServiceNames.AddRange(this.InnerList);

            // We have a pretty bad performance here: O(n^2), but since service name lists should 
            // be small (<<50) and Merge() should not be called frequently, this shouldn't be an issue.
            foreach (object item in serviceNames)
            {
                AddIfNew(newServiceNames, item as string);
            }

            ServiceNameCollection newCollection = new ServiceNameCollection(newServiceNames);
            return newCollection;
        }

        // Normalize, check for duplicates, and add if the value is unique.
        private static void AddIfNew(ArrayList newServiceNames, string serviceName)
        {
            if (String.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(SR.security_ServiceNameCollection_EmptyServiceName);
            }

            serviceName = NormalizeServiceName(serviceName);

            if (!Contains(serviceName, newServiceNames))
            {
                newServiceNames.Add(serviceName);
            }
        }

        // Assumes searchServiceName and serviceNames have already been normalized.
        internal static bool Contains(string searchServiceName, ICollection serviceNames)
        {
            Debug.Assert(serviceNames != null);
            Debug.Assert(!String.IsNullOrEmpty(searchServiceName));

            foreach (string serviceName in serviceNames)
            {
                if (Match(serviceName, searchServiceName))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(string searchServiceName)
        {
            string searchName = NormalizeServiceName(searchServiceName);

            return Contains(searchName, InnerList);
        }

        // Normalizes any punycode to unicode in an Service Name (SPN) host.
        // If the algorithm fails at any point then the original input is returned.
        // ServiceName is in one of the following forms:
        // prefix/host
        // prefix/host:port
        // prefix/host/DistinguishedName
        // prefix/host:port/DistinguishedName
        internal static string NormalizeServiceName(string inputServiceName)
        {
            if (string.IsNullOrWhiteSpace(inputServiceName))
            {
                return inputServiceName;
            }

            // Separate out the prefix
            int slashIndex = inputServiceName.IndexOf('/');
            if (slashIndex < 0)
            {
                return inputServiceName;
            }

            string prefix = inputServiceName.Substring(0, slashIndex + 1); // Includes slash
            string hostPortAndDistinguisher = inputServiceName.Substring(slashIndex + 1); // Excludes slash

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

                // Check for distinguisher.
                int nextSlashIndex = hostPortAndDistinguisher.IndexOf('/');
                if (nextSlashIndex >= 0)
                {
                    // host:port/distinguisher or host/distinguisher
                    hostAndPort = hostPortAndDistinguisher.Substring(0, nextSlashIndex); // Excludes Slash
                    distinguisher = hostPortAndDistinguisher.Substring(nextSlashIndex); // Includes Slash
                    host = hostAndPort; // We don't know if there is a port yet.
                    // No need to validate the distinguisher.
                }

                // Check for port.
                int colonIndex = hostAndPort.LastIndexOf(':'); // Allow IPv6 addresses.
                if (colonIndex >= 0)
                {
                    // host:port
                    host = hostAndPort.Substring(0, colonIndex); // Excludes colon 
                    port = hostAndPort.Substring(colonIndex + 1); // Excludes colon 

                    // Loosely validate the port just to make sure it was a port and not something else.
                    UInt16 portValue;
                    if (!UInt16.TryParse(port, NumberStyles.Integer, CultureInfo.InvariantCulture, out portValue))
                    {
                        return inputServiceName;
                    }

                    // Re-include the colon for the final output.  Do not change the port format.
                    port = hostAndPort.Substring(colonIndex);
                }

                hostType = Uri.CheckHostName(host); // Re-validate the host.
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

            Uri constructedUri;

            // We need to avoid any unexpected exceptions on this code path.
            if (!Uri.TryCreate(UriScheme.Http + UriScheme.SchemeDelimiter + host, UriKind.Absolute, out constructedUri))
            {
                return inputServiceName;
            }

            string normalizedHost = constructedUri.GetComponents(
                UriComponents.NormalizedHost, UriFormat.SafeUnescaped);

            string normalizedServiceName = string.Format(CultureInfo.InvariantCulture,
                "{0}{1}{2}{3}", prefix, normalizedHost, port, distinguisher);

            // Don't return the new one unless we absolutely have to.  It may have only changed casing.
            if (Match(inputServiceName, normalizedServiceName))
            {
                return inputServiceName;
            }

            return normalizedServiceName;
        }

        // Assumes already normalized.
        internal static bool Match(string serviceName1, string serviceName2)
        {
            return (String.Compare(serviceName1, serviceName2, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}
