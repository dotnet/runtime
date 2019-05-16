// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class CachedDirectoryStoreProvider
    {
        // These intervals are mostly arbitrary.
        // Prior to this caching these stores were always read "hot" from disk, and 30 seconds
        // seems like "long enough" for performance gains with "short enough" that if the filesystem
        // has LastWrite updating disabled that the process will be mostly responsive.
        private static readonly TimeSpan s_lastWriteRecheckInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_assumeInvalidInterval = TimeSpan.FromSeconds(30);

        private readonly Stopwatch _recheckStopwatch = new Stopwatch();
        private readonly DirectoryInfo _storeDirectoryInfo;

        private SafeX509StackHandle _nativeCollection;
        private DateTime _loadLastWrite;

        internal CachedDirectoryStoreProvider(string storeName)
        {
            string storePath = DirectoryBasedStoreProvider.GetStorePath(storeName);
            _storeDirectoryInfo = new DirectoryInfo(storePath);
        }

        internal SafeX509StackHandle GetNativeCollection()
        {
            SafeX509StackHandle ret = _nativeCollection;

            TimeSpan elapsed = _recheckStopwatch.Elapsed;

            if (ret == null || elapsed >= s_lastWriteRecheckInterval)
            {
                lock (_recheckStopwatch)
                {
                    _storeDirectoryInfo.Refresh();
                    DirectoryInfo info = _storeDirectoryInfo;

                    if (ret == null ||
                        elapsed >= s_assumeInvalidInterval ||
                        (info.Exists && info.LastWriteTimeUtc != _loadLastWrite))
                    {
                        SafeX509StackHandle newColl = Interop.Crypto.NewX509Stack();
                        Interop.Crypto.CheckValidOpenSslHandle(newColl);

                        if (info.Exists)
                        {
                            Interop.Crypto.X509StackAddDirectoryStore(newColl, info.FullName);
                            _loadLastWrite = info.LastWriteTimeUtc;
                        }


                        // The existing collection is not Disposed here, intentionally.
                        // It could be in the gap between when they are returned from this method and
                        // not yet used in a P/Invoke, which would result in an exception being thrown.
                        // In order to maintain "finalization-free" this method would need to always
                        // DangerousAddRef, and the callers would need to DangerousRelease,
                        // adding more interlocked operations on every call.
                        ret = newColl;
                        _nativeCollection = newColl;
                        _recheckStopwatch.Restart();
                    }
                }
            }

            Debug.Assert(ret != null);
            return ret;
        }
    }
}
