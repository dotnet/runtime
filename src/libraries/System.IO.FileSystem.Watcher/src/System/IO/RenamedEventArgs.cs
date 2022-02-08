// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    /// <devdoc>
    ///    Provides data for the <see cref='System.IO.FileSystemWatcher.Renamed'/> event.
    /// </devdoc>
    public class RenamedEventArgs : FileSystemEventArgs
    {
        private readonly string? _oldName;
        private readonly string _oldFullPath;

        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.IO.RenamedEventArgs'/> class.
        /// </devdoc>
        public RenamedEventArgs(WatcherChangeTypes changeType, string directory, string? name, string? oldName)
            : base(changeType, directory, name)
        {
            _oldName = oldName;
            _oldFullPath = Path.Join(Path.GetFullPath(directory), oldName);

            if (string.IsNullOrWhiteSpace(oldName))
            {
                _oldFullPath = PathInternal.EnsureTrailingSeparator(_oldFullPath);
            }
        }

        /// <devdoc>
        ///    Gets the previous fully qualified path of the affected file or directory.
        /// </devdoc>
        public string OldFullPath
        {
            get
            {
                return _oldFullPath;
            }
        }

        /// <devdoc>
        ///    Gets the old name of the affected file or directory.
        /// </devdoc>
        public string? OldName
        {
            get
            {
                return _oldName;
            }
        }
    }
}
