using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

namespace WebAssemblyInfo
{
    public class SignatureDecoder : ISignatureTypeProvider<string, GenericContext>
    {
        static string GetShapeRank(ArrayShape shape)
        {
            StringBuilder sb = new();

            for (int i = 0; i < shape.Rank; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                int lower = 0;
                if (shape.LowerBounds.Length < i)
                {
                    lower = shape.LowerBounds[i];
                    sb.Append(lower);
                }

                sb.Append("...");

                if (i < shape.Sizes.Length)
                {
                    sb.Append(shape.Sizes[i] + lower - 1);
                }
            }

            return sb.ToString();
        }

        public string GetArrayType(string elementType, ArrayShape shape)
        {
            var ranks = shape.Rank > 0 ? GetShapeRank(shape) : "";

            return $"{elementType}[{ranks}]";
        }

        public string GetByReferenceType(string elementType)
        {
            return $"{elementType}&";
        }

        static string GetParameterTypes(MethodSignature<string> signature)
        {
            StringBuilder sb = new();
            var count = signature.RequiredParameterCount;
            var pTypes = signature.ParameterTypes;
            var pCount = pTypes.Length;

            for (int i = 0; i < pCount; i++)
            {
                if (i > 0)
                    sb.Append(i == count ? "..., " : ", ");

                sb.Append(pTypes[i]);
            }

            return sb.ToString();
        }

        public string GetFunctionPointerType(MethodSignature<string> signature)
        {
            return $"method {signature.ReturnType} *({GetParameterTypes(signature)})";
        }

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            return $"{genericType}<{string.Join(',', typeArguments)}>";
        }

        public string GetGenericMethodParameter(GenericContext genericContext, int index)
        {
            var reader = genericContext.Reader;

            return $"!!{reader.GetString(reader.GetGenericParameter(genericContext.Parameters[index]).Name)}";
        }

        public string GetGenericTypeParameter(GenericContext genericContext, int index)
        {
            var reader = genericContext.Reader;
            return $"!{reader.GetString(reader.GetGenericParameter(genericContext.TypeParameters[index]).Name)}";
        }

        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
        {
            var modStr = isRequired ? "modReq" : "modOpt";

            return $"{unmodifiedType}{modStr}({modifier})";
        }

        public string GetPinnedType(string elementType)
        {
            return $"{elementType} pinned";
        }

        public string GetPointerType(string elementType)
        {
            return $"{elementType}*";
        }

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return typeCode switch
            {
                PrimitiveTypeCode.Boolean => "bool",
                PrimitiveTypeCode.Byte => "byte",
                PrimitiveTypeCode.Char => "char",
                PrimitiveTypeCode.Double => "double",
                PrimitiveTypeCode.Int16 => "short",
                PrimitiveTypeCode.Int32 => "int",
                PrimitiveTypeCode.Int64 => "long",
                PrimitiveTypeCode.IntPtr => "IntPtr",
                PrimitiveTypeCode.Object => "object",
                PrimitiveTypeCode.SByte => "sbyte",
                PrimitiveTypeCode.Single => "float",
                PrimitiveTypeCode.String => "string",
                PrimitiveTypeCode.TypedReference => "TypedReference",
                PrimitiveTypeCode.UInt16 => "ushort",
                PrimitiveTypeCode.UInt32 => "uint",
                PrimitiveTypeCode.UInt64 => "ulong",
                PrimitiveTypeCode.UIntPtr => "UIntPtr",
                PrimitiveTypeCode.Void => "void",
                _ => throw new ArgumentOutOfRangeException(nameof(typeCode)),
            };
        }

        public string GetSZArrayType(string elementType)
        {
            return $"{elementType}[]";
        }

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var td = reader.GetTypeDefinition(handle);
            var name = reader.GetString(td.Name);
            var fullName = td.Namespace.IsNil ? name : $"{reader.GetString(td.Namespace)}.{name}";

            return td.IsNested ? $"{GetTypeFromDefinition(reader, td.GetDeclaringType(), 0)}/{fullName}" : fullName;
        }

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var tr = reader.GetTypeReference(handle);
            var name = reader.GetString(tr.Name);

            // todo: scope
            return tr.Namespace.IsNil ? name : $"{reader.GetString(tr.Namespace)}.{name}";
        }

        public string GetTypeFromSpecification(MetadataReader reader, GenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        }
    }
}
