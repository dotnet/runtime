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
            ArgumentNullException.ThrowIfNull(contentType);
            ArgumentNullException.ThrowIfNull(content);

            ContentType = contentType;
            Content = content;
        }

        public Oid ContentType { get; }

        public byte[] Content { get; }

        public static Oid GetContentType(byte[] encodedMessage)
        {
            ArgumentNullException.ThrowIfNull(encodedMessage);

            return PkcsPal.Instance.GetEncodedMessageType(encodedMessage);
        }

#if NET || NETSTANDARD2_1
        public static Oid GetContentType(ReadOnlySpan<byte> encodedMessage)
        {
            return PkcsPal.Instance.GetEncodedMessageType(encodedMessage);
        }
#endif
    }
}
