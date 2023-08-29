// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    [EventSource(Name = "Private.InternalDiagnostics.System.Net.Security")]
    internal sealed partial class NetEventSource
    {
#if WINDOWS
        // More events are defined in NetEventSource.Security.Windows.cs
        private const int LocatingPrivateKeyId = OperationReturnedSomethingId + 1;
#else
        private const int LocatingPrivateKeyId = NextAvailableEventId;
#endif
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
        private const int CertificateFromCertContextId = ReceivedFrameId + 1;

        [NonEvent]
        public void SslStreamCtor(SslStream sslStream, Stream innerStream)
        {
            string? localId = null;
            string? remoteId = null;

            if (innerStream is NetworkStream ns)
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

        [Event(SslStreamCtorId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void SslStreamCtor(string thisOrContextObject, string? localId, string? remoteId) =>
              WriteEvent(SslStreamCtorId, thisOrContextObject, localId, remoteId);

        [NonEvent]
        public void LocatingPrivateKey(X509Certificate x509Certificate, object instance) =>
            LocatingPrivateKey(x509Certificate.ToString(fVerbose: true), GetHashCode(instance));

        [Event(LocatingPrivateKeyId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void LocatingPrivateKey(string x509Certificate, int sslStreamHash) =>
            WriteEvent(LocatingPrivateKeyId, x509Certificate, sslStreamHash);

        [NonEvent]
        public void CertIsType2(object instance) =>
            CertIsType2(GetHashCode(instance));

        [Event(CertIsType2Id, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void CertIsType2(int sslStreamHash) =>
            WriteEvent(CertIsType2Id, sslStreamHash);

        [NonEvent]
        public void FoundCertInStore(bool serverMode, object instance) =>
            FoundCertInStore(serverMode ? "LocalMachine" : "CurrentUser", GetHashCode(instance));

        [Event(FoundCertInStoreId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void FoundCertInStore(string store, int sslStreamHash) =>
            WriteEvent(FoundCertInStoreId, store, sslStreamHash);

        [NonEvent]
        public void NotFoundCertInStore(object instance) =>
            NotFoundCertInStore(GetHashCode(instance));

        [Event(NotFoundCertInStoreId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NotFoundCertInStore(int sslStreamHash) =>
            WriteEvent(NotFoundCertInStoreId, sslStreamHash);

        [NonEvent]
        public void RemoteCertificate(X509Certificate? remoteCertificate) =>
            RemoteCertificate(remoteCertificate?.ToString(fVerbose: true));

        [Event(RemoteCertificateId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void RemoteCertificate(string? remoteCertificate) =>
            WriteEvent(RemoteCertificateId, remoteCertificate);

        [NonEvent]
        public void CertificateFromDelegate(SslStream SslStream) =>
            CertificateFromDelegate(GetHashCode(SslStream));

        [Event(CertificateFromDelegateId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void CertificateFromDelegate(int sslStreamHash) =>
            WriteEvent(CertificateFromDelegateId, sslStreamHash);

        [NonEvent]
        public void NoDelegateNoClientCert(SslStream SslStream) =>
            NoDelegateNoClientCert(GetHashCode(SslStream));

        [Event(NoDelegateNoClientCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NoDelegateNoClientCert(int sslStreamHash) =>
            WriteEvent(NoDelegateNoClientCertId, sslStreamHash);

        [NonEvent]
        public void NoDelegateButClientCert(SslStream SslStream) =>
            NoDelegateButClientCert(GetHashCode(SslStream));

        [Event(NoDelegateButClientCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NoDelegateButClientCert(int sslStreamHash) =>
            WriteEvent(NoDelegateButClientCertId, sslStreamHash);

        [NonEvent]
        public void AttemptingRestartUsingCert(X509Certificate? clientCertificate, SslStream SslStream) =>
            AttemptingRestartUsingCert(clientCertificate?.ToString(fVerbose: true), GetHashCode(SslStream));

        [Event(AttemptingRestartUsingCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void AttemptingRestartUsingCert(string? clientCertificate, int sslStreamHash) =>
            WriteEvent(AttemptingRestartUsingCertId, clientCertificate, sslStreamHash);

        [NonEvent]
        public void NoIssuersTryAllCerts(SslStream SslStream) =>
            NoIssuersTryAllCerts(GetHashCode(SslStream));

        [Event(NoIssuersTryAllCertsId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void NoIssuersTryAllCerts(int sslStreamHash) =>
            WriteEvent(NoIssuersTryAllCertsId, sslStreamHash);

        [NonEvent]
        public void LookForMatchingCerts(int issuersCount, SslStream SslStream) =>
            LookForMatchingCerts(issuersCount, GetHashCode(SslStream));

        [Event(LookForMatchingCertsId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void LookForMatchingCerts(int issuersCount, int sslStreamHash) =>
            WriteEvent(LookForMatchingCertsId, issuersCount, sslStreamHash);

        [NonEvent]
        public void SelectedCert(X509Certificate clientCertificate, SslStream SslStream) =>
            SelectedCert(clientCertificate?.ToString(fVerbose: true), GetHashCode(SslStream));

        [Event(SelectedCertId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void SelectedCert(string? clientCertificate, int sslStreamHash) =>
            WriteEvent(SelectedCertId, clientCertificate, sslStreamHash);

        [NonEvent]
        public void CertsAfterFiltering(int filteredCertsCount, SslStream SslStream) =>
            CertsAfterFiltering(filteredCertsCount, GetHashCode(SslStream));

        [Event(CertsAfterFilteringId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void CertsAfterFiltering(int filteredCertsCount, int sslStreamHash) =>
            WriteEvent(CertsAfterFilteringId, filteredCertsCount, sslStreamHash);

        [NonEvent]
        public void FindingMatchingCerts(SslStream SslStream) =>
            FindingMatchingCerts(GetHashCode(SslStream));

        [Event(FindingMatchingCertsId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void FindingMatchingCerts(int sslStreamHash) =>
            WriteEvent(FindingMatchingCertsId, sslStreamHash);

        [NonEvent]
        public void UsingCachedCredential(SslStream SslStream) =>
            UsingCachedCredential(GetHashCode(SslStream));

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
            WriteEvent(SspiSelectedCipherSuitId,
                process, (int)sslProtocol, (int)cipherAlgorithm, cipherStrength,
                (int)hashAlgorithm, hashStrength, (int)keyExchangeAlgorithm, keyExchangeStrength);
        }

        [NonEvent]
        public void RemoteCertificateError(SslStream SslStream, string message) =>
            RemoteCertificateError(GetHashCode(SslStream), message);

        [Event(RemoteCertificateErrorId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertificateError(int sslStreamHash, string message) =>
            WriteEvent(RemoteCertificateErrorId, sslStreamHash, message);

        [NonEvent]
        public void RemoteCertDeclaredValid(SslStream SslStream) =>
            RemoteCertDeclaredValid(GetHashCode(SslStream));

        [Event(RemoteVertificateValidId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertDeclaredValid(int sslStreamHash) =>
            WriteEvent(RemoteVertificateValidId, sslStreamHash);

        [NonEvent]
        public void RemoteCertHasNoErrors(SslStream SslStream) =>
            RemoteCertHasNoErrors(GetHashCode(SslStream));

        [Event(RemoteCertificateSuccessId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertHasNoErrors(int sslStreamHash) =>
            WriteEvent(RemoteCertificateSuccessId, sslStreamHash);

        [NonEvent]
        public void RemoteCertUserDeclaredInvalid(SslStream SslStream) =>
            RemoteCertUserDeclaredInvalid(GetHashCode(SslStream));

        [Event(RemoteCertificateInvalidId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void RemoteCertUserDeclaredInvalid(int sslStreamHash) =>
            WriteEvent(RemoteCertificateInvalidId, sslStreamHash);

        [NonEvent]
        public void SentFrame(SslStream sslStream, ReadOnlySpan<byte> frame)
        {
            TlsFrameHelper.TlsFrameInfo info = default;
            bool isComplete = TlsFrameHelper.TryGetFrameInfo(frame, ref info);
            SentFrame(IdOf(sslStream), info.ToString(), isComplete ? 1 : 0);
        }

        [Event(SentFrameId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void SentFrame(string sslStream, string tlsFrame, int isComplete) =>
            WriteEvent(SentFrameId, sslStream, tlsFrame, isComplete);

        [NonEvent]
        public void ReceivedFrame(SslStream sslStream, TlsFrameHelper.TlsFrameInfo frameInfo) =>
            ReceivedFrame(IdOf(sslStream), frameInfo.ToString(), 1);

        [NonEvent]
        public void ReceivedFrame(SslStream sslStream, ReadOnlySpan<byte> frame)
        {
            TlsFrameHelper.TlsFrameInfo info = default;
            bool isComplete = TlsFrameHelper.TryGetFrameInfo(frame, ref info);
            ReceivedFrame(IdOf(sslStream), info.ToString(), isComplete ? 1 : 0);
        }

        [Event(ReceivedFrameId, Keywords = Keywords.Default, Level = EventLevel.Verbose)]
        private void ReceivedFrame(string sslStream, string tlsFrame, int isComplete) =>
            WriteEvent(ReceivedFrameId, sslStream, tlsFrame, isComplete);

        [NonEvent]
        public void CertificateFromCertContext(SslStream sslStream) =>
            CertificateFromCertContext(GetHashCode(sslStream));

        [Event(CertificateFromCertContextId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void CertificateFromCertContext(int sslStreamHash) =>
            WriteEvent(CertificateFromCertContextId, sslStreamHash);

        static partial void AdditionalCustomizedToString(object value, ref string? result)
        {
            if (value is X509Certificate cert)
            {
                result = cert.ToString(fVerbose: true);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, string arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8)
        {
            arg1 ??= "";

            fixed (char* arg1Ptr = arg1)
            {
                const int NumEventDatas = 8;
                EventData* descrs = stackalloc EventData[NumEventDatas];

                descrs[0] = new EventData
                {
                    DataPointer = (IntPtr)(arg1Ptr),
                    Size = (arg1.Length + 1) * sizeof(char)
                };
                descrs[1] = new EventData
                {
                    DataPointer = (IntPtr)(&arg2),
                    Size = sizeof(int)
                };
                descrs[2] = new EventData
                {
                    DataPointer = (IntPtr)(&arg3),
                    Size = sizeof(int)
                };
                descrs[3] = new EventData
                {
                    DataPointer = (IntPtr)(&arg4),
                    Size = sizeof(int)
                };
                descrs[4] = new EventData
                {
                    DataPointer = (IntPtr)(&arg5),
                    Size = sizeof(int)
                };
                descrs[5] = new EventData
                {
                    DataPointer = (IntPtr)(&arg6),
                    Size = sizeof(int)
                };
                descrs[6] = new EventData
                {
                    DataPointer = (IntPtr)(&arg7),
                    Size = sizeof(int)
                };
                descrs[7] = new EventData
                {
                    DataPointer = (IntPtr)(&arg8),
                    Size = sizeof(int)
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }
    }
}
