// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

using System.Reflection.Runtime.General;

using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;
using Internal.Runtime.TypeLoader;
using Internal.TypeSystem.NativeFormat;
using Internal.TypeSystem.NoMetadata;
using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;

namespace Internal.TypeSystem
{
    public abstract partial class TypeSystemContext
    {
        internal TemplateLocator TemplateLookup => new TemplateLocator();

        internal class RuntimeTypeHandleToParameterTypeRuntimeTypeHandleHashtable : LockFreeReaderHashtableOfPointers<RuntimeTypeHandle, RuntimeTypeHandle>
        {
            protected override bool CompareKeyToValue(RuntimeTypeHandle key, RuntimeTypeHandle value)
            {
                unsafe
                {
                    return value.ToEETypePtr()->RelatedParameterType->ToRuntimeTypeHandle().Equals(key);
                }
            }

            protected override bool CompareValueToValue(RuntimeTypeHandle value1, RuntimeTypeHandle value2)
            {
                return value1.Equals(value2);
            }

            protected override RuntimeTypeHandle ConvertIntPtrToValue(IntPtr pointer)
            {
                unsafe
                {
                    return ((MethodTable*)pointer.ToPointer())->ToRuntimeTypeHandle();
                }
            }

            protected override IntPtr ConvertValueToIntPtr(RuntimeTypeHandle value)
            {
                return value.ToIntPtr();
            }

            protected override RuntimeTypeHandle CreateValueFromKey(RuntimeTypeHandle key)
            {
                throw new NotSupportedException();
            }

            protected override int GetKeyHashCode(RuntimeTypeHandle key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(RuntimeTypeHandle value)
            {
                unsafe
                {
                    return (int)value.ToEETypePtr()->RelatedParameterType->HashCode;
                }
            }
        }

        internal static RuntimeTypeHandleToParameterTypeRuntimeTypeHandleHashtable[] s_ArrayTypesCaches = new RuntimeTypeHandleToParameterTypeRuntimeTypeHandleHashtable[MDArray.MaxRank + 1];
        /// <summary>
        ///  Cache of array types created by the builder to prevent duplication
        /// </summary>
        internal static RuntimeTypeHandleToParameterTypeRuntimeTypeHandleHashtable GetArrayTypesCache(bool isMdArray, int rank)
        {
            if (isMdArray && (rank < MDArray.MinRank || rank > MDArray.MaxRank))
                throw new PlatformNotSupportedException();

            if (!isMdArray)
                rank = 0;

            if (s_ArrayTypesCaches[rank] == null)
                s_ArrayTypesCaches[rank] = new RuntimeTypeHandleToParameterTypeRuntimeTypeHandleHashtable();
            return s_ArrayTypesCaches[rank];
        }

        /// <summary>
        /// Cache of pointer types created by the builder to prevent duplication
        /// </summary>
        internal static RuntimeTypeHandleToParameterTypeRuntimeTypeHandleHashtable PointerTypesCache { get; } =
            new RuntimeTypeHandleToParameterTypeRuntimeTypeHandleHashtable();

        /// <summary>
        /// Cache of ByRef types created by the builder to prevent duplication
        /// </summary>
        internal static RuntimeTypeHandleToParameterTypeRuntimeTypeHandleHashtable ByRefTypesCache { get; } =
            new RuntimeTypeHandleToParameterTypeRuntimeTypeHandleHashtable();

        public Instantiation ResolveRuntimeTypeHandles(RuntimeTypeHandle[] runtimeTypeHandles)
        {
            TypeDesc[] TypeDescs = new TypeDesc[runtimeTypeHandles.Length];
            for (int i = 0; i < runtimeTypeHandles.Length; i++)
                TypeDescs[i] = ResolveRuntimeTypeHandle(runtimeTypeHandles[i]);
            return new Instantiation(TypeDescs);
        }

