// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.IL;
using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;

//
// Functionality related to instantiating unboxing thunks
//
// To support calling canonical interface methods on generic valuetypes,
// the compiler needs to generate unboxing+instantiating thunks that bridge
// the difference between the two calling conventions.
//
// As a refresher:
// * Instance methods on shared generic valuetypes expect two arguments
//   (aside from the arguments declared in the signature): a ByRef to the
//   first byte of the value of the valuetype (this), and a generic context
//   argument (MethodTable)
// * Interface calls expect 'this' to be a reference type (with the generic
//   context to be inferred from 'this' by the callee).
//
// Instantiating and unboxing stubs bridge this by extracting a managed
// pointer out of a boxed valuetype, along with the MethodTable of the boxed
// valuetype (to provide the generic context) before dispatching to the
// instance method with the different calling convention.
//
// We compile them by:
// * Pretending the unboxing stub is an instance method on a reference type
//   with the same layout as a boxed valuetype (this matches the calling
//   convention expected by the caller).
// * Having the unboxing stub load the m_pEEType field (to get generic
//   context) and a byref to the actual value (to get a 'this' expected by
//   valuetype methods)
// * Generating a call to a fake instance method on the valuetype that has
//   the hidden (generic context) argument explicitly present in the
//   signature. We need a fake method to be able to refer to the hidden parameter
//   from IL.
//
// At a later stage (once codegen is done), we replace the references to the
// fake instance method with the real instance method. Their signatures after
// compilation is identical.
//

namespace ILCompiler
{
    // Contains functionality related to pseudotypes representing boxed instances of value types
    public partial class CompilerTypeSystemContext
    {
        /// <summary>
        /// For a shared (canonical) instance method on a generic valuetype, gets a method that can be used to call the
        /// method given a boxed version of the generic valuetype as 'this' pointer.
        /// </summary>
        public MethodDesc GetSpecialUnboxingThunk(MethodDesc targetMethod, ModuleDesc ownerModuleOfThunk)
        {
            Debug.Assert(targetMethod.IsSharedByGenericInstantiations);
            Debug.Assert(!targetMethod.Signature.IsStatic);
            Debug.Assert(!targetMethod.HasInstantiation);

            TypeDesc owningType = targetMethod.OwningType;
            Debug.Assert(owningType.IsValueType);

            var owningTypeDefinition = (MetadataType)owningType.GetTypeDefinition();

            // Get a reference type that has the same layout as the boxed valuetype.
            var typeKey = new BoxedValuetypeHashtableKey(owningTypeDefinition, ownerModuleOfThunk);
            BoxedValueType boxedTypeDefinition = _boxedValuetypeHashtable.GetOrCreateValue(typeKey);

            // Get a method on the reference type with the same signature as the target method (but different
            // calling convention, since 'this' will be a reference type).
            var targetMethodDefinition = targetMethod.GetTypicalMethodDefinition();
            var methodKey = new UnboxingThunkHashtableKey(targetMethodDefinition, boxedTypeDefinition);
            GenericUnboxingThunk thunkDefinition = _unboxingThunkHashtable.GetOrCreateValue(methodKey);

            // Find the thunk on the instantiated version of the reference type.
            Debug.Assert(owningType != owningTypeDefinition);
            InstantiatedType boxedType = boxedTypeDefinition.MakeInstantiatedType(owningType.Instantiation);

            MethodDesc thunk = GetMethodForInstantiatedType(thunkDefinition, boxedType);
            Debug.Assert(!thunk.HasInstantiation);

            return thunk;
        }

