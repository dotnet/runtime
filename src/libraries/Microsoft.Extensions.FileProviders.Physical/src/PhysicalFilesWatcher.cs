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
        // Key: deepest existing ancestor directory path. Value: the watcher for that directory.
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

            _root = root;

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

            IChangeToken changeToken = GetOrAddChangeToken(filter);
// We made sure that browser/iOS/tvOS never uses FileSystemWatcher.
#pragma warning disable CA1416 // Validate platform compatibility
            TryEnableFileSystemWatcher();
#pragma warning restore CA1416 // Validate platform compatibility

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
                // get rid of \. in Windows and ./ in UNIX's at the start of path file
                : GetOrAddFilePathChangeToken(RemoveRelativePathSegment(pattern));
        }

        private static string RemoveRelativePathSegment(string pattern) =>
            // The pattern has already been normalized to unix directory separators
            pattern.StartsWith("./", StringComparison.Ordinal) ? pattern.Substring(2) : pattern;

        internal IChangeToken GetOrAddFilePathChangeToken(string filePath)
        {
            // When using a FileSystemWatcher, if any parent directory in the path does not yet exist,
            // return a token backed by a watcher that internally cascades through the missing directory
            // levels and only fires once the target file itself is created.  This avoids adding recursive
            // inotify watches and avoids spurious token fires for intermediate directory creations.
            if (_fileWatcher != null && HasMissingParentDirectory(filePath))
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

            return changeToken;
        }

        // Returns true when at least one directory component of filePath (relative to _root) does not exist.
        // filePath uses '/' separators (already normalized by NormalizePath).
        private bool HasMissingParentDirectory(string filePath)
        {
            int lastSlash = filePath.LastIndexOf('/');

            if (lastSlash < 0)
            {
                return false; // file sits directly under _root, which is assumed to exist
            }

            return !Directory.Exists(Path.Combine(_root, filePath.Substring(0, lastSlash)));
        }

        // Returns the absolute path of the deepest existing ancestor directory of filePath.
        // filePath uses '/' separators (already normalized).
        private string FindDeepestExistingAncestor(string filePath)
        {
            // Walk from the deepest candidate upward; return the first one that exists.
            // In the common case where all parents are present this succeeds in one check.
            int slashIndex = filePath.LastIndexOf('/');
            while (slashIndex > 0)
            {
                string candidate = Path.Combine(_root, filePath.Substring(0, slashIndex));

                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                slashIndex = filePath.LastIndexOf('/', slashIndex - 1);
            }
            return _root;
        }

        // Returns a change token that fires only when the target file identified by filePath is
        // actually created.  A non-recursive FileSystemWatcher is placed at the deepest existing
        // ancestor of filePath and automatically advances through intermediate directory levels as
        // they are created, so no recursive inotify watches are added and the token does not fire
        // for intermediate directory creations.  The watcher self-recovers if a watched directory
        // is deleted, so the token only ever fires on file creation.
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private CancellationChangeToken GetOrAddPendingCreationToken(string filePath)
        {
            string watchDir = FindDeepestExistingAncestor(filePath);

            // Compute the remaining path components from watchDir down to the target file.
            // filePath uses '/' separators; _root ends with the OS directory separator.
            string relWatchDir = watchDir.Length > _root.Length
                ? watchDir.Substring(_root.Length).Replace(Path.DirectorySeparatorChar, '/')
                : string.Empty;
            string remainingRelPath = relWatchDir.Length > 0
                ? filePath.Substring(relWatchDir.Length + 1) // skip "relWatchDir/"
                : filePath;
            string[] remainingComponents = remainingRelPath.Split('/');

            // Key by filePath so each watched path has its own cascading watcher.
            while (true)
            {
                if (_pendingCreationWatchers.TryGetValue(filePath, out PendingCreationWatcher? existing))
                {
                    if (!existing.Cts.IsCancellationRequested)
                    {
                        return new CancellationChangeToken(existing.Cts.Token);
                    }

                    // Stale watcher (already fired); remove it and create a fresh one.
#if NET
                    _pendingCreationWatchers.TryRemove(new KeyValuePair<string, PendingCreationWatcher>(filePath, existing));
#else
                    _pendingCreationWatchers.TryRemove(filePath, out _);
#endif
                    // Do not dispose here – cleanup is already scheduled via Cts.Token.Register.
                }

                PendingCreationWatcher newWatcher;
                try
                {
                    newWatcher = new PendingCreationWatcher(_root, filePath, watchDir, remainingComponents);
                }
                catch
                {
                    // Unexpected – the watchDir was confirmed to exist moments ago.
                    // Fall back to an already-cancelled token so the caller re-registers immediately.
                    return new CancellationChangeToken(new CancellationToken(canceled: true));
                }

                if (_pendingCreationWatchers.TryAdd(filePath, newWatcher))
                {
                    // When the token fires, remove this entry and dispose the watcher asynchronously
                    // (we must not dispose a FileSystemWatcher on its own event thread).
                    newWatcher.Cts.Token.Register(static state =>
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

                        Task.Factory.StartNew(
                            static watcher => ((PendingCreationWatcher)watcher!).Dispose(),
                            w,
                            CancellationToken.None,
                            TaskCreationOptions.DenyChildAttach,
                            TaskScheduler.Default);
                    }, Tuple.Create(_pendingCreationWatchers, filePath, newWatcher));

                    return new CancellationChangeToken(newWatcher.Cts.Token);
                }

                // Another thread won the TryAdd race; dispose ours and retry.
                newWatcher.Dispose();
            }
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
                if (disposing)
                {
                    _fileWatcher?.Dispose();
                    _timer?.Dispose();

// We made sure that browser/iOS/tvOS never uses FileSystemWatcher so _pendingCreationWatchers is always empty on those platforms.
#pragma warning disable CA1416
                    foreach (System.Collections.Generic.KeyValuePair<string, PendingCreationWatcher> kvp in _pendingCreationWatchers)
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

            foreach (System.Collections.Generic.KeyValuePair<string, ChangeTokenInfo> wildCardEntry in _wildcardTokenLookup)
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

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private void TryEnableFileSystemWatcher()
        {
            if (_fileWatcher != null)
            {
                lock (_fileWatcherLock)
                {
                    if ((!_filePathTokenLookup.IsEmpty || !_wildcardTokenLookup.IsEmpty) &&
                        !_fileWatcher.EnableRaisingEvents)
                    {
                        // Perf: Turn off the file monitoring if no files to monitor.
                        _fileWatcher.EnableRaisingEvents = true;
                    }
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
            foreach (System.Collections.Generic.KeyValuePair<IPollingChangeToken, IPollingChangeToken> item in changeTokens)
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
        // created the watcher automatically advances to watch the next level, so the CTS is
        // only cancelled when the final target file is actually created.
        // The watcher self-recovers when the watched directory is deleted (Error event), so
        // no spurious CTS cancellation occurs for directory churn.
        // Only ONE inotify watch (or equivalent) is active at any given time.
        private sealed class PendingCreationWatcher : IDisposable
        {
            public readonly CancellationTokenSource Cts = new();
            private FileSystemWatcher? _watcher; // protected by _advanceLock
            private Queue<string> _remainingComponents; // protected by _advanceLock
            private readonly string _root;
            private readonly string _filePath;
            private readonly object _advanceLock = new();
            private int _disposed;

            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            public PendingCreationWatcher(string root, string filePath, string existingDirectory, string[] remainingComponents)
            {
                _root = root;
                _filePath = filePath;
                _remainingComponents = new Queue<string>(remainingComponents);
                lock (_advanceLock)
                {
                    SetupWatcherNoLock(existingDirectory);
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
                           Directory.Exists(Path.Combine(startDir, _remainingComponents.Peek())))
                    {
                        startDir = Path.Combine(startDir, _remainingComponents.Dequeue());
                    }

                    // If the final target already exists, fire immediately without starting a watcher.
                    if (_remainingComponents.Count == 1)
                    {
                        string target = Path.Combine(startDir, _remainingComponents.Peek());
                        if (File.Exists(target) || Directory.Exists(target))
                        {
                            Cts.Cancel();
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
                        Cts.Cancel();
                        return;
                    }

                    _watcher = newWatcher;

                    // Phase 3: post-start race check.  The OS watch is now live; re-examine the
                    // very next expected component once to catch anything created during setup.
                    if (_remainingComponents.Count == 0)
                    {
                        return;
                    }

                    string next = _remainingComponents.Peek();
                    string nextPath = Path.Combine(startDir, next);

                    if (_remainingComponents.Count == 1)
                    {
                        // Target file – if it appeared during setup, fire and clean up.
                        if (File.Exists(nextPath) || Directory.Exists(nextPath))
                        {
                            ScheduleDispose(_watcher);
                            _watcher = null;
                            Cts.Cancel();
                        }
                        return;
                    }

                    if (!Directory.Exists(nextPath))
                    {
                        return; // Nothing to do – the watcher is properly set up.
                    }

                    // The next directory appeared during setup.  Dispose the watcher we just
                    // created and loop to advance to the next level without firing the token.
                    ScheduleDispose(_watcher);
                    _watcher = null;
                    _remainingComponents.Dequeue();
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
                if (Cts.IsCancellationRequested)
                {
                    return;
                }

                lock (_advanceLock)
                {
                    // Ignore events from stale watchers that have already been replaced.
                    if (sender != _watcher || Cts.IsCancellationRequested)
                    {
                        return;
                    }

                    // Only react to the creation of the expected next path component.
                    if (!string.Equals(e.Name, _remainingComponents.Peek(), StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    _remainingComponents.Dequeue();

                    // Schedule disposal of the current watcher before replacing it, so that
                    // Dispose() is never called on the watcher's own event thread.
                    ScheduleDispose(_watcher);
                    _watcher = null;

                    if (_remainingComponents.Count == 0)
                    {
                        // The target file was created – fire the token.
                        Cts.Cancel();
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
                // The watched directory may have been deleted or an internal buffer overflow
                // occurred.  Reset to the deepest still-existing ancestor so that the watch
                // can recover without cancelling the CTS prematurely.
                lock (_advanceLock)
                {
                    if (sender != _watcher || Cts.IsCancellationRequested)
                    {
                        return;
                    }

                    ScheduleDispose(_watcher);
                    _watcher = null;
                    RewatchFromDeepestAncestorNoLock();
                }
            }

            // Must be called with _advanceLock held and _watcher == null.
            // Recomputes the deepest existing ancestor of _filePath from _root, rebuilds
            // _remainingComponents, and restarts the watch without cancelling the CTS.
            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("wasi")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            [SupportedOSPlatform("maccatalyst")]
            private void RewatchFromDeepestAncestorNoLock()
            {
                string[] allComponents = _filePath.Split('/');
                // _root may end with the OS directory separator; Path.Combine handles that correctly.
                string dir = _root;
                int consumed = 0;

                while (consumed < allComponents.Length - 1)
                {
                    string candidate = Path.Combine(dir, allComponents[consumed]);
                    if (!Directory.Exists(candidate))
                    {
                        break;
                    }

                    dir = candidate;
                    consumed++;
                }

                _remainingComponents = new Queue<string>();
                for (int i = consumed; i < allComponents.Length; i++)
                {
                    _remainingComponents.Enqueue(allComponents[i]);
                }

                SetupWatcherNoLock(dir);
            }

            // Schedules disposal of a FileSystemWatcher on a thread-pool thread so that
            // it is never called on the watcher's own event-dispatching thread.
            private static void ScheduleDispose(FileSystemWatcher? watcher)
            {
                if (watcher is null)
                {
                    return;
                }

                Task.Factory.StartNew(
                    static w => ((FileSystemWatcher)w!).Dispose(),
                    watcher,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
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

                // Use ScheduleDispose so that FileSystemWatcher.Dispose() is never called
                // on the watcher's own event thread, which could cause a deadlock.
                ScheduleDispose(w);
                Cts.Cancel();
                Cts.Dispose();
            }
        }
    }
}
