// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace System.Security
{
    /// <summary>
    /// Helper class for getting identity hashes for types that used
    /// to live in Assembly Evidence.
    /// </summary>
    internal static class IdentityHelper
    {
        private static readonly char[] s_base32Char =
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h',
            'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
            'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
            'y', 'z', '0', '1', '2', '3', '4', '5'
        };

        /// <summary>
        /// Gives a hash equivalent to what Url.Normalize() gives.
        /// </summary>
        internal static string GetNormalizedUriHash(Uri uri)
        {
            // On .NET Framework System.Security.Url is used as evidence, it has an internal Normalize() method.
            // Uri.ToString() appears to be functionally equivalent.
            return GetStrongHashSuitableForObjectName(uri.ToString());
        }

        /// <summary>
        /// Uses the AssemblyName's public key to generate a hash equivalent to what
        /// StrongName.Normalize() gives.
        /// </summary>
        internal static string? GetNormalizedStrongNameHash(AssemblyName name)
        {
            byte[]? publicKey = name.GetPublicKey();

            // If we don't have a key, we're not strong named
            if (publicKey == null || publicKey.Length == 0)
                return null;

            // Emulate what we get from StrongName.Normalize().
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(publicKey);
                bw.Write(name.Version!.Major);
                bw.Write(name.Name!);

                ms.Position = 0;
                return GetStrongHashSuitableForObjectName(ms);
            }
        }

        internal static string GetStrongHashSuitableForObjectName(string name)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter b = new BinaryWriter(ms))
            {
                b.Write(name.ToUpperInvariant());

                ms.Position = 0;
                return GetStrongHashSuitableForObjectName(ms);
            }
        }

        [Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "Compat: Used to generate an 8.3 filename.")]
        internal static string GetStrongHashSuitableForObjectName(Stream stream)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                return ToBase32StringSuitableForDirName(sha1.ComputeHash(stream));
            }
        }

        // This is from the .NET Framework Path class. The implementation in the runtime libraries was optimized for internal Path usage so
        // we can't share the implementation.
        internal static string ToBase32StringSuitableForDirName(byte[] buff)
        {
            // This routine is optimised to be used with buffs of length 20
            Debug.Assert(((buff.Length % 5) == 0), "Unexpected hash length");

            StringBuilder sb = new StringBuilder();
            int l, i;

            l = buff.Length;
            i = 0;

            // Create l chars using the last 5 bits of each byte.
            // Consume 3 MSB bits 5 bytes at a time.

            do
            {
                byte b0 = (i < l) ? buff[i++] : (byte)0;
                byte b1 = (i < l) ? buff[i++] : (byte)0;
                byte b2 = (i < l) ? buff[i++] : (byte)0;
                byte b3 = (i < l) ? buff[i++] : (byte)0;
                byte b4 = (i < l) ? buff[i++] : (byte)0;

                // Consume the 5 Least significant bits of each byte
                sb.Append(s_base32Char[b0 & 0x1F]);
                sb.Append(s_base32Char[b1 & 0x1F]);
                sb.Append(s_base32Char[b2 & 0x1F]);
                sb.Append(s_base32Char[b3 & 0x1F]);
                sb.Append(s_base32Char[b4 & 0x1F]);

                // Consume 3 MSB of b0, b1, MSB bits 6, 7 of b3, b4
                sb.Append(s_base32Char[(
                        ((b0 & 0xE0) >> 5) |
                        ((b3 & 0x60) >> 2))]);

                sb.Append(s_base32Char[(
                        ((b1 & 0xE0) >> 5) |
                        ((b4 & 0x60) >> 2))]);

                // Consume 3 MSB bits of b2, 1 MSB bit of b3, b4

                b2 >>= 5;

                Debug.Assert(((b2 & 0xF8) == 0), "Unexpected set bits");

                if ((b3 & 0x80) != 0)
                    b2 |= 0x08;
                if ((b4 & 0x80) != 0)
                    b2 |= 0x10;

                sb.Append(s_base32Char[b2]);

            } while (i < l);

            return sb.ToString();
        }
    }
}
