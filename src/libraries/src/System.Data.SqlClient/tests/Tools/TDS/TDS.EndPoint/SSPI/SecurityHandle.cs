// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// Security handle
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityHandle
    {
        public IntPtr LowPart;
        public IntPtr HighPart;

        /// <summary>
        /// Check if instance of the security handle is valid
        /// </summary>
        internal bool IsValid()
        {
            return (LowPart != IntPtr.Zero) || (HighPart != IntPtr.Zero);
        }
    }
}
