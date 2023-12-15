// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.NativeFormat;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.NativeFormat;
#if ECMA_METADATA_SUPPORT
using System.Reflection.Runtime.TypeInfos.EcmaFormat;
using System.Reflection.Runtime.MethodInfos.EcmaFormat;
#endif
using System.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.Runtime;

namespace Internal.Reflection.Core.Execution
{
    //
    // This singleton class acts as an entrypoint from System.Private.Reflection.Execution to System.Private.Reflection.Core.
    //
    [CLSCompliant(false)]
    public static class ExecutionDomain
    {
        //
        // Retrieves the MethodBase for a given method handle. Helper to implement Delegate.GetMethodInfo()
        //
        public static MethodBase GetMethod(RuntimeTypeHandle declaringTypeHandle, QMethodDefinition methodHandle, RuntimeTypeHandle[] genericMethodTypeArgumentHandles)
        {
            RuntimeTypeInfo contextTypeInfo = declaringTypeHandle.GetRuntimeTypeInfoForRuntimeTypeHandle();
            RuntimeNamedMethodInfo? runtimeNamedMethodInfo = null;

            if (methodHandle.IsNativeFormatMetadataBased)
            {
                MethodHandle nativeFormatMethodHandle = methodHandle.NativeFormatHandle;
                NativeFormatRuntimeNamedTypeInfo definingTypeInfo = contextTypeInfo.AnchoringTypeDefinitionForDeclaredMembers.CastToNativeFormatRuntimeNamedTypeInfo();
                MetadataReader reader = definingTypeInfo.Reader;
                if (nativeFormatMethodHandle.IsConstructor(reader))
                {
                    return RuntimePlainConstructorInfo<NativeFormatMethodCommon>.GetRuntimePlainConstructorInfo(new NativeFormatMethodCommon(nativeFormatMethodHandle, definingTypeInfo, contextTypeInfo));
                }
                else
                {
                    // RuntimeMethodHandles always yield methods whose ReflectedType is the DeclaringType.
                    RuntimeTypeInfo reflectedType = contextTypeInfo;
                    runtimeNamedMethodInfo = RuntimeNamedMethodInfo<NativeFormatMethodCommon>.GetRuntimeNamedMethodInfo(new NativeFormatMethodCommon(nativeFormatMethodHandle, definingTypeInfo, contextTypeInfo), reflectedType);
                }
            }
#if ECMA_METADATA_SUPPORT
            else
            {
                System.Reflection.Metadata.MethodDefinitionHandle ecmaFormatMethodHandle = methodHandle.EcmaFormatHandle;
                EcmaFormatRuntimeNamedTypeInfo definingEcmaTypeInfo = contextTypeInfo.AnchoringTypeDefinitionForDeclaredMembers.CastToEcmaFormatRuntimeNamedTypeInfo();
                System.Reflection.Metadata.MetadataReader reader = definingEcmaTypeInfo.Reader;
                if (ecmaFormatMethodHandle.IsConstructor(reader))
                {
                    return RuntimePlainConstructorInfo<EcmaFormatMethodCommon>.GetRuntimePlainConstructorInfo(new EcmaFormatMethodCommon(ecmaFormatMethodHandle, definingEcmaTypeInfo, contextTypeInfo));
                }
                else
                {
                    // RuntimeMethodHandles always yield methods whose ReflectedType is the DeclaringType.
                    RuntimeTypeInfo reflectedType = contextTypeInfo;
                    runtimeNamedMethodInfo = RuntimeNamedMethodInfo<EcmaFormatMethodCommon>.GetRuntimeNamedMethodInfo(new EcmaFormatMethodCommon(ecmaFormatMethodHandle, definingEcmaTypeInfo, contextTypeInfo), reflectedType);
                }
            }
#endif

            if (!runtimeNamedMethodInfo.IsGenericMethod || genericMethodTypeArgumentHandles == null)
            {
                return runtimeNamedMethodInfo;
            }
            else
            {
                RuntimeTypeInfo[] genericTypeArguments = new RuntimeTypeInfo[genericMethodTypeArgumentHandles.Length];
                for (int i = 0; i < genericMethodTypeArgumentHandles.Length; i++)
                {
                    genericTypeArguments[i] = genericMethodTypeArgumentHandles[i].GetRuntimeTypeInfoForRuntimeTypeHandle();
                }
                return RuntimeConstructedGenericMethodInfo.GetRuntimeConstructedGenericMethodInfo(runtimeNamedMethodInfo, genericTypeArguments);
            }
        }

