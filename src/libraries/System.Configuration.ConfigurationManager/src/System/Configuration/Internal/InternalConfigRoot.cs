// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace System.Configuration.Internal
{
    // InternalConfigRoot holds the root of a configuration hierarchy.
    // It managed creation, removal, and the search for BaseConfigurationRecord's.
    // in a thread-safe manner.
    //
    // The BaseConfigurationRecord hierarchy is protected with the
    // _hierarchyLock. Functions that assume that the lock as been
    // taken begin with the prefix "hl", for example, "hlFindConfigRecord".
    internal sealed class InternalConfigRoot : IInternalConfigRoot
    {
        private ReaderWriterLock _hierarchyLock;
        private bool _isDesignTime;

        internal InternalConfigRoot()
        {
        }

        internal InternalConfigRoot(Configuration currentConfiguration, UpdateConfigHost host)
        {
            CurrentConfiguration = currentConfiguration;
            UpdateConfigHost = host;
        }

        internal IInternalConfigHost Host { get; private set; }

        internal UpdateConfigHost UpdateConfigHost { get; private set; }

        internal BaseConfigurationRecord RootConfigRecord { get; private set; }

        internal Configuration CurrentConfiguration { get; }

        public event InternalConfigEventHandler ConfigChanged;
        public event InternalConfigEventHandler ConfigRemoved;

        void IInternalConfigRoot.Init(IInternalConfigHost host, bool isDesignTime)
        {
            Host = host;
            _isDesignTime = isDesignTime;
            _hierarchyLock = new ReaderWriterLock();

            // Dummy record to hold _children for root
            if (_isDesignTime) RootConfigRecord = MgmtConfigurationRecord.Create(this, null, string.Empty, null);
            else
            {
                RootConfigRecord =
                    (BaseConfigurationRecord)RuntimeConfigurationRecord.Create(this, null, string.Empty);
            }
        }

        bool IInternalConfigRoot.IsDesignTime => _isDesignTime;

        public object GetSection(string section, string configPath)
        {
            BaseConfigurationRecord configRecord = (BaseConfigurationRecord)GetUniqueConfigRecord(configPath);
            object result = configRecord.GetSection(section);
            return result;
        }

        // Get the nearest ancestor path (including self) which contains unique configuration information.
        public string GetUniqueConfigPath(string configPath)
        {
            IInternalConfigRecord configRecord = GetUniqueConfigRecord(configPath);

            return configRecord?.ConfigPath;
        }

        // Get the nearest ancestor record (including self) which contains unique configuration information.
        public IInternalConfigRecord GetUniqueConfigRecord(string configPath)
        {
            BaseConfigurationRecord configRecord = (BaseConfigurationRecord)GetConfigRecord(configPath);
            while (configRecord.IsEmpty)
            {
                BaseConfigurationRecord parentConfigRecord = configRecord.Parent;

                // If all config records are empty, return the immediate child of the
                // root placeholder (e.g. machine.config)
                if (parentConfigRecord.IsRootConfig) break;

                configRecord = parentConfigRecord;
            }

            return configRecord;
        }

        // Get the config record for a path.
        // If the record does not exist, create it if it is needed.
        public IInternalConfigRecord GetConfigRecord(string configPath)
        {
            if (!ConfigPathUtility.IsValid(configPath)) throw ExceptionUtil.ParameterInvalid(nameof(configPath));

            string[] parts = ConfigPathUtility.GetParts(configPath);

            // First search under the reader lock, so that multiple searches
            // can proceed in parallel.
            try
            {
                int index;
                BaseConfigurationRecord currentRecord;

                AcquireHierarchyLockForRead();

                HlFindConfigRecord(parts, out index, out currentRecord);

                // check if found
                if ((index == parts.Length) || !currentRecord.HlNeedsChildFor(parts[index])) return currentRecord;
            }
            finally
            {
                ReleaseHierarchyLockForRead();
            }

            // Not found, so search again under exclusive writer lock so that
            // we can create the record.
            try
            {
                int index;
                BaseConfigurationRecord currentRecord;

                AcquireHierarchyLockForWrite();

                HlFindConfigRecord(parts, out index, out currentRecord);

                if (index == parts.Length) return currentRecord;

                string currentConfigPath = string.Join(BaseConfigurationRecord.ConfigPathSeparatorString, parts, 0,
                    index);

                // Create new records
                while ((index < parts.Length) && currentRecord.HlNeedsChildFor(parts[index]))
                {
                    string configName = parts[index];
                    currentConfigPath = ConfigPathUtility.Combine(currentConfigPath, configName);
                    BaseConfigurationRecord childRecord;

                    childRecord = _isDesignTime
                        ? MgmtConfigurationRecord.Create(this, currentRecord, currentConfigPath, null)
                        : (BaseConfigurationRecord) RuntimeConfigurationRecord.Create(this, currentRecord, currentConfigPath);

                    currentRecord.HlAddChild(configName, childRecord);

                    index++;
                    currentRecord = childRecord;
                }

                return currentRecord;
            }
            finally
            {
                ReleaseHierarchyLockForWrite();
            }
        }

        // Find and remove the config record and all its children for the config path.
        public void RemoveConfig(string configPath)
        {
            RemoveConfigImpl(configPath, null);
        }

        private void AcquireHierarchyLockForRead()
        {
            // Protect against unexpected recursive entry on this thread.
            // We do this in retail, too, because the results would be very bad if this were to fail,
            // and the testing for this is not easy for all scenarios.
            if (_hierarchyLock.IsReaderLockHeld)
                throw ExceptionUtil.UnexpectedError(
                    "System.Configuration.Internal.InternalConfigRoot::AcquireHierarchyLockForRead - reader lock already held by this thread");

            if (_hierarchyLock.IsWriterLockHeld)
                throw ExceptionUtil.UnexpectedError(
                    "System.Configuration.Internal.InternalConfigRoot::AcquireHierarchyLockForRead - writer lock already held by this thread");

            _hierarchyLock.AcquireReaderLock(-1);
        }

        private void ReleaseHierarchyLockForRead()
        {
            Debug.Assert(!_hierarchyLock.IsWriterLockHeld, "!_hierarchyLock.IsWriterLockHeld");

            if (_hierarchyLock.IsReaderLockHeld) _hierarchyLock.ReleaseReaderLock();
        }

        private void AcquireHierarchyLockForWrite()
        {
            // Protect against unexpected recursive entry on this thread.
            // We do this in retail, too, because the results would be very bad if this were to fail,
            // and the testing for this is not easy for all scenarios.
            if (_hierarchyLock.IsReaderLockHeld)
                throw ExceptionUtil.UnexpectedError(
                    "System.Configuration.Internal.InternalConfigRoot::AcquireHierarchyLockForWrite - reader lock already held by this thread");

            if (_hierarchyLock.IsWriterLockHeld)
                throw ExceptionUtil.UnexpectedError(
                    "System.Configuration.Internal.InternalConfigRoot::AcquireHierarchyLockForWrite - writer lock already held by this thread");

            _hierarchyLock.AcquireWriterLock(-1);
        }

        private void ReleaseHierarchyLockForWrite()
        {
            Debug.Assert(!_hierarchyLock.IsReaderLockHeld, "!_hierarchyLock.IsReaderLockHeld");

            if (_hierarchyLock.IsWriterLockHeld)
                _hierarchyLock.ReleaseWriterLock();
        }

        // Find a config record.
        // If found, nextIndex == parts.Length and the resulting record is in currentRecord.
        // If not found, nextIndex is the index of the part of the path not found, and currentRecord
        // is the record that has been found so far (nexIndex - 1).
        private void HlFindConfigRecord(string[] parts, out int nextIndex, out BaseConfigurationRecord currentRecord)
        {
            currentRecord = RootConfigRecord;
            nextIndex = 0;
            for (; nextIndex < parts.Length; nextIndex++)
            {
                BaseConfigurationRecord childRecord = currentRecord.HlGetChild(parts[nextIndex]);
                if (childRecord == null)
                    break;

                currentRecord = childRecord;
            }
        }

        // Find and remove the config record and all its children for the config path.
        // Optionally ensure the config record matches a desired config record.
        private void RemoveConfigImpl(string configPath, BaseConfigurationRecord configRecord)
        {
            if (!ConfigPathUtility.IsValid(configPath)) throw ExceptionUtil.ParameterInvalid(nameof(configPath));

            string[] parts = ConfigPathUtility.GetParts(configPath);

            BaseConfigurationRecord currentRecord;

            // search under exclusive writer lock
            try
            {
                int index;
                AcquireHierarchyLockForWrite();
                HlFindConfigRecord(parts, out index, out currentRecord);

                // Return if not found, or does not match the one we are trying to remove.
                if ((index != parts.Length) || ((configRecord != null) && !ReferenceEquals(configRecord, currentRecord)))
                    return;

                // Remove it from the hierarchy.
                currentRecord.Parent.HlRemoveChild(parts[parts.Length - 1]);
            }
            finally
            {
                ReleaseHierarchyLockForWrite();
            }

            OnConfigRemoved(new InternalConfigEventArgs(configPath));

            // Close the record. This is safe to do outside the lock.
            currentRecord.CloseRecursive();
        }

        // Remove the config record and all its children for the config path.
        public void RemoveConfigRecord(BaseConfigurationRecord configRecord)
        {
            RemoveConfigImpl(configRecord.ConfigPath, configRecord);
        }

        // Clear the result of a configSection evaluation at a particular point
        // in the hierarchy.
        public void ClearResult(BaseConfigurationRecord configRecord, string configKey, bool forceEvaluation)
        {
            string[] parts = ConfigPathUtility.GetParts(configRecord.ConfigPath);

            try
            {
                int index;
                BaseConfigurationRecord currentRecord;
                AcquireHierarchyLockForRead();
                HlFindConfigRecord(parts, out index, out currentRecord);

                // clear result only if configRecord it is still in the hierarchy
                if ((index == parts.Length) && ReferenceEquals(configRecord, currentRecord))
                    currentRecord.HlClearResultRecursive(configKey, forceEvaluation);
            }
            finally
            {
                ReleaseHierarchyLockForRead();
            }
        }

        private void OnConfigRemoved(InternalConfigEventArgs e)
        {
            ConfigRemoved?.Invoke(this, e);
        }

        internal void FireConfigChanged(string configPath)
        {
            OnConfigChanged(new InternalConfigEventArgs(configPath));
        }

        private void OnConfigChanged(InternalConfigEventArgs e)
        {
            ConfigChanged?.Invoke(this, e);
        }
    }
}