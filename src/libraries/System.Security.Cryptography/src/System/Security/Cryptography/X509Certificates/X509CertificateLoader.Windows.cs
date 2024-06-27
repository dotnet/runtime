// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    public static partial class X509CertificateLoader
    {
        private static partial ICertificatePal LoadCertificatePal(ReadOnlySpan<byte> data)
        {
            unsafe
            {
                fixed (byte* dataPtr = data)
                {
                    Interop.Crypt32.DATA_BLOB blob = new Interop.Crypt32.DATA_BLOB(
                        (IntPtr)dataPtr,
                        (uint)data.Length);

                    return LoadCertificate(
                        Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_BLOB,
                        &blob);
                }
            }
        }

        private static partial ICertificatePal LoadCertificatePalFromFile(string path)
        {
            unsafe
            {
                fixed (char* pathPtr = path)
                {
                    return LoadCertificate(
                        Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_FILE,
                        pathPtr);
                }
            }
        }

        static partial void LoadPkcs12NoLimits(
            ReadOnlyMemory<byte> data,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags,
            ref Pkcs12Return earlyReturn)
        {
            bool deleteKeyContainer = ShouldDeleteKeyContainer(keyStorageFlags);

            using (SafeCertStoreHandle storeHandle = ImportPfx(data.Span, password, keyStorageFlags))
            {
                CertificatePal pal = LoadPkcs12(storeHandle, deleteKeyContainer);
                earlyReturn = new Pkcs12Return(pal);
            }
        }

        static partial void LoadPkcs12NoLimits(
            ReadOnlyMemory<byte> data,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags,
            ref X509Certificate2Collection? earlyReturn)
        {
            bool deleteKeyContainers = ShouldDeleteKeyContainer(keyStorageFlags);

            using (SafeCertStoreHandle storeHandle = ImportPfx(data.Span, password, keyStorageFlags))
            {
                earlyReturn = LoadPkcs12Collection(storeHandle, deleteKeyContainers);
            }
        }

        private static partial Pkcs12Return LoadPkcs12(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags)
        {
            bool deleteKeyContainer = ShouldDeleteKeyContainer(keyStorageFlags);

            using (SafeCertStoreHandle storeHandle = ImportPfx(ref bagState, password, keyStorageFlags))
            {
                CertificatePal pal = LoadPkcs12(storeHandle, deleteKeyContainer);
                return new Pkcs12Return(pal);
            }
        }

        private static CertificatePal LoadPkcs12(
            SafeCertStoreHandle storeHandle,
            bool deleteKeyContainer)
        {
            // Find the first cert with private key. If none, then simply take the very first cert.
            // Along the way, delete the persisted keys of any cert we don't accept.
            SafeCertContextHandle? bestCert = null;
            SafeCertContextHandle? nextCert = null;
            bool havePrivKey = false;

            while (Interop.crypt32.CertEnumCertificatesInStore(storeHandle, ref nextCert))
            {
                Debug.Assert(nextCert is not null);
                Debug.Assert(!nextCert.IsInvalid);

                if (nextCert.ContainsPrivateKey)
                {
                    if (bestCert is not null && bestCert.ContainsPrivateKey)
                    {
                        // We already found our chosen one. Free up this one's key and move on.

                        // If this one has a persisted private key, clean up the key file.
                        // If it was an ephemeral private key no action is required.
                        if (nextCert.HasPersistedPrivateKey)
                        {
                            SafeCertContextHandleWithKeyContainerDeletion.DeleteKeyContainer(nextCert);
                        }
                    }
                    else
                    {
                        // Found our first cert that has a private key.
                        //
                        // Set it up as our chosen one but keep iterating
                        // as we need to free up the keys of any remaining certs.
                        bestCert?.Dispose();
                        bestCert = nextCert.Duplicate();
                        havePrivKey = true;
                    }
                }
                else
                {
                    // Doesn't have a private key but hang on to it anyway,
                    // in case we don't find any certs with a private key.
                    bestCert ??= nextCert.Duplicate();
                }
            }

            if (bestCert is null)
            {
                throw new CryptographicException(SR.Cryptography_Pfx_NoCertificates);
            }

            bool deleteThisKeyContainer = havePrivKey && deleteKeyContainer;
            CertificatePal pal = new CertificatePal(bestCert, deleteThisKeyContainer);
            return pal;
        }

        private static partial X509Certificate2Collection LoadPkcs12Collection(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags)
        {
            bool deleteKeyContainers = ShouldDeleteKeyContainer(keyStorageFlags);

            using (SafeCertStoreHandle storeHandle = ImportPfx(ref bagState, password, keyStorageFlags))
            {
                return LoadPkcs12Collection(storeHandle, deleteKeyContainers);
            }
        }

        private static X509Certificate2Collection LoadPkcs12Collection(
            SafeCertStoreHandle storeHandle,
            bool deleteKeyContainers)
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();
            SafeCertContextHandle? nextCert = null;

            while (Interop.crypt32.CertEnumCertificatesInStore(storeHandle, ref nextCert))
            {
                Debug.Assert(nextCert is not null);
                Debug.Assert(!nextCert.IsInvalid);

                bool deleteThis = deleteKeyContainers && nextCert.HasPersistedPrivateKey;
                CertificatePal pal = new CertificatePal(nextCert.Duplicate(), deleteThis);
                coll.Add(new X509Certificate2(pal));
            }

            return coll;
        }

        private static unsafe CertificatePal LoadCertificate(
            Interop.Crypt32.CertQueryObjectType objectType,
            void* pvObject)
        {
            Debug.Assert(objectType != 0);
            Debug.Assert(pvObject != (void*)0);

            const Interop.Crypt32.ContentType ContentType =
                Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_CERT;
            const Interop.Crypt32.ExpectedContentTypeFlags ExpectedContentType =
                Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT;

            bool loaded = Interop.Crypt32.CryptQueryObject(
                objectType,
                pvObject,
                ExpectedContentType,
                Interop.Crypt32.ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL,
                dwFlags: 0,
                pdwMsgAndCertEncodingType: IntPtr.Zero,
                out Interop.Crypt32.ContentType actualType,
                pdwFormatType: IntPtr.Zero,
                phCertStore: IntPtr.Zero,
                phMsg: IntPtr.Zero,
                out SafeCertContextHandle singleContext);

            if (!loaded)
            {
                singleContext.Dispose();
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
            }

            // Since contentType is an input filter, actualType should not be possible to disagree.
            //
            // Since contentType is only CERT, singleContext should either be valid, or the
            // function should have returned false.
            if (actualType != ContentType || singleContext.IsInvalid)
            {
                singleContext.Dispose();
                throw new CryptographicException();
            }

            CertificatePal pal = new CertificatePal(singleContext, deleteKeyContainer: false);
            return pal;
        }

        private static SafeCertStoreHandle ImportPfx(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags)
        {
            ArraySegment<byte> reassembled = bagState.ToPfx(password);
            SafeCertStoreHandle storeHandle = ImportPfx(reassembled, password, keyStorageFlags);
            CryptoPool.Return(reassembled);

            return storeHandle;
        }

        private static unsafe SafeCertStoreHandle ImportPfx(
            ReadOnlySpan<byte> data,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags)
        {
            const int MaxStackPasswordLength = 64;
            Span<char> szPassword = stackalloc char[MaxStackPasswordLength + 1];
            Interop.Crypt32.PfxCertStoreFlags flags = MapKeyStorageFlags(keyStorageFlags);

            if (password.Length >= MaxStackPasswordLength)
            {
                szPassword = new char[password.Length + 1];
            }

            SafeCertStoreHandle storeHandle;

            fixed (byte* dataPtr = data)
            fixed (char* szPtr = szPassword)
            {
                try
                {
                    password.CopyTo(szPassword);
                    szPassword[password.Length] = '\0';

                    Interop.Crypt32.DATA_BLOB blob = new((IntPtr)dataPtr, (uint)data.Length);

                    storeHandle = Interop.Crypt32.PFXImportCertStore(
                        ref blob,
                        szPtr,
                        flags);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(szPassword));
                }
            }

            if (storeHandle.IsInvalid)
            {
                Exception e = Marshal.GetHRForLastWin32Error().ToCryptographicException();
                storeHandle.Dispose();
                throw e;
            }

            return storeHandle;
        }

        private static Interop.Crypt32.PfxCertStoreFlags MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert((keyStorageFlags & KeyStorageFlagsAll) == keyStorageFlags);

            Interop.Crypt32.PfxCertStoreFlags pfxCertStoreFlags = 0;
            if ((keyStorageFlags & X509KeyStorageFlags.UserKeySet) == X509KeyStorageFlags.UserKeySet)
                pfxCertStoreFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_USER_KEYSET;
            else if ((keyStorageFlags & X509KeyStorageFlags.MachineKeySet) == X509KeyStorageFlags.MachineKeySet)
                pfxCertStoreFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_MACHINE_KEYSET;

            if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable)
                pfxCertStoreFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_EXPORTABLE;
            if ((keyStorageFlags & X509KeyStorageFlags.UserProtected) == X509KeyStorageFlags.UserProtected)
                pfxCertStoreFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_USER_PROTECTED;

            // If a user is asking for an Ephemeral key they should be willing to test their code to find out
            // that it will no longer import into CAPI. This solves problems of legacy CSPs being
            // difficult to do SHA-2 RSA signatures with, simplifies the story for UWP, and reduces the
            // complexity of pointer interpretation.
            if ((keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) == X509KeyStorageFlags.EphemeralKeySet)
                pfxCertStoreFlags |= Interop.Crypt32.PfxCertStoreFlags.PKCS12_NO_PERSIST_KEY | Interop.Crypt32.PfxCertStoreFlags.PKCS12_ALWAYS_CNG_KSP;

            // In .NET Framework loading a PFX then adding the key to the Windows Certificate Store would
            // enable a native application compiled against CAPI to find that private key and interoperate with it.
            //
            // For .NET Core this behavior is being retained.

            return pfxCertStoreFlags;
        }

        private static bool ShouldDeleteKeyContainer(X509KeyStorageFlags keyStorageFlags)
        {
            // If PersistKeySet is set we don't delete the key, so that it persists.
            // If EphemeralKeySet is set we don't delete the key, because there's no file, so it's a wasteful call.
            const X509KeyStorageFlags DeleteUnless =
                X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet;

            bool deleteKeyContainer = ((keyStorageFlags & DeleteUnless) == 0);
            return deleteKeyContainer;
        }
    }
}
