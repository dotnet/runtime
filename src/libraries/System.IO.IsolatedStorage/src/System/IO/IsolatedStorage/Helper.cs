// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace System.IO.IsolatedStorage
{
    internal static partial class Helper
    {
        private const string IsolatedStorageDirectoryName = "IsolatedStorage";

        private static string s_machineRootDirectory;
        private static string s_roamingUserRootDirectory;
        private static string s_userRootDirectory;

        private static readonly char[] s_base32Char =
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h',
            'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
            'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
            'y', 'z', '0', '1', '2', '3', '4', '5'
        };

        /// <summary>
        /// The full root directory is the relevant special folder from Environment.GetFolderPath() plus "IsolatedStorage"
        /// and a set of random directory names if not roaming.
        /// 
        /// Examples:
        /// 
        ///     User: @"C:\Users\jerem\AppData\Local\IsolatedStorage\10v31ho4.bo2\eeolfu22.f2w\"
        ///     User|Roaming: @"C:\Users\jerem\AppData\Roaming\IsolatedStorage\"
        ///     Machine: @"C:\ProgramData\IsolatedStorage\nin03cyc.wr0\o3j0urs3.0sn\"
        /// 
        /// Identity for the current store gets tacked on after this.
        /// </summary>
        internal static string GetRootDirectory(IsolatedStorageScope scope)
        {
            if (IsRoaming(scope))
            {
                if (string.IsNullOrEmpty(s_roamingUserRootDirectory))
                {
                    s_roamingUserRootDirectory = GetDataDirectory(scope);
                }
                return s_roamingUserRootDirectory;
            }

            if (IsMachine(scope))
            {
                if (string.IsNullOrEmpty(s_machineRootDirectory))
                {
                    s_machineRootDirectory = GetRandomDirectory(GetDataDirectory(scope), scope);
                }
                return s_machineRootDirectory;
            }

            if (string.IsNullOrEmpty(s_userRootDirectory))
                s_userRootDirectory = GetRandomDirectory(GetDataDirectory(scope), scope);

            return s_userRootDirectory;
        }

        internal static string GetRandomDirectory(string rootDirectory, IsolatedStorageScope scope)
        {
            string randomDirectory = GetExistingRandomDirectory(rootDirectory);
            if (string.IsNullOrEmpty(randomDirectory))
            {
                using (Mutex m = CreateMutexNotOwned(rootDirectory))
                {
                    if (!m.WaitOne())
                    {
                        throw new IsolatedStorageException(SR.IsolatedStorage_Init);
                    }

                    try
                    {
                        randomDirectory = GetExistingRandomDirectory(rootDirectory);
                        if (string.IsNullOrEmpty(randomDirectory))
                        {
                            // Someone else hasn't created the directory before we took the lock
                            randomDirectory = Path.Combine(rootDirectory, Path.GetRandomFileName(), Path.GetRandomFileName());
                            CreateDirectory(randomDirectory, scope);
                        }
                    }
                    finally
                    {
                        m.ReleaseMutex();
                    }
                }
            }

            return randomDirectory;
        }

        internal static string GetExistingRandomDirectory(string rootDirectory)
        {
            // Look for an existing random directory at the given root
            // (a set of nested directories that were created via Path.GetRandomFileName())

            // Older versions of the desktop framework created longer (24 character) random paths and would
            // migrate them if they could not find the new style directory.

            if (!Directory.Exists(rootDirectory))
                return null;

            foreach (string directory in Directory.GetDirectories(rootDirectory))
            {
                if (Path.GetFileName(directory)?.Length == 12)
                {
                    foreach (string subdirectory in Directory.GetDirectories(directory))
                    {
                        if (Path.GetFileName(subdirectory)?.Length == 12)
                        {
                            return subdirectory;
                        }
                    }
                }
            }

            return null;
        }

        private static Mutex CreateMutexNotOwned(string pathName)
        {
            return new Mutex(initiallyOwned: false, name: @"Global\" + GetStrongHashSuitableForObjectName(pathName));
        }

        internal static string GetNormalizedUriHash(Uri uri)
        {
            // On desktop System.Security.Url is used as evidence, it has an internal Normalize() method.
            // Uri.ToString() appears to be functionally equivalent.
            return GetStrongHashSuitableForObjectName(uri.ToString());
        }

        internal static string GetNormalizedStrongNameHash(AssemblyName name)
        {
            byte[] publicKey = name.GetPublicKey();

            // If we don't have a key, we're not strong named
            if (publicKey == null || publicKey.Length == 0)
                return null;

            // Emulate what we get from StrongName.Normalize().
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(publicKey);
                    bw.Write(name.Version.Major);
                    bw.Write(name.Name);

                    ms.Position = 0;
                    return GetStrongHashSuitableForObjectName(ms);
                }
            }
        }

        internal static string GetStrongHashSuitableForObjectName(string name)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter b = new BinaryWriter(ms))
                {
                    b.Write(name.ToUpperInvariant());

                    ms.Position = 0;
                    return GetStrongHashSuitableForObjectName(ms);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "Compat: Used to generate an 8.3 filename.")]
        internal static string GetStrongHashSuitableForObjectName(Stream stream)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                return ToBase32StringSuitableForDirName(sha1.ComputeHash(stream));
            }
        }

        // This is from the NetFX Path class. The implementation in CoreFx was optimized for internal Path usage so
        // we can't share the implementation.
        internal static string ToBase32StringSuitableForDirName(byte[] buff)
        {
            // This routine is optimised to be used with buffs of length 20
            Debug.Assert(((buff.Length % 5) == 0), "Unexpected hash length");

            StringBuilder sb = new StringBuilder();
            byte b0, b1, b2, b3, b4;
            int l, i;

            l = buff.Length;
            i = 0;

            // Create l chars using the last 5 bits of each byte.  
            // Consume 3 MSB bits 5 bytes at a time.

            do
            {
                b0 = (i < l) ? buff[i++] : (byte)0;
                b1 = (i < l) ? buff[i++] : (byte)0;
                b2 = (i < l) ? buff[i++] : (byte)0;
                b3 = (i < l) ? buff[i++] : (byte)0;
                b4 = (i < l) ? buff[i++] : (byte)0;

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

        internal static bool IsMachine(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Machine) != 0);
        internal static bool IsAssembly(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Assembly) != 0);
        internal static bool IsApplication(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Application) != 0);
        internal static bool IsRoaming(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Roaming) != 0);
        internal static bool IsDomain(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Domain) != 0);
    }
}
