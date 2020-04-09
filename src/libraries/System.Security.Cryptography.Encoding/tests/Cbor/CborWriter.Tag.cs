// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter
    {
        public void WriteTag(CborTag tag)
        {
            WriteUnsignedInteger(CborMajorType.Tag, (ulong)tag);
            _isTagContext = true;
        }

        // Additional tagged type support

        internal const string Rfc3339FormatString = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";

        public void WriteDateTimeOffset(DateTimeOffset value)
        {
            string dateString =
                value.Offset == TimeSpan.Zero ?
                value.UtcDateTime.ToString(Rfc3339FormatString) : // prefer 'Z' over '+00:00'
                value.ToString(Rfc3339FormatString);

            WriteTag(CborTag.DateTimeString);
            WriteTextString(dateString);
        }

        public void WriteUnixTimeSeconds(long value)
        {
            WriteTag(CborTag.DateTimeUnixSeconds);
            WriteInt64(value);
        }

        public void WriteBigInteger(BigInteger value)
        {
            bool isUnsigned = value.Sign >= 0;
            BigInteger unsignedValue = isUnsigned ? value : -1 - value;
            byte[] unsignedBigEndianEncoding = unsignedValue.ToByteArray(isUnsigned: true, isBigEndian: true);

            WriteTag(isUnsigned ? CborTag.UnsignedBigNum : CborTag.NegativeBigNum);
            WriteByteString(unsignedBigEndianEncoding);
        }
    }
}
