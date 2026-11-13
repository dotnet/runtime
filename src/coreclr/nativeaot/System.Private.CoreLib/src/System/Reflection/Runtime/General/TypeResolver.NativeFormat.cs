// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.TypeInfos;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.General
{
    internal static partial class TypeResolver
    {
        //
        // Main routine to resolve a typeDef/Ref/Spec.
        //
        internal static RuntimeTypeInfo Resolve(this Handle typeDefRefOrSpec, MetadataReader reader, TypeContext typeContext)
        {
            Exception? exception = null;
            RuntimeTypeInfo? runtimeType = typeDefRefOrSpec.TryResolve(reader, typeContext, ref exception);
            if (runtimeType == null)
                throw exception!;
            return runtimeType;
        }

        internal static RuntimeTypeInfo? TryResolve(this Handle typeDefRefOrSpec, MetadataReader reader, TypeContext typeContext, ref Exception? exception)
        {
            HandleType handleType = typeDefRefOrSpec.HandleType;
            if (handleType == HandleType.TypeDefinition)
                return typeDefRefOrSpec.ToTypeDefinitionHandle(reader).ResolveTypeDefinition(reader);
            else if (handleType == HandleType.TypeReference)
                return typeDefRefOrSpec.ToTypeReferenceHandle(reader).TryResolveTypeReference(reader, ref exception);
            else if (handleType == HandleType.TypeSpecification)
                return typeDefRefOrSpec.ToTypeSpecificationHandle(reader).GetTypeSpecification(reader).Signature.TryResolve(typeContext, ref exception);
            else
                throw new BadImageFormatException();  // Expected TypeRef, Def or Spec.
        }

        internal static RuntimeTypeInfo Resolve(this TypeSignature signature, TypeContext typeContext)
        {
            Exception? exception = null;
            RuntimeTypeInfo? runtimeType = signature.TryResolve(typeContext, ref exception);
            if (runtimeType is null)
                throw exception!;
            return runtimeType;
        }

        //
        // Main routine to resolve a typeDefinition.
        //
        internal static RuntimeTypeInfo ResolveTypeDefinition(this TypeDefinitionHandle typeDefinitionHandle, MetadataReader reader)
        {
            return typeDefinitionHandle.GetNamedType(reader);
        }

        //
        // Resolve ECMA-335 II.23.1.16 signature elements with native handles for named types.
        //
        internal static RuntimeTypeInfo? TryResolve(this TypeSignature signature, TypeContext typeContext, ref Exception? exception)
        {
            if (signature.IsHandle)
                return signature.Handle.TryResolve(signature.Reader, typeContext, ref exception);

            SignatureReader reader = signature.GetReader();
            return TryResolveTypeSignature(ref reader, signature.Reader, typeContext, ref exception);
        }

        private static RuntimeTypeInfo? TryResolveTypeSignature(ref SignatureReader reader, MetadataReader metadataReader, TypeContext typeContext, ref Exception? exception)
        {
            while (reader.TypeCode is SignatureTypeCode.RequiredModifier or SignatureTypeCode.OptionalModifier)
            {
                reader.ReadByte();
                reader.ReadTypeHandle();
            }

            switch ((SignatureTypeCode)reader.ReadByte())
            {
                case SignatureTypeCode.Void:
                    return typeof(void).ToRuntimeTypeInfo();
                case SignatureTypeCode.Boolean:
                    return typeof(bool).ToRuntimeTypeInfo();
                case SignatureTypeCode.Char:
                    return typeof(char).ToRuntimeTypeInfo();
                case SignatureTypeCode.SByte:
                    return typeof(sbyte).ToRuntimeTypeInfo();
                case SignatureTypeCode.Byte:
                    return typeof(byte).ToRuntimeTypeInfo();
                case SignatureTypeCode.Int16:
                    return typeof(short).ToRuntimeTypeInfo();
                case SignatureTypeCode.UInt16:
                    return typeof(ushort).ToRuntimeTypeInfo();
                case SignatureTypeCode.Int32:
                    return typeof(int).ToRuntimeTypeInfo();
                case SignatureTypeCode.UInt32:
                    return typeof(uint).ToRuntimeTypeInfo();
                case SignatureTypeCode.Int64:
                    return typeof(long).ToRuntimeTypeInfo();
                case SignatureTypeCode.UInt64:
                    return typeof(ulong).ToRuntimeTypeInfo();
                case SignatureTypeCode.Single:
                    return typeof(float).ToRuntimeTypeInfo();
                case SignatureTypeCode.Double:
                    return typeof(double).ToRuntimeTypeInfo();
                case SignatureTypeCode.String:
                    return typeof(string).ToRuntimeTypeInfo();
                case SignatureTypeCode.TypedReference:
                    return typeof(TypedReference).ToRuntimeTypeInfo();
                case SignatureTypeCode.IntPtr:
                    return typeof(IntPtr).ToRuntimeTypeInfo();
                case SignatureTypeCode.UIntPtr:
                    return typeof(UIntPtr).ToRuntimeTypeInfo();
                case SignatureTypeCode.Object:
                    return typeof(object).ToRuntimeTypeInfo();

                case SignatureTypeCode.Class:
                case SignatureTypeCode.ValueType:
                    return reader.ReadTypeHandle().TryResolve(metadataReader, typeContext, ref exception);

                case SignatureTypeCode.Array:
                {
                    RuntimeTypeInfo? elementType = TryResolveTypeSignature(ref reader, metadataReader, typeContext, ref exception);
                    if (elementType is null)
                        return null;
                    int rank = reader.ReadCompressedInteger();
                    if (rank <= 0)
                        throw new BadImageFormatException(); // Bad rank.
                    int sizes = reader.ReadCompressedInteger();
                    for (int i = 0; i < sizes; i++)
                        reader.ReadCompressedInteger();
                    int lowerBounds = reader.ReadCompressedInteger();
                    for (int i = 0; i < lowerBounds; i++)
                        reader.ReadCompressedSignedInteger();
                    return elementType.GetMultiDimArrayType(rank);
                }

                case SignatureTypeCode.ByReference:
                {
                    RuntimeTypeInfo? targetType = TryResolveTypeSignature(ref reader, metadataReader, typeContext, ref exception);
                    if (targetType is null)
                        return null;
                    return targetType.GetByRefType();
                }

                case SignatureTypeCode.GenericMethodParameter:
                    return typeContext.GenericMethodArguments[reader.ReadCompressedInteger()];

                case SignatureTypeCode.Pointer:
                {
                    RuntimeTypeInfo? targetType = TryResolveTypeSignature(ref reader, metadataReader, typeContext, ref exception);
                    if (targetType is null)
                        return null;
                    return targetType.GetPointerType();
                }

                case SignatureTypeCode.FunctionPointer:
                {
                    byte header = reader.ReadMethodHeader(out _, out int count);
                    RuntimeTypeInfo? returnType = TryResolveTypeSignature(ref reader, metadataReader, typeContext, ref exception);
                    if (returnType is null)
                        return null;
                    var parameterTypes = new RuntimeTypeInfo[count];
                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        if (reader.TypeCode == SignatureTypeCode.Sentinel)
                            reader.ReadByte();
                        RuntimeTypeInfo? parameterType = TryResolveTypeSignature(ref reader, metadataReader, typeContext, ref exception);
                        if (parameterType is null)
                            return null;
                        parameterTypes[i] = parameterType;
                    }
                    bool isUnmanaged = ((Internal.Metadata.NativeFormat.SignatureCallingConvention)header & Internal.Metadata.NativeFormat.SignatureCallingConvention.UnmanagedCallingConventionMask) != 0;
                    return RuntimeFunctionPointerTypeInfo.GetFunctionPointerTypeInfo(returnType, parameterTypes, isUnmanaged);
                }

                case SignatureTypeCode.SZArray:
                {
                    RuntimeTypeInfo? elementType = TryResolveTypeSignature(ref reader, metadataReader, typeContext, ref exception);
                    if (elementType is null)
                        return null;
                    return elementType.GetArrayType();
                }

                case SignatureTypeCode.GenericTypeInstance:
                {
                    RuntimeTypeInfo? genericTypeDefinition = TryResolveTypeSignature(ref reader, metadataReader, typeContext, ref exception);
                    if (genericTypeDefinition is null)
                        return null;
                    int count = reader.ReadCompressedInteger();
                    var genericTypeArguments = new RuntimeTypeInfo[count];
                    for (int i = 0; i < genericTypeArguments.Length; i++)
                    {
                        RuntimeTypeInfo? genericTypeArgument = TryResolveTypeSignature(ref reader, metadataReader, typeContext, ref exception);
                        if (genericTypeArgument is null)
                            return null;
                        genericTypeArguments[i] = genericTypeArgument;
                    }
                    return genericTypeDefinition.GetConstructedGenericTypeNoConstraintCheck(genericTypeArguments);
                }

                case SignatureTypeCode.GenericTypeParameter:
                    return typeContext.GenericTypeArguments[reader.ReadCompressedInteger()];

                default:
                    throw new BadImageFormatException(); // Unexpected type signature element.
            }
        }

        //
        // Main routine to resolve a typeReference.
        //
        private static RuntimeTypeInfo? TryResolveTypeReference(this TypeReferenceHandle typeReferenceHandle, MetadataReader reader, ref Exception? exception)
        {
            TypeReference typeReference = typeReferenceHandle.GetTypeReference(reader);
            string name = typeReference.TypeName.GetString(reader);
            Handle parent = typeReference.NamespaceOrEnclosingType;
            HandleType parentType = parent.HandleType;
            RuntimeTypeInfo? outerTypeInfo = null;

            // Check if this is a reference to a nested type.

            if (parentType == HandleType.TypeDefinition)
            {
                outerTypeInfo = parent.ToTypeDefinitionHandle(reader).GetNamedType(reader);
            }
            else if (parentType == HandleType.TypeReference)
            {
                RuntimeTypeInfo? outerType = parent.ToTypeReferenceHandle(reader).TryResolveTypeReference(reader, ref exception);
                if (outerType == null)
                    return null;
                outerTypeInfo = outerType;   // Since we got to outerType via a metadata reference, we're assured GetTypeInfo() won't throw a missing metadata exception.
            }
            if (outerTypeInfo != null)
            {
                // It was a nested type. We've already resolved the containing type recursively - just find the nested among its direct children.
                Type? resolvedType = outerTypeInfo.GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (resolvedType == null)
                {
                    exception = Helpers.CreateTypeLoadException(outerTypeInfo.FullName + "+" + name, outerTypeInfo.Assembly.FullName);
                    return null;
                }
                return resolvedType.ToRuntimeTypeInfo();
            }


            // If we got here, the typeReference was to a non-nested type.
            if (parentType == HandleType.NamespaceReference)
            {
                NamespaceReferenceHandle namespaceReferenceHandle = parent.ToNamespaceReferenceHandle(reader);
                string fullName = namespaceReferenceHandle.ToFullyQualifiedTypeName(name, reader);
                ScopeReferenceHandle scopeReferenceHandle = namespaceReferenceHandle.GetNamespaceReference(reader).Scope;

                RuntimeAssemblyName assemblyName = scopeReferenceHandle.ToRuntimeAssemblyName(reader);
                RuntimeAssemblyInfo runtimeAssembly;
                exception = RuntimeAssemblyInfo.TryGetRuntimeAssembly(assemblyName, out runtimeAssembly);
                if (exception != null)
                    return null;
                Type runtimeType = runtimeAssembly.GetTypeCore(fullName, throwOnError: false, ignoreCase: false);
                if (runtimeType == null)
                {
                    exception = Helpers.CreateTypeLoadException(fullName, assemblyName.FullName);
                    return null;
                }
                return runtimeType.ToRuntimeTypeInfo();
            }

            throw new BadImageFormatException(); // Expected TypeReference parent to be typeRef, typeDef or namespaceRef.
        }
    }
}
