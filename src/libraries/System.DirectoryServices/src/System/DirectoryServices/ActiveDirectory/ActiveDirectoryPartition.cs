// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.DirectoryServices.ActiveDirectory
{
    public abstract class ActiveDirectoryPartition : IDisposable
    {
        private bool _disposed;
        internal string partitionName = null!;
        internal DirectoryContext context = null!;
        internal DirectoryEntryManager directoryEntryMgr = null!;

        #region constructors
        protected ActiveDirectoryPartition()
        {
        }

        internal ActiveDirectoryPartition(DirectoryContext context, string name)
        {
            this.context = context;
            this.partitionName = name;
        }
        #endregion constructors

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
        }

        // private Dispose method
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // check if this is an explicit Dispose
                // only then clean up the directory entries
                if (disposing)
                {
                    // dispose all directory entries
                    foreach (DirectoryEntry entry in directoryEntryMgr.GetCachedDirectoryEntries())
                    {
                        entry.Dispose();
                    }
                }
                _disposed = true;
            }
        }
        #endregion IDisposable

        #region public methods
        public override string ToString() => Name;

        public abstract DirectoryEntry GetDirectoryEntry();

        #endregion public methods

        #region public properties
        // Public Properties
        public string Name
        {
            get
            {
                CheckIfDisposed();
                return partitionName;
            }
        }
        #endregion public properties

        #region private methods

        internal void CheckIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        #endregion private methods
    }
}
