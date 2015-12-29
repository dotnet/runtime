// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private const int ENUMERATE_SECURITY_PACKAGES_ID = 1;
        private const int SSPI_PACKAGE_NOT_FOUND_ID = 2;
        private const int ACQUIRE_DEFAULT_CREDENTIAL_ID = 3;
        private const int ACQUIRE_CREDENTIALS_HANDLE_ID = 4;
        private const int SECURE_CHANNEL_CTOR_ID = 5;
        private const int LOCATING_PRIVATE_KEY_ID = 6;
        private const int CERT_IS_TYPE_2_ID = 7;
        private const int FOUND_CERT_IN_STORE_ID = 8;
        private const int NOT_FOUND_CERT_IN_STORE_ID = 9;
        private const int INITIALIZE_SECURITY_CONTEXT_ID = 10;
        private const int SECURITY_CONTEXT_INPUT_BUFFER_ID = 11;
        private const int SECURITY_CONTEXT_INPUT_BUFFERS_ID = 12;
        private const int ACCEPT_SECURITY_CONTEXT_ID = 13;
        private const int OPERATION_RETURNED_SOMETHING_ID = 14;
        private const int REMOTE_CERTIFICATE_ID = 15;
        private const int CERTIFICATE_FROM_DELEGATE_ID = 16;
        private const int NO_DELEGATE_NO_CLIENT_CERT_ID = 17;
        private const int NO_DELEGATE_BUT_CLIENT_CERT_ID = 18;
        private const int ATTEMPTING_RESTART_USING_CERT_ID = 19;
        private const int NO_ISSUERS_TRY_ALL_CERTS_ID = 20;
        private const int LOOK_FOR_MATCHING_CERTS_ID = 21;
        private const int SELECTED_CERT_ID = 22;
        private const int CERTS_AFTER_FILTERING_ID = 23;
        private const int FINDING_MATCHING_CERTS_ID = 24;
        private const int USING_CACHED_CREDENTIAL_ID = 25;
        private const int SSPI_SELECTED_CIPHER_SUITE_ID = 26;
        private const int REMOTE_CERTIFICATE_ERROR_ID = 27;
        private const int REMOTE_CERTIFICATE_VALID_ID = 28;
        private const int REMOTE_CERTIFICATE_SUCCESS_ID = 29;
        private const int REMOTE_CERTIFICATE_INVALID_ID = 30;


        private readonly static SecurityEventSource s_log = new SecurityEventSource();
        private SecurityEventSource() { }
        public static SecurityEventSource Log
        {
            get
            {
                return s_log;
            }
        }

        [Event(ENUMERATE_SECURITY_PACKAGES_ID, Keywords = Keywords.Default,
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
            s_log.WriteEvent(ENUMERATE_SECURITY_PACKAGES_ID, arg1Str);
        }

        [Event(SSPI_PACKAGE_NOT_FOUND_ID, Keywords = Keywords.Default,
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
            s_log.WriteEvent(SSPI_PACKAGE_NOT_FOUND_ID, arg1Str);
        }

        [NonEvent]
        internal static void SecureChannelCtor(object secureChannel,
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

        [Event(SECURE_CHANNEL_CTOR_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void SecureChannelCtor(string hostname, int secureChannelHash, int clientCertificatesCount, EncryptionPolicy encryptionPolicy)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            fixed (char* arg1Ptr = hostname)
            {
                const int SIZEDATA = 4;
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];
                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (hostname.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(&secureChannelHash);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(&clientCertificatesCount);
                dataDesc[2].Size = sizeof(int);
                dataDesc[3].DataPointer = (IntPtr)(&encryptionPolicy);
                dataDesc[3].Size = sizeof(int);

                WriteEventCore(SECURE_CHANNEL_CTOR_ID, SIZEDATA, dataDesc);
            }
        }

        [Event(LOCATING_PRIVATE_KEY_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void LocatingPrivateKey(string x509Certificate, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(LOCATING_PRIVATE_KEY_ID, x509Certificate, secureChannelHash);
        }

        [Event(CERT_IS_TYPE_2_ID, Keywords = Keywords.Default,
    Level = EventLevel.Informational)]
        internal void CertIsType2(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(CERT_IS_TYPE_2_ID, secureChannelHash);
        }

        [Event(FOUND_CERT_IN_STORE_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void FoundCertInStore(string store, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(FOUND_CERT_IN_STORE_ID, store, secureChannelHash);
        }

        [Event(NOT_FOUND_CERT_IN_STORE_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void NotFoundCertInStore(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(NOT_FOUND_CERT_IN_STORE_ID, secureChannelHash);
        }

        [Event(REMOTE_CERTIFICATE_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void RemoteCertificate(string remoteCertificate)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(REMOTE_CERTIFICATE_ID, remoteCertificate);
        }


        [Event(CERTIFICATE_FROM_DELEGATE_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void CertificateFromDelegate(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(CERTIFICATE_FROM_DELEGATE_ID, secureChannelHash);
        }

        [Event(NO_DELEGATE_NO_CLIENT_CERT_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void NoDelegateNoClientCert(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(NO_DELEGATE_NO_CLIENT_CERT_ID, secureChannelHash);
        }

        [Event(NO_DELEGATE_BUT_CLIENT_CERT_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void NoDelegateButClientCert(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(NO_DELEGATE_BUT_CLIENT_CERT_ID, secureChannelHash);
        }

        [Event(ATTEMPTING_RESTART_USING_CERT_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void AttemptingRestartUsingCert(string clientCertificate, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(ATTEMPTING_RESTART_USING_CERT_ID, clientCertificate, secureChannelHash);
        }

        [Event(NO_ISSUERS_TRY_ALL_CERTS_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void NoIssuersTryAllCerts(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(NO_ISSUERS_TRY_ALL_CERTS_ID, secureChannelHash);
        }

        [Event(LOOK_FOR_MATCHING_CERTS_ID, Keywords = Keywords.Default,
Level = EventLevel.Informational)]
        internal void LookForMatchingCerts(int issuersCount, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(LOOK_FOR_MATCHING_CERTS_ID, issuersCount, secureChannelHash);
        }

        [Event(SELECTED_CERT_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void SelectedCert(string clientCertificate, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(SELECTED_CERT_ID, clientCertificate, secureChannelHash);
        }

        [Event(CERTS_AFTER_FILTERING_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void CertsAfterFiltering(int filteredCertsCount, int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(CERTS_AFTER_FILTERING_ID, filteredCertsCount, secureChannelHash);
        }

        [Event(FINDING_MATCHING_CERTS_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void FindingMatchingCerts(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(FINDING_MATCHING_CERTS_ID, secureChannelHash);
        }

        [Event(USING_CACHED_CREDENTIAL_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal void UsingCachedCredential(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(USING_CACHED_CREDENTIAL_ID, secureChannelHash);
        }

        [Event(SSPI_SELECTED_CIPHER_SUITE_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational)]
        internal unsafe void SspiSelectedCipherSuite(string process,
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
                const int SIZEDATA = 8;
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];
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
                WriteEventCore(SSPI_SELECTED_CIPHER_SUITE_ID, SIZEDATA, dataDesc);
            }
        }

        [Event(REMOTE_CERTIFICATE_ERROR_ID, Keywords = Keywords.Default,
            Level = EventLevel.Verbose)]
        internal void RemoteCertificateError(int secureChannelHash, string message)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(REMOTE_CERTIFICATE_ERROR_ID, secureChannelHash, message);
        }

        [Event(REMOTE_CERTIFICATE_VALID_ID, Keywords = Keywords.Default,
            Level = EventLevel.Verbose)]
        internal void RemoteCertDeclaredValid(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(REMOTE_CERTIFICATE_VALID_ID, secureChannelHash);
        }

        [Event(REMOTE_CERTIFICATE_SUCCESS_ID, Keywords = Keywords.Default,
            Level = EventLevel.Verbose)]
        internal void RemoteCertHasNoErrors(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(REMOTE_CERTIFICATE_SUCCESS_ID, secureChannelHash);
        }

        [Event(REMOTE_CERTIFICATE_INVALID_ID, Keywords = Keywords.Default,
            Level = EventLevel.Verbose)]
        internal void RemoteCertUserDeclaredInvalid(int secureChannelHash)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            WriteEvent(REMOTE_CERTIFICATE_INVALID_ID, secureChannelHash);
        }

        public class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }
    }
}
