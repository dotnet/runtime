// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Abstract derivations of SafeHandle designed to provide the common
// functionality supporting Win32 handles. More specifically, they describe how
// an invalid handle looks (for instance, some handles use -1 as an invalid
// handle value, others use 0).
//
// Further derivations of these classes can specialise this even further (e.g.
// file or registry handles).
// 
//

namespace Microsoft.Win32.SafeHandles
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.Runtime.ConstrainedExecution;

    // Class of safe handle which uses 0 or -1 as an invalid handle.
    [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode=true)]
#endif
    public abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
    {
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle) : base(IntPtr.Zero, ownsHandle) 
        {
        }

#if FEATURE_CORECLR
        // A default constructor is needed to satisfy CoreCLR inheritence rules. It should not be called at runtime
        protected SafeHandleZeroOrMinusOneIsInvalid()
        {
            throw new NotImplementedException();
        }
#endif // FEATURE_CORECLR

        public override bool IsInvalid {
            [System.Security.SecurityCritical]
            get { return handle.IsNull() || handle == new IntPtr(-1); }
        }
    }

    // Class of safe handle which uses only -1 as an invalid handle.
    [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode=true)]
#endif
    public abstract class SafeHandleMinusOneIsInvalid : SafeHandle
    {
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected SafeHandleMinusOneIsInvalid(bool ownsHandle) : base(new IntPtr(-1), ownsHandle) 
        {
        }

#if FEATURE_CORECLR
        // A default constructor is needed to satisfy CoreCLR inheritence rules. It should not be called at runtime
        protected SafeHandleMinusOneIsInvalid()
        {
            throw new NotImplementedException();
        }
#endif // FEATURE_CORECLR

        public override bool IsInvalid {
            [System.Security.SecurityCritical]
            get { return handle == new IntPtr(-1); }
        }
    }

    // Class of critical handle which uses 0 or -1 as an invalid handle.
    [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode=true)]
#endif
    public abstract class CriticalHandleZeroOrMinusOneIsInvalid : CriticalHandle
    {
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected CriticalHandleZeroOrMinusOneIsInvalid() : base(IntPtr.Zero) 
        {
        }

        public override bool IsInvalid {
            [System.Security.SecurityCritical]
            get { return handle.IsNull() || handle == new IntPtr(-1); }
        }
    }

    // Class of critical handle which uses only -1 as an invalid handle.
    [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode=true)]
#endif
    public abstract class CriticalHandleMinusOneIsInvalid : CriticalHandle
    {
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected CriticalHandleMinusOneIsInvalid() : base(new IntPtr(-1)) 
        {
        }

        public override bool IsInvalid {
            [System.Security.SecurityCritical]
            get { return handle == new IntPtr(-1); }
        }
    }

}
