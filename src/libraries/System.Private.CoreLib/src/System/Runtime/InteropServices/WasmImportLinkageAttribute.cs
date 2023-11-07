// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Used to control WASM import module linkage for an associated P/Invoke.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class WasmImportLinkageAttribute : Attribute
    {
        /// <summary>
        /// Instance constructor.
        /// </summary>
        public WasmImportLinkageAttribute() { }
    }
}
