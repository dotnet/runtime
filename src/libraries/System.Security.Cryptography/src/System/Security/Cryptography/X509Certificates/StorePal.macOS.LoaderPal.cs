// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Apple;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class StorePal
    {
        private sealed class AppleCertLoader : ILoaderPal
        {
            private readonly SafeCFArrayHandle _collectionHandle;
            private readonly SafeTemporaryKeychainHandle? _tmpKeychain;

            public AppleCertLoader(SafeCFArrayHandle collectionHandle, SafeTemporaryKeychainHandle? tmpKeychain)
            {
                _collectionHandle = collectionHandle;
                _tmpKeychain = tmpKeychain;
            }

            public void Dispose()
            {
                _collectionHandle.Dispose();
                _tmpKeychain?.Dispose();
            }

            public void MoveTo(X509Certificate2Collection collection)
            {
                long longCount = Interop.CoreFoundation.CFArrayGetCount(_collectionHandle);

                if (longCount > int.MaxValue)
                    throw new CryptographicException();

                int count = (int)longCount;

                // Apple returns things in the opposite order from Windows, so read backwards.
                for (int i = count - 1; i >= 0; i--)
                {
                    IntPtr handle = Interop.CoreFoundation.CFArrayGetValueAtIndex(_collectionHandle, i);

                    if (handle != IntPtr.Zero)
                    {
                        ICertificatePal? certPal = AppleCertificatePal.FromHandle(handle, throwOnFail: false);

                        if (certPal != null)
                        {
                            X509Certificate2 cert = new X509Certificate2(certPal);
                            collection.Add(cert);
                        }
                    }
                }
            }
        }

        private sealed class ApplePkcs12CertLoader : ILoaderPal
        {
            private readonly ApplePkcs12Reader _pkcs12;
            private readonly SafeKeychainHandle _keychain;
            private SafePasswordHandle _password;
            private readonly bool _exportable;

            public ApplePkcs12CertLoader(
                ApplePkcs12Reader pkcs12,
                SafeKeychainHandle keychain,
                SafePasswordHandle password,
                bool exportable)
            {
                _pkcs12 = pkcs12;
                _keychain = keychain;
                _exportable = exportable;

                bool addedRef = false;
                password.DangerousAddRef(ref addedRef);
                _password = password;
            }

            public void Dispose()
            {
                _pkcs12.Dispose();

                // Only dispose the keychain if it's a temporary handle.
                (_keychain as SafeTemporaryKeychainHandle)?.Dispose();

                SafePasswordHandle? password = Interlocked.Exchange(ref _password, null!);
                password?.DangerousRelease();
            }

            public void MoveTo(X509Certificate2Collection collection)
            {
                foreach (UnixPkcs12Reader.CertAndKey certAndKey in _pkcs12.EnumerateAll())
                {
                    AppleCertificatePal pal = (AppleCertificatePal)certAndKey.Cert!;
                    SafeSecKeyRefHandle? safeSecKeyRefHandle =
                        ApplePkcs12Reader.GetPrivateKey(certAndKey.Key);

                    using (safeSecKeyRefHandle)
                    {
                        ICertificatePal newPal;

                        // SecItemImport doesn't seem to respect non-exportable import for PKCS#8,
                        // only PKCS#12.
                        //
                        // So, as part of reading this PKCS#12 we now need to write the minimum
                        // PKCS#12 in a normalized form, and ask the OS to import it.
                        if (!_exportable && safeSecKeyRefHandle != null)
                        {
                            newPal = AppleCertificatePal.ImportPkcs12NonExportable(
                                pal,
                                safeSecKeyRefHandle,
                                _password,
                                _keychain);
                        }
                        else
                        {
                            newPal = pal.MoveToKeychain(_keychain, safeSecKeyRefHandle) ?? pal;
                        }

                        X509Certificate2 cert = new X509Certificate2(newPal);
                        collection.Add(cert);
                    }
                }
            }
        }
    }
}
