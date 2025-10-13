// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;

using Internal.NativeFormat;

namespace Internal.TypeSystem.Ecma
{
    /// <summary>
    /// Override of MetadataType that uses actual Ecma335 metadata.
    /// </summary>
    public sealed partial class EcmaType : MetadataType, EcmaModule.IEntityHandleObject
    {
        private const TypeAttributes TypeAttributesExtendedLayout = (TypeAttributes)0x00000018;
        private EcmaModule _module;
        private TypeDefinitionHandle _handle;

        private TypeDefinition _typeDefinition;

        // Cached values
        private unsafe volatile byte* _namePointer;
        private int _nameLength;
        private unsafe volatile byte* _namespacePointer;
        private int _namespaceLength;
        private TypeDesc[] _genericParameters;
        private MetadataType _baseType;
        private int _hashcode;

        internal EcmaType(EcmaModule module, TypeDefinitionHandle handle)
        {
            _module = module;
            _handle = handle;

            _typeDefinition = module.MetadataReader.GetTypeDefinition(handle);

            _baseType = this; // Not yet initialized flag
        }

        public override int GetHashCode()
        {
            if (_hashcode != 0)
                return _hashcode;
            return InitializeHashCode();
        }

        private int InitializeHashCode()
        {
            int hashCode = VersionResilientHashCode.NameHashCode(Namespace, Name);

            DefType containingType = ContainingType;
            if (containingType != null)
            {
                hashCode = VersionResilientHashCode.NestedTypeHashCode(containingType.GetHashCode(), hashCode);
            }

            return _hashcode = hashCode;
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
                _genericParameters = EmptyTypes;
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

        public override EcmaModule Module
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
                ThrowHelper.ThrowTypeLoadException(GetNamespace(), GetName(), Module);
            }
            _baseType = type;
            return type;
        }

        public override MetadataType BaseType
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

