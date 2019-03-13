// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Helpers to generate ToString() output for Type objects that occur as part of MemberInfo objects. Not used to generate ToString() for
    /// System.Type itself.
    /// 
    /// Though this may seem like something that belongs at the format-agnostic layer, it is not acceptable for ToString() to
    /// trigger resolving. Thus, ToString() must be built up using only the raw data in the metadata and without creating or
    /// resolving Type objects.
    /// </summary>
    internal static class EcmaToStringHelpers
    {
        public static string ToTypeString(this EntityHandle handle, in TypeContext typeContext, MetadataReader reader)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert(reader != null);

            HandleKind kind = handle.Kind;
            switch (kind)
            {
                case HandleKind.TypeDefinition:
                    return ((TypeDefinitionHandle)handle).ToTypeString(reader);

                case HandleKind.TypeReference:
                    return ((TypeReferenceHandle)handle).ToTypeString(reader);

                case HandleKind.TypeSpecification:
                    return ((TypeSpecificationHandle)handle).ToTypeString(reader, typeContext);

                default:
                    Debug.Fail($"Invalid handle passed to ToTypeString: 0x{handle.GetToken():x8}");
                    return "?";
            }
        }

        public static string ToTypeString(this TypeDefinitionHandle handle, MetadataReader reader)
        {
            TypeDefinition td = handle.GetTypeDefinition(reader);
            string ns = td.Namespace.GetStringOrNull(reader) ?? string.Empty;
            string name = td.Name.GetString(reader);
            if (td.IsNested)
            {
                string declaringTypeName = td.GetDeclaringType().ToTypeString(reader);
                name = declaringTypeName + "+" + name;
            }
            return ns.AppendTypeName(name);
        }

        public static string ToTypeString(this TypeReferenceHandle handle, MetadataReader reader)
        {
            TypeReference tr = handle.GetTypeReference(reader);
            string ns = tr.Namespace.GetStringOrNull(reader) ?? string.Empty;
            string name = tr.Name.GetString(reader);
            if (tr.ResolutionScope.Kind == HandleKind.TypeDefinition || tr.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                string declaringTypeName = tr.ResolutionScope.ToTypeString(default, reader);
                name = declaringTypeName + "+" + name;
            }
            return ns.AppendTypeName(name);
        }

        public static string ToTypeString(this TypeSpecificationHandle handle, MetadataReader reader, in TypeContext typeContext)
        {
            return handle.GetTypeSpecification(reader).DecodeSignature(EcmaSignatureTypeProviderForToString.Instance, typeContext);
        }
    }
}
