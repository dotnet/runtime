// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GCPerfTestFramework.Metrics.Builders
{
    // Condemned reasons are organized into the following groups.
    // Each group corresponds to one or more reasons. 
    // Groups are organized in the way that they mean something to users. 
    internal enum CondemnedReasonGroup
    {
        // The first 4 will have values of a number which is the generation.
        // Note that right now these 4 have the exact same value as what's in
        // Condemned_Reason_Generation.
        CRG_Initial_Generation = 0,
        CRG_Final_Generation = 1,
        CRG_Alloc_Exceeded = 2,
        CRG_Time_Tuning = 3,

        // The following are either true(1) or false(0). They are not 
        // a 1:1 mapping from 
        CRG_Induced = 4,
        CRG_Low_Ephemeral = 5,
        CRG_Expand_Heap = 6,
        CRG_Fragmented_Ephemeral = 7,
        CRG_Fragmented_Gen1_To_Gen2 = 8,
        CRG_Fragmented_Gen2 = 9,
        CRG_Fragmented_Gen2_High_Mem = 10,
        CRG_GC_Before_OOM = 11,
        CRG_Too_Small_For_BGC = 12,
        CRG_Ephemeral_Before_BGC = 13,
        CRG_Internal_Tuning = 14,
        CRG_Max = 15,
    }

}
