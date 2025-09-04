// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal static partial class CertificateHelpers
    {
        private static partial CryptographicException GetExceptionForLastError() =>
            Marshal.GetLastPInvokeError().ToCryptographicException();

        private static partial CertificatePal CopyFromRawBytes(CertificatePal certificate) =>
            (CertificatePal)CertificatePal.FromBlob(certificate.RawData, SafePasswordHandle.InvalidHandle, X509KeyStorageFlags.PersistKeySet);

        private static partial SafeNCryptKeyHandle CreateSafeNCryptKeyHandle(IntPtr handle, SafeHandle parentHandle) =>
            new SafeNCryptKeyHandle(handle, parentHandle);

        private static partial int GuessKeySpec(
            CngProvider provider,
            string keyName,
            bool machineKey,
            CngAlgorithmGroup? algorithmGroup)
        {
            if (provider == CngProvider.MicrosoftSoftwareKeyStorageProvider ||
                provider == CngProvider.MicrosoftSmartCardKeyStorageProvider)
            {
                // Well-known CNG providers, keySpec is 0.
                return 0;
            }

            try
            {
                CngKeyOpenOptions options = machineKey ? CngKeyOpenOptions.MachineKey : CngKeyOpenOptions.None;

                using (CngKey.Open(keyName, provider, options))
                {
                    // It opened with keySpec 0, so use keySpec 0.
                    return 0;
                }
            }
            catch (CryptographicException)
            {
                // While NTE_BAD_KEYSET is what we generally expect here for RSA, on Windows 7
                // PROV_DSS produces NTE_BAD_PROV_TYPE, and PROV_DSS_DH produces NTE_NO_KEY.
                //
                // So we'll just try the CAPI fallback for any error code, and see what happens.

                CspParameters cspParameters = new CspParameters
                {
                    ProviderName = provider.Provider,
                    KeyContainerName = keyName,
                    Flags = CspProviderFlags.UseExistingKey,
                    KeyNumber = (int)KeyNumber.Signature,
                };

                if (machineKey)
                {
                    cspParameters.Flags |= CspProviderFlags.UseMachineKeyStore;
                }

                if (TryGuessKeySpec(cspParameters, algorithmGroup, out int keySpec))
                {
                    return keySpec;
                }

                throw;
            }
        }

        private static bool TryGuessKeySpec(
            CspParameters cspParameters,
            CngAlgorithmGroup? algorithmGroup,
            out int keySpec)
        {
            if (algorithmGroup == CngAlgorithmGroup.Rsa)
            {
                return TryGuessRsaKeySpec(cspParameters, out keySpec);
            }

            if (algorithmGroup == CngAlgorithmGroup.Dsa)
            {
                return TryGuessDsaKeySpec(cspParameters, out keySpec);
            }

            keySpec = 0;
            return false;
        }

        private static bool TryGuessRsaKeySpec(CspParameters cspParameters, out int keySpec)
        {
            // Try the AT_SIGNATURE spot in each of the 4 RSA provider type values,
            // ideally one of them will work.
            const int PROV_RSA_FULL = 1;
            const int PROV_RSA_SIG = 2;
            const int PROV_RSA_SCHANNEL = 12;
            const int PROV_RSA_AES = 24;

            // These are ordered in terms of perceived likeliness, given that the key
            // is AT_SIGNATURE.
            ReadOnlySpan<int> provTypes =
            [
                PROV_RSA_FULL,
                PROV_RSA_AES,
                PROV_RSA_SCHANNEL,

                // Nothing should be PROV_RSA_SIG, but if everything else has failed,
                // just try this last thing.
                PROV_RSA_SIG,
            ];

            foreach (int provType in provTypes)
            {
                cspParameters.ProviderType = provType;

                try
                {
                    using (new RSACryptoServiceProvider(cspParameters))
                    {
                        keySpec = cspParameters.KeyNumber;
                        return true;
                    }
                }
                catch (CryptographicException)
                {
                }
            }

            Debug.Fail("RSA key did not open with KeyNumber 0 or AT_SIGNATURE");
            keySpec = 0;
            return false;
        }

        private static bool TryGuessDsaKeySpec(CspParameters cspParameters, out int keySpec)
        {
            const int PROV_DSS = 3;
            const int PROV_DSS_DH = 13;

            ReadOnlySpan<int> provTypes =
            [
                PROV_DSS_DH,
                PROV_DSS,
            ];

            foreach (int provType in provTypes)
            {
                cspParameters.ProviderType = provType;

                try
                {
                    using (new DSACryptoServiceProvider(cspParameters))
                    {
                        keySpec = cspParameters.KeyNumber;
                        return true;
                    }
                }
                catch (CryptographicException)
                {
                }
            }

            Debug.Fail("DSA key did not open with KeyNumber 0 or AT_SIGNATURE");
            keySpec = 0;
            return false;
        }
    }
}
