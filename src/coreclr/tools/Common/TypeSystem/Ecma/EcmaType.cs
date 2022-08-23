// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Debug = System.Diagnostics.Debug;

using Internal.NativeFormat;

namespace Internal.TypeSystem.Ecma
{
    /// <summary>
    /// Override of MetadataType that uses actual Ecma335 metadata.
    /// </summary>
    public sealed partial class EcmaType : MetadataType, EcmaModule.IEntityHandleObject
    {
        private EcmaModule _module;
        private TypeDefinitionHandle _handle;

        private TypeDefinition _typeDefinition;

        // Cached values
        private string _typeName;
        private string _typeNamespace;
        private TypeDesc[] _genericParameters;
        private MetadataType _baseType;
        private int _hashcode;

        internal EcmaType(EcmaModule module, TypeDefinitionHandle handle)
        {
            _module = module;
            _handle = handle;

            _typeDefinition = module.MetadataReader.GetTypeDefinition(handle);

            _baseType = this; // Not yet initialized flag

#if DEBUG
            // Initialize name eagerly in debug builds for convenience
            InitializeName();
            InitializeNamespace();
#endif
        }

        public override int GetHashCode()
        {
            if (_hashcode != 0)
                return _hashcode;
            return InitializeHashCode();
        }

        private int InitializeHashCode()
        {
            TypeDesc containingType = ContainingType;
            if (containingType == null)
            {
                string ns = Namespace;
                var hashCodeBuilder = new TypeHashingAlgorithms.HashCodeBuilder(ns);
                if (ns.Length > 0)
                    hashCodeBuilder.Append(".");
                hashCodeBuilder.Append(Name);
                _hashcode = hashCodeBuilder.ToHashCode();
            }
            else
            {
                _hashcode = TypeHashingAlgorithms.ComputeNestedTypeHashCode(
                    containingType.GetHashCode(), TypeHashingAlgorithms.ComputeNameHashCode(Name));
            }

            return _hashcode;
        }

        EntityHandle EcmaModule.IEntityHandleObject.Handle
        {
            get
            {
                return _handle;
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _module.Context;
            }
        }

        private void ComputeGenericParameters()
        {
            var genericParameterHandles = _typeDefinition.GetGenericParameters();
            int count = genericParameterHandles.Count;
            if (count > 0)
            {
                TypeDesc[] genericParameters = new TypeDesc[count];
                int i = 0;
                foreach (var genericParameterHandle in genericParameterHandles)
                {
                    genericParameters[i++] = new EcmaGenericParameter(_module, genericParameterHandle);
                }
                Interlocked.CompareExchange(ref _genericParameters, genericParameters, null);
            }
            else
            {
                _genericParameters = TypeDesc.EmptyTypes;
            }
        }

        public override Instantiation Instantiation
        {
            get
            {
                if (_genericParameters == null)
                    ComputeGenericParameters();
                return new Instantiation(_genericParameters);
            }
        }

        public override ModuleDesc Module
        {
            get
            {
                return _module;
            }
        }

        public EcmaModule EcmaModule
        {
            get
            {
                return _module;
            }
        }

        public MetadataReader MetadataReader
        {
            get
            {
                return _module.MetadataReader;
            }
        }

        public TypeDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        private MetadataType InitializeBaseType()
        {
            var baseTypeHandle = _typeDefinition.BaseType;
            if (baseTypeHandle.IsNil)
            {
                _baseType = null;
                return null;
            }

            var type = _module.GetType(baseTypeHandle) as MetadataType;
            if (type == null)
            {
                // PREFER: "new TypeSystemException.TypeLoadException(ExceptionStringID.ClassLoadBadFormat, this)" but the metadata is too broken
                ThrowHelper.ThrowTypeLoadException(Namespace, Name, Module);
            }
            _baseType = type;
            return type;
        }

        public override DefType BaseType
        {
            get
            {
                if (_baseType == this)
                    return InitializeBaseType();
                return _baseType;
            }
        }

