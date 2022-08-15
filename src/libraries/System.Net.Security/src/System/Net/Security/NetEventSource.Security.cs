// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    [EventSource(Name = "Private.InternalDiagnostics.System.Net.Security", LocalizationResources = "FxResources.System.Net.Security.SR")]
    internal sealed partial class NetEventSource
    {
        private const int LocatingPrivateKeyId = NextAvailableEventId + 1;
        private const int CertIsType2Id = LocatingPrivateKeyId + 1;
        private const int FoundCertInStoreId = CertIsType2Id + 1;
        private const int NotFoundCertInStoreId = FoundCertInStoreId + 1;
        private const int RemoteCertificateId = NotFoundCertInStoreId + 1;
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
        private const int RemoteCertificateSuccessId = RemoteVertificateValidId + 1;
        private const int RemoteCertificateInvalidId = RemoteCertificateSuccessId + 1;
        private const int SslStreamCtorId = RemoteCertificateInvalidId + 1;
        private const int SentFrameId = SslStreamCtorId + 1;
        private const int ReceivedFrameId = SentFrameId + 1;

        [Event(EnumerateSecurityPackagesId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void EnumerateSecurityPackages(string? securityPackage)
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
        public void SslStreamCtor(SslStream sslStream, Stream innerStream)
        {
            if (IsEnabled())
            {
                string? localId = null;
                string? remoteId = null;

                NetworkStream? ns = innerStream as NetworkStream;
                if (ns != null)
                {
                    try
                    {
                        localId = ns.Socket.LocalEndPoint?.ToString();
                        remoteId = ns.Socket.RemoteEndPoint?.ToString();

                    }
                    catch { };
                }

                localId ??= IdOf(innerStream);

                SslStreamCtor(IdOf(sslStream), localId, remoteId);
            }
        }

        [Event(SslStreamCtorId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void SslStreamCtor(string thisOrContextObject, string? localId, string? remoteId) =>
              WriteEvent(SslStreamCtorId, thisOrContextObject, localId, remoteId);

        [NonEvent]
        public void LocatingPrivateKey(X509Certificate x509Certificate, object instance)
        {
            if (IsEnabled())
            {
                LocatingPrivateKey(x509Certificate.ToString(true), GetHashCode(instance));
            }
        }
        [Event(LocatingPrivateKeyId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void LocatingPrivateKey(string x509Certificate, int sslStreamHash) =>
            WriteEvent(LocatingPrivateKeyId, x509Certificate, sslStreamHash);

        [NonEvent]
        public void CertIsType2(object instance)
        {
            if (IsEnabled())
            {
                CertIsType2(GetHashCode(instance));
            }
        }
        [Event(CertIsType2Id, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void CertIsType2(int sslStreamHash) =>
            WriteEvent(CertIsType2Id, sslStreamHash);

        [NonEvent]
        public void FoundCertInStore(bool serverMode, object instance)
        {
            if (IsEnabled())
            {
                FoundCertInStore(serverMode ? "LocalMachine" : "CurrentUser", GetHashCode(instance));
            }
        }
        [Event(FoundCertInStoreId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void FoundCertInStore(string store, int sslStreamHash) =>
            WriteEvent(FoundCertInStoreId, store, sslStreamHash);

        [NonEvent]
        public void NotFoundCertInStore(object instance)
        {
            if (IsEnabled())
            {
                NotFoundCertInStore(GetHashCode(instance));
            }
        }
        [Event(NotFoundCertInStoreId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NotFoundCertInStore(int sslStreamHash) =>
            WriteEvent(NotFoundCertInStoreId, sslStreamHash);

        [NonEvent]
        public void RemoteCertificate(X509Certificate? remoteCertificate)
        {
            if (IsEnabled())
            {
                RemoteCertificate(remoteCertificate?.ToString(true));
            }
        }
        [Event(RemoteCertificateId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void RemoteCertificate(string? remoteCertificate) =>
            WriteEvent(RemoteCertificateId, remoteCertificate);

        [NonEvent]
        public void CertificateFromDelegate(SslStream SslStream)
        {
            if (IsEnabled())
            {
                CertificateFromDelegate(GetHashCode(SslStream));
            }
        }
        [Event(CertificateFromDelegateId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void CertificateFromDelegate(int sslStreamHash) =>
            WriteEvent(CertificateFromDelegateId, sslStreamHash);

        [NonEvent]
        public void NoDelegateNoClientCert(SslStream SslStream)
        {
            if (IsEnabled())
            {
                NoDelegateNoClientCert(GetHashCode(SslStream));
            }
        }
        [Event(NoDelegateNoClientCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NoDelegateNoClientCert(int sslStreamHash) =>
            WriteEvent(NoDelegateNoClientCertId, sslStreamHash);

        [NonEvent]
        public void NoDelegateButClientCert(SslStream SslStream)
        {
            if (IsEnabled())
            {
                NoDelegateButClientCert(GetHashCode(SslStream));
            }
        }
        [Event(NoDelegateButClientCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NoDelegateButClientCert(int sslStreamHash) =>
            WriteEvent(NoDelegateButClientCertId, sslStreamHash);

        [NonEvent]
        public void AttemptingRestartUsingCert(X509Certificate? clientCertificate, SslStream SslStream)
        {
            if (IsEnabled())
            {
                AttemptingRestartUsingCert(clientCertificate?.ToString(true), GetHashCode(SslStream));
            }
        }
        [Event(AttemptingRestartUsingCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void AttemptingRestartUsingCert(string? clientCertificate, int sslStreamHash) =>
            WriteEvent(AttemptingRestartUsingCertId, clientCertificate, sslStreamHash);

        [NonEvent]
        public void NoIssuersTryAllCerts(SslStream SslStream)
        {
            if (IsEnabled())
            {
                NoIssuersTryAllCerts(GetHashCode(SslStream));
            }
        }
        [Event(NoIssuersTryAllCertsId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NoIssuersTryAllCerts(int sslStreamHash) =>
            WriteEvent(NoIssuersTryAllCertsId, sslStreamHash);

        [NonEvent]
        public void LookForMatchingCerts(int issuersCount, SslStream SslStream)
        {
            if (IsEnabled())
            {
                LookForMatchingCerts(issuersCount, GetHashCode(SslStream));
            }
        }
        [Event(LookForMatchingCertsId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void LookForMatchingCerts(int issuersCount, int sslStreamHash) =>
            WriteEvent(LookForMatchingCertsId, issuersCount, sslStreamHash);

        [NonEvent]
        public void SelectedCert(X509Certificate clientCertificate, SslStream SslStream)
        {
            if (IsEnabled())
            {
                SelectedCert(clientCertificate?.ToString(true), GetHashCode(SslStream));
            }
        }
        [Event(SelectedCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void SelectedCert(string? clientCertificate, int sslStreamHash) =>
            WriteEvent(SelectedCertId, clientCertificate, sslStreamHash);

        [NonEvent]
        public void CertsAfterFiltering(int filteredCertsCount, SslStream SslStream)
        {
            if (IsEnabled())
            {
                CertsAfterFiltering(filteredCertsCount, GetHashCode(SslStream));
            }
        }
        [Event(CertsAfterFilteringId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void CertsAfterFiltering(int filteredCertsCount, int sslStreamHash) =>
            WriteEvent(CertsAfterFilteringId, filteredCertsCount, sslStreamHash);

        [NonEvent]
        public void FindingMatchingCerts(SslStream SslStream)
        {
            if (IsEnabled())
            {
                FindingMatchingCerts(GetHashCode(SslStream));
            }
        }
        [Event(FindingMatchingCertsId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void FindingMatchingCerts(int sslStreamHash) =>
            WriteEvent(FindingMatchingCertsId, sslStreamHash);

        [NonEvent]
        public void UsingCachedCredential(SslStream SslStream)
        {
            if (IsEnabled())
            {
                UsingCachedCredential(GetHashCode(SslStream));
            }
        }
        [Event(UsingCachedCredentialId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void UsingCachedCredential(int sslStreamHash) =>
            WriteEvent(UsingCachedCredentialId, sslStreamHash);

        [Event(SspiSelectedCipherSuitId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void SspiSelectedCipherSuite(
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
        public void RemoteCertificateError(SslStream SslStream, string message)
        {
            if (IsEnabled())
            {
                RemoteCertificateError(GetHashCode(SslStream), message);
            }
        }
        [Event(RemoteCertificateErrorId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertificateError(int sslStreamHash, string message) =>
            WriteEvent(RemoteCertificateErrorId, sslStreamHash, message);

        [NonEvent]
        public void RemoteCertDeclaredValid(SslStream SslStream)
        {
            if (IsEnabled())
            {
                RemoteCertDeclaredValid(GetHashCode(SslStream));
            }
        }
        [Event(RemoteVertificateValidId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertDeclaredValid(int sslStreamHash) =>
            WriteEvent(RemoteVertificateValidId, sslStreamHash);

        [NonEvent]
        public void RemoteCertHasNoErrors(SslStream SslStream)
        {
            if (IsEnabled())
            {
                RemoteCertHasNoErrors(GetHashCode(SslStream));
            }
        }
        [Event(RemoteCertificateSuccessId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertHasNoErrors(int sslStreamHash) =>
            WriteEvent(RemoteCertificateSuccessId, sslStreamHash);

        [NonEvent]
        public void RemoteCertUserDeclaredInvalid(SslStream SslStream)
        {
            if (IsEnabled())
            {
                RemoteCertUserDeclaredInvalid(GetHashCode(SslStream));
            }
        }
        [Event(RemoteCertificateInvalidId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertUserDeclaredInvalid(int sslStreamHash) =>
            WriteEvent(RemoteCertificateInvalidId, sslStreamHash);

        [NonEvent]
        public void SentFrame(SslStream sslStream, ReadOnlySpan<byte> frame)
        {
            if (IsEnabled())
            {
               TlsFrameHelper.TlsFrameInfo info = default;
               bool isComplete = TlsFrameHelper.TryGetFrameInfo(frame, ref info);
               SentFrame(IdOf(sslStream), info.ToString(), isComplete ? 1 : 0);
            }
        }
        [Event(SentFrameId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void SentFrame(string sslStream, string tlsFrame, int isComplete) =>
            WriteEvent(SentFrameId, sslStream, tlsFrame, isComplete);

        [NonEvent]
        public void ReceivedFrame(SslStream sslStream, TlsFrameHelper.TlsFrameInfo frameInfo)
        {
            if (IsEnabled())
            {
                ReceivedFrame(IdOf(sslStream), frameInfo.ToString(), 1);
            }
        }
        [NonEvent]
        public void ReceivedFrame(SslStream sslStream, ReadOnlySpan<byte> frame)
        {
            if (IsEnabled())
            {
                TlsFrameHelper.TlsFrameInfo info = default;
                bool isComplete = TlsFrameHelper.TryGetFrameInfo(frame, ref info);
                ReceivedFrame(IdOf(sslStream), info.ToString(), isComplete ? 1 : 0);
            }
        }
        [Event(ReceivedFrameId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void ReceivedFrame(string sslStream, string tlsFrame, int isComplete) =>
            WriteEvent(ReceivedFrameId, sslStream, tlsFrame, isComplete);

        static partial void AdditionalCustomizedToString<T>(T value, ref string? result)
        {
            X509Certificate? cert = value as X509Certificate;
            if (cert != null)
            {
                result = cert.ToString(fVerbose: true);
            }
        }
    }
}