        public MethodDesc GetUnboxingThunk(MethodDesc targetMethod, ModuleDesc ownerModuleOfThunk)
        {
            TypeDesc owningType = targetMethod.OwningType;
            Debug.Assert(owningType.IsValueType);

            var owningTypeDefinition = (MetadataType)owningType.GetTypeDefinition();

            // Get a reference type that has the same layout as the boxed valuetype.
            var typeKey = new BoxedValuetypeHashtableKey(owningTypeDefinition, ownerModuleOfThunk);
            BoxedValueType boxedTypeDefinition = _boxedValuetypeHashtable.GetOrCreateValue(typeKey);

            // Get a method on the reference type with the same signature as the target method (but different
            // calling convention, since 'this' will be a reference type).
            var targetMethodDefinition = targetMethod.GetTypicalMethodDefinition();
            var methodKey = new UnboxingThunkHashtableKey(targetMethodDefinition, boxedTypeDefinition);
            UnboxingThunk thunkDefinition = _nonGenericUnboxingThunkHashtable.GetOrCreateValue(methodKey);

            // Find the thunk on the instantiated version of the reference type.
            MethodDesc thunk;
            if (owningType != owningTypeDefinition)
            {
                InstantiatedType boxedType = boxedTypeDefinition.MakeInstantiatedType(owningType.Instantiation);
                thunk = GetMethodForInstantiatedType(thunkDefinition, boxedType);
            }
            else
            {
                thunk = thunkDefinition;
            }
            if (thunk.HasInstantiation)
                thunk = thunk.MakeInstantiatedMethod(targetMethod.Instantiation);

            return thunk;
        }

        /// <summary>
        /// Returns true of <paramref name="method"/> is a standin method for unboxing thunk target.
        /// </summary>
        public bool IsSpecialUnboxingThunkTargetMethod(MethodDesc method)
        {
            return method.GetTypicalMethodDefinition().GetType() == typeof(ValueTypeInstanceMethodWithHiddenParameter);
        }

        /// <summary>
        /// Returns the real target method of an unboxing stub.
        /// </summary>
        public MethodDesc GetRealSpecialUnboxingThunkTargetMethod(MethodDesc method)
        {
            MethodDesc typicalMethod = method.GetTypicalMethodDefinition();
            MethodDesc methodDefinitionRepresented = ((ValueTypeInstanceMethodWithHiddenParameter)typicalMethod).MethodRepresented;
            return GetMethodForInstantiatedType(methodDefinitionRepresented, (InstantiatedType)method.OwningType);
        }

        private struct BoxedValuetypeHashtableKey
        {
            public readonly MetadataType ValueType;
            public readonly ModuleDesc OwningModule;

            public BoxedValuetypeHashtableKey(MetadataType valueType, ModuleDesc owningModule)
            {
                ValueType = valueType;
                OwningModule = owningModule;
            }
        }

        private sealed class BoxedValuetypeHashtable : LockFreeReaderHashtable<BoxedValuetypeHashtableKey, BoxedValueType>
        {
            protected override int GetKeyHashCode(BoxedValuetypeHashtableKey key)
            {
                return key.ValueType.GetHashCode();
            }
            protected override int GetValueHashCode(BoxedValueType value)
            {
                return value.ValueTypeRepresented.GetHashCode();
            }
            protected override bool CompareKeyToValue(BoxedValuetypeHashtableKey key, BoxedValueType value)
            {
                return ReferenceEquals(key.ValueType, value.ValueTypeRepresented) &&
                    ReferenceEquals(key.OwningModule, value.Module);
            }
            protected override bool CompareValueToValue(BoxedValueType value1, BoxedValueType value2)
            {
                return ReferenceEquals(value1.ValueTypeRepresented, value2.ValueTypeRepresented) &&
                    ReferenceEquals(value1.Module, value2.Module);
            }
            protected override BoxedValueType CreateValueFromKey(BoxedValuetypeHashtableKey key)
            {
                return new BoxedValueType(key.OwningModule, key.ValueType);
            }
        }
        private BoxedValuetypeHashtable _boxedValuetypeHashtable = new BoxedValuetypeHashtable();

        private struct UnboxingThunkHashtableKey
        {
            public readonly MethodDesc TargetMethod;
            public readonly BoxedValueType OwningType;

            public UnboxingThunkHashtableKey(MethodDesc targetMethod, BoxedValueType owningType)
            {
                TargetMethod = targetMethod;
                OwningType = owningType;
            }
        }

