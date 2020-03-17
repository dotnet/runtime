﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Physical
{
    /// <summary>
    /// A polling based <see cref="IChangeToken"/> for wildcard patterns.
    /// </summary>
    public class PollingWildCardChangeToken : IPollingChangeToken
    {
        private static readonly byte[] Separator = Encoding.Unicode.GetBytes("|");
        private readonly object _enumerationLock = new object();
        private readonly DirectoryInfoBase _directoryInfo;
        private readonly Matcher _matcher;
        private bool _changed;
        private DateTime? _lastScanTimeUtc;
        private byte[] _byteBuffer;
        private byte[] _previousHash;
        private CancellationTokenSource _tokenSource;
        private CancellationChangeToken _changeToken;

        /// <summary>
        /// Initializes a new instance of <see cref="PollingWildCardChangeToken"/>.
        /// </summary>
        /// <param name="root">The root of the file system.</param>
        /// <param name="pattern">The pattern to watch.</param>
        public PollingWildCardChangeToken(
            string root,
            string pattern)
            : this(
                new DirectoryInfoWrapper(new DirectoryInfo(root)),
                pattern,
                Physical.Clock.Instance)
        {
        }

        // Internal for unit testing.
        internal PollingWildCardChangeToken(
            DirectoryInfoBase directoryInfo,
            string pattern,
            IClock clock)
        {
            _directoryInfo = directoryInfo;
            Clock = clock;

            _matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            _matcher.AddInclude(pattern);
            CalculateChanges();
        }

        /// <inheritdoc />
        public bool ActiveChangeCallbacks { get; internal set; }

        // Internal for unit testing.
        internal TimeSpan PollingInterval { get; set; } = PhysicalFilesWatcher.DefaultPollingInterval;

        internal CancellationTokenSource CancellationTokenSource
        {
            get => _tokenSource;
            set
            {
                Debug.Assert(_tokenSource == null, "We expect CancellationTokenSource to be initialized exactly once.");

                _tokenSource = value;
                _changeToken = new CancellationChangeToken(_tokenSource.Token);
            }
        }

        CancellationTokenSource IPollingChangeToken.CancellationTokenSource => CancellationTokenSource;

        private IClock Clock { get; }

        /// <inheritdoc />
        public bool HasChanged
        {
            get
            {
                if (_changed)
                {
                    return _changed;
                }

                if (Clock.UtcNow - _lastScanTimeUtc >= PollingInterval)
                {
                    lock (_enumerationLock)
                    {
                        _changed = CalculateChanges();
                    }
                }

                return _changed;
            }
        }

        private bool CalculateChanges()
        {
            var result = _matcher.Execute(_directoryInfo);

            var files = result.Files.OrderBy(f => f.Path, StringComparer.Ordinal);
            using (var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                foreach (var file in files)
                {
                    var lastWriteTimeUtc = GetLastWriteUtc(file.Path);
                    if (_lastScanTimeUtc != null && _lastScanTimeUtc < lastWriteTimeUtc)
                    {
                        // _lastScanTimeUtc is the greatest timestamp that any last writes could have been.
                        // If a file has a newer timestamp than this value, it must've changed.
                        return true;
                    }

                    ComputeHash(sha256, file.Path, lastWriteTimeUtc);
                }

                var currentHash = sha256.GetHashAndReset();
                if (!ArrayEquals(_previousHash, currentHash))
                {
                    return true;
                }

                _previousHash = currentHash;
                _lastScanTimeUtc = Clock.UtcNow;
            }

            return false;
        }

        /// <summary>
        /// Gets the last write time of the file at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The root relative path.</param>
        /// <returns>The <see cref="DateTime"/> that the file was last modified.</returns>
        protected virtual DateTime GetLastWriteUtc(string path)
        {
            return File.GetLastWriteTimeUtc(Path.Combine(_directoryInfo.FullName, path));
        }

        private static bool ArrayEquals(byte[] previousHash, byte[] currentHash)
        {
            if (previousHash == null)
            {
                // First run
                return true;
            }

            Debug.Assert(previousHash.Length == currentHash.Length);
            for (var i = 0; i < previousHash.Length; i++)
            {
                if (previousHash[i] != currentHash[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void ComputeHash(IncrementalHash sha256, string path, DateTime lastChangedUtc)
        {
            var byteCount = Encoding.Unicode.GetByteCount(path);
            if (_byteBuffer == null || byteCount > _byteBuffer.Length)
            {
                _byteBuffer = new byte[Math.Max(byteCount, 256)];
            }

            var length = Encoding.Unicode.GetBytes(path, 0, path.Length, _byteBuffer, 0);
            sha256.AppendData(_byteBuffer, 0, length);
            sha256.AppendData(Separator, 0, Separator.Length);

            Debug.Assert(_byteBuffer.Length > sizeof(long));
            unsafe
            {
                fixed (byte* b = _byteBuffer)
                {
                    *((long*)b) = lastChangedUtc.Ticks;
                }
            }
            sha256.AppendData(_byteBuffer, 0, sizeof(long));
            sha256.AppendData(Separator, 0, Separator.Length);
        }

        IDisposable IChangeToken.RegisterChangeCallback(Action<object> callback, object state)
        {
            if (!ActiveChangeCallbacks)
            {
                return EmptyDisposable.Instance;
            }

            return _changeToken.RegisterChangeCallback(callback, state);
        }
    }
}
