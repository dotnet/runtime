// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using DependencyNode = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the Custom Attribute metadata table.
    /// </summary>
    public sealed partial class CustomAttributeNode : TokenBasedNode
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

        public static void AddDependenciesDueToAssemblyCustomAttributes(ref DependencyList dependencies, ref IReadOnlyCollection<CombinedDependencyListEntry> conditionalDependencies, NodeFactory factory, EcmaModule module, CustomAttributeHandleCollection handles)
        {
            List<CombinedDependencyListEntry> conditions = null;

            foreach (CustomAttributeHandle customAttribute in handles)
            {
                if (factory.Settings.StripSecurity && IsCustomAttributeForSecurity(module, customAttribute))
                    continue;

                if (IsDebuggerAttribute(module, customAttribute))
                {
                    TypeDesc targetType = GetDebuggerAttributeTargetType(module, customAttribute);
                    if (targetType?.GetTypeDefinition() is EcmaType targetEcmaType && factory.IsModuleTrimmed(targetEcmaType.Module))
                    {
                        conditions ??= new List<CombinedDependencyListEntry>();
                        conditions.Add(new(
                            factory.CustomAttribute(module, customAttribute),
                            factory.TypeDefinition(targetEcmaType.Module, targetEcmaType.Handle),
                            "Debugger attribute target type"));
                    }
                    else if (targetType is not null)
                    {
                        dependencies ??= new DependencyList();
                        dependencies.Add(factory.CustomAttribute(module, customAttribute), "Debugger attribute target type");
                    }

                    continue;
                }

                dependencies ??= new DependencyList();
                dependencies.Add(factory.CustomAttribute(module, customAttribute), "Custom attribute");
            }

            conditionalDependencies = conditions is not null
                ? conditions
                : Array.Empty<CombinedDependencyListEntry>();
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

        public static bool IsDebuggerAttribute(EcmaModule module, CustomAttributeHandle handle)
        {
            MetadataReader metadataReader = module.MetadataReader;
            return metadataReader.GetAttributeNamespaceAndName(handle, out StringHandle namespaceHandle, out StringHandle nameHandle)
                && metadataReader.StringEquals(namespaceHandle, "System.Diagnostics"u8)
                && (metadataReader.StringEquals(nameHandle, "DebuggerDisplayAttribute"u8)
                    || metadataReader.StringEquals(nameHandle, "DebuggerTypeProxyAttribute"u8));
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
                // Metadata decode failed.
                _isCorrupted = true;
                return dependencies;
            }

            AddDependenciesForDebuggerAttribute(dependencies, factory, customAttribute, decodedValue);

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

        private void AddDependenciesForDebuggerAttribute(DependencyList dependencies, NodeFactory factory, CustomAttribute customAttribute, CustomAttributeValue<TypeDesc> decodedValue)
        {
            if (!factory.Settings.KeepMembersForDebugger || !IsDebuggerAttribute(_module, Handle))
                return;

            TypeDesc targetType;
            if (customAttribute.Parent.Kind == HandleKind.TypeDefinition)
            {
                targetType = _module.GetType((TypeDefinitionHandle)customAttribute.Parent);
            }
            else if (customAttribute.Parent.Kind == HandleKind.AssemblyDefinition)
            {
                targetType = GetDebuggerAttributeTargetType(_module, decodedValue);
            }
            else
            {
                return;
            }

            if (targetType?.GetTypeDefinition() is not EcmaType targetEcmaType)
                return;

            MetadataReader metadataReader = _module.MetadataReader;
            metadataReader.GetAttributeNamespaceAndName(Handle, out _, out StringHandle nameHandle);

            if (metadataReader.StringEquals(nameHandle, "DebuggerDisplayAttribute"u8))
            {
                if (decodedValue.FixedArguments.Length > 0 && decodedValue.FixedArguments[0].Value is string displayString)
                    AddDependenciesForDebuggerDisplayAttributeValue(dependencies, factory, targetEcmaType, displayString);

                foreach (CustomAttributeNamedArgument<TypeDesc> namedArg in decodedValue.NamedArguments)
                {
                    if (namedArg.Name is "Name" or "Type" && namedArg.Value is string namedDisplayString)
                        AddDependenciesForDebuggerDisplayAttributeValue(dependencies, factory, targetEcmaType, namedDisplayString);
                }
            }
            else
            {
                TypeDesc proxyType = null;
                if (decodedValue.FixedArguments.Length > 0)
                {
                    object constructorArgument = decodedValue.FixedArguments[0].Value;
                    proxyType = constructorArgument as TypeDesc;
                    if (proxyType is null && constructorArgument is string proxyTypeName)
                        proxyType = targetEcmaType.Module.GetTypeByCustomAttributeTypeName(proxyTypeName, throwIfNotFound: false);
                }

                if (proxyType?.GetTypeDefinition() is EcmaType proxyEcmaType)
                {
                    dependencies.Add(factory.ReflectedType(proxyEcmaType), "Referenced by debugger attribute");
                    AddDependenciesForAllMethodsAndFields(dependencies, factory, proxyEcmaType);
                }
            }
        }

        private static TypeDesc GetDebuggerAttributeTargetType(EcmaModule module, CustomAttributeHandle customAttribute)
        {
            try
            {
                return GetDebuggerAttributeTargetType(module, module.MetadataReader.GetCustomAttribute(customAttribute).DecodeValue(new CustomAttributeTypeProvider(module)));
            }
            catch (Exception ex) when (ex is TypeSystemException or BadImageFormatException)
            {
                return null;
            }
        }

        private static TypeDesc GetDebuggerAttributeTargetType(EcmaModule module, CustomAttributeValue<TypeDesc> decodedValue)
        {
            foreach (CustomAttributeNamedArgument<TypeDesc> namedArg in decodedValue.NamedArguments)
            {
                if (namedArg.Name == "Target")
                    return namedArg.Value as TypeDesc;

                if (namedArg.Name == "TargetTypeName" && namedArg.Value is string targetTypeName)
                    return module.GetTypeByCustomAttributeTypeName(targetTypeName, throwIfNotFound: false);
            }

            return null;
        }

        private static void AddDependenciesForDebuggerDisplayAttributeValue(DependencyList dependencies, NodeFactory factory, EcmaType type, string displayString)
        {
            if (string.IsNullOrEmpty(displayString))
                return;

            foreach (Match match in DebuggerDisplayAttributeValueRegex.Matches(displayString))
            {
                ReadOnlySpan<char> realMatch = match.Value.AsSpan(1, match.Value.Length - 2);

                if (ContainsNqSuffixRegex.IsMatch(realMatch))
                {
                    int commaIndex = realMatch.LastIndexOf(',');
                    if (commaIndex >= 0)
                        realMatch = realMatch[..commaIndex];
                }

                if (realMatch.EndsWith("()", StringComparison.Ordinal))
                {
                    ReadOnlySpan<char> methodName = realMatch[..^2];

                    // Calls to methods on some member are not supported.
                    if (methodName.Contains('.'))
                        continue;

                    if (TryAddDependencyForMethodWithNoParameters(dependencies, factory, type, methodName.ToString()))
                        continue;
                }
                else
                {
                    string memberName = realMatch.ToString();

                    if (TryAddDependencyForField(dependencies, factory, type, memberName))
                        continue;

                    if (TryAddDependencyForProperty(dependencies, factory, type, memberName))
                        continue;
                }

                for (TypeDesc currentType = type; currentType?.GetTypeDefinition() is EcmaType ecmaType; currentType = currentType.BaseType)
                    AddDependenciesForAllMethodsAndFields(dependencies, factory, ecmaType);

                return;
            }
        }

        private static bool TryAddDependencyForMethodWithNoParameters(DependencyList dependencies, NodeFactory factory, EcmaType type, string methodName)
        {
            for (TypeDesc currentType = type; currentType?.GetTypeDefinition() is EcmaType ecmaType; currentType = currentType.BaseType)
            {
                MetadataReader reader = ecmaType.MetadataReader;
                TypeDefinition typeDef = reader.GetTypeDefinition(ecmaType.Handle);

                foreach (MethodDefinitionHandle methodHandle in typeDef.GetMethods())
                {
                    MethodDefinition methodDefinition = reader.GetMethodDefinition(methodHandle);
                    if (!reader.StringComparer.Equals(methodDefinition.Name, methodName))
                        continue;

                    MethodDesc method = ecmaType.Module.GetMethod(methodHandle);
                    if (method.Signature.Length == 0)
                    {
                        dependencies.Add(factory.ReflectedMethod(method), "Referenced by debugger attribute");
                        return true;
                    }
                }
            }

            return false;
        }

        [GeneratedRegex("{[^{}]+}")]
        private static partial Regex DebuggerDisplayAttributeValueRegex { get; }

        [GeneratedRegex(@".+,\s*nq")]
        private static partial Regex ContainsNqSuffixRegex { get; }

        private static bool TryAddDependencyForField(DependencyList dependencies, NodeFactory factory, EcmaType type, string fieldName)
        {
            byte[] fieldNameBytes = Encoding.UTF8.GetBytes(fieldName);

            for (TypeDesc currentType = type; currentType?.GetTypeDefinition() is EcmaType ecmaType; currentType = currentType.BaseType)
            {
                FieldDesc field = ecmaType.GetField(fieldNameBytes);
                if (field is not null)
                {
                    dependencies.Add(factory.ReflectedField(field), "Referenced by debugger attribute");
                    return true;
                }
            }

            return false;
        }

        private static bool TryAddDependencyForProperty(DependencyList dependencies, NodeFactory factory, EcmaType type, string propertyName)
        {
            for (TypeDesc currentType = type; currentType?.GetTypeDefinition() is EcmaType ecmaType; currentType = currentType.BaseType)
            {
                MetadataReader reader = ecmaType.MetadataReader;
                TypeDefinition typeDef = reader.GetTypeDefinition(ecmaType.Handle);

                foreach (PropertyDefinitionHandle propertyHandle in typeDef.GetProperties())
                {
                    PropertyDefinition property = reader.GetPropertyDefinition(propertyHandle);
                    if (!reader.StringComparer.Equals(property.Name, propertyName))
                        continue;

                    dependencies.Add(factory.PropertyDefinition(ecmaType.Module, propertyHandle), "Referenced by debugger attribute");

                    PropertyAccessors accessors = property.GetAccessors();
                    if (!accessors.Getter.IsNil)
                        dependencies.Add(factory.ReflectedMethod(ecmaType.Module.GetMethod(accessors.Getter)), "Referenced by debugger attribute");
                    if (!accessors.Setter.IsNil)
                        dependencies.Add(factory.ReflectedMethod(ecmaType.Module.GetMethod(accessors.Setter)), "Referenced by debugger attribute");

                    return true;
                }
            }

            return false;
        }

        private static void AddDependenciesForAllMethodsAndFields(DependencyList dependencies, NodeFactory factory, EcmaType type)
        {
            MetadataReader reader = type.MetadataReader;
            TypeDefinition typeDef = reader.GetTypeDefinition(type.Handle);

            foreach (MethodDefinitionHandle methodHandle in typeDef.GetMethods())
                dependencies.Add(factory.ReflectedMethod(type.Module.GetMethod(methodHandle)), "Referenced by debugger attribute");

            foreach (FieldDefinitionHandle fieldHandle in typeDef.GetFields())
                dependencies.Add(factory.ReflectedField(type.Module.GetField(fieldHandle)), "Referenced by debugger attribute");
        }

        private static void GetDependenciesFromCustomAttributeArgument(DependencyList dependencies, NodeFactory factory, TypeDesc type, object value)
        {
            // Report the type itself (e.g. enum types that need to be kept for boxing)
            dependencies.Add(factory.ReflectedType(type), "Custom attribute blob");

            if (type.UnderlyingType.IsPrimitive || type.IsString || value is null)
                return;

            if (type.IsSzArray)
            {
                TypeDesc elementType = ((ArrayType)type).ElementType;
                if (!elementType.UnderlyingType.IsPrimitive && !elementType.IsString)
                {
                    // DecodeValue always produces ImmutableArray<...> for SzArray types.
                    var arrayElements = (ImmutableArray<CustomAttributeTypedArgument<TypeDesc>>)value;
                    foreach (CustomAttributeTypedArgument<TypeDesc> element in arrayElements)
                    {
                        GetDependenciesFromCustomAttributeArgument(dependencies, factory, element.Type, element.Value);
                    }
                }
            }
            else if (value is TypeDesc typeofType)
            {
                // typeof() - the value is a TypeDesc
                dependencies.Add(factory.ReflectedType(typeofType), "Custom attribute blob");
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
                    if (!accessors.Setter.IsNil)
                    {
                        dependencies.Add(factory.ReflectedMethod(ecmaType.Module.GetMethod(accessors.Setter)), "Custom attribute blob");
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
                dependencies.Add(factory.ReflectedField(field), "Custom attribute blob");
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
            if (_isCorrupted || _module.TryGetMethod(customAttribute.Constructor) is not MethodDesc constructor)
            {
                blobBuilder.WriteBytes(_module.MetadataReader.GetBlobBytes(customAttribute.Value));
                return;
            }

            BlobReader valueReader = _module.MetadataReader.GetBlobReader(customAttribute.Value);
            var formatter = new CustomAttributeTypeNameFormatter();

            // The dependency walk already decoded the blob successfully unless `_isCorrupted` is set.
            blobBuilder.WriteUInt16(valueReader.ReadUInt16());

            MethodSignature constructorSig = constructor.Signature;
            for (int i = 0; i < constructorSig.Length; i++)
            {
                GetFixedArgumentTypeCodes(constructorSig[i], out SerializationTypeCode valueTypeCode, out SerializationTypeCode elementValueTypeCode);
                CopySerializedValue(ref valueReader, blobBuilder, formatter, valueTypeCode, elementValueTypeCode);
            }

            ushort namedArgumentCount = valueReader.ReadUInt16();
            blobBuilder.WriteUInt16(namedArgumentCount);

            for (int i = 0; i < namedArgumentCount; i++)
            {
                CustomAttributeNamedArgumentKind kind = (CustomAttributeNamedArgumentKind)valueReader.ReadByte();
                blobBuilder.WriteByte((byte)kind);

                CopyNamedArgumentType(ref valueReader, blobBuilder, formatter, out SerializationTypeCode valueTypeCode, out SerializationTypeCode elementValueTypeCode);
                CopySerializedString(ref valueReader, blobBuilder, rewriteTypeName: false, formatter);
                CopySerializedValue(ref valueReader, blobBuilder, formatter, valueTypeCode, elementValueTypeCode);
            }
        }

        // We can avoid the intermediate array allocation after https://github.com/dotnet/runtime/issues/85280 (or with unsafe code now)
        private static void CopyRawBytes(ref BlobReader reader, BlobBuilder blobBuilder, int byteCount)
            => blobBuilder.WriteBytes(reader.ReadBytes(byteCount));

        private TypeDesc? CopySerializedString(ref BlobReader valueReader, BlobBuilder blobBuilder, bool rewriteTypeName, CustomAttributeTypeNameFormatter formatter)
        {
            string? s = valueReader.ReadSerializedString();
            TypeDesc? resolved = null;

            if (rewriteTypeName && s is not null)
            {
                resolved = _module.GetTypeByCustomAttributeTypeName(s);
                s = formatter.FormatName(resolved, true);
            }

            blobBuilder.WriteSerializedString(s);
            return resolved;
        }

        private static void GetFixedArgumentTypeCodes(TypeDesc type, out SerializationTypeCode valueTypeCode, out SerializationTypeCode elementValueTypeCode)
        {
            elementValueTypeCode = default;
            if (type.IsPrimitive || type.IsEnum)
            {
                valueTypeCode = GetPrimitiveSerializationTypeCode(type.UnderlyingType);
            }
            else if (type.IsString)
            {
                valueTypeCode = SerializationTypeCode.String;
            }
            else if (type.IsObject)
            {
                valueTypeCode = SerializationTypeCode.TaggedObject;
            }
            else if (type.IsSzArray)
            {
                valueTypeCode = SerializationTypeCode.SZArray;
                GetFixedArgumentTypeCodes(((ArrayType)type).ElementType, out elementValueTypeCode, out _);
            }
            else
            {
                valueTypeCode = SerializationTypeCode.Type;
            }
        }

        private static SerializationTypeCode GetPrimitiveSerializationTypeCode(TypeDesc type)
            => type.Category switch
            {
                TypeFlags.Boolean => SerializationTypeCode.Boolean,
                TypeFlags.Char => SerializationTypeCode.Char,
                TypeFlags.SByte => SerializationTypeCode.SByte,
                TypeFlags.Byte => SerializationTypeCode.Byte,
                TypeFlags.Int16 => SerializationTypeCode.Int16,
                TypeFlags.UInt16 => SerializationTypeCode.UInt16,
                TypeFlags.Int32 => SerializationTypeCode.Int32,
                TypeFlags.UInt32 => SerializationTypeCode.UInt32,
                TypeFlags.Int64 => SerializationTypeCode.Int64,
                TypeFlags.UInt64 => SerializationTypeCode.UInt64,
                TypeFlags.Single => SerializationTypeCode.Single,
                TypeFlags.Double => SerializationTypeCode.Double,
                _ => throw new BadImageFormatException(),
            };

        private void CopyNamedArgumentType(ref BlobReader valueReader, BlobBuilder blobBuilder, CustomAttributeTypeNameFormatter formatter, out SerializationTypeCode valueTypeCode, out SerializationTypeCode elementValueTypeCode)
        {
            valueTypeCode = valueReader.ReadSerializationTypeCode();
            elementValueTypeCode = default;
            blobBuilder.WriteByte((byte)valueTypeCode);

            switch (valueTypeCode)
            {
                case SerializationTypeCode.SZArray:
                    CopyNamedArgumentType(ref valueReader, blobBuilder, formatter, out elementValueTypeCode, out _);
                    break;
                case SerializationTypeCode.Enum:
                    TypeDesc enumType = CopySerializedString(ref valueReader, blobBuilder, rewriteTypeName: true, formatter);
                    valueTypeCode = GetPrimitiveSerializationTypeCode(enumType.UnderlyingType);
                    break;
            }
        }

        private void CopySerializedValue(ref BlobReader valueReader, BlobBuilder blobBuilder, CustomAttributeTypeNameFormatter formatter, SerializationTypeCode valueTypeCode, SerializationTypeCode elementValueTypeCode)
        {
            switch (valueTypeCode)
            {
                case SerializationTypeCode.Boolean or SerializationTypeCode.Byte or SerializationTypeCode.SByte:
                    CopyRawBytes(ref valueReader, blobBuilder, 1);
                    break;
                case SerializationTypeCode.Char or SerializationTypeCode.Int16 or SerializationTypeCode.UInt16:
                    CopyRawBytes(ref valueReader, blobBuilder, 2);
                    break;
                case SerializationTypeCode.Int32 or SerializationTypeCode.UInt32 or SerializationTypeCode.Single:
                    CopyRawBytes(ref valueReader, blobBuilder, 4);
                    break;
                case SerializationTypeCode.Int64 or SerializationTypeCode.UInt64 or SerializationTypeCode.Double:
                    CopyRawBytes(ref valueReader, blobBuilder, 8);
                    break;
                case SerializationTypeCode.String:
                    CopySerializedString(ref valueReader, blobBuilder, rewriteTypeName: false, formatter);
                    break;
                case SerializationTypeCode.Type:
                    CopySerializedString(ref valueReader, blobBuilder, rewriteTypeName: true, formatter);
                    break;
                case SerializationTypeCode.SZArray:
                    int elementCount = valueReader.ReadInt32();
                    blobBuilder.WriteInt32(elementCount);
                    for (int i = 0; i < elementCount; i++)
                    {
                        CopySerializedValue(ref valueReader, blobBuilder, formatter, elementValueTypeCode, default);
                    }
                    break;
                case SerializationTypeCode.TaggedObject:
                    CopyNamedArgumentType(ref valueReader, blobBuilder, formatter, out SerializationTypeCode boxedTypeCode, out SerializationTypeCode boxedElementTypeCode);
                    CopySerializedValue(ref valueReader, blobBuilder, formatter, boxedTypeCode, boxedElementTypeCode);
                    break;
            }
        }

        public override string ToString()
        {
            // TODO: Need to write a helper to get the name of the type
            return "Custom Attribute";
        }
    }
}
