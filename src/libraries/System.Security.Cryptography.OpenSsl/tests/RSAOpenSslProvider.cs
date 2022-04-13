// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSAOpenSslProvider : IRSAProvider
    {
        private bool? _supportsSha1Signatures;

        public RSA Create() => new RSAOpenSsl();

        public RSA Create(int keySize) => new RSAOpenSsl(keySize);

        public bool Supports384PrivateKey => true;

        public bool SupportsLargeExponent => true;

        public bool SupportsSha2Oaep => true;

        public bool SupportsPss => true;

        public bool SupportsSha1Signatures
        {
            get
            {
                if (!_supportsSha1Signatures.HasValue)
                {
                    if (OperatingSystem.IsLinux())
                    {
                        RSA rsa = Create();

                        try
                        {
                            rsa.SignData(Array.Empty<byte>(), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                            _supportsSha1Signatures = true;
                        }
                        catch (CryptographicException)
                        {
                            _supportsSha1Signatures = false;
                        }
                        finally
                        {
                            rsa.Dispose();
                        }
                    }
                    else
                    {
                        // Currently all non-Linux OSes support RSA-SHA1.
                        _supportsSha1Signatures = true;
                    }
                }

                return _supportsSha1Signatures.Value;
            }
        }
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new RSAOpenSslProvider();
    }
}
