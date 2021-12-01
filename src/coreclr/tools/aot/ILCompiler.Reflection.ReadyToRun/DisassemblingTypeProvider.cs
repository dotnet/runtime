// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    public class DisassemblingGenericContext
    {
        public DisassemblingGenericContext(string[] typeParameters, string[] methodParameters)
        {
            MethodParameters = methodParameters;
            TypeParameters = typeParameters;
        }

        public string[] MethodParameters { get; }
        public string[] TypeParameters { get; }
    }

    public abstract class StringTypeProviderBase<TGenericContext> : ISignatureTypeProvider<string, TGenericContext>
    {
        public virtual string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Void:
                    return "void";

                case PrimitiveTypeCode.Boolean:
                    return "bool";

                case PrimitiveTypeCode.Char:
                    return "char";

                case PrimitiveTypeCode.SByte:
                    return "sbyte";

                case PrimitiveTypeCode.Byte:
                    return "byte";

                case PrimitiveTypeCode.Int16:
                    return "short";

                case PrimitiveTypeCode.UInt16:
                    return "ushort";

                case PrimitiveTypeCode.Int32:
                    return "int";

                case PrimitiveTypeCode.UInt32:
                    return "uint";

                case PrimitiveTypeCode.Int64:
                    return "long";

                case PrimitiveTypeCode.UInt64:
                    return "ulong";

                case PrimitiveTypeCode.Single:
                    return "float";

                case PrimitiveTypeCode.Double:
                    return "double";

                case PrimitiveTypeCode.String:
                    return "string";

                case PrimitiveTypeCode.TypedReference:
                    return "typedbyref";

                case PrimitiveTypeCode.IntPtr:
                    return "IntPtr";

                case PrimitiveTypeCode.UIntPtr:
                    return "UIntPtr";

                case PrimitiveTypeCode.Object:
                    return "object";

                default:
                    throw new NotImplementedException();
            }
        }

        public virtual string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind = 0)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }

        public virtual string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind = 0)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }

        public virtual string GetSZArrayType(string elementType)
        {
            return elementType + "[]";
        }

        public virtual string GetPointerType(string elementType)
        {
            return elementType + "*";
        }

        public virtual string GetByReferenceType(string elementType)
        {
            return "ref " + elementType;
        }

        public virtual string GetPinnedType(string elementType)
        {
            return elementType + " pinned";
        }

        public virtual string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            return genericType + "<" + String.Join(",", typeArguments) + ">";
        }

        public virtual string GetArrayType(string elementType, ArrayShape shape)
        {
            var builder = new StringBuilder();

            builder.Append(elementType);
            builder.Append('[');

            for (int i = 0; i < shape.Rank; i++)
            {
                int lowerBound = 0;

                if (i < shape.LowerBounds.Length)
                {
                    lowerBound = shape.LowerBounds[i];
                    builder.Append(lowerBound);
                }

                builder.Append("...");

                if (i < shape.Sizes.Length)
                {
                    builder.Append(lowerBound + shape.Sizes[i] - 1);
                }

                if (i < shape.Rank - 1)
                {
                    builder.Append(',');
                }
            }

            builder.Append(']');

            return builder.ToString();
        }

        public virtual string GetModifiedType(string modifierType, string unmodifiedType, bool isRequired)
        {
            return unmodifiedType + (isRequired ? " modreq(" : " modopt(") + modifierType + ")";
        }

        public virtual string GetFunctionPointerType(MethodSignature<string> signature)
        {
            ImmutableArray<string> parameterTypes = signature.ParameterTypes;

            int requiredParameterCount = signature.RequiredParameterCount;

            var builder = new StringBuilder();
            builder.Append("method ");
            builder.Append(signature.ReturnType);
            builder.Append(" *(");

            int i;
            for (i = 0; i < requiredParameterCount; i++)
            {
                builder.Append(parameterTypes[i]);
                if (i < parameterTypes.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            if (i < parameterTypes.Length)
            {
                builder.Append("..., ");
                for (; i < parameterTypes.Length; i++)
                {
                    builder.Append(parameterTypes[i]);
                    if (i < parameterTypes.Length - 1)
                    {
                        builder.Append(", ");
                    }
                }
            }

            builder.Append(')');
            return builder.ToString();
        }

        public abstract string GetGenericMethodParameter(TGenericContext genericContext, int index);
        public abstract string GetGenericTypeParameter(TGenericContext genericContext, int index);
        public abstract string GetTypeFromSpecification(MetadataReader reader, TGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind);
    }

    // Test implementation of ISignatureTypeProvider<TType, TGenericContext> that uses strings in ilasm syntax as TType.
    // A real provider in any sort of perf constraints would not want to allocate strings freely like this, but it keeps test code simple.
    public class DisassemblingTypeProvider : StringTypeProviderBase<DisassemblingGenericContext>
    {
        public override string GetGenericMethodParameter(DisassemblingGenericContext genericContext, int index)
        {
            if (genericContext.MethodParameters == null || index >= genericContext.MethodParameters.Length)
            {
                return "!!" + index.ToString();
            }
            return genericContext.MethodParameters[index];
        }

        public override string GetGenericTypeParameter(DisassemblingGenericContext genericContext, int index)
        {
            if (genericContext.TypeParameters == null || index >= genericContext.TypeParameters.Length)
            {
                return "!" + index.ToString();
            }
            return genericContext.TypeParameters[index];
        }

        public override string GetTypeFromSpecification(MetadataReader reader, DisassemblingGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }

        public string GetTypeFromHandle(MetadataReader reader, DisassemblingGenericContext genericContext, EntityHandle handle)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }
    }

}
