// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Win32;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Exposes all the metadata for a specific event Provider.  An instance
    /// of this class is obtained from EventLogManagement and is scoped to a
    /// single Locale.
    /// </summary>
    public class ProviderMetadata : IDisposable
    {
        //
        // access to the data member reference is safe, while
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        private readonly EventLogHandle _handle = EventLogHandle.Zero;

        private EventLogHandle _defaultProviderHandle = EventLogHandle.Zero;

        private readonly EventLogSession _session;

        private readonly string _providerName;
        private readonly CultureInfo _cultureInfo;
        private readonly string _logFilePath;

        // caching of the IEnumerable<EventLevel>, <EventTask>, <EventKeyword>, <EventOpcode> on the ProviderMetadata
        // they do not change with every call.
        private IList<EventLevel> _levels;
        private IList<EventOpcode> _opcodes;
        private IList<EventTask> _tasks;
        private IList<EventKeyword> _keywords;
        private IList<EventLevel> _standardLevels;
        private IList<EventOpcode> _standardOpcodes;
        private IList<EventTask> _standardTasks;
        private IList<EventKeyword> _standardKeywords;
        private IList<EventLogLink> _channelReferences;

        private readonly object _syncObject;

        public ProviderMetadata(string providerName)
            : this(providerName, null, null, null)
        {
        }

        public ProviderMetadata(string providerName, EventLogSession session, CultureInfo targetCultureInfo)
            : this(providerName, session, targetCultureInfo, null)
        {
        }

        internal ProviderMetadata(string providerName, EventLogSession session, CultureInfo targetCultureInfo, string logFilePath)
        {
            targetCultureInfo ??= CultureInfo.CurrentCulture;
            session ??= EventLogSession.GlobalSession;

            _session = session;
            _providerName = providerName;
            _cultureInfo = targetCultureInfo;
            _logFilePath = logFilePath;

            _handle = NativeWrapper.EvtOpenProviderMetadata(_session.Handle, _providerName, _logFilePath, 0);

            _syncObject = new object();
        }

        internal EventLogHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        public string Name
        {
            get { return _providerName; }
        }

        public Guid Id
        {
            get
            {
                return (Guid)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataPublisherGuid);
            }
        }

        public string MessageFilePath
        {
            get
            {
                return (string)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataMessageFilePath);
            }
        }

        public string ResourceFilePath
        {
            get
            {
                return (string)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataResourceFilePath);
            }
        }

        public string ParameterFilePath
        {
            get
            {
                return (string)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataParameterFilePath);
            }
        }

        public Uri HelpLink
        {
            get
            {
                string helpLinkStr = (string)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataHelpLink);
                if (string.IsNullOrEmpty(helpLinkStr))
                    return null;
                return new Uri(helpLinkStr);
            }
        }

        private uint ProviderMessageID
        {
            get
            {
                return (uint)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataPublisherMessageID);
            }
        }

        public string DisplayName
        {
            get
            {
                uint msgId = (uint)this.ProviderMessageID;

                if (msgId == 0xffffffff)
                    return null;

                return NativeWrapper.EvtFormatMessage(_handle, msgId);
            }
        }

        public IList<EventLogLink> LogLinks
        {
            get
            {
                EventLogHandle elHandle = EventLogHandle.Zero;
                try
                {
                    lock (_syncObject)
                    {
                        if (_channelReferences != null)
                            return _channelReferences;

                        elHandle = NativeWrapper.EvtGetPublisherMetadataPropertyHandle(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataChannelReferences);

                        int arraySize = NativeWrapper.EvtGetObjectArraySize(elHandle);

                        List<EventLogLink> channelList = new List<EventLogLink>(arraySize);

                        for (int index = 0; index < arraySize; index++)
                        {
                            string channelName = (string)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataChannelReferencePath);

                            uint channelId = (uint)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataChannelReferenceID);

                            uint flag = (uint)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataChannelReferenceFlags);

                            bool isImported;
                            if (flag == (int)Interop.Wevtapi.EVT_CHANNEL_REFERENCE_FLAGS.EvtChannelReferenceImported)
                                isImported = true;
                            else
                                isImported = false;

                            int channelRefMessageId = unchecked((int)((uint)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataChannelReferenceMessageID)));
                            string channelRefDisplayName;

                            // if channelRefMessageId == -1, we do not have anything in the message table.
                            if (channelRefMessageId == -1)
                            {
                                channelRefDisplayName = null;
                            }
                            else
                            {
                                channelRefDisplayName = NativeWrapper.EvtFormatMessage(_handle, unchecked((uint)channelRefMessageId));
                            }

                            if (channelRefDisplayName == null && isImported)
                            {
                                if (string.Equals(channelName, "Application", StringComparison.OrdinalIgnoreCase))
                                    channelRefMessageId = 256;
                                else if (string.Equals(channelName, "System", StringComparison.OrdinalIgnoreCase))
                                    channelRefMessageId = 258;
                                else if (string.Equals(channelName, "Security", StringComparison.OrdinalIgnoreCase))
                                    channelRefMessageId = 257;
                                else
                                    channelRefMessageId = -1;

                                if (channelRefMessageId != -1)
                                {
                                    if (_defaultProviderHandle.IsInvalid)
                                    {
                                        _defaultProviderHandle = NativeWrapper.EvtOpenProviderMetadata(_session.Handle, null, null, 0);
                                    }

                                    channelRefDisplayName = NativeWrapper.EvtFormatMessage(_defaultProviderHandle, unchecked((uint)channelRefMessageId));
                                }
                            }

                            channelList.Add(new EventLogLink(channelName, isImported, channelRefDisplayName, channelId));
                        }

                        _channelReferences = channelList.AsReadOnly();
                    }

                    return _channelReferences;
                }
                finally
                {
                    elHandle.Dispose();
                }
            }
        }

        internal enum ObjectTypeName
        {
            Level = 0,
            Opcode = 1,
            Task = 2,
            Keyword = 3
        }

        internal string FindStandardLevelDisplayName(string name, uint value)
        {
            _standardLevels ??= (List<EventLevel>)GetProviderListProperty(_defaultProviderHandle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataLevels);
            foreach (EventLevel standardLevel in _standardLevels)
            {
                if (standardLevel.Name == name && standardLevel.Value == value)
                    return standardLevel.DisplayName;
            }
            return null;
        }
        internal string FindStandardOpcodeDisplayName(string name, uint value)
        {
            _standardOpcodes ??= (List<EventOpcode>)GetProviderListProperty(_defaultProviderHandle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataOpcodes);
            foreach (EventOpcode standardOpcode in _standardOpcodes)
            {
                if (standardOpcode.Name == name && standardOpcode.Value == value)
                    return standardOpcode.DisplayName;
            }
            return null;
        }
        internal string FindStandardKeywordDisplayName(string name, long value)
        {
            _standardKeywords ??= (List<EventKeyword>)GetProviderListProperty(_defaultProviderHandle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataKeywords);
            foreach (EventKeyword standardKeyword in _standardKeywords)
            {
                if (standardKeyword.Name == name && standardKeyword.Value == value)
                    return standardKeyword.DisplayName;
            }
            return null;
        }
        internal string FindStandardTaskDisplayName(string name, uint value)
        {
            _standardTasks ??= (List<EventTask>)GetProviderListProperty(_defaultProviderHandle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataTasks);
            foreach (EventTask standardTask in _standardTasks)
            {
                if (standardTask.Name == name && standardTask.Value == value)
                    return standardTask.DisplayName;
            }
            return null;
        }

        internal object GetProviderListProperty(EventLogHandle providerHandle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID metadataProperty)
        {
            EventLogHandle elHandle = EventLogHandle.Zero;

            try
            {
                Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID propName;
                Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID propValue;
                Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID propMessageId;
                ObjectTypeName objectTypeName;

                List<EventLevel> levelList = null;
                List<EventOpcode> opcodeList = null;
                List<EventKeyword> keywordList = null;
                List<EventTask> taskList = null;

                elHandle = NativeWrapper.EvtGetPublisherMetadataPropertyHandle(providerHandle, metadataProperty);

                int arraySize = NativeWrapper.EvtGetObjectArraySize(elHandle);

                switch (metadataProperty)
                {
                    case Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataLevels:
                        propName = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataLevelName;
                        propValue = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataLevelValue;
                        propMessageId = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataLevelMessageID;
                        objectTypeName = ObjectTypeName.Level;
                        levelList = new List<EventLevel>(arraySize);
                        break;

                    case Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataOpcodes:
                        propName = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataOpcodeName;
                        propValue = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataOpcodeValue;
                        propMessageId = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataOpcodeMessageID;
                        objectTypeName = ObjectTypeName.Opcode;
                        opcodeList = new List<EventOpcode>(arraySize);
                        break;

                    case Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataKeywords:
                        propName = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataKeywordName;
                        propValue = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataKeywordValue;
                        propMessageId = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataKeywordMessageID;
                        objectTypeName = ObjectTypeName.Keyword;
                        keywordList = new List<EventKeyword>(arraySize);
                        break;

                    case Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataTasks:
                        propName = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataTaskName;
                        propValue = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataTaskValue;
                        propMessageId = Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataTaskMessageID;
                        objectTypeName = ObjectTypeName.Task;
                        taskList = new List<EventTask>(arraySize);
                        break;

                    default:
                        return null;
                }
                for (int index = 0; index < arraySize; index++)
                {
                    string generalName = (string)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)propName);

                    uint generalValue = 0;
                    long generalValueKeyword = 0;
                    if (objectTypeName != ObjectTypeName.Keyword)
                    {
                        generalValue = (uint)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)propValue);
                    }
                    else
                    {
                        generalValueKeyword = unchecked((long)((ulong)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)propValue)));
                    }

                    int generalMessageId = unchecked((int)((uint)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)propMessageId)));

                    string generalDisplayName = null;

                    if (generalMessageId == -1)
                    {
                        if (providerHandle != _defaultProviderHandle)
                        {
                            if (_defaultProviderHandle.IsInvalid)
                            {
                                _defaultProviderHandle = NativeWrapper.EvtOpenProviderMetadata(_session.Handle, null, null, 0);
                            }

                            generalDisplayName = objectTypeName switch
                            {
                                ObjectTypeName.Level => FindStandardLevelDisplayName(generalName, generalValue),
                                ObjectTypeName.Opcode => FindStandardOpcodeDisplayName(generalName, generalValue >> 16),
                                ObjectTypeName.Keyword => FindStandardKeywordDisplayName(generalName, generalValueKeyword),
                                ObjectTypeName.Task => FindStandardTaskDisplayName(generalName, generalValue),
                                _ => null,
                            };
                        }
                    }
                    else
                    {
                        generalDisplayName = NativeWrapper.EvtFormatMessage(providerHandle, unchecked((uint)generalMessageId));
                    }

                    switch (objectTypeName)
                    {
                        case ObjectTypeName.Level:
                            levelList.Add(new EventLevel(generalName, (int)generalValue, generalDisplayName));
                            break;
                        case ObjectTypeName.Opcode:
                            opcodeList.Add(new EventOpcode(generalName, (int)(generalValue >> 16), generalDisplayName));
                            break;
                        case ObjectTypeName.Keyword:
                            keywordList.Add(new EventKeyword(generalName, (long)generalValueKeyword, generalDisplayName));
                            break;
                        case ObjectTypeName.Task:
                            Guid taskGuid = (Guid)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataTaskEventGuid);
                            taskList.Add(new EventTask(generalName, (int)generalValue, generalDisplayName, taskGuid));
                            break;
                        default:
                            return null;
                    }
                }

                return objectTypeName switch
                {
                    ObjectTypeName.Level => levelList,
                    ObjectTypeName.Opcode => opcodeList,
                    ObjectTypeName.Keyword => keywordList,
                    ObjectTypeName.Task => taskList,
                    _ => null,
                };
            }
            finally
            {
                elHandle.Dispose();
            }
        }

        public IList<EventLevel> Levels
        {
            get
            {
                List<EventLevel> el;
                lock (_syncObject)
                {
                    if (_levels != null)
                        return _levels;

                    el = (List<EventLevel>)this.GetProviderListProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataLevels);
                    _levels = el.AsReadOnly();
                }
                return _levels;
            }
        }

        public IList<EventOpcode> Opcodes
        {
            get
            {
                List<EventOpcode> eo;
                lock (_syncObject)
                {
                    if (_opcodes != null)
                        return _opcodes;

                    eo = (List<EventOpcode>)this.GetProviderListProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataOpcodes);
                    _opcodes = eo.AsReadOnly();
                }
                return _opcodes;
            }
        }

        public IList<EventKeyword> Keywords
        {
            get
            {
                List<EventKeyword> ek;
                lock (_syncObject)
                {
                    if (_keywords != null)
                        return _keywords;

                    ek = (List<EventKeyword>)this.GetProviderListProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataKeywords);
                    _keywords = ek.AsReadOnly();
                }
                return _keywords;
            }
        }

        public IList<EventTask> Tasks
        {
            get
            {
                List<EventTask> et;
                lock (_syncObject)
                {
                    if (_tasks != null)
                        return _tasks;

                    et = (List<EventTask>)this.GetProviderListProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataTasks);
                    _tasks = et.AsReadOnly();
                }
                return _tasks;
            }
        }

        public IEnumerable<EventMetadata> Events
        {
            get
            {
                List<EventMetadata> emList = new List<EventMetadata>();

                EventLogHandle emEnumHandle = NativeWrapper.EvtOpenEventMetadataEnum(_handle, 0);

                using (emEnumHandle)
                {
                    while (true)
                    {
                        EventLogHandle emHandle = emHandle = NativeWrapper.EvtNextEventMetadata(emEnumHandle, 0);
                        if (emHandle == null)
                            break;

                        using (emHandle)
                        {
                            unchecked
                            {
                                uint emId = (uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, Interop.Wevtapi.EVT_EVENT_METADATA_PROPERTY_ID.EventMetadataEventID);
                                byte emVersion = (byte)((uint)(NativeWrapper.EvtGetEventMetadataProperty(emHandle, Interop.Wevtapi.EVT_EVENT_METADATA_PROPERTY_ID.EventMetadataEventVersion)));
                                byte emChannelId = (byte)((uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, Interop.Wevtapi.EVT_EVENT_METADATA_PROPERTY_ID.EventMetadataEventChannel));
                                byte emLevel = (byte)((uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, Interop.Wevtapi.EVT_EVENT_METADATA_PROPERTY_ID.EventMetadataEventLevel));
                                byte emOpcode = (byte)((uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, Interop.Wevtapi.EVT_EVENT_METADATA_PROPERTY_ID.EventMetadataEventOpcode));
                                short emTask = (short)((uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, Interop.Wevtapi.EVT_EVENT_METADATA_PROPERTY_ID.EventMetadataEventTask));
                                long emKeywords = (long)(ulong)NativeWrapper.EvtGetEventMetadataProperty(emHandle, Interop.Wevtapi.EVT_EVENT_METADATA_PROPERTY_ID.EventMetadataEventKeyword);
                                string emTemplate = (string)NativeWrapper.EvtGetEventMetadataProperty(emHandle, Interop.Wevtapi.EVT_EVENT_METADATA_PROPERTY_ID.EventMetadataEventTemplate);
                                int messageId = (int)((uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, Interop.Wevtapi.EVT_EVENT_METADATA_PROPERTY_ID.EventMetadataEventMessageID));

                                string emMessage = (messageId == -1)
                                    ? null
                                    : NativeWrapper.EvtFormatMessage(_handle, (uint)messageId);

                                EventMetadata em = new EventMetadata(emId, emVersion, emChannelId, emLevel, emOpcode, emTask, emKeywords, emTemplate, emMessage, this);
                                emList.Add(em);
                            }
                        }
                    }
                    return emList.AsReadOnly();
                }
            }
        }

        // throws if Provider metadata has been uninstalled since this object was created.
        internal void CheckReleased()
        {
            lock (_syncObject)
            {
                this.GetProviderListProperty(_handle, Interop.Wevtapi.EVT_PUBLISHER_METADATA_PROPERTY_ID.EvtPublisherMetadataTasks);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_handle != null && !_handle.IsInvalid)
                _handle.Dispose();
        }
    }
}
