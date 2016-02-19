// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Security;
using System.Runtime.Versioning;

namespace System.Xml
{
    /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver"]/*' />
    /// <devdoc>
    ///    <para>Resolves external XML resources named by a Uniform
    ///       Resource Identifier (URI). This class is <see langword='abstract'/>
    ///       .</para>
    /// </devdoc>
    internal abstract partial class XmlResolver
    {
        /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver.GetEntity1"]/*' />
        /// <devdoc>
        ///    <para>Maps a
        ///       URI to an Object containing the actual resource.</para>
        /// </devdoc>

        public abstract Object GetEntity(Uri absoluteUri,
                                         string role,
                                         Type ofObjectToReturn);



        /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver.ResolveUri"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            if (baseUri == null || (!baseUri.IsAbsoluteUri && baseUri.OriginalString.Length == 0))
            {
                return new Uri(relativeUri, UriKind.RelativeOrAbsolute);
            }
            else
            {
                if (relativeUri == null || relativeUri.Length == 0)
                {
                    return baseUri;
                }
                // relative base Uri
                if (!baseUri.IsAbsoluteUri)
                {
                    // create temporary base for the relative URIs
                    Uri tmpBaseUri = new Uri("tmp:///");

                    // create absolute base URI with the temporary base
                    Uri absBaseUri = new Uri(tmpBaseUri, baseUri.OriginalString);

                    // resolve the relative Uri into a new absolute URI
                    Uri resolvedAbsUri = new Uri(absBaseUri, relativeUri);

                    // make it relative by removing temporary base
                    Uri resolvedRelUri = tmpBaseUri.MakeRelativeUri(resolvedAbsUri);

                    return resolvedRelUri;
                }
                return new Uri(baseUri, relativeUri);
            }
        }


        public virtual bool SupportsType(Uri absoluteUri, Type type)
        {
            if (absoluteUri == null)
            {
                throw new ArgumentNullException(nameof(absoluteUri));
            }
            if (type == null || type == typeof(System.IO.Stream))
            {
                return true;
            }
            return false;
        }
    }
}