        // This dictionary is in every scenario - create it eagerly
        private LowLevelDictionary<RuntimeTypeHandle, TypeDesc> _runtimeTypeHandleResolutionCache =
             new LowLevelDictionary<RuntimeTypeHandle, TypeDesc>();

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        internal TypeDesc GetTypeDescFromQHandle(QTypeDefinition qTypeDefinition)
        {
#if ECMA_METADATA_SUPPORT
            if (qTypeDefinition.IsNativeFormatMetadataBased)
#endif
            {
                MetadataReader nativeFormatMetadataReader = qTypeDefinition.NativeFormatReader;
                TypeDefinitionHandle typeDefinitionHandle = qTypeDefinition.NativeFormatHandle;
                NativeFormatModuleInfo module = ModuleList.Instance.GetModuleInfoForMetadataReader(nativeFormatMetadataReader);
                NativeFormatMetadataUnit metadataUnit = ResolveMetadataUnit(module);
                NativeFormatType nativeFormatType = (NativeFormatType)metadataUnit.GetType(typeDefinitionHandle);
                return nativeFormatType;
            }
#if ECMA_METADATA_SUPPORT
            else if (qTypeDefinition.IsEcmaFormatMetadataBased)
            {
                EcmaModuleInfo module = ModuleList.Instance.GetModuleInfoForMetadataReader(qTypeDefinition.EcmaFormatReader);
                Ecma.EcmaModule ecmaModule = ResolveEcmaModule(module);
                Ecma.EcmaType ecmaType = (Ecma.EcmaType)ecmaModule.GetType(qTypeDefinition.EcmaFormatHandle);
                return ecmaType;
            }
#endif
            return null;
        }
#endif

        // Helper routine for ResolveRuntimeTypeHandle, used to handle lookups which may result in a metadata based type.
        private TypeDesc TryGetMetadataBasedTypeFromRuntimeTypeHandle_Uncached(RuntimeTypeHandle rtth)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            QTypeDefinition qTypeDefinition;
            if (TypeLoaderEnvironment.Instance.TryGetMetadataForNamedType(rtth, out qTypeDefinition))
            {
                return GetTypeDescFromQHandle(qTypeDefinition);
            }
#endif
            return null;
        }

