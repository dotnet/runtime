// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class OpenSslCachedSystemStoreProvider : IStorePal
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
        private static string[]? s_rootStoreDirectories;
        private static bool s_defaultRootDir;
        private static string? s_rootStoreFile;
        private static DateTime[]? s_directoryLastWrite;
        private static DateTime s_fileLastWrite;

        // Use non-Value-Tuple so that it's an atomic update.
        private static Tuple<SafeX509StackHandle, SafeX509StackHandle>? s_nativeCollections;

        private readonly bool _isRoot;

        private OpenSslCachedSystemStoreProvider(bool isRoot)
        {
            _isRoot = isRoot;
        }

        internal static OpenSslCachedSystemStoreProvider MachineRoot { get; } =
            new OpenSslCachedSystemStoreProvider(true);

        internal static OpenSslCachedSystemStoreProvider MachineIntermediate { get; } =
            new OpenSslCachedSystemStoreProvider(false);


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
                    if (ret == null ||
                        elapsed > s_assumeInvalidInterval ||
                        LastWriteTimesHaveChanged())
                    {
                        ret = LoadMachineStores();
                    }
                }
            }

            Debug.Assert(ret != null);
            return ret;
        }

        private static bool LastWriteTimesHaveChanged()
        {
            Debug.Assert(
                Monitor.IsEntered(s_recheckStopwatch),
                "LastWriteTimesHaveChanged assumes a lock(s_recheckStopwatch)");

            if (s_rootStoreFile != null)
            {
                _ = TryStatFile(s_rootStoreFile, out DateTime lastModified);
                if (lastModified != s_fileLastWrite)
                {
                    return true;
                }
            }

            if (s_rootStoreDirectories != null && s_directoryLastWrite != null)
            {
                for (int i = 0; i < s_rootStoreDirectories.Length; i++)
                {
                    _ = TryStatDirectory(s_rootStoreDirectories[i], out DateTime lastModified);
                    if (lastModified != s_directoryLastWrite[i])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Tuple<SafeX509StackHandle, SafeX509StackHandle> LoadMachineStores()
        {
            Debug.Assert(
                Monitor.IsEntered(s_recheckStopwatch),
                "LoadMachineStores assumes a lock(s_recheckStopwatch)");

            SafeX509StackHandle rootStore = Interop.Crypto.NewX509Stack();
            Interop.Crypto.CheckValidOpenSslHandle(rootStore);
            SafeX509StackHandle intermedStore = Interop.Crypto.NewX509Stack();
            Interop.Crypto.CheckValidOpenSslHandle(intermedStore);

            var uniqueRootCerts = new HashSet<X509Certificate2>();
            var uniqueIntermediateCerts = new HashSet<X509Certificate2>();
            bool firstLoad = (s_nativeCollections == null);

            if (firstLoad)
            {
                s_rootStoreDirectories = GetRootStoreDirectories(out s_defaultRootDir);
                s_directoryLastWrite = new DateTime[s_rootStoreDirectories.Length];
                s_rootStoreFile = GetRootStoreFile();
            }
            else
            {
                Debug.Assert(s_rootStoreDirectories is not null);
                Debug.Assert(s_directoryLastWrite is not null);
            }

            if (s_rootStoreFile != null)
            {
                ProcessFile(s_rootStoreFile, out s_fileLastWrite);
            }

            bool hasStoreData = false;

            for (int i = 0; i < s_rootStoreDirectories.Length; i++)
            {
                hasStoreData = ProcessDir(s_rootStoreDirectories[i], out s_directoryLastWrite[i]);
            }

            if (firstLoad && !hasStoreData && s_defaultRootDir)
            {
                const string DefaultCertDir = "/etc/ssl/certs";
                hasStoreData = ProcessDir(DefaultCertDir, out DateTime lastModified);
                if (hasStoreData)
                {
                    s_rootStoreDirectories = new[] { DefaultCertDir };
                    s_directoryLastWrite = new[] { lastModified };
                }
            }

            bool ProcessDir(string dir, out DateTime lastModified)
            {
                if (!TryStatDirectory(dir, out lastModified))
                {
                    return false;
                }

                bool hasStoreData = false;

                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    hasStoreData |= ProcessFile(file, out _, skipStat: true);
                }

                return hasStoreData;
            }

            bool ProcessFile(string file, out DateTime lastModified, bool skipStat = false)
            {
                bool readData = false;

                if (skipStat)
                {
                    lastModified = default;
                }
                else if (!TryStatFile(file, out lastModified))
                {
                    return false;
                }

                using (SafeBioHandle fileBio = Interop.Crypto.BioNewFile(file, "rb"))
                {
                    // The handle may be invalid, for example when we don't have read permission for the file.
                    if (fileBio.IsInvalid)
                    {
                        Interop.Crypto.ErrClearError();
                        return false;
                    }

                    // Some distros ship with two variants of the same certificate.
                    // One is the regular format ('BEGIN CERTIFICATE') and the other
                    // contains additional AUX-data ('BEGIN TRUSTED CERTIFICATE').
                    // The additional data contains the appropriate usage (e.g. emailProtection, serverAuth, ...).
                    // Because we don't validate for a specific usage, derived certificates are rejected.
                    // For now, we skip the certificates with AUX data and use the regular certificates.
                    ICertificatePal? pal;
                    while (OpenSslX509CertificateReader.TryReadX509PemNoAux(fileBio, out pal))
                    {
                        readData = true;
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

                return readData;
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
            s_recheckStopwatch.Restart();
            return newCollections;
        }

        private static string? GetRootStoreFile()
        {
            string? rootFile = Interop.Crypto.GetX509RootStoreFile();

            if (!string.IsNullOrEmpty(rootFile))
            {
                return Path.GetFullPath(rootFile);
            }

            return null;
        }

        private static string[] GetRootStoreDirectories(out bool isDefault)
        {
            string rootDirectory = Interop.Crypto.GetX509RootStorePath(out isDefault) ?? "";

            string[] directories = rootDirectory.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < directories.Length; i++)
            {
                directories[i] = Path.GetFullPath(directories[i]);
            }

            // Remove duplicates.
            if (directories.Length > 1)
            {
                var set = new HashSet<string>(directories, StringComparer.Ordinal);
                if (set.Count != directories.Length)
                {
                    // Preserve the original order.
                    string[] directoriesTrimmed = new string[set.Count];
                    int j = 0;
                    for (int i = 0; i < directories.Length; i++)
                    {
                        string directory = directories[i];
                        if (set.Remove(directory))
                        {
                            directoriesTrimmed[j++] = directory;
                        }
                    }
                    Debug.Assert(set.Count == 0);
                    directories = directoriesTrimmed;
                }
            }

            return directories;
        }

        private static bool TryStatFile(string path, out DateTime lastModified)
            => TryStat(path, Interop.Sys.FileTypes.S_IFREG, out lastModified);

        private static bool TryStatDirectory(string path, out DateTime lastModified)
            => TryStat(path, Interop.Sys.FileTypes.S_IFDIR, out lastModified);

        private static bool TryStat(string path, int fileType, out DateTime lastModified)
        {
            lastModified = default;

            Interop.Sys.FileStatus status;
            // Use Stat to follow links.
            if (Interop.Sys.Stat(path, out status) < 0 ||
                (status.Mode & Interop.Sys.FileTypes.S_IFMT) != fileType)
            {
                return false;
            }

            lastModified = DateTime.UnixEpoch + TimeSpan.FromTicks(status.MTime * TimeSpan.TicksPerSecond + status.MTimeNsec / TimeSpan.NanosecondsPerTick);
            return true;
        }
    }
}
