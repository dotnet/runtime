// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// Security integer
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityInteger
    {
        public uint LowPart;
        public int HighPart;
    }
}
