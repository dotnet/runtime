// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Specifies that the P/Invoke marked with this attribute should be linked in as a WASM import.
    /// </summary>
    /// <remarks>
    /// See https://webassembly.github.io/spec/core/syntax/modules.html#imports.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class WasmImportLinkageAttribute : Attribute
    {
        /// <summary>
        /// Instance constructor.
        /// </summary>
        public WasmImportLinkageAttribute() { }
    }
}
