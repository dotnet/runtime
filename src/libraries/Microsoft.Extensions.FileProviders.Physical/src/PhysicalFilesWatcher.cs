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
    /// <see cref="IChangeToken" /> when files are created, change, renamed, or deleted.
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

        // Tracks non-recursive watchers used when parent directories of a watched path do not yet exist.
        // Key: the file or directory path being watched that requires pending-creation handling.
        // Value: the corresponding pending-creation watcher, which internally watches the deepest existing ancestor directory.
        // We made sure that browser/iOS/tvOS never uses FileSystemWatcher so this is always empty on those platforms.
        private readonly ConcurrentDictionary<string, PendingCreationWatcher> _pendingCreationWatchers
            = new(StringComparer.OrdinalIgnoreCase);

        private Timer? _timer;
        private bool _timerInitialized;
        private object _timerLock = new();
        private readonly Func<Timer> _timerFactory;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhysicalFilesWatcher"/> class that watches files in <paramref name="root"/>.
        /// Wraps an instance of <see cref="System.IO.FileSystemWatcher"/>.
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
        /// Wraps an instance of <see cref="System.IO.FileSystemWatcher"/>.
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

            _root = root.Length > 0 && root[root.Length - 1] != Path.DirectorySeparatorChar && root[root.Length - 1] != Path.AltDirectorySeparatorChar
                ? root + Path.DirectorySeparatorChar
                : root;

            if (fileSystemWatcher != null)
            {
#if NET
                if (OperatingSystem.IsBrowser() || OperatingSystem.IsWasi() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()) || OperatingSystem.IsTvOS())
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.FileSystemWatcher_PlatformNotSupported, typeof(FileSystemWatcher)));
                }
