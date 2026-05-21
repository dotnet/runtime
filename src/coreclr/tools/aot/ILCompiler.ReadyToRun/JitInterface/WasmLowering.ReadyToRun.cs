// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ILCompiler;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysis.ReadyToRun;

using Internal.TypeSystem;

namespace Internal.JitInterface
{
    public static partial class WasmLowering
    {
        internal static bool CurrentArgLowersValueTypeToPassAsByref(ArgIterator argit)
        {
            if (argit.IsValueType())
            {
                // Check to see if this argument lowers to a byref on the wasm side
                ITypeHandle typeHandle;
                argit.GetArgType(out typeHandle);
                if (WasmLowering.LowerToAbiType(((TypeHandle)typeHandle).GetRuntimeTypeHandle()) == null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
