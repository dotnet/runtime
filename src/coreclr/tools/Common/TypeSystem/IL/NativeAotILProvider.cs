// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL
{
    public sealed class NativeAotILProvider : ILProvider
    {
        private static MethodIL TryGetRuntimeImplementedMethodIL(MethodDesc method)
        {
            // Provides method bodies for runtime implemented methods. It can return null for
            // methods that are treated specially by the codegen.

            Debug.Assert(method.IsRuntimeImplemented);

            TypeDesc owningType = method.OwningType;

            if (owningType.IsDelegate)
            {
                return DelegateMethodILEmitter.EmitIL(method);
            }

            return null;
        }

        /// <summary>
        /// Provides method bodies for intrinsics recognized by the compiler.
        /// It can return null if it's not an intrinsic recognized by the compiler,
        /// but an intrinsic e.g. recognized by codegen.
        /// </summary>
        private static MethodIL TryGetIntrinsicMethodIL(MethodDesc method)
        {
            Debug.Assert(method.IsIntrinsic);

            MetadataType owningType = method.OwningType as MetadataType;
            if (owningType == null)
                return null;

            if (owningType.Name.SequenceEqual("Unsafe"u8))
            {
                if (owningType.Namespace.SequenceEqual("System.Runtime.CompilerServices"u8))
                    return UnsafeIntrinsics.EmitIL(method);
            }
            else if (owningType.Name.SequenceEqual("Debug"u8))
            {
                if (owningType.Namespace.SequenceEqual("System.Diagnostics"u8) && method.Name.SequenceEqual("DebugBreak"u8))
                    return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.break_, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
            }
            else if (owningType.Name.SequenceEqual("RuntimeAugments"u8))
            {
                if (owningType.Namespace.SequenceEqual("Internal.Runtime.Augments"u8) && method.Name.SequenceEqual("GetCanonType"u8))
                    return GetCanonTypeIntrinsic.EmitIL(method);
            }
            else if (owningType.Name.SequenceEqual("MethodTable"u8))
            {
                if (owningType.Namespace.SequenceEqual("Internal.Runtime"u8) && method.Name.SequenceEqual("get_SupportsRelativePointers"u8))
                {
                    ILOpcode value = method.Context.Target.SupportsRelativePointers ?
                        ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0;
                    return new ILStubMethodIL(method, new byte[] { (byte)value, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                }
            }
            else if (owningType.Name.SequenceEqual("Stream"u8))
            {
                if (owningType.Namespace.SequenceEqual("System.IO"u8))
                    return StreamIntrinsics.EmitIL(method);
            }

            return null;
        }

        /// <summary>
        /// Provides method bodies for intrinsics recognized by the compiler that
        /// are specialized per instantiation. It can return null if the intrinsic
        /// is not recognized.
        /// </summary>
        private static MethodIL TryGetPerInstantiationIntrinsicMethodIL(MethodDesc method)
        {
            Debug.Assert(method.IsIntrinsic);

            MetadataType owningType = method.OwningType.GetTypeDefinition() as MetadataType;
            if (owningType == null)
                return null;

            if (owningType.Name.SequenceEqual("Interlocked"u8))
            {
                if (owningType.Namespace.SequenceEqual("System.Threading"u8))
                    return InterlockedIntrinsics.EmitIL(method);
            }
            else if (owningType.Name.SequenceEqual("Activator"u8))
            {
                TypeSystemContext context = owningType.Context;
                if (method.Name.SequenceEqual("CreateInstance"u8) && method.Signature.Length == 0 && method.HasInstantiation
                    && method.Instantiation[0] is TypeDesc activatedType
                    && activatedType != context.UniversalCanonType
                    && activatedType.IsValueType
                    && activatedType.GetParameterlessConstructor() == null)
                {
                    ILEmitter emit = new ILEmitter();
                    ILCodeStream codeStream = emit.NewCodeStream();

                    var t = emit.NewLocal(context.GetSignatureVariable(0, method: true));
                    codeStream.EmitLdLoca(t);
                    codeStream.Emit(ILOpcode.initobj, emit.NewToken(context.GetSignatureVariable(0, method: true)));
                    codeStream.EmitLdLoc(t);
                    codeStream.Emit(ILOpcode.ret);

                    return new InstantiatedMethodIL(method, emit.Link(method.GetMethodDefinition()));
                }
            }
            else if (owningType.Name.SequenceEqual("RuntimeHelpers"u8))
            {
                if (owningType.Namespace.SequenceEqual("System.Runtime.CompilerServices"u8))
                    return RuntimeHelpersIntrinsics.EmitIL(method);
            }
            else if (owningType.Name.SequenceEqual("Comparer`1"u8))
            {
                if (method.Name.SequenceEqual("Create"u8) && owningType.Namespace.SequenceEqual("System.Collections.Generic"u8))
                    return ComparerIntrinsics.EmitComparerCreate(method);
            }
            else if (owningType.Name.SequenceEqual("EqualityComparer`1"u8))
            {
                if (method.Name.SequenceEqual("Create"u8) && owningType.Namespace.SequenceEqual("System.Collections.Generic"u8))
                    return ComparerIntrinsics.EmitEqualityComparerCreate(method);
            }
            else if (owningType.Name.SequenceEqual("ComparerHelpers"u8))
            {
                if (!owningType.Namespace.SequenceEqual("Internal.IntrinsicSupport"u8))
                    return null;

                if (method.Name.SequenceEqual("EnumOnlyCompare"u8))
                {
                    //calls CompareTo for underlyingType to avoid boxing

                    TypeDesc elementType = method.Instantiation[0];
                    if (!elementType.IsEnum)
                        return null;

                    TypeDesc underlyingType = elementType.UnderlyingType;
                    TypeDesc returnType = method.Context.GetWellKnownType(WellKnownType.Int32);
                    MethodDesc underlyingCompareToMethod = underlyingType.GetKnownMethod("CompareTo"u8,
                        new MethodSignature(
                            MethodSignatureFlags.None,
                            genericParameterCount: 0,
                            returnType: returnType,
                            parameters: new TypeDesc[] {underlyingType}));

                    ILEmitter emitter = new ILEmitter();
                    var codeStream = emitter.NewCodeStream();

                    codeStream.EmitLdArga(0);
                    codeStream.EmitLdArg(1);
                    codeStream.Emit(ILOpcode.call, emitter.NewToken(underlyingCompareToMethod));
                    codeStream.Emit(ILOpcode.ret);

                    return emitter.Link(method);
                }
            }
            else if (owningType.Name.SequenceEqual("EqualityComparerHelpers"u8))
            {
                if (!owningType.Namespace.SequenceEqual("Internal.IntrinsicSupport"u8))
                    return null;

                if (method.Name.SequenceEqual("EnumOnlyEquals"u8))
                {
                    // EnumOnlyEquals would basically like to do this:
                    // static bool EnumOnlyEquals<T>(T x, T y) where T: struct => x == y;
                    // This is not legal though.
                    // We don't want to do this:
                    // static bool EnumOnlyEquals<T>(T x, T y) where T: struct => x.Equals(y);
                    // Because it would box y.
                    // So we resort to some per-instantiation magic.

                    TypeDesc elementType = method.Instantiation[0];
                    if (!elementType.IsEnum)
                        return null;

                    ILOpcode convInstruction;
                    if (((DefType)elementType).InstanceFieldSize.AsInt <= 4)
                    {
                        convInstruction = ILOpcode.conv_i4;
                    }
                    else
                    {
                        Debug.Assert(((DefType)elementType).InstanceFieldSize.AsInt == 8);
                        convInstruction = ILOpcode.conv_i8;
                    }

                    return new ILStubMethodIL(method, new byte[] {
                        (byte)ILOpcode.ldarg_0,
                        (byte)convInstruction,
                        (byte)ILOpcode.ldarg_1,
                        (byte)convInstruction,
                        (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.ceq),
                        (byte)ILOpcode.ret,
                    },
                    Array.Empty<LocalVariableDefinition>(), null);
                }
                else if (method.Name.SequenceEqual("GetComparerForReferenceTypesOnly"u8))
                {
                    TypeDesc elementType = method.Instantiation[0];
                    if (!elementType.IsRuntimeDeterminedSubtype
                        && !elementType.IsCanonicalSubtype(CanonicalFormKind.Any)
                        && !elementType.IsGCPointer)
                    {
                        return new ILStubMethodIL(method, new byte[] {
                            (byte)ILOpcode.ldnull,
                            (byte)ILOpcode.ret
                        },
                        Array.Empty<LocalVariableDefinition>(), null);
                    }
                }
                else if (method.Name.SequenceEqual("StructOnlyEquals"u8))
                {
                    TypeDesc elementType = method.Instantiation[0];
                    if (!elementType.IsRuntimeDeterminedSubtype
                        && !elementType.IsCanonicalSubtype(CanonicalFormKind.Any)
                        && !elementType.IsGCPointer)
                    {
                        Debug.Assert(elementType.IsValueType);

                        MethodDesc methodToCall = GetMethodToCall(elementType);
                        if (methodToCall == null)
                            return null;

                        return new ILStubMethodIL(method, new byte[]
                        {
                            (byte)ILOpcode.ldarg_0,
                            (byte)ILOpcode.ldarg_1,
                            (byte)ILOpcode.call, 1, 0, 0, 0,
                            (byte)ILOpcode.ret
                        },
                        Array.Empty<LocalVariableDefinition>(), new object[] { methodToCall });

                        static MethodDesc GetMethodToCall(TypeDesc elementType)
                        {
                            TypeSystemContext context = elementType.Context;
                            MetadataType helperType = context.SystemModule.GetKnownType("Internal.IntrinsicSupport"u8, "EqualityComparerHelpers"u8);

                            if (elementType.IsEnum)
                                return helperType.GetKnownMethod("EnumOnlyEquals"u8, null)
                                    .MakeInstantiatedMethod(elementType);

                            if (elementType.IsNullable)
                            {
                                bool? nullableOfEquatable = ComparerIntrinsics.ImplementsIEquatable(elementType.Instantiation[0]);
                                if (nullableOfEquatable.HasValue && nullableOfEquatable.Value)
                                    return helperType.GetKnownMethod("StructOnlyEqualsNullable"u8, null)
                                        .MakeInstantiatedMethod(elementType.Instantiation[0]);
                                return null; // Fallback to default implementation based on EqualityComparer
                            }

                            bool? equatable = ComparerIntrinsics.ImplementsIEquatable(elementType);
                            if (!equatable.HasValue)
                                return null;
                            return helperType.GetKnownMethod(equatable.Value ? "StructOnlyEqualsIEquatable"u8 : "StructOnlyNormalEquals"u8, null)
                                .MakeInstantiatedMethod(elementType);
                        }
                    }
                }
            }

            return null;
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            if (method is EcmaMethod ecmaMethod)
            {
                if (ecmaMethod.IsIntrinsic)
                {
                    MethodIL result = TryGetIntrinsicMethodIL(ecmaMethod);
                    if (result != null)
                        return result;
                }

                if (ecmaMethod.IsRuntimeImplemented)
                {
                    MethodIL result = TryGetRuntimeImplementedMethodIL(ecmaMethod);
                    if (result != null)
                        return result;
                }

                if (ecmaMethod.IsAsync)
                {
                    if (ecmaMethod.Signature.ReturnsTaskOrValueTask())
                    {
                        return AsyncThunkILEmitter.EmitTaskReturningThunk(ecmaMethod, ((CompilerTypeSystemContext)ecmaMethod.Context).GetAsyncVariantMethod(ecmaMethod));
                    }
                    else
                    {
                        // We only allow non-Task returning runtime async methods in CoreLib
                        if (ecmaMethod.OwningType.Module != ecmaMethod.Context.SystemModule)
                            ThrowHelper.ThrowBadImageFormatException();
                    }
                }

                MethodIL methodIL = EcmaMethodIL.Create(ecmaMethod);
                if (methodIL != null)
                    return methodIL;

                methodIL = UnsafeAccessors.TryGetIL(ecmaMethod);
                if (methodIL != null)
                    return methodIL;

                return null;
            }
            else
            if (method is MethodForInstantiatedType || method is InstantiatedMethod)
            {
                // Intrinsics specialized per instantiation
                if (method.IsIntrinsic)
                {
                    MethodIL methodIL = TryGetPerInstantiationIntrinsicMethodIL(method);
                    if (methodIL != null)
                        return methodIL;
                }

                MethodDesc typicalMethod = method.GetTypicalMethodDefinition();
                if (typicalMethod is SpecializableILStubMethod specializableMethod)
                {
                    MethodIL methodIL = specializableMethod.EmitIL(method);
                    if (methodIL != null)
                        return methodIL;
                }

                var methodDefinitionIL = GetMethodIL(typicalMethod);
                if (methodDefinitionIL == null)
                    return null;
                return new InstantiatedMethodIL(method, methodDefinitionIL);
            }
            else
            if (method is ILStubMethod)
            {
                return ((ILStubMethod)method).EmitIL();
            }
            else
            if (method is ArrayMethod)
            {
                return ArrayMethodILEmitter.EmitIL((ArrayMethod)method);
            }
            else
            if (method is AsyncMethodVariant asyncVariantImpl)
            {
                if (asyncVariantImpl.IsAsync)
                {
                    return new AsyncEcmaMethodIL(asyncVariantImpl, EcmaMethodIL.Create(asyncVariantImpl.Target));
                }
                else
                {
                    return AsyncThunkILEmitter.EmitAsyncMethodThunk(asyncVariantImpl, asyncVariantImpl.Target);
                }
            }
            else
            {
                Debug.Assert(!(method is PInvokeTargetNativeMethod), "Who is asking for IL of PInvokeTargetNativeMethod?");
                return null;
            }
        }

        private sealed class AsyncEcmaMethodIL : MethodIL
        {
            private readonly AsyncMethodVariant _variant;
            private readonly EcmaMethodIL _ecmaIL;

            public AsyncEcmaMethodIL(AsyncMethodVariant variant, EcmaMethodIL ecmaIL)
                => (_variant, _ecmaIL) = (variant, ecmaIL);

            // This is the reason we need this class - the method that owns the IL is the variant.
            public override MethodDesc OwningMethod => _variant;

            // Everything else dispatches to EcmaMethodIL
            public override MethodDebugInformation GetDebugInfo() => _ecmaIL.GetDebugInfo();
            public override ILExceptionRegion[] GetExceptionRegions() => _ecmaIL.GetExceptionRegions();
            public override byte[] GetILBytes() => _ecmaIL.GetILBytes();
            public override LocalVariableDefinition[] GetLocals() => _ecmaIL.GetLocals();
            public override object GetObject(int token, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw) => _ecmaIL.GetObject(token, notFoundBehavior);
            public override bool IsInitLocals => _ecmaIL.IsInitLocals;
            public override int MaxStack => _ecmaIL.MaxStack;
        }
    }
}
