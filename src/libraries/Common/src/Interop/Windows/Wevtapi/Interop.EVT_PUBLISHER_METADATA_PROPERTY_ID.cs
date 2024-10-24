// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        internal enum EVT_PUBLISHER_METADATA_PROPERTY_ID
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
    }
}
