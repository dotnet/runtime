// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Physical
{
    /// <summary>
    /// A change token that polls for file system changes.
    /// </summary>
    /// <remarks>
    /// <para>Polling occurs every 4 seconds.</para>
    /// <para>By default, this change token does not raise change callbacks. Callers should watch for <see cref="HasChanged" /> to turn
    /// from <see langword="false"/> to <see langword="true"/>.
    /// When <see cref="ActiveChangeCallbacks"/> is <see langword="true"/>, callbacks registered via
    /// <see cref="RegisterChangeCallback"/> will be invoked when the file or directory changes.</para>
    /// </remarks>
    public class PollingFileChangeToken : IPollingChangeToken
    {
        private readonly FileInfo _fileInfo;
        private DirectoryInfo? _directoryInfo;
        private DateTime _previousWriteTimeUtc;
        private DateTime _lastCheckedTimeUtc;
        private bool _hasChanged;
        private CancellationTokenSource? _tokenSource;
        private CancellationChangeToken? _changeToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="PollingFileChangeToken"/> class that polls the specified file or directory
        /// for changes as determined by <see cref="FileSystemInfo.LastWriteTimeUtc"/>.
        /// </summary>
        /// <param name="fileInfo">The <see cref="FileInfo"/> containing the path to poll.</param>
        public PollingFileChangeToken(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
            _previousWriteTimeUtc = GetLastWriteTimeUtc();
        }

        // Internal for unit testing
        internal static TimeSpan PollingInterval { get; set; } = PhysicalFilesWatcher.DefaultPollingInterval;

        private DateTime GetLastWriteTimeUtc()
        {
            _fileInfo.Refresh();

            if (_fileInfo.Exists)
            {
                return FileSystemInfoHelper.GetFileLinkTargetLastWriteTimeUtc(_fileInfo) ?? _fileInfo.LastWriteTimeUtc;
            }

            // This is not thread-safe, but that's not an issue since DirectoryInfos are cheap and interchangeable.
            _directoryInfo ??= new DirectoryInfo(_fileInfo.FullName);
            _directoryInfo.Refresh();

            if (_directoryInfo.Exists)
            {
                return _directoryInfo.LastWriteTimeUtc;
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Gets a value that indicates whether this token will proactively raise callbacks. If <see langword="false"/>, the token
        /// consumer must poll <see cref="HasChanged"/> to detect changes.
        /// </summary>
        public bool ActiveChangeCallbacks { get; internal set; }

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

        /// <summary>
        /// Gets a value that indicates whether the file or directory has changed since the change token was created.
        /// </summary>
        /// <remarks>
        /// Once the file or directory changes, this value is always <see langword="true"/>. Change tokens should not be reused once expired. The caller should discard this
        /// instance once it sees <see cref="HasChanged" /> is true.
        /// </remarks>
        public bool HasChanged
        {
            get
            {
                if (_hasChanged)
                {
                    return _hasChanged;
                }

                DateTime currentTime = DateTime.UtcNow;
                if (currentTime - _lastCheckedTimeUtc < PollingInterval)
                {
                    return _hasChanged;
                }

                DateTime lastWriteTimeUtc = GetLastWriteTimeUtc();
                if (_previousWriteTimeUtc != lastWriteTimeUtc)
                {
                    _previousWriteTimeUtc = lastWriteTimeUtc;
                    _hasChanged = true;
                }

                _lastCheckedTimeUtc = currentTime;
                return _hasChanged;
            }
        }

        /// <summary>
        /// Registers a callback that will be invoked when the token changes, if <see cref="ActiveChangeCallbacks"/> is <see langword="true"/>.
        /// If <see cref="ActiveChangeCallbacks"/> is <see langword="false"/>, no callback is registered and an empty disposable is returned.
        /// </summary>
        /// <param name="callback">The callback to invoke. This parameter is ignored when <see cref="ActiveChangeCallbacks"/> is <see langword="false"/>.</param>
        /// <param name="state">The state to pass to <paramref name="callback"/>. This parameter is ignored when <see cref="ActiveChangeCallbacks"/> is <see langword="false"/>.</param>
        /// <returns>A disposable object that no-ops when disposed.</returns>
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            if (!ActiveChangeCallbacks)
            {
                return EmptyDisposable.Instance;
            }

            return _changeToken!.RegisterChangeCallback(callback, state);
        }
    }
}
