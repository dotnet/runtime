// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        internal enum EVT_SYSTEM_PROPERTY_ID
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
    }
}
