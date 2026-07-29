// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.Wasm;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public partial class ReadyToRunGenericHelperNode : INodeWithTypeSignature
    {
        MethodSignature INodeWithTypeSignature.Signature
        {
            get
            {
                TypeSystemContext context = DictionaryOwner switch
                {
                    TypeDesc type => type.Context,
                    MethodDesc method => method.Context,
                    _ => throw new NotSupportedException()
                };
                TypeDesc intPtrType = context.GetWellKnownType(WellKnownType.IntPtr);
                bool isDelegateCtor = Id == ReadyToRunHelperId.DelegateCtor;
                return new MethodSignature(
                    MethodSignatureFlags.Static,
                    genericParameterCount: 0,
                    isDelegateCtor ? context.GetWellKnownType(WellKnownType.Void) : intPtrType,
                    isDelegateCtor ? [intPtrType, intPtrType, intPtrType] : [intPtrType]);
            }
        }

        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;
        bool INodeWithTypeSignature.IsAsyncCall => false;
        bool INodeWithTypeSignature.HasGenericContextArg => false;

        protected override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            throw new PlatformNotSupportedException("NativeAOT WebAssembly generic lookup helpers are not supported.");
        }

        protected virtual void EmitLoadGenericContext(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            throw new PlatformNotSupportedException("NativeAOT WebAssembly generic context loading is not supported.");
        }
    }

    public partial class ReadyToRunGenericLookupFromTypeNode
    {
        protected override void EmitLoadGenericContext(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            throw new PlatformNotSupportedException(
                "NativeAOT WebAssembly generic context loading from a type is not supported.");
        }
    }
}
