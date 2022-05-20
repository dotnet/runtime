// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Enumeration;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <devdoc>
    ///    Listens to the system directory change notifications and
    ///    raises events when a directory or file within a directory changes.
    /// </devdoc>
    public partial class FileSystemWatcher : Component, ISupportInitialize
    {
        // Filters collection
        private readonly NormalizedFilterCollection _filters = new NormalizedFilterCollection();

        // Directory being monitored
        private string _directory;

        // The watch filter for the API call.
        private const NotifyFilters c_defaultNotifyFilters = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        private NotifyFilters _notifyFilters = c_defaultNotifyFilters;

        // Flag to watch subtree of this directory
        private bool _includeSubdirectories;

        // Flag to note whether we are attached to the thread pool and responding to changes
        private bool _enabled;

        // Are we in init?
        private bool _initializing;

        // Buffer size
        private uint _internalBufferSize = 8192;

        // Used for synchronization
        private bool _disposed;

        // Event handlers
        private FileSystemEventHandler? _onChangedHandler;
        private FileSystemEventHandler? _onCreatedHandler;
        private FileSystemEventHandler? _onDeletedHandler;
        private RenamedEventHandler? _onRenamedHandler;
        private ErrorEventHandler? _onErrorHandler;

        private const int c_notifyFiltersValidMask = (int)(NotifyFilters.Attributes |
                                                           NotifyFilters.CreationTime |
                                                           NotifyFilters.DirectoryName |
                                                           NotifyFilters.FileName |
                                                           NotifyFilters.LastAccess |
                                                           NotifyFilters.LastWrite |
                                                           NotifyFilters.Security |
                                                           NotifyFilters.Size);

#if DEBUG
        static FileSystemWatcher()
        {
            int s_notifyFiltersValidMask = 0;
            foreach (int enumValue in Enum.GetValues<NotifyFilters>())
                s_notifyFiltersValidMask |= enumValue;
            Debug.Assert(c_notifyFiltersValidMask == s_notifyFiltersValidMask, "The NotifyFilters enum has changed. The c_notifyFiltersValidMask must be updated to reflect the values of the NotifyFilters enum.");
        }
#endif

        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.IO.FileSystemWatcher'/> class.
        /// </devdoc>
        public FileSystemWatcher()
        {
            _directory = string.Empty;
        }

        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.IO.FileSystemWatcher'/> class,
        ///    given the specified directory to monitor.
        /// </devdoc>
        public FileSystemWatcher(string path)
        {
            CheckPathValidity(path);
            _directory = path;
        }

        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.IO.FileSystemWatcher'/> class,
        ///    given the specified directory and type of files to monitor.
        /// </devdoc>
        public FileSystemWatcher(string path, string filter)
        {
            CheckPathValidity(path);
            ArgumentNullException.ThrowIfNull(filter);

            _directory = path;
            Filter = filter;
        }

        /// <devdoc>
        ///    Gets or sets the type of changes to watch for.
        /// </devdoc>
        public NotifyFilters NotifyFilter
        {
            get
            {
                return _notifyFilters;
            }
            set
            {
                if (((int)value & ~c_notifyFiltersValidMask) != 0)
                    throw new ArgumentException(SR.Format(SR.InvalidEnumArgument, nameof(value), (int)value, nameof(NotifyFilters)));

                if (_notifyFilters != value)
                {
                    _notifyFilters = value;

                    Restart();
                }
            }
        }

        public Collection<string> Filters => _filters;

        /// <devdoc>
        ///    Gets or sets a value indicating whether the component is enabled.
        /// </devdoc>
        public bool EnableRaisingEvents
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                if (IsSuspended())
                {
                    _enabled = value; // Alert the Component to start watching for events when EndInit is called.
                }
                else
                {
                    if (value)
                    {
                        StartRaisingEventsIfNotDisposed(); // will set _enabled to true once successfully started
                    }
                    else
                    {
                        StopRaisingEvents(); // will set _enabled to false
                    }
                }
            }
        }

        /// <devdoc>
        ///    Gets or sets the filter string, used to determine what files are monitored in a directory.
        /// </devdoc>
        public string Filter
        {
            get
            {
                return Filters.Count == 0 ? "*" : Filters[0];
            }
            set
            {
                Filters.Clear();
                Filters.Add(value);
            }
        }

        /// <devdoc>
        ///    Gets or sets a value indicating whether subdirectories within the specified path should be monitored.
        /// </devdoc>
        public bool IncludeSubdirectories
        {
            get
            {
                return _includeSubdirectories;
            }
            set
            {
                if (_includeSubdirectories != value)
                {
                    _includeSubdirectories = value;

                    Restart();
                }
            }
        }

        /// <devdoc>
        ///    Gets or sets the size of the internal buffer.
        /// </devdoc>
        public int InternalBufferSize
        {
            get
            {
                return (int)_internalBufferSize;
            }
            set
            {
                if (_internalBufferSize != value)
                {
                    if (value < 4096)
                    {
                        _internalBufferSize = 4096;
                    }
                    else
                    {
                        _internalBufferSize = (uint)value;
                    }

                    Restart();
                }
            }
        }

        /// <summary>Allocates a buffer of the requested internal buffer size.</summary>
        /// <returns>The allocated buffer.</returns>
        private byte[] AllocateBuffer()
        {
            try
            {
                return new byte[_internalBufferSize];
            }
            catch (OutOfMemoryException)
            {
                throw new OutOfMemoryException(SR.Format(SR.BufferSizeTooLarge, _internalBufferSize));
            }
        }

        /// <devdoc>
        ///    Gets or sets the path of the directory to watch.
        /// </devdoc>
        [Editor("System.Diagnostics.Design.FSWPathEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string Path
        {
            get
            {
                return _directory;
            }
            set
            {
                value = (value == null) ? string.Empty : value;
                if (!string.Equals(_directory, value, PathInternal.StringComparison))
                {
                    if (value.Length == 0)
                        throw new ArgumentException(SR.Format(SR.InvalidDirName, value), nameof(Path));

                    if (!Directory.Exists(value))
                        throw new ArgumentException(SR.Format(SR.InvalidDirName_NotExists, value), nameof(Path));

                    _directory = value;
                    Restart();
                }
            }
        }

        /// <devdoc>
        ///    Occurs when a file or directory in the specified <see cref='System.IO.FileSystemWatcher.Path'/> is changed.
        /// </devdoc>
        public event FileSystemEventHandler? Changed
        {
            add
            {
                _onChangedHandler += value;
            }
            remove
            {
                _onChangedHandler -= value;
            }
        }

        /// <devdoc>
        ///    Occurs when a file or directory in the specified <see cref='System.IO.FileSystemWatcher.Path'/> is created.
        /// </devdoc>
        public event FileSystemEventHandler? Created
        {
            add
            {
                _onCreatedHandler += value;
            }
            remove
            {
                _onCreatedHandler -= value;
            }
        }

        /// <devdoc>
        ///    Occurs when a file or directory in the specified <see cref='System.IO.FileSystemWatcher.Path'/> is deleted.
        /// </devdoc>
        public event FileSystemEventHandler? Deleted
        {
            add
            {
                _onDeletedHandler += value;
            }
            remove
            {
                _onDeletedHandler -= value;
            }
        }

        /// <devdoc>
        ///    Occurs when the internal buffer overflows.
        /// </devdoc>
        public event ErrorEventHandler? Error
        {
            add
            {
                _onErrorHandler += value;
            }
            remove
            {
                _onErrorHandler -= value;
            }
        }

        /// <devdoc>
        ///    Occurs when a file or directory in the specified <see cref='System.IO.FileSystemWatcher.Path'/>
        ///    is renamed.
        /// </devdoc>
        public event RenamedEventHandler? Renamed
        {
            add
            {
                _onRenamedHandler += value;
            }
            remove
            {
                _onRenamedHandler -= value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    //Stop raising events cleans up managed and
                    //unmanaged resources.
                    StopRaisingEvents();

                    // Clean up managed resources
                    _onChangedHandler = null;
                    _onCreatedHandler = null;
                    _onDeletedHandler = null;
                    _onRenamedHandler = null;
                    _onErrorHandler = null;
                }
                else
                {
                    FinalizeDispose();
                }
            }
            finally
            {
                _disposed = true;
                base.Dispose(disposing);
            }
        }

        private static void CheckPathValidity(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            // Early check for directory parameter so that an exception can be thrown as early as possible.
            if (path.Length == 0)
                throw new ArgumentException(SR.Format(SR.InvalidDirName, path), nameof(path));

            if (!Directory.Exists(path))
                throw new ArgumentException(SR.Format(SR.InvalidDirName_NotExists, path), nameof(path));
        }

        /// <summary>
        /// Sees if the name given matches the name filter we have.
        /// </summary>
        private bool MatchPattern(ReadOnlySpan<char> relativePath)
        {
            ReadOnlySpan<char> name = IO.Path.GetFileName(relativePath);
            if (name.Length == 0)
                return false;

            string[] filters = _filters.GetFilters();
            if (filters.Length == 0)
                return true;

            foreach (string filter in filters)
            {
                if (FileSystemName.MatchesSimpleExpression(filter, name, ignoreCase: !PathInternal.IsCaseSensitive))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Raises the event to each handler in the list.
        /// </summary>
        private void NotifyInternalBufferOverflowEvent()
        {
            if (_onErrorHandler != null)
            {
                OnError(new ErrorEventArgs(
                        new InternalBufferOverflowException(SR.Format(SR.FSW_BufferOverflow, _directory))));
            }
        }

        /// <summary>
        /// Raises the event to each handler in the list.
        /// </summary>
        private void NotifyRenameEventArgs(WatcherChangeTypes action, ReadOnlySpan<char> name, ReadOnlySpan<char> oldName)
        {
            // filter if there's no handler or neither new name or old name match a specified pattern
            if (_onRenamedHandler != null &&
                (MatchPattern(name) || MatchPattern(oldName)))
            {
                OnRenamed(new RenamedEventArgs(action, _directory, name.IsEmpty ? null : name.ToString(), oldName.IsEmpty ? null : oldName.ToString()));
            }
        }

        private FileSystemEventHandler? GetHandler(WatcherChangeTypes changeType)
        {
            switch (changeType)
            {
                case WatcherChangeTypes.Created:
                    return _onCreatedHandler;
                case WatcherChangeTypes.Deleted:
                    return _onDeletedHandler;
                case WatcherChangeTypes.Changed:
                    return _onChangedHandler;
            }

            Debug.Fail("Unknown FileSystemEvent change type!  Value: " + changeType);
            return null;
        }

        /// <summary>
        /// Raises the event to each handler in the list.
        /// </summary>
        private void NotifyFileSystemEventArgs(WatcherChangeTypes changeType, ReadOnlySpan<char> name)
        {
            FileSystemEventHandler? handler = GetHandler(changeType);

            if (handler != null && MatchPattern(name.IsEmpty ? _directory : name))
            {
                InvokeOn(new FileSystemEventArgs(changeType, _directory, name.IsEmpty ? null : name.ToString()), handler);
            }
        }

        /// <summary>
        /// Raises the event to each handler in the list.
        /// </summary>
        private void NotifyFileSystemEventArgs(WatcherChangeTypes changeType, string name)
        {
            FileSystemEventHandler? handler = GetHandler(changeType);

            if (handler != null && MatchPattern(string.IsNullOrEmpty(name) ? _directory : name))
            {
                InvokeOn(new FileSystemEventArgs(changeType, _directory, name), handler);
            }
        }

        /// <devdoc>
        ///    Raises the <see cref='System.IO.FileSystemWatcher.Changed'/> event.
        /// </devdoc>
        protected void OnChanged(FileSystemEventArgs e)
        {
            InvokeOn(e, _onChangedHandler);
        }

        /// <devdoc>
        ///    Raises the <see cref='System.IO.FileSystemWatcher.Created'/> event.
        /// </devdoc>
        protected void OnCreated(FileSystemEventArgs e)
        {
            InvokeOn(e, _onCreatedHandler);
        }

        /// <devdoc>
        ///    Raises the <see cref='System.IO.FileSystemWatcher.Deleted'/> event.
        /// </devdoc>
        protected void OnDeleted(FileSystemEventArgs e)
        {
            InvokeOn(e, _onDeletedHandler);
        }

        private void InvokeOn(FileSystemEventArgs e, FileSystemEventHandler? handler)
        {
            if (handler != null)
            {
                ISynchronizeInvoke? syncObj = SynchronizingObject;
                if (syncObj != null && syncObj.InvokeRequired)
                    syncObj.BeginInvoke(handler, new object[] { this, e });
                else
                    handler(this, e);
            }
        }

        /// <devdoc>
        ///    Raises the <see cref='System.IO.FileSystemWatcher.Error'/> event.
        /// </devdoc>
        protected void OnError(ErrorEventArgs e)
        {
            ErrorEventHandler? handler = _onErrorHandler;
            if (handler != null)
            {
                ISynchronizeInvoke? syncObj = SynchronizingObject;
                if (syncObj != null && syncObj.InvokeRequired)
                    syncObj.BeginInvoke(handler, new object[] { this, e });
                else
                    handler(this, e);
            }
        }

        /// <devdoc>
        ///    Raises the <see cref='System.IO.FileSystemWatcher.Renamed'/> event.
        /// </devdoc>
        protected void OnRenamed(RenamedEventArgs e)
        {
            RenamedEventHandler? handler = _onRenamedHandler;
            if (handler != null)
            {
                ISynchronizeInvoke? syncObj = SynchronizingObject;
                if (syncObj != null && syncObj.InvokeRequired)
                    syncObj.BeginInvoke(handler, new object[] { this, e });
                else
                    handler(this, e);
            }
        }

        public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType) =>
            WaitForChanged(changeType, Timeout.Infinite);

        public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType, int timeout)
        {
            // The full framework implementation doesn't do any argument validation, so
            // none is done here, either.

            var tcs = new TaskCompletionSource<WaitForChangedResult>();
            FileSystemEventHandler? fseh = null;
            RenamedEventHandler? reh = null;

            // Register the event handlers based on what events are desired.  The full framework
            // doesn't register for the Error event, so this doesn't either.
            if ((changeType & (WatcherChangeTypes.Created | WatcherChangeTypes.Deleted | WatcherChangeTypes.Changed)) != 0)
            {
                fseh = (s, e) =>
                {
                    if ((e.ChangeType & changeType) != 0)
                    {
                        tcs.TrySetResult(new WaitForChangedResult(e.ChangeType, e.Name, oldName: null, timedOut: false));
                    }
                };
                if ((changeType & WatcherChangeTypes.Created) != 0)
                    Created += fseh;
                if ((changeType & WatcherChangeTypes.Deleted) != 0)
                    Deleted += fseh;
                if ((changeType & WatcherChangeTypes.Changed) != 0)
                    Changed += fseh;
            }
            if ((changeType & WatcherChangeTypes.Renamed) != 0)
            {
                reh = (s, e) =>
                {
                    if ((e.ChangeType & changeType) != 0)
                    {
                        tcs.TrySetResult(new WaitForChangedResult(e.ChangeType, e.Name, e.OldName, timedOut: false));
                    }
                };
                Renamed += reh;
            }
            try
            {
                // Enable the FSW if it wasn't already.
                bool wasEnabled = EnableRaisingEvents;
                if (!wasEnabled)
                {
                    EnableRaisingEvents = true;
                }

                // Block until an appropriate event arrives or until we timeout.
                Debug.Assert(EnableRaisingEvents, "Expected EnableRaisingEvents to be true");
                tcs.Task.Wait(timeout);

                // Reset the enabled state to what it was.
                EnableRaisingEvents = wasEnabled;
            }
            finally
            {
                // Unregister the event handlers.
                if (reh != null)
                {
                    Renamed -= reh;
                }
                if (fseh != null)
                {
                    if ((changeType & WatcherChangeTypes.Changed) != 0)
                        Changed -= fseh;
                    if ((changeType & WatcherChangeTypes.Deleted) != 0)
                        Deleted -= fseh;
                    if ((changeType & WatcherChangeTypes.Created) != 0)
                        Created -= fseh;
                }
            }

            // Return the results.
            return tcs.Task.IsCompletedSuccessfully ?
                tcs.Task.Result :
                WaitForChangedResult.TimedOutResult;
        }

        public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType, TimeSpan timeout) =>
            WaitForChanged(changeType, ToTimeoutMilliseconds(timeout));

        private static int ToTimeoutMilliseconds(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
            return (int)totalMilliseconds;
        }

        /// <devdoc>
        ///     Stops and starts this object.
        /// </devdoc>
        /// <internalonly/>
        private void Restart()
        {
            if ((!IsSuspended()) && _enabled)
            {
                StopRaisingEvents();
                StartRaisingEventsIfNotDisposed();
            }
        }

        private void StartRaisingEventsIfNotDisposed()
        {
            //Cannot allocate the directoryHandle and the readBuffer if the object has been disposed; finalization has been suppressed.
            ObjectDisposedException.ThrowIf(_disposed, this);
            StartRaisingEvents();
        }

        public override ISite? Site
        {
            get
            {
                return base.Site;
            }
            set
            {
                base.Site = value;

                // set EnableRaisingEvents to true at design time so the user
                // doesn't have to manually.
                if (Site != null && Site.DesignMode)
                    EnableRaisingEvents = true;
            }
        }

        public ISynchronizeInvoke? SynchronizingObject { get; set; }

        public void BeginInit()
        {
            bool oldEnabled = _enabled;
            StopRaisingEvents();
            _enabled = oldEnabled;
            _initializing = true;
        }

        public void EndInit()
        {
            _initializing = false;
            // Start listening to events if _enabled was set to true at some point.
            if (_directory.Length != 0 && _enabled)
                StartRaisingEvents();
        }

        private bool IsSuspended()
        {
            return _initializing || DesignMode;
        }

        private sealed class NormalizedFilterCollection : Collection<string>
        {
            internal NormalizedFilterCollection() : base(new ImmutableStringList())
            {
            }

            protected override void InsertItem(int index, string item)
            {
                base.InsertItem(index, string.IsNullOrEmpty(item) || item == "*.*" ? "*" : item);
            }

            protected override void SetItem(int index, string item)
            {
                base.SetItem(index, string.IsNullOrEmpty(item) || item == "*.*" ? "*" : item);
            }

            internal string[] GetFilters() => ((ImmutableStringList)Items).Items;

            /// <summary>
            /// List that maintains its underlying data in an immutable array, such that the list
            /// will never modify an array returned from its Items property. This is to allow
            /// the array to be enumerated safely while another thread might be concurrently mutating
            /// the collection.
            /// </summary>
            private sealed class ImmutableStringList : IList<string>
            {
                public string[] Items = Array.Empty<string>();

                public string this[int index]
                {
                    get
                    {
                        string[] items = Items;
                        if ((uint)index >= (uint)items.Length)
                        {
                            throw new ArgumentOutOfRangeException(nameof(index));
                        }
                        return items[index];
                    }
                    set
                    {
                        string[] clone = (string[])Items.Clone();
                        clone[index] = value;
                        Items = clone;
                    }
                }

                public int Count => Items.Length;

                public bool IsReadOnly => false;

                public void Add(string item)
                {
                    // Collection<T> doesn't use this method.
                    throw new NotSupportedException();
                }

                public void Clear() => Items = Array.Empty<string>();

                public bool Contains(string item) => Array.IndexOf(Items, item) != -1;

                public void CopyTo(string[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);

                public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)Items).GetEnumerator();

                public int IndexOf(string item) => Array.IndexOf(Items, item);

                public void Insert(int index, string item)
                {
                    string[] items = Items;
                    string[] newItems = new string[items.Length + 1];
                    items.AsSpan(0, index).CopyTo(newItems);
                    items.AsSpan(index).CopyTo(newItems.AsSpan(index + 1));
                    newItems[index] = item;
                    Items = newItems;
                }

                public bool Remove(string item)
                {
                    // Collection<T> doesn't use this method.
                    throw new NotSupportedException();
                }

                public void RemoveAt(int index)
                {
                    string[] items = Items;
                    string[] newItems = new string[items.Length - 1];
                    items.AsSpan(0, index).CopyTo(newItems);
                    items.AsSpan(index + 1).CopyTo(newItems.AsSpan(index));
                    Items = newItems;
                }

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
        }
    }
}