        public TypeDesc ResolveRuntimeTypeHandle(RuntimeTypeHandle rtth)
        {
            TypeDesc returnedType;
            if (_runtimeTypeHandleResolutionCache.TryGetValue(rtth, out returnedType))
                return returnedType;

            if (rtth.Equals(CanonType.RuntimeTypeHandle))
            {
                returnedType = CanonType;
            }
            else if (rtth.Equals(UniversalCanonType.RuntimeTypeHandle))
            {
                returnedType = UniversalCanonType;
            }
            else if (RuntimeAugments.IsGenericTypeDefinition(rtth))
            {
                returnedType = TryGetMetadataBasedTypeFromRuntimeTypeHandle_Uncached(rtth);
                if (returnedType == null)
                {
                    unsafe
                    {
                        TypeDesc[] genericParameters = new TypeDesc[rtth.ToEETypePtr()->GenericArgumentCount];
                        for (int i = 0; i < genericParameters.Length; i++)
                        {
                            genericParameters[i] = GetSignatureVariable(i, false);
                        }

                        returnedType = new NoMetadataType(this, rtth, null, new Instantiation(genericParameters), rtth.GetHashCode());
                    }
                }
            }
            else if (RuntimeAugments.IsGenericType(rtth))
            {
                RuntimeTypeHandle typeDefRuntimeTypeHandle;
                RuntimeTypeHandle[] genericArgRuntimeTypeHandles;
                typeDefRuntimeTypeHandle = RuntimeAugments.GetGenericInstantiation(rtth, out genericArgRuntimeTypeHandles);

                DefType typeDef = (DefType)ResolveRuntimeTypeHandle(typeDefRuntimeTypeHandle);
                Instantiation genericArgs = ResolveRuntimeTypeHandles(genericArgRuntimeTypeHandles);
                returnedType = ResolveGenericInstantiation(typeDef, genericArgs);
            }
            else if (RuntimeAugments.IsArrayType(rtth))
            {
                RuntimeTypeHandle elementTypeHandle = RuntimeAugments.GetRelatedParameterTypeHandle(rtth);
                TypeDesc elementType = ResolveRuntimeTypeHandle(elementTypeHandle);
                unsafe
                {
                    if (rtth.ToEETypePtr()->IsSzArray)
                        returnedType = GetArrayType(elementType);
                    else
                        returnedType = GetArrayType(elementType, rtth.ToEETypePtr()->ArrayRank);
                }
            }
            else if (RuntimeAugments.IsUnmanagedPointerType(rtth))
            {
                RuntimeTypeHandle targetTypeHandle = RuntimeAugments.GetRelatedParameterTypeHandle(rtth);
                TypeDesc targetType = ResolveRuntimeTypeHandle(targetTypeHandle);
                returnedType = GetPointerType(targetType);
            }
            else if (RuntimeAugments.IsByRefType(rtth))
            {
                RuntimeTypeHandle targetTypeHandle = RuntimeAugments.GetRelatedParameterTypeHandle(rtth);
                TypeDesc targetType = ResolveRuntimeTypeHandle(targetTypeHandle);
                returnedType = GetByRefType(targetType);
            }
            else
            {
                returnedType = TryGetMetadataBasedTypeFromRuntimeTypeHandle_Uncached(rtth);
                if (returnedType == null)
                {
                    returnedType = new NoMetadataType(this, rtth, null, Instantiation.Empty, rtth.GetHashCode());
                }
            }

            // We either retrieved an existing DefType that is already registered with the runtime
            // or one that is not associated with an MethodTable yet. If it's not associated, associate it.
            if (returnedType.RuntimeTypeHandle.IsNull())
            {
                TypeBuilderState state = returnedType.GetTypeBuilderStateIfExist();
                bool skipStoringRuntimeTypeHandle = false;

                // If we've already attempted to lookup and failed to retrieve this type handle, we
                // may have already decided to create a new one. In that case, do not attempt to abort
                // that creation process as it may have already begun the process of type creation
                if (state != null && state.AttemptedAndFailedToRetrieveTypeHandle)
                    skipStoringRuntimeTypeHandle = true;

                if (!skipStoringRuntimeTypeHandle)
                    returnedType.SetRuntimeTypeHandleUnsafe(rtth);
            }

            _runtimeTypeHandleResolutionCache.Add(rtth, returnedType);

            return returnedType.WithDebugName();
        }

        private struct GenericTypeInstanceKey : IEquatable<GenericTypeInstanceKey>
        {
            private DefType _typeDefinition;
            private Instantiation _instantiation;
            private int _hashCode;

            public GenericTypeInstanceKey(DefType typeDefinition, Instantiation instantiation)
            {
                _typeDefinition = typeDefinition;
                _instantiation = instantiation;

                _hashCode = instantiation.ComputeGenericInstanceHashCode(typeDefinition.GetHashCode());
            }

            public bool Equals(GenericTypeInstanceKey other)
            {
                if (_typeDefinition != other._typeDefinition)
                    return false;

                Debug.Assert(_instantiation.Length == other._instantiation.Length);

                for (int i = 0; i < _instantiation.Length; i++)
                    if (_instantiation[i] != other._instantiation[i])
                        return false;

                return true;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is GenericTypeInstanceKey))
                    return false;

                return Equals((GenericTypeInstanceKey)obj);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }

        private struct RuntimeMethodKey
        {
            private bool _unboxingStub;
            private DefType _owningType;
            private MethodNameAndSignature _methodNameAndSignature;
            private int _hashCode;

            public RuntimeMethodKey(bool unboxingStub, DefType owningType, MethodNameAndSignature nameAndSignature)
            {
                _unboxingStub = unboxingStub;
                _owningType = owningType;
                _methodNameAndSignature = nameAndSignature;

                _hashCode = TypeHashingAlgorithms.ComputeMethodHashCode(owningType.GetHashCode(), TypeHashingAlgorithms.ComputeNameHashCode(nameAndSignature.Name));
            }

            public class RuntimeMethodKeyHashtable : LockFreeReaderHashtable<RuntimeMethodKey, MethodDesc>
            {
                protected override int GetKeyHashCode(RuntimeMethodKey key)
                {
                    return key._hashCode;
                }

