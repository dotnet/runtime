// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    // We removed base type SafeHandleZeroOrMinusOneIsInvalid and want to make overrides explicit
    public partial class SafeMemoryMappedFileHandle : System.Runtime.InteropServices.SafeHandle
    {
        public override bool IsInvalid { get { return default(bool); } }
    }
}
