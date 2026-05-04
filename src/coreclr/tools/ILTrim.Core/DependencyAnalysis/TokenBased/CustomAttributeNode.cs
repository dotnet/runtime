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
                return dependencies;
            }

            foreach (CustomAttributeTypedArgument<TypeDesc> fixedArg in decodedValue.FixedArguments)
            {
                GetDependenciesFromCustomAttributeArgument(dependencies, factory, fixedArg.Type, fixedArg.Value);
            }

            // Resolve the constructor once for all named arguments
            MethodDesc constructor = _module.TryGetMethod(customAttribute.Constructor);

            foreach (CustomAttributeNamedArgument<TypeDesc> namedArg in decodedValue.NamedArguments)
            {
                if (constructor is not null)
                {
                    if (namedArg.Kind == CustomAttributeNamedArgumentKind.Property)
                        GetDependenciesFromPropertySetter(dependencies, factory, constructor.OwningType, namedArg.Name);
                    else if (namedArg.Kind == CustomAttributeNamedArgumentKind.Field)
                        GetDependenciesFromField(dependencies, factory, constructor.OwningType, namedArg.Name);
                }

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
            if (attributeType is not EcmaType ecmaType)
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
            CustomAttributeValue<TypeDesc> decodedValue;
            try
            {
                decodedValue = customAttribute.DecodeValue(new CustomAttributeTypeProvider(_module));
            }
            catch (Exception ex) when (ex is TypeSystemException or BadImageFormatException)
            {
                blobBuilder.WriteBytes(_module.MetadataReader.GetBlobBytes(customAttribute.Value));
                return;
            }

            MethodDesc constructor = _module.TryGetMethod(customAttribute.Constructor);
            if (constructor is null)
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
            }
            else if (argType.IsSzArray)
            {
                TypeDesc elementType = ((ArrayType)argType).ElementType;
                if (elementType.IsObject)
                    type.SZArray().ObjectArray();
                else
                    EncodeElementType(type.SZArray().ElementType(), elementType, formatter);
            }
            else
            {
                EncodeElementType(type.ScalarType(), argType, formatter);
            }
        }

        private static void EncodeElementType(CustomAttributeElementTypeEncoder encoder, TypeDesc type, CustomAttributeTypeNameFormatter formatter)
        {
            switch (type.Category)
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
                    if (type.IsEnum)
                        encoder.Enum(formatter.FormatName(type, true));
                    else if (IsSystemType(type))
                        encoder.SystemType();
                    else if (type.IsString)
                        encoder.String();
                    break;
            }
        }

        private void WriteLiteralValue(LiteralEncoder literal, TypeDesc declaredType, TypeDesc actualType, object value, CustomAttributeTypeNameFormatter formatter)
        {
            if (declaredType is null)
            {
                // Nothing to write
            }
            else if (declaredType.IsObject)
            {
                if (value is null)
                {
                    literal.TaggedScalar(out CustomAttributeElementTypeEncoder type, out ScalarEncoder scalar);
                    type.String();
                    scalar.Constant(null);
                }
                else if (actualType is not null && actualType.IsSzArray)
                {
                    literal.TaggedVector(out CustomAttributeArrayTypeEncoder arrayType, out VectorEncoder vector);
                    TypeDesc elementType = ((ArrayType)actualType).ElementType;
                    if (elementType.IsObject)
                        arrayType.ObjectArray();
                    else
                        EncodeElementType(arrayType.ElementType(), elementType, formatter);
                    WriteVectorElements(vector, elementType, value, formatter);
                }
                else
                {
                    literal.TaggedScalar(out CustomAttributeElementTypeEncoder typeEncoder, out ScalarEncoder scalarEncoder);
                    EncodeElementType(typeEncoder, actualType, formatter);
                    WriteScalarValue(scalarEncoder, actualType, value, formatter);
                }
            }
            else if (declaredType.IsSzArray)
            {
                if (value is null)
                {
                    literal.Scalar().NullArray();
                }
                else
                {
                    TypeDesc elementType = ((ArrayType)declaredType).ElementType;
                    WriteVectorElements(literal.Vector(), elementType, value, formatter);
                }
            }
            else
            {
                // Scalar value (primitive, enum, string, Type)
                WriteScalarValue(literal.Scalar(), declaredType, value, formatter);
            }
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
                scalar.SystemType(value is TypeDesc typeofType ? formatter.FormatName(typeofType, true) : null);
            else
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
