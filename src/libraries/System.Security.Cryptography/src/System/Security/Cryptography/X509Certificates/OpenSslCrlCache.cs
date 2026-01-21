// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal static class OpenSslCrlCache
    {
        private static readonly string s_crlDir =
            PersistedFiles.GetUserFeatureDirectory(
                X509Persistence.CryptographyFeatureName,
                X509Persistence.CrlsSubFeatureName);

        private static readonly string s_ocspDir =
            PersistedFiles.GetUserFeatureDirectory(
                X509Persistence.CryptographyFeatureName,
                X509Persistence.OcspSubFeatureName);

        private static readonly ConcurrentDictionary<string, WeakReference<CachedCrlEntry>> s_crlCache = new();

        private const ulong X509_R_CERT_ALREADY_IN_HASH_TABLE = 0x0B07D065;

        public static void AddCrlForCertificate(
            SafeX509Handle cert,
            SafeX509StoreHandle store,
            X509RevocationMode revocationMode,
            DateTime verificationTime,
            TimeSpan downloadTimeout)
        {
            // In Offline mode, accept any cached CRL we have.
            // "CRL is Expired" is a better match for Offline than "Could not find CRL"
            if (revocationMode != X509RevocationMode.Online)
            {
                verificationTime = DateTime.MinValue;
            }

            string? url = GetCdpUrl(cert);

            if (url == null)
            {
                return;
            }

            string crlFileName = GetCrlFileName(cert, url);

            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.CrlIdentifiersDetermined(cert, url, crlFileName);
            }

            if (AddCachedCrl(crlFileName, store, verificationTime))
            {
                return;
            }

            // Don't do any work if we're prohibited from fetching new CRLs
            if (revocationMode != X509RevocationMode.Online)
            {
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.CrlCheckOffline();
                }

                return;
            }

            DownloadAndAddCrl(url, crlFileName, store, downloadTimeout);
        }

        private static bool AddCachedCrl(string crlFileName, SafeX509StoreHandle store, DateTime verificationTime)
        {
            // OpenSSL is going to convert our input time to universal, so we should be in Local or
            // Unspecified (local-assumed).
            Debug.Assert(
                verificationTime.Kind != DateTimeKind.Utc,
                "UTC verificationTime should have been normalized to Local");

            if (s_crlCache.TryGetValue(crlFileName, out WeakReference<CachedCrlEntry>? cacheEntryRef))
            {
                Debug.Assert(cacheEntryRef is not null);

                if (cacheEntryRef.TryGetTarget(out CachedCrlEntry? cacheEntry))
                {
                    Debug.Assert(cacheEntry is not null);

                    if (verificationTime < cacheEntry.Expiration)
                    {
                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.CrlCacheInMemoryHit(cacheEntry.Expiration);
                        }

                        AttachCrl(store, cacheEntry.CrlHandle);
                        return true;
                    }

                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                    {
                        OpenSslX509ChainEventSource.Log.CrlCacheInMemoryExpired(verificationTime, cacheEntry.Expiration);
                    }
                }
                else if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.CrlCacheInMemoryCollected();
                }
            }
            else if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.CrlCacheInMemoryMiss();
            }

            // Check the disk cache.
            // For uncached this is the first load, for collected it's a reload,
            // for expired it's checking to see if another process has updated the disk cache.
            CachedCrlEntry? diskCacheEntry = CheckDiskCache(crlFileName, verificationTime);

            if (diskCacheEntry is null)
            {
                return false;
            }

            UpdateCacheAndAttachCrl(crlFileName, store, diskCacheEntry);
            return true;
        }

        private static void UpdateCacheAndAttachCrl(string crlFileName, SafeX509StoreHandle store, CachedCrlEntry newEntry)
        {
            Debug.Assert(!newEntry.CrlHandle.IsInvalid);

            WeakReference<CachedCrlEntry> cacheEntryRef = UpdateCache(crlFileName, new WeakReference<CachedCrlEntry>(newEntry));

            if (!cacheEntryRef.TryGetTarget(out CachedCrlEntry? cacheEntry))
            {
                // If AddOrUpdate picks another entry, but that gets collected between there and here,
                // use the disk cache entry we loaded instead of spinning or fresh download.
                cacheEntry = newEntry;
            }

            Debug.Assert(cacheEntry is not null);

            if (!ReferenceEquals(cacheEntry, newEntry))
            {
                // Another thread inserted a better entry than we were trying to,
                // dispose our CRL.
                newEntry.CrlHandle.Dispose();
            }

            // We've already touched the collection, so purge off anything collected.
            foreach (KeyValuePair<string, WeakReference<CachedCrlEntry>> kvp in s_crlCache)
            {
                if (!kvp.Value.TryGetTarget(out _))
                {
                    s_crlCache.TryRemove(kvp);
                }
            }

            AttachCrl(store, cacheEntry.CrlHandle);

            static WeakReference<CachedCrlEntry> UpdateCache(string key, WeakReference<CachedCrlEntry> newWeakRef)
            {
                return s_crlCache.AddOrUpdate(
                    key,
                    newWeakRef,
                    (key, cur) =>
                    {
                        bool hasCur = cur.TryGetTarget(out CachedCrlEntry? curEntry);
                        bool hasNew = newWeakRef.TryGetTarget(out CachedCrlEntry? innerNewEntry);

                        if (hasCur && hasNew)
                        {
                            Debug.Assert(curEntry is not null);
                            Debug.Assert(innerNewEntry is not null);

                            // Keep the one with the later expiration
                            return curEntry.Expiration >= innerNewEntry.Expiration ? cur : newWeakRef;
                        }

                        // The target of the WeakReference passed in here is still rooted,
                        // so if one of cur and new is missing, it has to have been cur.
                        Debug.Assert(hasNew);
                        return newWeakRef;
                    });
            }
        }

        private static void AttachCrl(SafeX509StoreHandle store, SafeX509CrlHandle crl)
        {
            Debug.Assert(!crl.IsInvalid);

            // X509_STORE_add_crl will increase the refcount on the CRL object,
            // so we don't need to worry about our copy getting cleaned up as a weak reference.
            if (!Interop.Crypto.X509StoreAddCrl(store, crl))
            {
                // Ignore error "cert already in store", throw on anything else. In any case the error queue will be cleared.
                if (X509_R_CERT_ALREADY_IN_HASH_TABLE == Interop.Crypto.ErrPeekLastError())
                {
                    Interop.Crypto.ErrClearError();
                }
                else
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }
            }
        }

        private static CachedCrlEntry? CheckDiskCache(string crlFileName, DateTime verificationTime)
        {
            string crlFile = GetCachedCrlPath(crlFileName);

            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.CrlCacheCheckStart();
            }

            try
            {
                return CheckDiskCacheCore(crlFile, verificationTime);
            }
            finally
            {
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.CrlCacheCheckStop();
                }
            }
        }

        private static CachedCrlEntry? CheckDiskCacheCore(string crlFile, DateTime verificationTime)
        {
            using (SafeBioHandle bio = Interop.Crypto.BioNewFile(crlFile, "rb"))
            {
                if (bio.IsInvalid)
                {
                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                    {
                        OpenSslX509ChainEventSource.Log.CrlCacheOpenError();
                    }

                    Interop.Crypto.ErrClearError();
                    return null;
                }

                // X509_STORE_add_crl will increase the refcount on the CRL object, so we should still
                // dispose our copy.
                SafeX509CrlHandle crl = Interop.Crypto.PemReadBioX509Crl(bio);

                {
                    if (crl.IsInvalid)
                    {
                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.CrlCacheDecodeError();
                        }

                        crl.Dispose();
                        Interop.Crypto.ErrClearError();
                        return null;
                    }

                    // If crl.LastUpdate is in the past, downloading a new version isn't really going
                    // to help, since we can't rewind the Internet. So this is just going to fail, but
                    // at least it can fail without using the network.
                    //
                    // If crl.NextUpdate is in the past, try downloading a newer version.
                    IntPtr nextUpdatePtr = Interop.Crypto.GetX509CrlNextUpdate(crl);
                    DateTime nextUpdate;

                    // If there is no crl.NextUpdate, this indicates that the CA is not providing
                    // any more updates to the CRL, or they made a mistake not providing a NextUpdate.
                    // We'll cache it for a few days to cover the case it was a mistake.
                    if (nextUpdatePtr == IntPtr.Zero)
                    {
                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.CrlCacheFileBasedExpiry();
                        }

                        try
                        {
                            nextUpdate = ExpirationTimeFromCacheFileTime(File.GetLastWriteTime(crlFile));
                        }
                        catch
                        {
                            // We couldn't determine when the CRL was last written to,
                            // so consider it expired.
                            Debug.Fail("Failed to get the last write time of the CRL file");
                            return null;
                        }
                    }
                    else
                    {
                        nextUpdate = OpenSslX509CertificateReader.ExtractValidityDateTime(nextUpdatePtr);
                    }

                    // In the event that we're to-the-second accurate on the match, OpenSSL will consider this
                    // to be already expired.
                    if (nextUpdate <= verificationTime)
                    {
                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.CrlCacheExpired(verificationTime, nextUpdate);
                        }

                        return null;
                    }

                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                    {
                        OpenSslX509ChainEventSource.Log.CrlCacheAcceptedFile(nextUpdate);
                    }

                    return new CachedCrlEntry(crl, nextUpdate);
                }
            }
        }

        private static void DownloadAndAddCrl(
            string url,
            string crlFileName,
            SafeX509StoreHandle store,
            TimeSpan downloadTimeout)
        {
            CachedCrlEntry? newEntry = DownloadAndCacheCrl(url, crlFileName, downloadTimeout);

            if (newEntry is not null)
            {
                UpdateCacheAndAttachCrl(crlFileName, store, newEntry);
            }
        }

        private static CachedCrlEntry? DownloadAndCacheCrl(
            string url,
            string crlFileName,
            TimeSpan downloadTimeout)
        {
            SafeX509CrlHandle? crl = OpenSslCertificateAssetDownloader.DownloadCrl(url, downloadTimeout);

            // null is a valid return (e.g. no remainingDownloadTime)
            if (crl == null || crl.IsInvalid)
            {
                return null;
            }

            IntPtr nextUpdatePtr = Interop.Crypto.GetX509CrlNextUpdate(crl);
            DateTime expiryTime;

            // If there is no crl.NextUpdate, this indicates that the CA is not providing
            // any more updates to the CRL, or they made a mistake not providing a NextUpdate.
            // We'll cache it for a few days to cover the case it was a mistake.
            if (nextUpdatePtr == IntPtr.Zero)
            {
                expiryTime = ExpirationTimeFromCacheFileTime(DateTime.Now);
            }
            else
            {
                expiryTime = OpenSslX509CertificateReader.ExtractValidityDateTime(nextUpdatePtr);
            }

            // Saving the CRL to the disk is just a performance optimization for later requests to not
            // need to use the network again, so failure to save shouldn't throw an exception or mark
            // the chain as invalid.
            try
            {
                string crlFile = GetCachedCrlPath(crlFileName, mkDir: true);

                using (SafeBioHandle bio = Interop.Crypto.BioNewFile(crlFile, "wb"))
                {
                    if (bio.IsInvalid || Interop.Crypto.PemWriteBioX509Crl(bio, crl) == 0)
                    {
                        // No bio, or write failed

                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.CrlCacheWriteFailed(crlFile);
                        }

                        Interop.Crypto.ErrClearError();
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.CrlCacheWriteSucceeded();
            }

            return new CachedCrlEntry(crl, expiryTime);
        }

        private static DateTime ExpirationTimeFromCacheFileTime(DateTime cacheFileTime)
        {
            return cacheFileTime.AddDays(3);
        }

        internal static string GetCachedOcspResponseDirectory()
        {
            return s_ocspDir;
        }

        private static string GetCrlFileName(SafeX509Handle cert, string crlUrl)
        {
            // X509_issuer_name_hash returns "unsigned long", which is marshalled as ulong.
            // But it only sets 32 bits worth of data, so force it down to uint just... in case.
            ulong persistentHashLong = Interop.Crypto.X509IssuerNameHash(cert);
            if (persistentHashLong == 0)
            {
                Interop.Crypto.ErrClearError();
            }

            uint persistentHash = unchecked((uint)persistentHashLong);
            Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];

            // Endianness isn't important, it just needs to be consistent.
            // (Even if the same storage was used for two different endianness systems it'd stabilize at two files).
            ReadOnlySpan<byte> utf16Url = MemoryMarshal.AsBytes(crlUrl.AsSpan());

            if (SHA256.HashData(utf16Url, hash) != hash.Length)
            {
                Debug.Fail("HashData failed or produced an incorrect length output");
                throw new CryptographicException();
            }

            uint urlHash = MemoryMarshal.Read<uint>(hash);

            // OpenSSL's hashed filename algorithm is the 8-character hex version of the 32-bit value
            // of X509_issuer_name_hash (or X509_subject_name_hash, depending on the context).
            //
            // We mix in an 8-character hex version of the "left"-most bytes of a hash of the URL to
            // disambiguate when one Issuing Authority separates their revocation across independent CRLs.
            return $"{persistentHash:x8}.{urlHash:x8}.crl";
        }

        private static string GetCachedCrlPath(string localFileName, bool mkDir = false)
        {
            if (mkDir)
            {
                Directory.CreateDirectory(s_crlDir);
            }

            return Path.Combine(s_crlDir, localFileName);
        }

        private static string? GetCdpUrl(SafeX509Handle cert)
        {
            ArraySegment<byte> crlDistributionPoints =
                OpenSslX509CertificateReader.FindFirstExtension(cert, Oids.CrlDistributionPoints);

            if (crlDistributionPoints.Array == null)
            {
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.NoCdpFound(cert);
                }

                return null;
            }

            try
            {
                AsnValueReader reader = new AsnValueReader(crlDistributionPoints, AsnEncodingRules.DER);
                AsnValueReader sequenceReader = reader.ReadSequence();
                reader.ThrowIfNotEmpty();

                while (sequenceReader.HasData)
                {
                    DistributionPointAsn.Decode(ref sequenceReader, crlDistributionPoints, out DistributionPointAsn distributionPoint);

                    // Only distributionPoint is supported
                    // Only fullName is supported, nameRelativeToCRLIssuer is for LDAP-based lookup.
                    if (distributionPoint.DistributionPoint.HasValue &&
                        distributionPoint.DistributionPoint.Value.FullName != null)
                    {
                        foreach (GeneralNameAsn name in distributionPoint.DistributionPoint.Value.FullName)
                        {
                            if (name.Uri != null)
                            {
                                if (Uri.TryCreate(name.Uri, UriKind.Absolute, out Uri? uri) &&
                                    uri.Scheme == "http")
                                {
                                    return name.Uri;
                                }
                                else
                                {
                                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                                    {
                                        OpenSslX509ChainEventSource.Log.NonHttpCdpEntry(name.Uri);
                                    }
                                }
                            }
                        }

                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.NoMatchingCdpEntry();
                        }
                    }
                }
            }
            catch (CryptographicException)
            {
                // Treat any ASN errors as if the extension was missing.
            }
            catch (AsnContentException)
            {
                // Treat any ASN errors as if the extension was missing.
            }
            finally
            {
                // The data came from a certificate, so it's public.
                CryptoPool.Return(crlDistributionPoints.Array, clearSize: 0);
            }

            return null;
        }

        private sealed class CachedCrlEntry
        {
            internal SafeX509CrlHandle CrlHandle { get; }
            internal DateTime Expiration { get; }

            internal CachedCrlEntry(SafeX509CrlHandle crlHandle, DateTime expiration)
            {
                CrlHandle = crlHandle;
                Expiration = expiration;
            }
        }
    }
}
