// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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

            WriteSignatureForType(new BlobEncoder(fieldSignature).FieldSignature(), fieldType, module);

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
            CoreTypeId? typeId = module.GetTypeIdFromCoreTypes(type);

            switch (typeId)
            {
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
                    signature.Builder.WriteByte((byte)SignatureTypeCode.TypedReference);
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
