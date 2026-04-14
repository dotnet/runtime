// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;

namespace System.Security.Cryptography.Cose
{
    internal static class CborReaderExtensions
    {
        internal static int ReadInt32ForCrypto(this CborReader reader)
        {
            try
            {
                return reader.ReadInt32();
            }
            catch (OverflowException ex)
            {
                throw new CryptographicException(SR.DecodeErrorWhileDecodingSeeInnerEx, ex);
            }
        }
    }
}
