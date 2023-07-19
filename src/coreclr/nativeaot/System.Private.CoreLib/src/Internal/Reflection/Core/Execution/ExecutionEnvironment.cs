// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Runtime.CompilerServices;
using Internal.Metadata.NativeFormat;

using OpenMethodInvoker = System.Reflection.Runtime.MethodInfos.OpenMethodInvoker;
using System.Reflection.Runtime.MethodInfos;

namespace Internal.Reflection.Core.Execution
{
    //
    // This class abstracts the underlying Redhawk (or whatever execution engine) runtime and exposes the services
    // that I.R.Core.Execution needs.
    //
    [CLSCompliant(false)]
    public abstract class ExecutionEnvironment
    {
        //==============================================================================================
        // Access to the underlying execution engine's object allocation routines.
        //==============================================================================================
        public abstract Array NewArray(RuntimeTypeHandle typeHandleForArrayType, int count);
        public abstract Array NewMultiDimArray(RuntimeTypeHandle typeHandleForArrayType, int[] lengths, int[] lowerBounds);

        //==============================================================================================
        // Execution engine policies.
        //==============================================================================================

        //
        // This returns a generic type with one generic parameter (representing the array element type)
        // whose base type and interface list determines what TypeInfo.BaseType and TypeInfo.ImplementedInterfaces
        // return for types that return true for IsArray.
        //
        public abstract RuntimeTypeHandle ProjectionTypeForArrays { get; }
        public abstract bool IsAssignableFrom(RuntimeTypeHandle dstType, RuntimeTypeHandle srcType);
        public abstract bool TryGetBaseType(RuntimeTypeHandle typeHandle, out RuntimeTypeHandle baseTypeHandle);
        public abstract IEnumerable<RuntimeTypeHandle> TryGetImplementedInterfaces(RuntimeTypeHandle typeHandle);
        public abstract void VerifyInterfaceIsImplemented(RuntimeTypeHandle typeHandle, RuntimeTypeHandle ifaceHandle);
        public abstract void GetInterfaceMap(Type instanceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType, out MethodInfo[] interfaceMethods, out MethodInfo[] targetMethods);

        //==============================================================================================
        // Reflection Mapping Tables
        //==============================================================================================
        public abstract QTypeDefinition GetMetadataForNamedType(RuntimeTypeHandle runtimeTypeHandle);
        public abstract bool TryGetNamedTypeForMetadata(QTypeDefinition qTypeDefinition, out RuntimeTypeHandle runtimeTypeHandle);

        public abstract bool TryGetArrayTypeForElementType(RuntimeTypeHandle elementTypeHandle, out RuntimeTypeHandle arrayTypeHandle);
        public abstract RuntimeTypeHandle GetArrayTypeElementType(RuntimeTypeHandle arrayTypeHandle);

        public abstract bool TryGetMultiDimArrayTypeForElementType(RuntimeTypeHandle elementTypeHandle, int rank, out RuntimeTypeHandle arrayTypeHandle);

        public abstract bool TryGetFunctionPointerTypeForComponents(RuntimeTypeHandle returnTypeHandle, RuntimeTypeHandle[] parameterHandles, bool isUnmanaged, out RuntimeTypeHandle functionPointerTypeHandle);
        public abstract void GetFunctionPointerTypeComponents(RuntimeTypeHandle functionPointerHandle, out RuntimeTypeHandle returnTypeHandle, out RuntimeTypeHandle[] parameterHandles, out bool isUnmanaged);

        public abstract bool TryGetPointerTypeForTargetType(RuntimeTypeHandle targetTypeHandle, out RuntimeTypeHandle pointerTypeHandle);
        public abstract RuntimeTypeHandle GetPointerTypeTargetType(RuntimeTypeHandle pointerTypeHandle);

        public abstract bool TryGetByRefTypeForTargetType(RuntimeTypeHandle targetTypeHandle, out RuntimeTypeHandle byRefTypeHandle);
        public abstract RuntimeTypeHandle GetByRefTypeTargetType(RuntimeTypeHandle byRefTypeHandle);

