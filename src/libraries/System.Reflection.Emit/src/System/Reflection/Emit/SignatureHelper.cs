// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Emit
{
    // TODO: Only support simple signatures. More complex signatures (generics, array, byref, pointers etc) will be added.
    internal static class MetadataSignatureHelper
    {
        internal static BlobBuilder FieldSignatureEncoder(Type fieldType, ModuleBuilderImpl module)
        {
            BlobBuilder fieldSignature = new();
            WriteSignatureForType(new BlobEncoder(fieldSignature).Field().Type(), fieldType, module);

            return fieldSignature;
        }

        internal static BlobBuilder ConstructorSignatureEncoder(ParameterInfo[]? parameters, ModuleBuilderImpl module)
        {
            BlobBuilder constructorSignature = new();

            new BlobEncoder(constructorSignature).
                MethodSignature(isInstanceMethod: true).
                Parameters((parameters == null) ? 0 : parameters.Length, out ReturnTypeEncoder retType, out ParametersEncoder parameterEncoder);

            retType.Void();

            if (parameters != null)
            {
                Type[]? typeParameters = Array.ConvertAll(parameters, parameter => parameter.ParameterType);

                foreach (Type parameter in typeParameters)
                {
                    WriteSignatureForType(parameterEncoder.AddParameter().Type(), parameter, module);
                }
            }

            return constructorSignature;
        }

        internal static BlobBuilder MethodSignatureEncoder(ModuleBuilderImpl module, Type[]? parameters,
            Type? returnType, SignatureCallingConvention convention, int genParamCount, bool isInstance)
        {
            // Encoding return type and parameters.
            BlobBuilder methodSignature = new();

            new BlobEncoder(methodSignature).
                MethodSignature(convention: convention, genericParameterCount: genParamCount, isInstanceMethod: isInstance).
                Parameters((parameters == null) ? 0 : parameters.Length, out ReturnTypeEncoder retEncoder, out ParametersEncoder parEncoder);

            if (returnType != null && returnType != module.GetTypeFromCoreAssembly(CoreTypeId.Void))
            {
                WriteSignatureForType(retEncoder.Type(), returnType, module);
            }
            else // If null mark ReturnTypeEncoder as void
            {
                retEncoder.Void();
            }

            if (parameters != null) // If parameters null, just keep the ParametersEncoder empty
            {
                foreach (Type parameter in parameters)
                {
                    WriteSignatureForType(parEncoder.AddParameter().Type(), parameter, module);
                }
            }

            return methodSignature;
        }

        private static void WriteSignatureForType(SignatureTypeEncoder signature, Type type, ModuleBuilderImpl module)
        {
            if (type.IsArray)
            {
                Type elementType = type.GetElementType()!;
                int rank = type.GetArrayRank();
                if (rank == 1)
                {
                    WriteSignatureForType(signature.SZArray(), elementType, module);
                }
                else
                {
                    signature.Array(out SignatureTypeEncoder elTypeSignature, out ArrayShapeEncoder arrayEncoder);
                    WriteSimpleSignature(elTypeSignature, elementType, module);
                    arrayEncoder.Shape(type.GetArrayRank(), ImmutableArray.Create<int>(), ImmutableArray.Create<int>(new int[rank]));
                }
            }
            else if (type.IsPointer)
            {
                WriteSignatureForType(signature.Pointer(), type.GetElementType()!, module);
            }
            else if (type.IsByRef)
            {
                signature.Builder.WriteByte((byte)SignatureTypeCode.ByReference);
                WriteSignatureForType(signature, type.GetElementType()!, module);
            }
            else if (type.IsGenericType)
            {
                Type[] genericArguments = type.GetGenericArguments();

                GenericTypeArgumentsEncoder encoder = signature.GenericInstantiation(
                    module.GetTypeHandle(type.GetGenericTypeDefinition()), genericArguments.Length, type.IsValueType);
                foreach (Type gType in genericArguments)
                {
                    if (gType.IsGenericMethodParameter)
                    {
                        encoder.AddArgument().GenericMethodTypeParameter(gType.GenericParameterPosition);
                    }
                    else if (gType.IsGenericParameter)
                    {
                        encoder.AddArgument().GenericTypeParameter(gType.GenericParameterPosition);
                    }
                    else
                    {
                        WriteSignatureForType(encoder.AddArgument(), gType, module);
                    }
                }
            }
            else if (type.IsGenericMethodParameter)
            {
                signature.GenericMethodTypeParameter(type.GenericParameterPosition);
            }
            else if (type.IsGenericParameter)
            {
                signature.GenericTypeParameter(type.GenericParameterPosition);
            }
            else
            {
                WriteSimpleSignature(signature, type, module);
            }
        }

        private static void WriteSimpleSignature(SignatureTypeEncoder signature, Type type, ModuleBuilderImpl module)
        {
            CoreTypeId? typeId = module.GetTypeIdFromCoreTypes(type);

            switch (typeId)
            {
                case CoreTypeId.Void:
                    signature.Builder.WriteByte((byte)SignatureTypeCode.Void);
                    return;
                case CoreTypeId.Boolean:
                    signature.Boolean();
                    return;
                case CoreTypeId.Byte:
                    signature.Byte();
                    return;
                case CoreTypeId.SByte:
                    signature.SByte();
                    return;
                case CoreTypeId.Char:
                    signature.Char();
                    return;
                case CoreTypeId.Int16:
                    signature.Int16();
                    return;
                case CoreTypeId.UInt16:
                    signature.UInt16();
                    return;
                case CoreTypeId.Int32:
                    signature.Int32();
                    return;
                case CoreTypeId.UInt32:
                    signature.UInt32();
                    return;
                case CoreTypeId.Int64:
                    signature.Int64();
                    return;
                case CoreTypeId.UInt64:
                    signature.UInt64();
                    return;
                case CoreTypeId.Single:
                    signature.Single();
                    return;
                case CoreTypeId.Double:
                    signature.Double();
                    return;
                case CoreTypeId.IntPtr:
                    signature.IntPtr();
                    return;
                case CoreTypeId.UIntPtr:
                    signature.UIntPtr();
                    return;
                case CoreTypeId.Object:
                    signature.Object();
                    return;
                case CoreTypeId.String:
                    signature.String();
                    return;
                case CoreTypeId.TypedReference:
                    signature.TypedReference();
                    return;
            }

            EntityHandle typeHandle = module.GetTypeHandle(type);
            signature.Type(typeHandle, type.IsValueType);
        }
    }

    internal enum CoreTypeId
    {
        Void,
        Object,
        Boolean,
        Char,
        SByte,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,
        String,
        IntPtr,
        UIntPtr,
        TypedReference,
    }
}
