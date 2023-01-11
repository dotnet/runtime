// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;

namespace System.Security.Authentication.ExtendedProtection
{
    public class ServiceNameCollection : ReadOnlyCollectionBase
    {
        public ServiceNameCollection(ICollection items)
        {
            ArgumentNullException.ThrowIfNull(items);

            // Normalize and filter for duplicates.
            AddIfNew(items, expectStrings: true);
        }

        /// <summary>
        /// Merges <paramref name="list"/> and <paramref name="serviceName"/> into a new collection.
        /// </summary>
        private ServiceNameCollection(IList list, string serviceName)
            : this(list, additionalCapacity: 1)
        {
            AddIfNew(serviceName);
        }

        /// <summary>
        /// Merges <paramref name="list"/> and <paramref name="serviceNames"/> into a new collection.
        /// </summary>
        private ServiceNameCollection(IList list, IEnumerable serviceNames)
            : this(list, additionalCapacity: GetCountOrOne(serviceNames))
        {
            // We have a pretty bad performance here: O(n^2), but since service name lists should
            // be small (<<50) and Merge() should not be called frequently, this shouldn't be an issue.
            AddIfNew(serviceNames, expectStrings: false);
        }

        private ServiceNameCollection(IList list, int additionalCapacity)
        {
            Debug.Assert(list != null);
            Debug.Assert(additionalCapacity >= 0);

            foreach (string? item in list)
            {
                InnerList.Add(item);
            }
        }

        public bool Contains(string? searchServiceName)
        {
            string? searchName = NormalizeServiceName(searchServiceName);

            foreach (string serviceName in InnerList)
            {
                if (string.Equals(serviceName, searchName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public ServiceNameCollection Merge(string serviceName) => new ServiceNameCollection(InnerList, serviceName);

        public ServiceNameCollection Merge(IEnumerable serviceNames) => new ServiceNameCollection(InnerList, serviceNames);

        /// <summary>
        /// Normalize, check for duplicates, and add each unique value.
        /// </summary>
        private void AddIfNew(IEnumerable serviceNames, bool expectStrings)
        {
            List<string>? list = serviceNames as List<string>;
            if (list != null)
            {
                AddIfNew(list);
                return;
            }

            ServiceNameCollection? snc = serviceNames as ServiceNameCollection;
            if (snc != null)
            {
                AddIfNew(snc.InnerList);
                return;
            }

            // NullReferenceException is thrown when serviceNames is null,
            // which is consistent with the behavior of the .NET Framework.
            foreach (object item in serviceNames)
            {
                // To match the behavior of the .NET Framework, when an item
                // in the collection is not a string:
                //  - Throw InvalidCastException when expectStrings is true.
                //  - Throw ArgumentException when expectStrings is false.
                AddIfNew(expectStrings ? (string)item : (item as string)!);
            }
        }

        /// <summary>
        /// Normalize, check for duplicates, and add each unique value.
        /// </summary>
        private void AddIfNew(List<string> serviceNames)
        {
            Debug.Assert(serviceNames != null);

            foreach (string serviceName in serviceNames)
            {
                AddIfNew(serviceName);
            }
        }

        /// <summary>
        /// Normalize, check for duplicates, and add each unique value.
        /// </summary>
        private void AddIfNew(IList serviceNames)
        {
            Debug.Assert(serviceNames != null);

            foreach (string serviceName in serviceNames)
            {
                AddIfNew(serviceName);
            }
        }

        /// <summary>
        /// Normalize, check for duplicates, and add if the value is unique.
        /// </summary>
        private void AddIfNew(string serviceName)
        {
            ArgumentException.ThrowIfNullOrEmpty(serviceName);

            serviceName = NormalizeServiceName(serviceName);

            if (!Contains(serviceName))
            {
                InnerList.Add(serviceName);
            }
        }

        /// <summary>
        /// Gets the collection Count, if available, otherwise 1.
        /// </summary>
        private static int GetCountOrOne(IEnumerable collection)
        {
            ICollection<string>? c = collection as ICollection<string>;
            return c != null ? c.Count : 1;
        }

        // Normalizes any punycode to Unicode in an Service Name (SPN) host.
        // If the algorithm fails at any point then the original input is returned.
        // ServiceName is in one of the following forms:
        // prefix/host
        // prefix/host:port
        // prefix/host/DistinguishedName
        // prefix/host:port/DistinguishedName
        [return: NotNullIfNotNull(nameof(inputServiceName))]
        private static string? NormalizeServiceName(string? inputServiceName)
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

            ReadOnlySpan<char> prefix = inputServiceName.AsSpan(0, slashIndex + 1); // Includes slash
            string hostPortAndDistinguisher = inputServiceName.Substring(slashIndex + 1); // Excludes slash

            if (hostPortAndDistinguisher.Length == 0)
            {
                return inputServiceName;
            }

            ReadOnlySpan<char> host = hostPortAndDistinguisher;
            ReadOnlySpan<char> port = default;
            ReadOnlySpan<char> distinguisher = default;

            // Check for the absence of a port or distinguisher.
            UriHostNameType hostType = Uri.CheckHostName(hostPortAndDistinguisher);
            if (hostType == UriHostNameType.Unknown)
            {
                ReadOnlySpan<char> hostAndPort = hostPortAndDistinguisher;

                // Check for distinguisher.
                int nextSlashIndex = hostPortAndDistinguisher.IndexOf('/');
                if (nextSlashIndex >= 0)
                {
                    // host:port/distinguisher or host/distinguisher
                    hostAndPort = hostPortAndDistinguisher.AsSpan(0, nextSlashIndex); // Excludes Slash
                    distinguisher = hostPortAndDistinguisher.AsSpan(nextSlashIndex); // Includes Slash
                    host = hostAndPort; // We don't know if there is a port yet.
                    // No need to validate the distinguisher.
                }

                // Check for port.
                int colonIndex = hostAndPort.LastIndexOf(':'); // Allow IPv6 addresses.
                if (colonIndex >= 0)
                {
                    // host:port
                    host = hostAndPort.Slice(0, colonIndex); // Excludes colon
                    port = hostAndPort.Slice(colonIndex + 1); // Excludes colon

                    // Loosely validate the port just to make sure it was a port and not something else.
                    if (!ushort.TryParse(port, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    {
                        return inputServiceName;
                    }

                    // Re-include the colon for the final output.  Do not change the port format.
                    port = hostAndPort.Slice(colonIndex);
                }

                // Re-validate the host.
                hostType = Uri.CheckHostName(
                    host.Length == hostPortAndDistinguisher.Length ?
                        hostPortAndDistinguisher :
                        host.ToString());
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

            // We need to avoid any unexpected exceptions on this code path.
            const string HttpSchemeAndDelimiter = UriScheme.Http + UriScheme.SchemeDelimiter;
            if (!Uri.TryCreate(string.Concat(HttpSchemeAndDelimiter, host), UriKind.Absolute, out constructedUri))
            {
                return inputServiceName;
            }

            string normalizedHost = constructedUri.GetComponents(
                UriComponents.NormalizedHost, UriFormat.SafeUnescaped);

            string normalizedServiceName = string.Concat(prefix, normalizedHost, port, distinguisher);

            // Don't return the new one unless we absolutely have to.  It may have only changed casing.
            if (string.Equals(inputServiceName, normalizedServiceName, StringComparison.OrdinalIgnoreCase))
            {
                return inputServiceName;
            }

            return normalizedServiceName;
        }
    }
}
