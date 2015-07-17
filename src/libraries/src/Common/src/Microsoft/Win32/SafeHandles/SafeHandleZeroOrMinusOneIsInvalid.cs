// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    // Issue 2499: Replace ad-hoc definitions of SafeHandleZeroOrMinusOneIsInvalid with a single definition
    //
    // Other definitions of this type should be removed in favor of this definition.
    internal abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
    {
        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero || handle == (IntPtr)(-1); }
        }
    }
}
