// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    public static class DelegateMethodILEmitter
    {
        public static MethodIL EmitIL(MethodDesc method)
        {
            Debug.Assert(method.OwningType.IsDelegate);
            Debug.Assert(method.OwningType.IsTypeDefinition);
            Debug.Assert(method.IsRuntimeImplemented);

            if (method.Name == "BeginInvoke" || method.Name == "EndInvoke")
            {
                // BeginInvoke and EndInvoke are not supported on .NET Core
                ILEmitter emit = new ILEmitter();
                ILCodeStream codeStream = emit.NewCodeStream();
                MethodDesc notSupportedExceptionHelper = method.Context.GetHelperEntryPoint("ThrowHelpers", "ThrowPlatformNotSupportedException");
                codeStream.EmitCallThrowHelper(emit, notSupportedExceptionHelper);
                return emit.Link(method);
            }

            if (method.Name == ".ctor")
            {
                // We only support delegate creation if the IL follows the delegate creation verifiability requirements
                // described in ECMA-335 III.4.21 (newobj - create a new object). The codegen is expected to
                // intrinsically expand the pattern.
                // If the delegate creation doesn't follow the pattern, we generate code that throws at runtime.
                // We could potentially implement this (unreliably) through the use of reflection metadata,
                // but it remains to be proven that this is an actual customer scenario.
                ILEmitter emit = new ILEmitter();
                ILCodeStream codeStream = emit.NewCodeStream();
                MethodDesc notSupportedExceptionHelper = method.Context.GetHelperEntryPoint("ThrowHelpers", "ThrowPlatformNotSupportedException");
                codeStream.EmitCallThrowHelper(emit, notSupportedExceptionHelper);
                return emit.Link(method);
            }

            if (method.Name == "Invoke")
            {
                TypeSystemContext context = method.Context;

                ILEmitter emit = new ILEmitter();
                TypeDesc delegateType = context.GetWellKnownType(WellKnownType.MulticastDelegate).BaseType;
                FieldDesc firstParameterField = delegateType.GetKnownField("_firstParameter");
                FieldDesc functionPointerField = delegateType.GetKnownField("_functionPointer");
                ILCodeStream codeStream = emit.NewCodeStream();

                codeStream.EmitLdArg(0);
                codeStream.Emit(ILOpcode.ldfld, emit.NewToken(firstParameterField.InstantiateAsOpen()));
                for (int i = 0; i < method.Signature.Length; i++)
                {
                    codeStream.EmitLdArg(i + 1);
                }
                codeStream.EmitLdArg(0);
                codeStream.Emit(ILOpcode.ldfld, emit.NewToken(functionPointerField.InstantiateAsOpen()));

                MethodSignature signature = method.Signature;
                if (method.OwningType.HasInstantiation)
                {
                    // If the owning type is generic, the signature will contain T's and U's.
                    // We need !0's and !1's.
                    TypeDesc[] typesToReplace = new TypeDesc[method.OwningType.Instantiation.Length];
                    TypeDesc[] replacementTypes = new TypeDesc[typesToReplace.Length];
                    for (int i = 0; i < typesToReplace.Length; i++)
                    {
                        typesToReplace[i] = method.OwningType.Instantiation[i];
                        replacementTypes[i] = context.GetSignatureVariable(i, method: false);
                    }
                    TypeDesc[] parameters = new TypeDesc[method.Signature.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        parameters[i] = method.Signature[i].ReplaceTypesInConstructionOfType(typesToReplace, replacementTypes);
                    }
                    TypeDesc returnType = method.Signature.ReturnType.ReplaceTypesInConstructionOfType(typesToReplace, replacementTypes);
                    signature = new MethodSignature(signature.Flags, signature.GenericParameterCount, returnType, parameters);
                }

                codeStream.Emit(ILOpcode.calli, emit.NewToken(signature));

                codeStream.Emit(ILOpcode.ret);

                return emit.Link(method);
            }

            return null;
        }
    }
}
