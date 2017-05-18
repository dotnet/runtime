// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Internal.Cryptography.Pal
{
    /// <summary>
    /// Provides an implementation of an X509Store which is backed by files in a directory.
    /// </summary>
    internal sealed class DirectoryBasedStoreProvider : IStorePal
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

            Debug.Assert(!X509Store.DisallowedStoreName.Equals(storeName, StringComparison.OrdinalIgnoreCase));

            _storePath = GetStorePath(storeName);

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
        }

        public void CloneTo(X509Certificate2Collection collection)
        {
            Debug.Assert(collection != null);

            if (!Directory.Exists(_storePath))
            {
                return;
            }

            var loadedCerts = new HashSet<X509Certificate2>();

            foreach (string filePath in Directory.EnumerateFiles(_storePath, PfxWildcard))
            {
                try
                {
                    var cert = new X509Certificate2(filePath);

                    // If we haven't already loaded a cert .Equal to this one, copy it to the collection.
                    if (loadedCerts.Add(cert))
                    {
                        collection.Add(cert);
                    }
                    else
                    {
                        cert.Dispose();
                    }
                }
                catch (CryptographicException)
                {
                    // The file wasn't a certificate, move on to the next one.
                }
            }
        }

        public void Add(ICertificatePal certPal)
        {
            if (_readOnly)
            {
                // Windows compatibility: Remove only throws when it needs to do work, add throws always.
                throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);
            }

            // This may well be the first time that we've added something to this store.
            Directory.CreateDirectory(_storePath);

            uint userId = Interop.Sys.GetEUid();
            EnsureDirectoryPermissions(_storePath, userId);

            OpenSslX509CertificateReader cert = (OpenSslX509CertificateReader)certPal;

            using (X509Certificate2 copy = new X509Certificate2(cert.DuplicateHandles()))
            {
                string thumbprint = copy.Thumbprint;
                bool findOpenSlot;

                // The odds are low that we'd have a thumbprint collision, but check anyways.
                string existingFilename = FindExistingFilename(copy, _storePath, out findOpenSlot);

                if (existingFilename != null)
                {
                    if (!copy.HasPrivateKey)
                    {
                        return;
                    }

                    try
                    {
                        using (X509Certificate2 fromFile = new X509Certificate2(existingFilename))
                        {
                            if (fromFile.HasPrivateKey)
                            {
                                // We have a private key, the file has a private key, we're done here.
                                return;
                            }
                        }
                    }
                    catch (CryptographicException)
                    {
                        // We can't read this file anymore, but a moment ago it was this certificate,
                        // so go ahead and overwrite it.
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
                    EnsureFilePermissions(stream, userId);
                    byte[] pkcs12 = copy.Export(X509ContentType.Pkcs12);
                    stream.Write(pkcs12, 0, pkcs12.Length);
                }
            }
        }

        public void Remove(ICertificatePal certPal)
        {
            OpenSslX509CertificateReader cert = (OpenSslX509CertificateReader)certPal;

            using (X509Certificate2 copy = new X509Certificate2(cert.DuplicateHandles()))
            {
                string currentFilename;

                do
                {
                    bool hadCandidates;
                    currentFilename = FindExistingFilename(copy, _storePath, out hadCandidates);

                    if (currentFilename != null)
                    {
                        if (_readOnly)
                        {
                            // Windows compatibility, the readonly check isn't done until after a match is found.
                            throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);
                        }

                        File.Delete(currentFilename);
                    }
                } while (currentFilename != null);
            }
        }

        SafeHandle IStorePal.SafeHandle
        {
            get { return null; }
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

        private static string GetStorePath(string storeName)
        {
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

            return Path.Combine(s_userStoreRoot, directoryName);
        }

        private static string GetDirectoryName(string storeName)
        {
            Debug.Assert(storeName != null);

            try
            {
                string fileName = Path.GetFileName(storeName);

                if (!StringComparer.Ordinal.Equals(storeName, fileName))
                {
                    throw new CryptographicException(SR.Format(SR.Security_InvalidValue, nameof(storeName)));
                }
            }
            catch (IOException e)
            {
                throw new CryptographicException(e.Message, e);
            }

            return storeName.ToLowerInvariant();
        }

        /// <summary>
        /// Checks the store directory has the correct permissions.
        /// </summary>
        /// <param name="path">
        /// The path of the directory to check.
        /// </param>
        /// <param name="userId">
        /// The current userId from GetEUid().
        /// </param>
        private static void EnsureDirectoryPermissions(string path, uint userId)
        {
            Interop.Sys.FileStatus dirStat;
            if (Interop.Sys.Stat(path, out dirStat) != 0)
            {
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                throw new CryptographicException(
                    SR.Cryptography_FileStatusError,
                    new IOException(error.GetErrorMessage(), error.RawErrno));
            }

            if (dirStat.Uid != userId)
            {
                throw new CryptographicException(SR.Format(SR.Cryptography_OwnerNotCurrentUser, path));
            }

            if ((dirStat.Mode & (int)Interop.Sys.Permissions.S_IRWXU) != (int)Interop.Sys.Permissions.S_IRWXU)
            {
                throw new CryptographicException(SR.Format(SR.Cryptography_InvalidDirectoryPermissions, path));
            }
        }

        /// <summary>
        /// Checks the file has the correct permissions and attempts to modify them if they're inappropriate.
        /// </summary>
        /// <param name="stream">
        /// The file stream to check.
        /// </param>
        /// <param name="userId">
        /// The current userId from GetEUid().
        /// </param>
        private static void EnsureFilePermissions(FileStream stream, uint userId)
        {
            // Verify that we're creating files with u+rw and g-rw, o-rw.
            const Interop.Sys.Permissions requiredPermissions =
                Interop.Sys.Permissions.S_IRUSR | Interop.Sys.Permissions.S_IWUSR;

            const Interop.Sys.Permissions forbiddenPermissions =
                Interop.Sys.Permissions.S_IRGRP | Interop.Sys.Permissions.S_IWGRP |
                Interop.Sys.Permissions.S_IROTH | Interop.Sys.Permissions.S_IWOTH;

            Interop.Sys.FileStatus stat;
            if (Interop.Sys.FStat(stream.SafeFileHandle, out stat) != 0)
            {
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                throw new CryptographicException(
                    SR.Cryptography_FileStatusError,
                    new IOException(error.GetErrorMessage(), error.RawErrno));
            }

            if (stat.Uid != userId)
            {
                throw new CryptographicException(SR.Format(SR.Cryptography_OwnerNotCurrentUser, stream.Name));
            }

            if ((stat.Mode & (int)requiredPermissions) != (int)requiredPermissions ||
                (stat.Mode & (int)forbiddenPermissions) != 0)
            {
                if (Interop.Sys.FChMod(stream.SafeFileHandle, (int)requiredPermissions) < 0)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    throw new CryptographicException(
                        SR.Format(SR.Cryptography_InvalidFilePermissions, stream.Name),
                        new IOException(error.GetErrorMessage(), error.RawErrno));
                }

                // Verify the chmod applied.
                if (Interop.Sys.FStat(stream.SafeFileHandle, out stat) != 0)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    throw new CryptographicException(
                        SR.Cryptography_FileStatusError,
                        new IOException(error.GetErrorMessage(), error.RawErrno));
                }

                if ((stat.Mode & (int)requiredPermissions) != (int)requiredPermissions ||
                    (stat.Mode & (int)forbiddenPermissions) != 0)
                {
                    throw new CryptographicException(SR.Format(SR.Cryptography_InvalidFilePermissions, stream.Name));
                }
            }
        }

        internal sealed class UnsupportedDisallowedStore : IStorePal
        {
            private readonly bool _readOnly;

            internal UnsupportedDisallowedStore(OpenFlags openFlags)
            {
                // ReadOnly is 0x00, so it is implicit unless either ReadWrite or MaxAllowed
                // was requested.
                OpenFlags writeFlags = openFlags & (OpenFlags.ReadWrite | OpenFlags.MaxAllowed);

                if (writeFlags == OpenFlags.ReadOnly)
                {
                    _readOnly = true;
                }

                string storePath = GetStorePath(X509Store.DisallowedStoreName);

                try
                {
                    if (Directory.Exists(storePath))
                    {
                        // If it has no files, leave it alone.
                        foreach (string filePath in Directory.EnumerateFiles(storePath))
                        {
                            string msg = SR.Format(SR.Cryptography_Unix_X509_DisallowedStoreNotEmpty, storePath);
                            throw new CryptographicException(msg, new PlatformNotSupportedException(msg));
                        }
                    }
                }
                catch (IOException)
                {
                    // Suppress the exception, treat the store as empty.
                }
            }

            public void Dispose()
            {
                // Nothing to do.
            }

            public void CloneTo(X509Certificate2Collection collection)
            {
                // Never show any data.
            }

            public void Add(ICertificatePal cert)
            {
                if (_readOnly)
                {
                    throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);
                }

                throw new CryptographicException(
                    SR.Cryptography_Unix_X509_NoDisallowedStore,
                    new PlatformNotSupportedException(SR.Cryptography_Unix_X509_NoDisallowedStore));
            }

            public void Remove(ICertificatePal cert)
            {
                // Remove never throws if it does no measurable work.
                // Since CloneTo always says the store is empty, no measurable work is ever done.
            }

            SafeHandle IStorePal.SafeHandle { get; } = null;
        }
    }
}
