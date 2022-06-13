// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.NativeFormat;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.NativeFormat;
#if ECMA_METADATA_SUPPORT
using System.Reflection.Runtime.TypeInfos.EcmaFormat;
using System.Reflection.Runtime.MethodInfos.EcmaFormat;
#endif
using System.Reflection.Runtime.TypeParsing;
using System.Reflection.Runtime.CustomAttributes;
using System.Runtime.CompilerServices;
using Internal.Metadata.NativeFormat;
using Internal.Runtime.Augments;

namespace Internal.Reflection.Core.Execution
{
    //
    // This singleton class acts as an entrypoint from System.Private.Reflection.Execution to System.Private.Reflection.Core.
    //
    [ReflectionBlocked]
    [CLSCompliant(false)]
    public sealed class ExecutionDomain
    {
        internal ExecutionDomain(ReflectionDomainSetup executionDomainSetup, ExecutionEnvironment executionEnvironment)
        {
            ExecutionEnvironment = executionEnvironment;
            ReflectionDomainSetup = executionDomainSetup;
        }

        //
        // Retrieves a type by name. Helper to implement Type.GetType();
        //
        public Type GetType(string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError, bool ignoreCase, IList<string> defaultAssemblyNames)
        {
            if (typeName == null)
                throw new ArgumentNullException();

            if (typeName.Length == 0)
            {
                if (throwOnError)
                    throw new TypeLoadException(SR.Arg_TypeLoadNullStr);
                else
                    return null;
            }

            TypeName parsedName = TypeParser.ParseAssemblyQualifiedTypeName(typeName, throwOnError: throwOnError);
            if (parsedName == null)
                return null;
            CoreAssemblyResolver coreAssemblyResolver = CreateCoreAssemblyResolver(assemblyResolver);
            CoreTypeResolver coreTypeResolver = CreateCoreTypeResolver(typeResolver, defaultAssemblyNames, throwOnError: throwOnError, ignoreCase: ignoreCase);
            GetTypeOptions getTypeOptions = new GetTypeOptions(coreAssemblyResolver, coreTypeResolver, throwOnError: throwOnError, ignoreCase: ignoreCase);

            return parsedName.ResolveType(null, getTypeOptions);
        }

        private static CoreAssemblyResolver CreateCoreAssemblyResolver(Func<AssemblyName, Assembly> assemblyResolver)
        {
            if (assemblyResolver == null)
            {
                return RuntimeAssemblyInfo.GetRuntimeAssemblyIfExists;
            }
            else
            {
                return delegate (RuntimeAssemblyName runtimeAssemblyName)
                {
                    AssemblyName assemblyName = runtimeAssemblyName.ToAssemblyName();
                    Assembly assembly = assemblyResolver(assemblyName);
                    return assembly;
                };
            }
        }

        private static CoreTypeResolver CreateCoreTypeResolver(Func<Assembly, string, bool, Type> typeResolver, IList<string> defaultAssemblyNames, bool throwOnError, bool ignoreCase)
        {
            if (typeResolver == null)
            {
                return delegate (Assembly containingAssemblyIfAny, string coreTypeName)
                {
                    if (containingAssemblyIfAny != null)
                    {
                        return containingAssemblyIfAny.GetTypeCore(coreTypeName, ignoreCase: ignoreCase);
                    }
                    else
                    {
                        foreach (string defaultAssemblyName in defaultAssemblyNames)
                        {
                            RuntimeAssemblyName runtimeAssemblyName = RuntimeAssemblyName.Parse(defaultAssemblyName);
                            RuntimeAssemblyInfo defaultAssembly = RuntimeAssemblyInfo.GetRuntimeAssemblyIfExists(runtimeAssemblyName);
                            if (defaultAssembly == null)
                                continue;
                            Type resolvedType = defaultAssembly.GetTypeCore(coreTypeName, ignoreCase: ignoreCase);
                            if (resolvedType != null)
                                return resolvedType;
                        }

                        if (throwOnError && defaultAssemblyNames.Count > 0)
                        {
                            // Though we don't have to throw a TypeLoadException exception (that's our caller's job), we can throw a more specific exception than he would so just do it.
                            throw Helpers.CreateTypeLoadException(coreTypeName, defaultAssemblyNames[0]);
                        }
                        return null;
                    }
                };
            }
            else
            {
                return delegate (Assembly containingAssemblyIfAny, string coreTypeName)
                {
                    string escapedName = coreTypeName.EscapeTypeNameIdentifier();
                    Type type = typeResolver(containingAssemblyIfAny, escapedName, ignoreCase);
                    return type;
                };
            }
        }

