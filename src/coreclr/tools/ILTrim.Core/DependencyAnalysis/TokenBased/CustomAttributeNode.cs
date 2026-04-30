// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the Custom Attribute metadata table.
    /// </summary>
    public sealed class CustomAttributeNode : TokenBasedNode
    {
        public CustomAttributeNode(EcmaModule module, CustomAttributeHandle handle)
            : base(module, handle)
        {
        }

        private CustomAttributeHandle Handle => (CustomAttributeHandle)_handle;

        public static void AddDependenciesDueToCustomAttributes(ref DependencyList dependencies, NodeFactory factory, EcmaModule module, CustomAttributeHandleCollection handles)
        {
            foreach (CustomAttributeHandle customAttribute in handles)
            {
                if (factory.Settings.StripSecurity && IsCustomAttributeForSecurity(module, customAttribute))
                    continue;

                dependencies ??= new DependencyList();
                dependencies.Add(factory.CustomAttribute(module, customAttribute), "Custom attribute");
            }
        }

        public static bool IsCustomAttributeForSecurity(EcmaModule module, CustomAttributeHandle handle)
        {
            MetadataReader metadataReader = module.MetadataReader;
            CustomAttribute ca = metadataReader.GetCustomAttribute(handle);
            if (metadataReader.GetAttributeNamespaceAndName(handle, out StringHandle namespaceHandle, out StringHandle nameHandle)
                && metadataReader.StringEquals(namespaceHandle, "System.Security"u8))
            {
                return metadataReader.StringEquals(nameHandle, "SecurityCriticalAttribute"u8)
                    || metadataReader.StringEquals(nameHandle, "SecuritySafeCriticalAttribute"u8)
                    || metadataReader.StringEquals(nameHandle, "SuppressUnmanagedCodeSecurityAttribute"u8)
                    || metadataReader.StringEquals(nameHandle, "DynamicSecurityMethodAttribute"u8)
                    || metadataReader.StringEquals(nameHandle, "UnverifiableCodeAttribute"u8)
                    || metadataReader.StringEquals(nameHandle, "AllowPartiallyTrustedCallersAttribute"u8)
                    || metadataReader.StringEquals(nameHandle, "SecurityTransparentAttribute"u8)
                    || metadataReader.StringEquals(nameHandle, "SecurityRulesAttribute"u8);
            }

            return false;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            CustomAttribute customAttribute = _module.MetadataReader.GetCustomAttribute(Handle);

            // We decided not to report parent as a dependency because we don't expect custom attributes to be needed outside of their parent references

            if (!customAttribute.Constructor.IsNil)
                yield return new DependencyListEntry(factory.GetNodeForMethodToken(_module, customAttribute.Constructor), "Custom attribute constructor");

            // Parse the custom attribute value blob and add dependencies from it
            CustomAttributeValue<TypeDesc> decodedValue;
            try
            {
                decodedValue = customAttribute.DecodeValue(new CustomAttributeTypeProvider(_module));
            }
            catch (Exception)
            {
                // If decoding fails (bad metadata, unresolvable types, etc.), skip blob analysis.
                yield break;
            }

            foreach (var entry in GetDependenciesFromCustomAttributeBlob(factory, decodedValue))
                yield return entry;
        }

        private IEnumerable<DependencyListEntry> GetDependenciesFromCustomAttributeBlob(NodeFactory factory, CustomAttributeValue<TypeDesc> value)
        {
            foreach (CustomAttributeTypedArgument<TypeDesc> fixedArg in value.FixedArguments)
            {
                foreach (var entry in GetDependenciesFromCustomAttributeArgument(factory, fixedArg.Type, fixedArg.Value))
                    yield return entry;
            }

            foreach (CustomAttributeNamedArgument<TypeDesc> namedArg in value.NamedArguments)
            {
                // Report the type that declares the field/property
                // (The constructor's declaring type was already reported above.)
                if (namedArg.Kind == CustomAttributeNamedArgumentKind.Property)
                {
                    // We need the setter of the property to be kept
                    MethodDesc constructor = _module.GetMethod(
                        _module.MetadataReader.GetCustomAttribute(Handle).Constructor);
                    if (constructor is not null)
                    {
                        foreach (var entry in GetDependenciesFromPropertySetter(factory, constructor.OwningType, namedArg.Name))
                            yield return entry;
                    }
                }
                else if (namedArg.Kind == CustomAttributeNamedArgumentKind.Field)
                {
                    MethodDesc constructor = _module.GetMethod(
                        _module.MetadataReader.GetCustomAttribute(Handle).Constructor);
                    if (constructor is not null)
                    {
                        foreach (var entry in GetDependenciesFromField(factory, constructor.OwningType, namedArg.Name))
                            yield return entry;
                    }
                }

                foreach (var entry in GetDependenciesFromCustomAttributeArgument(factory, namedArg.Type, namedArg.Value))
                    yield return entry;
            }
        }

        private IEnumerable<DependencyListEntry> GetDependenciesFromCustomAttributeArgument(NodeFactory factory, TypeDesc type, object value)
        {
            if (type is null)
                yield break;

            // Report the type itself (e.g. enum types that need to be kept for boxing)
            object reflectedType = factory.ReflectedType(type);
            if (reflectedType is DependencyAnalysisFramework.DependencyNodeCore<NodeFactory> typeNode)
                yield return new DependencyListEntry(typeNode, "Custom attribute blob");

            if (type.UnderlyingType.IsPrimitive || type.IsString || value is null)
                yield break;

            if (type.IsSzArray)
            {
                TypeDesc elementType = ((ArrayType)type).ElementType;
                if (elementType.UnderlyingType.IsPrimitive || elementType.IsString)
                    yield break;

                if (value is ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> arrayElements)
                {
                    foreach (CustomAttributeTypedArgument<TypeDesc> element in arrayElements)
                    {
                        foreach (var entry in GetDependenciesFromCustomAttributeArgument(factory, element.Type, element.Value))
                            yield return entry;
                    }
                }

                yield break;
            }

            // typeof() - the value is a TypeDesc
            if (value is TypeDesc typeofType)
            {
                object reflectedTypeofType = factory.ReflectedType(typeofType);
                if (reflectedTypeofType is DependencyAnalysisFramework.DependencyNodeCore<NodeFactory> typeofNode)
                    yield return new DependencyListEntry(typeofNode, "Custom attribute blob");
            }
        }

        private static IEnumerable<DependencyListEntry> GetDependenciesFromPropertySetter(NodeFactory factory, TypeDesc attributeType, string propertyName)
        {
            if (attributeType is not EcmaType ecmaType)
                yield break;

            MetadataReader reader = ecmaType.MetadataReader;
            TypeDefinition typeDef = reader.GetTypeDefinition(ecmaType.Handle);

            foreach (PropertyDefinitionHandle propDefHandle in typeDef.GetProperties())
            {
                PropertyDefinition propDef = reader.GetPropertyDefinition(propDefHandle);
                if (reader.StringComparer.Equals(propDef.Name, propertyName))
                {
                    PropertyAccessors accessors = propDef.GetAccessors();
                    if (!accessors.Setter.IsNil)
                    {
                        object reflectedMethod = factory.ReflectedMethod(ecmaType.Module.GetMethod(accessors.Setter));
                        if (reflectedMethod is DependencyAnalysisFramework.DependencyNodeCore<NodeFactory> methodNode)
                            yield return new DependencyListEntry(methodNode, "Custom attribute blob");
                    }

                    yield break;
                }
            }

            // Check base type
            TypeDesc baseType = attributeType.BaseType;
            if (baseType is not null)
            {
                foreach (var entry in GetDependenciesFromPropertySetter(factory, baseType, propertyName))
                    yield return entry;
            }
        }

        private static IEnumerable<DependencyListEntry> GetDependenciesFromField(NodeFactory factory, TypeDesc attributeType, string fieldName)
        {
            FieldDesc field = attributeType.GetField(Encoding.UTF8.GetBytes(fieldName));
            if (field is not null)
            {
                object reflectedField = factory.ReflectedField(field);
                if (reflectedField is DependencyAnalysisFramework.DependencyNodeCore<NodeFactory> fieldNode)
                    yield return new DependencyListEntry(fieldNode, "Custom attribute blob");

                yield break;
            }

            // Check base type
            TypeDesc baseType = attributeType.BaseType;
            if (baseType is not null)
            {
                foreach (var entry in GetDependenciesFromField(factory, baseType, fieldName))
                    yield return entry;
            }
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            CustomAttribute customAttribute = reader.GetCustomAttribute(Handle);

            var builder = writeContext.MetadataBuilder;

            // Resolve type name strings in the blob to their definitions.
            // Trimming may drop type forwarders, so we re-encode type names
            // to point to the assembly where the type is actually defined.
            byte[] valueBlob = RewriteCustomAttributeBlob(customAttribute);

            return builder.AddCustomAttribute(writeContext.TokenMap.MapToken(customAttribute.Parent),
                writeContext.TokenMap.MapToken(customAttribute.Constructor),
                builder.GetOrAddBlob(valueBlob));
        }

        private byte[] RewriteCustomAttributeBlob(CustomAttribute customAttribute)
        {
            byte[] originalBlob = _module.MetadataReader.GetBlobBytes(customAttribute.Value);

            CustomAttributeValue<TypeDesc> decodedValue;
            try
            {
                decodedValue = customAttribute.DecodeValue(new CustomAttributeTypeProvider(_module));
            }
            catch (Exception)
            {
                return originalBlob;
            }

            // Check if any type name string needs rewriting
            var formatter = new CustomAttributeTypeNameFormatter();
            bool needsRewrite = false;

            CheckArgumentsForRewrite(decodedValue, formatter, originalBlob, ref needsRewrite);

            if (!needsRewrite)
                return originalBlob;

            // Re-encode the entire blob
            return EncodeCustomAttributeBlob(customAttribute, decodedValue, formatter);
        }

        private void CheckArgumentsForRewrite(
            CustomAttributeValue<TypeDesc> decodedValue,
            CustomAttributeTypeNameFormatter formatter,
            byte[] originalBlob,
            ref bool needsRewrite)
        {
            foreach (CustomAttributeTypedArgument<TypeDesc> fixedArg in decodedValue.FixedArguments)
            {
                CheckArgumentValueForRewrite(fixedArg.Type, fixedArg.Value, formatter, ref needsRewrite);
                if (needsRewrite) return;
            }

            foreach (CustomAttributeNamedArgument<TypeDesc> namedArg in decodedValue.NamedArguments)
            {
                CheckArgumentValueForRewrite(namedArg.Type, namedArg.Value, formatter, ref needsRewrite);
                if (needsRewrite) return;
            }
        }

        private void CheckArgumentValueForRewrite(TypeDesc type, object value, CustomAttributeTypeNameFormatter formatter, ref bool needsRewrite)
        {
            if (value is null || type is null)
                return;

            if (value is TypeDesc typeofType)
            {
                // Check if the formatted name differs from what was originally stored
                // Any typeof() with a type forwarder reference might need rewriting
                needsRewrite = true;
                return;
            }

            if (type.IsSzArray && value is ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> arrayElements)
            {
                TypeDesc elementType = ((ArrayType)type).ElementType;
                if (!elementType.UnderlyingType.IsPrimitive && !elementType.IsString)
                {
                    foreach (CustomAttributeTypedArgument<TypeDesc> element in arrayElements)
                    {
                        CheckArgumentValueForRewrite(element.Type, element.Value, formatter, ref needsRewrite);
                        if (needsRewrite) return;
                    }
                }
            }
        }

        private byte[] EncodeCustomAttributeBlob(
            CustomAttribute customAttribute,
            CustomAttributeValue<TypeDesc> decodedValue,
            CustomAttributeTypeNameFormatter formatter)
        {
            var blobBuilder = new BlobBuilder();

            // Prolog (0x0001)
            blobBuilder.WriteUInt16(1);

            // Resolve the constructor to get parameter types
            MethodDesc constructor;
            try
            {
                constructor = _module.GetMethod(customAttribute.Constructor);
            }
            catch (Exception)
            {
                return _module.MetadataReader.GetBlobBytes(customAttribute.Value);
            }

            // Write fixed arguments
            MethodSignature constructorSig = constructor.Signature;
            for (int i = 0; i < decodedValue.FixedArguments.Length; i++)
            {
                TypeDesc paramType = constructorSig[i];
                WriteArgumentValue(blobBuilder, paramType, decodedValue.FixedArguments[i].Type, decodedValue.FixedArguments[i].Value, formatter, isFixedArg: true);
            }

            // NumNamed
            blobBuilder.WriteUInt16((ushort)decodedValue.NamedArguments.Length);

            // Write named arguments
            foreach (CustomAttributeNamedArgument<TypeDesc> namedArg in decodedValue.NamedArguments)
            {
                // Field or property
                blobBuilder.WriteByte(namedArg.Kind == CustomAttributeNamedArgumentKind.Field ? (byte)0x53 : (byte)0x54);

                // Field/property type
                WriteFieldOrPropType(blobBuilder, namedArg.Type);

                // Name as SerString
                WriteSerString(blobBuilder, namedArg.Name);

                // Value
                WriteArgumentValue(blobBuilder, namedArg.Type, namedArg.Type, namedArg.Value, formatter, isFixedArg: false);
            }

            return blobBuilder.ToArray();
        }

        private void WriteFieldOrPropType(BlobBuilder builder, TypeDesc type)
        {
            if (type is null)
                return;

            // ECMA-335 II.23.3 - FieldOrPropType encoding
            if (type.IsEnum)
            {
                builder.WriteByte(0x55); // ELEMENT_TYPE_ENUM
                var formatter = new CustomAttributeTypeNameFormatter();
                WriteSerString(builder, formatter.FormatName(type, true));
                return;
            }

            if (type.IsSzArray)
            {
                builder.WriteByte(0x1D); // ELEMENT_TYPE_SZARRAY
                WriteFieldOrPropType(builder, ((ArrayType)type).ElementType);
                return;
            }

            switch (type.UnderlyingType.Category)
            {
                case TypeFlags.Boolean: builder.WriteByte(0x02); break;
                case TypeFlags.Char: builder.WriteByte(0x03); break;
                case TypeFlags.SByte: builder.WriteByte(0x04); break;
                case TypeFlags.Byte: builder.WriteByte(0x05); break;
                case TypeFlags.Int16: builder.WriteByte(0x06); break;
                case TypeFlags.UInt16: builder.WriteByte(0x07); break;
                case TypeFlags.Int32: builder.WriteByte(0x08); break;
                case TypeFlags.UInt32: builder.WriteByte(0x09); break;
                case TypeFlags.Int64: builder.WriteByte(0x0A); break;
                case TypeFlags.UInt64: builder.WriteByte(0x0B); break;
                case TypeFlags.Single: builder.WriteByte(0x0C); break;
                case TypeFlags.Double: builder.WriteByte(0x0D); break;
                default:
                    if (type.IsString)
                    {
                        builder.WriteByte(0x0E); // ELEMENT_TYPE_STRING
                    }
                    else if (IsSystemType(type))
                    {
                        builder.WriteByte(0x50); // TYPE
                    }
                    else if (type.IsObject)
                    {
                        builder.WriteByte(0x51); // TAGGED_OBJECT / boxed
                    }
                    break;
            }
        }

        private void WriteArgumentValue(BlobBuilder builder, TypeDesc declaredType, TypeDesc actualType, object value, CustomAttributeTypeNameFormatter formatter, bool isFixedArg)
        {
            if (declaredType is null)
                return;

            // Boxed (object-typed) arguments: write field/prop type tag then the value
            if (declaredType.IsObject && !isFixedArg)
            {
                WriteBoxedArgumentValue(builder, actualType, value, formatter);
                return;
            }

            if (declaredType.IsObject && isFixedArg)
            {
                WriteBoxedArgumentValue(builder, actualType, value, formatter);
                return;
            }

            if (value is null)
            {
                if (declaredType.IsString)
                {
                    // Null string
                    builder.WriteByte(0xFF);
                    return;
                }

                if (IsSystemType(declaredType))
                {
                    // Null type
                    builder.WriteByte(0xFF);
                    return;
                }

                if (declaredType.IsSzArray)
                {
                    // Null array = 0xFFFFFFFF
                    builder.WriteUInt32(0xFFFFFFFF);
                    return;
                }
            }

            // Enum values
            if (declaredType.IsEnum)
            {
                WritePrimitiveValue(builder, declaredType.UnderlyingType, value);
                return;
            }

            // Primitive types
            if (declaredType.UnderlyingType.IsPrimitive)
            {
                WritePrimitiveValue(builder, declaredType, value);
                return;
            }

            // String
            if (declaredType.IsString)
            {
                WriteSerString(builder, (string)value);
                return;
            }

            // System.Type (typeof)
            if (IsSystemType(declaredType))
            {
                if (value is TypeDesc typeofType)
                {
                    string typeName = formatter.FormatName(typeofType, true);
                    WriteSerString(builder, typeName);
                }
                else
                {
                    builder.WriteByte(0xFF);
                }
                return;
            }

            // SZArray
            if (declaredType.IsSzArray)
            {
                if (value is ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> arrayElements)
                {
                    builder.WriteUInt32((uint)arrayElements.Length);
                    TypeDesc elementType = ((ArrayType)declaredType).ElementType;
                    foreach (CustomAttributeTypedArgument<TypeDesc> element in arrayElements)
                    {
                        WriteArgumentValue(builder, elementType, element.Type, element.Value, formatter, isFixedArg: true);
                    }
                }
                else
                {
                    builder.WriteUInt32(0xFFFFFFFF);
                }
                return;
            }
        }

        private void WriteBoxedArgumentValue(BlobBuilder builder, TypeDesc actualType, object value, CustomAttributeTypeNameFormatter formatter)
        {
            if (actualType is null || value is null)
            {
                // Null boxed value - this should not normally happen but handle gracefully
                builder.WriteByte(0x0E); // string type tag
                builder.WriteByte(0xFF); // null
                return;
            }

            WriteFieldOrPropType(builder, actualType);
            WriteArgumentValue(builder, actualType, actualType, value, formatter, isFixedArg: true);
        }

        private static void WritePrimitiveValue(BlobBuilder builder, TypeDesc type, object value)
        {
            switch (type.Category)
            {
                case TypeFlags.Boolean: builder.WriteBoolean((bool)value); break;
                case TypeFlags.Char: builder.WriteUInt16((ushort)(char)value); break;
                case TypeFlags.SByte: builder.WriteSByte((sbyte)value); break;
                case TypeFlags.Byte: builder.WriteByte((byte)value); break;
                case TypeFlags.Int16: builder.WriteInt16((short)value); break;
                case TypeFlags.UInt16: builder.WriteUInt16((ushort)value); break;
                case TypeFlags.Int32: builder.WriteInt32((int)value); break;
                case TypeFlags.UInt32: builder.WriteUInt32((uint)value); break;
                case TypeFlags.Int64: builder.WriteInt64((long)value); break;
                case TypeFlags.UInt64: builder.WriteUInt64((ulong)value); break;
                case TypeFlags.Single: builder.WriteSingle((float)value); break;
                case TypeFlags.Double: builder.WriteDouble((double)value); break;
            }
        }

        private static void WriteSerString(BlobBuilder builder, string value)
        {
            if (value is null)
            {
                builder.WriteByte(0xFF);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            builder.WriteCompressedInteger(bytes.Length);
            builder.WriteBytes(bytes);
        }

        private static bool IsSystemType(TypeDesc type)
        {
            if (type is not MetadataType mdType)
                return false;

            return mdType.Name.SequenceEqual("Type"u8)
                && mdType.Namespace.SequenceEqual("System"u8);
        }

        public override string ToString()
        {
            // TODO: Need to write a helper to get the name of the type
            return "Custom Attribute";
        }
    }
}
