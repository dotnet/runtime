// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.Win32
{
    internal static partial class UnsafeNativeMethods
    {
        // Event log specific codes:

        internal const int ERROR_EVT_MESSAGE_NOT_FOUND = 15027;
        internal const int ERROR_EVT_MESSAGE_ID_NOT_FOUND = 15028;
        internal const int ERROR_EVT_UNRESOLVED_VALUE_INSERT = 15029;
        internal const int ERROR_EVT_UNRESOLVED_PARAMETER_INSERT = 15030;
        internal const int ERROR_EVT_MAX_INSERTS_REACHED = 15031;
        internal const int ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND = 15033;
        internal const int ERROR_MUI_FILE_NOT_FOUND = 15100;

        internal enum EvtQueryFlags
        {
            EvtQueryChannelPath = 0x1,
            EvtQueryFilePath = 0x2,
            EvtQueryForwardDirection = 0x100,
            EvtQueryReverseDirection = 0x200,
            EvtQueryTolerateQueryErrors = 0x1000
        }

        [Flags]
        internal enum EvtSubscribeFlags
        {
            EvtSubscribeToFutureEvents = 1,
            EvtSubscribeStartAtOldestRecord = 2,
            EvtSubscribeStartAfterBookmark = 3,
            EvtSubscribeTolerateQueryErrors = 0x1000,
            EvtSubscribeStrict = 0x10000
        }

        /// <summary>
        /// Evt Variant types
        /// </summary>
        internal enum EvtVariantType
        {
            EvtVarTypeNull = 0,
            EvtVarTypeString = 1,
            EvtVarTypeAnsiString = 2,
            EvtVarTypeSByte = 3,
            EvtVarTypeByte = 4,
            EvtVarTypeInt16 = 5,
            EvtVarTypeUInt16 = 6,
            EvtVarTypeInt32 = 7,
            EvtVarTypeUInt32 = 8,
            EvtVarTypeInt64 = 9,
            EvtVarTypeUInt64 = 10,
            EvtVarTypeSingle = 11,
            EvtVarTypeDouble = 12,
            EvtVarTypeBoolean = 13,
            EvtVarTypeBinary = 14,
            EvtVarTypeGuid = 15,
            EvtVarTypeSizeT = 16,
            EvtVarTypeFileTime = 17,
            EvtVarTypeSysTime = 18,
            EvtVarTypeSid = 19,
            EvtVarTypeHexInt32 = 20,
            EvtVarTypeHexInt64 = 21,
            // these types used internally
            EvtVarTypeEvtHandle = 32,
            EvtVarTypeEvtXml = 35,
            // Array = 128
            EvtVarTypeStringArray = 129,
            EvtVarTypeUInt32Array = 136
        }

        internal enum EvtMasks
        {
            EVT_VARIANT_TYPE_MASK = 0x7f,
            EVT_VARIANT_TYPE_ARRAY = 128
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SystemTime
        {
            [MarshalAs(UnmanagedType.U2)]
            public short Year;
            [MarshalAs(UnmanagedType.U2)]
            public short Month;
            [MarshalAs(UnmanagedType.U2)]
            public short DayOfWeek;
            [MarshalAs(UnmanagedType.U2)]
            public short Day;
            [MarshalAs(UnmanagedType.U2)]
            public short Hour;
            [MarshalAs(UnmanagedType.U2)]
            public short Minute;
            [MarshalAs(UnmanagedType.U2)]
            public short Second;
            [MarshalAs(UnmanagedType.U2)]
            public short Milliseconds;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto)]
#pragma warning disable 618 // Ssytem.Core still uses SecurityRuleSet.Level1
        [SecurityCritical(SecurityCriticalScope.Everything)]
#pragma warning restore 618
        internal struct EvtVariant
        {
            [FieldOffset(0)]
            public uint UInteger;
            [FieldOffset(0)]
            public int Integer;
            [FieldOffset(0)]
            public byte UInt8;
            [FieldOffset(0)]
            public short Short;
            [FieldOffset(0)]
            public ushort UShort;
            [FieldOffset(0)]
            public uint Bool;
            [FieldOffset(0)]
            public byte ByteVal;
            [FieldOffset(0)]
            public byte SByte;
            [FieldOffset(0)]
            public ulong ULong;
            [FieldOffset(0)]
            public long Long;
            [FieldOffset(0)]
            public float Single;
            [FieldOffset(0)]
            public double Double;
            [FieldOffset(0)]
            public IntPtr StringVal;
            [FieldOffset(0)]
            public IntPtr AnsiString;
            [FieldOffset(0)]
            public IntPtr SidVal;
            [FieldOffset(0)]
            public IntPtr Binary;
            [FieldOffset(0)]
            public IntPtr Reference;
            [FieldOffset(0)]
            public IntPtr Handle;
            [FieldOffset(0)]
            public IntPtr GuidReference;
            [FieldOffset(0)]
            public ulong FileTime;
            [FieldOffset(0)]
            public IntPtr SystemTime;
            [FieldOffset(0)]
            public IntPtr SizeT;
            [FieldOffset(8)]
            public uint Count;   // number of elements (not length) in bytes.
            [FieldOffset(12)]
            public uint Type;
        }

        internal enum EvtEventPropertyId
        {
            EvtEventQueryIDs = 0,
            EvtEventPath = 1
        }

        /// <summary>
        /// The query flags to get information about query
        /// </summary>
        internal enum EvtQueryPropertyId
        {
            EvtQueryNames = 0,   //String;   //Variant will be array of EvtVarTypeString
            EvtQueryStatuses = 1 //UInt32;   //Variant will be Array of EvtVarTypeUInt32
        }

        /// <summary>
        /// Publisher Metadata properties
        /// </summary>
        internal enum EvtPublisherMetadataPropertyId
        {
            EvtPublisherMetadataPublisherGuid = 0,      // EvtVarTypeGuid
            EvtPublisherMetadataResourceFilePath = 1,       // EvtVarTypeString
            EvtPublisherMetadataParameterFilePath = 2,      // EvtVarTypeString
            EvtPublisherMetadataMessageFilePath = 3,        // EvtVarTypeString
            EvtPublisherMetadataHelpLink = 4,               // EvtVarTypeString
            EvtPublisherMetadataPublisherMessageID = 5,     // EvtVarTypeUInt32

            EvtPublisherMetadataChannelReferences = 6,      // EvtVarTypeEvtHandle, ObjectArray
            EvtPublisherMetadataChannelReferencePath = 7,   // EvtVarTypeString
            EvtPublisherMetadataChannelReferenceIndex = 8,  // EvtVarTypeUInt32
            EvtPublisherMetadataChannelReferenceID = 9,     // EvtVarTypeUInt32
            EvtPublisherMetadataChannelReferenceFlags = 10,  // EvtVarTypeUInt32
            EvtPublisherMetadataChannelReferenceMessageID = 11, // EvtVarTypeUInt32

            EvtPublisherMetadataLevels = 12,                 // EvtVarTypeEvtHandle, ObjectArray
            EvtPublisherMetadataLevelName = 13,              // EvtVarTypeString
            EvtPublisherMetadataLevelValue = 14,             // EvtVarTypeUInt32
            EvtPublisherMetadataLevelMessageID = 15,         // EvtVarTypeUInt32

            EvtPublisherMetadataTasks = 16,                  // EvtVarTypeEvtHandle, ObjectArray
            EvtPublisherMetadataTaskName = 17,               // EvtVarTypeString
            EvtPublisherMetadataTaskEventGuid = 18,          // EvtVarTypeGuid
            EvtPublisherMetadataTaskValue = 19,              // EvtVarTypeUInt32
            EvtPublisherMetadataTaskMessageID = 20,          // EvtVarTypeUInt32

            EvtPublisherMetadataOpcodes = 21,                // EvtVarTypeEvtHandle, ObjectArray
            EvtPublisherMetadataOpcodeName = 22,             // EvtVarTypeString
            EvtPublisherMetadataOpcodeValue = 23,            // EvtVarTypeUInt32
            EvtPublisherMetadataOpcodeMessageID = 24,        // EvtVarTypeUInt32

            EvtPublisherMetadataKeywords = 25,               // EvtVarTypeEvtHandle, ObjectArray
            EvtPublisherMetadataKeywordName = 26,            // EvtVarTypeString
            EvtPublisherMetadataKeywordValue = 27,           // EvtVarTypeUInt64
            EvtPublisherMetadataKeywordMessageID = 28//,       // EvtVarTypeUInt32
            // EvtPublisherMetadataPropertyIdEND
        }

        internal enum EvtChannelReferenceFlags
        {
            EvtChannelReferenceImported = 1
        }

        internal enum EvtEventMetadataPropertyId
        {
            EventMetadataEventID,        // EvtVarTypeUInt32
            EventMetadataEventVersion,   // EvtVarTypeUInt32
            EventMetadataEventChannel,   // EvtVarTypeUInt32
            EventMetadataEventLevel,     // EvtVarTypeUInt32
            EventMetadataEventOpcode,    // EvtVarTypeUInt32
            EventMetadataEventTask,      // EvtVarTypeUInt32
            EventMetadataEventKeyword,   // EvtVarTypeUInt64
            EventMetadataEventMessageID, // EvtVarTypeUInt32
            EventMetadataEventTemplate   // EvtVarTypeString
            // EvtEventMetadataPropertyIdEND
        }

        // CHANNEL CONFIGURATION
        internal enum EvtChannelConfigPropertyId
        {
            EvtChannelConfigEnabled = 0,            // EvtVarTypeBoolean
            EvtChannelConfigIsolation,              // EvtVarTypeUInt32, EVT_CHANNEL_ISOLATION_TYPE
            EvtChannelConfigType,                   // EvtVarTypeUInt32, EVT_CHANNEL_TYPE
            EvtChannelConfigOwningPublisher,        // EvtVarTypeString
            EvtChannelConfigClassicEventlog,        // EvtVarTypeBoolean
            EvtChannelConfigAccess,                 // EvtVarTypeString
            EvtChannelLoggingConfigRetention,       // EvtVarTypeBoolean
            EvtChannelLoggingConfigAutoBackup,      // EvtVarTypeBoolean
            EvtChannelLoggingConfigMaxSize,         // EvtVarTypeUInt64
            EvtChannelLoggingConfigLogFilePath,     // EvtVarTypeString
            EvtChannelPublishingConfigLevel,        // EvtVarTypeUInt32
            EvtChannelPublishingConfigKeywords,     // EvtVarTypeUInt64
            EvtChannelPublishingConfigControlGuid,  // EvtVarTypeGuid
            EvtChannelPublishingConfigBufferSize,   // EvtVarTypeUInt32
            EvtChannelPublishingConfigMinBuffers,   // EvtVarTypeUInt32
            EvtChannelPublishingConfigMaxBuffers,   // EvtVarTypeUInt32
            EvtChannelPublishingConfigLatency,      // EvtVarTypeUInt32
            EvtChannelPublishingConfigClockType,    // EvtVarTypeUInt32, EVT_CHANNEL_CLOCK_TYPE
            EvtChannelPublishingConfigSidType,      // EvtVarTypeUInt32, EVT_CHANNEL_SID_TYPE
            EvtChannelPublisherList,                // EvtVarTypeString | EVT_VARIANT_TYPE_ARRAY
            EvtChannelConfigPropertyIdEND
        }

        // LOG INFORMATION
        internal enum EvtLogPropertyId
        {
            EvtLogCreationTime = 0,             // EvtVarTypeFileTime
            EvtLogLastAccessTime,               // EvtVarTypeFileTime
            EvtLogLastWriteTime,                // EvtVarTypeFileTime
            EvtLogFileSize,                     // EvtVarTypeUInt64
            EvtLogAttributes,                   // EvtVarTypeUInt32
            EvtLogNumberOfLogRecords,           // EvtVarTypeUInt64
            EvtLogOldestRecordNumber,           // EvtVarTypeUInt64
            EvtLogFull,                         // EvtVarTypeBoolean
        }

        internal enum EvtExportLogFlags
        {
            EvtExportLogChannelPath = 1,
            EvtExportLogFilePath = 2,
            EvtExportLogTolerateQueryErrors = 0x1000
        }

        // RENDERING
        internal enum EvtRenderContextFlags
        {
            EvtRenderContextValues = 0,      // Render specific properties
            EvtRenderContextSystem = 1,      // Render all system properties (System)
            EvtRenderContextUser = 2         // Render all user properties (User/EventData)
        }

        internal enum EvtRenderFlags
        {
            EvtRenderEventValues = 0,       // Variants
            EvtRenderEventXml = 1,          // XML
            EvtRenderBookmark = 2           // Bookmark
        }

        internal enum EvtFormatMessageFlags
        {
            EvtFormatMessageEvent = 1,
            EvtFormatMessageLevel = 2,
            EvtFormatMessageTask = 3,
            EvtFormatMessageOpcode = 4,
            EvtFormatMessageKeyword = 5,
            EvtFormatMessageChannel = 6,
            EvtFormatMessageProvider = 7,
            EvtFormatMessageId = 8,
            EvtFormatMessageXml = 9
        }

        internal enum EvtSystemPropertyId
        {
            EvtSystemProviderName = 0,          // EvtVarTypeString
            EvtSystemProviderGuid,              // EvtVarTypeGuid
            EvtSystemEventID,                   // EvtVarTypeUInt16
            EvtSystemQualifiers,                // EvtVarTypeUInt16
            EvtSystemLevel,                     // EvtVarTypeUInt8
            EvtSystemTask,                      // EvtVarTypeUInt16
            EvtSystemOpcode,                    // EvtVarTypeUInt8
            EvtSystemKeywords,                  // EvtVarTypeHexInt64
            EvtSystemTimeCreated,               // EvtVarTypeFileTime
            EvtSystemEventRecordId,             // EvtVarTypeUInt64
            EvtSystemActivityID,                // EvtVarTypeGuid
            EvtSystemRelatedActivityID,         // EvtVarTypeGuid
            EvtSystemProcessID,                 // EvtVarTypeUInt32
            EvtSystemThreadID,                  // EvtVarTypeUInt32
            EvtSystemChannel,                   // EvtVarTypeString
            EvtSystemComputer,                  // EvtVarTypeString
            EvtSystemUserID,                    // EvtVarTypeSid
            EvtSystemVersion,                   // EvtVarTypeUInt8
            EvtSystemPropertyIdEND
        }

        // SESSION
        internal enum EvtLoginClass
        {
            EvtRpcLogin = 1
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct EvtRpcLogin
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Server;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string User;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Domain;
            public CoTaskMemUnicodeSafeHandle Password;
            public int Flags;
        }

        // SEEK
        [Flags]
        internal enum EvtSeekFlags
        {
            EvtSeekRelativeToFirst = 1,
            EvtSeekRelativeToLast = 2,
            EvtSeekRelativeToCurrent = 3,
            EvtSeekRelativeToBookmark = 4,
            EvtSeekOriginMask = 7,
            EvtSeekStrict = 0x10000
        }

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtQuery(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string path,
                            [MarshalAs(UnmanagedType.LPWStr)] string query,
                            int flags);

        // SEEK
        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtSeek(
                            EventLogHandle resultSet,
                            long position,
                            EventLogHandle bookmark,
                            int timeout,
                            EvtSeekFlags flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtSubscribe(
                            EventLogHandle session,
                            SafeWaitHandle signalEvent,
                            [MarshalAs(UnmanagedType.LPWStr)] string path,
                            [MarshalAs(UnmanagedType.LPWStr)] string query,
                            EventLogHandle bookmark,
                            IntPtr context,
                            IntPtr callback,
                            int flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtNext(
                            EventLogHandle queryHandle,
                            int eventSize,
                            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] events,
                            int timeout,
                            int flags,
                            ref int returned);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtCancel(EventLogHandle handle);

        [GeneratedDllImport(Interop.Libraries.Wevtapi)]
        internal static partial bool EvtClose(IntPtr handle);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtGetEventInfo(
                            EventLogHandle eventHandle,
                            EvtEventPropertyId propertyId,
                            int bufferSize,
                            IntPtr bufferPtr,
                            out int bufferUsed);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtGetQueryInfo(
                            EventLogHandle queryHandle,
                            EvtQueryPropertyId propertyId,
                            int bufferSize,
                            IntPtr buffer,
                            ref int bufferRequired);

        // PUBLISHER METADATA
        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtOpenPublisherMetadata(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string publisherId,
                            [MarshalAs(UnmanagedType.LPWStr)] string logFilePath,
                            int locale,
                            int flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtGetPublisherMetadataProperty(
                            EventLogHandle publisherMetadataHandle,
                            EvtPublisherMetadataPropertyId propertyId,
                            int flags,
                            int publisherMetadataPropertyBufferSize,
                            IntPtr publisherMetadataPropertyBuffer,
                            out int publisherMetadataPropertyBufferUsed);

        // NEW

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtGetObjectArraySize(
                            EventLogHandle objectArray,
                            out int objectArraySize);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtGetObjectArrayProperty(
                            EventLogHandle objectArray,
                            int propertyId,
                            int arrayIndex,
                            int flags,
                            int propertyValueBufferSize,
                            IntPtr propertyValueBuffer,
                            out int propertyValueBufferUsed);

        // NEW 2
        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtOpenEventMetadataEnum(
                            EventLogHandle publisherMetadata,
                            int flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtNextEventMetadata(
                            EventLogHandle eventMetadataEnum,
                            int flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtGetEventMetadataProperty(
                            EventLogHandle eventMetadata,
                            EvtEventMetadataPropertyId propertyId,
                            int flags,
                            int eventMetadataPropertyBufferSize,
                            IntPtr eventMetadataPropertyBuffer,
                            out int eventMetadataPropertyBufferUsed);

        // Channel Configuration Native Api

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtOpenChannelEnum(
                            EventLogHandle session,
                            int flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial bool EvtNextChannelPath(
                            EventLogHandle channelEnum,
                            int channelPathBufferSize,
                            [Out] char[]? channelPathBuffer,
                            out int channelPathBufferUsed);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtOpenPublisherEnum(
                            EventLogHandle session,
                            int flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial bool EvtNextPublisherId(
                            EventLogHandle publisherEnum,
                            int publisherIdBufferSize,
                            [Out] char[]? publisherIdBuffer,
                            out int publisherIdBufferUsed);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtOpenChannelConfig(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string channelPath,
                            int flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtSaveChannelConfig(
                            EventLogHandle channelConfig,
                            int flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtSetChannelConfigProperty(
                            EventLogHandle channelConfig,
                            EvtChannelConfigPropertyId propertyId,
                            int flags,
                            ref EvtVariant propertyValue);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtGetChannelConfigProperty(
                            EventLogHandle channelConfig,
                            EvtChannelConfigPropertyId propertyId,
                            int flags,
                            int propertyValueBufferSize,
                            IntPtr propertyValueBuffer,
                            out int propertyValueBufferUsed);

        // Log Information Native Api

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtOpenLog(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string path,
                            PathType flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtGetLogInfo(
                            EventLogHandle log,
                            EvtLogPropertyId propertyId,
                            int propertyValueBufferSize,
                            IntPtr propertyValueBuffer,
                            out int propertyValueBufferUsed);

        // LOG MANIPULATION

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtExportLog(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string channelPath,
                            [MarshalAs(UnmanagedType.LPWStr)] string query,
                            [MarshalAs(UnmanagedType.LPWStr)] string targetFilePath,
                            int flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtArchiveExportedLog(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)]string logFilePath,
                            int locale,
                            int flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtClearLog(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)]string channelPath,
                            [MarshalAs(UnmanagedType.LPWStr)]string targetFilePath,
                            int flags);

        // RENDERING
        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtCreateRenderContext(
                            int valuePathsCount,
                            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)]
                                string[] valuePaths,
                            EvtRenderContextFlags flags);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial bool EvtRender(
                            EventLogHandle context,
                            EventLogHandle eventHandle,
                            EvtRenderFlags flags,
                            int buffSize,
                            [Out] char[]? buffer,
                            out int buffUsed,
                            out int propCount);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, EntryPoint = "EvtRender", SetLastError = true)]
        internal static partial bool EvtRender(
                            EventLogHandle context,
                            EventLogHandle eventHandle,
                            EvtRenderFlags flags,
                            int buffSize,
                            IntPtr buffer,
                            out int buffUsed,
                            out int propCount);

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto)]
        internal struct EvtStringVariant
        {
            [MarshalAs(UnmanagedType.LPWStr), FieldOffset(0)]
            public string StringVal;
            [FieldOffset(8)]
            public uint Count;
            [FieldOffset(12)]
            public uint Type;
        };

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable types.
        [DllImport(Interop.Libraries.Wevtapi, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern bool EvtFormatMessage(
                             EventLogHandle publisherMetadataHandle,
                             EventLogHandle eventHandle,
                             uint messageId,
                             int valueCount,
                             EvtStringVariant[] values,
                             EvtFormatMessageFlags flags,
                             int bufferSize,
                             [Out] char[]? buffer,
                             out int bufferUsed);
#pragma warning restore DLLIMPORTGENANALYZER015

        [GeneratedDllImport(Interop.Libraries.Wevtapi, EntryPoint = "EvtFormatMessage", SetLastError = true)]
        internal static partial bool EvtFormatMessageBuffer(
                             EventLogHandle publisherMetadataHandle,
                             EventLogHandle eventHandle,
                             uint messageId,
                             int valueCount,
                             IntPtr values,
                             EvtFormatMessageFlags flags,
                             int bufferSize,
                             IntPtr buffer,
                             out int bufferUsed);

        // SESSION
#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable types.
        [DllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static extern EventLogHandle EvtOpenSession(
                            EvtLoginClass loginClass,
                            ref EvtRpcLogin login,
                            int timeout,
                            int flags);
#pragma warning restore DLLIMPORTGENANALYZER015

        // BOOKMARK
        [GeneratedDllImport(Interop.Libraries.Wevtapi, EntryPoint = "EvtCreateBookmark", SetLastError = true)]
        internal static partial EventLogHandle EvtCreateBookmark(
                            [MarshalAs(UnmanagedType.LPWStr)] string bookmarkXml);

        [GeneratedDllImport(Interop.Libraries.Wevtapi, SetLastError = true)]
        internal static partial bool EvtUpdateBookmark(
                            EventLogHandle bookmark,
                            EventLogHandle eventHandle);
        //
        // EventLog
        //
    }
}
