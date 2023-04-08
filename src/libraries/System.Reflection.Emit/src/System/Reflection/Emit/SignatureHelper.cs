// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Emit
{
    // TODO: Only support simple signatures. More complex signatures will be added.
    internal static class MetadataSignatureHelper
    {
        internal static BlobBuilder FieldSignatureEncoder(Type fieldType, ModuleBuilderImpl module)
        {
            BlobBuilder fieldSignature = new();

            WriteSignatureTypeForReflectionType(new BlobEncoder(fieldSignature).FieldSignature(), fieldType, module);

            return fieldSignature;
        }

        internal static BlobBuilder MethodSignatureEncoder(ModuleBuilderImpl module, Type[]? parameters, Type? returnType, bool isInstance)
        {
            // Encoding return type and parameters.
            BlobBuilder methodSignature = new();

            ParametersEncoder parEncoder;
            ReturnTypeEncoder retEncoder;

            new BlobEncoder(methodSignature).
                MethodSignature(isInstanceMethod: isInstance).
                Parameters((parameters == null) ? 0 : parameters.Length, out retEncoder, out parEncoder);

            if (returnType != null && returnType != module.GetTypeFromCoreAssembly(CoreTypeId.Void))
            {
                WriteSignatureTypeForReflectionType(retEncoder.Type(), returnType, module);
            }
            else // If null mark ReturnTypeEncoder as void
            {
                retEncoder.Void();
            }

            if (parameters != null) // If parameters null, just keep the ParametersEncoder empty
            {
                foreach (Type parameter in parameters)
                {
                    WriteSignatureTypeForReflectionType(parEncoder.AddParameter().Type(), parameter, module);
                }
            }

            return methodSignature;
        }

        private static void WriteSignatureTypeForReflectionType(SignatureTypeEncoder signature, Type type, ModuleBuilderImpl module)
        {
            CoreTypeId? typeId = module.GetTypeIdFromCoreTypes(type);

            // We need to translate from Reflection.Type to SignatureTypeEncoder.
            switch (typeId)
            {
                case CoreTypeId.Boolean:
                    signature.Boolean();
                    break;
                case CoreTypeId.Byte:
                    signature.Byte();
                    break;
                case CoreTypeId.SByte:
                    signature.SByte();
                    break;
                case CoreTypeId.Char:
                    signature.Char();
                    break;
                case CoreTypeId.Int16:
                    signature.Int16();
                    break;
                case CoreTypeId.UInt16:
                    signature.UInt16();
                    break;
                case CoreTypeId.Int32:
                    signature.Int32();
                    break;
                case CoreTypeId.UInt32:
                    signature.UInt32();
                    break;
                case CoreTypeId.Int64:
                    signature.Int64();
                    break;
                case CoreTypeId.UInt64:
                    signature.UInt64();
                    break;
                case CoreTypeId.Single:
                    signature.Single();
                    break;
                case CoreTypeId.Double:
                    signature.Double();
                    break;
                case CoreTypeId.IntPtr:
                    signature.IntPtr();
                    break;
                case CoreTypeId.UIntPtr:
                    signature.UIntPtr();
                    break;
                case CoreTypeId.Object:
                    signature.Object();
                    break;
                case CoreTypeId.String:
                    signature.String();
                    break;
                default:
                    throw new NotSupportedException(SR.Format(SR.NotSupported_Signature, type.FullName));
            }
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
    }
}
