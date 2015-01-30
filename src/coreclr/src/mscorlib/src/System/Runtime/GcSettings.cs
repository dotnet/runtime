// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace System.Runtime {
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    // These settings are the same format as in clr\src\vm\gcpriv.h
    // make sure you change that file if you change this file!

    [Serializable]
    public enum GCLargeObjectHeapCompactionMode
    {
        Default = 1,
        CompactOnce = 2
    }

    [Serializable]
    public enum GCLatencyMode
    {
        Batch = 0, 
        Interactive = 1,
        LowLatency = 2,
        SustainedLowLatency = 3
    }

    public static class GCSettings 
    {
        public static GCLatencyMode LatencyMode
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get 
            {
                return (GCLatencyMode)(GC.GetGCLatencyMode());
            }

            // We don't want to allow this API when hosted.
            [System.Security.SecurityCritical]  // auto-generated_required
            [HostProtection(MayLeakOnAbort = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set 
            {
                if ((value < GCLatencyMode.Batch) || (value > GCLatencyMode.SustainedLowLatency))
                {
                    throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                }
                Contract.EndContractBlock();

                GC.SetGCLatencyMode((int)value);
            }
        }

        public static GCLargeObjectHeapCompactionMode LargeObjectHeapCompactionMode
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get 
            {
                return (GCLargeObjectHeapCompactionMode)(GC.GetLOHCompactionMode());
            }

            // We don't want to allow this API when hosted.
            [System.Security.SecurityCritical]  // auto-generated_required
            [HostProtection(MayLeakOnAbort = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set 
            {
                if ((value < GCLargeObjectHeapCompactionMode.Default) || 
                    (value > GCLargeObjectHeapCompactionMode.CompactOnce))
                {
                    throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                }
                Contract.EndContractBlock();

                GC.SetLOHCompactionMode((int)value);
            }
        }

        public static bool IsServerGC 
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return GC.IsServerGC();
            }
        }            
    }
}
