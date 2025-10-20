// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
        private readonly object _enumerationLock = new();
        private readonly DirectoryInfoBase _directoryInfo;
        private readonly Matcher _matcher;
        private bool _changed;
        private DateTime _lastScanTimeUtc;
#if !NET
        private byte[]? _byteBuffer;
#endif
        private byte[]? _previousHash;
        private CancellationTokenSource? _tokenSource;
        private CancellationChangeToken? _changeToken;

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

        [DisallowNull]
        internal CancellationTokenSource? CancellationTokenSource
        {
            get => _tokenSource;
            set
            {
                Debug.Assert(_tokenSource == null, "We expect CancellationTokenSource to be initialized exactly once.");

                _tokenSource = value;
                _changeToken = new CancellationChangeToken(_tokenSource.Token);
            }
        }

        CancellationTokenSource? IPollingChangeToken.CancellationTokenSource => CancellationTokenSource;

        private IClock Clock { get; }

        /// <inheritdoc />
        public bool HasChanged
        {
            get
            {
                if (_changed)
                {
                    return true;
                }

                if (ShouldRefresh())
                {
                    lock (_enumerationLock)
                    {
                        if (!_changed && ShouldRefresh())
                        {
                            _changed = CalculateChanges();
                        }
                    }
                }

                return _changed;

                bool ShouldRefresh() => Clock.UtcNow - _lastScanTimeUtc >= PollingInterval;
            }
        }

        private bool CalculateChanges()
        {
            PatternMatchingResult result = _matcher.Execute(_directoryInfo);

            IOrderedEnumerable<FilePatternMatch> files = result.Files.OrderBy(f => f.Path, StringComparer.Ordinal);
            using (var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                foreach (FilePatternMatch file in files)
                {
                    DateTime lastWriteTimeUtc = GetLastWriteUtc(file.Path);
                    if (_lastScanTimeUtc.Ticks != 0 && _lastScanTimeUtc < lastWriteTimeUtc)
                    {
                        // _lastScanTimeUtc is the greatest timestamp that any last writes could have been.
                        // If a file has a newer timestamp than this value, it must've changed.
                        return true;
                    }

                    ComputeHash(sha256, file.Path, lastWriteTimeUtc);
                }

#if NET
                Span<byte> currentHash = stackalloc byte[256 / 8];
                sha256.GetHashAndReset(currentHash);
                if (_previousHash is null)
                {
                    _previousHash = currentHash.ToArray(); // First run
                }
                else if (!_previousHash.AsSpan().SequenceEqual(currentHash))
                {
                    return true;
                }
#else
                byte[] currentHash = sha256.GetHashAndReset();
                if (_previousHash is null)
                {
                    _previousHash = currentHash; // First run
                }
                else if (!_previousHash.AsSpan().SequenceEqual(currentHash.AsSpan()))
                {
                    return true;
                }
#endif

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
            string filePath = Path.Combine(_directoryInfo.FullName, path);
            return FileSystemInfoHelper.GetFileLinkTargetLastWriteTimeUtc(filePath) ?? File.GetLastWriteTimeUtc(filePath);
        }

#if NET
        private static void ComputeHash(IncrementalHash sha256, string path, DateTime lastChangedUtc)
        {
            sha256.AppendData(MemoryMarshal.AsBytes(path.AsSpan()));
            sha256.AppendData(MemoryMarshal.AsBytes([lastChangedUtc]));
        }
#else
        private void ComputeHash(IncrementalHash sha256, string path, DateTime lastChangedUtc)
        {
            int byteCount = path.Length * 2;
            if (_byteBuffer == null || byteCount > _byteBuffer.Length)
            {
                _byteBuffer = new byte[Math.Max(byteCount, 256)];
            }

            MemoryMarshal.AsBytes(path.AsSpan()).CopyTo(_byteBuffer.AsSpan());
            sha256.AppendData(_byteBuffer, 0, byteCount);

            BinaryPrimitives.WriteInt64LittleEndian(_byteBuffer, lastChangedUtc.Ticks);
            sha256.AppendData(_byteBuffer, 0, sizeof(long));
        }
#endif

        IDisposable IChangeToken.RegisterChangeCallback(Action<object?> callback, object? state)
        {
            if (!ActiveChangeCallbacks)
            {
                return EmptyDisposable.Instance;
            }

            return _changeToken!.RegisterChangeCallback(callback, state);
        }
    }
}
