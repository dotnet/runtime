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
                return typeDefRefOrSpec.ToTypeSpecificationHandle(reader).GetTypeSpecification(reader).Signature.TryResolve(reader, typeContext, ref exception);
            else
                throw new BadImageFormatException();  // Expected TypeRef, Def or Spec.
        }

        internal static RuntimeTypeInfo Resolve(this SigPointer signature, MetadataReader reader, TypeContext typeContext)
        {
            SigParser sp = new SigParser(signature);
            return Resolve(ref sp, reader, typeContext);
        }

        // Resolves the type at the current position and advances the parser past it.
        internal static RuntimeTypeInfo Resolve(ref SigParser sp, MetadataReader reader, TypeContext typeContext)
        {
            Exception? exception = null;
            RuntimeTypeInfo? runtimeType = TryResolveTypeSignature(ref sp, reader, typeContext, ref exception);
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
        internal static RuntimeTypeInfo? TryResolve(this SigPointer signature, MetadataReader reader, TypeContext typeContext, ref Exception? exception)
        {
            SigParser sp = new SigParser(signature);
            return TryResolveTypeSignature(ref sp, reader, typeContext, ref exception);
        }

        private static RuntimeTypeInfo? TryResolveTypeSignature(ref SigParser sp, MetadataReader reader, TypeContext typeContext, ref Exception? exception)
        {
            switch (sp.GetElemType())
            {
                case CorElementType.ELEMENT_TYPE_VOID:
                    return typeof(void).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return typeof(bool).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return typeof(char).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_I1:
                    return typeof(sbyte).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_U1:
                    return typeof(byte).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_I2:
                    return typeof(short).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_U2:
                    return typeof(ushort).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_I4:
                    return typeof(int).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_U4:
                    return typeof(uint).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_I8:
                    return typeof(long).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_U8:
                    return typeof(ulong).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_R4:
                    return typeof(float).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_R8:
                    return typeof(double).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_STRING:
                    return typeof(string).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_TYPEDBYREF:
                    return typeof(TypedReference).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_I:
                    return typeof(IntPtr).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_U:
                    return typeof(UIntPtr).ToRuntimeTypeInfo();
                case CorElementType.ELEMENT_TYPE_OBJECT:
                    return typeof(object).ToRuntimeTypeInfo();

                case CorElementType.ELEMENT_TYPE_CLASS:
                case CorElementType.ELEMENT_TYPE_VALUETYPE:
                    return sp.GetToken().TryResolve(reader, typeContext, ref exception);

                case CorElementType.ELEMENT_TYPE_ARRAY:
                {
                    RuntimeTypeInfo? elementType = TryResolveTypeSignature(ref sp, reader, typeContext, ref exception);
                    if (elementType is null)
                        return null;
                    int rank = sp.GetArrayShape();
                    if (rank <= 0)
                        throw new BadImageFormatException(); // Bad rank.
                    return elementType.GetMultiDimArrayType(rank);
                }

                case CorElementType.ELEMENT_TYPE_BYREF:
                {
                    RuntimeTypeInfo? targetType = TryResolveTypeSignature(ref sp, reader, typeContext, ref exception);
                    if (targetType is null)
                        return null;
                    return targetType.GetByRefType();
                }

                case CorElementType.ELEMENT_TYPE_MVAR:
                    return typeContext.GenericMethodArguments[sp.GetData()];

                case CorElementType.ELEMENT_TYPE_PTR:
                {
                    RuntimeTypeInfo? targetType = TryResolveTypeSignature(ref sp, reader, typeContext, ref exception);
                    if (targetType is null)
                        return null;
                    return targetType.GetPointerType();
                }

                case CorElementType.ELEMENT_TYPE_FNPTR:
                {
                    // Same header validation as CoreCLR's SigPointer::GetTypeHandleThrowing.
                    MdSigCallingConvention callConv = sp.GetCallingConvInfo();
                    if ((callConv & MdSigCallingConvention.CallConvMask) == MdSigCallingConvention.Field
                        || (callConv & MdSigCallingConvention.Generic) != 0)
                        throw new BadImageFormatException();
                    int count = sp.GetData();
                    RuntimeTypeInfo? returnType = TryResolveTypeSignature(ref sp, reader, typeContext, ref exception);
                    if (returnType is null)
                        return null;
                    var parameterTypes = new RuntimeTypeInfo[count];
                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        RuntimeTypeInfo? parameterType = TryResolveTypeSignature(ref sp, reader, typeContext, ref exception);
                        if (parameterType is null)
                            return null;
                        parameterTypes[i] = parameterType;
                    }
                    bool isUnmanaged = (callConv & MdSigCallingConvention.CallConvMask) != 0;
                    return RuntimeFunctionPointerTypeInfo.GetFunctionPointerTypeInfo(returnType, parameterTypes, isUnmanaged);
                }

                case CorElementType.ELEMENT_TYPE_SZARRAY:
                {
                    RuntimeTypeInfo? elementType = TryResolveTypeSignature(ref sp, reader, typeContext, ref exception);
                    if (elementType is null)
                        return null;
                    return elementType.GetArrayType();
                }

                case CorElementType.ELEMENT_TYPE_GENERICINST:
                {
                    RuntimeTypeInfo? genericTypeDefinition = TryResolveTypeSignature(ref sp, reader, typeContext, ref exception);
                    if (genericTypeDefinition is null)
                        return null;
                    int count = sp.GetData();
                    var genericTypeArguments = new RuntimeTypeInfo[count];
                    for (int i = 0; i < genericTypeArguments.Length; i++)
                    {
                        RuntimeTypeInfo? genericTypeArgument = TryResolveTypeSignature(ref sp, reader, typeContext, ref exception);
                        if (genericTypeArgument is null)
                            return null;
                        genericTypeArguments[i] = genericTypeArgument;
                    }
                    return genericTypeDefinition.GetConstructedGenericTypeNoConstraintCheck(genericTypeArguments);
                }

                case CorElementType.ELEMENT_TYPE_VAR:
                    return typeContext.GenericTypeArguments[sp.GetData()];

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
