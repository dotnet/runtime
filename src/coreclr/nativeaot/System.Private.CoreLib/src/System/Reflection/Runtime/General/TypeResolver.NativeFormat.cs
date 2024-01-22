// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.TypeInfos;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core.Execution;

using NativeFormatModifiedType = global::Internal.Metadata.NativeFormat.ModifiedType;

namespace System.Reflection.Runtime.General
{
    internal static partial class TypeResolver
    {
        //
        // Main routine to resolve a typeDef/Ref/Spec. Also accepts ModifiedTypes (will unwrap and ignore the custom modifiers.)
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
                return typeDefRefOrSpec.ToTypeSpecificationHandle(reader).TryResolveTypeSignature(reader, typeContext, ref exception);
            else if (handleType == HandleType.ModifiedType)
            {
                NativeFormatModifiedType modifiedType = typeDefRefOrSpec.ToModifiedTypeHandle(reader).GetModifiedType(reader);
                return modifiedType.Type.TryResolve(reader, typeContext, ref exception);
            }
            else
                throw new BadImageFormatException();  // Expected TypeRef, Def or Spec.
        }


        //
        // Main routine to resolve a typeDefinition.
        //
        internal static RuntimeTypeInfo ResolveTypeDefinition(this TypeDefinitionHandle typeDefinitionHandle, MetadataReader reader)
        {
            return typeDefinitionHandle.GetNamedType(reader);
        }

        //
        // Main routine to parse a metadata type specification signature.
        //
        private static RuntimeTypeInfo? TryResolveTypeSignature(this TypeSpecificationHandle typeSpecHandle, MetadataReader reader, TypeContext typeContext, ref Exception? exception)
        {
            Handle typeHandle = typeSpecHandle.GetTypeSpecification(reader).Signature;
            switch (typeHandle.HandleType)
            {
                case HandleType.ArraySignature:
                    {
                        ArraySignature sig = typeHandle.ToArraySignatureHandle(reader).GetArraySignature(reader);
                        int rank = sig.Rank;
                        if (rank <= 0)
                            throw new BadImageFormatException(); // Bad rank.
                        RuntimeTypeInfo? elementType = sig.ElementType.TryResolve(reader, typeContext, ref exception);
                        if (elementType == null)
                            return null;
                        return elementType.GetMultiDimArrayType(rank);
                    }

                case HandleType.ByReferenceSignature:
                    {
                        ByReferenceSignature sig = typeHandle.ToByReferenceSignatureHandle(reader).GetByReferenceSignature(reader);
                        RuntimeTypeInfo? targetType = sig.Type.TryResolve(reader, typeContext, ref exception);
                        if (targetType == null)
                            return null;
                        return targetType.GetByRefType();
                    }

                case HandleType.MethodTypeVariableSignature:
                    {
                        MethodTypeVariableSignature sig = typeHandle.ToMethodTypeVariableSignatureHandle(reader).GetMethodTypeVariableSignature(reader);
                        return typeContext.GenericMethodArguments[sig.Number];
                    }

                case HandleType.PointerSignature:
                    {
                        PointerSignature sig = typeHandle.ToPointerSignatureHandle(reader).GetPointerSignature(reader);
                        RuntimeTypeInfo? targetType = sig.Type.TryResolve(reader, typeContext, ref exception);
                        if (targetType == null)
                            return null;
                        return targetType.GetPointerType();
                    }

                case HandleType.FunctionPointerSignature:
                    {
                        FunctionPointerSignature sig = typeHandle.ToFunctionPointerSignatureHandle(reader).GetFunctionPointerSignature(reader);
                        MethodSignature methodSig = sig.Signature.GetMethodSignature(reader);
                        RuntimeTypeInfo? returnType = methodSig.ReturnType.TryResolve(reader, typeContext, ref exception);
                        if (returnType == null)
                            return null;
                        var parameterTypes = new RuntimeTypeInfo[methodSig.Parameters.Count];
                        int i = 0;
                        foreach (Handle paramTypeHandle in methodSig.Parameters)
                        {
                            RuntimeTypeInfo? parameterType = paramTypeHandle.TryResolve(reader, typeContext, ref exception);
                            if (parameterType == null)
                                return null;
                            parameterTypes[i++] = parameterType;
                        }
                        bool isUnmanaged = (methodSig.CallingConvention & Internal.Metadata.NativeFormat.SignatureCallingConvention.UnmanagedCallingConventionMask) != 0;
                        return RuntimeFunctionPointerTypeInfo.GetFunctionPointerTypeInfo(returnType, parameterTypes, isUnmanaged);
                    }

                case HandleType.SZArraySignature:
                    {
                        SZArraySignature sig = typeHandle.ToSZArraySignatureHandle(reader).GetSZArraySignature(reader);
                        RuntimeTypeInfo? elementType = sig.ElementType.TryResolve(reader, typeContext, ref exception);
                        if (elementType == null)
                            return null;
                        return elementType.GetArrayType();
                    }

                case HandleType.TypeDefinition:
                    {
                        return typeHandle.ToTypeDefinitionHandle(reader).ResolveTypeDefinition(reader);
                    }

                case HandleType.TypeInstantiationSignature:
                    {
                        TypeInstantiationSignature sig = typeHandle.ToTypeInstantiationSignatureHandle(reader).GetTypeInstantiationSignature(reader);
                        RuntimeTypeInfo? genericTypeDefinition = sig.GenericType.TryResolve(reader, typeContext, ref exception);
                        if (genericTypeDefinition == null)
                            return null;
                        LowLevelList<RuntimeTypeInfo> genericTypeArguments = new LowLevelList<RuntimeTypeInfo>();
                        foreach (Handle genericTypeArgumentHandle in sig.GenericTypeArguments)
                        {
                            RuntimeTypeInfo? genericTypeArgument = genericTypeArgumentHandle.TryResolve(reader, typeContext, ref exception);
                            if (genericTypeArgument == null)
                                return null;
                            genericTypeArguments.Add(genericTypeArgument);
                        }
                        return genericTypeDefinition.GetConstructedGenericTypeNoConstraintCheck(genericTypeArguments.ToArray());
                    }

                case HandleType.TypeReference:
                    {
                        return typeHandle.ToTypeReferenceHandle(reader).TryResolveTypeReference(reader, ref exception);
                    }

                case HandleType.TypeVariableSignature:
                    {
                        TypeVariableSignature sig = typeHandle.ToTypeVariableSignatureHandle(reader).GetTypeVariableSignature(reader);
                        return typeContext.GenericTypeArguments[sig.Number];
                    }

                default:
                    throw new NotSupportedException(); // Unexpected Type signature type.
            }
        }