        //=======================================================================================
        // This group of methods jointly service the Type.GetTypeFromHandle() path. The caller
        // is responsible for analyzing the RuntimeTypeHandle to figure out which flavor to call.
        //=======================================================================================
        internal static RuntimeTypeInfo GetNamedTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            QTypeDefinition qTypeDefinition = ReflectionCoreExecution.ExecutionEnvironment.GetMetadataForNamedType(typeHandle);
#if ECMA_METADATA_SUPPORT
            if (qTypeDefinition.IsNativeFormatMetadataBased)
#endif
            {
                return qTypeDefinition.NativeFormatHandle.GetNamedType(qTypeDefinition.NativeFormatReader, typeHandle);
            }
#if ECMA_METADATA_SUPPORT
            else
            {
                return System.Reflection.Runtime.TypeInfos.EcmaFormat.EcmaFormatRuntimeNamedTypeInfo.GetRuntimeNamedTypeInfo(qTypeDefinition.EcmaFormatReader,
                    qTypeDefinition.EcmaFormatHandle,
                    typeHandle);
            }
#endif
        }

        internal static unsafe RuntimeTypeInfo GetRuntimeTypeInfo(MethodTable* pEEType)
        {
            Debug.Assert(pEEType != null);

            RuntimeTypeInfo runtimeTypeInfo;

            if (pEEType->IsDefType)
            {
                if (pEEType->IsGeneric)
                {
                    runtimeTypeInfo = GetConstructedGenericTypeForHandle(pEEType);
                }
                else
                {
                    runtimeTypeInfo = GetNamedTypeForHandle(new RuntimeTypeHandle(pEEType));
                }
            }
            else if (pEEType->IsArray)
            {
                runtimeTypeInfo = RuntimeArrayTypeInfo.GetArrayTypeInfo(
                    new RuntimeTypeHandle(pEEType->RelatedParameterType).GetRuntimeTypeInfoForRuntimeTypeHandle(),
                    multiDim: !pEEType->IsSzArray, rank: pEEType->ArrayRank,
                    precomputedTypeHandle: new RuntimeTypeHandle(pEEType));
            }
            else if (pEEType->IsPointer)
            {
                runtimeTypeInfo = RuntimePointerTypeInfo.GetPointerTypeInfo(
                    new RuntimeTypeHandle(pEEType->RelatedParameterType).GetRuntimeTypeInfoForRuntimeTypeHandle(),
                    precomputedTypeHandle: new RuntimeTypeHandle(pEEType));
            }
            else if (pEEType->IsByRef)
            {
                runtimeTypeInfo = RuntimeByRefTypeInfo.GetByRefTypeInfo(
                    new RuntimeTypeHandle(pEEType->RelatedParameterType).GetRuntimeTypeInfoForRuntimeTypeHandle(),
                    precomputedTypeHandle: new RuntimeTypeHandle(pEEType));
            }
            else if (pEEType->IsFunctionPointer)
            {
                runtimeTypeInfo = GetFunctionPointerTypeForHandle(pEEType);
            }
            else
            {
                Debug.Fail("Invalid RuntimeTypeHandle");
                throw new ArgumentException(SR.Arg_InvalidHandle);
            }

            return runtimeTypeInfo;

            static RuntimeTypeInfo GetConstructedGenericTypeForHandle(MethodTable* pEEType)
            {
                RuntimeTypeInfo[] genericTypeArguments = new RuntimeTypeInfo[pEEType->GenericArity];
                MethodTableList arguments = pEEType->GenericArguments;
                for (int i = 0; i < genericTypeArguments.Length; i++)
                {
                    genericTypeArguments[i] = new RuntimeTypeHandle(arguments[i]).GetRuntimeTypeInfoForRuntimeTypeHandle();
                }

                return RuntimeConstructedGenericTypeInfo.GetRuntimeConstructedGenericTypeInfo(
                    new RuntimeTypeHandle(pEEType->GenericDefinition).GetRuntimeTypeInfoForRuntimeTypeHandle(),
                    genericTypeArguments,
                    precomputedTypeHandle: new RuntimeTypeHandle(pEEType));
            }

            static RuntimeTypeInfo GetFunctionPointerTypeForHandle(MethodTable* pEEType)
            {
                RuntimeTypeInfo[] parameterTypes;

                uint count = pEEType->NumFunctionPointerParameters;
                if (count == 0)
                {
                    parameterTypes = Array.Empty<RuntimeTypeInfo>();
                }
                else
                {
                    parameterTypes = new RuntimeTypeInfo[count];
                    MethodTableList parameters = pEEType->FunctionPointerParameters;
                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        parameterTypes[i] = new RuntimeTypeHandle(parameters[i]).GetRuntimeTypeInfoForRuntimeTypeHandle();
                    }
                }

                return RuntimeFunctionPointerTypeInfo.GetFunctionPointerTypeInfo(
                    new RuntimeTypeHandle(pEEType->FunctionPointerReturnType).GetRuntimeTypeInfoForRuntimeTypeHandle(),
                    parameterTypes,
                    pEEType->IsUnmanagedFunctionPointer,
                    precomputedTypeHandle: new RuntimeTypeHandle(pEEType));
            }
        }
    }
}
