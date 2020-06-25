
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Runtime.Caching
{
    public sealed partial class HostFileChangeMonitor : FileChangeMonitor
    {
        public HostFileChangeMonitor(IList<string> filePaths)
        {
            throw new PlatformNotSupportedException();
        }

        public override ReadOnlyCollection<string> FilePaths
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public override DateTimeOffset LastModified
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public override string UniqueId
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            throw new PlatformNotSupportedException();
        }
    }
}