                protected override int GetValueHashCode(MethodDesc value)
                {
                    return value.GetHashCode();
                }

                protected override bool CompareKeyToValue(RuntimeMethodKey key, MethodDesc value)
                {
                    if (value is RuntimeMethodDesc)
                    {
                        RuntimeMethodDesc runtimeMethod = (RuntimeMethodDesc)value;

                        if (key._unboxingStub != runtimeMethod.UnboxingStub)
                            return false;

                        if (!key._owningType.Equals(runtimeMethod.OwningType))
                            return false;

                        if (!key._methodNameAndSignature.Equals(runtimeMethod.NameAndSignature))
                            return false;

                        return true;
                    }
                    else
                    {
                        // Only RuntimeMethodDesc can be an unboxing stub
                        if (key._unboxingStub)
                            return false;

                        if (!key._owningType.Equals(value.OwningType))
                            return false;

                        if (!key._methodNameAndSignature.Equals(value.NameAndSignature))
                            return false;

                        return true;
                    }
                }

                protected override bool CompareValueToValue(MethodDesc value1, MethodDesc value2)
                {
                    if (value1 is RuntimeMethodDesc || value2 is RuntimeMethodDesc)
                    {
                        // If one is a RuntimeMethodDesc, they must both be to be equals
                        if (!(value1 is RuntimeMethodDesc) || !(value2 is RuntimeMethodDesc))
                        {
                            return false;
                        }
                        if (((RuntimeMethodDesc)value1).UnboxingStub != ((RuntimeMethodDesc)value2).UnboxingStub)
                            return false;

                        if (!value1.OwningType.Equals(value2.OwningType))
                            return false;

                        if (!value1.NameAndSignature.Equals(value2.NameAndSignature))
                            return false;

                        return true;
                    }
                    else
                    {
                        // Allocation of non RuntimeMethodDescs is not handled by this context, so we can just
                        // do a reference equality check here.
                        return value1 == value2;
                    }
                }

                protected override MethodDesc CreateValueFromKey(RuntimeMethodKey key)
                {
                    // unboxing stubs are always RuntimeMethodDesc
                    if (!key._unboxingStub)
                    {
                        // Instantiated Types always get their methods through GetMethodForInstantiatedType
                        if (key._owningType is InstantiatedType)
                        {
                            MethodDesc typicalMethod = key._owningType.Context.ResolveRuntimeMethod(key._unboxingStub, (DefType)key._owningType.GetTypeDefinition(), key._methodNameAndSignature, IntPtr.Zero, false);
                            return typicalMethod.Context.GetMethodForInstantiatedType(typicalMethod, (InstantiatedType)key._owningType);
                        }

                        // Otherwise, just check to see if there is a method discoverable via GetMethods
                        foreach (MethodDesc potentialMethod in key._owningType.GetMethods())
                        {
                            if (CompareKeyToValue(key, potentialMethod))
                            {
                                return potentialMethod;
                            }
                        }
                    }
                    else
                    {
                        // We should only have unboxing stubs on value types
                        Debug.Assert(key._owningType.IsValueType);
                    }

                    return new RuntimeMethodDesc(key._unboxingStub, key._owningType, key._methodNameAndSignature, key._hashCode);
                }
            }
        }

        private RuntimeMethodKey.RuntimeMethodKeyHashtable _runtimeMethods;

        internal MethodDesc ResolveRuntimeMethod(bool unboxingStub, DefType owningType, MethodNameAndSignature nameAndSignature, IntPtr functionPointer, bool usgFunctionPointer)
        {
            if (_runtimeMethods == null)
                _runtimeMethods = new RuntimeMethodKey.RuntimeMethodKeyHashtable();

            MethodDesc retVal = _runtimeMethods.GetOrCreateValue(new RuntimeMethodKey(unboxingStub, owningType, nameAndSignature));

            if (functionPointer != IntPtr.Zero)
            {
                retVal.SetFunctionPointer(functionPointer, usgFunctionPointer);
            }

            return retVal;
        }

        private LowLevelDictionary<GenericTypeInstanceKey, DefType> _genericTypeInstances;

