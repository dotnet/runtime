// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL
{
    public sealed class NativeAotILProvider : ILProvider
    {
        private MethodIL TryGetRuntimeImplementedMethodIL(MethodDesc method)
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
        private MethodIL TryGetIntrinsicMethodIL(MethodDesc method)
        {
            Debug.Assert(method.IsIntrinsic);

            MetadataType owningType = method.OwningType as MetadataType;
            if (owningType == null)
                return null;

            switch (owningType.Name)
            {
                case "Interlocked":
                    {
                        if (owningType.Namespace == "System.Threading")
                            return InterlockedIntrinsics.EmitIL(method);
                    }
                    break;
                case "Unsafe":
                    {
                        if (owningType.Namespace == "System.Runtime.CompilerServices")
                            return UnsafeIntrinsics.EmitIL(method);
                    }
                    break;
                case "MemoryMarshal":
                    {
                        if (owningType.Namespace == "System.Runtime.InteropServices")
                            return MemoryMarshalIntrinsics.EmitIL(method);
                    }
                    break;
                case "Volatile":
                    {
                        if (owningType.Namespace == "System.Threading")
                            return VolatileIntrinsics.EmitIL(method);
                    }
                    break;
                case "Debug":
                    {
                        if (owningType.Namespace == "System.Diagnostics" && method.Name == "DebugBreak")
                            return new ILStubMethodIL(method, new byte[] { (byte)ILOpcode.break_, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                    }
                    break;
                case "RuntimeAugments":
                    {
                        if (owningType.Namespace == "Internal.Runtime.Augments" && method.Name == "GetCanonType")
                            return GetCanonTypeIntrinsic.EmitIL(method);
                    }
                    break;
                case "MethodTable":
                    {
                        if (owningType.Namespace == "Internal.Runtime" && method.Name == "get_SupportsRelativePointers")
                        {
                            ILOpcode value = method.Context.Target.SupportsRelativePointers ?
                                ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0;
                            return new ILStubMethodIL(method, new byte[] { (byte)value, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), null);
                        }
                    }
                    break;
                case "Stream":
                    {
                        if (owningType.Namespace == "System.IO")
                            return StreamIntrinsics.EmitIL(method);
                    }
                    break;
            }

            return null;
        }

        /// <summary>
        /// Provides method bodies for intrinsics recognized by the compiler that
        /// are specialized per instantiation. It can return null if the intrinsic
        /// is not recognized.
        /// </summary>
        private MethodIL TryGetPerInstantiationIntrinsicMethodIL(MethodDesc method)
        {
            Debug.Assert(method.IsIntrinsic);

            MetadataType owningType = method.OwningType.GetTypeDefinition() as MetadataType;
            if (owningType == null)
                return null;

            string methodName = method.Name;

            switch (owningType.Name)
            {
                case "Activator":
                    {
                        TypeSystemContext context = owningType.Context;
                        if (methodName == "CreateInstance" && method.Signature.Length == 0 && method.HasInstantiation
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
                    break;
                case "RuntimeHelpers":
                    {
                        if (owningType.Namespace == "System.Runtime.CompilerServices")
                            return RuntimeHelpersIntrinsics.EmitIL(method);
                    }
                    break;
                case "Comparer`1":
                    {
                        if (methodName == "Create" && owningType.Namespace == "System.Collections.Generic")
                            return ComparerIntrinsics.EmitComparerCreate(method);
                    }
                    break;
                case "EqualityComparer`1":
                    {
                        if (methodName == "Create" && owningType.Namespace == "System.Collections.Generic")
                            return ComparerIntrinsics.EmitEqualityComparerCreate(method);
                    }
                    break;
                case "ComparerHelpers":
                    {
                        if (owningType.Namespace != "Internal.IntrinsicSupport")
                            return null;

                        if (methodName == "EnumOnlyCompare")
                        {
                            //calls CompareTo for underlyingType to avoid boxing

                            TypeDesc elementType = method.Instantiation[0];
                            if (!elementType.IsEnum)
                                return null;

                            TypeDesc underlyingType = elementType.UnderlyingType;
                            TypeDesc returnType = method.Context.GetWellKnownType(WellKnownType.Int32);
                            MethodDesc underlyingCompareToMethod = underlyingType.GetKnownMethod("CompareTo",
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
                    break;
                case "EqualityComparerHelpers":
                    {
                        if (owningType.Namespace != "Internal.IntrinsicSupport")
                            return null;

                        if (methodName == "EnumOnlyEquals")
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
                        else if (methodName == "GetComparerForReferenceTypesOnly")
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
                        else if (methodName == "StructOnlyEquals")
                        {
                            TypeDesc elementType = method.Instantiation[0];
                            if (!elementType.IsRuntimeDeterminedSubtype
                                && !elementType.IsCanonicalSubtype(CanonicalFormKind.Any)
                                && !elementType.IsGCPointer)
                            {
                                Debug.Assert(elementType.IsValueType);

                                TypeSystemContext context = elementType.Context;
                                MetadataType helperType = context.SystemModule.GetKnownType("Internal.IntrinsicSupport", "EqualityComparerHelpers");

                                MethodDesc methodToCall;
                                if (elementType.IsEnum)
                                {
                                    methodToCall = helperType.GetKnownMethod("EnumOnlyEquals", null).MakeInstantiatedMethod(elementType);
                                }
                                else if (elementType.IsNullable && ComparerIntrinsics.ImplementsIEquatable(elementType.Instantiation[0]))
                                {
                                    methodToCall = helperType.GetKnownMethod("StructOnlyEqualsNullable", null).MakeInstantiatedMethod(elementType.Instantiation[0]);
                                }
                                else if (ComparerIntrinsics.ImplementsIEquatable(elementType))
                                {
                                    methodToCall = helperType.GetKnownMethod("StructOnlyEqualsIEquatable", null).MakeInstantiatedMethod(elementType);
                                }
                                else
                                {
                                    methodToCall = helperType.GetKnownMethod("StructOnlyNormalEquals", null).MakeInstantiatedMethod(elementType);
                                }

                                return new ILStubMethodIL(method, new byte[]
                                {
                                    (byte)ILOpcode.ldarg_0,
                                    (byte)ILOpcode.ldarg_1,
                                    (byte)ILOpcode.call, 1, 0, 0, 0,
                                    (byte)ILOpcode.ret
                                },
                                Array.Empty<LocalVariableDefinition>(), new object[] { methodToCall });
                            }
                        }
                    }
                    break;
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

                MethodIL methodIL = EcmaMethodIL.Create(ecmaMethod);
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

                var methodDefinitionIL = GetMethodIL(method.GetTypicalMethodDefinition());
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
            {
                Debug.Assert(!(method is PInvokeTargetNativeMethod), "Who is asking for IL of PInvokeTargetNativeMethod?");
                return null;
            }
        }
    }
}
