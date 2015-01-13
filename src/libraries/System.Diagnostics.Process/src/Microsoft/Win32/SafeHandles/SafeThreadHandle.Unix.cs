// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Class:  SafeThreadHandle 
**
**
** A wrapper for a thread handle
**
** 
===========================================================*/

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed partial class SafeThreadHandle : SafeHandle
    {
        private const int DefaultInvalidHandleValue = -1; // TODO: Determine this

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        protected override bool ReleaseHandle()
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }
    }
}