        public abstract bool TryGetConstructedGenericTypeForComponents(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles, out RuntimeTypeHandle runtimeTypeHandle);
        public abstract bool TryGetConstructedGenericTypeForComponentsNoConstraintCheck(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles, out RuntimeTypeHandle runtimeTypeHandle);

        //==============================================================================================
        // Invoke and field access support.
        //==============================================================================================
        public abstract MethodBaseInvoker TryGetMethodInvoker(RuntimeTypeHandle declaringTypeHandle, QMethodDefinition methodHandle, RuntimeTypeHandle[] genericMethodTypeArgumentHandles);
        public abstract FieldAccessor TryGetFieldAccessor(MetadataReader reader, RuntimeTypeHandle declaringTypeHandle, RuntimeTypeHandle fieldTypeHandle, FieldHandle fieldHandle);

        //==============================================================================================
        // RuntimeMethodHandle and RuntimeFieldHandle support.
        //==============================================================================================
        public abstract bool TryGetMethodFromHandle(RuntimeMethodHandle runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle, out QMethodDefinition methodHandle, out RuntimeTypeHandle[] genericMethodTypeArgumentHandles);
        public abstract bool TryGetMethodFromHandleAndType(RuntimeMethodHandle runtimeMethodHandle, RuntimeTypeHandle declaringTypeHandle, out QMethodDefinition methodHandle, out RuntimeTypeHandle[] genericMethodTypeArgumentHandles);
        public abstract bool TryGetFieldFromHandle(RuntimeFieldHandle runtimeFieldHandle, out RuntimeTypeHandle declaringTypeHandle, out FieldHandle fieldHandle);
        public abstract bool TryGetFieldFromHandleAndType(RuntimeFieldHandle runtimeFieldHandle, RuntimeTypeHandle declaringTypeHandle, out FieldHandle fieldHandle);


        //==============================================================================================
        // Manifest resource stream support.
        //==============================================================================================
        public abstract ManifestResourceInfo GetManifestResourceInfo(Assembly assembly, string resourceName);
        public abstract string[] GetManifestResourceNames(Assembly assembly);
        public abstract Stream GetManifestResourceStream(Assembly assembly, string name);

        //==============================================================================================
        // Other
        //==============================================================================================
        public abstract FieldAccessor CreateLiteralFieldAccessor(object value, RuntimeTypeHandle fieldTypeHandle);
        public abstract void GetEnumInfo(RuntimeTypeHandle typeHandle, out string[] names, out object[] values, out bool isFlags);
        public abstract IntPtr GetDynamicInvokeThunk(MethodBaseInvoker invoker);

        //==============================================================================================
        // Non-public methods
        //==============================================================================================
        internal MethodBaseInvoker GetMethodInvoker(RuntimeTypeInfo declaringType, QMethodDefinition methodHandle, RuntimeTypeInfo[] genericMethodTypeArguments, MemberInfo exceptionPertainant, out Exception exception)
        {
            exception = null;

            if (declaringType.ContainsGenericParameters)
                return new OpenMethodInvoker();
            for (int i = 0; i < genericMethodTypeArguments.Length; i++)
            {
                if (genericMethodTypeArguments[i].ContainsGenericParameters)
                    return new OpenMethodInvoker();
            }

            RuntimeTypeHandle typeDefinitionHandle = declaringType.TypeHandle;
            RuntimeTypeHandle[] genericMethodTypeArgumentHandles = new RuntimeTypeHandle[genericMethodTypeArguments.Length];

            for (int i = 0; i < genericMethodTypeArguments.Length; i++)
            {
                genericMethodTypeArgumentHandles[i] = genericMethodTypeArguments[i].TypeHandle;
            }
            MethodBaseInvoker methodInvoker = TryGetMethodInvoker(typeDefinitionHandle, methodHandle, genericMethodTypeArgumentHandles);
            if (methodInvoker == null)
                exception = ReflectionCoreExecution.ExecutionDomain.CreateNonInvokabilityException(exceptionPertainant);
            return methodInvoker;
        }

        protected MethodBaseInvoker GetMethodInvoker(MethodInfo methodInfo)
        {
            return ((RuntimeMethodInfo)methodInfo).MethodInvoker;
        }
    }
}
