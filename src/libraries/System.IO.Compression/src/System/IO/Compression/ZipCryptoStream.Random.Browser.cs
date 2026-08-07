// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal sealed partial class ZipCryptoStream
    {
        // Browser deliberately does not reference System.Security.Cryptography: WinZip AES is
        // unsupported there (WinZipAes.PlatformNotSupported.cs), so taking the dependency just to
        // fill 10 bytes of ZipCrypto header salt would pull the whole cryptography assembly into
        // every browser app that touches a zip archive.
        //
        // Instead the salt comes straight from the System.Native random interop, which routes to
        // minipal_get_cryptographically_secure_random_bytes and from there to SystemJS_RandomBytes
        // (crypto.getRandomValues). That is the very same generator that backs
        // RandomNumberGenerator on browser (see RandomNumberGeneratorImplementation.Browser.cs),
        // so the salt is exactly as strong as on the other platforms; only the assembly dependency
        // differs. The interop throws when the platform cannot produce random bytes, so a header
        // is never written with a predictable salt.
        private static unsafe void FillHeaderRandomBytes(Span<byte> header)
        {
            Span<byte> randomBytes = header.Slice(0, 10);
            fixed (byte* pRandomBytes = randomBytes)
            {
                Interop.GetCryptographicallySecureRandomBytes(pRandomBytes, randomBytes.Length);
            }
        }
    }
}
