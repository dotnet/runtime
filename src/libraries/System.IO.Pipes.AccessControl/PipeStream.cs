// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  PipeStream
**
**
** Purpose: Base class for pipe streams.
**
**
===========================================================*/

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes {

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public abstract class PipeStream : Stream {

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        [System.Security.SecurityCritical]
        public PipeSecurity GetAccessControl() {
            if (m_state == PipeState.Closed) {
                __Error.PipeNotOpen();
            }
            if (m_handle == null) {
                throw new InvalidOperationException(SR.GetString(SR.InvalidOperation_PipeHandleNotSet));
            }
            if (m_handle.IsClosed) {
                __Error.PipeNotOpen();
            }

            return new PipeSecurity(m_handle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        [System.Security.SecurityCritical]
        public void SetAccessControl(PipeSecurity pipeSecurity) {
            if (pipeSecurity == null) {
                throw new ArgumentNullException("pipeSecurity");
            }
            CheckPipePropertyOperations();

            pipeSecurity.Persist(m_handle);
        }
    }
}


