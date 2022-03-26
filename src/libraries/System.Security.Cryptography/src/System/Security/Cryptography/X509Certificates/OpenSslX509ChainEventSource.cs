// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    [EventSource(Name = "System.Security.Cryptography.X509Certificates.X509Chain.OpenSsl")]
    internal sealed class OpenSslX509ChainEventSource : EventSource
    {
        internal static readonly OpenSslX509ChainEventSource Log = new OpenSslX509ChainEventSource();

        private const int EventId_ChainStart = 1;
        private const int EventId_ChainStop = 2;
        private const int EventId_FlushStores = 3;
        private const int EventId_FindFirstChainFinished = 4;
        private const int EventId_FindChainViaAiaFinished = 5;
        private const int EventId_AiaDisabled = 6;
        private const int EventId_NoAiaFound = 7;
        private const int EventId_InvalidAia = 8;
        private const int EventId_NonHttpAiaEntry = 9;
        private const int EventId_AssetDownloadStart = 10;
        private const int EventId_AssetDownloadStop = 11;
        private const int EventId_HttpClientNotAvailable = 12;
        private const int EventId_DownloadTimeExceeded = 13;
        private const int EventId_InvalidDownloadedCertificate = 14;
        private const int EventId_InvalidDownloadedCrl = 15;
        private const int EventId_InvalidDownloadedOcsp = 16;
        private const int EventId_DownloadCompleteStatusCode = 17;
        private const int EventId_DownloadRedirected = 18;
        private const int EventId_DownloadRedirectsExceeded = 19;
        private const int EventId_DownloadRedirectNotFollowed = 20;
        private const int EventId_UntrustedChainWithRevocation = 21;
        private const int EventId_NonHttpCdpEntry = 22;
        private const int EventId_NoCdpFound = 23;
        private const int EventId_NoMatchingAiaEntry = 24;
        private const int EventId_NoMatchingCdpEntry = 25;
        private const int EventId_CrlCacheCheckStart = 26;
        private const int EventId_CrlCacheCheckStop = 27;
        private const int EventId_CrlCacheOpenError = 28;
        private const int EventId_CrlCacheDecodeError = 29;
        private const int EventId_CrlCacheExpired = 30;
        private const int EventId_CrlCacheFileBasedExpiry = 31;
        private const int EventId_CrlCacheAcceptedFile = 32;
        private const int EventId_CrlCacheWriteFailed = 33;
        private const int EventId_CrlCacheWriteSucceeded = 34;
        private const int EventId_CrlCheckOffline = 35;
        private const int EventId_CrlChainFinished = 36;
        private const int EventId_AllRevocationErrorsIgnored = 37;
        private const int EventId_OcspResponseFromCache = 38;
        private const int EventId_OcspResponseFromDownload = 39;
        private const int EventId_RawElementStatus = 40;
        private const int EventId_FinalElementStatus = 41;
        private const int EventId_CouldNotOpenCAStore = 42;
        private const int EventId_CachingIntermediate = 43;
        private const int EventId_CachingIntermediateFailed = 44;
        private const int EventId_RevocationCheckStart = 45;
        private const int EventId_RevocationCheckStop = 46;
        private const int EventId_CrlIdentifiersDetermined = 47;

        private static string GetCertificateSubject(SafeX509Handle certHandle)
        {
            bool addedRef = false;

            try
            {
                // Ensure that certHandle stays alive while we use an interior pointer.
                certHandle.DangerousAddRef(ref addedRef);
                X500DistinguishedName dn = Interop.Crypto.LoadX500Name(Interop.Crypto.X509GetSubjectName(certHandle));
                return dn.Name;
            }
            finally
            {
                if (addedRef)
                {
                    certHandle.DangerousRelease();
                }
            }
        }

        internal bool ShouldLogElementStatuses()
        {
            return IsEnabled(EventLevel.Verbose, EventKeywords.None);
        }

        [Event(
            EventId_ChainStart,
            Message = "Starting X.509 chain build.",
            Opcode = EventOpcode.Start,
            Level = EventLevel.Informational)]
        internal void ChainStart()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_ChainStart);
            }
        }

        [Event(
            EventId_ChainStop,
            Opcode = EventOpcode.Stop,
            Level = EventLevel.Informational)]
        internal void ChainStop()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_ChainStop);
            }
        }

        [Event(EventId_FlushStores, Level = EventLevel.Informational, Message = "Manual store flush triggered.")]
        internal void FlushStores()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_FlushStores);
            }
        }

        [NonEvent]
        internal void FindFirstChainFinished(Interop.Crypto.X509VerifyStatusCode code)
        {
            if (IsEnabled())
            {
                FindFirstChainFinished(code.Code);
            }
        }

        [Event(
            EventId_FindFirstChainFinished,
            Level = EventLevel.Verbose,
            Message = "First build finished with status {0}.")]
        private void FindFirstChainFinished(int code)
        {
            WriteEvent(EventId_FindFirstChainFinished, code);
        }

        [NonEvent]
        internal void FindChainViaAiaFinished(Interop.Crypto.X509VerifyStatusCode code, int downloadCount)
        {
            if (IsEnabled())
            {
                FindChainViaAiaFinished(code.Code, downloadCount);
            }
        }

        [Event(
            EventId_FindChainViaAiaFinished,
            Level = EventLevel.Verbose,
            Message = "AIA-based build retrieved {1} certificate(s) and finished with status {0}.")]
        private void FindChainViaAiaFinished(int code, int downloadCount)
        {
            WriteEvent(EventId_FindChainViaAiaFinished, code, downloadCount);
        }

        [Event(
            EventId_AiaDisabled,
            Level = EventLevel.Informational,
            Message = "The chain is incomplete, but AIA is disabled.")]
        internal void AiaDisabled()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_AiaDisabled);
            }
        }

        [NonEvent]
        internal void NoAiaFound(SafeX509Handle cert)
        {
            if (IsEnabled())
            {
                NoAiaFound(GetCertificateSubject(cert));
            }
        }

        [Event(
            EventId_NoAiaFound,
            Level = EventLevel.Informational,
            Message = "Certificate '{0}' does not have an AuthorityInformationAccess extension.")]
        private void NoAiaFound(string subjectName)
        {
            WriteEvent(EventId_NoAiaFound, subjectName);
        }

        [Event(
            EventId_InvalidAia,
            Level = EventLevel.Informational,
            Message = "The AuthorityInformationAccess extension could not be read.")]
        internal void InvalidAia()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_InvalidAia);
            }
        }

        [Event(
            EventId_NonHttpAiaEntry,
            Level = EventLevel.Verbose,
            Message = "Skipping AIA entry '{0}' because the protocol is not HTTP.")]
        internal void NonHttpAiaEntry(string uri)
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_NonHttpAiaEntry, uri);
            }
        }

        [NonEvent]
        internal void AssetDownloadStart(long timeoutMs, string uri)
        {
            if (IsEnabled())
            {
                AssetDownloadStart(timeoutMs > int.MaxValue ? -1 : (int)timeoutMs, uri);
            }
        }

        [Event(
            EventId_AssetDownloadStart,
            Message = "Starting download of certificate asset uri '{1}' with a {0}ms timeout.",
            Opcode = EventOpcode.Start,
            Level = EventLevel.Informational)]
        private void AssetDownloadStart(int timeoutMs, string uri)
        {
            WriteEvent(EventId_AssetDownloadStart, timeoutMs, uri);
        }

        [Event(
            EventId_AssetDownloadStop,
            Opcode = EventOpcode.Stop,
            Level = EventLevel.Informational,
            Message = "Asset download finished with a {0}-byte response.")]
        internal void AssetDownloadStop(int downloadSize)
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_AssetDownloadStop, downloadSize);
            }
        }

        [Event(EventId_HttpClientNotAvailable, Level = EventLevel.Error, Message = "HttpClient is not available.")]
        internal void HttpClientNotAvailable()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_HttpClientNotAvailable);
            }
        }

        [Event(
            EventId_DownloadTimeExceeded,
            Level = EventLevel.Informational,
            Message = "Not attempting further downloads as the available download time has been exceeded.")]
        internal void DownloadTimeExceeded()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_DownloadTimeExceeded);
            }
        }

        [Event(
            EventId_InvalidDownloadedCertificate,
            Level = EventLevel.Informational,
            Message = "The downloaded asset did not successfully decode as an X.509 certificate.")]
        internal void InvalidDownloadedCertificate()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_InvalidDownloadedCertificate);
            }
        }

        [Event(
            EventId_InvalidDownloadedCrl,
            Level = EventLevel.Informational,
            Message = "The downloaded asset did not successfully decode as an X.509 CRL.")]
        internal void InvalidDownloadedCrl()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_InvalidDownloadedCrl);
            }
        }

        [Event(
            EventId_InvalidDownloadedOcsp,
            Level = EventLevel.Informational,
            Message = "The downloaded asset did not successfully decode as an OCSP Response.")]
        internal void InvalidDownloadedOcsp()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_InvalidDownloadedOcsp);
            }
        }

        [Event(
            EventId_DownloadCompleteStatusCode,
            Level = EventLevel.Verbose,
            Message = "The download completed with status code '{0}'.")]
        private void DownloadCompleteStatusCode(int statusCode)
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_DownloadCompleteStatusCode, statusCode);
            }
        }

        [NonEvent]
        internal void DownloadRedirected(Uri redirectUri)
        {
            if (IsEnabled())
            {
                DownloadRedirected(redirectUri.ToString());
            }
        }

        [Event(
            EventId_DownloadRedirected,
            Level = EventLevel.Informational,
            Message = "Following redirect to new URL: {0}")]
        private void DownloadRedirected(string redirectUri)
        {
            WriteEvent(EventId_DownloadRedirected, redirectUri);
        }

        [Event(
            EventId_DownloadRedirectsExceeded,
            Level = EventLevel.Informational,
            Message = "Redirect limit exceeded, aborting download.")]
        internal void DownloadRedirectsExceeded()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_DownloadRedirectsExceeded);
            }
        }

        [NonEvent]
        internal void DownloadRedirectNotFollowed(Uri redirectUri)
        {
            if (IsEnabled())
            {
                DownloadRedirectNotFollowed(redirectUri.ToString());
            }
        }

        [Event(
            EventId_DownloadRedirectNotFollowed,
            Level = EventLevel.Informational,
            Message = "Not following redirect because the scheme is not supported: {0}")]
        private void DownloadRedirectNotFollowed(string redirectUri)
        {
            WriteEvent(EventId_DownloadRedirectNotFollowed, redirectUri);
        }

        [Event(
            EventId_UntrustedChainWithRevocation,
            Level = EventLevel.Informational,
            Message = "The certificate chain is untrusted, marking revocation status as Unknown.")]
        internal void UntrustedChainWithRevocation()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_UntrustedChainWithRevocation);
            }
        }

        [Event(
            EventId_NonHttpCdpEntry,
            Level = EventLevel.Verbose,
            Message = "Skipping CDP entry '{0}' because the protocol is not HTTP.")]
        internal void NonHttpCdpEntry(string uri)
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_NonHttpCdpEntry, uri);
            }
        }

        [NonEvent]
        internal void NoCdpFound(SafeX509Handle cert)
        {
            if (IsEnabled())
            {
                NoCdpFound(GetCertificateSubject(cert));
            }
        }

        [Event(
            EventId_NoCdpFound,
            Level = EventLevel.Informational,
            Message = "Certificate '{0}' does not have a CRL Distribution Point extension.")]
        private void NoCdpFound(string subjectName)
        {
            WriteEvent(EventId_NoCdpFound, subjectName);
        }

        [Event(
            EventId_NoMatchingAiaEntry,
            Level = EventLevel.Informational,
            Message = "The certificate has an Authority Information Access extension, but no appropriate entry for record type '{0}'")]
        internal void NoMatchingAiaEntry(string recordTypeOid)
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_NoMatchingAiaEntry, recordTypeOid);
            }
        }

        [Event(
            EventId_NoMatchingCdpEntry,
            Level = EventLevel.Informational,
            Message = "The certificate has a CRL Distribution Point extension, but the extension has no appropriate entries.")]
        internal void NoMatchingCdpEntry()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_NoMatchingCdpEntry);
            }
        }

        [Event(
            EventId_CrlCacheCheckStart,
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Message = "Checking for a cached CRL.")]
        internal void CrlCacheCheckStart()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CrlCacheCheckStart);
            }
        }

        [Event(
            EventId_CrlCacheCheckStop,
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop)]
        internal void CrlCacheCheckStop()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CrlCacheCheckStop);
            }
        }

        [Event(
            EventId_CrlCacheOpenError,
            Level = EventLevel.Verbose,
            Message = "Could not open the CRL cache file, it likely does not exist.")]
        internal void CrlCacheOpenError()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CrlCacheOpenError);
            }
        }

        [Event(
            EventId_CrlCacheDecodeError,
            Level = EventLevel.Warning,
            Message = "The CRL cache file did not successfully decode.")]
        internal void CrlCacheDecodeError()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CrlCacheDecodeError);
            }
        }

        [Event(
            EventId_CrlCacheExpired,
            Level = EventLevel.Verbose,
            Message = "The cached CRL's nextUpdate value ({1:O}) is not after the verification time ({0:O}).")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "verificationTime and nextUpdate are DateTime values, which are trimmer safe")]
        internal void CrlCacheExpired(DateTime verificationTime, DateTime nextUpdate)
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CrlCacheExpired, verificationTime, nextUpdate);
            }
        }

        [Event(
            EventId_CrlCacheFileBasedExpiry,
            Level = EventLevel.Verbose,
            Message = "The cached crl has no nextUpdate value, basing nextUpdate on the file write time.")]
        internal void CrlCacheFileBasedExpiry()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CrlCacheFileBasedExpiry);
            }
        }

        [Event(
            EventId_CrlCacheAcceptedFile,
            Level = EventLevel.Verbose,
            Message = "The cached crl nextUpdate value ({0:O}) is acceptable, using the cached file.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "nextUpdate is a DateTime value, which is trimmer safe")]
        internal void CrlCacheAcceptedFile(DateTime nextUpdate)
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CrlCacheAcceptedFile, nextUpdate);
            }
        }

        [Event(
            EventId_CrlCacheWriteFailed,
            Level = EventLevel.Informational,
            Message = "Failed to write the downloaded CRL to the cache at path '{0}'. Further use of this CRL will repeat the download.")]
        internal void CrlCacheWriteFailed(string cacheFile)
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CrlCacheWriteFailed, cacheFile);
            }
        }

        [Event(
            EventId_CrlCacheWriteSucceeded,
            Level = EventLevel.Verbose,
            Message = "The downloaded CRL was successfully written to the cache.")]
        internal void CrlCacheWriteSucceeded()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CrlCacheWriteSucceeded);
            }
        }

        [Event(
            EventId_CrlCheckOffline,
            Level = EventLevel.Verbose,
            Message = "Skipping CRL download because the revocation mode is Offline.")]
        internal void CrlCheckOffline()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CrlCheckOffline);
            }
        }

        [NonEvent]
        internal void CrlChainFinished(Interop.Crypto.X509VerifyStatusCode code)
        {
            if (IsEnabled())
            {
                CrlChainFinished(code.Code);
            }
        }

        [Event(
            EventId_CrlChainFinished,
            Level = EventLevel.Verbose,
            Message = "With CRLs applied, the chain build finished with status {0}.")]
        private void CrlChainFinished(int code)
        {
            WriteEvent(EventId_CrlChainFinished, code);
        }

        [Event(
            EventId_AllRevocationErrorsIgnored,
            Level = EventLevel.Verbose,
            Message = "The chain error details only included ignored status codes.")]
        internal void AllRevocationErrorsIgnored()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_AllRevocationErrorsIgnored);
            }
        }

        [NonEvent]
        internal void OcspResponseFromCache(int chainDepth, Interop.Crypto.X509VerifyStatusCode code)
        {
            if (IsEnabled())
            {
                OcspResponseFromCache(chainDepth, code.Code);
            }
        }

        [Event(
            EventId_OcspResponseFromCache,
            Level = EventLevel.Verbose,
            Message = "The OCSP cache result for the certificate at depth {0} is {1}.")]
        private void OcspResponseFromCache(int chainDepth, int code)
        {
            WriteEvent(EventId_OcspResponseFromCache, chainDepth, code);
        }

        [NonEvent]
        internal void OcspResponseFromDownload(int chainDepth, Interop.Crypto.X509VerifyStatusCode code)
        {
            if (IsEnabled())
            {
                OcspResponseFromDownload(chainDepth, code.Code);
            }
        }

        [Event(
            EventId_OcspResponseFromDownload,
            Level = EventLevel.Verbose,
            Message = "The OCSP retrieval result for the certificate at depth {0} is {1}.")]
        private void OcspResponseFromDownload(int chainDepth, int code)
        {
            WriteEvent(EventId_OcspResponseFromDownload, chainDepth, code);
        }

        [NonEvent]
        internal void RawElementStatus(int chainDepth, object errorCollection, Func<object, string> toString)
        {
            if (ShouldLogElementStatuses())
            {
                string statusCodes = toString(errorCollection);
                RawElementStatus(chainDepth, statusCodes);
            }
        }

        [Event(
            EventId_RawElementStatus,
            Level = EventLevel.Verbose,
            Message = "The reported errors for the chain element at depth {0} are {1}.")]
        private void RawElementStatus(int chainDepth, string statusCodes)
        {
            WriteEvent(EventId_RawElementStatus, chainDepth, statusCodes);
        }

        [NonEvent]
        internal void FinalElementStatus(int chainDepth, object errorCollection, Func<object, string> toString)
        {
            if (ShouldLogElementStatuses())
            {
                string statusCodes = toString(errorCollection);
                FinalElementStatus(chainDepth, statusCodes);
            }
        }

        [Event(
            EventId_FinalElementStatus,
            Level = EventLevel.Verbose,
            Message = "After OCSP and code normalization, the errors for the chain element at depth {0} are {1}.")]
        private void FinalElementStatus(int chainDepth, string statusCodes)
        {
            WriteEvent(EventId_FinalElementStatus, chainDepth, statusCodes);
        }

        [Event(
            EventId_CouldNotOpenCAStore,
            Level = EventLevel.Warning,
            Message = "Not caching downloaded intermediate certificates because the CurrentUser\\CA store failed to open.")]
        internal void CouldNotOpenCAStore()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_CouldNotOpenCAStore);
            }
        }

        [NonEvent]
        internal void CachingIntermediate(X509Certificate2 certificate)
        {
            if (IsEnabled())
            {
                CachingIntermediate(certificate.Subject);
            }
        }

        [Event(
            EventId_CachingIntermediate,
            Level = EventLevel.Verbose,
            Message = "Caching the intermediate certificate ('{0}') to the CurrentUser\\CA store.")]
        private void CachingIntermediate(string subjectName)
        {
            WriteEvent(EventId_CachingIntermediate, subjectName);
        }

        [NonEvent]
        internal void CachingIntermediateFailed(X509Certificate2 certificate)
        {
            if (IsEnabled())
            {
                CachingIntermediateFailed(certificate.Subject);
            }
        }

        [Event(
            EventId_CachingIntermediateFailed,
            Level = EventLevel.Warning,
            Message = "Adding the downloaded intermediate '{0}' to the CurrentUser\\CA store failed.")]
        private void CachingIntermediateFailed(string subjectName)
        {
            WriteEvent(EventId_CachingIntermediateFailed);
        }

        [Event(
            EventId_RevocationCheckStart,
            Message = "Starting revocation check in mode '{0}' with scope '{1}' on a {2}-element chain.",
            Opcode = EventOpcode.Start,
            Level = EventLevel.Informational)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "revocationMode and revocationFlag are enums, and are trimmer safe")]
        internal void RevocationCheckStart(X509RevocationMode revocationMode, X509RevocationFlag revocationFlag, int chainSize)
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_RevocationCheckStart, revocationMode, revocationFlag, chainSize);
            }
        }

        [Event(
            EventId_RevocationCheckStop,
            Opcode = EventOpcode.Stop,
            Level = EventLevel.Informational)]
        internal void RevocationCheckStop()
        {
            if (IsEnabled())
            {
                WriteEvent(EventId_RevocationCheckStop);
            }
        }

        [NonEvent]
        internal void CrlIdentifiersDetermined(SafeX509Handle cert, string crlDistributionPoint, string cacheFileName)
        {
            if (IsEnabled())
            {
                CrlIdentifiersDetermined(GetCertificateSubject(cert), crlDistributionPoint, cacheFileName);
            }
        }

        [Event(
            EventId_CrlIdentifiersDetermined,
            Level = EventLevel.Verbose,
            Message = "Certificate '{0}' has a CRL Distribution Point of '{1}', will use '{2}' as the cache file.")]
        private void CrlIdentifiersDetermined(string subjectName, string crlDistributionPoint, string cacheFileName)
        {
            WriteEvent(EventId_CrlIdentifiersDetermined, subjectName, crlDistributionPoint, cacheFileName);
        }
    }
}