        //
        // Retrieves the MethodBase for a given method handle. Helper to implement Delegate.GetMethodInfo()
        //
        public MethodBase GetMethod(RuntimeTypeHandle declaringTypeHandle, QMethodDefinition methodHandle, RuntimeTypeHandle[] genericMethodTypeArgumentHandles)
        {
            RuntimeTypeInfo contextTypeInfo = declaringTypeHandle.GetTypeForRuntimeTypeHandle();
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
                    genericTypeArguments[i] = genericMethodTypeArgumentHandles[i].GetTypeForRuntimeTypeHandle();
                }
                return RuntimeConstructedGenericMethodInfo.GetRuntimeConstructedGenericMethodInfo(runtimeNamedMethodInfo, genericTypeArguments);
            }
        }

        //=======================================================================================
        // This group of methods jointly service the Type.GetTypeFromHandle() path. The caller
        // is responsible for analyzing the RuntimeTypeHandle to figure out which flavor to call.
        //=======================================================================================
        public Type GetNamedTypeForHandle(RuntimeTypeHandle typeHandle, bool isGenericTypeDefinition)
        {
            QTypeDefinition qTypeDefinition;

            if (ExecutionEnvironment.TryGetMetadataForNamedType(typeHandle, out qTypeDefinition))
            {
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
            else
            {
                if (ExecutionEnvironment.IsReflectionBlocked(typeHandle))
                {
                    return RuntimeBlockedTypeInfo.GetRuntimeBlockedTypeInfo(typeHandle, isGenericTypeDefinition);
                }
                else
                {
                    return RuntimeNoMetadataNamedTypeInfo.GetRuntimeNoMetadataNamedTypeInfo(typeHandle, isGenericTypeDefinition);
                }
            }
        }

        public Type GetArrayTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            RuntimeTypeHandle elementTypeHandle;
            if (!ExecutionEnvironment.TryGetArrayTypeElementType(typeHandle, out elementTypeHandle))
                throw CreateMissingMetadataException((Type?)null);

            return elementTypeHandle.GetTypeForRuntimeTypeHandle().GetArrayType(typeHandle);
        }

        public Type GetMdArrayTypeForHandle(RuntimeTypeHandle typeHandle, int rank)
        {
            RuntimeTypeHandle elementTypeHandle;
            if (!ExecutionEnvironment.TryGetArrayTypeElementType(typeHandle, out elementTypeHandle))
                throw CreateMissingMetadataException((Type?)null);

            return elementTypeHandle.GetTypeForRuntimeTypeHandle().GetMultiDimArrayType(rank, typeHandle);
        }

        public Type GetPointerTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            RuntimeTypeHandle targetTypeHandle;
            if (!ExecutionEnvironment.TryGetPointerTypeTargetType(typeHandle, out targetTypeHandle))
                throw CreateMissingMetadataException((Type?)null);

            return targetTypeHandle.GetTypeForRuntimeTypeHandle().GetPointerType(typeHandle);
        }

        public Type GetByRefTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            RuntimeTypeHandle targetTypeHandle;
            if (!ExecutionEnvironment.TryGetByRefTypeTargetType(typeHandle, out targetTypeHandle))
                throw CreateMissingMetadataException((Type?)null);

            return targetTypeHandle.GetTypeForRuntimeTypeHandle().GetByRefType(typeHandle);
        }

        public Type GetConstructedGenericTypeForHandle(RuntimeTypeHandle typeHandle)
        {
            RuntimeTypeHandle genericTypeDefinitionHandle;
            RuntimeTypeHandle[] genericTypeArgumentHandles;
            genericTypeDefinitionHandle = RuntimeAugments.GetGenericInstantiation(typeHandle, out genericTypeArgumentHandles);

            // Reflection blocked constructed generic types simply pretend to not be generic
            // This is reasonable, as the behavior of reflection blocked types is supposed
            // to be that they expose the minimal information about a type that is necessary
            // for users of Object.GetType to move from that type to a type that isn't
            // reflection blocked. By not revealing that reflection blocked types are generic
            // we are making it appear as if implementation detail types exposed to user code
            // are all non-generic, which is theoretically possible, and by doing so
            // we avoid (in all known circumstances) the very complicated case of representing
            // the interfaces, base types, and generic parameter types of reflection blocked
            // generic type definitions.
            if (ExecutionEnvironment.IsReflectionBlocked(genericTypeDefinitionHandle))
            {
                return RuntimeBlockedTypeInfo.GetRuntimeBlockedTypeInfo(typeHandle, isGenericTypeDefinition: false);
            }

            RuntimeTypeInfo genericTypeDefinition = genericTypeDefinitionHandle.GetTypeForRuntimeTypeHandle();
            int count = genericTypeArgumentHandles.Length;
            RuntimeTypeInfo[] genericTypeArguments = new RuntimeTypeInfo[count];
            for (int i = 0; i < count; i++)
            {
                genericTypeArguments[i] = genericTypeArgumentHandles[i].GetTypeForRuntimeTypeHandle();
            }
            return genericTypeDefinition.GetConstructedGenericType(genericTypeArguments, typeHandle);
        }

        //=======================================================================================
        // MissingMetadataExceptions.
        //=======================================================================================
        public Exception CreateMissingMetadataException(Type? pertainant)
        {
            return this.ReflectionDomainSetup.CreateMissingMetadataException(pertainant);
        }

        public Exception CreateMissingMetadataException(TypeInfo? pertainant)
        {
            return this.ReflectionDomainSetup.CreateMissingMetadataException(pertainant);
        }

        public Exception CreateMissingMetadataException(TypeInfo pertainant, string nestedTypeName)
        {
            return this.ReflectionDomainSetup.CreateMissingMetadataException(pertainant, nestedTypeName);
        }

        public Exception CreateNonInvokabilityException(MemberInfo pertainant)
        {
            return this.ReflectionDomainSetup.CreateNonInvokabilityException(pertainant);
        }

        public Exception CreateMissingArrayTypeException(Type elementType, bool isMultiDim, int rank)
        {
            return ReflectionDomainSetup.CreateMissingArrayTypeException(elementType, isMultiDim, rank);
        }

        public Exception CreateMissingConstructedGenericTypeException(Type genericTypeDefinition, Type[] genericTypeArguments)
        {
            return ReflectionDomainSetup.CreateMissingConstructedGenericTypeException(genericTypeDefinition, genericTypeArguments);
        }

        //=======================================================================================
        // Miscellaneous.
        //=======================================================================================
        public RuntimeTypeHandle GetTypeHandleIfAvailable(Type type)
        {
            if (type is not RuntimeType)
                return default(RuntimeTypeHandle);

            RuntimeTypeInfo runtimeType = type.CastToRuntimeTypeInfo();
            if (runtimeType == null)
                return default(RuntimeTypeHandle);
            return runtimeType.InternalTypeHandleIfAvailable;
        }

        public bool SupportsReflection(Type type)
        {
            if (type is not RuntimeType)
                return false;

            RuntimeTypeInfo runtimeType = type.CastToRuntimeTypeInfo();
            if (null == runtimeType.InternalNameIfAvailable)
                return false;

            if (ExecutionEnvironment.IsReflectionBlocked(type.TypeHandle))
            {
                // The type is an internal framework type and is blocked from reflection
                return false;
            }

            if (runtimeType.InternalFullNameOfAssembly == Internal.Runtime.Augments.RuntimeAugments.HiddenScopeAssemblyName)
            {
                // The type is an internal framework type but is reflectable for internal class library use
                // where we make the type appear in a hidden assembly
                return false;
            }

            return true;
        }

        internal ExecutionEnvironment ExecutionEnvironment { get; }

        internal ReflectionDomainSetup ReflectionDomainSetup { get; }

        internal static IEnumerable<Type> PrimitiveTypes => s_primitiveTypes;

        private static readonly Type[] s_primitiveTypes =
        {
                    typeof(bool),
                    typeof(char),
                    typeof(sbyte),
                    typeof(byte),
                    typeof(short),
                    typeof(ushort),
                    typeof(int),
                    typeof(uint),
                    typeof(long),
                    typeof(ulong),
                    typeof(float),
                    typeof(double),
                    typeof(IntPtr),
                    typeof(UIntPtr),
        };
    }
}