        //
        // Main routine to resolve a typeReference.
        //
        private static RuntimeTypeInfo? TryResolveTypeReference(this TypeReferenceHandle typeReferenceHandle, MetadataReader reader, ref Exception? exception)
        {
            TypeReference typeReference = typeReferenceHandle.GetTypeReference(reader);
            string name = typeReference.TypeName.GetString(reader);
            Handle parent = typeReference.ParentNamespaceOrType;
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
                    exception = Helpers.CreateTypeLoadException(outerTypeInfo.FullName + "+" + name, outerTypeInfo.Assembly);
                    return null;
                }
                return resolvedType.ToRuntimeTypeInfo();
            }


            // If we got here, the typeReference was to a non-nested type.
            if (parentType == HandleType.NamespaceReference)
            {
                NamespaceReferenceHandle namespaceReferenceHandle = parent.ToNamespaceReferenceHandle(reader);
                string fullName = namespaceReferenceHandle.ToFullyQualifiedTypeName(name, reader);
                Handle parentHandleToSearch = parent;

                while (parentHandleToSearch.HandleType != HandleType.ScopeReference)
                {
                    parentHandleToSearch = parentHandleToSearch.ToNamespaceReferenceHandle(reader).GetNamespaceReference(reader).ParentScopeOrNamespace;
                }
                ScopeReferenceHandle scopeReferenceHandle = parentHandleToSearch.ToScopeReferenceHandle(reader);

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
