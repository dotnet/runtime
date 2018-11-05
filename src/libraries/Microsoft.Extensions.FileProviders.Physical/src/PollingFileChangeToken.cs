// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Physical
{
    /// <summary>
    ///     <para>
    ///     A change token that polls for file system changes.
    ///     </para>
    ///     <para>
    ///     This change token does not raise any change callbacks. Callers should watch for <see cref="HasChanged" /> to turn
    ///     from false to true
    ///     and dispose the token after this happens.
    ///     </para>
    /// </summary>
    /// <remarks>
    /// Polling occurs every 4 seconds.
    /// </remarks>
    public class PollingFileChangeToken : IPollingChangeToken
    {
        private readonly FileInfo _fileInfo;
        private DateTime _previousWriteTimeUtc;
        private DateTime _lastCheckedTimeUtc;
        private bool _hasChanged;
        private CancellationTokenSource _tokenSource;
        private CancellationChangeToken _changeToken;

        /// <summary>
        /// Initializes a new instance of <see cref="PollingFileChangeToken" /> that polls the specified file for changes as
        /// determined by <see cref="System.IO.FileSystemInfo.LastWriteTimeUtc" />.
        /// </summary>
        /// <param name="fileInfo">The <see cref="System.IO.FileInfo"/> to poll</param>
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
            return _fileInfo.Exists ? _fileInfo.LastWriteTimeUtc : DateTime.MinValue;
        }

        /// <summary>
        /// Always false.
        /// </summary>
        public bool ActiveChangeCallbacks { get; internal set; }

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

        /// <summary>
        /// True when the file has changed since the change token was created. Once the file changes, this value is always true
        /// </summary>
        /// <remarks>
        /// Once true, the value will always be true. Change tokens should not re-used once expired. The caller should discard this
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

                var currentTime = DateTime.UtcNow;
                if (currentTime - _lastCheckedTimeUtc < PollingInterval)
                {
                    return _hasChanged;
                }

                var lastWriteTimeUtc = GetLastWriteTimeUtc();
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
        /// Does not actually register callbacks.
        /// </summary>
        /// <param name="callback">This parameter is ignored</param>
        /// <param name="state">This parameter is ignored</param>
        /// <returns>A disposable object that noops when disposed</returns>
        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            if (!ActiveChangeCallbacks)
            {
                return EmptyDisposable.Instance;
            }

            return _changeToken.RegisterChangeCallback(callback, state);
        }
    }
}