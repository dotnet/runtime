// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            Debug.Assert(!method.IsGenericMethodDefinition);

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
                    // Rewrite the generic Interlocked.CompareExchange<T> to be a call to one of the non-generic overloads.
                    TypeDesc ceArgType = null;

                    TypeDesc tType = method.Instantiation[0];
                    if (!tType.IsValueType)
                    {
                        ceArgType = method.Context.GetWellKnownType(WellKnownType.Object);
                    }
                    else if ((tType.IsPrimitive || tType.IsEnum) && (tType.UnderlyingType.Category is not (TypeFlags.Single or TypeFlags.Double)))
                    {
                        int size = tType.GetElementSize().AsInt;
                        Debug.Assert(size is 1 or 2 or 4 or 8);
                        ceArgType = size switch
                        {
                            1 => method.Context.GetWellKnownType(WellKnownType.Byte),
                            2 => method.Context.GetWellKnownType(WellKnownType.UInt16),
                            4 => method.Context.GetWellKnownType(WellKnownType.Int32),
                            _ => method.Context.GetWellKnownType(WellKnownType.Int64),
                        };
                    }

                    if (ceArgType is not null)
                    {
                        MethodDesc compareExchangeNonGeneric = method.OwningType.GetKnownMethod("CompareExchange",
                            new MethodSignature(
                                MethodSignatureFlags.Static,
                                genericParameterCount: 0,
                                returnType: ceArgType,
                                parameters: [ceArgType.MakeByRefType(), ceArgType, ceArgType]));

                        ILEmitter emit = new ILEmitter();
                        ILCodeStream codeStream = emit.NewCodeStream();
                        codeStream.EmitLdArg(0);
                        codeStream.EmitLdArg(1);
                        codeStream.EmitLdArg(2);
                        codeStream.Emit(ILOpcode.call, emit.NewToken(compareExchangeNonGeneric));
                        codeStream.Emit(ILOpcode.ret);
                        return emit.Link(method);
                    }
                }
            }

            return null;
        }
    }
}
