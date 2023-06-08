// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Security;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace System.Xml
{
    /// <devdoc>
    ///    <para>Resolves external XML resources named by a Uniform
    ///       Resource Identifier (URI). This class is <see langword='abstract'/>
    ///       .</para>
    /// </devdoc>
    public abstract partial class XmlResolver
    {
        /// <devdoc>
        ///    <para>Maps a
        ///       URI to an Object containing the actual resource.</para>
        /// </devdoc>

        public abstract object? GetEntity(Uri absoluteUri,
                                          string? role,
                                          Type? ofObjectToReturn);

        public virtual Task<object> GetEntityAsync(Uri absoluteUri,
                                                   string? role,
                                                   Type? ofObjectToReturn)
        {
            throw new NotImplementedException();
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual Uri ResolveUri(Uri? baseUri, string? relativeUri)
        {
            if (baseUri == null || (!baseUri.IsAbsoluteUri && baseUri.OriginalString.Length == 0))
            {
                Uri uri = new Uri(relativeUri!, UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri && uri.OriginalString.Length > 0)
                {
                    uri = new Uri(Path.GetFullPath(relativeUri!));
                }

                return uri;
            }
            else
            {
                if (string.IsNullOrEmpty(relativeUri))
                {
                    return baseUri;
                }

                // relative base Uri
                if (!baseUri.IsAbsoluteUri)
                {
                    throw new NotSupportedException(SR.Xml_RelativeUriNotSupported);
                }

                return new Uri(baseUri, relativeUri);
            }
        }

        //UE attension
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual ICredentials Credentials
        {
            set { }
        }

        public virtual bool SupportsType(Uri absoluteUri, Type? type)
        {
            ArgumentNullException.ThrowIfNull(absoluteUri);

            if (type == null || type == typeof(Stream))
            {
                return true;
            }

            return false;
        }
    }
}
