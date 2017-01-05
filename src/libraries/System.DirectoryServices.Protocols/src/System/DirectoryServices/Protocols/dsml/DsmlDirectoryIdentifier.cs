// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Globalization;

    public class DsmlDirectoryIdentifier : DirectoryIdentifier
    {
        // private members
        private Uri _uri = null;

        public DsmlDirectoryIdentifier(Uri serverUri)
        {
            if (serverUri == null)
            {
                throw new ArgumentNullException("serverUri");
            }

            //   Is it a http or https Uri?
            if ((String.Compare(serverUri.Scheme, "http", StringComparison.OrdinalIgnoreCase) != 0) &&
               (String.Compare(serverUri.Scheme, "https", StringComparison.OrdinalIgnoreCase) != 0))
            {
                throw new ArgumentException(Res.GetString(Res.DsmlNonHttpUri));
            }

            _uri = serverUri;
        }

        public Uri ServerUri
        {
            get
            {
                return _uri;
            }
        }
    }
}
