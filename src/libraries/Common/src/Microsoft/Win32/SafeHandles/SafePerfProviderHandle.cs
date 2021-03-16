// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafePerfProviderHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafePerfProviderHandle() : base(true) { }

        protected override bool ReleaseHandle()
        {
            uint Status = Interop.PerfCounter.PerfStopProvider(handle);
            Debug.Assert(Status == (uint)Interop.Errors.ERROR_SUCCESS, "PerfStopProvider() fails");
            return true;
        }
    }
}
