// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;

using Internal.Metadata.NativeFormat;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;
using Internal.TypeSystem;
using Internal.TypeSystem.NativeFormat;
using Internal.TypeSystem.NoMetadata;
using Internal.Reflection.Core;
using Internal.Reflection.Execution;

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// TypeSystemContext that can interfact with the
    /// Redhawk runtime type system and native metadata
    /// </summary>
    public partial class TypeLoaderTypeSystemContext : TypeSystemContext
    {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        private static readonly MetadataFieldLayoutAlgorithm s_metadataFieldLayoutAlgorithm = new MetadataFieldLayoutAlgorithm();
        private static readonly MetadataVirtualMethodAlgorithm s_metadataVirtualMethodAlgorithm = new MetadataVirtualMethodAlgorithm();
        private static readonly MetadataRuntimeInterfacesAlgorithm s_metadataRuntimeInterfacesAlgorithm = new MetadataRuntimeInterfacesAlgorithm();
#endif
        private static readonly NoMetadataFieldLayoutAlgorithm s_noMetadataFieldLayoutAlgorithm = new NoMetadataFieldLayoutAlgorithm();
        private static readonly NoMetadataRuntimeInterfacesAlgorithm s_noMetadataRuntimeInterfacesAlgorithm = new NoMetadataRuntimeInterfacesAlgorithm();
        private static readonly NativeLayoutFieldAlgorithm s_nativeLayoutFieldAlgorithm = new NativeLayoutFieldAlgorithm();
        private static readonly NativeLayoutInterfacesAlgorithm s_nativeLayoutInterfacesAlgorithm = new NativeLayoutInterfacesAlgorithm();

        public TypeLoaderTypeSystemContext(TargetDetails targetDetails) : base(targetDetails)
        {
            ModuleDesc systemModule = null;

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            systemModule = ((MetadataType)GetWellKnownType(WellKnownType.Object)).Module;
#endif

            InitializeSystemModule(systemModule);
        }

        public override FieldLayoutAlgorithm GetLayoutAlgorithmForType(DefType type)
        {
            if ((type == UniversalCanonType)
#if SUPPORT_DYNAMIC_CODE
                || (type.IsRuntimeDeterminedType && (((RuntimeDeterminedType)type).CanonicalType == UniversalCanonType)))
#else
                )
#endif
            {
                return UniversalCanonLayoutAlgorithm.Instance;
            }
            else if (type.RetrieveRuntimeTypeHandleIfPossible())
            {
                // If the type is already constructed, use the NoMetadataFieldLayoutAlgorithm.
                // its more efficient than loading from native layout or metadata.
                return s_noMetadataFieldLayoutAlgorithm;
            }
            if (type.HasNativeLayout)
            {
                return s_nativeLayoutFieldAlgorithm;
            }
            else if (type is NoMetadataType)
            {
                return s_noMetadataFieldLayoutAlgorithm;
            }
            else
            {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                return s_metadataFieldLayoutAlgorithm;
#else
                Debug.Assert(false);
                return null;
#endif
            }
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForDefType(DefType type)
        {
            if (type.RetrieveRuntimeTypeHandleIfPossible() && !type.IsGenericDefinition)
            {
                // If the type is already constructed, use the NoMetadataRuntimeInterfacesAlgorithm.
                // its more efficient than loading from native layout or metadata.
                return s_noMetadataRuntimeInterfacesAlgorithm;
            }
            else if (type.HasNativeLayout)
            {
                return s_nativeLayoutInterfacesAlgorithm;
            }
            else if (type is NoMetadataType)
            {
                return s_noMetadataRuntimeInterfacesAlgorithm;
            }
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            else if (type is MetadataType)
            {
                return s_metadataRuntimeInterfacesAlgorithm;
            }
#endif
            else
            {
                Debug.Assert(false);
                return null;
            }
        }

        protected internal sealed override bool IsIDynamicInterfaceCastableInterface(DefType type)
        {
            throw new NotImplementedException();
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForNonPointerArrayType(ArrayType type)
        {
            // At runtime, we're instantiating an Array<T> instantiation as the template, so we know we'll always have
            // a NativeLayoutInterfacesAlgorithm to work with
            return s_nativeLayoutInterfacesAlgorithm;
        }

        public override DefType GetWellKnownType(WellKnownType wellKnownType, bool throwIfNotFound = true)
        {
            switch (wellKnownType)
            {
                case WellKnownType.Void:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(void).TypeHandle);

                case WellKnownType.Boolean:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(bool).TypeHandle);

                case WellKnownType.Char:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(char).TypeHandle);

                case WellKnownType.SByte:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(sbyte).TypeHandle);

                case WellKnownType.Byte:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(byte).TypeHandle);

                case WellKnownType.Int16:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(short).TypeHandle);

                case WellKnownType.UInt16:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(ushort).TypeHandle);

                case WellKnownType.Int32:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(int).TypeHandle);

                case WellKnownType.UInt32:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(uint).TypeHandle);

                case WellKnownType.Int64:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(long).TypeHandle);

                case WellKnownType.UInt64:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(ulong).TypeHandle);

                case WellKnownType.IntPtr:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(IntPtr).TypeHandle);

                case WellKnownType.UIntPtr:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(UIntPtr).TypeHandle);

                case WellKnownType.Single:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(float).TypeHandle);

                case WellKnownType.Double:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(double).TypeHandle);

                case WellKnownType.ValueType:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(ValueType).TypeHandle);

                case WellKnownType.Enum:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(Enum).TypeHandle);

                case WellKnownType.Nullable:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(Nullable<>).TypeHandle);

                case WellKnownType.Object:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(object).TypeHandle);

                case WellKnownType.String:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(string).TypeHandle);

                case WellKnownType.Array:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(Array).TypeHandle);

                case WellKnownType.MulticastDelegate:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(MulticastDelegate).TypeHandle);

                case WellKnownType.RuntimeTypeHandle:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(RuntimeTypeHandle).TypeHandle);

                case WellKnownType.RuntimeMethodHandle:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(RuntimeMethodHandle).TypeHandle);

                case WellKnownType.RuntimeFieldHandle:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(RuntimeFieldHandle).TypeHandle);

                case WellKnownType.Exception:
                    return (DefType)ResolveRuntimeTypeHandle(typeof(Exception).TypeHandle);

                default:
                    if (throwIfNotFound)
                        throw new TypeLoadException();
                    else
                        return null;
            }
        }

        public override ModuleDesc ResolveAssembly(AssemblyName name, bool throwErrorIfNotFound)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            AssemblyBindResult bindResult;
            Exception failureException;
            if (!AssemblyBinderImplementation.Instance.Bind(name.ToRuntimeAssemblyName(), cacheMissedLookups: true, out bindResult, out failureException))
            {
                if (throwErrorIfNotFound)
                    throw failureException;
                return null;
            }

            var moduleList = Internal.Runtime.TypeLoader.ModuleList.Instance;

            if (bindResult.Reader != null)
            {
                NativeFormatModuleInfo primaryModule = moduleList.GetModuleInfoForMetadataReader(bindResult.Reader);
                NativeFormatMetadataUnit metadataUnit = ResolveMetadataUnit(primaryModule);
                return metadataUnit.GetModule(bindResult.ScopeDefinitionHandle);
            }
