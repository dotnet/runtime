// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.Wasm;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public partial class JumpStubNode : INodeWithTypeSignature
    {
        private INodeWithTypeSignature TargetWithSignature => (INodeWithTypeSignature)Target;

        MethodSignature INodeWithTypeSignature.Signature => TargetWithSignature.Signature;
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => TargetWithSignature.IsUnmanagedCallersOnly;
        bool INodeWithTypeSignature.IsAsyncCall => TargetWithSignature.IsAsyncCall;
        bool INodeWithTypeSignature.HasGenericContextArg => TargetWithSignature.HasGenericContextArg;

        protected override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            throw new PlatformNotSupportedException("NativeAOT WebAssembly jump stubs are not supported.");
        }
    }
}