        private sealed class UnboxingThunkHashtable : LockFreeReaderHashtable<UnboxingThunkHashtableKey, GenericUnboxingThunk>
        {
            protected override int GetKeyHashCode(UnboxingThunkHashtableKey key)
            {
                return key.TargetMethod.GetHashCode();
            }
            protected override int GetValueHashCode(GenericUnboxingThunk value)
            {
                return value.TargetMethod.GetHashCode();
            }
            protected override bool CompareKeyToValue(UnboxingThunkHashtableKey key, GenericUnboxingThunk value)
            {
                return ReferenceEquals(key.TargetMethod, value.TargetMethod) &&
                    ReferenceEquals(key.OwningType, value.OwningType);
            }
            protected override bool CompareValueToValue(GenericUnboxingThunk value1, GenericUnboxingThunk value2)
            {
                return ReferenceEquals(value1.TargetMethod, value2.TargetMethod) &&
                    ReferenceEquals(value1.OwningType, value2.OwningType);
            }
            protected override GenericUnboxingThunk CreateValueFromKey(UnboxingThunkHashtableKey key)
            {
                return new GenericUnboxingThunk(key.OwningType, key.TargetMethod);
            }
        }
        private UnboxingThunkHashtable _unboxingThunkHashtable = new UnboxingThunkHashtable();

        private sealed class NonGenericUnboxingThunkHashtable : LockFreeReaderHashtable<UnboxingThunkHashtableKey, UnboxingThunk>
        {
            protected override int GetKeyHashCode(UnboxingThunkHashtableKey key)
            {
                return key.TargetMethod.GetHashCode();
            }
            protected override int GetValueHashCode(UnboxingThunk value)
            {
                return value.TargetMethod.GetHashCode();
            }
            protected override bool CompareKeyToValue(UnboxingThunkHashtableKey key, UnboxingThunk value)
            {
                return ReferenceEquals(key.TargetMethod, value.TargetMethod) &&
                    ReferenceEquals(key.OwningType, value.OwningType);
            }
            protected override bool CompareValueToValue(UnboxingThunk value1, UnboxingThunk value2)
            {
                return ReferenceEquals(value1.TargetMethod, value2.TargetMethod) &&
                    ReferenceEquals(value1.OwningType, value2.OwningType);
            }
            protected override UnboxingThunk CreateValueFromKey(UnboxingThunkHashtableKey key)
            {
                return new UnboxingThunk(key.OwningType, key.TargetMethod);
            }
        }

        private NonGenericUnboxingThunkHashtable _nonGenericUnboxingThunkHashtable = new NonGenericUnboxingThunkHashtable();

        /// <summary>
        /// A type with an identical layout to the layout of a boxed value type.
        /// The type has a single field of the type of the valuetype it represents.
        /// </summary>
        private sealed partial class BoxedValueType : MetadataType, INonEmittableType
        {
            public MetadataType ValueTypeRepresented { get; }

            public override ModuleDesc Module { get; }

            public override string Name => "Boxed_" + ValueTypeRepresented.Name;

            public override string Namespace => ValueTypeRepresented.Namespace;
            public override string DiagnosticName => "Boxed_" + ValueTypeRepresented.DiagnosticName;
            public override string DiagnosticNamespace => ValueTypeRepresented.DiagnosticNamespace;
            public override Instantiation Instantiation => ValueTypeRepresented.Instantiation;
            public override PInvokeStringFormat PInvokeStringFormat => PInvokeStringFormat.AutoClass;
            public override bool IsExplicitLayout => false;
            public override bool IsSequentialLayout => true;
            public override bool IsBeforeFieldInit => false;
            public override MetadataType MetadataBaseType => (MetadataType)Context.GetWellKnownType(WellKnownType.Object);
            public override DefType BaseType => MetadataBaseType;
            public override bool IsSealed => true;
            public override bool IsAbstract => false;
            public override DefType ContainingType => null;
            public override DefType[] ExplicitlyImplementedInterfaces => Array.Empty<DefType>();
            public override TypeSystemContext Context => ValueTypeRepresented.Context;

            public override int GetInlineArrayLength()
            {
                Debug.Fail("if this can be an inline array, implement GetInlineArrayLength");
                throw new InvalidOperationException();
            }

