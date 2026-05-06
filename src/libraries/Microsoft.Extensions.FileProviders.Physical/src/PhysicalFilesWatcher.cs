// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders.Physical.Internal;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Physical
{
    /// <summary>
    /// Watches a physical file system for changes and triggers events on
    /// <see cref="IChangeToken" /> when files or directories are created, changed, renamed, or deleted.
    /// </summary>
    public class PhysicalFilesWatcher : IDisposable
    {
        private static readonly Action<object?> _cancelTokenSource = state => ((CancellationTokenSource?)state)!.Cancel();

        internal static TimeSpan DefaultPollingInterval = TimeSpan.FromSeconds(4);

        private readonly ConcurrentDictionary<string, ChangeTokenInfo> _filePathTokenLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, ChangeTokenInfo> _wildcardTokenLookup = new(StringComparer.OrdinalIgnoreCase);

        private readonly FileSystemWatcher? _fileWatcher;
        private readonly object _fileWatcherLock = new();
        private readonly string _root;
        private readonly ExclusionFilters _filters;

        // A single non-recursive watcher used when _root does not exist.
        // Watches for _root to be created, then enables the main FileSystemWatcher.
        private PendingCreationWatcher? _rootCreationWatcher;
        private readonly object _rootCreationWatcherLock = new();
        private bool _rootWasUnavailable;

        private Timer? _timer;
        private bool _timerInitialized;
        private object _timerLock = new();
        private readonly Func<Timer> _timerFactory;
        private volatile bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhysicalFilesWatcher"/> class that watches files in <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The root directory for the watcher.</param>
        /// <param name="fileSystemWatcher">The wrapped watcher that's watching <paramref name="root"/>.</param>
        /// <param name="pollForChanges">
        /// <see langword="true"/> for the poller to use polling to trigger instances of
        /// <see cref="IChangeToken"/> created by <see cref="CreateFileChangeToken(string)"/>; otherwise, <see langword="false"/>.
        /// </param>
        public PhysicalFilesWatcher(
            string root,
            FileSystemWatcher? fileSystemWatcher,
            bool pollForChanges)
            : this(root, fileSystemWatcher, pollForChanges, ExclusionFilters.Sensitive)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhysicalFilesWatcher"/> class that watches files in <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The root directory for the watcher.</param>
        /// <param name="fileSystemWatcher">The wrapped watcher that is watching <paramref name="root"/>.</param>
        /// <param name="pollForChanges">
        /// <see langword="true"/> for the poller to use polling to trigger instances of
        /// <see cref="IChangeToken"/> created by <see cref="CreateFileChangeToken(string)"/>; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="filters">A bitwise combination of the enumeration values that specifies which files or directories are excluded. Notifications of changes to these are not raised.</param>
        public PhysicalFilesWatcher(
            string root,
            FileSystemWatcher? fileSystemWatcher,
            bool pollForChanges,
            ExclusionFilters filters)
        {
            if (fileSystemWatcher == null && !pollForChanges)
            {
                throw new ArgumentNullException(nameof(fileSystemWatcher), SR.Error_FileSystemWatcherRequiredWithoutPolling);
            }

            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _root = PathUtils.EnsureTrailingSlash(Path.GetFullPath(root));

            if (fileSystemWatcher != null)
            {
#if NET
                if (OperatingSystem.IsBrowser() || OperatingSystem.IsWasi() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()) || OperatingSystem.IsTvOS())
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.FileSystemWatcher_PlatformNotSupported, typeof(FileSystemWatcher)));
                }
