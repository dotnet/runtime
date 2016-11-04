// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System;
    using System.IO;
    using System.Text;
    using System.Security;
    using System.Net;
    using System.Threading.Tasks;
    using System.Runtime.Versioning;

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

        public abstract Object GetEntity(Uri absoluteUri,
                                         string role,
                                         Type ofObjectToReturn);



        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            if (baseUri == null || (!baseUri.IsAbsoluteUri && baseUri.OriginalString.Length == 0))
            {
                Uri uri = new Uri(relativeUri, UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri && uri.OriginalString.Length > 0)
                {
                    uri = new Uri(Uri.UriSchemeFile + Uri.SchemeDelimiter + Path.GetFullPath(relativeUri));
                }
                return uri;
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
					// TODO: should this be SILVERLIGHT or desktop behavior?
#if SILVERLIGHT
                    // create temporary base for the relative URIs
                    Uri tmpBaseUri = new Uri("tmp:///");

                    // create absolute base URI with the temporary base
                    Uri absBaseUri = new Uri(tmpBaseUri, baseUri.OriginalString);

                    // resolve the relative Uri into a new absolute URI
                    Uri resolvedAbsUri = new Uri(absBaseUri, relativeUri);

                    // make it relative by removing temporary base
                    Uri resolvedRelUri = tmpBaseUri.MakeRelativeUri(resolvedAbsUri);

                    return resolvedRelUri;
#else
                    throw new NotSupportedException(SR.Xml_RelativeUriNotSupported);
#endif
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

        public virtual bool SupportsType(Uri absoluteUri, Type type)
        {
            if (absoluteUri == null)
            {
                throw new ArgumentNullException(nameof(absoluteUri));
            }
            if (type == null || type == typeof(Stream))
            {
                return true;
            }
            return false;
        }
    }
}