            public BoxedValueType(ModuleDesc owningModule, MetadataType valuetype)
            {
                // BoxedValueType has the same genericness as the valuetype it's wrapping.
                // Making BoxedValueType wrap the genericness (and be itself nongeneric) would
                // require a name mangling scheme to allow generating stable and unique names
                // for the wrappers.
                Debug.Assert(valuetype.IsTypeDefinition);

                Debug.Assert(valuetype.IsValueType);

                Module = owningModule;
                ValueTypeRepresented = valuetype;

                // Unboxing thunks for byref-like types don't make sense. Byref-like types cannot be boxed.
                // We still allow these to exist in the system, because it's easier than trying to prevent
                // their creation. We create them as if they existed (in lieu of e.g. pointing all of them
                // to the same __unreachable method body) so that the various places that store pointers to
                // them because they want to be able to extract the target instance method can use the same
                // mechanism they use for everything else at runtime.
            }

            public override ClassLayoutMetadata GetClassLayout() => default(ClassLayoutMetadata);
            public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => false;
            public override IEnumerable<MetadataType> GetNestedTypes() => Array.Empty<MetadataType>();
            public override MetadataType GetNestedType(string name) => null;
            protected override MethodImplRecord[] ComputeVirtualMethodImplsForType() => Array.Empty<MethodImplRecord>();
            public override MethodImplRecord[] FindMethodsImplWithMatchingDeclName(string name) => Array.Empty<MethodImplRecord>();

            public override int GetHashCode()
            {
                string ns = Namespace;
                var hashCodeBuilder = new Internal.NativeFormat.TypeHashingAlgorithms.HashCodeBuilder(ns);
                if (ns.Length > 0)
                    hashCodeBuilder.Append(".");
                hashCodeBuilder.Append(Name);
                return hashCodeBuilder.ToHashCode();
            }

            protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
            {
                TypeFlags flags = 0;

                if ((mask & TypeFlags.HasGenericVarianceComputed) != 0)
                {
                    flags |= TypeFlags.HasGenericVarianceComputed;
                }

                if ((mask & TypeFlags.CategoryMask) != 0)
                {
                    flags |= TypeFlags.Class;
                }

                flags |= TypeFlags.HasFinalizerComputed;
                flags |= TypeFlags.AttributeCacheComputed;

                return flags;
            }

            public override FieldDesc GetField(string name)
            {
                return null;
            }

            public override IEnumerable<FieldDesc> GetFields()
            {
                return Array.Empty<FieldDesc>();
            }
        }

        /// <summary>
        /// Does a method represent an unboxing stub
        /// </summary>
        public bool IsSpecialUnboxingThunk(MethodDesc method)
        {
            if (method.GetTypicalMethodDefinition().GetType() == typeof(GenericUnboxingThunk))
                return true;

            return false;
        }

        /// <summary>
        /// Convert from an unboxing stub to the actual target method
        /// </summary>
        public MethodDesc GetTargetOfSpecialUnboxingThunk(MethodDesc method)
        {
            MethodDesc typicalMethodTarget = ((GenericUnboxingThunk)method.GetTypicalMethodDefinition()).TargetMethod;

            MethodDesc methodOnInstantiatedType = typicalMethodTarget;
            if (method.OwningType.HasInstantiation)
            {
                InstantiatedType instantiatedType = GetInstantiatedType((MetadataType)typicalMethodTarget.OwningType, method.OwningType.Instantiation);
                methodOnInstantiatedType = GetMethodForInstantiatedType(typicalMethodTarget, instantiatedType);
            }

            MethodDesc instantiatedMethod = methodOnInstantiatedType;
            if (method.HasInstantiation)
            {
                instantiatedMethod = GetInstantiatedMethod(methodOnInstantiatedType, method.Instantiation);
            }

            return instantiatedMethod;
        }

        /// <summary>
        /// Represents a thunk to call shared instance method on boxed valuetypes.
        /// </summary>
        private sealed partial class GenericUnboxingThunk : ILStubMethod
        {
            private MethodDesc _targetMethod;
            private ValueTypeInstanceMethodWithHiddenParameter _nakedTargetMethod;
            private BoxedValueType _owningType;

            public GenericUnboxingThunk(BoxedValueType owningType, MethodDesc targetMethod)
            {
                Debug.Assert(targetMethod.OwningType.IsValueType);
                Debug.Assert(!targetMethod.Signature.IsStatic);

                _owningType = owningType;
                _targetMethod = targetMethod;
                _nakedTargetMethod = new ValueTypeInstanceMethodWithHiddenParameter(targetMethod);
            }

            public override TypeSystemContext Context => _targetMethod.Context;

