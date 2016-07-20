// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Resolves external XML resources named by a Uniform Resource Identifier (URI).
    /// </summary>
    internal class XmlSystemPathResolver : XmlResolver
    {
        public XmlSystemPathResolver()
        {
        }

        // Maps a URI to an Object containing the actual resource.
        public override Object GetEntity(Uri uri, string role, Type typeOfObjectToReturn)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (typeOfObjectToReturn != null && typeOfObjectToReturn != typeof(Stream) && typeOfObjectToReturn != typeof(Object))
            {
                throw new XmlException(SR.Xml_UnsupportedClass, string.Empty);
            }

            string filePath = uri.OriginalString;
            if (uri.IsAbsoluteUri)
            {
                if (!uri.IsFile)
                    throw new XmlException(SR.Xml_SystemPathResolverCannotOpenUri, uri.ToString());

                filePath = uri.LocalPath;
            }

            try
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (ArgumentException e)
            {
                throw new XmlException(SR.Xml_SystemPathResolverCannotOpenUri, uri.ToString(), e, null);
            }
        }

        public override Task<Object> GetEntityAsync(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            return Task.FromResult<object>(GetEntity(absoluteUri, role, ofObjectToReturn));
        }
    }
}
