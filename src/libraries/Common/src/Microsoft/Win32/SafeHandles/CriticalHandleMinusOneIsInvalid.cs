// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    // Issue 2499: Replace ad-hoc definitions of SafeHandleZeroOrMinusOneIsInvalid with a single definition
    //
    // Other definitions of this type should be removed in favor of this definition.
    internal abstract class CriticalHandleMinusOneIsInvalid : CriticalHandle
    {
        protected CriticalHandleMinusOneIsInvalid() : base(new IntPtr(-1))
        {
        }

        public override bool IsInvalid
        {
            get { return handle == new IntPtr(-1); }
        }
    }
}