            public override TypeDesc OwningType => _owningType;

            public override MethodSignature Signature => _targetMethod.Signature;

            public MethodDesc TargetMethod => _targetMethod;

            public override string Name
            {
                get
                {
                    return _targetMethod.Name + "_Unbox";
                }
            }

            public override string DiagnosticName
            {
                get
                {
                    return _targetMethod.DiagnosticName + "_Unbox";
                }
            }

            public override MethodIL EmitIL()
            {
                if (_owningType.ValueTypeRepresented.IsByRefLike)
                {
                    // If this is the fake unboxing thunk for ByRef-like types, just make a method that throws.
                    return new ILStubMethodIL(this,
                        new byte[] { (byte)ILOpcode.ldnull, (byte)ILOpcode.throw_ },
                        Array.Empty<LocalVariableDefinition>(),
                        Array.Empty<object>());
                }

                // Generate the unboxing stub. This loosely corresponds to following C#:
                // return BoxedValue.InstanceMethod(this.m_pEEType, [rest of parameters])

                ILEmitter emit = new ILEmitter();
                ILCodeStream codeStream = emit.NewCodeStream();

                bool isX86 = Context.Target.Architecture == TargetArchitecture.X86;

                FieldDesc eeTypeField = Context.GetWellKnownType(WellKnownType.Object).GetKnownField("m_pEEType");

                // Load ByRef to the field with the value of the boxed valuetype
                codeStream.EmitLdArg(0);
                codeStream.Emit(ILOpcode.ldflda, emit.NewToken(Context.SystemModule.GetKnownType("System.Runtime.CompilerServices", "RawData").GetField("Data")));

                if (isX86)
                {
                    for (int i = 0; i < _targetMethod.Signature.Length; i++)
                    {
                        codeStream.EmitLdArg(i + 1);
                    }
                }

                // Load the MethodTable of the boxed valuetype (this is the hidden generic context parameter expected
                // by the (canonical) instance method, but normally not part of the signature in IL).
                codeStream.EmitLdArg(0);
                codeStream.Emit(ILOpcode.ldfld, emit.NewToken(eeTypeField));

                // Load rest of the arguments
                if (!isX86)
                {
                    for (int i = 0; i < _targetMethod.Signature.Length; i++)
                    {
                        codeStream.EmitLdArg(i + 1);
                    }
                }

                // Call an instance method on the target valuetype that has a fake instantiation parameter
                // in it's signature. This will be swapped by the actual instance method after codegen is done.
                codeStream.Emit(ILOpcode.call, emit.NewToken(_nakedTargetMethod.InstantiateAsOpen()));
                codeStream.Emit(ILOpcode.ret);

                return emit.Link(this);
            }
        }

        /// <summary>
        /// Represents a thunk to call instance method on boxed valuetypes.
        /// </summary>
        private sealed partial class UnboxingThunk : ILStubMethod
        {
            private MethodDesc _targetMethod;
            private BoxedValueType _owningType;

            public UnboxingThunk(BoxedValueType owningType, MethodDesc targetMethod)
            {
                Debug.Assert(targetMethod.OwningType.IsValueType);
                Debug.Assert(!targetMethod.Signature.IsStatic);

                _owningType = owningType;
                _targetMethod = targetMethod;
            }

            public override TypeSystemContext Context => _targetMethod.Context;

            public override TypeDesc OwningType => _owningType;

            public override MethodSignature Signature => _targetMethod.Signature;

            public MethodDesc TargetMethod => _targetMethod;

            public override string Name
            {
                get
                {
                    return _targetMethod.Name + "_Unbox";
                }
            }

            public override string DiagnosticName
            {
                get
                {
                    return _targetMethod.DiagnosticName + "_Unbox";
                }
            }

