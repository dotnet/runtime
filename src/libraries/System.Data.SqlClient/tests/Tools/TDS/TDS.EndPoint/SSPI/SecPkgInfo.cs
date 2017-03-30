// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// Security package info
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecPkgInfo
    {
        /// <summary>
        /// Set of bit flags that describes the capabilities of the security package
        /// </summary>
        public uint Capabilities;

        /// <summary>
        /// Specifies the version of the package protocol. Must be 1.
        /// </summary>
        public ushort Version;

        /// <summary>
        /// Specifies a DCE RPC identifier, if appropriate
        /// </summary>
        public ushort RPCID;

        /// <summary>
        /// Specifies the maximum size, in bytes, of the token.
        /// </summary>
        public int MaxToken;

        /// <summary>
        /// Pointer to a null-terminated string that contains the name of the security package.
        /// </summary>
        [MarshalAs(UnmanagedType.LPTStr)]
        public string Name;

        /// <summary>
        /// Pointer to a null-terminated string. This can be any additional string passed back by the package.
        /// </summary>
        [MarshalAs(UnmanagedType.LPTStr)]
        public string Comment;
    }
}