        public override MetadataType MetadataBaseType
        {
            get
            {
                if (_baseType == this)
                    return InitializeBaseType();
                return _baseType;
            }
        }

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = 0;

            if ((mask & TypeFlags.CategoryMask) != 0)
            {
                TypeDesc baseType = this.BaseType;

                if (baseType != null && baseType.IsWellKnownType(WellKnownType.ValueType))
                {
                    flags |= TypeFlags.ValueType;
                }
                else
                if (baseType != null && baseType.IsWellKnownType(WellKnownType.Enum))
                {
                    flags |= TypeFlags.Enum;
                }
                else
                {
                    if ((_typeDefinition.Attributes & TypeAttributes.Interface) != 0)
                        flags |= TypeFlags.Interface;
                    else
                        flags |= TypeFlags.Class;
                }

                // All other cases are handled during TypeSystemContext initialization
            }

            if ((mask & TypeFlags.HasGenericVarianceComputed) != 0)
            {
                flags |= TypeFlags.HasGenericVarianceComputed;

                foreach (GenericParameterDesc genericParam in Instantiation)
                {
                    if (genericParam.Variance != GenericVariance.None)
                    {
                        flags |= TypeFlags.HasGenericVariance;
                        break;
                    }
                }
            }

            if ((mask & TypeFlags.HasFinalizerComputed) != 0)
            {
                flags |= TypeFlags.HasFinalizerComputed;

                if (GetFinalizer() != null)
                    flags |= TypeFlags.HasFinalizer;
            }

            if ((mask & TypeFlags.AttributeCacheComputed) != 0)
            {
                MetadataReader reader = MetadataReader;
                MetadataStringComparer stringComparer = reader.StringComparer;
                bool isValueType = IsValueType;

                flags |= TypeFlags.AttributeCacheComputed;

                foreach (CustomAttributeHandle attributeHandle in _typeDefinition.GetCustomAttributes())
                {
                    if (MetadataReader.GetAttributeNamespaceAndName(attributeHandle, out StringHandle namespaceHandle, out StringHandle nameHandle))
                    {
                        if (isValueType &&
                            stringComparer.Equals(nameHandle, "IsByRefLikeAttribute") &&
                            stringComparer.Equals(namespaceHandle, "System.Runtime.CompilerServices"))
                            flags |= TypeFlags.IsByRefLike;

                        if (stringComparer.Equals(nameHandle, "IntrinsicAttribute") &&
                            stringComparer.Equals(namespaceHandle, "System.Runtime.CompilerServices"))
                            flags |= TypeFlags.IsIntrinsic;
                    }
                }
            }

            return flags;
        }

        private string InitializeName()
        {
            var metadataReader = this.MetadataReader;
            _typeName = metadataReader.GetString(_typeDefinition.Name);
            return _typeName;
        }

        public override string Name
        {
            get
            {
                if (_typeName == null)
                    return InitializeName();
                return _typeName;
            }
        }

        private string InitializeNamespace()
        {
            var metadataReader = this.MetadataReader;
            _typeNamespace = metadataReader.GetString(_typeDefinition.Namespace);
            return _typeNamespace;
        }

        public override string Namespace
        {
            get
            {
                if (_typeNamespace == null)
                    return InitializeNamespace();
                return _typeNamespace;
            }
        }

        public override IEnumerable<MethodDesc> GetMethods()
        {
            foreach (var handle in _typeDefinition.GetMethods())
            {
                yield return (EcmaMethod)_module.GetObject(handle);
            }
        }

        public override IEnumerable<MethodDesc> GetVirtualMethods()
        {
            MetadataReader reader = _module.MetadataReader;
            foreach (var handle in _typeDefinition.GetMethods())
            {
                MethodDefinition methodDef = reader.GetMethodDefinition(handle);
                if ((methodDef.Attributes & MethodAttributes.Virtual) != 0)
                    yield return (EcmaMethod)_module.GetObject(handle);
            }
        }