#if ECMA_METADATA_SUPPORT
            else if (bindResult.EcmaMetadataReader != null)
            {
                EcmaModuleInfo ecmaModule = moduleList.GetModuleInfoForMetadataReader(bindResult.EcmaMetadataReader);
                return ResolveEcmaModule(ecmaModule);
            }
#endif
            else
            {
                // Should not be possible to reach here
                throw new Exception();
            }
#else
            return null;
#endif
        }

        public override VirtualMethodAlgorithm GetVirtualMethodAlgorithmForType(TypeDesc type)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            Debug.Assert(!type.IsArray, "Wanted to call GetClosestMetadataType?");

            return s_metadataVirtualMethodAlgorithm;
#else
            Debug.Assert(false);
            return null;
#endif
        }

        protected internal override Instantiation ConvertInstantiationToCanonForm(Instantiation instantiation, CanonicalFormKind kind, out bool changed)
        {
            return StandardCanonicalizationAlgorithm.ConvertInstantiationToCanonForm(instantiation, kind, out changed);
        }

        protected internal override TypeDesc ConvertToCanon(TypeDesc typeToConvert, CanonicalFormKind kind)
        {
            return StandardCanonicalizationAlgorithm.ConvertToCanon(typeToConvert, kind);
        }

        protected internal override bool ComputeHasGCStaticBase(FieldDesc field)
        {
            Debug.Assert(field.IsStatic);

            if (field is NativeLayoutFieldDesc)
            {
                return ((NativeLayoutFieldDesc)field).FieldStorage == Internal.NativeFormat.FieldStorage.GCStatic;
            }

            TypeDesc fieldType = field.FieldType;
            if (fieldType.IsValueType)
            {
                FieldDesc typicalField = field.GetTypicalFieldDefinition();

                if (field != typicalField)
                {
                    if (typicalField.FieldType.IsSignatureVariable)
                        return true;
                }
                if (fieldType.IsEnum || fieldType.IsPrimitive)
                    return false;
                return true;
            }
            else
                return fieldType.IsGCPointer;
        }

        protected internal override bool ComputeHasStaticConstructor(TypeDesc type)
        {
            if (type.RetrieveRuntimeTypeHandleIfPossible())
            {
                unsafe
                {
                    return type.RuntimeTypeHandle.ToEETypePtr()->HasCctor;
                }
            }
            else if (type is MetadataType)
            {
                return ((MetadataType)type).GetStaticConstructor() != null;
            }
            return false;
        }

        public override bool SupportsUniversalCanon => true;
        public override bool SupportsCanon => true;
    }
}
