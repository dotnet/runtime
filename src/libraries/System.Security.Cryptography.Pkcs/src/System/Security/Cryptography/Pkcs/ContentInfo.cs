// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            : this(Oids.Pkcs7DataOid.CopyOid(), content)
        {
        }

        public ContentInfo(Oid contentType, byte[] content)
        {
            if (contentType is null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            ContentType = contentType;
            Content = content;
        }

        public Oid ContentType { get; }

        public byte[] Content { get; }

        public static Oid GetContentType(byte[] encodedMessage)
        {
            if (encodedMessage is null)
            {
                throw new ArgumentNullException(nameof(encodedMessage));
            }

            return PkcsPal.Instance.GetEncodedMessageType(encodedMessage);
        }

        public static Oid GetContentType(ReadOnlySpan<byte> encodedMessage)
        {
            return PkcsPal.Instance.GetEncodedMessageType(encodedMessage);
        }
    }
}
