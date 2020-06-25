// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace System.IO
{
    public partial class FileSystemWatcher : Component, ISupportInitialize
    {
        public FileSystemWatcher()
        {
            throw new PlatformNotSupportedException();
        }

        public FileSystemWatcher(string path)
        {
            throw new PlatformNotSupportedException();
        }

        public FileSystemWatcher(string path, string filter)
        {
            throw new PlatformNotSupportedException();
        }

        public bool EnableRaisingEvents
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string Filter
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public Collection<string> Filters
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool IncludeSubdirectories
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public int InternalBufferSize
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public NotifyFilters NotifyFilter
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string Path
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public override ISite? Site
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public ISynchronizeInvoke? SynchronizingObject
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public event FileSystemEventHandler? Changed
        {
            add
            {
                throw new PlatformNotSupportedException();
            }
            remove
            {
                throw new PlatformNotSupportedException();
            }
        }

        public event FileSystemEventHandler? Created
        {
            add
            {
                throw new PlatformNotSupportedException();
            }
            remove
            {
                throw new PlatformNotSupportedException();
            }
        }

        public event FileSystemEventHandler? Deleted
        {
            add
            {
                throw new PlatformNotSupportedException();
            }
            remove
            {
                throw new PlatformNotSupportedException();
            }
        }

        public event ErrorEventHandler? Error
        {
            add
            {
                throw new PlatformNotSupportedException();
            }
            remove
            {
                throw new PlatformNotSupportedException();
            }
        }

        public event RenamedEventHandler? Renamed
        {
            add
            {
                throw new PlatformNotSupportedException();
            }
            remove
            {
                throw new PlatformNotSupportedException();
            }
        }

        public void BeginInit()
        {
            throw new PlatformNotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            throw new PlatformNotSupportedException();
        }

        public void EndInit()
        {
            throw new PlatformNotSupportedException();
        }

        protected void OnChanged(FileSystemEventArgs e)
        {
            throw new PlatformNotSupportedException();
        }

        protected void OnCreated(FileSystemEventArgs e)
        {
            throw new PlatformNotSupportedException();
        }

        protected void OnDeleted(FileSystemEventArgs e)
        {
            throw new PlatformNotSupportedException();
        }

        protected void OnError(ErrorEventArgs e)
        {
            throw new PlatformNotSupportedException();
        }

        protected void OnRenamed(RenamedEventArgs e)
        {
            throw new PlatformNotSupportedException();
        }

        public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType)
        {
            throw new PlatformNotSupportedException();
        }

        public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType, int timeout)
        {
            throw new PlatformNotSupportedException();
        }
    }
}