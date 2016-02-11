// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    public partial class SafeWaitHandle
    {
        // Manually added because SafeHandleZeroOrMinusOneIsInvalid is removed.
        public override bool IsInvalid {[System.Security.SecurityCriticalAttribute]get { return default(bool); } }
    }
}

namespace System.Runtime.InteropServices
{
    public partial class CriticalHandle
    {
        // Manually added because CriticalFinalizerObject.Finalize is removed.
        ~CriticalHandle() { }
    }

    public partial class SafeHandle
    {
        // Manually added because CriticalFinalizerObject.Finalize is removed.
        ~SafeHandle() { }
    }
}
