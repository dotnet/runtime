// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Reflection.Runtime.General;
using System.Text;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.NoMetadata
{
    /// <summary>
    /// Type that once had metadata, but that metadata is not available
    /// for the lifetime of the TypeSystemContext. Directly correlates
    /// to a RuntimeTypeHandle useable in the current environment.
    /// This type replaces the placeholder NoMetadataType that comes
    /// with the common type system codebase
    /// </summary>
    internal class NoMetadataType : DefType
    {
        private TypeSystemContext _context;
        private int _hashcode;
        private RuntimeTypeHandle _genericTypeDefinition;
        private DefType _genericTypeDefinitionAsDefType;
        private Instantiation _instantiation;

        // "_baseType == this" means "base type was not initialized yet"
        private DefType _baseType;

        public unsafe NoMetadataType(TypeSystemContext context, RuntimeTypeHandle genericTypeDefinition, int instantiationLength, ReadOnlySpan<Runtime.GenericVariance> runtimeVarianceData, int hashcode)
        {
            TypeDesc[] genericParameters;
            if (instantiationLength == 0)
            {
                genericParameters = Array.Empty<TypeDesc>();
            }
            else
            {
                genericParameters = new TypeDesc[instantiationLength];
                for (int i = 0; i < genericParameters.Length; i++)
                {
                    GenericVariance variance = runtimeVarianceData.Length == 0 ? GenericVariance.None : runtimeVarianceData[i] switch
                    {
                        Runtime.GenericVariance.Contravariant => GenericVariance.Contravariant,
                        Runtime.GenericVariance.Covariant => GenericVariance.Covariant,
                        Runtime.GenericVariance.NonVariant or Runtime.GenericVariance.ArrayCovariant => GenericVariance.None,
                        _ => throw new NotImplementedException()
                    };
                    genericParameters[i] = new RuntimeGenericParameterDesc(GenericParameterKind.Type, i, this, variance);
                }
            }

            Instantiation instantiation = new Instantiation(genericParameters);
            Init(context, genericTypeDefinition, null, instantiation, hashcode);
        }

        public unsafe NoMetadataType(TypeSystemContext context, RuntimeTypeHandle genericTypeDefinition, DefType genericTypeDefinitionAsDefType, Instantiation instantiation, int hashcode)
        {
            Init(context, genericTypeDefinition, genericTypeDefinitionAsDefType, instantiation, hashcode);
        }

        private void Init(TypeSystemContext context, RuntimeTypeHandle genericTypeDefinition, DefType genericTypeDefinitionAsDefType, Instantiation instantiation, int hashcode)
        {
            _hashcode = hashcode;
            _context = context;
            _genericTypeDefinition = genericTypeDefinition;
            _genericTypeDefinitionAsDefType = genericTypeDefinitionAsDefType;
            _genericTypeDefinitionAsDefType ??= this;

            _instantiation = instantiation;

            // Instantiation must either be:
            // Something valid (if the type is generic, or a generic type definition)
            // or Empty (if the type isn't a generic of any form)
            unsafe
            {
                Debug.Assert(((_instantiation.Length > 0) && _genericTypeDefinition.ToEETypePtr()->IsGenericTypeDefinition) ||
                             ((_instantiation.Length == 0) && !_genericTypeDefinition.ToEETypePtr()->IsGenericTypeDefinition));
            }

            // Base type is not initialized
            _baseType = this;
        }

        public override int GetHashCode()
        {
            return _hashcode;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _context;
            }
        }

        public override DefType BaseType
        {
            get
            {
                // _baseType == this means we didn't initialize it yet
                if (_baseType != this)
                    return _baseType;

                if (RetrieveRuntimeTypeHandleIfPossible())
                {
                    RuntimeTypeHandle baseTypeHandle;
                    if (!RuntimeAugments.TryGetBaseType(RuntimeTypeHandle, out baseTypeHandle))
                    {
                        Debug.Assert(false);
                    }

                    DefType baseType = !baseTypeHandle.IsNull() ? (DefType)Context.ResolveRuntimeTypeHandle(baseTypeHandle) : null;
                    SetBaseType(baseType);

                    return baseType;
                }
                else
                {
                    // Parsing of the base type has not yet happened. Perform that part of native layout parsing
                    // just-in-time
                    TypeBuilderState state = GetOrCreateTypeBuilderState();

                    ComputeTemplate();
                    NativeParser typeInfoParser = state.GetParserForNativeLayoutInfo();
                    NativeParser baseTypeParser = typeInfoParser.GetParserForBagElementKind(BagElementKind.BaseType);

                    ParseBaseType(state.NativeLayoutInfo.LoadContext, baseTypeParser);
                    Debug.Assert(_baseType != this);
                    return _baseType;
                }
            }
        }

        internal override void ParseBaseType(NativeLayoutInfoLoadContext nativeLayoutInfoLoadContext, NativeParser baseTypeParser)
        {
            if (!baseTypeParser.IsNull)
            {
                // If the base type is available from the native layout info use it if the type we have is a NoMetadataType
                SetBaseType((DefType)nativeLayoutInfoLoadContext.GetType(ref baseTypeParser));
            }
            else
            {
                // Set the base type for no metadata types, if we reach this point, and there isn't a parser, then we simply use the value from the template
                SetBaseType(ComputeTemplate().BaseType);
            }
        }

        /// <summary>
        /// This is used to set base type for generic types without metadata
        /// </summary>
        public void SetBaseType(DefType baseType)
        {
            Debug.Assert(_baseType == this || _baseType == baseType);
            _baseType = baseType;
        }

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = 0;

            if ((mask & TypeFlags.CategoryMask) != 0)
            {
                unsafe
                {
                    MethodTable* MethodTable = _genericTypeDefinition.ToEETypePtr();
                    EETypeElementType elementType = MethodTable->ElementType;
                    if (elementType == EETypeElementType.SystemArray)
                    {
                        // System.Array is a regular class in the type system
                        flags |= TypeFlags.Class;
                    }
                    else if (elementType <= EETypeElementType.Double &&
                        (MethodTable->IsGenericTypeDefinition || MethodTable->BaseType == typeof(System.Enum).TypeHandle.ToEETypePtr()))
                    {
                        // Enums are represented as their underlying type in the runtime type system
                        // Note: we check for IsGenericDefinition above to cover generic enums (base types are not set
                        // on generic definition MethodTable)
                        flags |= TypeFlags.Enum;
                    }
                    else
                    {
                        // Paranoid check that we handled enums above
                        Debug.Assert(MethodTable->IsGenericTypeDefinition ||
                            MethodTable->BaseType != typeof(System.Enum).TypeHandle.ToEETypePtr());

                        // The rest of values should be directly castable to TypeFlags
                        Debug.Assert((int)EETypeElementType.Void == (int)TypeFlags.Void);
                        Debug.Assert((int)EETypeElementType.Int32 == (int)TypeFlags.Int32);
                        Debug.Assert((int)EETypeElementType.IntPtr == (int)TypeFlags.IntPtr);
                        Debug.Assert((int)EETypeElementType.Double == (int)TypeFlags.Double);
                        Debug.Assert((int)EETypeElementType.Pointer == (int)TypeFlags.Pointer);
                        Debug.Assert((int)EETypeElementType.Class == (int)TypeFlags.Class);
                        Debug.Assert((int)EETypeElementType.Nullable == (int)TypeFlags.Nullable);

                        flags |= (TypeFlags)elementType;
                    }
                }
            }

            if ((mask & TypeFlags.AttributeCacheComputed) != 0)
            {
                flags |= TypeFlags.AttributeCacheComputed;

                unsafe
                {
                    MethodTable* MethodTable = _genericTypeDefinition.ToEETypePtr();
                    if (MethodTable->IsByRefLike)
                    {
                        flags |= TypeFlags.IsByRefLike;
                    }
                }
            }

            if ((mask & TypeFlags.HasGenericVarianceComputed) != 0)
            {
                flags |= TypeFlags.HasGenericVarianceComputed;

                unsafe
                {
                    if (_genericTypeDefinition.ToEETypePtr()->HasGenericVariance)
                        flags |= TypeFlags.HasGenericVariance;
                }
            }

            return flags;
        }

        // Canonicalization handling

        public override bool IsCanonicalSubtype(CanonicalFormKind policy)
        {
            foreach (TypeDesc t in Instantiation)
            {
                if (t.IsCanonicalSubtype(policy))
                {
                    return true;
                }
            }

            return false;
        }

        protected override TypeDesc ConvertToCanonFormImpl(CanonicalFormKind kind)
        {
            bool needsChange;
            Instantiation canonInstantiation = Context.ConvertInstantiationToCanonForm(Instantiation, kind, out needsChange);
            if (needsChange)
            {
                TypeDesc openType = GetTypeDefinition();
                return Context.ResolveGenericInstantiation((DefType)openType, canonInstantiation);
            }

            return this;
        }

        public override TypeDesc GetTypeDefinition()
        {
            if (_genericTypeDefinitionAsDefType != null)
                return _genericTypeDefinitionAsDefType;
            else
                return this;
        }

        public override TypeDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            TypeDesc[] clone = null;

            for (int i = 0; i < _instantiation.Length; i++)
            {
                TypeDesc uninst = _instantiation[i];
                TypeDesc inst = uninst.InstantiateSignature(typeInstantiation, methodInstantiation);
                if (inst != uninst)
                {
                    if (clone == null)
                    {
                        clone = new TypeDesc[_instantiation.Length];
                        for (int j = 0; j < clone.Length; j++)
                        {
                            clone[j] = _instantiation[j];
                        }
                    }
                    clone[i] = inst;
                }
            }

            return (clone == null) ? this : _genericTypeDefinitionAsDefType.Context.ResolveGenericInstantiation(_genericTypeDefinitionAsDefType, new Instantiation(clone));
        }

        public override Instantiation Instantiation
        {
            get
            {
                return _instantiation;
            }
        }

        public override TypeDesc UnderlyingType
        {
            get
            {
                if (!this.IsEnum)
                    return this;

                unsafe
                {
                    EETypeElementType elementType = RuntimeTypeHandle.ToEETypePtr()->ElementType;
                    Debug.Assert((int)EETypeElementType.Void == (int)WellKnownType.Void);
                    Debug.Assert((int)EETypeElementType.Int32 == (int)WellKnownType.Int32);
                    Debug.Assert((int)EETypeElementType.IntPtr == (int)WellKnownType.IntPtr);
                    Debug.Assert((int)EETypeElementType.Double == (int)WellKnownType.Double);
                    Debug.Assert(elementType <= EETypeElementType.Double);
                    return Context.GetWellKnownType((WellKnownType)elementType);
                }
            }
        }

        private void GetTypeNameHelper(out string name, out string nsName, out string assemblyName)
        {
            RuntimeTypeHandle genericDefinitionHandle = GetTypeDefinition().GetRuntimeTypeHandle();
            Debug.Assert(!genericDefinitionHandle.IsNull());

#if DEBUG
            QTypeDefinition qTypeDefinition;

            string enclosingDummy;

            // Try to get the name from metadata
            if (TypeLoaderEnvironment.TryGetMetadataForNamedType(genericDefinitionHandle, out qTypeDefinition))
            {
                TypeDefinitionHandle typeDefHandle = qTypeDefinition.NativeFormatHandle;
                typeDefHandle.GetFullName(qTypeDefinition.NativeFormatReader, out name, out enclosingDummy, out nsName);
                assemblyName = typeDefHandle.GetContainingModuleName(qTypeDefinition.NativeFormatReader);
            }
            else
#endif
            {
                name = genericDefinitionHandle.LowLevelToStringRawEETypeAddress();
                nsName = "";
                assemblyName = "?";
            }
        }

        public string NamespaceForDiagnostics
        {
            get
            {
                GetTypeNameHelper(out _, out string nsName, out _);
                return nsName;
            }
        }

        public string NameForDiagnostics
        {
            get
            {
                GetTypeNameHelper(out string name, out _, out _);
                return name;
            }
        }

        public string DiagnosticModuleName
        {
            get
            {
                GetTypeNameHelper(out _, out _, out string assemblyName);
                return assemblyName;
            }
        }

#if DEBUG
        private string _cachedToString;

        public override string ToString()
        {
            if (_cachedToString != null)
                return _cachedToString;

            StringBuilder sb = new StringBuilder();

            if (!_genericTypeDefinition.IsNull())
                sb.Append(_genericTypeDefinition.LowLevelToString());
            else if (!RuntimeTypeHandle.IsNull())
                sb.Append(RuntimeTypeHandle.LowLevelToString());

            if (!Instantiation.IsNull)
            {
                for (int i = 0; i < Instantiation.Length; i++)
                {
                    sb.Append(i == 0 ? "[" : ", ");
                    sb.Append(Instantiation[i].ToString());
                }
                if (Instantiation.Length > 0) sb.Append(']');
            }

            _cachedToString = sb.ToString();

            return _cachedToString;
        }
#endif
    }
}
