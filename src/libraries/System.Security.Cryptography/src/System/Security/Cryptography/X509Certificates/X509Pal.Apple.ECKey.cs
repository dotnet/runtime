// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Apple;

namespace System.Security.Cryptography.X509Certificates
{
    internal partial class X509Pal
    {
        private sealed partial class AppleX509Pal : ManagedX509ExtensionProcessor, IX509Pal
        {
            public ECDsa DecodeECDsaPublicKey(ICertificatePal? certificatePal)
            {
                return new ECDsaImplementation.ECDsaSecurityTransforms(DecodeECPublicKey(certificatePal));
            }

            public ECDiffieHellman DecodeECDiffieHellmanPublicKey(ICertificatePal? certificatePal)
            {
                return new ECDiffieHellmanImplementation.ECDiffieHellmanSecurityTransforms(DecodeECPublicKey(certificatePal));
            }

            private static SafeSecKeyRefHandle DecodeECPublicKey(ICertificatePal? certificatePal)
            {
                const int errSecInvalidKeyRef = -67712;
                const int errSecUnsupportedKeySize = -67735;

                if (certificatePal is null)
                    throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);

                AppleCertificatePal applePal = (AppleCertificatePal)certificatePal;
                SafeSecKeyRefHandle key = Interop.AppleCrypto.X509GetPublicKey(applePal.CertificateHandle);

                // If X509GetPublicKey uses the new SecCertificateCopyKey API it can return an invalid
                // key reference for unsupported algorithms. This currently happens for the BrainpoolP160r1
                // algorithm in the test suite (as of macOS Mojave Developer Preview 4).
                if (key.IsInvalid)
                {
                    key.Dispose();
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecInvalidKeyRef);
                }

                // EccGetKeySizeInBits can fail for two reasons. First, the Apple implementation has changed
                // and we receive values from API that were not previously handled. In that case the
                // implementation will need to be adjusted to handle these values. Second, we deliberately
                // return 0 from the native code to prevent hitting buggy API implementations in Apple code
                // later.
                if (Interop.AppleCrypto.EccGetKeySizeInBits(key) == 0)
                {
                    key.Dispose();
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecUnsupportedKeySize);
                }

                return key;
            }
        }
    }
}
