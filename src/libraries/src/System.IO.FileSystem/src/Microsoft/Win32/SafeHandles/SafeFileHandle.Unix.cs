// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.Security;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeFileHandle : SafeHandle
    {
        internal bool? IsAsync { get; set; }
    }
}
