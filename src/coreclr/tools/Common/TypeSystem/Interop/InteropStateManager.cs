// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.IL.Stubs;
using Internal.TypeSystem.Interop;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// This class manages and caches  interop state information
    /// </summary>
    public sealed class InteropStateManager
    {
        private readonly ModuleDesc _generatedAssembly;
        private readonly NativeStructTypeHashtable _nativeStructHashtable;
        private readonly StructMarshallingThunkHashTable _structMarshallingThunkHashtable;
        private readonly DelegateMarshallingStubHashtable _delegateMarshallingThunkHashtable;
        private readonly ForwardDelegateCreationStubHashtable _forwardDelegateCreationStubHashtable;
        private readonly PInvokeDelegateWrapperHashtable _pInvokeDelegateWrapperHashtable;
        private readonly InlineArrayHashTable _inlineArrayHashtable;
        private readonly PInvokeLazyFixupFieldHashtable _pInvokeLazyFixupFieldHashtable;
        private readonly PInvokeCalliHashtable _pInvokeCalliHashtable;

        public InteropStateManager(ModuleDesc generatedAssembly)
        {
            _generatedAssembly = generatedAssembly;
            _structMarshallingThunkHashtable = new StructMarshallingThunkHashTable(this, _generatedAssembly.GetGlobalModuleType());
            _nativeStructHashtable = new NativeStructTypeHashtable(this, _generatedAssembly);
            _delegateMarshallingThunkHashtable = new DelegateMarshallingStubHashtable(this, _generatedAssembly.GetGlobalModuleType());
            _forwardDelegateCreationStubHashtable = new ForwardDelegateCreationStubHashtable(this, _generatedAssembly.GetGlobalModuleType());
            _pInvokeDelegateWrapperHashtable = new PInvokeDelegateWrapperHashtable(this, _generatedAssembly);
            _inlineArrayHashtable = new InlineArrayHashTable(this, _generatedAssembly);
            _pInvokeLazyFixupFieldHashtable = new PInvokeLazyFixupFieldHashtable(_generatedAssembly.GetGlobalModuleType());
            _pInvokeCalliHashtable = new PInvokeCalliHashtable(this, _generatedAssembly.GetGlobalModuleType());
        }
        //
        // Delegate Marshalling Stubs
        //

        /// <summary>
        /// Generates marshalling stubs for open static delegates
        /// </summary>
        public DelegateMarshallingMethodThunk GetOpenStaticDelegateMarshallingThunk(TypeDesc delegateType)
        {
            if (delegateType is ByRefType)
            {
                delegateType = delegateType.GetParameterType();
            }

            Debug.Assert(delegateType is MetadataType);


            // Get the stub for marshalling open static delegate
            var stubKey = new DelegateMarshallingStubHashtableKey((MetadataType)delegateType, DelegateMarshallingMethodThunkKind.ReverseOpenStatic);
            return _delegateMarshallingThunkHashtable.GetOrCreateValue(stubKey);
        }

        /// <summary>
        /// Generates marshalling stubs for closed instance delegates
        /// </summary>
        public DelegateMarshallingMethodThunk GetClosedDelegateMarshallingThunk(TypeDesc delegateType)
        {
            if (delegateType is ByRefType)
            {
                delegateType = delegateType.GetParameterType();
            }

            Debug.Assert(delegateType is MetadataType);


            // Get the stub for marshalling open static delegate
            var stubKey = new DelegateMarshallingStubHashtableKey((MetadataType)delegateType, DelegateMarshallingMethodThunkKind.ReverseClosed);
            return _delegateMarshallingThunkHashtable.GetOrCreateValue(stubKey);
        }

        /// <summary>
        /// Generates thunk for creating delegate
        /// </summary>
        public ForwardDelegateCreationThunk GetForwardDelegateCreationThunk(TypeDesc delegateType)
        {
            if (delegateType is ByRefType)
            {
                delegateType = delegateType.GetParameterType();
            }

            Debug.Assert(delegateType is MetadataType);

            // Get the stub for creating delegate
            return _forwardDelegateCreationStubHashtable.GetOrCreateValue((MetadataType)delegateType);
        }

        public PInvokeDelegateWrapper GetPInvokeDelegateWrapper(TypeDesc delegateType)
        {
            if (delegateType is ByRefType)
            {
                delegateType = delegateType.GetParameterType();
            }

            Debug.Assert(delegateType is MetadataType);

            // Get the Type that wraps the native function
            return _pInvokeDelegateWrapperHashtable.GetOrCreateValue((MetadataType)delegateType);
        }

        //
        //  Struct Marshalling
        //  To support struct marshalling compiler needs to generate a native type which
        //  imitates the original struct being passed to managed side with corresponding
        //  fields of marshalled types. Additionally it needs to generate three thunks
        //      1. Managed to Native Thunk: For forward marshalling
        //      2. Native to Managed Thunk: For reverse marshalling
        //      3. Cleanup Thunk: for cleaning up any allocated resources
        //
        /// <summary>
        /// Generates a Native struct type which imitates the managed struct
        /// </summary>
        public NativeStructType GetStructMarshallingNativeType(TypeDesc managedType)
        {
            if (managedType is ByRefType)
            {
                managedType = managedType.GetParameterType();
            }

            Debug.Assert(managedType is MetadataType);

            return _nativeStructHashtable.GetOrCreateValue((MetadataType)managedType);
        }

        /// <summary>
        ///  Generates a thunk to marshal the fields of the struct from managed to native
        /// </summary>
        public MethodDesc GetStructMarshallingManagedToNativeThunk(TypeDesc managedType)
        {
            if (managedType is ByRefType)
            {
                managedType = managedType.GetParameterType();
            }

            Debug.Assert(managedType is MetadataType);

            var methodKey = new StructMarshallingThunkKey((MetadataType)managedType, StructMarshallingThunkType.ManagedToNative);
            return _structMarshallingThunkHashtable.GetOrCreateValue(methodKey);
        }

        /// <summary>
        ///  Generates a thunk to marshal the fields of the struct from native to managed
        /// </summary>
        public MethodDesc GetStructMarshallingNativeToManagedThunk(TypeDesc managedType)
        {
            if (managedType is ByRefType)
            {
                managedType = managedType.GetParameterType();
            }

            Debug.Assert(managedType is MetadataType);

            var methodKey = new StructMarshallingThunkKey((MetadataType)managedType, StructMarshallingThunkType.NativeToManaged);
            return _structMarshallingThunkHashtable.GetOrCreateValue(methodKey);
        }

        /// <summary>
        ///  Generates a thunk to cleanup any allocated resources during marshalling
        /// </summary>
        public MethodDesc GetStructMarshallingCleanupThunk(TypeDesc managedType)
        {
            if (managedType is ByRefType)
            {
                managedType = ((ByRefType)managedType).GetParameterType();
            }

            Debug.Assert(managedType is MetadataType);

            var methodKey = new StructMarshallingThunkKey((MetadataType)managedType, StructMarshallingThunkType.Cleanup);
            return _structMarshallingThunkHashtable.GetOrCreateValue(methodKey);
        }

        public TypeDesc GetInlineArrayType(InlineArrayCandidate candidate)
        {
            return _inlineArrayHashtable.GetOrCreateValue(candidate);
        }

        public FieldDesc GetPInvokeLazyFixupField(MethodDesc method)
        {
            return _pInvokeLazyFixupFieldHashtable.GetOrCreateValue(method);
        }

        public MethodDesc GetPInvokeCalliStub(MethodSignature signature, ModuleDesc moduleContext)
        {
            // Normalize calling convention details on the signature
            var normalizedSignatureBuilder = new MethodSignatureBuilder(signature);
            normalizedSignatureBuilder.Flags = (signature.Flags & MethodSignatureFlags.Static) | MethodSignatureFlags.UnmanagedCallingConvention;
            normalizedSignatureBuilder.SetEmbeddedSignatureData(signature.GetStandaloneMethodSignatureCallingConventions().EncodeAsEmbeddedSignatureData(moduleContext.Context));
            return _pInvokeCalliHashtable.GetOrCreateValue(new CalliMarshallingMethodThunkKey(normalizedSignatureBuilder.ToSignature(), MarshalHelpers.IsRuntimeMarshallingEnabled(moduleContext)));
        }

        private class NativeStructTypeHashtable : LockFreeReaderHashtable<MetadataType, NativeStructType>
        {
            protected override int GetKeyHashCode(MetadataType key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(NativeStructType value)
            {
                return value.ManagedStructType.GetHashCode();
            }

            protected override bool CompareKeyToValue(MetadataType key, NativeStructType value)
            {
                return Object.ReferenceEquals(key, value.ManagedStructType);
            }

            protected override bool CompareValueToValue(NativeStructType value1, NativeStructType value2)
            {
                return Object.ReferenceEquals(value1.ManagedStructType, value2.ManagedStructType);
            }

            protected override NativeStructType CreateValueFromKey(MetadataType key)
            {
                return new NativeStructType(_owningModule, key, _interopStateManager);
            }

            private readonly InteropStateManager _interopStateManager;
            private readonly ModuleDesc _owningModule;

            public NativeStructTypeHashtable(InteropStateManager interopStateManager, ModuleDesc owningModule)
            {
                _interopStateManager = interopStateManager;
                _owningModule = owningModule;
            }
        }

        private struct StructMarshallingThunkKey
        {
            public readonly MetadataType ManagedType;
            public readonly StructMarshallingThunkType ThunkType;

            public StructMarshallingThunkKey(MetadataType type, StructMarshallingThunkType thunkType)
            {
                ManagedType = type;
                ThunkType = thunkType;
            }
        }

        private class StructMarshallingThunkHashTable : LockFreeReaderHashtable<StructMarshallingThunkKey, StructMarshallingThunk>
        {
            protected override int GetKeyHashCode(StructMarshallingThunkKey key)
            {
                return key.ManagedType.GetHashCode() ^ (int)key.ThunkType;
            }

            protected override int GetValueHashCode(StructMarshallingThunk value)
            {
                return value.ManagedType.GetHashCode() ^ (int)value.ThunkType;
            }

            protected override bool CompareKeyToValue(StructMarshallingThunkKey key, StructMarshallingThunk value)
            {
                return Object.ReferenceEquals(key.ManagedType, value.ManagedType) &&
                        key.ThunkType == value.ThunkType;
            }

            protected override bool CompareValueToValue(StructMarshallingThunk value1, StructMarshallingThunk value2)
            {
                return Object.ReferenceEquals(value1.ManagedType, value2.ManagedType) &&
                        value1.ThunkType == value2.ThunkType;
            }

            protected override StructMarshallingThunk CreateValueFromKey(StructMarshallingThunkKey key)
            {
                return new StructMarshallingThunk(_owningType, key.ManagedType, key.ThunkType, _interopStateManager);
            }

            private readonly InteropStateManager _interopStateManager;
            private readonly TypeDesc _owningType;

            public StructMarshallingThunkHashTable(InteropStateManager interopStateManager, TypeDesc owningType)
            {
                _interopStateManager = interopStateManager;
                _owningType = owningType;
            }
        }

        private class InlineArrayHashTable : LockFreeReaderHashtable<InlineArrayCandidate, InlineArrayType>
        {
            protected override int GetKeyHashCode(InlineArrayCandidate key)
            {
                return key.ElementType.GetHashCode() ^ (int)key.Length;
            }

            protected override int GetValueHashCode(InlineArrayType value)
            {
                return value.ElementType.GetHashCode() ^ (int)value.Length;
            }

            protected override bool CompareKeyToValue(InlineArrayCandidate key, InlineArrayType value)
            {
                return Object.ReferenceEquals(key.ElementType, value.ElementType) &&
                        key.Length == value.Length;
            }

            protected override bool CompareValueToValue(InlineArrayType value1, InlineArrayType value2)
            {
                return Object.ReferenceEquals(value1.ElementType, value2.ElementType) &&
                        value1.Length == value2.Length;
            }

            protected override InlineArrayType CreateValueFromKey(InlineArrayCandidate key)
            {
                return new InlineArrayType(_owningModule, key.ElementType, key.Length, _interopStateManager);
            }

            private readonly InteropStateManager _interopStateManager;
            private readonly ModuleDesc _owningModule;

            public InlineArrayHashTable(InteropStateManager interopStateManager, ModuleDesc owningModule)
            {
                _interopStateManager = interopStateManager;
                _owningModule = owningModule;
            }
        }

        private struct DelegateMarshallingStubHashtableKey
        {
            public readonly MetadataType DelegateType;
            public readonly DelegateMarshallingMethodThunkKind Kind;

            public DelegateMarshallingStubHashtableKey(MetadataType type, DelegateMarshallingMethodThunkKind kind)
            {
                DelegateType = type;
                Kind = kind;
            }
        }
        private class DelegateMarshallingStubHashtable : LockFreeReaderHashtable<DelegateMarshallingStubHashtableKey, DelegateMarshallingMethodThunk>
        {
            protected override int GetKeyHashCode(DelegateMarshallingStubHashtableKey key)
            {
                return key.DelegateType.GetHashCode() ^ (int)key.Kind;
            }

            protected override int GetValueHashCode(DelegateMarshallingMethodThunk value)
            {
                return value.DelegateType.GetHashCode() ^ (int)value.Kind;
            }

            protected override bool CompareKeyToValue(DelegateMarshallingStubHashtableKey key, DelegateMarshallingMethodThunk value)
            {
                return Object.ReferenceEquals(key.DelegateType, value.DelegateType) &&
                    key.Kind== value.Kind;
            }

            protected override bool CompareValueToValue(DelegateMarshallingMethodThunk value1, DelegateMarshallingMethodThunk value2)
            {
                return Object.ReferenceEquals(value1.DelegateType, value2.DelegateType) &&
                    value1.Kind== value2.Kind;
            }

            protected override DelegateMarshallingMethodThunk CreateValueFromKey(DelegateMarshallingStubHashtableKey key)
            {
                return new DelegateMarshallingMethodThunk(key.DelegateType, _owningType,
                    _interopStateManager, key.Kind);
            }

            private TypeDesc _owningType;
            private InteropStateManager _interopStateManager;

            public DelegateMarshallingStubHashtable(InteropStateManager interopStateManager, TypeDesc owningType)
            {
                _interopStateManager = interopStateManager;
                _owningType = owningType;
            }
        }

        private class ForwardDelegateCreationStubHashtable : LockFreeReaderHashtable<MetadataType, ForwardDelegateCreationThunk>
        {
            protected override int GetKeyHashCode(MetadataType key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(ForwardDelegateCreationThunk value)
            {
                return value.DelegateType.GetHashCode();
            }

            protected override bool CompareKeyToValue(MetadataType key, ForwardDelegateCreationThunk value)
            {
                return Object.ReferenceEquals(key, value.DelegateType);
            }

            protected override bool CompareValueToValue(ForwardDelegateCreationThunk value1, ForwardDelegateCreationThunk value2)
            {
                return Object.ReferenceEquals(value1.DelegateType, value2.DelegateType);
            }

            protected override ForwardDelegateCreationThunk CreateValueFromKey(MetadataType key)
            {
                return new ForwardDelegateCreationThunk(key, _owningType, _interopStateManager);
            }

            private TypeDesc _owningType;
            private InteropStateManager _interopStateManager;

            public ForwardDelegateCreationStubHashtable(InteropStateManager interopStateManager, TypeDesc owningType)
            {
                _interopStateManager = interopStateManager;
                _owningType = owningType;
            }
        }

        private class PInvokeDelegateWrapperHashtable : LockFreeReaderHashtable<MetadataType, PInvokeDelegateWrapper>
        {
            protected override int GetKeyHashCode(MetadataType key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(PInvokeDelegateWrapper value)
            {
                return value.DelegateType.GetHashCode();
            }

            protected override bool CompareKeyToValue(MetadataType key, PInvokeDelegateWrapper value)
            {
                return Object.ReferenceEquals(key, value.DelegateType);
            }

            protected override bool CompareValueToValue(PInvokeDelegateWrapper value1, PInvokeDelegateWrapper value2)
            {
                return Object.ReferenceEquals(value1.DelegateType, value2.DelegateType);
            }

            protected override PInvokeDelegateWrapper CreateValueFromKey(MetadataType key)
            {
                return new PInvokeDelegateWrapper(_owningModule, key, _interopStateManager);
            }

            private readonly InteropStateManager _interopStateManager;
            private readonly ModuleDesc _owningModule;

            public PInvokeDelegateWrapperHashtable(InteropStateManager interopStateManager, ModuleDesc owningModule)
            {
                _interopStateManager = interopStateManager;
                _owningModule = owningModule;
            }
        }

        private class PInvokeLazyFixupFieldHashtable : LockFreeReaderHashtable<MethodDesc, PInvokeLazyFixupField>
        {
            protected override int GetKeyHashCode(MethodDesc key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(PInvokeLazyFixupField value)
            {
                return value.TargetMethod.GetHashCode();
            }

            protected override bool CompareKeyToValue(MethodDesc key, PInvokeLazyFixupField value)
            {
                return key == value.TargetMethod;
            }

            protected override bool CompareValueToValue(PInvokeLazyFixupField value1, PInvokeLazyFixupField value2)
            {
                return value1.TargetMethod == value2.TargetMethod;
            }

            protected override PInvokeLazyFixupField CreateValueFromKey(MethodDesc key)
            {
                return new PInvokeLazyFixupField(_owningType, key);
            }

            private readonly DefType _owningType;

            public PInvokeLazyFixupFieldHashtable(DefType owningType)
            {
                _owningType = owningType;
            }
        }

        private readonly record struct CalliMarshallingMethodThunkKey(MethodSignature Signature, bool RuntimeMarshallingEnabled);

        private class PInvokeCalliHashtable : LockFreeReaderHashtable<CalliMarshallingMethodThunkKey, CalliMarshallingMethodThunk>
        {
            private readonly InteropStateManager _interopStateManager;
            private readonly TypeDesc _owningType;

            protected override int GetKeyHashCode(CalliMarshallingMethodThunkKey key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(CalliMarshallingMethodThunk value)
            {
                return new CalliMarshallingMethodThunkKey(value.TargetSignature, value.RuntimeMarshallingEnabled).GetHashCode();
            }

            protected override bool CompareKeyToValue(CalliMarshallingMethodThunkKey key, CalliMarshallingMethodThunk value)
            {
                return key.Signature.Equals(value.TargetSignature) && key.RuntimeMarshallingEnabled == value.RuntimeMarshallingEnabled;
            }

            protected override bool CompareValueToValue(CalliMarshallingMethodThunk value1, CalliMarshallingMethodThunk value2)
            {
                return value1.TargetSignature.Equals(value2.TargetSignature) && value1.RuntimeMarshallingEnabled == value2.RuntimeMarshallingEnabled;
            }

            protected override CalliMarshallingMethodThunk CreateValueFromKey(CalliMarshallingMethodThunkKey key)
            {
                return new CalliMarshallingMethodThunk(key.Signature, _owningType, _interopStateManager, key.RuntimeMarshallingEnabled);
            }

            public PInvokeCalliHashtable(InteropStateManager interopStateManager, TypeDesc owningType)
            {
                _interopStateManager = interopStateManager;
                _owningType = owningType;
            }
        }
    }
}
