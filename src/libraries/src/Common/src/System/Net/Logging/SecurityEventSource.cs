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
    [EventSource(Name = "Microsoft-System-Net-Security",
        Guid = "066c0e27-a02d-5a98-9a4d-078cc3b1a896",
        LocalizationResources = "FxResources.System.Net.Security.SR")]
    internal sealed partial class SecurityEventSource : EventSource
    {
        private const int EnumerateSecurityPackagesId = 1;
        private const int SspiPackageNotFoundId = 2;
        private const int AcquireDefaultCredentialId = 3;
        private const int AcquireCredentialsHandleId = 4;
        private const int SecureChannelCtorId = 5;
        private const int LocatingPrivateKeyId = 6;
        private const int CertIsType2Id = 7;
        private const int FoundCertInStoreId = 8;
        private const int NotFoundCertInStoreId = 9;
        private const int InitializeSecurityContextId = 10;
        private const int SecurityContextInputBufferId = 11;
        private const int SecurityContextInputBuffersId = 12;
        private const int AcceptSecuritContextId = 13;
        private const int OperationReturnedSomethingId = 14;
        private const int RemoteCertificateId = 15;
        private const int CertificateFromDelegateId = 16;
        private const int NoDelegateNoClientCertId = 17;
        private const int NoDelegateButClientCertId = 18;
        private const int AttemptingRestartUsingCertId = 19;
        private const int NoIssuersTryAllCertsId = 20;
        private const int LookForMatchingCertsId = 21;
        private const int SelectedCertId = 22;
        private const int CertsAfterFilteringId = 23;
        private const int FindingMatchingCertsId = 24;
        private const int UsingCachedCredentialId = 25;
        private const int SspiSelectedCipherSuitId = 26;
        private const int RemoteCertificateErrorId = 27;
        private const int RemoteVertificateValidId = 28;
        private const int RemoteCertificateSuccesId = 29;
        private const int REmoteCertificateInvalidId = 30;

        private readonly static SecurityEventSource s_log = new SecurityEventSource();
        private SecurityEventSource() { }
        public static SecurityEventSource Log
        {
            get
            {
                return s_log;
            }
        }

        [Event(EnumerateSecurityPackagesId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void EnumerateSecurityPackages(string securityPackage)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            string arg1Str = "";
            if (securityPackage != null)
            {
                arg1Str = securityPackage;
            }

            s_log.WriteEvent(EnumerateSecurityPackagesId, arg1Str);
        }

        [Event(SspiPackageNotFoundId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void SspiPackageNotFound(string packageName)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            string arg1Str = "";
            if (packageName != null)
            {
                arg1Str = packageName;
            }

            s_log.WriteEvent(SspiPackageNotFoundId, arg1Str);
        }

        [NonEvent]
        internal static void SecureChannelCtor(
                                            object secureChannel,
                                            string hostname,
                                            X509CertificateCollection clientCertificates,
                                            EncryptionPolicy encryptionPolicy)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            int clientCertificatesCount = 0;
            string arg1Str = "";
            if (clientCertificates != null)
            {
                clientCertificatesCount = clientCertificates.Count;
            }

            if (hostname != null)
            {
                arg1Str = hostname;
            }

            s_log.SecureChannelCtor(arg1Str, LoggingHash.HashInt(secureChannel), clientCertificatesCount, encryptionPolicy);
        }

        [Event(SecureChannelCtorId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void SecureChannelCtor(string hostname, int secureChannelHash, int clientCertificatesCount, EncryptionPolicy encryptionPolicy)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            fixed (char* arg1Ptr = hostname)
            {
                const int SizeData = 4;
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (hostname.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(&secureChannelHash);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(&clientCertificatesCount);
                dataDesc[2].Size = sizeof(int);
                dataDesc[3].DataPointer = (IntPtr)(&encryptionPolicy);
                dataDesc[3].Size = sizeof(int);

                WriteEventCore(SecureChannelCtorId, SizeData, dataDesc);
            }
        }

        [Event(LocatingPrivateKeyId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void LocatingPrivateKey(string x509Certificate, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(LocatingPrivateKeyId, x509Certificate, secureChannelHash);
        }

        [Event(CertIsType2Id, Keywords = Keywords.Default,
    Level = EventLevel.Informational)]
        internal void CertIsType2(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(CertIsType2Id, secureChannelHash);
        }

        [Event(FoundCertInStoreId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void FoundCertInStore(string store, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(FoundCertInStoreId, store, secureChannelHash);
        }

        [Event(NotFoundCertInStoreId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void NotFoundCertInStore(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(NotFoundCertInStoreId, secureChannelHash);
        }

        [Event(RemoteCertificateId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void RemoteCertificate(string remoteCertificate)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(RemoteCertificateId, remoteCertificate);
        }


        [Event(CertificateFromDelegateId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void CertificateFromDelegate(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(CertificateFromDelegateId, secureChannelHash);
        }

        [Event(NoDelegateNoClientCertId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void NoDelegateNoClientCert(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(NoDelegateNoClientCertId, secureChannelHash);
        }

        [Event(NoDelegateButClientCertId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void NoDelegateButClientCert(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(NoDelegateButClientCertId, secureChannelHash);
        }

        [Event(AttemptingRestartUsingCertId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void AttemptingRestartUsingCert(string clientCertificate, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(AttemptingRestartUsingCertId, clientCertificate, secureChannelHash);
        }

        [Event(NoIssuersTryAllCertsId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void NoIssuersTryAllCerts(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(NoIssuersTryAllCertsId, secureChannelHash);
        }

        [Event(LookForMatchingCertsId, Keywords = Keywords.Default,
Level = EventLevel.Informational)]
        internal void LookForMatchingCerts(int issuersCount, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(LookForMatchingCertsId, issuersCount, secureChannelHash);
        }

        [Event(SelectedCertId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void SelectedCert(string clientCertificate, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(SelectedCertId, clientCertificate, secureChannelHash);
        }

        [Event(CertsAfterFilteringId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void CertsAfterFiltering(int filteredCertsCount, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(CertsAfterFilteringId, filteredCertsCount, secureChannelHash);
        }

        [Event(FindingMatchingCertsId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void FindingMatchingCerts(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(FindingMatchingCertsId, secureChannelHash);
        }

        [Event(UsingCachedCredentialId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void UsingCachedCredential(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(UsingCachedCredentialId, secureChannelHash);
        }

        [Event(SspiSelectedCipherSuitId, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void SspiSelectedCipherSuite(
                                                string process,
                                                SslProtocols sslProtocol,
                                                CipherAlgorithmType cipherAlgorithm,
                                                int cipherStrength,
                                                HashAlgorithmType hashAlgorithm,
                                                int hashStrength,
                                                ExchangeAlgorithmType keyExchangeAlgorithm,
                                                int keyExchangeStrength)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            fixed (char* arg1Ptr = process)
            {
                const int SizeData = 8;
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (process.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(&sslProtocol);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(&cipherAlgorithm);
                dataDesc[2].Size = sizeof(int);
                dataDesc[3].DataPointer = (IntPtr)(&cipherStrength);
                dataDesc[3].Size = sizeof(int);
                dataDesc[4].DataPointer = (IntPtr)(&hashAlgorithm);
                dataDesc[4].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(&hashStrength);
                dataDesc[2].Size = sizeof(int);
                dataDesc[3].DataPointer = (IntPtr)(&keyExchangeAlgorithm);
                dataDesc[3].Size = sizeof(int);
                dataDesc[4].DataPointer = (IntPtr)(&keyExchangeStrength);
                dataDesc[4].Size = sizeof(int);
                WriteEventCore(SspiSelectedCipherSuitId, SizeData, dataDesc);
            }
        }

        [Event(RemoteCertificateErrorId, Keywords = Keywords.Default,
            Level = EventLevel.Verbose)]
        internal void RemoteCertificateError(int secureChannelHash, string message)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(RemoteCertificateErrorId, secureChannelHash, message);
        }

        [Event(RemoteVertificateValidId, Keywords = Keywords.Default,
            Level = EventLevel.Verbose)]
        internal void RemoteCertDeclaredValid(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(RemoteVertificateValidId, secureChannelHash);
        }

        [Event(RemoteCertificateSuccesId, Keywords = Keywords.Default,
            Level = EventLevel.Verbose)]
        internal void RemoteCertHasNoErrors(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(RemoteCertificateSuccesId, secureChannelHash);
        }

        [Event(REmoteCertificateInvalidId, Keywords = Keywords.Default,
            Level = EventLevel.Verbose)]
        internal void RemoteCertUserDeclaredInvalid(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(REmoteCertificateInvalidId, secureChannelHash);
        }

        public class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }
    }
}
