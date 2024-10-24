// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        internal enum EVT_QUERY_PROPERTY_ID
        {
            EvtQueryNames = 0,   //String;   //Variant will be array of EvtVarTypeString
            EvtQueryStatuses = 1 //UInt32;   //Variant will be Array of EvtVarTypeUInt32
        }
    }
}
