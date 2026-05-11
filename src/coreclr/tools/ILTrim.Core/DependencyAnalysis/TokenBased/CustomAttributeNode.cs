// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using DependencyNode = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the Custom Attribute metadata table.
    /// </summary>
    public sealed class CustomAttributeNode : TokenBasedNode
    {
        // Set when dependency-time custom attribute decoding fails.
        // Rewrite then preserves the original blob for this node.
        private bool _isCorrupted;

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
            DependencyList dependencies = new DependencyList();

            CustomAttribute customAttribute = _module.MetadataReader.GetCustomAttribute(Handle);

            // We decided not to report parent as a dependency because we don't expect custom attributes to be needed outside of their parent references

            dependencies.Add(factory.GetNodeForMethodToken(_module, customAttribute.Constructor), "Custom attribute constructor");

            // Parse the custom attribute value blob and add dependencies from it
            CustomAttributeValue<TypeDesc> decodedValue;
            try
            {
                decodedValue = customAttribute.DecodeValue(new CustomAttributeTypeProvider(_module));
            }
            catch (Exception ex) when (ex is TypeSystemException or BadImageFormatException)
            {
                // Attribute ctor doesn't resolve, typeof() refers to something that can't be loaded,
                // attribute refers to a non-existing field, malformed blob, etc.
                _isCorrupted = true;
                return dependencies;
            }

            foreach (CustomAttributeTypedArgument<TypeDesc> fixedArg in decodedValue.FixedArguments)
            {
                GetDependenciesFromCustomAttributeArgument(dependencies, factory, fixedArg.Type, fixedArg.Value);
            }

            // Resolve the constructor once for all named arguments
            MethodDesc constructor = _module.TryGetMethod(customAttribute.Constructor);
            if (constructor is null)
                return dependencies;

            foreach (CustomAttributeNamedArgument<TypeDesc> namedArg in decodedValue.NamedArguments)
            {
                if (namedArg.Kind == CustomAttributeNamedArgumentKind.Property)
                    GetDependenciesFromPropertySetter(dependencies, factory, constructor.OwningType, namedArg.Name);
                else if (namedArg.Kind == CustomAttributeNamedArgumentKind.Field)
                    GetDependenciesFromField(dependencies, factory, constructor.OwningType, namedArg.Name);

                GetDependenciesFromCustomAttributeArgument(dependencies, factory, namedArg.Type, namedArg.Value);
            }

            return dependencies;
        }

        private static void GetDependenciesFromCustomAttributeArgument(DependencyList dependencies, NodeFactory factory, TypeDesc type, object value)
        {
            if (type is null)
                return;

            // Report the type itself (e.g. enum types that need to be kept for boxing)
            if (factory.ReflectedType(type) is DependencyNode typeNode)
                dependencies.Add(typeNode, "Custom attribute blob");

            if (type.UnderlyingType.IsPrimitive || type.IsString || value is null)
                return;

            if (type.IsSzArray)
            {
                TypeDesc elementType = ((ArrayType)type).ElementType;
                if (!elementType.UnderlyingType.IsPrimitive && !elementType.IsString
                    && value is ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> arrayElements)
                {
                    foreach (CustomAttributeTypedArgument<TypeDesc> element in arrayElements)
                    {
                        GetDependenciesFromCustomAttributeArgument(dependencies, factory, element.Type, element.Value);
                    }
                }
            }
            else if (value is TypeDesc typeofType)
            {
                // typeof() - the value is a TypeDesc
                if (factory.ReflectedType(typeofType) is DependencyNode typeofNode)
                    dependencies.Add(typeofNode, "Custom attribute blob");
            }
        }

        private static void GetDependenciesFromPropertySetter(DependencyList dependencies, NodeFactory factory, TypeDesc attributeType, string propertyName)
        {
            if (attributeType.GetTypeDefinition() is not EcmaType ecmaType)
                return;

            MetadataReader reader = ecmaType.MetadataReader;
            TypeDefinition typeDef = reader.GetTypeDefinition(ecmaType.Handle);

            foreach (PropertyDefinitionHandle propDefHandle in typeDef.GetProperties())
            {
                PropertyDefinition propDef = reader.GetPropertyDefinition(propDefHandle);
                if (reader.StringComparer.Equals(propDef.Name, propertyName))
                {
                    PropertyAccessors accessors = propDef.GetAccessors();
                    if (!accessors.Setter.IsNil
                        && factory.ReflectedMethod(ecmaType.Module.GetMethod(accessors.Setter)) is DependencyNode methodNode)
                    {
                        dependencies.Add(methodNode, "Custom attribute blob");
                    }

                    return;
                }
            }

            // Check base type
            TypeDesc baseType = attributeType.BaseType;
            if (baseType is not null)
                GetDependenciesFromPropertySetter(dependencies, factory, baseType, propertyName);
        }

        private static void GetDependenciesFromField(DependencyList dependencies, NodeFactory factory, TypeDesc attributeType, string fieldName)
        {
            FieldDesc field = attributeType.GetField(Encoding.UTF8.GetBytes(fieldName));
            if (field is not null)
            {
                if (factory.ReflectedField(field) is DependencyNode fieldNode)
                    dependencies.Add(fieldNode, "Custom attribute blob");
            }
            else
            {
                // Check base type
                TypeDesc baseType = attributeType.BaseType;
                if (baseType is not null)
                    GetDependenciesFromField(dependencies, factory, baseType, fieldName);
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
            BlobBuilder blobBuilder = writeContext.GetSharedBlobBuilder();
            RewriteCustomAttributeBlob(customAttribute, blobBuilder);

            return builder.AddCustomAttribute(writeContext.TokenMap.MapToken(customAttribute.Parent),
                writeContext.TokenMap.MapToken(customAttribute.Constructor),
                builder.GetOrAddBlob(blobBuilder));
        }

        private void RewriteCustomAttributeBlob(CustomAttribute customAttribute, BlobBuilder blobBuilder)
        {
            byte[] originalBlob = _module.MetadataReader.GetBlobBytes(customAttribute.Value);
            if (_isCorrupted)
            {
                blobBuilder.WriteBytes(originalBlob);
                return;
            }

            MethodDesc constructor = _module.TryGetMethod(customAttribute.Constructor);
            if (constructor is null)
            {
                blobBuilder.WriteBytes(originalBlob);
                return;
            }

            try
            {
                BlobReader valueReader = _module.MetadataReader.GetBlobReader(customAttribute.Value);
                var formatter = new CustomAttributeTypeNameFormatter();
                var typeProvider = new CustomAttributeTypeProvider(_module);

                // The dependency walk already decoded the blob successfully unless `_isCorrupted` is set.
                blobBuilder.WriteUInt16(valueReader.ReadUInt16());

                MethodSignature constructorSig = constructor.Signature;
                for (int i = 0; i < constructorSig.Length; i++)
                {
                    CopyFixedArgument(ref valueReader, blobBuilder, originalBlob, constructorSig[i], typeProvider, formatter);
                }

                ushort namedArgumentCount = valueReader.ReadUInt16();
                blobBuilder.WriteUInt16(namedArgumentCount);

                for (int i = 0; i < namedArgumentCount; i++)
                {
                    CustomAttributeNamedArgumentKind kind = (CustomAttributeNamedArgumentKind)valueReader.ReadByte();
                    blobBuilder.WriteByte((byte)kind);

                    CopyNamedArgumentType(ref valueReader, blobBuilder, originalBlob, typeProvider, formatter, out SerializationTypeCode valueTypeCode, out SerializationTypeCode elementValueTypeCode);
                    CopySerializedString(ref valueReader, blobBuilder, originalBlob, rewriteTypeName: false, typeProvider, formatter);
                    CopySerializedValue(ref valueReader, blobBuilder, originalBlob, typeProvider, formatter, valueTypeCode, elementValueTypeCode);
                }
            }
            catch (Exception ex) when (ex is TypeSystemException or BadImageFormatException)
            {
                blobBuilder.Clear();
                blobBuilder.WriteBytes(originalBlob);
            }
        }

        private static void CopyRawBytes(ref BlobReader reader, BlobBuilder blobBuilder, byte[] originalBlob, int byteCount)
        {
            int startOffset = reader.Offset;
            reader.Offset = startOffset + byteCount;
            blobBuilder.WriteBytes(originalBlob, startOffset, byteCount);
        }

        private static void CopySerializedString(ref BlobReader valueReader, BlobBuilder blobBuilder, byte[] originalBlob, bool rewriteTypeName, CustomAttributeTypeProvider typeProvider, CustomAttributeTypeNameFormatter formatter)
        {
            int startOffset = valueReader.Offset;
            string? original = valueReader.ReadSerializedString();
            int endOffset = valueReader.Offset;

            if (rewriteTypeName && original is not null)
            {
                TypeDesc resolved = typeProvider.GetTypeFromSerializedName(original);
                if (resolved is not null)
                {
                    string rewritten = formatter.FormatName(resolved, true);
                    if (rewritten != original)
                    {
                        blobBuilder.WriteSerializedString(rewritten);
                        return;
                    }
                }
            }

            blobBuilder.WriteBytes(originalBlob, startOffset, endOffset - startOffset);
        }

        private static void CopyFixedArgument(ref BlobReader valueReader, BlobBuilder blobBuilder, byte[] originalBlob, TypeDesc type, CustomAttributeTypeProvider typeProvider, CustomAttributeTypeNameFormatter formatter)
        {
            GetFixedArgumentTypeCodes(type, typeProvider, out SerializationTypeCode valueTypeCode, out SerializationTypeCode elementValueTypeCode);
            CopySerializedValue(ref valueReader, blobBuilder, originalBlob, typeProvider, formatter, valueTypeCode, elementValueTypeCode);
        }

        private static void GetFixedArgumentTypeCodes(TypeDesc type, CustomAttributeTypeProvider typeProvider, out SerializationTypeCode valueTypeCode, out SerializationTypeCode elementValueTypeCode)
        {
            elementValueTypeCode = default;

            if (type.UnderlyingType.IsPrimitive)
            {
                valueTypeCode = GetPrimitiveSerializationTypeCode(type);
            }
            else if (type.IsString)
            {
                valueTypeCode = SerializationTypeCode.String;
            }
            else if (IsSystemType(type))
            {
                valueTypeCode = SerializationTypeCode.Type;
            }
            else if (type.IsObject)
            {
                valueTypeCode = SerializationTypeCode.TaggedObject;
            }
            else if (type.IsSzArray)
            {
                valueTypeCode = SerializationTypeCode.SZArray;
                GetFixedArgumentTypeCodes(((ArrayType)type).ElementType, typeProvider, out elementValueTypeCode, out _);
            }
            else if (type.IsEnum)
            {
                valueTypeCode = (SerializationTypeCode)typeProvider.GetUnderlyingEnumType(type);
            }
            else
            {
                valueTypeCode = SerializationTypeCode.Invalid;
            }
        }

        private static SerializationTypeCode GetPrimitiveSerializationTypeCode(TypeDesc type)
        {
            switch (type.Category)
            {
                case TypeFlags.Boolean: return SerializationTypeCode.Boolean;
                case TypeFlags.Char: return SerializationTypeCode.Char;
                case TypeFlags.SByte: return SerializationTypeCode.SByte;
                case TypeFlags.Byte: return SerializationTypeCode.Byte;
                case TypeFlags.Int16: return SerializationTypeCode.Int16;
                case TypeFlags.UInt16: return SerializationTypeCode.UInt16;
                case TypeFlags.Int32: return SerializationTypeCode.Int32;
                case TypeFlags.UInt32: return SerializationTypeCode.UInt32;
                case TypeFlags.Int64: return SerializationTypeCode.Int64;
                case TypeFlags.UInt64: return SerializationTypeCode.UInt64;
                case TypeFlags.Single: return SerializationTypeCode.Single;
                case TypeFlags.Double: return SerializationTypeCode.Double;
                default: return SerializationTypeCode.Invalid;
            }
        }

        private static void CopyNamedArgumentType(ref BlobReader valueReader, BlobBuilder blobBuilder, byte[] originalBlob, CustomAttributeTypeProvider typeProvider, CustomAttributeTypeNameFormatter formatter, out SerializationTypeCode valueTypeCode, out SerializationTypeCode elementValueTypeCode, bool isElementType = false)
        {
            elementValueTypeCode = default;

            SerializationTypeCode typeCode = valueReader.ReadSerializationTypeCode();
            blobBuilder.WriteByte((byte)typeCode);

            if (typeCode == SerializationTypeCode.Boolean
                || typeCode == SerializationTypeCode.Char
                || typeCode == SerializationTypeCode.SByte
                || typeCode == SerializationTypeCode.Byte
                || typeCode == SerializationTypeCode.Int16
                || typeCode == SerializationTypeCode.UInt16
                || typeCode == SerializationTypeCode.Int32
                || typeCode == SerializationTypeCode.UInt32
                || typeCode == SerializationTypeCode.Int64
                || typeCode == SerializationTypeCode.UInt64
                || typeCode == SerializationTypeCode.Single
                || typeCode == SerializationTypeCode.Double
                || typeCode == SerializationTypeCode.String
                || typeCode == SerializationTypeCode.Type
                || typeCode == SerializationTypeCode.TaggedObject)
            {
                valueTypeCode = typeCode;
            }
            else if (typeCode == SerializationTypeCode.SZArray)
            {
                if (!isElementType)
                {
                    CopyNamedArgumentType(ref valueReader, blobBuilder, originalBlob, typeProvider, formatter, out elementValueTypeCode, out _, isElementType: true);
                    valueTypeCode = SerializationTypeCode.SZArray;
                }
                else
                {
                    valueTypeCode = SerializationTypeCode.Invalid;
                }
            }
            else if (typeCode == SerializationTypeCode.Enum)
            {
                int startOffset = valueReader.Offset;
                string? enumTypeName = valueReader.ReadSerializedString();
                int endOffset = valueReader.Offset;
                TypeDesc enumType = enumTypeName is not null ? typeProvider.GetTypeFromSerializedName(enumTypeName) : null;

                if (enumType is not null)
                {
                    string rewritten = formatter.FormatName(enumType, true);
                    if (rewritten == enumTypeName)
                        blobBuilder.WriteBytes(originalBlob, startOffset, endOffset - startOffset);
                    else
                        blobBuilder.WriteSerializedString(rewritten);

                    valueTypeCode = (SerializationTypeCode)typeProvider.GetUnderlyingEnumType(enumType);
                }
                else
                {
                    blobBuilder.WriteBytes(originalBlob, startOffset, endOffset - startOffset);
                    valueTypeCode = SerializationTypeCode.Invalid;
                }
            }
            else
            {
                valueTypeCode = SerializationTypeCode.Invalid;
            }
        }

        private static void CopySerializedValue(ref BlobReader valueReader, BlobBuilder blobBuilder, byte[] originalBlob, CustomAttributeTypeProvider typeProvider, CustomAttributeTypeNameFormatter formatter, SerializationTypeCode valueTypeCode, SerializationTypeCode elementValueTypeCode)
        {
            if (valueTypeCode == SerializationTypeCode.Invalid)
                throw new BadImageFormatException("Invalid custom attribute serialization type code.");

            if (valueTypeCode == SerializationTypeCode.Boolean
                || valueTypeCode == SerializationTypeCode.Byte
                || valueTypeCode == SerializationTypeCode.SByte)
            {
                CopyRawBytes(ref valueReader, blobBuilder, originalBlob, 1);
            }
            else if (valueTypeCode == SerializationTypeCode.Char
                || valueTypeCode == SerializationTypeCode.Int16
                || valueTypeCode == SerializationTypeCode.UInt16)
            {
                CopyRawBytes(ref valueReader, blobBuilder, originalBlob, 2);
            }
            else if (valueTypeCode == SerializationTypeCode.Int32
                || valueTypeCode == SerializationTypeCode.UInt32
                || valueTypeCode == SerializationTypeCode.Single)
            {
                CopyRawBytes(ref valueReader, blobBuilder, originalBlob, 4);
            }
            else if (valueTypeCode == SerializationTypeCode.Int64
                || valueTypeCode == SerializationTypeCode.UInt64
                || valueTypeCode == SerializationTypeCode.Double)
            {
                CopyRawBytes(ref valueReader, blobBuilder, originalBlob, 8);
            }
            else if (valueTypeCode == SerializationTypeCode.String)
            {
                CopySerializedString(ref valueReader, blobBuilder, originalBlob, rewriteTypeName: false, typeProvider, formatter);
            }
            else if (valueTypeCode == SerializationTypeCode.Type)
            {
                CopySerializedString(ref valueReader, blobBuilder, originalBlob, rewriteTypeName: true, typeProvider, formatter);
            }
            else if (valueTypeCode == SerializationTypeCode.SZArray)
            {
                int elementCount = valueReader.ReadInt32();
                blobBuilder.WriteInt32(elementCount);
                if (elementCount > 0)
                {
                    for (int i = 0; i < elementCount; i++)
                    {
                        CopySerializedValue(ref valueReader, blobBuilder, originalBlob, typeProvider, formatter, elementValueTypeCode, default);
                    }
                }
            }
            else if (valueTypeCode == SerializationTypeCode.TaggedObject)
            {
                CopyNamedArgumentType(ref valueReader, blobBuilder, originalBlob, typeProvider, formatter, out SerializationTypeCode boxedTypeCode, out SerializationTypeCode boxedElementTypeCode);
                CopySerializedValue(ref valueReader, blobBuilder, originalBlob, typeProvider, formatter, boxedTypeCode, boxedElementTypeCode);
            }
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
