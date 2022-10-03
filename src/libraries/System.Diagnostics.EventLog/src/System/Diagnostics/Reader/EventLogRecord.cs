// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace System.Diagnostics.Eventing.Reader
{
    public class EventLogRecord : EventRecord
    {
        private readonly EventLogSession _session;

        private readonly NativeWrapper.SystemProperties _systemProperties;
        private string _containerChannel;
        private int[] _matchedQueryIds;

        // A dummy object which is used only for the locking.
        private readonly object _syncObject;

        // Cached DisplayNames for each instance
        private string _levelName;
        private string _taskName;
        private string _opcodeName;
        private IEnumerable<string> _keywordsNames;

        // Cached DisplayNames for each instance
        private bool _levelNameReady;
        private bool _taskNameReady;
        private bool _opcodeNameReady;

        private readonly ProviderMetadataCachedInformation _cachedMetadataInformation;

        internal EventLogRecord(EventLogHandle handle, EventLogSession session, ProviderMetadataCachedInformation cachedMetadataInfo)
        {
            _cachedMetadataInformation = cachedMetadataInfo;
            Handle = handle;
            _session = session;
            _systemProperties = new NativeWrapper.SystemProperties();
            _syncObject = new object();
        }

        internal EventLogHandle Handle
        {
            get;
        }

        internal void PrepareSystemData()
        {
            if (_systemProperties.filled)
                return;

            // Prepare the System Context, if it is not already initialized.
            _session.SetupSystemContext();

            lock (_syncObject)
            {
                if (_systemProperties.filled == false)
                {
                    NativeWrapper.EvtRenderBufferWithContextSystem(_session.renderContextHandleSystem, Handle, UnsafeNativeMethods.EvtRenderFlags.EvtRenderEventValues, _systemProperties);
                    _systemProperties.filled = true;
                }
            }
        }

        public override int Id
        {
            get
            {
                PrepareSystemData();
                if (_systemProperties.Id == null)
                    return 0;
                return (int)_systemProperties.Id;
            }
        }

        public override byte? Version
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.Version;
            }
        }

        public override int? Qualifiers
        {
            get
            {
                PrepareSystemData();
                return (int?)(uint?)_systemProperties.Qualifiers;
            }
        }

        public override byte? Level
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.Level;
            }
        }

        public override int? Task
        {
            get
            {
                PrepareSystemData();
                return (int?)(uint?)_systemProperties.Task;
            }
        }

        public override short? Opcode
        {
            get
            {
                PrepareSystemData();
                return (short?)(ushort?)_systemProperties.Opcode;
            }
        }

        public override long? Keywords
        {
            get
            {
                PrepareSystemData();
                return (long?)_systemProperties.Keywords;
            }
        }

        public override long? RecordId
        {
            get
            {
                PrepareSystemData();
                return (long?)_systemProperties.RecordId;
            }
        }

        public override string ProviderName
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.ProviderName;
            }
        }

        public override Guid? ProviderId
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.ProviderId;
            }
        }

        public override string LogName
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.ChannelName;
            }
        }

        public override int? ProcessId
        {
            get
            {
                PrepareSystemData();
                return (int?)_systemProperties.ProcessId;
            }
        }

        public override int? ThreadId
        {
            get
            {
                PrepareSystemData();
                return (int?)_systemProperties.ThreadId;
            }
        }

        public override string MachineName
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.ComputerName;
            }
        }

        public override System.Security.Principal.SecurityIdentifier UserId
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.UserId;
            }
        }

        public override DateTime? TimeCreated
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.TimeCreated;
            }
        }

        public override Guid? ActivityId
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.ActivityId;
            }
        }

        public override Guid? RelatedActivityId
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.RelatedActivityId;
            }
        }

        public string ContainerLog
        {
            get
            {
                if (_containerChannel != null)
                    return _containerChannel;
                lock (_syncObject)
                {
                    return _containerChannel ??= (string)NativeWrapper.EvtGetEventInfo(this.Handle, UnsafeNativeMethods.EvtEventPropertyId.EvtEventPath);
                }
            }
        }

        public IEnumerable<int> MatchedQueryIds
        {
            get
            {
                if (_matchedQueryIds != null)
                    return _matchedQueryIds;
                lock (_syncObject)
                {
                    return _matchedQueryIds ??= (int[])NativeWrapper.EvtGetEventInfo(this.Handle, UnsafeNativeMethods.EvtEventPropertyId.EvtEventQueryIDs);
                }
            }
        }

        public override EventBookmark Bookmark
        {
            get
            {
                EventLogHandle bookmarkHandle = NativeWrapper.EvtCreateBookmark(null);
                NativeWrapper.EvtUpdateBookmark(bookmarkHandle, Handle);
                string bookmarkText = NativeWrapper.EvtRenderBookmark(bookmarkHandle);

                return new EventBookmark(bookmarkText);
            }
        }

        public override string FormatDescription()
        {
            return _cachedMetadataInformation.GetFormatDescription(this.ProviderName, Handle);
        }

        public override string FormatDescription(IEnumerable<object> values)
        {
            if (values == null)
                return this.FormatDescription();

            // Copy the value IEnumerable to an array.
            string[] theValues = Array.Empty<string>();
            int i = 0;
            foreach (object o in values)
            {
                if (theValues.Length == i)
                    Array.Resize(ref theValues, i + 1);
                if (o is EventProperty elp)
                {
                    theValues[i] = elp.Value.ToString();
                }
                else
                {
                    theValues[i] = o.ToString();
                }
                i++;
            }

            return _cachedMetadataInformation.GetFormatDescription(this.ProviderName, Handle, theValues);
        }

        public override string LevelDisplayName
        {
            get
            {
                if (_levelNameReady)
                    return _levelName;
                lock (_syncObject)
                {
                    if (_levelNameReady == false)
                    {
                        _levelNameReady = true;
                        _levelName = _cachedMetadataInformation.GetLevelDisplayName(this.ProviderName, Handle);
                    }
                    return _levelName;
                }
            }
        }

        public override string OpcodeDisplayName
        {
            get
            {
                lock (_syncObject)
                {
                    if (_opcodeNameReady == false)
                    {
                        _opcodeNameReady = true;
                        _opcodeName = _cachedMetadataInformation.GetOpcodeDisplayName(this.ProviderName, Handle);
                    }
                    return _opcodeName;
                }
            }
        }

        public override string TaskDisplayName
        {
            get
            {
                if (_taskNameReady)
                    return _taskName;
                lock (_syncObject)
                {
                    if (!_taskNameReady)
                    {
                        _taskNameReady = true;
                        _taskName = _cachedMetadataInformation.GetTaskDisplayName(this.ProviderName, Handle);
                    }
                    return _taskName;
                }
            }
        }

        public override IEnumerable<string> KeywordsDisplayNames
        {
            get
            {
                if (_keywordsNames != null)
                    return _keywordsNames;
                lock (_syncObject)
                {
                    return _keywordsNames ??= _cachedMetadataInformation.GetKeywordDisplayNames(this.ProviderName, Handle);
                }
            }
        }

        public override IList<EventProperty> Properties
        {
            get
            {
                _session.SetupUserContext();
                IList<object> properties = NativeWrapper.EvtRenderBufferWithContextUserOrValues(_session.renderContextHandleUser, Handle);
                List<EventProperty> list = new List<EventProperty>();
                foreach (object value in properties)
                {
                    list.Add(new EventProperty(value));
                }
                return list;
            }
        }

        public IList<object> GetPropertyValues(EventLogPropertySelector propertySelector)
        {
            ArgumentNullException.ThrowIfNull(propertySelector);

            return NativeWrapper.EvtRenderBufferWithContextUserOrValues(propertySelector.Handle, Handle);
        }

        public override string ToXml()
        {
            char[] renderBuffer = GC.AllocateUninitializedArray<char>(2000);
            return NativeWrapper.EvtRenderXml(EventLogHandle.Zero, Handle, renderBuffer);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (Handle != null && !Handle.IsInvalid)
                    Handle.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        internal static EventLogHandle GetBookmarkHandleFromBookmark(EventBookmark bookmark)
        {
            if (bookmark == null)
                return EventLogHandle.Zero;
            EventLogHandle handle = NativeWrapper.EvtCreateBookmark(bookmark.BookmarkText);
            return handle;
        }
    }
}
