// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics {
    using System.Runtime.InteropServices;    
    using System;    
    using System.Security.Permissions;    
    using System.Security;
    using Microsoft.Win32;

    // All of this code was ported to native and this implementation is no longer used.  It is not meant to be accessed directly.  
    // This code was no longer maintained, and it accessed the same shared memory that the new code accessed.  To be certain there 
    // are no security holes and no serious bugs, we have removed all of the real code. 
    
    
    /// <internalonly/>
    public sealed class PerformanceCounterManager : ICollectData
    {
        [
            Obsolete("This class has been deprecated.  Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.  http://go.microsoft.com/fwlink/?linkid=14202")
        ]
        public PerformanceCounterManager() {              
        }
        
        /// <internalonly/>
        [
            Obsolete("This class has been deprecated.  Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.  http://go.microsoft.com/fwlink/?linkid=14202")
        ]
        void ICollectData.CollectData(int callIdx, IntPtr valueNamePtr, IntPtr dataPtr, int totalBytes, out IntPtr res) {
            res = (IntPtr)(-1);
        }

        /// <internalonly/>
        [
            Obsolete("This class has been deprecated.  Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.  http://go.microsoft.com/fwlink/?linkid=14202")
        ]
        void ICollectData.CloseData() {            
        }
    }
}
