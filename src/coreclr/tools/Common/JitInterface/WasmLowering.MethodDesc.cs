// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The MethodDesc-facing half of wasm signature lowering. It is split out of WasmLowering.cs because
// it needs the compiler's MethodDesc extension methods (Common/Compiler/TypeExtensions.cs and
// MethodExtensions.cs), which a tool that only lowers MethodSignatures does not want to link.

using ILCompiler;
using ILCompiler.DependencyAnalysis.Wasm;

using Internal.TypeSystem;

namespace Internal.JitInterface
{
    public static partial class WasmLowering
    {
        /// <summary>
        /// Gets the Wasm-level signature for a given MethodDesc.
        /// </summary>
        public static WasmSignature GetSignature(MethodDesc method)
        {
            return GetSignature(method.Signature, GetLoweringFlags(method));
        }

        public static LoweringFlags GetLoweringFlags(MethodDesc method)
        {
            LoweringFlags flags = 0;
            if (method.RequiresInstMethodDescArg() || method.RequiresInstMethodTableArg())
            {
                flags |= LoweringFlags.HasGenericContextArg;
            }
            if (method.IsAsyncCall())
            {
                flags |= LoweringFlags.IsAsyncCall;
            }
            if (method.IsUnmanagedCallersOnly)
            {
                flags |= LoweringFlags.IsUnmanagedCallersOnly;
            }
            return flags;
        }
    }
}
