// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