        public override MethodDesc GetMethod(string name, MethodSignature signature, Instantiation substitution)
        {
            var metadataReader = this.MetadataReader;
            var stringComparer = metadataReader.StringComparer;

            foreach (var handle in _typeDefinition.GetMethods())
            {
                if (stringComparer.Equals(metadataReader.GetMethodDefinition(handle).Name, name))
                {
                    var method = (EcmaMethod)_module.GetObject(handle);
                    if (signature == null || signature.Equals(method.Signature.ApplySubstitution(substitution)))
                        return method;
                }
            }

            return null;
        }

        public override MethodDesc GetStaticConstructor()
        {
            var metadataReader = this.MetadataReader;
            var stringComparer = metadataReader.StringComparer;

            foreach (var handle in _typeDefinition.GetMethods())
            {
                var methodDefinition = metadataReader.GetMethodDefinition(handle);
                if (methodDefinition.Attributes.IsRuntimeSpecialName() &&
                    stringComparer.Equals(methodDefinition.Name, ".cctor"))
                {
                    var method = (EcmaMethod)_module.GetObject(handle);
                    return method;
                }
            }

            return null;
        }

        public override MethodDesc GetDefaultConstructor()
        {
            if (IsAbstract)
                return null;

            MetadataReader metadataReader = this.MetadataReader;
            MetadataStringComparer stringComparer = metadataReader.StringComparer;

            foreach (var handle in _typeDefinition.GetMethods())
            {
                var methodDefinition = metadataReader.GetMethodDefinition(handle);
                MethodAttributes attributes = methodDefinition.Attributes;
                if (attributes.IsRuntimeSpecialName() && attributes.IsPublic()
                    && stringComparer.Equals(methodDefinition.Name, ".ctor"))
                {
                    var method = (EcmaMethod)_module.GetObject(handle);
                    MethodSignature sig = method.Signature;

                    if (sig.Length != 0)
                        continue;

                    if ((sig.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask) == MethodSignatureFlags.CallingConventionVarargs)
                        continue;

                    return method;
                }
            }

            return null;
        }

        public override MethodDesc GetFinalizer()
        {
            // System.Object defines Finalize but doesn't use it, so we can determine that a type has a Finalizer
            // by checking for a virtual method override that lands anywhere other than Object in the inheritance
            // chain.
            if (!HasBaseType)
                return null;

            TypeDesc objectType = Context.GetWellKnownType(WellKnownType.Object);
            MethodDesc decl = objectType.GetMethod("Finalize", null);

            if (decl != null)
            {
                MethodDesc impl = this.FindVirtualFunctionTargetMethodOnObjectType(decl);
                if (impl == null)
                {
                    // TODO: invalid input: the type doesn't derive from our System.Object
                    throw new TypeLoadException(this.GetFullName());
                }

                if (impl.OwningType != objectType)
                {
                    return impl;
                }

                return null;
            }

            // Class library doesn't have finalizers
            return null;
        }

        public override IEnumerable<FieldDesc> GetFields()
        {
            foreach (var handle in _typeDefinition.GetFields())
            {
                var field = (EcmaField)_module.GetObject(handle);
                yield return field;
            }
        }

        public override FieldDesc GetField(string name)
        {
            var metadataReader = this.MetadataReader;
            var stringComparer = metadataReader.StringComparer;

            foreach (var handle in _typeDefinition.GetFields())
            {
                if (stringComparer.Equals(metadataReader.GetFieldDefinition(handle).Name, name))
                {
                    var field = (EcmaField)_module.GetObject(handle);
                    return field;
                }
            }

            return null;
        }

        public override IEnumerable<MetadataType> GetNestedTypes()
        {
            foreach (var handle in _typeDefinition.GetNestedTypes())
            {
                yield return (EcmaType)_module.GetObject(handle);
            }
        }

