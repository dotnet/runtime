// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Internal.Cryptography.Pal
{
    /// <summary>
    /// Provides an implementation of an X509Store which is backed by files in a directory.
    /// </summary>
    internal class DirectoryBasedStoreProvider : IStorePal
    {
        // {thumbprint}.1.pfx to {thumbprint}.9.pfx
        private const int MaxSaveAttempts = 9; 
        private const string PfxExtension = ".pfx";
        // *.pfx ({thumbprint}.pfx or {thumbprint}.{ordinal}.pfx)
        private const string PfxWildcard = "*" + PfxExtension;
        // .*.pfx ({thumbprint}.{ordinal}.pfx)
        private const string PfxOrdinalWildcard = "." + PfxWildcard;

        private static string s_userStoreRoot;

        private readonly string _storePath;
        private readonly object _fileWatcherLock = new object();
        private List<X509Certificate2> _certificates;
        private FileSystemWatcher _watcher;

        private readonly bool _readOnly;

#if DEBUG
        static DirectoryBasedStoreProvider()
        {
            Debug.Assert(
                0 == OpenFlags.ReadOnly,
                "OpenFlags.ReadOnly is not zero, read-only detection will not work");
        }
#endif

        internal DirectoryBasedStoreProvider(string storeName, OpenFlags openFlags)
        {
            if (string.IsNullOrEmpty(storeName))
            {
                throw new CryptographicException(SR.Arg_EmptyOrNullString);
            }

            string directoryName = GetDirectoryName(storeName);

            if (s_userStoreRoot == null)
            {
                // Do this here instead of a static field initializer so that
                // the static initializer isn't capable of throwing the "home directory not found"
                // exception.
                s_userStoreRoot = PersistedFiles.GetUserFeatureDirectory(
                    X509Persistence.CryptographyFeatureName,
                    X509Persistence.X509StoresSubFeatureName);
            }

            _storePath = Path.Combine(s_userStoreRoot, directoryName);

            if (0 != (openFlags & OpenFlags.OpenExistingOnly))
            {
                if (!Directory.Exists(_storePath))
                {
                    throw new CryptographicException(SR.Cryptography_X509_StoreNotFound);
                }
            }

            // ReadOnly is 0x00, so it is implicit unless either ReadWrite or MaxAllowed
            // was requested.
            OpenFlags writeFlags = openFlags & (OpenFlags.ReadWrite | OpenFlags.MaxAllowed);

            if (writeFlags == OpenFlags.ReadOnly)
            {
                _readOnly = true;
            }
        }
        
        public void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }
        }

        public void FindAndCopyTo(
            X509FindType findType,
            object findValue,
            bool validOnly,
            X509Certificate2Collection collection)
        {
        }

        public byte[] Export(X509ContentType contentType, string password)
        {
            // Export is for X509Certificate2Collections in their IStorePal guise,
            // if someone wanted to export whole stores they'd need to do
            // store.Certificates.Export(...), which would end up in the
            // CollectionBackedStoreProvider.
            Debug.Fail("Export was unexpected on a DirectoryBasedStore");
            throw new InvalidOperationException();
        }

        public void CopyTo(X509Certificate2Collection collection)
        {
            Debug.Assert(collection != null);

            // Copy the reference locally, any directory change operations
            // will cause the field to be reset to null.
            List<X509Certificate2> certificates = _certificates;

            if (certificates == null)
            {
                // ReadDirectory will both load _certificates and return the answer, so this call
                // will have stable results across multiple adds/deletes happening in parallel.
                certificates = ReadDirectory();
                Debug.Assert(certificates != null);
            }

            foreach (X509Certificate2 cert in certificates)
            {
                collection.Add(cert);
            }
        }

        private List<X509Certificate2> ReadDirectory()
        {
            if (!Directory.Exists(_storePath))
            {
                // Don't assign the field here, because we don't have a FileSystemWatcher
                // yet to tell us that something has been added.
                return new List<X509Certificate2>(0);
            }

            List<X509Certificate2> certs = new List<X509Certificate2>();

            lock (_fileWatcherLock)
            {
                if (_watcher == null)
                {
                    _watcher = new FileSystemWatcher(_storePath, PfxWildcard)
                    {
                        NotifyFilter = NotifyFilters.LastWrite,
                    };

                    FileSystemEventHandler handler = FlushCache;
                    _watcher.Changed += handler;
                    _watcher.Created += handler;
                    _watcher.Deleted += handler;
                    // The Renamed event has a different delegate type
                    _watcher.Renamed += FlushCache;
                    _watcher.Error += FlushCache;
                }

                // Start watching for change events to know that another instance
                // has messed with the underlying store.  This keeps us aligned
                // with the Windows implementation, which opens stores with change
                // notifications.
                _watcher.EnableRaisingEvents = true;

                foreach (string filePath in Directory.EnumerateFiles(_storePath, PfxWildcard))
                {
                    X509Certificate2 cert;

                    try
                    {
                        cert = new X509Certificate2(filePath);
                    }
                    catch (CryptographicException)
                    {
                        // The file wasn't a certificate, move on to the next one.
                        continue;
                    }

                    certs.Add(cert);
                }

                // Don't release _fileWatcherLock until _certificates is assigned, otherwise
                // we may be setting it to a stale value after the change event said to clear it
                _certificates = certs;
            }

            return certs;
        }

        public void Add(ICertificatePal certPal)
        {
            if (_readOnly)
            {
                // Windows compatibility: Remove only throws when it needs to do work, add throws always.
                throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);
            }

            // Save the collection to a local so it's consistent for the whole method
            List<X509Certificate2> certificates = _certificates;
            OpenSslX509CertificateReader cert = (OpenSslX509CertificateReader)certPal;

            using (X509Certificate2 copy = new X509Certificate2(cert.DuplicateHandles()))
            {
                // certificates will be null if anything has changed since the last call to
                // get_Certificates; including Add being called without get_Certificates being
                // called at all.
                if (certificates != null)
                {
                    foreach (X509Certificate2 inCollection in certificates)
                    {
                        if (inCollection.Equals(copy))
                        {
                            if (!copy.HasPrivateKey || inCollection.HasPrivateKey)
                            {
                                // If the existing store only knows about a public key, but we're
                                // adding a public+private pair, continue with the add.
                                //
                                // So, therefore, if we aren't adding a private key, or already have one,
                                // we don't need to do anything.
                                return;
                            }
                        }
                    }
                }

                // This may well be the first time that we've added something to this store.
                Directory.CreateDirectory(_storePath);

                string thumbprint = copy.Thumbprint;
                bool findOpenSlot;

                // The odds are low that we'd have a thumbprint colission, but check anyways.
                string existingFilename = FindExistingFilename(copy, _storePath, out findOpenSlot);

                if (existingFilename != null)
                {
                    bool dataExistsAlready = false;

                    // If the file on disk is just a public key, but we're trying to add a private key,
                    // we'll want to overwrite it.
                    if (copy.HasPrivateKey)
                    {
                        try
                        {
                            using (X509Certificate2 fromFile = new X509Certificate2(existingFilename))
                            {
                                if (fromFile.HasPrivateKey)
                                {
                                    // We have a private key, the file has a private key, we're done here.
                                    dataExistsAlready = true;
                                }
                            }
                        }
                        catch (CryptographicException)
                        {
                            // We can't read this file anymore, but a moment ago it was this certificate,
                            // so go ahead and overwrite it.
                        }
                    }
                    else
                    {
                        // If we're just a public key then the file has at least as much data as we do.
                        dataExistsAlready = true;
                    }

                    if (dataExistsAlready)
                    {
                        // The file was added but our collection hasn't resynced yet.
                        // Set _certificates to null to force a resync.
                        _certificates = null;
                        return;
                    }
                }

                string destinationFilename;
                FileMode mode = FileMode.CreateNew;

                if (existingFilename != null)
                {
                    destinationFilename = existingFilename;
                    mode = FileMode.Create;
                }
                else if (findOpenSlot)
                {
                    destinationFilename = FindOpenSlot(thumbprint);
                }
                else
                {
                    destinationFilename = Path.Combine(_storePath, thumbprint + PfxExtension);
                }

                using (FileStream stream = new FileStream(destinationFilename, mode))
                {
                    byte[] pkcs12 = copy.Export(X509ContentType.Pkcs12);
                    stream.Write(pkcs12, 0, pkcs12.Length);
                }

#if DEBUG
                // Verify that we're creating files with u+rw and o-rw, g-rw.
                const Interop.Sys.Permissions requiredPermissions =
                    Interop.Sys.Permissions.S_IRUSR |
                    Interop.Sys.Permissions.S_IWUSR;

                const Interop.Sys.Permissions forbiddenPermissions =
                    Interop.Sys.Permissions.S_IROTH |
                    Interop.Sys.Permissions.S_IWOTH |
                    Interop.Sys.Permissions.S_IRGRP |
                    Interop.Sys.Permissions.S_IWGRP;

                Interop.Sys.FileStatus stat;

                Debug.Assert(Interop.Sys.Stat(destinationFilename, out stat) == 0);
                Debug.Assert((stat.Mode & (int)requiredPermissions) != 0, "Created PFX has insufficient permissions to function");
                Debug.Assert((stat.Mode & (int)forbiddenPermissions) == 0, "Created PFX has too broad of permissions");
#endif
            }

            // Null out _certificates so the next call to get_Certificates causes a re-scan.
            _certificates = null;
        }

        public void Remove(ICertificatePal certPal)
        {
            OpenSslX509CertificateReader cert = (OpenSslX509CertificateReader)certPal;

            using (X509Certificate2 copy = new X509Certificate2(cert.DuplicateHandles()))
            {
                bool hadCandidates;
                string currentFilename = FindExistingFilename(copy, _storePath, out hadCandidates);

                if (currentFilename != null)
                {
                    if (_readOnly)
                    {
                        // Windows compatibility, the readonly check isn't done until after a match is found.
                        throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);
                    }

                    File.Delete(currentFilename);
                }
            }

            // Null out _certificates so the next call to get_Certificates causes a re-scan.
            _certificates = null;
        }

        private static string FindExistingFilename(X509Certificate2 cert, string storePath, out bool hadCandidates)
        {
            hadCandidates = false;

            foreach (string maybeMatch in Directory.EnumerateFiles(storePath, cert.Thumbprint + PfxWildcard))
            {
                hadCandidates = true;

                try
                {
                    using (X509Certificate2 candidate = new X509Certificate2(maybeMatch))
                    {
                        if (candidate.Equals(cert))
                        {
                            return maybeMatch;
                        }
                    }
                }
                catch (CryptographicException)
                {
                    // Contents weren't interpretable as a certificate, so it's not a match.
                }
            }

            return null;
        }

        private string FindOpenSlot(string thumbprint)
        {
            // We already know that {thumbprint}.pfx is taken, so start with {thumbprint}.1.pfx

            // We need space for {thumbprint} (thumbprint.Length)
            // And ".0.pfx" (6)
            // If MaxSaveAttempts is big enough to use more than one digit, we need that space, too (MaxSaveAttempts / 10)
            StringBuilder pathBuilder = new StringBuilder(thumbprint.Length + PfxOrdinalWildcard.Length + (MaxSaveAttempts / 10));

            pathBuilder.Append(thumbprint);
            pathBuilder.Append('.');
            int prefixLength = pathBuilder.Length;

            for (int i = 1; i <= MaxSaveAttempts; i++)
            {
                pathBuilder.Length = prefixLength;
                
                pathBuilder.Append(i);
                pathBuilder.Append(PfxExtension);

                string builtPath = Path.Combine(_storePath, pathBuilder.ToString());

                if (!File.Exists(builtPath))
                {
                    return builtPath;
                }
            }

            throw new CryptographicException(SR.Cryptography_X509_StoreNoFileAvailable);
        }

        private void FlushCache(object sender, EventArgs e)
        {
            lock (_fileWatcherLock)
            {
                // Events might end up not firing until after the object was disposed, which could cause
                // problems consistently reading _watcher; so save it to a local.
                FileSystemWatcher watcher = _watcher;

                if (watcher != null)
                {
                    // Stop processing events until we read again, particularly because
                    // there's nothing else we'll do until then.
                    watcher.EnableRaisingEvents = false;
                }

                _certificates = null;
            }
        }

        private static string GetDirectoryName(string storeName)
        {
            Debug.Assert(storeName != null);

            try
            {
                string fileName = Path.GetFileName(storeName);

                if (!StringComparer.Ordinal.Equals(storeName, fileName))
                {
                    throw new CryptographicException(SR.Format(SR.Security_InvalidValue, "storeName"));
                }
            }
            catch (IOException e)
            {
                throw new CryptographicException(e.Message, e);
            }

            return storeName.ToLowerInvariant();
        }
    }
}
