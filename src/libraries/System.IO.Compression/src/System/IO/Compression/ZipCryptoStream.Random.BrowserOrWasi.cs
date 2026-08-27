// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal sealed partial class ZipCryptoStream
    {
        // Browser and wasi deliberately do not reference System.Security.Cryptography: WinZip AES
        // is unsupported there (WinZipAes.PlatformNotSupported.cs) because the cryptography
        // assembly is PlatformNotSupported on both, so taking the dependency just to fill 10 bytes
        // of ZipCrypto header salt would pull the whole assembly into every app that touches a zip
        // archive.
        //
        // Instead the salt comes straight from the System.Native random interop, which routes to
        // minipal_get_cryptographically_secure_random_bytes - SystemJS_RandomBytes
        // (crypto.getRandomValues) on browser, getentropy on wasi. On browser that is the very
        // same generator that backs RandomNumberGenerator (see
        // RandomNumberGeneratorImplementation.Browser.cs), so the salt is exactly as strong as on
        // the other platforms; only the assembly dependency differs. The interop throws when the
        // platform cannot produce random bytes, so a header is never written with a predictable
        // salt.
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
