// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MonoTargetsTasks
{
    // For some valuetypes we cannot determine if they are compatible with disabled
    // runtime marshaling without first resolving their base types. In this case we
    // first mark the assembly as Inconclusive and do a second pass over the collected
    // base type references in order to decide. If the base types are System.Enum,
    // then the valuetypes are enumerations, and are compatible.
    internal enum Compatibility
    {
        Compatible,
        Incompatible,
        Inconclusive
    }

    internal sealed class InconclusiveCompatibilityCollection
    {
        private readonly Dictionary<string, HashSet<string>> _data = new();

        public bool IsEmpty => _data.Count == 0;

        public void Add(string assyName, string namespaceName, string typeName)
        {
            HashSet<string>? incAssyTypes;

            if (!_data.TryGetValue(assyName, out incAssyTypes))
            {
                incAssyTypes = new();
                _data.Add(assyName, incAssyTypes);
            }

            incAssyTypes.Add($"{namespaceName}:{typeName}");
        }

        public HashSet<string> EnumerateForAssembly(string assyName)
        {
            if (_data.TryGetValue(assyName, out HashSet<string>? incAssyTypes))
                return incAssyTypes!;

            return new HashSet<string>();
        }
    }

    internal sealed class MinimalMarshalingTypeCompatibilityProvider : ISignatureTypeProvider<Compatibility, object>
    {
        internal MinimalMarshalingTypeCompatibilityProvider(TaskLoggingHelper log)
        {
          _log = log;
        }

        private readonly TaskLoggingHelper _log;

        // assembly name -> set of types needed for second pass
        private readonly InconclusiveCompatibilityCollection _inconclusive = new();

        public bool IsSecondPassNeeded => !_inconclusive.IsEmpty;
        public HashSet<string> GetInconclusiveTypesForAssembly(string assyName) => _inconclusive.EnumerateForAssembly(assyName);

        public Compatibility GetArrayType(Compatibility elementType, ArrayShape shape) => Compatibility.Incompatible;
        public Compatibility GetByReferenceType(Compatibility elementType) => Compatibility.Incompatible;
        public Compatibility GetFunctionPointerType(MethodSignature<Compatibility> signature) => Compatibility.Compatible;
        public Compatibility GetGenericInstantiation(Compatibility genericType, ImmutableArray<Compatibility> typeArguments) => genericType;
        public Compatibility GetGenericMethodParameter(object genericContext, int index) => Compatibility.Incompatible;
        public Compatibility GetGenericTypeParameter(object genericContext, int index) => Compatibility.Incompatible;
        public Compatibility GetModifiedType(Compatibility modifier, Compatibility unmodifiedType, bool isRequired) => Compatibility.Incompatible;
        public Compatibility GetPinnedType(Compatibility elementType) => Compatibility.Compatible;
        public Compatibility GetPointerType(Compatibility elementType) => Compatibility.Compatible;
        public Compatibility GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return typeCode switch
            {
            PrimitiveTypeCode.Object => Compatibility.Incompatible,
            PrimitiveTypeCode.String => Compatibility.Incompatible,
            PrimitiveTypeCode.TypedReference => Compatibility.Incompatible,
            _ => Compatibility.Compatible
            };
        }

        public Compatibility GetSZArrayType(Compatibility elementType) => Compatibility.Incompatible;

        public Compatibility GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(handle);
            if (reader.GetString(typeDef.Namespace) == "System" &&
                reader.GetString(typeDef.Name) == "Enum")
                return Compatibility.Compatible;

            try
            {
                EntityHandle baseTypeHandle = typeDef.BaseType;
                if (baseTypeHandle.Kind == HandleKind.TypeReference)
                {
                    TypeReference baseType = reader.GetTypeReference((TypeReferenceHandle)baseTypeHandle);
                    if (reader.GetString(baseType.Namespace) == "System" &&
                        reader.GetString(baseType.Name) == "Enum")
                        return Compatibility.Compatible;
                }
                else if (baseTypeHandle.Kind == HandleKind.TypeSpecification)
                {
                    TypeSpecification specInner = reader.GetTypeSpecification((TypeSpecificationHandle)baseTypeHandle);
                    return specInner.DecodeSignature<Compatibility, object>(this, new object());
                }
                else if (baseTypeHandle.Kind == HandleKind.TypeDefinition)
                {
                    TypeDefinitionHandle handleInner = (TypeDefinitionHandle)baseTypeHandle;
                    if (handle != handleInner)
                        return GetTypeFromDefinition(reader, handleInner, rawTypeKind);
                }
            }
            catch (BadImageFormatException ex)
            {
                _log.LogMessage(MessageImportance.Low, ex.Message);
            }

            return Compatibility.Incompatible;
        }

        public Compatibility GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            if (rawTypeKind == 0x11 /*ELEMENT_TYPE_VALUETYPE*/)
            {
                TypeReference typeRef = reader.GetTypeReference(handle);
                EntityHandle scope = typeRef.ResolutionScope;

                if (scope.Kind == HandleKind.AssemblyReference)
                {
                    AssemblyReferenceHandle assyRefHandle = (AssemblyReferenceHandle)typeRef.ResolutionScope;
                    AssemblyReference assyRef = reader.GetAssemblyReference(assyRefHandle);

                    _inconclusive.Add(assyName: reader.GetString(assyRef.Name),
                        namespaceName: reader.GetString(typeRef.Namespace), typeName: reader.GetString(typeRef.Name));
                    return Compatibility.Inconclusive;
                }
                else
                {
                    throw new NotImplementedException(string.Format("Unsupported ResolutionScope kind '{0}' used in type {1}:{2}.",
                        scope.Kind.ToString(), reader.GetString(typeRef.Namespace), reader.GetString(typeRef.Name)));
                }
            }

            return Compatibility.Incompatible;
        }

        public Compatibility GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            TypeSpecification spec = reader.GetTypeSpecification((TypeSpecificationHandle)handle);
            return spec.DecodeSignature<Compatibility, object>(this, genericContext);
        }
    }
}