        /// <summary>
        /// Get a DefType that is the generic instantiation of an open generic type over instantiation arguments
        /// This looks like a rename of GetInstantiatedType, but isn't because the corert GetInstantiatedType
        /// relies on typeDef being a MetadataType, whereas this permits non-metadata types.
        /// </summary>
        public DefType ResolveGenericInstantiation(DefType typeDef, Instantiation arguments)
        {
            Debug.Assert(typeDef.Instantiation.IsNull || typeDef.Instantiation.Length == arguments.Length);

            MetadataType typeAsMetadataType = typeDef as MetadataType;

            if (typeAsMetadataType != null)
                return GetInstantiatedType(typeAsMetadataType, arguments);

            if (_genericTypeInstances == null)
                _genericTypeInstances = new LowLevelDictionary<GenericTypeInstanceKey, DefType>();

            GenericTypeInstanceKey key = new GenericTypeInstanceKey(typeDef, arguments);

            DefType result;
            if (!_genericTypeInstances.TryGetValue(key, out result))
            {
                NoMetadataType nmTypeDef = (NoMetadataType)typeDef;
                Debug.Assert(RuntimeAugments.IsGenericTypeDefinition(nmTypeDef.RuntimeTypeHandle));
                result = new NoMetadataType(this, nmTypeDef.RuntimeTypeHandle, nmTypeDef, arguments, key.GetHashCode());

                _genericTypeInstances.Add(key, result);
            }

            return result.WithDebugName();
        }

