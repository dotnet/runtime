// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using System.Globalization;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    //TODO: If localization resources are not found, logging does not work. Issue #5126.
    [EventSource(Name = "Microsoft-System-Net-Security", LocalizationResources = "FxResources.System.Net.Security.SR")]
    internal sealed partial class NetEventSource
    {
        private const int EnumerateSecurityPackagesId = NextAvailableEventId;
        private const int SspiPackageNotFoundId = EnumerateSecurityPackagesId + 1;
        private const int AcquireDefaultCredentialId = SspiPackageNotFoundId + 1;
        private const int AcquireCredentialsHandleId = AcquireDefaultCredentialId + 1;
        private const int SecureChannelCtorId = AcquireCredentialsHandleId + 1;
        private const int LocatingPrivateKeyId = SecureChannelCtorId + 1;
        private const int CertIsType2Id = LocatingPrivateKeyId + 1;
        private const int FoundCertInStoreId = CertIsType2Id + 1;
        private const int NotFoundCertInStoreId = FoundCertInStoreId + 1;
        private const int InitializeSecurityContextId = NotFoundCertInStoreId + 1;
        private const int SecurityContextInputBufferId = InitializeSecurityContextId + 1;
        private const int SecurityContextInputBuffersId = SecurityContextInputBufferId + 1;
        private const int AcceptSecuritContextId = SecurityContextInputBuffersId + 1;
        private const int OperationReturnedSomethingId = AcceptSecuritContextId + 1;
        private const int RemoteCertificateId = OperationReturnedSomethingId + 1;
        private const int CertificateFromDelegateId = RemoteCertificateId + 1;
        private const int NoDelegateNoClientCertId = CertificateFromDelegateId + 1;
        private const int NoDelegateButClientCertId = NoDelegateNoClientCertId + 1;
        private const int AttemptingRestartUsingCertId = NoDelegateButClientCertId + 1;
        private const int NoIssuersTryAllCertsId = AttemptingRestartUsingCertId + 1;
        private const int LookForMatchingCertsId = NoIssuersTryAllCertsId + 1;
        private const int SelectedCertId = LookForMatchingCertsId + 1;
        private const int CertsAfterFilteringId = SelectedCertId + 1;
        private const int FindingMatchingCertsId = CertsAfterFilteringId + 1;
        private const int UsingCachedCredentialId = FindingMatchingCertsId + 1;
        private const int SspiSelectedCipherSuitId = UsingCachedCredentialId + 1;
        private const int RemoteCertificateErrorId = SspiSelectedCipherSuitId + 1;
        private const int RemoteVertificateValidId = RemoteCertificateErrorId + 1;
        private const int RemoteCertificateSuccesId = RemoteVertificateValidId + 1;
        private const int RemoteCertificateInvalidId = RemoteCertificateSuccesId + 1;

        [Event(EnumerateSecurityPackagesId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void EnumerateSecurityPackages(string securityPackage)
        {
            if (IsEnabled())
            {
                WriteEvent(EnumerateSecurityPackagesId, securityPackage ?? "");
            }
        }

        [Event(SspiPackageNotFoundId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void SspiPackageNotFound(string packageName)
        {
            if (IsEnabled())
            {
                WriteEvent(SspiPackageNotFoundId, packageName ?? "");
            }
        }

        [NonEvent]
        public void SecureChannelCtor(SecureChannel secureChannel, string hostname, X509CertificateCollection clientCertificates, EncryptionPolicy encryptionPolicy)
        {
            if (IsEnabled())
            {
                SecureChannelCtor(hostname, GetHashCode(secureChannel), clientCertificates?.Count ?? 0, encryptionPolicy);
            }
        }
        [Event(SecureChannelCtorId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private unsafe void SecureChannelCtor(string hostname, int secureChannelHash, int clientCertificatesCount, EncryptionPolicy encryptionPolicy) =>
            WriteEvent(SecureChannelCtorId, hostname, secureChannelHash, clientCertificatesCount, (int)encryptionPolicy);

        [NonEvent]
        public void LocatingPrivateKey(X509Certificate x509Certificate, SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                LocatingPrivateKey(x509Certificate.ToString(true), GetHashCode(secureChannel));
            }
        }
        [Event(LocatingPrivateKeyId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void LocatingPrivateKey(string x509Certificate, int secureChannelHash) =>
            WriteEvent(LocatingPrivateKeyId, x509Certificate, secureChannelHash);

        [NonEvent]
        public void CertIsType2(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                CertIsType2(GetHashCode(secureChannel));
            }
        }
        [Event(CertIsType2Id, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void CertIsType2(int secureChannelHash) =>
            WriteEvent(CertIsType2Id, secureChannelHash);

        [NonEvent]
        public void FoundCertInStore(bool serverMode, SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                FoundCertInStore(serverMode ? "LocalMachine" : "CurrentUser", GetHashCode(secureChannel));
            }
        }
        [Event(FoundCertInStoreId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void FoundCertInStore(string store, int secureChannelHash) =>
            WriteEvent(FoundCertInStoreId, store, secureChannelHash);

        [NonEvent]
        public void NotFoundCertInStore(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                NotFoundCertInStore(GetHashCode(secureChannel));
            }
        }
        [Event(NotFoundCertInStoreId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NotFoundCertInStore(int secureChannelHash) =>
            WriteEvent(NotFoundCertInStoreId, secureChannelHash);

        [NonEvent]
        public void RemoteCertificate(X509Certificate remoteCertificate)
        {
            if (IsEnabled())
            {
                WriteEvent(RemoteCertificateId, remoteCertificate?.ToString(true));
            }
        }
        [Event(RemoteCertificateId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void RemoteCertificate(string remoteCertificate) =>
            WriteEvent(RemoteCertificateId, remoteCertificate);

        [NonEvent]
        public void CertificateFromDelegate(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                CertificateFromDelegate(GetHashCode(secureChannel));
            }
        }
        [Event(CertificateFromDelegateId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void CertificateFromDelegate(int secureChannelHash) =>
            WriteEvent(CertificateFromDelegateId, secureChannelHash);

        [NonEvent]
        public void NoDelegateNoClientCert(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                NoDelegateNoClientCert(GetHashCode(secureChannel));
            }
        }
        [Event(NoDelegateNoClientCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NoDelegateNoClientCert(int secureChannelHash) =>
            WriteEvent(NoDelegateNoClientCertId, secureChannelHash);

        [NonEvent]
        public void NoDelegateButClientCert(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                NoDelegateButClientCert(GetHashCode(secureChannel));
            }
        }
        [Event(NoDelegateButClientCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NoDelegateButClientCert(int secureChannelHash) =>
            WriteEvent(NoDelegateButClientCertId, secureChannelHash);

        [NonEvent]
        public void AttemptingRestartUsingCert(X509Certificate clientCertificate, SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                AttemptingRestartUsingCert(clientCertificate?.ToString(true), GetHashCode(secureChannel));
            }
        }
        [Event(AttemptingRestartUsingCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void AttemptingRestartUsingCert(string clientCertificate, int secureChannelHash) =>
            WriteEvent(AttemptingRestartUsingCertId, clientCertificate, secureChannelHash);

        [NonEvent]
        public void NoIssuersTryAllCerts(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                NoIssuersTryAllCerts(GetHashCode(secureChannel));
            }
        }
        [Event(NoIssuersTryAllCertsId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NoIssuersTryAllCerts(int secureChannelHash) =>
            WriteEvent(NoIssuersTryAllCertsId, secureChannelHash);

        [NonEvent]
        public void LookForMatchingCerts(int issuersCount, SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                LookForMatchingCerts(issuersCount, GetHashCode(secureChannel));
            }
        }
        [Event(LookForMatchingCertsId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void LookForMatchingCerts(int issuersCount, int secureChannelHash) =>
            WriteEvent(LookForMatchingCertsId, issuersCount, secureChannelHash);

        [NonEvent]
        public void SelectedCert(X509Certificate clientCertificate, SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                SelectedCert(clientCertificate?.ToString(true), GetHashCode(secureChannel));
            }
        }
        [Event(SelectedCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void SelectedCert(string clientCertificate, int secureChannelHash) =>
            WriteEvent(SelectedCertId, clientCertificate, secureChannelHash);

        [NonEvent]
        public void CertsAfterFiltering(int filteredCertsCount, SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                CertsAfterFiltering(filteredCertsCount, GetHashCode(secureChannel));
            }
        }
        [Event(CertsAfterFilteringId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void CertsAfterFiltering(int filteredCertsCount, int secureChannelHash) =>
            WriteEvent(CertsAfterFilteringId, filteredCertsCount, secureChannelHash);

        [NonEvent]
        public void FindingMatchingCerts(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                FindingMatchingCerts(GetHashCode(secureChannel));
            }
        }
        [Event(FindingMatchingCertsId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void FindingMatchingCerts(int secureChannelHash) =>
            WriteEvent(FindingMatchingCertsId, secureChannelHash);

        [NonEvent]
        public void UsingCachedCredential(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                WriteEvent(UsingCachedCredentialId, GetHashCode(secureChannel));
            }
        }
        [Event(UsingCachedCredentialId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void UsingCachedCredential(int secureChannelHash) =>
            WriteEvent(UsingCachedCredentialId, secureChannelHash);

        [Event(SspiSelectedCipherSuitId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public unsafe void SspiSelectedCipherSuite(
            string process,
            SslProtocols sslProtocol,
            CipherAlgorithmType cipherAlgorithm,
            int cipherStrength,
            HashAlgorithmType hashAlgorithm,
            int hashStrength,
            ExchangeAlgorithmType keyExchangeAlgorithm,
            int keyExchangeStrength)
        {
            if (IsEnabled())
            {
                WriteEvent(SspiSelectedCipherSuitId, 
                    process, (int)sslProtocol, (int)cipherAlgorithm, cipherStrength,
                    (int)hashAlgorithm, hashStrength, (int)keyExchangeAlgorithm, keyExchangeStrength);
            }
        }

        [NonEvent]
        public void RemoteCertificateError(SecureChannel secureChannel, string message)
        {
            if (IsEnabled())
            {
                RemoteCertificateError(GetHashCode(secureChannel), message);
            }
        }
        [Event(RemoteCertificateErrorId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertificateError(int secureChannelHash, string message) =>
            WriteEvent(RemoteCertificateErrorId, secureChannelHash, message);

        [NonEvent]
        public void RemoteCertDeclaredValid(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                RemoteCertDeclaredValid(GetHashCode(secureChannel));
            }
        }
        [Event(RemoteVertificateValidId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertDeclaredValid(int secureChannelHash) =>
            WriteEvent(RemoteVertificateValidId, secureChannelHash);

        [NonEvent]
        public void RemoteCertHasNoErrors(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                RemoteCertHasNoErrors(GetHashCode(secureChannel));
            }
        }
        [Event(RemoteCertificateSuccesId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertHasNoErrors(int secureChannelHash) =>
            WriteEvent(RemoteCertificateSuccesId, secureChannelHash);

        [NonEvent]
        public void RemoteCertUserDeclaredInvalid(SecureChannel secureChannel)
        {
            if (IsEnabled())
            {
                RemoteCertUserDeclaredInvalid(GetHashCode(secureChannel));
            }
        }
        [Event(RemoteCertificateInvalidId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertUserDeclaredInvalid(int secureChannelHash) =>
            WriteEvent(RemoteCertificateInvalidId, secureChannelHash);

        static partial void AdditionalCustomizedToString<T>(T value, ref string result)
        {
            X509Certificate cert = value as X509Certificate;
            if (cert != null)
            {
                result = cert.ToString(fVerbose: true);
            }
        }
    }
}
