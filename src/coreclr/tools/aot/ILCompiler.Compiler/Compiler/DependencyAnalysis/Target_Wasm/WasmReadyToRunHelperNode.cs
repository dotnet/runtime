// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.Wasm;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public partial class ReadyToRunHelperNode : INodeWithTypeSignature
    {
        MethodSignature INodeWithTypeSignature.Signature
        {
            get
            {
                TypeSystemContext context = Target switch
                {
                    DelegateCreationInfo info => info.Constructor.Method.Context,
                    TypeDesc type => type.Context,
                    MethodDesc method => method.Context,
                    _ => throw new NotSupportedException()
                };

                TypeDesc intPtrType = context.GetWellKnownType(WellKnownType.IntPtr);
                TypeDesc returnType;
                TypeDesc[] parameters;
                switch (Id)
                {
                    case ReadyToRunHelperId.DelegateCtor:
                        // The helper receives the two arguments populated by the JIT and supplies
                        // the target method and optional thunk before tailcalling the constructor.
                        returnType = context.GetWellKnownType(WellKnownType.Void);
                        parameters = [intPtrType, intPtrType];
                        break;

                    case ReadyToRunHelperId.ResolveVirtualFunction:
                        returnType = intPtrType;
                        parameters = [intPtrType];
                        break;

                    default:
                        returnType = intPtrType;
                        parameters = [];
                        break;
                }

                return new MethodSignature(
                    MethodSignatureFlags.Static,
                    genericParameterCount: 0,
                    returnType,
                    parameters);
            }
        }

        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;
        bool INodeWithTypeSignature.IsAsyncCall => false;
        bool INodeWithTypeSignature.HasGenericContextArg => false;

        protected override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            throw new PlatformNotSupportedException("NativeAOT WebAssembly ReadyToRun helpers are not supported.");
        }
    }
}