        public override MetadataType GetNestedType(string name)
        {
            var metadataReader = this.MetadataReader;
            var stringComparer = metadataReader.StringComparer;

            foreach (var handle in _typeDefinition.GetNestedTypes())
            {
                bool nameMatched;
                TypeDefinition type = metadataReader.GetTypeDefinition(handle);
                if (type.Namespace.IsNil)
                {
                    nameMatched = stringComparer.Equals(type.Name, name);
                }
                else
                {
                    string typeName = metadataReader.GetString(type.Name);
                    typeName = metadataReader.GetString(type.Namespace) + "." + typeName;
                    nameMatched = typeName == name;
                }

                if (nameMatched)
                    return (EcmaType)_module.GetObject(handle);
            }

            return null;
        }

        public TypeAttributes Attributes
        {
            get
            {
                return _typeDefinition.Attributes;
            }
        }

        public override DefType ContainingType
        {
            get
            {
                if (!_typeDefinition.Attributes.IsNested())
                    return null;

                var handle = _typeDefinition.GetDeclaringType();
                return (DefType)_module.GetType(handle);
            }
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return !MetadataReader.GetCustomAttributeHandle(_typeDefinition.GetCustomAttributes(),
                attributeNamespace, attributeName).IsNil;
        }

        public override ClassLayoutMetadata GetClassLayout()
        {
            TypeLayout layout = _typeDefinition.GetLayout();

            ClassLayoutMetadata result;
            result.PackingSize = layout.PackingSize;
            result.Size = layout.Size;

            // Skip reading field offsets if this is not explicit layout
            if (IsExplicitLayout)
            {
                var fieldDefinitionHandles = _typeDefinition.GetFields();
                var numInstanceFields = 0;

                foreach (var handle in fieldDefinitionHandles)
                {
                    var fieldDefinition = MetadataReader.GetFieldDefinition(handle);
                    if ((fieldDefinition.Attributes & FieldAttributes.Static) != 0)
                        continue;

                    numInstanceFields++;
                }

                result.Offsets = new FieldAndOffset[numInstanceFields];

                int index = 0;
                foreach (var handle in fieldDefinitionHandles)
                {
                    var fieldDefinition = MetadataReader.GetFieldDefinition(handle);
                    if ((fieldDefinition.Attributes & FieldAttributes.Static) != 0)
                        continue;

                    // Note: GetOffset() returns -1 when offset was not set in the metadata
                    int specifiedOffset = fieldDefinition.GetOffset();
                    result.Offsets[index] =
                        new FieldAndOffset((EcmaField)_module.GetObject(handle), specifiedOffset == -1 ? FieldAndOffset.InvalidOffset : new LayoutInt(specifiedOffset));

                    index++;
                }
            }
            else
                result.Offsets = null;

            return result;
        }

        public override bool IsExplicitLayout
        {
            get
            {
                return (_typeDefinition.Attributes & TypeAttributes.ExplicitLayout) != 0;
            }
        }

        public override bool IsSequentialLayout
        {
            get
            {
                return (_typeDefinition.Attributes & TypeAttributes.SequentialLayout) != 0;
            }
        }

        public override bool IsBeforeFieldInit
        {
            get
            {
                return (_typeDefinition.Attributes & TypeAttributes.BeforeFieldInit) != 0;
            }
        }

        public override bool IsModuleType
        {
            get
            {
                return _handle.Equals(MetadataTokens.TypeDefinitionHandle(0x00000001 /* COR_GLOBAL_PARENT_TOKEN */));
            }
        }

        public override bool IsSealed
        {
            get
            {
                return (_typeDefinition.Attributes & TypeAttributes.Sealed) != 0;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return (_typeDefinition.Attributes & TypeAttributes.Abstract) != 0;
            }
        }

        public override PInvokeStringFormat PInvokeStringFormat
        {
            get
            {
                return (PInvokeStringFormat)(_typeDefinition.Attributes & TypeAttributes.StringFormatMask);
            }
        }
    }
}
