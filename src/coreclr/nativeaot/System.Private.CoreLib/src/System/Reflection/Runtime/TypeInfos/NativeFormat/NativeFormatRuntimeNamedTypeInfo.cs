// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.General;
using System.Text;

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.TypeInfos.NativeFormat
{
    internal sealed partial class NativeFormatRuntimeNamedTypeInfo : RuntimeNamedTypeInfo
    {
        private NativeFormatRuntimeNamedTypeInfo(MetadataReader reader, TypeDefinitionHandle typeDefinitionHandle, RuntimeTypeHandle typeHandle) :
            base(typeHandle)
        {
            _reader = reader;
            _typeDefinitionHandle = typeDefinitionHandle;
            _typeDefinition = _typeDefinitionHandle.GetTypeDefinition(reader);
        }

        public sealed override Assembly Assembly
        {
            get
            {
                // If an assembly is split across multiple metadata blobs then the defining scope may
                // not be the canonical scope representing the assembly. We need to look up the assembly
                // by name to ensure we get the right one.

                ScopeDefinitionHandle scopeDefinitionHandle = NamespaceDefinition.Scope;
                RuntimeAssemblyName runtimeAssemblyName = scopeDefinitionHandle.ToRuntimeAssemblyName(_reader);

                return RuntimeAssemblyInfo.GetRuntimeAssembly(runtimeAssemblyName);
            }
        }

        public sealed override bool IsByRefLike
        {
            get
            {
                // If we have a type handle, ask the runtime
                RuntimeTypeHandle typeHandle = InternalTypeHandleIfAvailable;
                if (!typeHandle.IsNull)
                    return Internal.Runtime.Augments.RuntimeAugments.IsByRefLike(typeHandle);

                // Otherwise fall back to attributes
                foreach (CustomAttributeHandle cah in _typeDefinition.CustomAttributes)
                {
                    if (cah.IsCustomAttributeOfType(_reader, "System.Runtime.CompilerServices", "IsByRefLikeAttribute"))
                        return true;
                }
                return false;
            }
        }

        protected sealed override Guid? ComputeGuidFromCustomAttributes()
        {
            //
            // Look for a [Guid] attribute. If found, return that.
            //
            foreach (CustomAttributeHandle cah in _typeDefinition.CustomAttributes)
            {
                // Match by name, as CoreCLR does, without resolving the constructor signature.
                if (cah.IsCustomAttributeOfType(_reader, "System.Runtime.InteropServices", "GuidAttribute"))
                {
                    ReadOnlySpan<byte> blob = cah.GetCustomAttribute(_reader).Blob.AsSpan();
                    const int GuidLength = 36;
                    // One-byte string length, GUID text, and a two-byte named-argument count.
                    if (blob.Length != sizeof(byte) + GuidLength + sizeof(ushort))
                        continue;

                    // Parse a 'Guid' directly from the encoded UTF8 buffer, instead of round-tripping through a 'string'
                    return Guid.Parse(blob.Slice(sizeof(byte), GuidLength));
                }
            }
            return null;
        }

        protected sealed override void GetPackSizeAndSize(out int packSize, out int size)
        {
            packSize = _typeDefinition.PackingSize;
            size = unchecked((int)(_typeDefinition.Size));
        }

        public sealed override bool IsGenericTypeDefinition
        {
            get
            {
                return _typeDefinition.GenericParameters.GetEnumerator().MoveNext();
            }
        }

        public sealed override string Namespace
        {
            get
            {
                string ns = _lazyNamespace;
                if (ns is null)
                {
                    ns = NamespaceDefinition.Name.GetStringOrNull(_reader).EscapeTypeNameIdentifier() ?? string.Empty;
                    _lazyNamespace = ns;
                }
                return ns.Length == 0 ? null : ns;
            }
        }

        public sealed override Type GetGenericTypeDefinition()
        {
            if (_typeDefinition.GenericParameters.GetEnumerator().MoveNext())
                return this.ToType();
            return base.GetGenericTypeDefinition();
        }

        public sealed override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        public sealed override string ToString()
        {
            StringBuilder? sb = null;

            foreach (GenericParameterHandle genericParameterHandle in _typeDefinition.GenericParameters)
            {
                if (sb == null)
                {
                    sb = new StringBuilder(FullName);
                    sb.Append('[');
                }
                else
                {
                    sb.Append(',');
                }

                sb.Append(genericParameterHandle.GetGenericParameter(_reader).Name.GetString(_reader));
            }

            if (sb == null)
            {
                return FullName;
            }
            else
            {
                return sb.Append(']').ToString();
            }
        }

        public sealed override TypeAttributes Attributes => _typeDefinition.Flags;

        public sealed override int GetHashCode()
        {
            return _typeDefinitionHandle.GetHashCode();
        }

        internal sealed override RuntimeTypeInfo InternalDeclaringType
        {
            get
            {
                RuntimeTypeInfo? declaringType = null;
                Handle namespaceOrEnclosingType = _typeDefinition.NamespaceOrEnclosingType;
                if (namespaceOrEnclosingType.HandleType == HandleType.TypeDefinition)
                {
                    declaringType = namespaceOrEnclosingType.ToTypeDefinitionHandle(_reader).ResolveTypeDefinition(_reader);
                }
                return declaringType;
            }
        }

        internal sealed override string InternalFullNameOfAssembly
        {
            get
            {
                ScopeDefinitionHandle scopeDefinitionHandle = NamespaceDefinition.Scope;
                return scopeDefinitionHandle.ToRuntimeAssemblyName(_reader).FullName;
            }
        }

        public sealed override string Name
        {
            get
            {
                ConstantStringValueHandle nameHandle = _typeDefinition.Name;
                string name = nameHandle.GetString(_reader);

                return name.EscapeTypeNameIdentifier();
            }
        }

        internal sealed override MetadataReader GetMetadataReader() => _reader;

        internal sealed override CustomAttributeHandleCollection GetCustomAttributeHandles() => _typeDefinition.CustomAttributes;

        public sealed override Type? GetNullableUnderlyingType()
        {
            return (this.ToType() == typeof(Nullable<>)) ? RuntimeGenericTypeParameters[0].ToType() : null;
        }

        internal sealed override void GetEnumValuesAndNames(out string[] unsortedNames, out object[] unsortedValues, out bool isFlags)
        {
            Debug.Assert(IsActualEnum);

            // Count the number of static fields. The single instance field may or may not have metadata,
            // so using `_typeDefinition.Fields.Count - 1` is not reliable.
            int staticFieldCount = 0;
            foreach (FieldHandle fieldHandle in _typeDefinition.Fields)
            {
                Field field = fieldHandle.GetField(_reader);
                if (0 != (field.Flags & FieldAttributes.Static))
                    staticFieldCount++;
            }

            unsortedNames = new string[staticFieldCount];
            unsortedValues = new object[staticFieldCount];

            int i = 0;
            foreach (FieldHandle fieldHandle in _typeDefinition.Fields)
            {
                Field field = fieldHandle.GetField(_reader);
                if (0 != (field.Flags & FieldAttributes.Static))
                {
                    unsortedNames[i] = field.Name.GetString(_reader);
                    Constant constant = field.DefaultValue.GetConstant(_reader);
                    BlobReader blob = new BlobReader(constant.Blob.AsSpan());
                    unsortedValues[i] = constant.TypeCode switch
                    {
                        ConstantTypeCode.SByte or ConstantTypeCode.Byte => (object)blob.ReadByte(),
                        ConstantTypeCode.Int16 or ConstantTypeCode.UInt16 => blob.ReadUInt16(),
                        ConstantTypeCode.Int32 or ConstantTypeCode.UInt32 => blob.ReadUInt32(),
                        ConstantTypeCode.Int64 or ConstantTypeCode.UInt64 => blob.ReadUInt64(),
                        _ => throw new UnreachableException(),
                    };
                    i++;
                }
            }

            isFlags = false;
            foreach (CustomAttributeHandle cah in _typeDefinition.CustomAttributes)
            {
                if (cah.IsCustomAttributeOfType(_reader, "System", "FlagsAttribute"))
                {
                    isFlags = true;
                    break;
                }
            }
        }

        internal sealed override RuntimeTypeInfo[] RuntimeGenericTypeParameters
        {
            get
            {
                ArrayBuilder<RuntimeTypeInfo> genericTypeParameters = new ArrayBuilder<RuntimeTypeInfo>(_typeDefinition.GenericParameters.Count);

                foreach (GenericParameterHandle genericParameterHandle in _typeDefinition.GenericParameters)
                {
                    RuntimeTypeInfo genericParameterType = NativeFormat.NativeFormatRuntimeGenericParameterTypeInfoForTypes.GetRuntimeGenericParameterTypeInfoForTypes(this, genericParameterHandle);
                    genericTypeParameters.Add(genericParameterType);
                }

                return genericTypeParameters.ToArray();
            }
        }

        //
        // Returns the base type as a typeDef, Ref, or Spec. Default behavior is to QTypeDefRefOrSpec.Null, which causes BaseType to return null.
        //
        internal sealed override QTypeDefRefOrSpec TypeRefDefOrSpecForBaseType
        {
            get
            {
                Handle baseType = _typeDefinition.BaseType;
                if (baseType.IsNil)
                    return QTypeDefRefOrSpec.Null;
                return new QTypeDefRefOrSpec(_reader, baseType);
            }
        }

        //
        // Returns the *directly implemented* interfaces as typedefs, specs or refs. ImplementedInterfaces will take care of the transitive closure and
        // insertion of the TypeContext.
        //
        internal sealed override QTypeDefRefOrSpec[] TypeRefDefOrSpecsForDirectlyImplementedInterfaces
        {
            get
            {
                ArrayBuilder<QTypeDefRefOrSpec> directlyImplementedInterfaces = new ArrayBuilder<QTypeDefRefOrSpec>(_typeDefinition.Interfaces.Count);
                foreach (Handle ifcHandle in _typeDefinition.Interfaces)
                    directlyImplementedInterfaces.Add(new QTypeDefRefOrSpec(_reader, ifcHandle));
                return directlyImplementedInterfaces.ToArray();
            }
        }

        internal MetadataReader Reader
        {
            get
            {
                return _reader;
            }
        }

        internal TypeDefinitionHandle TypeDefinitionHandle
        {
            get
            {
                return _typeDefinitionHandle;
            }
        }

        internal EventHandleCollection DeclaredEventHandles
        {
            get
            {
                return _typeDefinition.Events;
            }
        }

        internal FieldHandleCollection DeclaredFieldHandles
        {
            get
            {
                return _typeDefinition.Fields;
            }
        }

        internal MethodHandleCollection DeclaredMethodAndConstructorHandles
        {
            get
            {
                return _typeDefinition.Methods;
            }
        }

        internal PropertyHandleCollection DeclaredPropertyHandles
        {
            get
            {
                return _typeDefinition.Properties;
            }
        }

        public bool Equals(NativeFormatRuntimeNamedTypeInfo? other)
        {
            // RuntimeTypeInfo.Equals(object) is the one that encapsulates our unification strategy so defer to him.
            object? otherAsObject = other;
            return base.Equals(otherAsObject);
        }

        internal sealed override QTypeDefRefOrSpec TypeDefinitionQHandle
        {
            get
            {
                return new QTypeDefRefOrSpec(_reader, _typeDefinitionHandle, true);
            }
        }

        private readonly MetadataReader _reader;
        private readonly TypeDefinitionHandle _typeDefinitionHandle;
        private readonly TypeDefinition _typeDefinition;

        private NamespaceDefinition NamespaceDefinition
        {
            get
            {
                Handle namespaceOrEnclosingType = _typeDefinition.NamespaceOrEnclosingType;
                while (namespaceOrEnclosingType.HandleType == HandleType.TypeDefinition)
                {
                    TypeDefinition typeDefinition = namespaceOrEnclosingType.ToTypeDefinitionHandle(_reader).GetTypeDefinition(_reader);
                    namespaceOrEnclosingType = typeDefinition.NamespaceOrEnclosingType;
                }

                return namespaceOrEnclosingType.ToNamespaceDefinitionHandle(_reader).GetNamespaceDefinition(_reader);
            }
        }

        private volatile string _lazyNamespace;
    }
}
