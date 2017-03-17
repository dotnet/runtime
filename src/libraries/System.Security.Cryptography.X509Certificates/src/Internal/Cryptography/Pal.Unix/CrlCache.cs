// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal static class CrlCache
    {
        public static void AddCrlForCertificate(
            X509Certificate2 cert,
            SafeX509StoreHandle store,
            X509RevocationMode revocationMode,
            DateTime verificationTime,
            ref TimeSpan remainingDownloadTime)
        {
            // In Offline mode, accept any cached CRL we have.
            // "CRL is Expired" is a better match for Offline than "Could not find CRL"
            if (revocationMode != X509RevocationMode.Online)
            {
                verificationTime = DateTime.MinValue;
            }

            if (AddCachedCrl(cert, store, verificationTime))
            {
                return;
            }

            // Don't do any work if we're over limit or prohibited from fetching new CRLs
            if (remainingDownloadTime <= TimeSpan.Zero ||
                revocationMode != X509RevocationMode.Online)
            {
                return;
            }

            DownloadAndAddCrl(cert, store, ref remainingDownloadTime);
        }

        private static bool AddCachedCrl(X509Certificate2 cert, SafeX509StoreHandle store, DateTime verificationTime)
        {
            string crlFile = GetCachedCrlPath(cert);

            using (SafeBioHandle bio = Interop.Crypto.BioNewFile(crlFile, "rb"))
            {
                if (bio.IsInvalid)
                {
                    return false;
                }

                // X509_STORE_add_crl will increase the refcount on the CRL object, so we should still
                // dispose our copy.
                using (SafeX509CrlHandle crl = Interop.Crypto.PemReadBioX509Crl(bio))
                {
                    if (crl.IsInvalid)
                    {
                        return false;
                    }

                    // If crl.LastUpdate is in the past, downloading a new version isn't really going
                    // to help, since we can't rewind the Internet. So this is just going to fail, but
                    // at least it can fail without using the network.
                    //
                    // If crl.NextUpdate is in the past, try downloading a newer version.
                    DateTime nextUpdate = OpenSslX509CertificateReader.ExtractValidityDateTime(
                        Interop.Crypto.GetX509CrlNextUpdate(crl));

                    // OpenSSL is going to convert our input time to universal, so we should be in Local or
                    // Unspecified (local-assumed).
                    Debug.Assert(
                        verificationTime.Kind != DateTimeKind.Utc,
                        "UTC verificationTime should have been normalized to Local");

                    // In the event that we're to-the-second accurate on the match, OpenSSL will consider this
                    // to be already expired.
                    if (nextUpdate <= verificationTime)
                    {
                        return false;
                    }

                    // TODO (#3063): Check the return value of X509_STORE_add_crl, and throw on any error other
                    // than X509_R_CERT_ALREADY_IN_HASH_TABLE
                    Interop.Crypto.X509StoreAddCrl(store, crl);

                    return true;
                }
            }
        }

        private static void DownloadAndAddCrl(
            X509Certificate2 cert,
            SafeX509StoreHandle store,
            ref TimeSpan remainingDownloadTime)
        {
            string url = GetCdpUrl(cert);

            if (url == null)
            {
                return;
            }

            // X509_STORE_add_crl will increase the refcount on the CRL object, so we should still
            // dispose our copy.
            using (SafeX509CrlHandle crl = CertificateAssetDownloader.DownloadCrl(url, ref remainingDownloadTime))
            {
                // null is a valid return (e.g. no remainingDownloadTime)
                if (crl != null && !crl.IsInvalid)
                {
                    // TODO (#3063): Check the return value of X509_STORE_add_crl, and throw on any error other
                    // than X509_R_CERT_ALREADY_IN_HASH_TABLE
                    Interop.Crypto.X509StoreAddCrl(store, crl);

                    // Saving the CRL to the disk is just a performance optimization for later requests to not
                    // need to use the network again, so failure to save shouldn't throw an exception or mark
                    // the chain as invalid.
                    try
                    {
                        string crlFile = GetCachedCrlPath(cert, mkDir: true);

                        using (SafeBioHandle bio = Interop.Crypto.BioNewFile(crlFile, "wb"))
                        {
                            if (!bio.IsInvalid)
                            {
                                Interop.Crypto.PemWriteBioX509Crl(bio, crl);
                            }
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
                }
            }
        }
        
        private static string GetCachedCrlPath(X509Certificate2 cert, bool mkDir=false)
        {
            OpenSslX509CertificateReader pal = (OpenSslX509CertificateReader)cert.Pal;

            string crlDir = PersistedFiles.GetUserFeatureDirectory(
                X509Persistence.CryptographyFeatureName,
                X509Persistence.CrlsSubFeatureName);

            // X509_issuer_name_hash returns "unsigned long", which is marshalled as ulong.
            // But it only sets 32 bits worth of data, so force it down to uint just... in case.
            ulong persistentHashLong = Interop.Crypto.X509IssuerNameHash(pal.SafeHandle);
            uint persistentHash = unchecked((uint)persistentHashLong);

            // OpenSSL's hashed filename algorithm is the 8-character hex version of the 32-bit value
            // of X509_issuer_name_hash (or X509_subject_name_hash, depending on the context).
            string localFileName = persistentHash.ToString("x8") + ".crl";

            if (mkDir)
            {
                Directory.CreateDirectory(crlDir);
            }

            return Path.Combine(crlDir, localFileName);
        }

        private static string GetCdpUrl(X509Certificate2 cert)
        {
            byte[] crlDistributionPoints = null;

            foreach (X509Extension extension in cert.Extensions)
            {
                if (StringComparer.Ordinal.Equals(extension.Oid.Value, Oids.CrlDistributionPoints))
                {
                    // If there's an Authority Information Access extension, it might be used for
                    // looking up additional certificates for the chain.
                    crlDistributionPoints = extension.RawData;
                    break;
                }
            }

            if (crlDistributionPoints == null)
            {
                return null;
            }

            // CRLDistributionPoints ::= SEQUENCE SIZE (1..MAX) OF DistributionPoint
            //
            // DistributionPoint ::= SEQUENCE {
            //    distributionPoint       [0]     DistributionPointName OPTIONAL,
            //    reasons                 [1]     ReasonFlags OPTIONAL,
            //    cRLIssuer               [2]     GeneralNames OPTIONAL }
            //
            // DistributionPointName ::= CHOICE {
            //    fullName                [0]     GeneralNames,
            //    nameRelativeToCRLIssuer [1]     RelativeDistinguishedName }
            //
            // GeneralNames ::= SEQUENCE SIZE (1..MAX) OF GeneralName
            //
            // GeneralName ::= CHOICE {
            //    otherName                       [0]     OtherName,
            //    rfc822Name                      [1]     IA5String,
            //    dNSName                         [2]     IA5String,
            //    x400Address                     [3]     ORAddress,
            //    directoryName                   [4]     Name,
            //    ediPartyName                    [5]     EDIPartyName,
            //    uniformResourceIdentifier       [6]     IA5String,
            //    iPAddress                       [7]     OCTET STRING,
            //    registeredID                    [8]     OBJECT IDENTIFIER }

            DerSequenceReader cdpSequence = new DerSequenceReader(crlDistributionPoints);

            while (cdpSequence.HasData)
            {
                const byte ContextSpecificFlag = 0x80;
                const byte ContextSpecific0 = ContextSpecificFlag;
                const byte ConstructedFlag = 0x20;
                const byte ContextSpecificConstructed0 = ContextSpecific0 | ConstructedFlag;
                const byte GeneralNameUri = ContextSpecificFlag | 0x06;

                DerSequenceReader distributionPointReader = cdpSequence.ReadSequence();
                byte tag = distributionPointReader.PeekTag();

                // Only distributionPoint is supported
                if (tag != ContextSpecificConstructed0)
                {
                    continue;
                }

                // The DistributionPointName is a CHOICE, not a SEQUENCE, but the reader is the same.
                DerSequenceReader dpNameReader = distributionPointReader.ReadSequence();
                tag = dpNameReader.PeekTag();

                // Only fullName is supported,
                // nameRelativeToCRLIssuer is for LDAP-based lookup.
                if (tag != ContextSpecificConstructed0)
                {
                    continue;
                }

                DerSequenceReader fullNameReader = dpNameReader.ReadSequence();

                while (fullNameReader.HasData)
                {
                    tag = fullNameReader.PeekTag();

                    if (tag != GeneralNameUri)
                    {
                        fullNameReader.SkipValue();
                        continue;
                    }

                    string uri = fullNameReader.ReadIA5String();

                    Uri parsedUri = new Uri(uri);

                    if (!StringComparer.Ordinal.Equals(parsedUri.Scheme, "http"))
                    {
                        continue;
                    }

                    return uri;
                }
            }

            return null;
        }
    }
}
