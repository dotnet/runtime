// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        internal enum EVT_EVENT_METADATA_PROPERTY_ID
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
    }
}
