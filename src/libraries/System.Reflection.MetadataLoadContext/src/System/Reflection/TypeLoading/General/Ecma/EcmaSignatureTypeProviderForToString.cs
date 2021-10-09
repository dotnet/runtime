// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

namespace System.Reflection.TypeLoading.Ecma
{
    //
    // This SignatureProvider is used to generate the Type portion of ToString() values for fields, methods, parameters, events
    // and properties.
    //
    // Though this may seem like something that belongs at the format-agnostic layer, it is not acceptable for ToString() to
    // trigger resolving. Thus, ToString() must be built up using only the raw data in the metadata and without creating or
    // resolving Type objects.
    //
    // Compat: Since this a new library, we are choosing not to carry forth the ancient oddities of runtime
    // Reflection's ToString() ("ByRef" instead of "&", "Int32" rather than "System.Int32"), etc.
    //
    internal sealed class EcmaSignatureTypeProviderForToString : ISignatureTypeProvider<string, TypeContext>
    {
        public static readonly EcmaSignatureTypeProviderForToString Instance = new EcmaSignatureTypeProviderForToString();

        private EcmaSignatureTypeProviderForToString() { }

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => handle.ToTypeString(reader);
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => handle.ToTypeString(reader);
        public string GetTypeFromSpecification(MetadataReader reader, TypeContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => handle.ToTypeString(reader, genericContext);

        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetArrayType(string elementType, ArrayShape shape) => elementType + Helpers.ComputeArraySuffix(shape.Rank, multiDim: true);
        public string GetByReferenceType(string elementType) => elementType + "&";
        public string GetPointerType(string elementType) => elementType + "*";

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(genericType);
            sb.Append('[');
            for (int i = 0; i < typeArguments.Length; i++)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append(typeArguments[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        public string GetGenericTypeParameter(TypeContext genericContext, int index) => genericContext.GetGenericTypeArgumentOrNull(index)?.ToString() ?? ("!" + index);
        public string GetGenericMethodParameter(TypeContext genericContext, int index) => genericContext.GetGenericMethodArgumentOrNull(index)?.ToString() ?? ("!!" + index);

        public string GetFunctionPointerType(MethodSignature<string> signature) => "?";
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
        public string GetPinnedType(string elementType) => elementType;

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            typeCode.ToCoreType().GetFullName(out ReadOnlySpan<byte> ns, out ReadOnlySpan<byte> name);
            return ns.ToUtf16() + "." + name.ToUtf16();  // This is not safe for types outside of a namespace, but all primitive types are known to be in "System"
        }
    }
}
