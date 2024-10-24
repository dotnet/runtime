// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        internal enum EVT_CHANNEL_CONFIG_PROPERTY_ID
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
    }
}
