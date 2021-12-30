// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Extension to TargetDetails related to Aot
    partial class TargetDetails
    {
        /// <summary>
        /// Offset by which fat function pointers are shifted to distinguish them
        /// from real function pointers.
        /// WebAssembly uses index tables, not addresses for function pointers, so the lower bits are not free to use.  
        /// </summary>
        public int FatFunctionPointerOffset => Architecture == TargetArchitecture.Wasm32 ? 1 << 31 : 2;
    }
}
