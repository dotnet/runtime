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
    /// <summary>
    /// Tests that apply to the filesystem/cache portions of the X509 infrastructure on Unix implementations.
    /// </summary>
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

        private static readonly Pkcs12LoaderLimits s_cspPreservingLimits = new Pkcs12LoaderLimits
        {
            PreserveStorageProvider = true,
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
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(6, capi: capi)),
                state => new X509Certificate2(state.Bytes, "", state.Flags));
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
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(1, 5, capi)),
                state => new X509Certificate2(state.Bytes, "", state.Flags));
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
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(6, capi: capi)),
                state => Cert.Import(state.Bytes, "", state.Flags));
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
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(1, 5, capi)),
                state => Cert.Import(state.Bytes, "", state.Flags));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_MultiplePrivateKey_SingleLoader(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(6, capi: capi)),
                state => X509CertificateLoader.LoadPkcs12(state.Bytes, "", state.Flags));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_SinglePrivateKey_SingleLoader(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(1, 5, capi)),
                state => X509CertificateLoader.LoadPkcs12(state.Bytes, "", state.Flags));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_MultiplePrivateKey_CollLoader(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(6, capi: capi)),
                state => new ImportedCollection(X509CertificateLoader.LoadPkcs12Collection(state.Bytes, "", state.Flags)));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_SinglePrivateKey_CollLoader(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(1, 5, capi)),
                state => new ImportedCollection(X509CertificateLoader.LoadPkcs12Collection(state.Bytes, "", state.Flags)));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_MultiplePrivateKey_SingleLoader_KeepCsp(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(6, capi: capi)),
                state => X509CertificateLoader.LoadPkcs12(state.Bytes, "", state.Flags, s_cspPreservingLimits));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_SinglePrivateKey_SingleLoader_KeepCsp(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(1, 5, capi)),
                state => X509CertificateLoader.LoadPkcs12(state.Bytes, "", state.Flags, s_cspPreservingLimits));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_MultiplePrivateKey_CollLoader_KeepCsp(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(6, capi: capi)),
                state => new ImportedCollection(
                    X509CertificateLoader.LoadPkcs12Collection(state.Bytes, "", state.Flags, s_cspPreservingLimits)));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.DefaultKeySet)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        [InlineData(X509KeyStorageFlags.UserKeySet)]
        [InlineData(X509KeyStorageFlags.UserKeySet, true)]
        [InlineData(X509KeyStorageFlags.MachineKeySet)]
        [InlineData(X509KeyStorageFlags.MachineKeySet, true)]
        public static void AllFilesDeleted_SinglePrivateKey_CollLoader_KeepCsp(X509KeyStorageFlags storageFlags, bool capi = false)
        {
            EnsureNoKeysGained(
                (Flags: storageFlags, Bytes: MakePfx(1, 5, capi)),
                state => new ImportedCollection(
                    X509CertificateLoader.LoadPkcs12Collection(state.Bytes, "", state.Flags, s_cspPreservingLimits)));
        }

        private static byte[] MakePfx(
            int certAndKeyCount = 1,
            int certOnlyCount = 0,
            bool capi = false,
            [CallerMemberName] string? name = null)
        {
            Pkcs12SafeContents keys = new Pkcs12SafeContents();
            Pkcs12SafeContents certs = new Pkcs12SafeContents();
            DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
            DateTimeOffset notAfter = notBefore.AddMinutes(10);

            // Every other key is a machine key. Depending on the minute when this line of code runs, it's
            // either starting with the first, or with the second.
            int machineKeySkew = DateTime.UtcNow.Minute % 2;

            PbeParameters pbeParams = new PbeParameters(
                PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                HashAlgorithmName.SHA1,
                1);

            for (int i = 0; i < certAndKeyCount; i++)
            {
                using (RSA key = RSA.Create(1024))
                {
                    CertificateRequest req = new CertificateRequest(
                        $"CN={name}.{i}",
                        key,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
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

                        certs.AddCertificate(cert);
                    }
                }
            }

            for (int i = 0; i < certOnlyCount; i++)
            {
                using (RSA key = RSA.Create(1024))
                {
                    CertificateRequest req = new CertificateRequest(
                        $"CN={name}.co.{i}",
                        key,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                    {
                        certs.AddCertificate(cert);
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
            HashSet<string> gainedFiles = null!;

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

                gainedFiles = new(keyPaths.EnumerateAllKeys());
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
                return Directory.EnumerateFiles(_capiUserRsa).Concat(Directory.EnumerateFiles(_capiUserDsa));
            }

            internal IEnumerable<string> EnumerateCapiMachineKeys()
            {
                return Directory.EnumerateFiles(_capiMachineRsa).Concat(Directory.EnumerateFiles(_capiMachineDsa));
            }

            internal IEnumerable<string> EnumerateCngUserKeys()
            {
                return Directory.EnumerateFiles(_cngUser);
            }

            internal IEnumerable<string> EnumerateCngMachineKeys()
            {
                return Directory.EnumerateFiles(_cngMachine);
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
