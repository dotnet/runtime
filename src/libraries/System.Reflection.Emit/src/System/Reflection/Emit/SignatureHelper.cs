// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Emit
{
    internal static class MetadataSignatureHelper
    {
        internal static BlobBuilder GetLocalSignature(List<LocalBuilder> locals, ModuleBuilderImpl module)
        {
            BlobBuilder localSignature = new();
            LocalVariablesEncoder encoder = new BlobEncoder(localSignature).LocalVariableSignature(locals.Count);

            foreach (LocalBuilder local in locals)
            {
                WriteSignatureForType(encoder.AddVariable().Type(local.LocalType.IsByRef, local.IsPinned),
                    local.LocalType.IsByRef ? local.LocalType.GetElementType()! : local.LocalType, module);
            }

            return localSignature;
        }

        internal static BlobBuilder GetFieldSignature(Type fieldType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, ModuleBuilderImpl module)
        {
            BlobBuilder fieldSignature = new();
            FieldTypeEncoder encoder = new BlobEncoder(fieldSignature).Field();
            WriteReturnTypeCustomModifiers(encoder.CustomModifiers(), requiredCustomModifiers, optionalCustomModifiers, module);
            WriteSignatureForType(encoder.Type(), fieldType, module);

            return fieldSignature;
        }

        internal static BlobBuilder GetConstructorSignature(ParameterInfo[]? parameters, ModuleBuilderImpl module)
        {
            BlobBuilder constructorSignature = new();

            parameters ??= Array.Empty<ParameterInfo>();

            new BlobEncoder(constructorSignature).
                MethodSignature(isInstanceMethod: true).
                Parameters(parameters.Length, out ReturnTypeEncoder retType, out ParametersEncoder parameterEncoder);

            retType.Void();

            WriteParametersSignature(module, Array.ConvertAll(parameters, p => p.ParameterType), parameterEncoder);

            return constructorSignature;
        }

        internal static BlobBuilder GetTypeSpecificationSignature(Type type, ModuleBuilderImpl module)
        {
            BlobBuilder typeSpecSignature = new();
            WriteSignatureForType(new BlobEncoder(typeSpecSignature).TypeSpecificationSignature(), type, module);

            return typeSpecSignature;
        }

        internal static BlobBuilder GetMethodSpecificationSignature(Type[] genericArguments, ModuleBuilderImpl module)
        {
            BlobBuilder methodSpecSignature = new();
            GenericTypeArgumentsEncoder encoder = new BlobEncoder(methodSpecSignature).MethodSpecificationSignature(genericArguments.Length);

            foreach (Type argument in genericArguments)
            {
                WriteSignatureForType(encoder.AddArgument(), argument, module);
            }

            return methodSpecSignature;
        }

        internal static BlobBuilder GetMethodSignature(ModuleBuilderImpl module, Type[]? parameters, Type? returnType, SignatureCallingConvention convention,
            int genParamCount = 0, bool isInstance = false, Type[]? optionalParameterTypes = null, Type[]? returnTypeRequiredModifiers = null,
            Type[]? returnTypeOptionalModifiers = null, Type[][]? parameterRequiredModifiers = null, Type[][]? parameterOptionalModifiers = null)
        {
            BlobBuilder methodSignature = new();

            int paramsLength = ((parameters == null) ? 0 : parameters.Length) + ((optionalParameterTypes == null) ? 0 : optionalParameterTypes.Length);

            new BlobEncoder(methodSignature).MethodSignature(convention, genParamCount, isInstance).
                    Parameters(paramsLength, out ReturnTypeEncoder retEncoder, out ParametersEncoder parEncoder);

            WriteReturnTypeCustomModifiers(retEncoder.CustomModifiers(), returnTypeRequiredModifiers, returnTypeOptionalModifiers, module);

            if (returnType != null && returnType != module.GetTypeFromCoreAssembly(CoreTypeId.Void))
            {
                WriteSignatureForType(retEncoder.Type(), returnType, module);
            }
            else
            {
                retEncoder.Void();
            }

            WriteParametersSignature(module, parameters, parEncoder, parameterRequiredModifiers, parameterOptionalModifiers);

            if (optionalParameterTypes != null && optionalParameterTypes.Length != 0)
            {
                WriteParametersSignature(module, optionalParameterTypes, parEncoder.StartVarArgs());
            }

            return methodSignature;
        }

        private static void WriteReturnTypeCustomModifiers(CustomModifiersEncoder encoder,
            Type[]? requiredModifiers, Type[]? optionalModifiers, ModuleBuilderImpl module)
        {
            if (requiredModifiers != null)
            {
                WriteCustomModifiers(encoder, requiredModifiers, isOptional: false, module);
            }

            if (optionalModifiers != null)
            {
                WriteCustomModifiers(encoder, optionalModifiers, isOptional: true, module);
            }
        }

        private static void WriteCustomModifiers(CustomModifiersEncoder encoder, Type[] customModifiers, bool isOptional, ModuleBuilderImpl module)
        {
            foreach (Type modifier in customModifiers)
            {
                encoder.AddModifier(module.GetTypeHandle(modifier), isOptional);
            }
        }

        private static void WriteParametersSignature(ModuleBuilderImpl module, Type[]? parameters,
            ParametersEncoder parameterEncoder, Type[][]? requiredModifiers = null, Type[][]? optionalModifiers = null)
        {
            if (parameters != null) // If parameters null, just keep the ParametersEncoder empty
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterTypeEncoder encoder = parameterEncoder.AddParameter();

                    if (requiredModifiers != null && requiredModifiers.Length > i && requiredModifiers[i] != null)
                    {
                        WriteCustomModifiers(encoder.CustomModifiers(), requiredModifiers[i], isOptional: false, module);
                    }

                    if (optionalModifiers != null && optionalModifiers.Length > i && optionalModifiers[i] != null)
                    {
                        WriteCustomModifiers(encoder.CustomModifiers(), optionalModifiers[i], isOptional: true, module);
                    }

                    WriteSignatureForType(encoder.Type(), parameters[i], module);
                }
            }
        }

        internal static BlobBuilder GetPropertySignature(PropertyBuilderImpl property, ModuleBuilderImpl module)
        {
            BlobBuilder propertySignature = new();

            new BlobEncoder(propertySignature).
                PropertySignature(isInstanceProperty: property.CallingConventions.HasFlag(CallingConventions.HasThis)).
                Parameters(property.ParameterTypes == null ? 0 : property.ParameterTypes.Length, out ReturnTypeEncoder retType, out ParametersEncoder paramEncoder);

            WriteReturnTypeCustomModifiers(retType.CustomModifiers(), property._returnTypeRequiredCustomModifiers, property._returnTypeOptionalCustomModifiers, module);
            WriteSignatureForType(retType.Type(), property.PropertyType, module);
            WriteParametersSignature(module, property.ParameterTypes, paramEncoder, property._parameterTypeRequiredCustomModifiers, property._parameterTypeOptionalCustomModifiers);

            return propertySignature;
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
