// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Xml
{
    /// <summary>
    /// Resolves external XML resources named by a Uniform Resource Identifier (URI).
    /// </summary>
    internal partial class XmlSystemPathResolver : XmlResolver
    {
        public XmlSystemPathResolver()
        {
        }

        // Maps a URI to an Object containing the actual resource.
        public override Object GetEntity(Uri uri, string role, Type typeOfObjectToReturn)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (typeOfObjectToReturn != null && typeOfObjectToReturn != typeof(Stream) && typeOfObjectToReturn != typeof(Object))
            {
                throw new XmlException(SR.Xml_UnsupportedClass, string.Empty);
            }

            string filePath = uri.OriginalString;
            if (uri.IsAbsoluteUri)
            {
                if (!uri.IsFile)
                    throw new XmlException(SR.Format(SR.Xml_SystemPathResolverCannotOpenUri, uri.ToString()));

                filePath = uri.LocalPath;
            }

            try
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (ArgumentException e)
            {
                throw new XmlException(SR.Format(SR.Xml_SystemPathResolverCannotOpenUri, uri.ToString()), e);
            }
        }

        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            return base.ResolveUri(baseUri, relativeUri);
        }
    }
}