        /// <summary>
        /// Find a method based on owner type and nativelayout name, method instantiation, and signature.
        /// </summary>
        public MethodDesc ResolveGenericMethodInstantiation(bool unboxingStub, DefType owningType, MethodNameAndSignature nameAndSignature, Instantiation methodInstantiation, IntPtr functionPointer, bool usgFunctionPointer)
        {
            var uninstantiatedMethod = ResolveRuntimeMethod(unboxingStub, owningType, nameAndSignature, IntPtr.Zero, false);

            MethodDesc returnedMethod;
            if (methodInstantiation.IsNull || (methodInstantiation.Length == 0))
            {
                returnedMethod = uninstantiatedMethod;
            }
            else
            {
                returnedMethod = GetInstantiatedMethod(uninstantiatedMethod, methodInstantiation);
            }

            if (functionPointer != IntPtr.Zero)
            {
                returnedMethod.SetFunctionPointer(functionPointer, usgFunctionPointer);
            }

            return returnedMethod;
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        public class ModuleToMetadataUnitHashtable : LockFreeReaderHashtable<NativeFormatModuleInfo, NativeFormat.NativeFormatMetadataUnit>
        {
            private TypeSystemContext _context;

            public ModuleToMetadataUnitHashtable(TypeSystemContext context)
            {
                _context = context;
            }

            protected override int GetKeyHashCode(NativeFormatModuleInfo key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(NativeFormat.NativeFormatMetadataUnit value)
            {
                return value.RuntimeModuleInfo.GetHashCode();
            }

            protected override bool CompareKeyToValue(NativeFormatModuleInfo key, NativeFormat.NativeFormatMetadataUnit value)
            {
                return key == value.RuntimeModuleInfo;
            }

            protected override bool CompareValueToValue(NativeFormat.NativeFormatMetadataUnit value1, NativeFormat.NativeFormatMetadataUnit value2)
            {
                return value1.RuntimeModuleInfo == value2.RuntimeModuleInfo;
            }

            protected override NativeFormat.NativeFormatMetadataUnit CreateValueFromKey(NativeFormatModuleInfo key)
            {
                return new NativeFormat.NativeFormatMetadataUnit(_context, key, key.MetadataReader);
            }
        }

        private ModuleToMetadataUnitHashtable _metadataUnits = null;

        internal NativeFormat.NativeFormatMetadataUnit ResolveMetadataUnit(NativeFormatModuleInfo module)
        {
            if (_metadataUnits == null)
                _metadataUnits = new ModuleToMetadataUnitHashtable(this);

            return _metadataUnits.GetOrCreateValue(module);
        }
#endif

        /// <summary>
        /// Returns an estimate of the number of objects tracked by this context
        /// </summary>
        public virtual int LoadFactor
        {
            get
            {
                int loadFactor = _arrayTypes.Count;
                loadFactor += _byRefTypes.Count;
                loadFactor += _fieldForInstantiatedTypes.Count;
                loadFactor += _instantiatedMethods.Count;
                loadFactor += _instantiatedTypes.Count;
                loadFactor += _methodForInstantiatedTypes.Count;
                loadFactor += _pointerTypes.Count;
                loadFactor += _signatureVariables.Count;

                if (_runtimeMethods != null)
                    loadFactor += _runtimeMethods.Count;

                if (_genericTypeInstances != null)
                    loadFactor += _genericTypeInstances.Count;

                // TODO: This doesn't track nongeneric types and members. Does that significantly affect the results?

                return loadFactor;
            }
        }

        private LowLevelList<TypeDesc> _typesToFlushTypeSystemStateFrom;

        /// <summary>
        /// Register the types that will get their attached TypeSystemState flushed if the
        /// type system context is recycled
        /// </summary>
        internal void RegisterTypeForTypeSystemStateFlushing(TypeDesc type)
        {
            if (_typesToFlushTypeSystemStateFrom == null)
                _typesToFlushTypeSystemStateFrom = new LowLevelList<TypeDesc>();
            _typesToFlushTypeSystemStateFrom.Add(type);
        }

        /// <summary>
        /// Remove the type system contexts from every type in this context that has one.
        /// This function must be called before a TypeSystemContext is recycled
        /// </summary>
        internal void FlushTypeBuilderStates()
        {
            if (_typesToFlushTypeSystemStateFrom != null)
            {
                for (int i = 0; i < _typesToFlushTypeSystemStateFrom.Count; i++)
                {
                    _typesToFlushTypeSystemStateFrom[i].TypeBuilderState = null;
                }
            }
            _typesToFlushTypeSystemStateFrom = null;
        }

#if ECMA_METADATA_SUPPORT
        public class ModuleToEcmaModuleHashtable : LockFreeReaderHashtable<EcmaModuleInfo, Internal.TypeSystem.Ecma.EcmaModule>
        {
            private TypeSystemContext _context;

            public ModuleToEcmaModuleHashtable(TypeSystemContext context)
            {
                _context = context;
            }

            protected override int GetKeyHashCode(EcmaModuleInfo key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(Internal.TypeSystem.Ecma.EcmaModule value)
            {
                return value.RuntimeModuleInfo.GetHashCode();
            }

            protected override bool CompareKeyToValue(EcmaModuleInfo key, Internal.TypeSystem.Ecma.EcmaModule value)
            {
                return key == value.RuntimeModuleInfo;
            }

            protected override bool CompareValueToValue(Internal.TypeSystem.Ecma.EcmaModule value1, Internal.TypeSystem.Ecma.EcmaModule value2)
            {
                return value1.RuntimeModuleInfo == value2.RuntimeModuleInfo;
            }

            protected override Internal.TypeSystem.Ecma.EcmaModule CreateValueFromKey(EcmaModuleInfo key)
            {
                Internal.TypeSystem.Ecma.EcmaModule result = new Internal.TypeSystem.Ecma.EcmaModule(_context, key.PE, key.MetadataReader, null, null);
                result.SetRuntimeModuleInfoUNSAFE(key);
                return result;
            }
        }

        private ModuleToEcmaModuleHashtable _ecmaModules = null;

        internal Internal.TypeSystem.Ecma.EcmaModule ResolveEcmaModule(EcmaModuleInfo module)
        {
            if (_ecmaModules == null)
                _ecmaModules = new ModuleToEcmaModuleHashtable(this);

            return _ecmaModules.GetOrCreateValue(module);
        }
#endif // ECMA_METADATA_SUPPORT
    }

    internal static partial class TypeNameHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T WithDebugName<T>(this T type) where T : TypeDesc
        {
#if DEBUG
            if (type.DebugName == null) type.DebugName = type.ToString();
#endif
            return type;
        }
    }
}
