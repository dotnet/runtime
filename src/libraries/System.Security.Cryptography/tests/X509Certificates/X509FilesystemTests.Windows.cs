// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Pkcs;
using System.Security.Principal;
using System.Threading;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [Collection("X509Filesystem")]
    public static class X509FilesystemTests
    {
        // Microsoft Strong Cryptographic Provider
        private static readonly AsnEncodedData s_capiCsp = new AsnEncodedData(
            new Oid("1.3.6.1.4.1.311.17.1", null),
            (
                "1E4E004D006900630072006F0073006F006600740020005300740072006F006E" +
                "0067002000430072007900700074006F00670072006100700068006900630020" +
                "00500072006F00760069006400650072"
            ).HexToByteArray());

        private static readonly AsnEncodedData s_machineKey = new AsnEncodedData(
            new Oid("1.3.6.1.4.1.311.17.2", null),
            [0x05, 0x00]);

        // 6 random keys that will used across all of the tests in this file
        private const int KeyGenKeySize = 2048;
        private static readonly RSA[] s_keys =
        {
            RSA.Create(KeyGenKeySize), RSA.Create(KeyGenKeySize), RSA.Create(KeyGenKeySize),
            RSA.Create(KeyGenKeySize), RSA.Create(KeyGenKeySize), RSA.Create(KeyGenKeySize),
        };

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_MultiplePrivateKey_Ctor(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            AllFilesDeletedTest(
                storageFlags,
                capi,
                multiPrivate: true,
                static (bytes, pwd, flags) => new X509Certificate2(bytes, pwd, flags));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_SinglePrivateKey_Ctor(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            AllFilesDeletedTest(
                storageFlags,
                capi,
                multiPrivate: false,
                static (bytes, pwd, flags) => new X509Certificate2(bytes, pwd, flags));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_MultiplePrivateKey_CollImport(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            AllFilesDeletedTest(
                storageFlags,
                capi,
                multiPrivate: true,
                Cert.Import);
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_SinglePrivateKey_CollImport(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            AllFilesDeletedTest(
                storageFlags,
                capi,
                multiPrivate: false,
                Cert.Import);
        }

        private static void AllFilesDeletedTest(
            X509KeyStorageFlags storageFlags,
            bool capi,
            bool multiPrivate,
            Func<byte[], string, X509KeyStorageFlags, IDisposable> importer,
            [CallerMemberName] string? name = null)
        {
            const X509KeyStorageFlags NonDefaultKeySet =
                X509KeyStorageFlags.UserKeySet |
                X509KeyStorageFlags.MachineKeySet;

            bool defaultKeySet = (storageFlags & NonDefaultKeySet) == 0;
            int certAndKeyCount = multiPrivate ? s_keys.Length : 1;

            byte[] pfx = MakePfx(certAndKeyCount, capi, name);

            EnsureNoKeysGained(
                (Bytes: pfx, Flags: storageFlags, Importer: importer),
                static state => state.Importer(state.Bytes, "", state.Flags));

            // When importing for DefaultKeySet, try both 010101 and 101010
            // intermixing of machine and user keys so that single key import
            // gets both a machine key and a user key.
            if (defaultKeySet)
            {
                pfx = MakePfx(certAndKeyCount, capi, name, 1);

                EnsureNoKeysGained(
                    (Bytes: pfx, Flags: storageFlags, Importer: importer),
                    static state => state.Importer(state.Bytes, "", state.Flags));
            }
        }

        private static byte[] MakePfx(
            int certAndKeyCount,
            bool capi,
            [CallerMemberName] string? name = null,
            int machineKeySkew = 0)
        {
            Pkcs12SafeContents keys = new Pkcs12SafeContents();
            Pkcs12SafeContents certs = new Pkcs12SafeContents();
            DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
            DateTimeOffset notAfter = notBefore.AddMinutes(10);

            PbeParameters pbeParams = new PbeParameters(
                PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                HashAlgorithmName.SHA1,
                1);

            Span<int> indices = [0, 1, 2, 3, 4, 5];
            RandomNumberGenerator.Shuffle(indices);

            for (int i = 0; i < s_keys.Length; i++)
            {
                RSA key = s_keys[indices[i]];

                CertificateRequest req = new CertificateRequest(
                        $"CN={name}.{i}",
                        key,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    Pkcs12CertBag certBag = certs.AddCertificate(cert);

                    if (i < certAndKeyCount)
                    {
                        Pkcs12ShroudedKeyBag keyBag = keys.AddShroudedKey(key, "", pbeParams);

                        if (capi)
                        {
                            keyBag.Attributes.Add(s_capiCsp);
                        }

                        if (int.IsEvenInteger(i + machineKeySkew))
                        {
                            keyBag.Attributes.Add(s_machineKey);
                        }

                        byte keyId = checked((byte)i);
                        Pkcs9LocalKeyId localKeyId = new Pkcs9LocalKeyId(new ReadOnlySpan<byte>(ref keyId));
                        keyBag.Attributes.Add(localKeyId);
                        certBag.Attributes.Add(localKeyId);
                    }
                }
            }

            Pkcs12Builder builder = new Pkcs12Builder();
            builder.AddSafeContentsEncrypted(certs, "", pbeParams);
            builder.AddSafeContentsUnencrypted(keys);
            builder.SealWithMac("", HashAlgorithmName.SHA1, 1);
            return builder.Encode();
        }

        private static void EnsureNoKeysGained<TState>(TState state, Func<TState, IDisposable> importer)
        {
            const int ERROR_ACCESS_DENIED = (unchecked((int)0x80010005));

            // In the good old days, before we had threads or parallel processes, these tests would be easy:
            // * Read the directory listing(s)
            // * Import a thing
            // * See what new things were added
            // * Dispose the thing
            // * See that the new things went away
            //
            // But, since files can be created by tests on other threads, or even by other processes,
            // recheck the directory a few times (MicroRetryCount) after sleeping (SleepMs).
            //
            // Sadly, that's not sufficient, because an extra file gained during that window could itself
            // be leaked, or be intentionally persisted beyond the recheck interval.  So, instead of failing,
            // try again from the beginning.  If we get parallel leaked on MacroRetryCount times in a row
            // we'll still false-fail, but unless a majority of the tests in the process are leaking keys,
            // it's unlikely.
            //
            // Before changing these constants to bigger numbers, consider the combinatorics. Failure will
            // sleep (MacroRetryCount * (MicroRetryCount - 1) * SleepMs) ms, and also involves non-zero work.
            // Failing 29 tests at (3, 5, 1000) adds about 6 minutes to the test run compared to success.

            const int MacroRetryCount = 3;
            const int MicroRetryCount = 5;
            const int SleepMs = 1000;

            KeyPaths keyPaths = KeyPaths.GetKeyPaths();
            HashSet<string> gainedFiles = null;

            for (int macro = 0; macro < MacroRetryCount; macro++)
            {
                List<string> keysBefore = new(keyPaths.EnumerateAllKeys());

                IDisposable imported = null;

                try
                {
                    imported = importer(state);
                }
                catch (CryptographicException ex) when (ex.HResult == ERROR_ACCESS_DENIED)
                {
                }

                imported?.Dispose();

                gainedFiles = new HashSet<string>(keyPaths.EnumerateAllKeys());
                gainedFiles.ExceptWith(keysBefore);

                for (int micro = 0; micro < MicroRetryCount; micro++)
                {
                    if (gainedFiles.Count == 0)
                    {
                        return;
                    }

                    HashSet<string> thisTry = new(keyPaths.EnumerateAllKeys());
                    gainedFiles.IntersectWith(thisTry);

                    if (gainedFiles.Count != 0 && micro < MicroRetryCount - 1)
                    {
                        Thread.Sleep(SleepMs);
                    }
                }
            }

            Assert.Empty(keyPaths.MapPaths(gainedFiles));
        }

        private sealed class KeyPaths
        {
            private static volatile KeyPaths s_instance;

            private string _capiUserDsa;
            private string _capiUserRsa;
            private string _capiMachineDsa;
            private string _capiMachineRsa;
            private string _cngUser;
            private string _cngMachine;

            private KeyPaths()
            {
            }

            internal IEnumerable<string> MapPaths(IEnumerable<string> paths)
            {
                foreach (string path in paths)
                {
                    yield return
                        Replace(path, _cngUser, "CNG-USER") ??
                        Replace(path, _capiUserRsa, "CAPI-USER-RSA") ??
                        Replace(path, _cngMachine, "CNG-MACH") ??
                        Replace(path, _capiMachineRsa, "CAPI-MACH-RSA") ??
                        Replace(path, _capiUserDsa, "CAPI-USER-DSS") ??
                        Replace(path, _capiMachineDsa, "CAPI-MACH-DSS") ??
                        path;
                }

                static string Replace(string path, string prefix, string ifMatched)
                {
                    if (path.StartsWith(prefix))
                    {
                        return path.Replace(prefix, ifMatched);
                    }

                    return null;
                }
            }

            internal IEnumerable<string> EnumerateCapiUserKeys()
            {
                return EnumerateFiles(_capiUserRsa).Concat(EnumerateFiles(_capiUserDsa));
            }

            internal IEnumerable<string> EnumerateCapiMachineKeys()
            {
                return EnumerateFiles(_capiMachineRsa).Concat(EnumerateFiles(_capiMachineDsa));
            }

            internal IEnumerable<string> EnumerateCngUserKeys()
            {
                return EnumerateFiles(_cngUser);
            }

            internal IEnumerable<string> EnumerateCngMachineKeys()
            {
                return EnumerateFiles(_cngMachine);
            }

            internal IEnumerable<string> EnumerateUserKeys()
            {
                return EnumerateCapiUserKeys().Concat(EnumerateCngUserKeys());
            }

            internal IEnumerable<string> EnumerateMachineKeys()
            {
                return EnumerateCapiMachineKeys().Concat(EnumerateCngMachineKeys());
            }

            internal IEnumerable<string> EnumerateAllKeys()
            {
                return EnumerateUserKeys().Concat(EnumerateMachineKeys());
            }

            private static IEnumerable<string> EnumerateFiles(string directory)
            {
                try
                {
                    return Directory.EnumerateFiles(directory);
                }
                catch (DirectoryNotFoundException)
                {
                }

                return [];
            }

            internal static KeyPaths GetKeyPaths()
            {
                if (s_instance is not null)
                {
                    return s_instance;
                }

                // https://learn.microsoft.com/en-us/windows/win32/seccng/key-storage-and-retrieval
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                string userSid = identity.User!.ToString();

                string userKeyBase = Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft",
                    "Crypto");

                string machineKeyBase = Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Microsoft",
                    "Crypto");

                KeyPaths paths = new()
                {
                    _capiUserDsa = Path.Join(userKeyBase, "DSS", userSid),
                    _capiUserRsa = Path.Join(userKeyBase, "RSA", userSid),
                    _capiMachineDsa = Path.Join(machineKeyBase, "DSS", "MachineKeys"),
                    _capiMachineRsa = Path.Join(machineKeyBase, "RSA", "MachineKeys"),
                    _cngUser = Path.Join(userKeyBase, "Keys"),
                    _cngMachine = Path.Join(machineKeyBase, "Keys"),
                };

                s_instance = paths;
                return s_instance;
            }
        }
    }
}
