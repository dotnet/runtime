// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class ContentInfo
    {
        //
        // Constructors
        //

        public ContentInfo(byte[] content)
            : this(Oid.FromOidValue(Oids.Pkcs7Data, OidGroup.ExtensionOrAttribute), content)
        {
        }

        public ContentInfo(Oid contentType, byte[] content)
        {
            if (contentType == null)
                throw new ArgumentNullException(nameof(contentType));
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            ContentType = contentType;
            Content = content;
        }

        public Oid ContentType { get; }

        public byte[] Content { get; }

        public static Oid GetContentType(byte[] encodedMessage)
        {
            if (encodedMessage == null)
                throw new ArgumentNullException(nameof(encodedMessage));
            return PkcsPal.Instance.GetEncodedMessageType(encodedMessage);
        }
    }
}