            public override MethodIL EmitIL()
            {
                if (_owningType.ValueTypeRepresented.IsByRefLike)
                {
                    // If this is the fake unboxing thunk for ByRef-like types, just make a method that throws.
                    return new ILStubMethodIL(this,
                        new byte[] { (byte)ILOpcode.ldnull, (byte)ILOpcode.throw_ },
                        Array.Empty<LocalVariableDefinition>(),
                        Array.Empty<object>());
                }

                // Generate the unboxing stub. This loosely corresponds to following C#:
                // return BoxedValue.InstanceMethod([rest of parameters])

                ILEmitter emit = new ILEmitter();
                ILCodeStream codeStream = emit.NewCodeStream();

                // unbox to get a pointer to the value type
                codeStream.EmitLdArg(0);
                codeStream.Emit(ILOpcode.ldflda, emit.NewToken(Context.SystemModule.GetKnownType("System.Runtime.CompilerServices", "RawData").GetField("Data")));

                // Load rest of the arguments
                for (int i = 0; i < _targetMethod.Signature.Length; i++)
                {
                    codeStream.EmitLdArg(i + 1);
                }

                TypeDesc owner = _targetMethod.OwningType;
                MethodDesc methodToInstantiate = _targetMethod;
                if (owner.HasInstantiation)
                {
                    MetadataType instantiatedOwner = (MetadataType)owner.InstantiateAsOpen();
                    methodToInstantiate = _targetMethod.Context.GetMethodForInstantiatedType(_targetMethod, (InstantiatedType)instantiatedOwner);
                }
                if (methodToInstantiate.HasInstantiation)
                {
                    TypeSystemContext context = methodToInstantiate.Context;

                    var inst = new TypeDesc[methodToInstantiate.Instantiation.Length];
                    for (int i = 0; i < inst.Length; i++)
                    {
                        inst[i] = context.GetSignatureVariable(i, true);
                    }

                    methodToInstantiate = context.GetInstantiatedMethod(methodToInstantiate, new Instantiation(inst));
                }

                codeStream.Emit(ILOpcode.call, emit.NewToken(methodToInstantiate));
                codeStream.Emit(ILOpcode.ret);

                return emit.Link(this);
            }

            public override Instantiation Instantiation => _targetMethod.Instantiation;
        }

        /// <summary>
        /// Represents an instance method on a generic valuetype with an explicit instantiation parameter in the
        /// signature. This is so that we can refer to the parameter from IL. References to this method will
        /// be replaced by the actual instance method after codegen is done.
        /// </summary>
        internal sealed partial class ValueTypeInstanceMethodWithHiddenParameter : MethodDesc
        {
            private MethodDesc _methodRepresented;
            private MethodSignature _signature;

            public ValueTypeInstanceMethodWithHiddenParameter(MethodDesc methodRepresented)
            {
                Debug.Assert(methodRepresented.OwningType.IsValueType);
                Debug.Assert(!methodRepresented.Signature.IsStatic);

                _methodRepresented = methodRepresented;
            }

            public MethodDesc MethodRepresented => _methodRepresented;

            // We really don't want this method to be inlined.
            public override bool IsNoInlining => true;

            public override bool IsInternalCall => true;

            public override bool IsIntrinsic => true;

            public override TypeSystemContext Context => _methodRepresented.Context;
            public override TypeDesc OwningType => _methodRepresented.OwningType;

            public override string Name => _methodRepresented.Name;
            public override string DiagnosticName => _methodRepresented.DiagnosticName;

            public override MethodSignature Signature
            {
                get
                {
                    if (_signature == null)
                    {
                        TypeDesc[] parameters = new TypeDesc[_methodRepresented.Signature.Length + 1];

                        // Shared instance methods on generic valuetypes have a hidden parameter with the generic context.
                        // We add it to the signature so that we can refer to it from IL.
                        if (Context.Target.Architecture == TargetArchitecture.X86)
                        {
                            for (int i = 0; i < _methodRepresented.Signature.Length; i++)
                                parameters[i] = _methodRepresented.Signature[i];
                            parameters[_methodRepresented.Signature.Length] = Context.GetWellKnownType(WellKnownType.Void).MakePointerType();
                        }
                        else
                        {
                            parameters[0] = Context.GetWellKnownType(WellKnownType.Void).MakePointerType();
                            for (int i = 0; i < _methodRepresented.Signature.Length; i++)
                                parameters[i + 1] = _methodRepresented.Signature[i];
                        }

                        _signature = new MethodSignature(_methodRepresented.Signature.Flags,
                            _methodRepresented.Signature.GenericParameterCount,
                            _methodRepresented.Signature.ReturnType,
                            parameters);
                    }

                    return _signature;
                }
            }

            public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => false;
        }
    }
}
