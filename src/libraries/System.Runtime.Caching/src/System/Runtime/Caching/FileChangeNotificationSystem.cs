// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Caching.Hosting;
using System.Runtime.Caching.Resources;
using System.Collections;
using System.IO;
using System.Security;
using System.Runtime.Versioning;

namespace System.Runtime.Caching
{
#if NET5_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
#endif
    internal sealed class FileChangeNotificationSystem : IFileChangeNotificationSystem
    {
        private readonly Hashtable _dirMonitors;
        private readonly object _lock;

        internal sealed class DirectoryMonitor
        {
            internal FileSystemWatcher Fsw;
        }

        internal sealed class FileChangeEventTarget
        {
            private readonly string _fileName;
            private readonly OnChangedCallback _onChangedCallback;
            private readonly FileSystemEventHandler _changedHandler;
            private readonly ErrorEventHandler _errorHandler;
            private readonly RenamedEventHandler _renamedHandler;

            private static bool EqualsIgnoreCase(string s1, string s2)
            {
                if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
                {
                    return true;
                }
                if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                {
                    return false;
                }
                if (s2.Length != s1.Length)
                {
                    return false;
                }
                return 0 == string.Compare(s1, 0, s2, 0, s2.Length, StringComparison.OrdinalIgnoreCase);
            }

            private void OnChanged(object sender, FileSystemEventArgs e)
            {
                if (EqualsIgnoreCase(_fileName, e.Name))
                {
                    _onChangedCallback(null);
                }
            }

            private void OnError(object sender, ErrorEventArgs e)
            {
                _onChangedCallback(null);
            }

            private void OnRenamed(object sender, RenamedEventArgs e)
            {
                if (EqualsIgnoreCase(_fileName, e.Name) || EqualsIgnoreCase(_fileName, e.OldName))
                {
                    _onChangedCallback(null);
                }
            }

            internal FileSystemEventHandler ChangedHandler { get { return _changedHandler; } }
            internal ErrorEventHandler ErrorHandler { get { return _errorHandler; } }
            internal RenamedEventHandler RenamedHandler { get { return _renamedHandler; } }

            internal FileChangeEventTarget(string fileName, OnChangedCallback onChangedCallback)
            {
                _fileName = fileName;
                _onChangedCallback = onChangedCallback;
                _changedHandler = new FileSystemEventHandler(this.OnChanged);
                _errorHandler = new ErrorEventHandler(this.OnError);
                _renamedHandler = new RenamedEventHandler(this.OnRenamed);
            }
        }

        internal FileChangeNotificationSystem()
        {
            _dirMonitors = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));
            _lock = new object();
        }

        void IFileChangeNotificationSystem.StartMonitoring(string filePath, OnChangedCallback onChangedCallback, out object state, out DateTimeOffset lastWriteTime, out long fileSize)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (onChangedCallback == null)
            {
                throw new ArgumentNullException(nameof(onChangedCallback));
            }
            FileInfo fileInfo = new FileInfo(filePath);
            string dir = Path.GetDirectoryName(filePath);
            DirectoryMonitor dirMon = _dirMonitors[dir] as DirectoryMonitor;
            if (dirMon == null)
            {
                lock (_lock)
                {
                    dirMon = _dirMonitors[dir] as DirectoryMonitor;
                    if (dirMon == null)
                    {
                        dirMon = new DirectoryMonitor();
                        dirMon.Fsw = new FileSystemWatcher(dir);
                        dirMon.Fsw.NotifyFilter = NotifyFilters.FileName
                                                  | NotifyFilters.DirectoryName
                                                  | NotifyFilters.CreationTime
                                                  | NotifyFilters.Size
                                                  | NotifyFilters.LastWrite
                                                  | NotifyFilters.Security;
                        dirMon.Fsw.EnableRaisingEvents = true;
                    }
                    _dirMonitors[dir] = dirMon;
                }
            }

            FileChangeEventTarget target = new FileChangeEventTarget(fileInfo.Name, onChangedCallback);

            lock (dirMon)
            {
                dirMon.Fsw.Changed += target.ChangedHandler;
                dirMon.Fsw.Created += target.ChangedHandler;
                dirMon.Fsw.Deleted += target.ChangedHandler;
                dirMon.Fsw.Error += target.ErrorHandler;
                dirMon.Fsw.Renamed += target.RenamedHandler;
            }

            state = target;
            lastWriteTime = File.GetLastWriteTime(filePath);
            fileSize = (fileInfo.Exists) ? fileInfo.Length : -1;
        }

        void IFileChangeNotificationSystem.StopMonitoring(string filePath, object state)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            FileChangeEventTarget target = state as FileChangeEventTarget;
            if (target == null)
            {
                throw new ArgumentException(SR.Invalid_state, nameof(state));
            }
            string dir = Path.GetDirectoryName(filePath);
            DirectoryMonitor dirMon = _dirMonitors[dir] as DirectoryMonitor;
            if (dirMon != null)
            {
                lock (dirMon)
                {
                    dirMon.Fsw.Changed -= target.ChangedHandler;
                    dirMon.Fsw.Created -= target.ChangedHandler;
                    dirMon.Fsw.Deleted -= target.ChangedHandler;
                    dirMon.Fsw.Error -= target.ErrorHandler;
                    dirMon.Fsw.Renamed -= target.RenamedHandler;
                }
            }
        }
    }
}