#endif

                string fswPath = fileSystemWatcher.Path;
                if (fswPath.Length > 0)
                {
                    string watcherFullPath = PathUtils.EnsureTrailingSlash(Path.GetFullPath(fswPath));

                    if (!_root.StartsWith(watcherFullPath, StringComparison.OrdinalIgnoreCase) &&
                        !watcherFullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException(SR.Format(SR.FileSystemWatcherPathError, watcherFullPath, _root), nameof(fileSystemWatcher));
                    }
                }

                _fileWatcher = fileSystemWatcher;
                _fileWatcher.IncludeSubdirectories = true;
                _fileWatcher.Created += OnChanged;
                _fileWatcher.Changed += OnChanged;
                _fileWatcher.Renamed += OnRenamed;
                _fileWatcher.Deleted += OnChanged;
                _fileWatcher.Error += OnError;
            }

            PollForChanges = pollForChanges;
            _filters = filters;

            PollingChangeTokens = new ConcurrentDictionary<IPollingChangeToken, IPollingChangeToken>();
            _timerFactory = () => NonCapturingTimer.Create(RaiseChangeEvents, state: PollingChangeTokens, dueTime: TimeSpan.Zero, period: DefaultPollingInterval);
        }

        internal bool PollForChanges { get; }

        internal bool UseActivePolling { get; set; }

        internal ConcurrentDictionary<IPollingChangeToken, IPollingChangeToken> PollingChangeTokens { get; }

        /// <summary>
        /// Creates an instance of <see cref="IChangeToken" /> for all files and directories that match the
        /// <paramref name="filter" />.
        /// </summary>
        /// <param name="filter">A globbing pattern for files and directories to watch.</param>
        /// <returns>A change token for all files and directories that match the filter.</returns>
        /// <remarks>
        /// Globbing patterns are relative to the root directory given in the constructor
        /// <see cref="PhysicalFilesWatcher(string, FileSystemWatcher, bool)" />. Globbing patterns
        /// are interpreted by <see cref="Matcher" />.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="filter" /> is <see langword="null"/>.</exception>
        public IChangeToken CreateFileChangeToken(string filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            filter = NormalizePath(filter);

            // Absolute paths and paths traversing above root not permitted.
            if (Path.IsPathRooted(filter) || PathUtils.PathNavigatesAboveRoot(filter))
            {
                return NullChangeToken.Singleton;
            }

            IChangeToken changeToken = GetOrAddChangeToken(filter);

            TryEnableFileSystemWatcher();

            return changeToken;
        }

        private IChangeToken GetOrAddChangeToken(string pattern)
        {
            if (UseActivePolling)
            {
                LazyInitializer.EnsureInitialized(ref _timer, ref _timerInitialized, ref _timerLock, _timerFactory);
            }

            return pattern.Contains('*') || IsDirectoryPath(pattern)
                ? GetOrAddWildcardChangeToken(pattern)
                // get rid of \. on Windows and ./ on UNIX at the start of the path
                : GetOrAddFilePathChangeToken(RemoveRelativePathSegment(pattern));
        }

        private static string RemoveRelativePathSegment(string pattern) =>
            // The pattern has already been normalized to unix directory separators
            pattern.StartsWith("./", StringComparison.Ordinal) ? pattern.Substring(2) : pattern;

        internal IChangeToken GetOrAddFilePathChangeToken(string filePath)
        {
            if (!_filePathTokenLookup.TryGetValue(filePath, out ChangeTokenInfo tokenInfo))
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var cancellationChangeToken = new CancellationChangeToken(cancellationTokenSource.Token);
                tokenInfo = new ChangeTokenInfo(cancellationTokenSource, cancellationChangeToken);
                tokenInfo = _filePathTokenLookup.GetOrAdd(filePath, tokenInfo);
            }

            IChangeToken changeToken = tokenInfo.ChangeToken;

            if (PollForChanges)
            {
                // The expiry of CancellationChangeToken is controlled by this type and consequently we can cache it.
                // PollingFileChangeToken on the other hand manages its own lifetime and consequently we cannot cache it.
                var pollingChangeToken = new PollingFileChangeToken(new FileInfo(Path.Combine(_root, filePath)));

                if (UseActivePolling)
                {
                    pollingChangeToken.ActiveChangeCallbacks = true;
                    pollingChangeToken.CancellationTokenSource = new CancellationTokenSource();
                    PollingChangeTokens.TryAdd(pollingChangeToken, pollingChangeToken);
                }

                changeToken = new CompositeChangeToken(
                    new[]
                    {
                        changeToken,
                        pollingChangeToken,
                    });
            }

            return changeToken;
        }

        internal IChangeToken GetOrAddWildcardChangeToken(string pattern)
        {
            if (!_wildcardTokenLookup.TryGetValue(pattern, out ChangeTokenInfo tokenInfo))
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var cancellationChangeToken = new CancellationChangeToken(cancellationTokenSource.Token);
                var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                matcher.AddInclude(pattern);
                tokenInfo = new ChangeTokenInfo(cancellationTokenSource, cancellationChangeToken, matcher);
                tokenInfo = _wildcardTokenLookup.GetOrAdd(pattern, tokenInfo);
            }

            IChangeToken changeToken = tokenInfo.ChangeToken;
            if (PollForChanges)
            {
                // The expiry of CancellationChangeToken is controlled by this type and consequently we can cache it.
                // PollingFileChangeToken on the other hand manages its own lifetime and consequently we cannot cache it.
                var pollingChangeToken = new PollingWildCardChangeToken(_root, pattern);

                if (UseActivePolling)
                {
                    pollingChangeToken.ActiveChangeCallbacks = true;
                    pollingChangeToken.CancellationTokenSource = new CancellationTokenSource();
                    PollingChangeTokens.TryAdd(pollingChangeToken, pollingChangeToken);
                }

                changeToken = new CompositeChangeToken(
                    new[]
                    {
                        changeToken,
                        pollingChangeToken,
                    });
            }

            return changeToken;
        }

        /// <summary>
        /// Disposes the provider. Change tokens may not trigger after the provider is disposed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the provider.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if invoked from <see cref="IDisposable.Dispose"/>; otherwise, <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing)
                {
                    lock (_fileWatcherLock)
                    {
                        _fileWatcher?.Dispose();
                    }

                    _timer?.Dispose();

                    // We made sure that browser/iOS/tvOS never uses FileSystemWatcher so _rootCreationWatcher is always null on those platforms.
#pragma warning disable CA1416
                    lock (_rootCreationWatcherLock)
                    {
                        _rootCreationWatcher?.Dispose();
                        _rootCreationWatcher = null;
                    }
#pragma warning restore CA1416
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            // For a file name change or a directory's name change notify registered tokens.
            OnFileSystemEntryChange(e.OldFullPath);
            OnFileSystemEntryChange(e.FullPath);

            if (Directory.Exists(e.FullPath))
            {
                try
                {
                    // If the renamed entity is a directory then notify tokens for every sub item.
                    foreach (
                        string newLocation in
                        Directory.EnumerateFileSystemEntries(e.FullPath, "*", SearchOption.AllDirectories))
                    {
                        // Calculated previous path of this moved item.
                        string oldLocation = Path.Combine(e.OldFullPath, newLocation.Substring(e.FullPath.Length + 1));
                        OnFileSystemEntryChange(oldLocation);
                        OnFileSystemEntryChange(newLocation);
                    }
                }
                catch (Exception ex) when (ex is IOException or SecurityException or DirectoryNotFoundException or UnauthorizedAccessException)
                {
                    // Swallow the exception.
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            OnFileSystemEntryChange(e.FullPath);
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private void OnError(object sender, ErrorEventArgs e)
        {
            // Notify all cache entries on error.
            CancelAll(_filePathTokenLookup);
            CancelAll(_wildcardTokenLookup);

            TryDisableFileSystemWatcher();

            static void CancelAll(ConcurrentDictionary<string, ChangeTokenInfo> tokens)
            {
                foreach (KeyValuePair<string, ChangeTokenInfo> entry in tokens)
                {
                    if (tokens.TryRemove(entry.Key, out ChangeTokenInfo matchInfo))
                    {
                        CancelToken(matchInfo);
                    }
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private void OnFileSystemEntryChange(string fullPath)
        {
            try
            {
                // Ignore events outside _root (can happen when the FSW watches an ancestor directory).
                if (!fullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // If it's an existing file, we need FileInfo; if it's an existing directory, we need DirectoryInfo; if it doesn't exist, either is fine.
                FileSystemInfo fileSystemInfo = new FileInfo(fullPath) is { Exists: true } fileInfo
                    ? fileInfo
                    : new DirectoryInfo(fullPath);

                if (FileSystemInfoHelper.IsExcluded(fileSystemInfo, _filters))
                {
                    return;
                }

                string relativePath = fullPath.Substring(_root.Length);
                ReportChangeForMatchedEntries(relativePath);
            }
            catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
            {
                // Swallow the exception.
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private void ReportChangeForMatchedEntries(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // System.IO.FileSystemWatcher may trigger events that are missing the file name,
                // which makes it appear as if the root directory is renamed or deleted. Moving the root directory
                // of the file watcher is not supported, so this type of event is ignored.
                return;
            }

            path = NormalizePath(path);

            bool matched = false;
            if (_filePathTokenLookup.TryRemove(path, out ChangeTokenInfo matchInfo))
            {
                CancelToken(matchInfo);
                matched = true;
            }

            foreach (KeyValuePair<string, ChangeTokenInfo> wildCardEntry in _wildcardTokenLookup)
            {
                PatternMatchingResult matchResult = wildCardEntry.Value.Matcher!.Match(path);
                if (matchResult.HasMatches &&
                    _wildcardTokenLookup.TryRemove(wildCardEntry.Key, out matchInfo))
                {
                    CancelToken(matchInfo);
                    matched = true;
                }
            }

            if (matched)
            {
                TryDisableFileSystemWatcher();
            }
        }

        // Called after enabling the FileSystemWatcher following a period where _root did not exist.
        // Fires tokens for any files already existing on disk, to cover the gap between
        // _root appearing and the FSW becoming active.
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private void ReportExistingWatchedEntries()
        {
            if ((!_filePathTokenLookup.IsEmpty || !_wildcardTokenLookup.IsEmpty) && Directory.Exists(_root))
            {
                try
                {
                    // This iterates through all file system entries under the root directory, which can be expensive if there are many of them.
                    // However, this is only called if the root directory was missing and was just created, so it is expected that there won't be many entries at this point.
                    foreach (string fullPath in
                        Directory.EnumerateFileSystemEntries(_root, "*", SearchOption.AllDirectories))
                    {
                        OnFileSystemEntryChange(fullPath);

                        if (_filePathTokenLookup.IsEmpty && _wildcardTokenLookup.IsEmpty)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or SecurityException or DirectoryNotFoundException or UnauthorizedAccessException)
                {
                    // Swallow - the directory may have been deleted or become inaccessible.
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private void TryDisableFileSystemWatcher()
        {
            if (_fileWatcher != null)
            {
                lock (_fileWatcherLock)
                {
                    if (_filePathTokenLookup.IsEmpty &&
                        _wildcardTokenLookup.IsEmpty &&
                        _fileWatcher.EnableRaisingEvents)
                    {
                        // Perf: Turn off the file monitoring if no files or directories to monitor.
                        _fileWatcher.EnableRaisingEvents = false;
                    }
                }
            }

            lock (_rootCreationWatcherLock)
            {
                if (_rootCreationWatcher != null &&
                    _filePathTokenLookup.IsEmpty &&
                    _wildcardTokenLookup.IsEmpty)
                {
                    _rootCreationWatcher.Dispose();
                    _rootCreationWatcher = null;
                }
            }
        }

        private void TryEnableFileSystemWatcher()
        {
            if (_fileWatcher is null)
            {
                return;
            }

// We made sure that browser/iOS/tvOS never uses FileSystemWatcher.
#pragma warning disable CA1416 // Validate platform compatibility
            bool needsRootWatcher = false;
            bool justEnabledAfterRootCreated = false;

            lock (_fileWatcherLock)
            {
                if (_disposed)
                {
                    return;
                }

                if (!_filePathTokenLookup.IsEmpty || !_wildcardTokenLookup.IsEmpty)
                {
                    bool rootExists = Directory.Exists(_root);

                    // On some platforms (e.g., Linux), FileSystemWatcher currently does not
                    // invoke OnError when the watched directory is deleted, so we don't disable
                    // the FSW and start root watcher at that point.
                    // Detect and handle this opportunistically now.
                    if (_fileWatcher.EnableRaisingEvents && !rootExists)
                    {
                        _fileWatcher.EnableRaisingEvents = false;
                    }

                    if (!_fileWatcher.EnableRaisingEvents)
                    {
                        if (!rootExists)
                        {
                            needsRootWatcher = true;
                            _rootWasUnavailable = true;
                        }
                        else
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(_fileWatcher.Path))
                                {
                                    _fileWatcher.Path = _root;
                                }

                                _fileWatcher.EnableRaisingEvents = true;

                                // Only scan for existing entries if the FSW was enabled after _root
                                // was initially missing (i.e. we went through the PCW path). In the
                                // normal case where _root always existed, there is no gap to cover.
                                justEnabledAfterRootCreated = _rootWasUnavailable;
                                _rootWasUnavailable = false;
                            }
                            catch (Exception ex) when (ex is ArgumentException or IOException)
                            {
                                // _root may have been deleted between the Directory.Exists check
                                // and the property sets above. Fall back to watching for root creation.
                                if (!Directory.Exists(_root))
                                {
                                    needsRootWatcher = true;
                                    _rootWasUnavailable = true;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
            }

            if (needsRootWatcher)
            {
                // Call outside the lock - EnsureRootCreationWatcher may invoke Token.Register,
                // which can fire synchronously and re-enter TryEnableFileSystemWatcher.
                EnsureRootCreationWatcher();
            }
            else if (justEnabledAfterRootCreated)
            {
                // After enabling the FSW following a root-missing period, check for entries that
                // were created before the watcher was active. Without this, files created between
                // _root appearing and the FSW being enabled would be missed.

                ReportExistingWatchedEntries();

                lock (_rootCreationWatcherLock)
                {
                    _rootCreationWatcher?.Dispose();
                    _rootCreationWatcher = null;
                }
            }
#pragma warning restore CA1416
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private void EnsureRootCreationWatcher()
        {
            PendingCreationWatcher? newWatcher = null;

            lock (_rootCreationWatcherLock)
            {
                if (_rootCreationWatcher is { } existing && !existing.Token.IsCancellationRequested)
                {
                    return; // already watching
                }

                _rootCreationWatcher?.Dispose();

                newWatcher = new PendingCreationWatcher(_root);
                _rootCreationWatcher = newWatcher;
            }

            // If the token is already cancelled (e.g. setup error),
            // don't register a callback - it would fire synchronously and could cause a tight retry loop.
            if (newWatcher.Token.IsCancellationRequested)
            {
                // If _root appeared during setup, enable the FSW now.
                if (Directory.Exists(_root))
                {
                    TryEnableFileSystemWatcher();
                }

                return;
            }

            // Register outside the lock to avoid deadlocking with TryEnableFileSystemWatcher
            // if the token gets cancelled (synchronous callback invocation).
            try
            {
                newWatcher.Token.Register(_ => TryEnableFileSystemWatcher(), null);
            }
            catch (ObjectDisposedException)
            {
                // Catch ObjectDisposedException in case PhysicalFilesWatcher.Dispose() ran
                // concurrently and disposed the PendingCreationWatcher between releasing the
                // lock and this Register call.

                // This can only happen on .NET Framework with the ThrowExceptionIfDisposedCancellationTokenSource compat switch on.
            }
        }

        private static string NormalizePath(string filter) => filter.Replace('\\', '/');

        private static bool IsDirectoryPath(string path)
        {
            return path.Length > 0 &&
                (path[path.Length - 1] == Path.DirectorySeparatorChar ||
                path[path.Length - 1] == Path.AltDirectorySeparatorChar);
        }

        private static void CancelToken(ChangeTokenInfo matchInfo)
        {
            if (matchInfo.TokenSource.IsCancellationRequested)
            {
                return;
            }

            Task.Factory.StartNew(
                _cancelTokenSource,
                matchInfo.TokenSource,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }

        internal static void RaiseChangeEvents(object? state)
        {
            Debug.Assert(state != null);

            // Iterating over a concurrent bag gives us a point in time snapshot making it safe
            // to remove items from it.
            var changeTokens = (ConcurrentDictionary<IPollingChangeToken, IPollingChangeToken>)state;
            foreach (KeyValuePair<IPollingChangeToken, IPollingChangeToken> item in changeTokens)
            {
                IPollingChangeToken token = item.Key;

                if (!token.HasChanged)
                {
                    continue;
                }

                if (!changeTokens.TryRemove(token, out _))
                {
                    // Move on if we couldn't remove the item.
                    continue;
                }

                // We're already on a background thread, don't need to spawn a background Task to cancel the CTS
                try
                {
                    token.CancellationTokenSource!.Cancel();
                }
                catch
                {

                }
            }
        }

        private readonly struct ChangeTokenInfo
        {
            public ChangeTokenInfo(
                CancellationTokenSource tokenSource,
                CancellationChangeToken changeToken)
                : this(tokenSource, changeToken, matcher: null)
            {
            }

            public ChangeTokenInfo(
                CancellationTokenSource tokenSource,
                CancellationChangeToken changeToken,
                Matcher? matcher)
            {
                TokenSource = tokenSource;
                ChangeToken = changeToken;
                Matcher = matcher;
            }

            public CancellationTokenSource TokenSource { get; }

            public CancellationChangeToken ChangeToken { get; }

            public Matcher? Matcher { get; }
        }

        // Watches for a non-existent directory to be created. Walks up from the target
        // directory to find the nearest existing ancestor, then uses a non-recursive
        // FileSystemWatcher to detect each missing directory level being created.
        // When the target directory itself appears, the token fires.
        private sealed class PendingCreationWatcher : IDisposable
        {
            private readonly string _targetDirectory;
            private FileSystemWatcher? _watcher; // protected by _lock
            private string _expectedName;        // protected by _lock
            private readonly object _lock = new();
            private int _disposed;

            private readonly CancellationTokenSource _cts = new();

            public CancellationToken Token { get; }

            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            public PendingCreationWatcher(string directory)
            {
                Token = _cts.Token;
                _targetDirectory = directory;

                // Walk up to find the nearest existing ancestor.
                string current = _targetDirectory;

                while (!Directory.Exists(current))
                {
                    // If no existing ancestor was found (e.g. unmounted drive on Windows), throw.
                    current = Path.GetDirectoryName(current) ?? throw new DirectoryNotFoundException(SR.Format(SR.RootDirectoryMissing, current));
                }

                // current is the deepest existing ancestor; _expectedName is its immediate child.
                _expectedName = GetChildName(current, _targetDirectory);

                bool shouldCancel;

                lock (_lock)
                {
                    shouldCancel = SetupWatcherNoLock(current);
                }

                if (shouldCancel)
                {
                    _cts.Cancel();
                }
            }

            // Returns the name of the immediate child of existingAncestor on the path to target.
            private static string GetChildName(string existingAncestor, string target)
            {
                Debug.Assert(target.StartsWith(existingAncestor, StringComparison.OrdinalIgnoreCase));

                ReadOnlySpan<char> remaining = target.AsSpan(existingAncestor.Length).TrimStart(PathUtils.PathSeparators);
                int separator = remaining.IndexOfAny(PathUtils.PathSeparators);
                return (separator >= 0 ? remaining.Slice(0, separator) : remaining).ToString();
            }

            private bool IsTargetDirectory(string path)
            {
                // _targetDirectory may have a trailing separator; compare ignoring it.
                ReadOnlySpan<char> target = _targetDirectory.AsSpan().TrimEnd(PathUtils.PathSeparators);
                return target.Equals(path.AsSpan().TrimEnd(PathUtils.PathSeparators), StringComparison.OrdinalIgnoreCase);
            }

            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            // Returns true if _cts should be cancelled after releasing the lock.
            private bool SetupWatcherNoLock(string watchDir)
            {
                while (true)
                {
                    // Fast-forward through directories that already exist.
                    while (Path.Combine(watchDir, _expectedName) is var childPath && Directory.Exists(childPath))
                    {
                        watchDir = childPath;

                        if (IsTargetDirectory(watchDir))
                        {
                            return true;
                        }

                        _expectedName = GetChildName(watchDir, _targetDirectory);
                    }

                    // Start watching for the next expected directory.
                    FileSystemWatcher? newWatcher = null;

                    try
                    {
                        newWatcher = new FileSystemWatcher(watchDir)
                        {
                            IncludeSubdirectories = false,
                            NotifyFilter = NotifyFilters.DirectoryName
                        };
                        newWatcher.Created += OnCreated;
                        newWatcher.Renamed += OnCreated;
                        newWatcher.Error += OnError;
                        newWatcher.EnableRaisingEvents = true;
                    }
                    catch (Exception ex) when (ex is ArgumentException or IOException)
                    {
                        newWatcher?.Dispose();
                        return true;
                    }

                    _watcher = newWatcher;

                    // Post-start race check: the directory may have appeared during setup.
                    string nextDir = Path.Combine(watchDir, _expectedName);

                    if (!Directory.Exists(nextDir))
                    {
                        return false; // watcher is properly set up
                    }

                    // It appeared - dispose this watcher and advance.
                    _watcher.Dispose();
                    _watcher = null;
                    watchDir = nextDir;

                    if (IsTargetDirectory(watchDir))
                    {
                        return true;
                    }

                    // In every iteration, _expectedName moves to the next path segment and watchDir gets longer, so the loop can't go on forever.
                    _expectedName = GetChildName(watchDir, _targetDirectory);
                }
            }

            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            private void OnCreated(object sender, FileSystemEventArgs e)
            {
                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                bool shouldCancel = false;

                lock (_lock)
                {
                    if (sender != _watcher || _cts.IsCancellationRequested)
                    {
                        return;
                    }

                    if (!string.Equals(e.Name, _expectedName, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    if (!Directory.Exists(e.FullPath))
                    {
                        return;
                    }

                    _watcher.Dispose();
                    _watcher = null;

                    string createdPath = e.FullPath;

                    if (IsTargetDirectory(createdPath))
                    {
                        shouldCancel = true;
                    }
                    else
                    {
                        _expectedName = GetChildName(createdPath, _targetDirectory);
                        shouldCancel = SetupWatcherNoLock(createdPath);
                    }
                }

                if (shouldCancel)
                {
                    TryCancelCts();
                }
            }

            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            private void OnError(object sender, ErrorEventArgs e)
            {
                FileSystemWatcher? watcher;

                lock (_lock)
                {
                    if (sender != _watcher || _cts.IsCancellationRequested)
                    {
                        return;
                    }

                    watcher = _watcher;
                    _watcher = null;
                }

                watcher?.Dispose();

                TryCancelCts();
            }

            private void TryCancelCts()
            {
                try
                {
                    _cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Dispose() ran concurrently and disposed _cts.
                }
            }

            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                FileSystemWatcher? w;
                lock (_lock)
                {
                    w = _watcher;
                    _watcher = null;
                }

                w?.Dispose();
                _cts.Dispose();
            }
        }
    }
}
