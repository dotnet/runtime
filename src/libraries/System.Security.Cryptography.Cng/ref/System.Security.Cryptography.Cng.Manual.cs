// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    public abstract partial class SafeNCryptHandle : System.Runtime.InteropServices.SafeHandle
    {
        public override bool IsInvalid { get { return default(bool); } }
    }
}
