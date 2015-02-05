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

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed partial class SafeThreadHandle : SafeHandle
    {
        private const int DefaultInvalidHandleValue = -1;

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return (long)handle < 0; }
        }

        protected override bool ReleaseHandle()
        {
            // Nop.
            return true;
        }
    }
}
