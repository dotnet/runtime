// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : SafeHandle
    {
        protected override bool ReleaseHandle()
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }
    }
}
