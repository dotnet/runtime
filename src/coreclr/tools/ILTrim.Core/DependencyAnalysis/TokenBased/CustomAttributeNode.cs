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
            catch (TypeSystemException)
            {
                // Attribute ctor doesn't resolve, typeof() refers to something that can't be loaded,
                // attribute refers to a non-existing field, etc.
                yield break;
            }
            catch (BadImageFormatException)
            {
                // System.Reflection.Metadata throws BadImageFormatException if the blob is malformed.
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

            // Resolve the constructor once for all named arguments
            MethodDesc constructor = _module.GetMethod(
                _module.MetadataReader.GetCustomAttribute(Handle).Constructor);

            foreach (CustomAttributeNamedArgument<TypeDesc> namedArg in value.NamedArguments)
            {
                if (constructor is not null)
                {
                    if (namedArg.Kind == CustomAttributeNamedArgumentKind.Property)
                    {
                        foreach (var entry in GetDependenciesFromPropertySetter(factory, constructor.OwningType, namedArg.Name))
                            yield return entry;
                    }
                    else if (namedArg.Kind == CustomAttributeNamedArgumentKind.Field)
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
            if (reflectedType is DependencyNode typeNode)
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
                if (reflectedTypeofType is DependencyNode typeofNode)
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
                        if (reflectedMethod is DependencyNode methodNode)
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
                if (reflectedField is DependencyNode fieldNode)
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
            BlobBuilder blobBuilder = writeContext.GetSharedBlobBuilder();
            RewriteCustomAttributeBlob(customAttribute, blobBuilder);

            return builder.AddCustomAttribute(writeContext.TokenMap.MapToken(customAttribute.Parent),
                writeContext.TokenMap.MapToken(customAttribute.Constructor),
                builder.GetOrAddBlob(blobBuilder));
        }

        private void RewriteCustomAttributeBlob(CustomAttribute customAttribute, BlobBuilder blobBuilder)
        {
            CustomAttributeValue<TypeDesc> decodedValue;
            try
            {
                decodedValue = customAttribute.DecodeValue(new CustomAttributeTypeProvider(_module));
            }
            catch (TypeSystemException)
            {
                blobBuilder.WriteBytes(_module.MetadataReader.GetBlobBytes(customAttribute.Value));
                return;
            }
            catch (BadImageFormatException)
            {
                blobBuilder.WriteBytes(_module.MetadataReader.GetBlobBytes(customAttribute.Value));
                return;
            }

            EncodeCustomAttributeBlob(customAttribute, decodedValue, blobBuilder);
        }

        private void EncodeCustomAttributeBlob(
            CustomAttribute customAttribute,
            CustomAttributeValue<TypeDesc> decodedValue,
            BlobBuilder blobBuilder)
        {
            MethodDesc constructor;
            try
            {
                constructor = _module.GetMethod(customAttribute.Constructor);
            }
            catch (TypeSystemException)
            {
                blobBuilder.WriteBytes(_module.MetadataReader.GetBlobBytes(customAttribute.Value));
                return;
            }

            var formatter = new CustomAttributeTypeNameFormatter();
            var encoder = new BlobEncoder(blobBuilder);
            encoder.CustomAttributeSignature(out FixedArgumentsEncoder fixedArgs, out CustomAttributeNamedArgumentsEncoder namedArgs);

            // Write fixed arguments
            MethodSignature constructorSig = constructor.Signature;
            for (int i = 0; i < decodedValue.FixedArguments.Length; i++)
            {
                TypeDesc paramType = constructorSig[i];
                WriteLiteralValue(fixedArgs.AddArgument(), paramType, decodedValue.FixedArguments[i].Type, decodedValue.FixedArguments[i].Value, formatter);
            }

            // Write named arguments
            NamedArgumentsEncoder namedArgsEncoder = namedArgs.Count(decodedValue.NamedArguments.Length);
            foreach (CustomAttributeNamedArgument<TypeDesc> namedArg in decodedValue.NamedArguments)
            {
                namedArgsEncoder.AddArgument(
                    namedArg.Kind == CustomAttributeNamedArgumentKind.Field,
                    out NamedArgumentTypeEncoder type,
                    out NameEncoder name,
                    out LiteralEncoder literal);

                EncodeNamedArgumentType(type, namedArg.Type, formatter);
                name.Name(namedArg.Name);
                WriteLiteralValue(literal, namedArg.Type, namedArg.Type, namedArg.Value, formatter);
            }
        }

        private static void EncodeNamedArgumentType(NamedArgumentTypeEncoder type, TypeDesc argType, CustomAttributeTypeNameFormatter formatter)
        {
            if (argType.IsObject)
            {
                type.Object();
                return;
            }

            if (argType.IsSzArray)
            {
                TypeDesc elementType = ((ArrayType)argType).ElementType;
                if (elementType.IsObject)
                {
                    type.SZArray().ObjectArray();
                }
                else
                {
                    EncodeElementType(type.SZArray().ElementType(), elementType, formatter);
                }
                return;
            }

            EncodeElementType(type.ScalarType(), argType, formatter);
        }

        private static void EncodeElementType(CustomAttributeElementTypeEncoder encoder, TypeDesc type, CustomAttributeTypeNameFormatter formatter)
        {
            if (type.IsEnum)
            {
                encoder.Enum(formatter.FormatName(type, true));
                return;
            }

            if (IsSystemType(type))
            {
                encoder.SystemType();
                return;
            }

            switch (type.UnderlyingType.Category)
            {
                case TypeFlags.Boolean: encoder.Boolean(); break;
                case TypeFlags.Char: encoder.Char(); break;
                case TypeFlags.SByte: encoder.SByte(); break;
                case TypeFlags.Byte: encoder.Byte(); break;
                case TypeFlags.Int16: encoder.Int16(); break;
                case TypeFlags.UInt16: encoder.UInt16(); break;
                case TypeFlags.Int32: encoder.Int32(); break;
                case TypeFlags.UInt32: encoder.UInt32(); break;
                case TypeFlags.Int64: encoder.Int64(); break;
                case TypeFlags.UInt64: encoder.UInt64(); break;
                case TypeFlags.Single: encoder.Single(); break;
                case TypeFlags.Double: encoder.Double(); break;
                default:
                    if (type.IsString)
                        encoder.String();
                    break;
            }
        }

        private void WriteLiteralValue(LiteralEncoder literal, TypeDesc declaredType, TypeDesc actualType, object value, CustomAttributeTypeNameFormatter formatter)
        {
            if (declaredType is null)
                return;

            // Boxed (object-typed) arguments: write type tag then the value
            if (declaredType.IsObject)
            {
                if (value is null)
                {
                    literal.TaggedScalar(out CustomAttributeElementTypeEncoder type, out ScalarEncoder scalar);
                    type.String();
                    scalar.Constant(null);
                    return;
                }

                if (actualType is not null && actualType.IsSzArray)
                {
                    literal.TaggedVector(out CustomAttributeArrayTypeEncoder arrayType, out VectorEncoder vector);
                    TypeDesc elementType = ((ArrayType)actualType).ElementType;
                    if (elementType.IsObject)
                        arrayType.ObjectArray();
                    else
                        EncodeElementType(arrayType.ElementType(), elementType, formatter);
                    WriteVectorElements(vector, elementType, value, formatter);
                    return;
                }

                literal.TaggedScalar(out CustomAttributeElementTypeEncoder typeEncoder, out ScalarEncoder scalarEncoder);
                EncodeElementType(typeEncoder, actualType, formatter);
                WriteScalarValue(scalarEncoder, actualType, value, formatter);
                return;
            }

            // Array arguments
            if (declaredType.IsSzArray)
            {
                if (value is null)
                {
                    literal.Scalar().NullArray();
                    return;
                }

                TypeDesc elementType = ((ArrayType)declaredType).ElementType;
                WriteVectorElements(literal.Vector(), elementType, value, formatter);
                return;
            }

            // Scalar value (primitive, enum, string, Type)
            WriteScalarValue(literal.Scalar(), declaredType, value, formatter);
        }

        private void WriteVectorElements(VectorEncoder vector, TypeDesc elementType, object value, CustomAttributeTypeNameFormatter formatter)
        {
            if (value is ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> arrayElements)
            {
                LiteralsEncoder literals = vector.Count(arrayElements.Length);
                foreach (CustomAttributeTypedArgument<TypeDesc> element in arrayElements)
                {
                    WriteLiteralValue(literals.AddLiteral(), elementType, element.Type, element.Value, formatter);
                }
            }
        }

        private static void WriteScalarValue(ScalarEncoder scalar, TypeDesc type, object value, CustomAttributeTypeNameFormatter formatter)
        {
            if (IsSystemType(type))
            {
                scalar.SystemType(value is TypeDesc typeofType ? formatter.FormatName(typeofType, true) : null);
                return;
            }

            // Primitives, enums (underlying value), strings, null
            scalar.Constant(value);
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
