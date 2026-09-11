// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ILCompiler;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using Internal.CallingConvention;

using Internal.TypeSystem;

namespace Internal.JitInterface
{
    public static partial class WasmLowering
    {
        internal static bool CurrentArgLowersValueTypeToPassAsByref(ArgIterator<TypeHandle> argit)
        {
            if (argit.IsValueType())
            {
                // Check to see if this argument lowers to a byref on the wasm side
                argit.GetArgType(out TypeHandle typeHandle);
                TypeDesc type = typeHandle.GetRuntimeTypeHandle();

                // Types split across several wasm parameters are passed by value, not by reference.
                if (TryGetMultiSegmentLayout(type, out _, out _))
                {
                    return false;
                }

                if (WasmLowering.LowerToAbiType(type) == null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
