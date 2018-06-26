// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace System.Xml
{
    public partial class XmlUrlResolver : XmlResolver
    {
        // Maps a URI to an Object containing the actual resource.
        public override async Task<object> GetEntityAsync(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            if (ofObjectToReturn == null || ofObjectToReturn == typeof(System.IO.Stream) || ofObjectToReturn == typeof(object))
            {
                return await DownloadManager.GetStreamAsync(absoluteUri, _credentials, _proxy, _cachePolicy).ConfigureAwait(false);
            }
            else
            {
                throw new XmlException(SR.Xml_UnsupportedClass, string.Empty);
            }
        }
    }
}
