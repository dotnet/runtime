// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.IO
{
    /// <devdoc>
    ///    Listens to the system directory change notifications and
    ///    raises events when a directory or file within a directory changes.
    /// </devdoc>

    public partial class FileSystemWatcher : IDisposable
    {
        /// <devdoc>
        ///     Private instance variables
        /// </devdoc>
        // Directory being monitored
        private string _directory;

        // Filter for name matching
        private string _filter;

        // The watch filter for the API call.
        private const NotifyFilters c_defaultNotifyFilters = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        private NotifyFilters _notifyFilters = c_defaultNotifyFilters;

        // Flag to watch subtree of this directory
        private bool _includeSubdirectories = false;

        // Flag to note whether we are attached to the thread pool and responding to changes
        private bool _enabled = false;

        // Buffer size
        private int _internalBufferSize = 8192;

        // Used for synchronization
        private bool _disposed;

        // Event handlers
        private FileSystemEventHandler _onChangedHandler = null;
        private FileSystemEventHandler _onCreatedHandler = null;
        private FileSystemEventHandler _onDeletedHandler = null;
        private RenamedEventHandler _onRenamedHandler = null;
        private ErrorEventHandler _onErrorHandler = null;

        // To validate the input for "path"
        private static readonly char[] s_wildcards = new char[] { '?', '*' };

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
            foreach (int enumValue in Enum.GetValues(typeof(NotifyFilters)))
                s_notifyFiltersValidMask |= enumValue;
            Debug.Assert(c_notifyFiltersValidMask == s_notifyFiltersValidMask, "The NotifyFilters enum has changed. The c_notifyFiltersValidMask must be updated to reflect the values of the NotifyFilters enum.");
        }
#endif

        ~FileSystemWatcher()
        {
            this.Dispose(false);
        }

        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.IO.FileSystemWatcher'/> class.
        /// </devdoc>
        public FileSystemWatcher()
        {
            _directory = String.Empty;
            _filter = "*.*";
        }

        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.IO.FileSystemWatcher'/> class,
        ///    given the specified directory to monitor.
        /// </devdoc>
        public FileSystemWatcher(string path) : this(path, "*.*")
        {
        }

        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.IO.FileSystemWatcher'/> class,
        ///    given the specified directory and type of files to monitor.
        /// </devdoc>
        public FileSystemWatcher(string path, string filter)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (filter == null)
                throw new ArgumentNullException("filter");

            // Early check for directory parameter so that an exception can be thrown as early as possible.
            if (path.Length == 0 || !Directory.Exists(path))
                throw new ArgumentException(SR.InvalidDirName, path);

            _directory = path;
            _filter = filter;
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
                    throw new ArgumentException(SR.Format(SR.InvalidEnumArgument, "value", (int)value, typeof(NotifyFilters).Name));

                if (_notifyFilters != value)
                {
                    _notifyFilters = value;

                    Restart();
                }
            }
        }

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

                _enabled = value;

                if (_enabled)
                {
                    StartRaisingEventsIfNotDisposed();
                }
                else
                {
                    StopRaisingEvents();
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
                return _filter;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    // Skip the string compare for "*.*" since it has no case-insensitive representation that differs from
                    // the case-sensitive representation.
                    _filter = "*.*";
                }
                else if (String.Compare(_filter, value, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    _filter = value;
                }
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
                return _internalBufferSize;
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
                        _internalBufferSize = value;
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
        public string Path
        {
            get
            {
                return _directory;
            }
            set
            {
                value = (value == null) ? string.Empty : value;
                if (!String.Equals(_directory, value, StringComparison.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(value))
                    {
                        throw new ArgumentException(SR.Format(SR.InvalidDirName, value));
                    }

                    _directory = value;
                    Restart();
                }
            }
        }

        /// <devdoc>
        ///    Occurs when a file or directory in the specified <see cref='System.IO.FileSystemWatcher.Path'/> is changed.
        /// </devdoc>
        public event FileSystemEventHandler Changed
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
        public event FileSystemEventHandler Created
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
        public event FileSystemEventHandler Deleted
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
        public event ErrorEventHandler Error
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
        public event RenamedEventHandler Renamed
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



        /// <devdoc>
        ///    Disposes of the <see cref='System.IO.FileSystemWatcher'/>.
        /// </devdoc>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <devdoc>
        /// </devdoc>
        protected virtual void Dispose(bool disposing)
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
            }
        }

        /// <devdoc>
        ///     Sees if the name given matches the name filter we have.
        /// </devdoc>
        /// <internalonly/>
        private bool MatchPattern(string relativePath)
        {
            string name = System.IO.Path.GetFileName(relativePath);
            if (name != null)
                return PatternMatcher.StrictMatchPattern(_filter.ToUpperInvariant(), name.ToUpperInvariant());
            else
                return false;
        }

        /// <devdoc>
        ///     Raises the event to each handler in the list.
        /// </devdoc>
        /// <internalonly/>
        private void NotifyInternalBufferOverflowEvent()
        {
            InternalBufferOverflowException ex = new InternalBufferOverflowException(SR.Format(SR.FSW_BufferOverflow, _directory));

            ErrorEventArgs errevent = new ErrorEventArgs(ex);

            OnError(errevent);
        }

        /// <devdoc>
        ///     Raises the event to each handler in the list.
        /// </devdoc>
        /// <internalonly/>
        private void NotifyRenameEventArgs(WatcherChangeTypes action, string name, string oldName)
        {
            // filter if neither new name or old name match a specified pattern
            if (!MatchPattern(name) && !MatchPattern(oldName))
            {
                return;
            }

            RenamedEventArgs renevent = new RenamedEventArgs(action, _directory, name, oldName);
            OnRenamed(renevent);
        }

        /// <devdoc>
        ///     Raises the event to each handler in the list.
        /// </devdoc>
        /// <internalonly/>
        private void NotifyFileSystemEventArgs(WatcherChangeTypes changeType, string name)
        {
            if (!MatchPattern(name))
            {
                return;
            }

            var args = new FileSystemEventArgs(changeType, _directory, name);
            switch (changeType)
            {
                case WatcherChangeTypes.Created:
                    OnCreated(args);
                    break;
                case WatcherChangeTypes.Deleted:
                    OnDeleted(args);
                    break;
                case WatcherChangeTypes.Changed:
                    OnChanged(args);
                    break;
                default:
                    Debug.Fail("Unknown FileSystemEvent change type!  Value: " + changeType);
                    break;
            }
        }

        /// <devdoc>
        ///    Raises the <see cref='System.IO.FileSystemWatcher.Changed'/> event.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers", MessageId = "0#", Justification = "Changing from protected to private would be a breaking change")]
        protected void OnChanged(FileSystemEventArgs e)
        {
            // To avoid race between remove handler and raising the event
            FileSystemEventHandler changedHandler = _onChangedHandler;

            if (changedHandler != null)
            {
                changedHandler(this, e);
            }
        }

        /// <devdoc>
        ///    Raises the <see cref='System.IO.FileSystemWatcher.Created'/> event.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers", MessageId = "0#", Justification = "Changing from protected to private would be a breaking change")]
        protected void OnCreated(FileSystemEventArgs e)
        {
            // To avoid race between remove handler and raising the event
            FileSystemEventHandler createdHandler = _onCreatedHandler;
            if (createdHandler != null)
            {
                createdHandler(this, e);
            }
        }

        /// <devdoc>
        ///    Raises the <see cref='System.IO.FileSystemWatcher.Deleted'/> event.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers", MessageId = "0#", Justification = "Changing from protected to private would be a breaking change")]
        protected void OnDeleted(FileSystemEventArgs e)
        {
            // To avoid race between remove handler and raising the event
            FileSystemEventHandler deletedHandler = _onDeletedHandler;
            if (deletedHandler != null)
            {
                deletedHandler(this, e);
            }
        }

        /// <devdoc>
        ///    Raises the <see cref='System.IO.FileSystemWatcher.Error'/> event.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers", MessageId = "0#", Justification = "Changing from protected to private would be a breaking change")]
        protected void OnError(ErrorEventArgs e)
        {
            // To avoid race between remove handler and raising the event
            ErrorEventHandler errorHandler = _onErrorHandler;
            if (errorHandler != null)
            {
                errorHandler(this, e);
            }
        }

        /// <devdoc>
        ///    Raises the <see cref='System.IO.FileSystemWatcher.Renamed'/> event.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers", MessageId = "0#", Justification = "Changing from protected to private would be a breaking change")]
        protected void OnRenamed(RenamedEventArgs e)
        {
            RenamedEventHandler renamedHandler = _onRenamedHandler;
            if (renamedHandler != null)
            {
                renamedHandler(this, e);
            }
        }

        /// <devdoc>
        ///     Stops and starts this object.
        /// </devdoc>
        /// <internalonly/>
        private void Restart()
        {
            if (_enabled)
            {
                StopRaisingEvents();
                StartRaisingEventsIfNotDisposed();
            }
        }

        private void StartRaisingEventsIfNotDisposed()
        {
            //Cannot allocate the directoryHandle and the readBuffer if the object has been disposed; finalization has been suppressed.
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
            StartRaisingEvents();
        }
    }
}
