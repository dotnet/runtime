// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;

namespace System.Xml
{
    public abstract partial class XmlResolver
    {
        /// <summary>
        /// Gets an XML resolver which resolves only file system URIs.
        /// </summary>
        /// <value>An XML resolver which resolves only file system URIs.</value>
        /// <remarks>
        /// Calling <see cref="GetEntity"/> or <see cref="GetEntityAsync"/> on the
        /// <see cref="XmlResolver"/> instance returned by this property will resolve only URIs which scheme is file.
        /// </remarks>
        public static XmlResolver FileSystemResolver => XmlFileSystemResolver.s_singleton;

        // An XML resolver that resolves only file system URIs.
        private sealed class XmlFileSystemResolver : XmlResolver
        {
            internal static readonly XmlFileSystemResolver s_singleton = new();

            // Private constructor ensures existing only one instance of XmlFileSystemResolver
            private XmlFileSystemResolver() { }

            public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                if ((ofObjectToReturn is null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
                    && absoluteUri.Scheme == "file")
                {
                    return new FileStream(absoluteUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
                }

                throw new XmlException(SR.Xml_UnsupportedClass, string.Empty);
            }

            public override Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                if (ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
                {
                    if (absoluteUri.Scheme == "file")
                    {
                        return Task.FromResult<object>(new FileStream(absoluteUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1, useAsync: true));
                    }
                }

                throw new XmlException(SR.Xml_UnsupportedClass, string.Empty);
            }
        }
    }
}