                        if (isValueType &&
                            stringComparer.Equals(nameHandle, "InlineArrayAttribute") &&
                            stringComparer.Equals(namespaceHandle, "System.Runtime.CompilerServices"))
                        {
                            flags |= TypeFlags.IsInlineArray;
                        }
                    }
                }
            }

            return flags;
        }

        private unsafe ReadOnlySpan<byte> InitializeName()
        {
            StringHandle handle = _typeDefinition.Name;
            _nameLength = MetadataReader.GetStringBytes(handle).Length;
            _namePointer = MetadataReader.MetadataPointer + MetadataReader.GetHeapMetadataOffset(HeapIndex.String) + MetadataReader.GetHeapOffset(handle);
            return new ReadOnlySpan<byte>(_namePointer, _nameLength);
        }

        public override unsafe ReadOnlySpan<byte> Name
        {
            get
            {
                byte* namePointer = _namePointer;
                if (namePointer != null)
                {
                    return new ReadOnlySpan<byte>(namePointer, _nameLength);
                }
                return InitializeName();
            }
        }

        private unsafe ReadOnlySpan<byte> InitializeNamespace()
        {
            StringHandle handle = _typeDefinition.Namespace;
            _namespaceLength = MetadataReader.GetStringBytes(handle).Length;
            _namespacePointer = MetadataReader.MetadataPointer + MetadataReader.GetHeapMetadataOffset(HeapIndex.String) + MetadataReader.GetHeapOffset(handle);
            return new ReadOnlySpan<byte>(_namespacePointer, _namespaceLength);
        }

        public override unsafe ReadOnlySpan<byte> Namespace
        {
            get
            {
                byte* namespacePointer = _namespacePointer;
                if (namespacePointer != null)
                {
                    return new ReadOnlySpan<byte>(namespacePointer, _namespaceLength);
                }
                return InitializeNamespace();
            }
        }

        public override IEnumerable<EcmaMethod> GetMethods()
        {
            foreach (var handle in _typeDefinition.GetMethods())
            {
                yield return _module.GetMethod(handle, this);
            }
        }

        public override IEnumerable<EcmaMethod> GetVirtualMethods()
        {
            MetadataReader reader = _module.MetadataReader;
            foreach (var handle in _typeDefinition.GetMethods())
            {
                MethodDefinition methodDef = reader.GetMethodDefinition(handle);
                if ((methodDef.Attributes & MethodAttributes.Virtual) != 0)
                    yield return _module.GetMethod(handle, this);
            }
        }

        /// <summary>
        /// Gets a named method on the type. This method only looks at methods defined
        /// in type's metadata. The <paramref name="signature"/> parameter can be null.
        /// If signature is not specified and there are multiple matches, the first one
        /// is returned. Returns null if method not found.
        /// </summary>
        public new EcmaMethod GetMethod(ReadOnlySpan<byte> name, MethodSignature signature)
        {
            return GetMethod(name, signature, default(Instantiation));
        }

        public override EcmaMethod GetMethod(ReadOnlySpan<byte> name, MethodSignature signature, Instantiation substitution)
        {
            var metadataReader = this.MetadataReader;

            foreach (var handle in _typeDefinition.GetMethods())
            {
                if (metadataReader.StringEquals(metadataReader.GetMethodDefinition(handle).Name, name))
                {
                    var method = _module.GetMethod(handle, this);
                    if (signature == null || signature.Equals(method.Signature.ApplySubstitution(substitution)))
                        return method;
                }
            }

            return null;
        }

        public override EcmaMethod GetMethodWithEquivalentSignature(ReadOnlySpan<byte> name, MethodSignature signature, Instantiation substitution)
        {
            var metadataReader = this.MetadataReader;

            foreach (var handle in _typeDefinition.GetMethods())
            {
                if (metadataReader.StringEquals(metadataReader.GetMethodDefinition(handle).Name, name))
                {
                    var method = _module.GetMethod(handle, this);
                    if (signature == null || signature.EquivalentTo(method.Signature.ApplySubstitution(substitution)))
                        return method;
                }
            }

            return null;
        }

        public override EcmaMethod GetStaticConstructor()
        {
            var metadataReader = this.MetadataReader;
            var stringComparer = metadataReader.StringComparer;

            foreach (var handle in _typeDefinition.GetMethods())
            {
                var methodDefinition = metadataReader.GetMethodDefinition(handle);
                if (methodDefinition.Attributes.IsRuntimeSpecialName() &&
                    stringComparer.Equals(methodDefinition.Name, ".cctor"))
                {
                    var method = _module.GetMethod(handle, this);
                    return method;
                }
            }

            return null;
        }

        public override EcmaMethod GetDefaultConstructor()
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
                    var method = _module.GetMethod(handle, this);
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
            MethodDesc decl = objectType.GetMethod("Finalize"u8, null);

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

        public override IEnumerable<EcmaField> GetFields()
        {
            foreach (var handle in _typeDefinition.GetFields())
            {
                var field = _module.GetField(handle, this);
                yield return field;
            }
        }

        public override TypeDesc UnderlyingType
        {
            get
            {
                if (!IsEnum)
                    return this;

                foreach (var handle in _typeDefinition.GetFields())
                {
                    var fieldInfo = _module.GetField(handle, this);
                    if (!fieldInfo.IsStatic)
                        return fieldInfo.FieldType;
                }

                return base.UnderlyingType; // Use the base implementation to get consistent error behavior
            }
        }

        public override EcmaField GetField(ReadOnlySpan<byte> name)
        {
            var metadataReader = this.MetadataReader;

            foreach (var handle in _typeDefinition.GetFields())
            {
                if (metadataReader.StringEquals(metadataReader.GetFieldDefinition(handle).Name, name))
                {
                    var field = _module.GetField(handle, this);
                    return field;
                }
            }

            return null;
        }

        public override IEnumerable<EcmaType> GetNestedTypes()
        {
            foreach (var handle in _typeDefinition.GetNestedTypes())
            {
                yield return (EcmaType)_module.GetObject(handle);
            }
        }

        public override EcmaType GetNestedType(string name)
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
                    return _module.GetType(handle);
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

        public override EcmaType ContainingType
        {
            get
            {
                if (!_typeDefinition.Attributes.IsNested())
                    return null;

                var handle = _typeDefinition.GetDeclaringType();
                return _module.GetType(handle);
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
            int inlineArrayLength = 0;

            if (IsInlineArray)
            {
                var attr = MetadataReader.GetCustomAttribute(MetadataReader.GetCustomAttributeHandle(_typeDefinition.GetCustomAttributes(),
                    "System.Runtime.CompilerServices", "InlineArrayAttribute"));

                var value = attr.DecodeValue(new CustomAttributeTypeProvider(_module)).FixedArguments[0].Value;

                inlineArrayLength = value is int intValue ? intValue : 0;
            }

            MetadataLayoutKind layoutKind = MetadataLayoutKind.Auto;
            if ((Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout)
            {
                layoutKind = MetadataLayoutKind.Sequential;
            }
            else if ((Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout)
            {
                layoutKind = MetadataLayoutKind.Explicit;
            }
            else if ((Attributes & TypeAttributes.LayoutMask) == TypeAttributesExtendedLayout)
            {
                var attrHandle = MetadataReader.GetCustomAttributeHandle(_typeDefinition.GetCustomAttributes(),
                    "System.Runtime.InteropServices", "ExtendedLayoutAttribute");

                if (attrHandle.IsNil)
                {
                    ThrowHelper.ThrowTypeLoadException(this);
                }

                var attr = MetadataReader.GetCustomAttribute(attrHandle);

                var attrValue = attr.DecodeValue(new CustomAttributeTypeProvider(_module));

                if (attrValue.FixedArguments is not [{ Value: int kind }])
                {
                    ThrowHelper.ThrowTypeLoadException(this);
                    return default;
                }

                switch (kind)
                {
                    case 0:
                        layoutKind = MetadataLayoutKind.CStruct;
                        break;
                    default:
                        ThrowHelper.ThrowTypeLoadException(this);
                        return default; // Invalid kind value
                }
            }

            return new ClassLayoutMetadata
            {
                Kind = layoutKind,
                PackingSize = layout.PackingSize,
                Size = layout.Size,
                InlineArrayLength = inlineArrayLength,
            };
        }

        public override bool IsExplicitLayout
        {
            get
            {
                return (_typeDefinition.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;
            }
        }

        public override bool IsSequentialLayout
        {
            get
            {
                return (_typeDefinition.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout;
            }
        }

        public override bool IsExtendedLayout
        {
            get
            {
                return (_typeDefinition.Attributes & TypeAttributes.LayoutMask) == TypeAttributesExtendedLayout;
            }
        }

        public override bool IsAutoLayout
        {
            get
            {
                return (_typeDefinition.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout;
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
