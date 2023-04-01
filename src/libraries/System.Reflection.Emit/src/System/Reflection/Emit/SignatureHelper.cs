// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Emit
{
    // TODO: Only support simple signatures. More complex signatures will be added.
    internal static class MetadataSignatureHelper
    {
        internal static BlobBuilder FieldSignatureEncoder(Type fieldType)
        {
            BlobBuilder fieldSignature = new();

            WriteSignatureTypeForReflectionType(new BlobEncoder(fieldSignature).FieldSignature(), fieldType);

            return fieldSignature;
        }

        internal static BlobBuilder MethodSignatureEncoder(ModuleBuilderImpl _module, Type[]? parameters, Type? returnType, bool isInstance)
        {
            // Encoding return type and parameters.
            BlobBuilder methodSignature = new();

            ParametersEncoder parEncoder;
            ReturnTypeEncoder retEncoder;

            new BlobEncoder(methodSignature).
                MethodSignature(isInstanceMethod: isInstance).
                Parameters((parameters == null) ? 0 : parameters.Length, out retEncoder, out parEncoder);

            if (returnType != null && returnType != _module.GetTypeFromCoreAssembly("System.Void"))
            {
                WriteSignatureTypeForReflectionType(retEncoder.Type(), returnType);
            }
            else // If null mark ReturnTypeEncoder as void
            {
                retEncoder.Void();
            }

            if (parameters != null) // If parameters null, just keep the ParametersEncoder empty
            {
                foreach (Type parameter in parameters)
                {
                    WriteSignatureTypeForReflectionType(parEncoder.AddParameter().Type(), parameter);
                }
            }

            return methodSignature;
        }

        private static void WriteSignatureTypeForReflectionType(SignatureTypeEncoder signature, Type type)
        {
            // We need to translate from Reflection.Type to SignatureTypeEncoder. Most common types for proof of concept. More types will be added.
            // TODO: This switch should be done by comparing Type objects, without fetching FullName.
            switch (type.FullName)
            {
                case "System.Boolean":
                    signature.Boolean();
                    break;
                case "System.Byte":
                    signature.Byte();
                    break;
                case "System.Char":
                    signature.Char();
                    break;
                case "System.Double":
                    signature.Double();
                    break;
                case "System.Int32":
                    signature.Int32();
                    break;
                case "System.Int64":
                    signature.Int64();
                    break;
                case "System.Object":
                    signature.Object();
                    break;
                case "System.String":
                    signature.String();
                    break;

                default: throw new NotSupportedException(SR.Format(SR.NotSupported_Signature, type.FullName));
            }
        }
    }
}
