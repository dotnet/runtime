// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


// Types moved down into System.Runtime.Handles
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.InteropServices.CriticalHandle))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.InteropServices.SafeHandle))]

namespace System.Runtime.InteropServices
{
    public partial class SafeBuffer
    {
        // Added because SafeHandleZeroOrMinusOneIsInvalid is removed
        public override bool IsInvalid { get { return default(bool); } }
    }
}