#endif

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
        /// <returns>A change token for all files that match the filter.</returns>
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

            return GetOrAddChangeToken(filter);
        }

        private IChangeToken GetOrAddChangeToken(string pattern)
        {
            if (UseActivePolling)
            {
                LazyInitializer.EnsureInitialized(ref _timer, ref _timerInitialized, ref _timerLock, _timerFactory);
            }

            return pattern.Contains('*') || IsDirectoryPath(pattern)
                ? GetOrAddWildcardChangeToken(pattern)
                // get rid of \. in Windows and ./ in UNIX's at the start of path file
                : GetOrAddFilePathChangeToken(RemoveRelativePathSegment(pattern));
        }

        private static string RemoveRelativePathSegment(string pattern) =>
            // The pattern has already been normalized to unix directory separators
            pattern.StartsWith("./", StringComparison.Ordinal) ? pattern.Substring(2) : pattern;

        internal IChangeToken GetOrAddFilePathChangeToken(string filePath)
        {
            // When using a FileSystemWatcher and _root does not exist, use a pending creation
            // token that cascades through missing directory levels above and including _root,
            // then through subdirectories down to the target file.  For missing directories
            // under _root, the recursive main FileSystemWatcher handles detection.
            if (_fileWatcher != null && !Directory.Exists(_root))
            {
                // We made sure that browser/iOS/tvOS never uses FileSystemWatcher.
#pragma warning disable CA1416
                return GetOrAddPendingCreationToken(filePath);
#pragma warning restore CA1416
            }

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

            TryEnableFileSystemWatcher();

            return changeToken;
        }

        // Returns a change token that fires when _root is created and the target file is
        // eventually created inside it. Only used when _root does not exist.
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private CancellationChangeToken GetOrAddPendingCreationToken(string filePath)
        {
            return GetOrAddPendingCreationToken(filePath, filePath.Split('/'));
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private CancellationChangeToken GetOrAddPendingCreationToken(string key, string[] remainingComponents)
        {
            // _root does not exist; the PendingCreationWatcher constructor will walk up
            // to find an existing ancestor above _root.
            string watchDir = _root;

            while (true)
            {
                if (_pendingCreationWatchers.TryGetValue(key, out PendingCreationWatcher? existing))
                {
                    if (!existing.ChangeToken.HasChanged)
                    {
                        return existing.ChangeToken;
                    }

                    // Stale watcher (already fired); remove it and create a fresh one.
                    _pendingCreationWatchers.TryRemove(key, out _);
                    // Do not dispose here – cleanup is already scheduled via ChangeToken registration.
                }

                PendingCreationWatcher newWatcher;
                try
                {
                    newWatcher = new PendingCreationWatcher(watchDir, remainingComponents);
                }
                catch
                {
                    // Unexpected – fall back to an already-cancelled token so the caller re-registers immediately.
                    return new CancellationChangeToken(new CancellationToken(canceled: true));
                }

                if (_pendingCreationWatchers.TryAdd(key, newWatcher))
                {
                    // When the token fires, remove this entry and dispose the watcher.
                    newWatcher.ChangeToken.RegisterChangeCallback(static state =>
                    {
                        var tuple = (Tuple<ConcurrentDictionary<string, PendingCreationWatcher>, string, PendingCreationWatcher>)state!;
                        ConcurrentDictionary<string, PendingCreationWatcher> dict = tuple.Item1;
                        string k = tuple.Item2;
                        PendingCreationWatcher w = tuple.Item3;

#if NET
                        dict.TryRemove(new KeyValuePair<string, PendingCreationWatcher>(k, w));
#else
                        // Only remove our specific entry, not a newer one that may have been added.
                        if (dict.TryRemove(k, out PendingCreationWatcher? removed) && removed != w)
                        {
                            // We removed a newer entry by accident; put it back (best effort).
                            dict.TryAdd(k, removed);
                        }
#endif

                        w.Dispose();
                    }, Tuple.Create(_pendingCreationWatchers, key, newWatcher));

                    return newWatcher.ChangeToken;
                }

                // Another thread won the TryAdd race; dispose ours and retry.
                newWatcher.Dispose();
            }
        }

        internal IChangeToken GetOrAddWildcardChangeToken(string pattern)
        {
            // When using a FileSystemWatcher and _root does not exist, use a pending creation
            // token to avoid enabling the main FSW on a non-existent directory (which would throw).
            // The token fires when _root appears, signalling the caller to re-register.
            // We use the _root directory name as the single remaining component so the
            // PendingCreationWatcher watches for _root to be created.
            if (_fileWatcher != null && !Directory.Exists(_root))
            {
                // We made sure that browser/iOS/tvOS never uses FileSystemWatcher.
                // Pass an empty components array — the PendingCreationWatcher constructor
                // will prepend the missing _root directory levels automatically.
#pragma warning disable CA1416
                return GetOrAddPendingCreationToken("wildcard:" + pattern, Array.Empty<string>());
#pragma warning restore CA1416
            }

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

            TryEnableFileSystemWatcher();

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
                if (disposing)
                {
                    _fileWatcher?.Dispose();
                    _timer?.Dispose();

// We made sure that browser/iOS/tvOS never uses FileSystemWatcher so _pendingCreationWatchers is always empty on those platforms.
#pragma warning disable CA1416
                    foreach (KeyValuePair<string, PendingCreationWatcher> kvp in _pendingCreationWatchers)
                    {
                        kvp.Value.Dispose();
                    }
                    _pendingCreationWatchers.Clear();
#pragma warning restore CA1416
                }
                _disposed = true;
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
                catch (Exception ex) when (
                    ex is IOException ||
                    ex is SecurityException ||
                    ex is DirectoryNotFoundException ||
                    ex is UnauthorizedAccessException)
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
            foreach (string path in _filePathTokenLookup.Keys)
            {
                ReportChangeForMatchedEntries(path);
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
                var fileSystemInfo = new FileInfo(fullPath);
                if (FileSystemInfoHelper.IsExcluded(fileSystemInfo, _filters))
                {
                    return;
                }

                string relativePath = fullPath.Substring(_root.Length);
                ReportChangeForMatchedEntries(relativePath);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is SecurityException ||
                ex is UnauthorizedAccessException)
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
                        // Perf: Turn off the file monitoring if no files to monitor.
                        _fileWatcher.EnableRaisingEvents = false;
                    }
                }
            }
        }

        private void TryEnableFileSystemWatcher()
        {
            if (_fileWatcher != null)
            {
                lock (_fileWatcherLock)
                {
// We made sure that browser/iOS/tvOS never uses FileSystemWatcher.
#pragma warning disable CA1416 // Validate platform compatibility
                    if ((!_filePathTokenLookup.IsEmpty || !_wildcardTokenLookup.IsEmpty) &&
                        !_fileWatcher.EnableRaisingEvents)
                    {
                        if (string.IsNullOrEmpty(_fileWatcher.Path))
                        {
                            _fileWatcher.Path = _root;
                        }

                        // Perf: Turn off the file monitoring if no files to monitor.
                        _fileWatcher.EnableRaisingEvents = true;
                    }
#pragma warning restore CA1416
                }
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

        // Watches a directory non-recursively for the creation of a specific sequence of path
        // components leading to a target file.  When an intermediate directory component is
        // created the watcher automatically advances to watch the next level, so the token is
        // only triggered when the final target file is actually created or when the underlying
        // FileSystemWatcher reports an error (for example, if the watched directory is deleted).
        // Only one non-recursive FileSystemWatcher is active at any given time.
        private sealed class PendingCreationWatcher : IDisposable
        {
            private readonly CancellationTokenSource _cts = new();
            private FileSystemWatcher? _watcher; // protected by _advanceLock
            private ArraySegment<string> _remainingComponents; // protected by _advanceLock
            private readonly object _advanceLock = new();
            private int _disposed;

            public CancellationChangeToken ChangeToken { get; }

            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            public PendingCreationWatcher(string directory, string[] remainingComponents)
            {
                ChangeToken = new CancellationChangeToken(_cts.Token);

                // If the directory doesn't exist (e.g. _root was deleted or never created),
                // walk up to find an existing ancestor and prepend the missing directory
                // names so the cascading watcher covers them.
                if (!Directory.Exists(directory))
                {
                    var missingDirs = new Stack<string>();
                    string current = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    while (!Directory.Exists(current))
                    {
                        missingDirs.Push(Path.GetFileName(current));
                        string? parent = Path.GetDirectoryName(current);
                        if (parent is null)
                        {
                            break;
                        }

                        current = parent;
                    }

                    var merged = new string[missingDirs.Count + remainingComponents.Length];
                    missingDirs.CopyTo(merged, 0);
                    Array.Copy(remainingComponents, 0, merged, missingDirs.Count, remainingComponents.Length);
                    remainingComponents = merged;
                    directory = current;
                }

                _remainingComponents = new ArraySegment<string>(remainingComponents);

                lock (_advanceLock)
                {
                    SetupWatcherNoLock(directory);
                }
            }

            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            private FileSystemWatcher CreateWatcher(string directory)
            {
                var fsw = new FileSystemWatcher(directory)
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                };
                fsw.Created += OnCreated;
                fsw.Renamed += OnCreated;
                fsw.Error += OnError;
                fsw.EnableRaisingEvents = true;
                return fsw;
            }

            // Must be called with _advanceLock held and _watcher == null.
            // Creates a new watcher at startDir using the current _remainingComponents queue.
            // Handles three concerns in a single loop:
            //   Phase 1 – pre-start fast-forward: skip components that already exist before
            //             registering the OS watch, to avoid a recursive watch at a high level.
            //   Phase 2 – watcher creation at the deepest reachable level.
            //   Phase 3 – post-start race check: re-examine the very next component once after
            //             the OS watch is registered, to catch anything created during the window
            //             between Phase 1's last check and when EnableRaisingEvents became true.
            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            private void SetupWatcherNoLock(string startDir)
            {
                while (true)
                {
                    // Phase 1: fast-forward through components that already exist.
                    while (_remainingComponents.Count > 1 &&
                           Directory.Exists(Path.Combine(startDir, _remainingComponents.At(0))))
                    {
                        startDir = Path.Combine(startDir, _remainingComponents.At(0));
                        _remainingComponents = _remainingComponents.Slice(1);
                    }

                    // If the final target already exists, fire immediately without starting a watcher.
                    if (_remainingComponents.Count == 1)
                    {
                        string target = Path.Combine(startDir, _remainingComponents.At(0));
                        if (File.Exists(target) || Directory.Exists(target))
                        {
                            _cts.Cancel();
                            return;
                        }
                    }

                    // Phase 2: start the OS watch at the current deepest reachable level.
                    FileSystemWatcher newWatcher;
                    try
                    {
                        newWatcher = CreateWatcher(startDir);
                    }
                    catch
                    {
                        // startDir was deleted in the narrow window since Phase 1 confirmed it.
                        // Cancel so the caller can re-register; the error path is only a best-effort
                        // fallback here (the Error handler handles the steady-state deleted-dir case).
                        _cts.Cancel();
                        return;
                    }

                    _watcher = newWatcher;

                    // Phase 3: post-start race check.  The OS watch is now live; re-examine the
                    // very next expected component once to catch anything created during setup.
                    if (_remainingComponents.Count == 0)
                    {
                        return;
                    }

                    string next = _remainingComponents.At(0);
                    string nextPath = Path.Combine(startDir, next);

                    if (_remainingComponents.Count == 1)
                    {
                        // Target file – if it appeared during setup, fire and clean up.
                        if (File.Exists(nextPath) || Directory.Exists(nextPath))
                        {
                            _watcher.Dispose();
                            _watcher = null;
                            _cts.Cancel();
                        }
                        // else
                        // {
                        //     TryEnableFileSystemWatcher();
                        // }

                        return;
                    }

                    if (!Directory.Exists(nextPath))
                    {
                        return; // Nothing to do – the watcher is properly set up.
                    }

                    // The next directory appeared during setup.  Dispose the watcher we just
                    // created and loop to advance to the next level without firing the token.
                    _watcher.Dispose();
                    _watcher = null;
                    _remainingComponents = _remainingComponents.Slice(1);
                    startDir = nextPath;
                    // continue looping
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

                lock (_advanceLock)
                {
                    // Ignore events from stale watchers that have already been replaced.
                    if (sender != _watcher || _cts.IsCancellationRequested)
                    {
                        return;
                    }

                    // Only react to the creation of the expected next path component.
                    if (!string.Equals(e.Name, _remainingComponents.At(0), StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    _remainingComponents = _remainingComponents.Slice(1);

                    _watcher.Dispose();
                    _watcher = null;

                    if (_remainingComponents.Count == 0)
                    {
                        // The target file was created – fire the token.
                        _cts.Cancel();
                    }
                    else
                    {
                        // An intermediate directory was created – advance the watcher.
                        SetupWatcherNoLock(e.FullPath);
                    }
                }
            }

            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            private void OnError(object sender, ErrorEventArgs e)
            {
                // The watched directory may have been deleted or another error may have occurred.
                // In either case, cancel the token to trigger re-registration, and dispose the watcher.
                lock (_advanceLock)
                {
                    if (sender != _watcher || _cts.IsCancellationRequested)
                    {
                        return;
                    }

                    _watcher?.Dispose();
                    _watcher = null;
                    _cts.Cancel();
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
                lock (_advanceLock)
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
