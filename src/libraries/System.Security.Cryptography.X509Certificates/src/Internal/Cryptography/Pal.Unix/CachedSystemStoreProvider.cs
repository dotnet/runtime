// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class CachedSystemStoreProvider : IStorePal
    {
        // These intervals are mostly arbitrary.
        // Prior to this refreshing cache the system collections were read just once per process, on the
        // assumption that system trust changes would happen before the process start (or would come
        // followed by a reboot for a kernel update, etc).
        // Customers requested something more often than "never" and 5 minutes seems like a reasonable
        // balance.
        private static readonly TimeSpan s_lastWriteRecheckInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_assumeInvalidInterval = TimeSpan.FromMinutes(5);
        private static readonly Stopwatch s_recheckStopwatch = new Stopwatch();
        private static readonly DirectoryInfo? s_rootStoreDirectoryInfo = SafeOpenRootDirectoryInfo();
        private static readonly DirectoryInfo? s_rootLinkedStoreDirectoryInfo = SafeOpenLinkedRootDirectoryInfo();
        private static readonly FileInfo? s_rootStoreFileInfo = SafeOpenRootFileInfo();

        // Use non-Value-Tuple so that it's an atomic update.
        private static Tuple<SafeX509StackHandle, SafeX509StackHandle>? s_nativeCollections;
        private static DateTime s_directoryCertsLastWrite;
        private static DateTime s_linkCertsLastWrite;
        private static DateTime s_fileCertsLastWrite;

        private readonly bool _isRoot;

        private CachedSystemStoreProvider(bool isRoot)
        {
            _isRoot = isRoot;
        }

        internal static CachedSystemStoreProvider MachineRoot { get; } =
            new CachedSystemStoreProvider(true);

        internal static CachedSystemStoreProvider MachineIntermediate { get; } =
            new CachedSystemStoreProvider(false);


        public void Dispose()
        {
            // No-op
        }

        public void CloneTo(X509Certificate2Collection collection)
        {
            Tuple<SafeX509StackHandle, SafeX509StackHandle> nativeColls = GetCollections();
            SafeX509StackHandle nativeColl = _isRoot ? nativeColls.Item1 : nativeColls.Item2;

            int count = Interop.Crypto.GetX509StackFieldCount(nativeColl);

            for (int i = 0; i < count; i++)
            {
                X509Certificate2 clone = new X509Certificate2(Interop.Crypto.GetX509StackField(nativeColl, i));
                collection.Add(clone);
            }
        }

        internal static void GetNativeCollections(out SafeX509StackHandle root, out SafeX509StackHandle intermediate)
        {
            Tuple<SafeX509StackHandle, SafeX509StackHandle> nativeColls = GetCollections();
            root = nativeColls.Item1;
            intermediate = nativeColls.Item2;
        }

        public void Add(ICertificatePal cert)
        {
            // These stores can only be opened in ReadOnly mode.
            throw new InvalidOperationException();
        }

        public void Remove(ICertificatePal cert)
        {
            // These stores can only be opened in ReadOnly mode.
            throw new InvalidOperationException();
        }

        public SafeHandle? SafeHandle => null;

        private static Tuple<SafeX509StackHandle, SafeX509StackHandle> GetCollections()
        {
            TimeSpan elapsed = s_recheckStopwatch.Elapsed;
            Tuple<SafeX509StackHandle, SafeX509StackHandle>? ret = s_nativeCollections;

            if (ret == null || elapsed > s_lastWriteRecheckInterval)
            {
                lock (s_recheckStopwatch)
                {
                    FileInfo? fileInfo = s_rootStoreFileInfo;
                    DirectoryInfo? dirInfo = s_rootStoreDirectoryInfo;
                    DirectoryInfo? linkInfo = s_rootLinkedStoreDirectoryInfo;

                    fileInfo?.Refresh();
                    dirInfo?.Refresh();
                    linkInfo?.Refresh();

                    if (ret == null ||
                        elapsed > s_assumeInvalidInterval ||
                        (fileInfo != null && fileInfo.Exists && fileInfo.LastWriteTimeUtc != s_fileCertsLastWrite) ||
                        (dirInfo != null && dirInfo.Exists && dirInfo.LastWriteTimeUtc != s_directoryCertsLastWrite) ||
                        (linkInfo != null && linkInfo.Exists && linkInfo.LastWriteTimeUtc != s_linkCertsLastWrite))
                    {
                        ret = LoadMachineStores(dirInfo, fileInfo, linkInfo);
                    }
                }
            }

            Debug.Assert(ret != null);
            return ret;
        }

        private static Tuple<SafeX509StackHandle, SafeX509StackHandle> LoadMachineStores(
            DirectoryInfo? rootStorePath,
            FileInfo? rootStoreFile,
            DirectoryInfo? linkedRootPath)
        {
            Debug.Assert(
                Monitor.IsEntered(s_recheckStopwatch),
                "LoadMachineStores assumes a lock(s_recheckStopwatch)");

            SafeX509StackHandle rootStore = Interop.Crypto.NewX509Stack();
            Interop.Crypto.CheckValidOpenSslHandle(rootStore);
            SafeX509StackHandle intermedStore = Interop.Crypto.NewX509Stack();
            Interop.Crypto.CheckValidOpenSslHandle(intermedStore);

            DateTime newFileTime = default;
            DateTime newDirTime = default;
            DateTime newLinkTime = default;

            var uniqueRootCerts = new HashSet<X509Certificate2>();
            var uniqueIntermediateCerts = new HashSet<X509Certificate2>();

            if (rootStoreFile != null && rootStoreFile.Exists)
            {
                newFileTime = rootStoreFile.LastWriteTimeUtc;
                ProcessFile(rootStoreFile);
            }

            if (rootStorePath != null && rootStorePath.Exists)
            {
                newDirTime = rootStorePath.LastWriteTimeUtc;
                foreach (FileInfo file in rootStorePath.EnumerateFiles())
                {
                    ProcessFile(file);
                }
            }

            if (linkedRootPath != null && linkedRootPath.Exists)
            {
                newLinkTime = linkedRootPath.LastWriteTimeUtc;
            }

            void ProcessFile(FileInfo file)
            {
                using (SafeBioHandle fileBio = Interop.Crypto.BioNewFile(file.FullName, "rb"))
                {
                    // The handle may be invalid, for example when we don't have read permission for the file.
                    if (fileBio.IsInvalid)
                    {
                        Interop.Crypto.ErrClearError();
                        return;
                    }

                    // Some distros ship with two variants of the same certificate.
                    // One is the regular format ('BEGIN CERTIFICATE') and the other
                    // contains additional AUX-data ('BEGIN TRUSTED CERTIFICATE').
                    // The additional data contains the appropriate usage (e.g. emailProtection, serverAuth, ...).
                    // Because we don't validate for a specific usage, derived certificates are rejected.
                    // For now, we skip the certificates with AUX data and use the regular certificates.
                    ICertificatePal? pal;
                    while (OpenSslX509CertificateReader.TryReadX509PemNoAux(fileBio, out pal) ||
                        OpenSslX509CertificateReader.TryReadX509Der(fileBio, out pal))
                    {
                        X509Certificate2 cert = new X509Certificate2(pal);

                        // The HashSets are just used for uniqueness filters, they do not survive this method.
                        if (StringComparer.Ordinal.Equals(cert.Subject, cert.Issuer))
                        {
                            if (uniqueRootCerts.Add(cert))
                            {
                                using (SafeX509Handle tmp = Interop.Crypto.X509UpRef(pal.Handle))
                                {
                                    if (!Interop.Crypto.PushX509StackField(rootStore, tmp))
                                    {
                                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                                    }

                                    // The ownership has been transferred to the stack
                                    tmp.SetHandleAsInvalid();
                                }

                                continue;
                            }
                        }
                        else
                        {
                            if (uniqueIntermediateCerts.Add(cert))
                            {
                                using (SafeX509Handle tmp = Interop.Crypto.X509UpRef(pal.Handle))
                                {
                                    if (!Interop.Crypto.PushX509StackField(intermedStore, tmp))
                                    {
                                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                                    }

                                    // The ownership has been transferred to the stack
                                    tmp.SetHandleAsInvalid();
                                }

                                continue;
                            }
                        }

                        // There's a good chance we'll encounter duplicates on systems that have both one-cert-per-file
                        // and one-big-file trusted certificate stores. Anything that wasn't unique will end up here.
                        cert.Dispose();
                    }
                }
            }

            foreach (X509Certificate2 cert in uniqueRootCerts)
            {
                cert.Dispose();
            }

            foreach (X509Certificate2 cert in uniqueIntermediateCerts)
            {
                cert.Dispose();
            }

            Tuple<SafeX509StackHandle, SafeX509StackHandle> newCollections =
                Tuple.Create(rootStore, intermedStore);

            Debug.Assert(
                Monitor.IsEntered(s_recheckStopwatch),
                "LoadMachineStores assumes a lock(s_recheckStopwatch)");

            // The existing collections are not Disposed here, intentionally.
            // They could be in the gap between when they are returned from this method and not yet used
            // in a P/Invoke, which would result in exceptions being thrown.
            // In order to maintain "finalization-free" the GetNativeCollections method would need to
            // DangerousAddRef, and the callers would need to DangerousRelease, adding more interlocked operations
            // on every call.

            Volatile.Write(ref s_nativeCollections, newCollections);
            s_directoryCertsLastWrite = newDirTime;
            s_fileCertsLastWrite = newFileTime;
            s_linkCertsLastWrite = newLinkTime;
            s_recheckStopwatch.Restart();
            return newCollections;
        }

        private static FileInfo? SafeOpenRootFileInfo()
        {
            string? rootFile = Interop.Crypto.GetX509RootStoreFile();

            if (!string.IsNullOrEmpty(rootFile))
            {
                try
                {
                    return new FileInfo(rootFile);
                }
                catch (ArgumentException)
                {
                    // If SSL_CERT_FILE is set to the empty string, or anything else which gives
                    // "The path is not of a legal form", then the GetX509RootStoreFile value is ignored.
                }
            }

            return null;
        }

        private static DirectoryInfo? SafeOpenRootDirectoryInfo()
        {
            string? rootDirectory = Interop.Crypto.GetX509RootStorePath();

            if (!string.IsNullOrEmpty(rootDirectory))
            {
                try
                {
                    return new DirectoryInfo(rootDirectory);
                }
                catch (ArgumentException)
                {
                    // If SSL_CERT_DIR is set to the empty string, or anything else which gives
                    // "The path is not of a legal form", then the GetX509RootStoreFile value is ignored.
                }
            }

            return null;
        }

        private static DirectoryInfo? SafeOpenLinkedRootDirectoryInfo()
        {
            string? rootDirectory = Interop.Crypto.GetX509RootStorePath();

            if (!string.IsNullOrEmpty(rootDirectory))
            {
                string? linkedDirectory = Interop.Sys.ReadLink(rootDirectory);
                if (linkedDirectory == null)
                {
                    return null;
                }

                if (linkedDirectory[0] == '/')
                {
                    rootDirectory = linkedDirectory;
                }
                else
                {
                    // relative link
                    var root = new DirectoryInfo(rootDirectory);
                    root  = new DirectoryInfo(Path.Join(root.Parent?.FullName, linkedDirectory));
                    rootDirectory = root.FullName;
                }

                try
                {
                    return new DirectoryInfo(rootDirectory);
                }
                catch (ArgumentException)
                {
                    // If SSL_CERT_DIR is set to the empty string, or anything else which gives
                    // "The path is not of a legal form", then the GetX509RootStoreFile value is ignored.
                }
            }

            return null;
        }
    }
}
