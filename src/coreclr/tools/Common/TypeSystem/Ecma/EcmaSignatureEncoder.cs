// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;

namespace Internal.TypeSystem.Ecma
{
    public interface IEntityHandleProvider
    {
        /// Implement to allow EcmaSignatureEncoder to encode types that need metadata references to be resolved.
        /// only used to express non-generic references
        EntityHandle GetTypeDefOrRefHandleForTypeDesc(TypeDesc type);
    }

    public class EcmaSignatureEncoder<TEntityHandleProvider> where TEntityHandleProvider : IEntityHandleProvider
    {
        private TEntityHandleProvider _entityHandleProvider;

        public EcmaSignatureEncoder(TEntityHandleProvider entityHandleProvider)
        {
            _entityHandleProvider = entityHandleProvider;
        }

        public void EncodeMethodSignature(BlobBuilder methodSignatureBlob, MethodSignature signature)
        {
            BlobEncoder encoder = new BlobEncoder(methodSignatureBlob);

            MethodSignatureEncoder methodSigEncoder = encoder.MethodSignature(
                SignatureCallingConvention.Default, signature.GenericParameterCount, !signature.IsStatic);

            ReturnTypeEncoder returnTypeEncoder;
            ParametersEncoder parametersEncoder;
            methodSigEncoder.Parameters(signature.Length, out returnTypeEncoder, out parametersEncoder);

            // Return Type Sig
            EncodeTypeSignature(returnTypeEncoder.Type(), signature.ReturnType);

            // Parameter Types Sig
            for (int i = 0; i < signature.Length; i++)
                EncodeTypeSignature(parametersEncoder.AddParameter().Type(), signature[i]);
        }

        public void EncodeTypeSignature(SignatureTypeEncoder encoder, TypeDesc type)
        {
            if (type is RuntimeDeterminedType)
            {
                EncodeTypeSignature(encoder, ((RuntimeDeterminedType)type).RuntimeDeterminedDetailsType);
                return;
            }

            switch (type.Category)
            {
                case TypeFlags.Boolean:
                    encoder.Boolean(); break;
                case TypeFlags.Byte:
                    encoder.Byte(); break;
                case TypeFlags.SByte:
                    encoder.SByte(); break;
                case TypeFlags.Char:
                    encoder.Char(); break;
                case TypeFlags.Int16:
                    encoder.Int16(); break;
                case TypeFlags.UInt16:
                    encoder.UInt16(); break;
                case TypeFlags.Int32:
                    encoder.Int32(); break;
                case TypeFlags.UInt32:
                    encoder.UInt32(); break;
                case TypeFlags.Int64:
                    encoder.Int64(); break;
                case TypeFlags.UInt64:
                    encoder.UInt64(); break;
                case TypeFlags.Single:
                    encoder.Single(); break;
                case TypeFlags.Double:
                    encoder.Double(); break;
                case TypeFlags.IntPtr:
                    encoder.IntPtr(); break;
                case TypeFlags.UIntPtr:
                    encoder.UIntPtr(); break;
                case TypeFlags.Void:
                    encoder.Builder.WriteByte((byte)PrimitiveTypeCode.Void);
                    break;

                case TypeFlags.SignatureTypeVariable:
                    encoder.GenericTypeParameter(((SignatureVariable)type).Index);
                    break;

                case TypeFlags.SignatureMethodVariable:
                    encoder.GenericMethodTypeParameter(((SignatureMethodVariable)type).Index);
                    break;

                case TypeFlags.GenericParameter:
                    {
                        var genericTypeDesc = (GenericParameterDesc)type;
                        if (genericTypeDesc.Kind == GenericParameterKind.Type)
                            encoder.GenericTypeParameter(genericTypeDesc.Index);
                        else
                            encoder.GenericMethodTypeParameter(genericTypeDesc.Index);
                    }
                    break;

                case TypeFlags.FunctionPointer:
                    {
                        FunctionPointerType fptrType = (FunctionPointerType)type;
                        encoder.FunctionPointer(
                            SignatureCallingConvention.Default,
                            fptrType.Signature.IsStatic ? default(FunctionPointerAttributes) : FunctionPointerAttributes.HasThis,
                            fptrType.Signature.GenericParameterCount);

                        // Return Type Sig
                        EncodeTypeSignature(encoder, fptrType.Signature.ReturnType);

                        // Parameter Types Sig
                        for (int i = 0; i < fptrType.Signature.Length; i++)
                            EncodeTypeSignature(encoder, fptrType.Signature[i]);
                    }
                    break;

                case TypeFlags.Array:
                    {
                        // Skip bounds and lobounds (TODO)
                        ImmutableArray<int> bounds = ImmutableArray.Create<int>();
                        ImmutableArray<int> lowerBounds = ImmutableArray.Create<int>();
                        encoder.Array(
                            elementType => EncodeTypeSignature(elementType, ((ArrayType)type).ElementType),
                            arrayShape => arrayShape.Shape(((ArrayType)type).Rank, bounds, lowerBounds));
                    }
                    break;

                case TypeFlags.SzArray:
                    encoder.SZArray();
                    EncodeTypeSignature(encoder, ((ParameterizedType)type).ParameterType);
                    break;

                case TypeFlags.ByRef:
                    encoder.Builder.WriteByte((byte)SignatureTypeCode.ByReference);
                    EncodeTypeSignature(encoder, ((ParameterizedType)type).ParameterType);
                    break;

                case TypeFlags.Pointer:
                    encoder.Builder.WriteByte((byte)SignatureTypeCode.Pointer);
                    EncodeTypeSignature(encoder, ((ParameterizedType)type).ParameterType);
                    break;

                case TypeFlags.Enum:
                case TypeFlags.Class:
                case TypeFlags.ValueType:
                case TypeFlags.Interface:
                case TypeFlags.Nullable:
                    {
                        if (type == type.Context.GetWellKnownType(WellKnownType.TypedReference))
                            encoder.Builder.WriteByte((byte)PrimitiveTypeCode.TypedReference);
                        else if (type == type.Context.GetWellKnownType(WellKnownType.Object))
                            encoder.PrimitiveType(PrimitiveTypeCode.Object);
                        else if (type == type.Context.GetWellKnownType(WellKnownType.String))
                            encoder.PrimitiveType(PrimitiveTypeCode.String);
                        else if (type.HasInstantiation && !type.IsGenericDefinition)
                        {
                            encoder.GenericInstantiation(_entityHandleProvider.GetTypeDefOrRefHandleForTypeDesc(type.GetTypeDefinition()), type.Instantiation.Length, type.IsValueType);

                            for (int i = 0; i < type.Instantiation.Length; i++)
                                EncodeTypeSignature(encoder, type.Instantiation[i]);
                        }
                        else
                        {
                            encoder.Type(_entityHandleProvider.GetTypeDefOrRefHandleForTypeDesc(type), type.IsValueType);
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException("Attempting to encode an invalid type signature.");
            }
        }
    }
}
