// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method bodies for System.Threading.Interlocked intrinsics.
    /// </summary>
    public static class InterlockedIntrinsics
    {
        public static MethodIL EmitIL(
#if READYTORUN
            ILCompiler.CompilationModuleGroup compilationModuleGroup,
#endif // READYTORUN
            MethodDesc method)
        {
            Debug.Assert(((MetadataType)method.OwningType).Name == "Interlocked");

            if (method.HasInstantiation && method.Name == "CompareExchange")
            {
#if READYTORUN
                // Check to see if the tokens needed to describe the CompareExchange are naturally present within
                // the compilation. The current implementation of stable tokens used by cross module inlining is not
                // compatible with rewriting the IL of a compiler generated intrinsic. Fortunately, it turns out
                // that the managed implementation of this intrinsic is correct, just a few more IL instructions.
                if (compilationModuleGroup.ContainsType(method.OwningType))
#endif // READYTORUN
                {
                    TypeDesc objectType = method.Context.GetWellKnownType(WellKnownType.Object);
                    MethodDesc compareExchangeObject = method.OwningType.GetKnownMethod("CompareExchange",
                        new MethodSignature(
                            MethodSignatureFlags.Static,
                            genericParameterCount: 0,
                            returnType: objectType,
                            parameters: new TypeDesc[] { objectType.MakeByRefType(), objectType, objectType }));

                    ILEmitter emit = new ILEmitter();
                    ILCodeStream codeStream = emit.NewCodeStream();
                    codeStream.EmitLdArg(0);
                    codeStream.EmitLdArg(1);
                    codeStream.EmitLdArg(2);
                    codeStream.Emit(ILOpcode.call, emit.NewToken(compareExchangeObject));
                    codeStream.Emit(ILOpcode.ret);
                    return emit.Link(method);
                }
            }

            return null;
        }
    }
}
