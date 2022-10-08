// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.NET.HostModel.ComHost
{
    public static class ClsidMap
    {
        private struct ClsidEntry
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }
            [JsonPropertyName("assembly")]
            public string Assembly { get; set; }
            [JsonPropertyName("progid")]
            public string ProgId { get; set; }
        }

        public static void Create(MetadataReader metadataReader, string clsidMapPath)
        {
            Dictionary<string, ClsidEntry> clsidMap = new Dictionary<string, ClsidEntry>();

            string assemblyName = GetAssemblyName(metadataReader).FullName;

            bool isAssemblyComVisible = IsComVisible(metadataReader, metadataReader.GetAssemblyDefinition());

            foreach (TypeDefinitionHandle type in metadataReader.TypeDefinitions)
            {
                TypeDefinition definition = metadataReader.GetTypeDefinition(type);

                // Only public COM-visible classes can be exposed via the COM host.
                if (TypeIsPublic(metadataReader, definition) && TypeIsClass(metadataReader, definition) && IsComVisible(metadataReader, definition, isAssemblyComVisible))
                {
                    Guid guid = GetTypeGuid(metadataReader, definition);
                    string guidString = GetTypeGuid(metadataReader, definition).ToString("B");

                    if (clsidMap.TryGetValue(guidString, out ClsidEntry value))
                    {
                        throw new ConflictingGuidException(value.Type, GetTypeName(metadataReader, definition), guid);
                    }

                    string progId = GetProgId(metadataReader, definition);

                    clsidMap.Add(guidString,
                        new ClsidEntry
                        {
                            Type = GetTypeName(metadataReader, definition),
                            Assembly = assemblyName,
                            ProgId = !string.IsNullOrWhiteSpace(progId) ? progId : null
                        });
                }
            }

            using (StreamWriter writer = File.CreateText(clsidMapPath))
            {
                writer.Write(JsonSerializer.Serialize(clsidMap, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
            }
        }

        private static bool TypeIsClass(MetadataReader metadataReader, TypeDefinition definition)
        {
            if ((definition.Attributes & TypeAttributes.Interface) != 0)
            {
                return false;
            }

            EntityHandle baseTypeEntity = definition.BaseType;
            if (baseTypeEntity.Kind == HandleKind.TypeReference)
            {
                TypeReference baseClass = metadataReader.GetTypeReference((TypeReferenceHandle)baseTypeEntity);
                if (baseClass.ResolutionScope.Kind == HandleKind.AssemblyReference)
                {
                    if (HasTypeName(metadataReader, baseClass, "System", "ValueType") || HasTypeName(metadataReader, baseClass, "System", "Enum"))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TypeIsPublic(MetadataReader reader, TypeDefinition type)
        {
            switch (type.Attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.Public:
                    return true;
                case TypeAttributes.NestedPublic:
                    return TypeIsPublic(reader, reader.GetTypeDefinition(type.GetDeclaringType()));
                default:
                    return false;
            }
        }

        private static string GetTypeName(MetadataReader metadataReader, TypeDefinition type)
        {
            if (!type.GetDeclaringType().IsNil)
            {
                return $"{GetTypeName(metadataReader, metadataReader.GetTypeDefinition(type.GetDeclaringType()))}+{metadataReader.GetString(type.Name)}";
            }
            return $"{metadataReader.GetString(type.Namespace)}{Type.Delimiter}{metadataReader.GetString(type.Name)}";
        }

        private static bool HasTypeName(MetadataReader metadataReader, TypeReference type, string ns, string name)
        {
            return metadataReader.StringComparer.Equals(type.Namespace, ns) && metadataReader.StringComparer.Equals(type.Name, name);
        }

        private static AssemblyName GetAssemblyName(MetadataReader metadataReader)
        {
            AssemblyName name = new AssemblyName();

            AssemblyDefinition definition = metadataReader.GetAssemblyDefinition();
            name.Name = metadataReader.GetString(definition.Name);
            name.Version = definition.Version;
            name.CultureInfo = CultureInfo.GetCultureInfo(metadataReader.GetString(definition.Culture));
            name.SetPublicKey(metadataReader.GetBlobBytes(definition.PublicKey));

            return name;
        }

        private static bool IsComVisible(MetadataReader reader, AssemblyDefinition assembly)
        {
            CustomAttributeHandle handle = GetComVisibleAttribute(reader, assembly.GetCustomAttributes());

            if (handle.IsNil)
            {
                return false;
            }

            CustomAttribute comVisibleAttribute = reader.GetCustomAttribute(handle);
            CustomAttributeValue<KnownType> data = comVisibleAttribute.DecodeValue(new TypeResolver());
            return (bool)data.FixedArguments[0].Value;
        }

        private static bool IsComVisible(MetadataReader metadataReader, TypeDefinition definition, bool assemblyComVisible)
        {
            // We need to ensure that all parent scopes of the given type are not explicitly non-ComVisible.
            bool? IsComVisibleCore(TypeDefinition typeDefinition)
            {
                CustomAttributeHandle handle = GetComVisibleAttribute(metadataReader, typeDefinition.GetCustomAttributes());
                if (handle.IsNil)
                {
                    return null;
                }

                CustomAttribute comVisibleAttribute = metadataReader.GetCustomAttribute(handle);
                CustomAttributeValue<KnownType> data = comVisibleAttribute.DecodeValue(new TypeResolver());
                return (bool)data.FixedArguments[0].Value;
            }

            if (!definition.GetDeclaringType().IsNil)
            {
                return IsComVisible(metadataReader, metadataReader.GetTypeDefinition(definition.GetDeclaringType()), assemblyComVisible) && (IsComVisibleCore(definition) ?? assemblyComVisible);
            }

            return IsComVisibleCore(definition) ?? assemblyComVisible;
        }

        private static CustomAttributeHandle GetComVisibleAttribute(MetadataReader reader, CustomAttributeHandleCollection customAttributes)
        {
            foreach (CustomAttributeHandle attr in customAttributes)
            {
                CustomAttribute attribute = reader.GetCustomAttribute(attr);
                if (IsTargetAttribute(reader, attribute, "System.Runtime.InteropServices", "ComVisibleAttribute"))
                {
                    return attr;
                }
            }
            return default;
        }

        private static Guid GetTypeGuid(MetadataReader reader, TypeDefinition type)
        {
            // Find the class' GUID by reading the GuidAttribute value.
            // We do not support implicit runtime-generated GUIDs for the .NET Core COM host.
            foreach (CustomAttributeHandle attr in type.GetCustomAttributes())
            {
                CustomAttribute attribute = reader.GetCustomAttribute(attr);
                if (IsTargetAttribute(reader, attribute, "System.Runtime.InteropServices", "GuidAttribute"))
                {
                    CustomAttributeValue<KnownType> data = attribute.DecodeValue(new TypeResolver());
                    return Guid.Parse((string)data.FixedArguments[0].Value);
                }
            }
            throw new MissingGuidException(GetTypeName(reader, type));
        }

        private static string GetProgId(MetadataReader reader, TypeDefinition type)
        {
            foreach (CustomAttributeHandle attr in type.GetCustomAttributes())
            {
                CustomAttribute attribute = reader.GetCustomAttribute(attr);
                if (IsTargetAttribute(reader, attribute, "System.Runtime.InteropServices", "ProgIdAttribute"))
                {
                    CustomAttributeValue<KnownType> data = attribute.DecodeValue(new TypeResolver());
                    return (string)data.FixedArguments[0].Value;
                }
            }
            return GetTypeName(reader, type);
        }

        private static bool IsTargetAttribute(MetadataReader reader, CustomAttribute attribute, string targetNamespace, string targetName)
        {
            StringHandle namespaceMaybe;
            StringHandle nameMaybe;
            switch (attribute.Constructor.Kind)
            {
                case HandleKind.MemberReference:
                    MemberReference refConstructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    TypeReference refType = reader.GetTypeReference((TypeReferenceHandle)refConstructor.Parent);
                    namespaceMaybe = refType.Namespace;
                    nameMaybe = refType.Name;
                    break;

                case HandleKind.MethodDefinition:
                    MethodDefinition defConstructor = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                    TypeDefinition defType = reader.GetTypeDefinition(defConstructor.GetDeclaringType());
                    namespaceMaybe = defType.Namespace;
                    nameMaybe = defType.Name;
                    break;

                default:
                    Debug.Assert(false, "Unknown attribute constructor kind");
                    return false;
            }

            return reader.StringComparer.Equals(namespaceMaybe, targetNamespace) && reader.StringComparer.Equals(nameMaybe, targetName);
        }

        private enum KnownType
        {
            Bool,
            String,
            SystemType,
            Unknown
        }

        private sealed class TypeResolver : ICustomAttributeTypeProvider<KnownType>
        {
            public KnownType GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                switch (typeCode)
                {
                    case PrimitiveTypeCode.Boolean:
                        return KnownType.Bool;
                    case PrimitiveTypeCode.String:
                        return KnownType.String;
                    default:
                        return KnownType.Unknown;
                }
            }

            public KnownType GetSystemType()
            {
                return KnownType.SystemType;
            }

            public KnownType GetSZArrayType(KnownType elementType)
            {
                return KnownType.Unknown;
            }

            public KnownType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                return KnownType.Unknown;
            }

            public KnownType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                return KnownType.Unknown;
            }

            public KnownType GetTypeFromSerializedName(string name)
            {
                return KnownType.Unknown;
            }

            public PrimitiveTypeCode GetUnderlyingEnumType(KnownType type)
            {
                throw new BadImageFormatException("Unexpectedly got an enum parameter for an attribute.");
            }

            public bool IsSystemType(KnownType type)
            {
                return type == KnownType.SystemType;
            }
        }

    }
}
