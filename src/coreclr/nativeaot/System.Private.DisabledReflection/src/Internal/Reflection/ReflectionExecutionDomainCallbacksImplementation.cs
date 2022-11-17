// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

using Internal.Runtime.Augments;

namespace Internal.Reflection
{
    internal class ReflectionExecutionDomainCallbacksImplementation : ReflectionExecutionDomainCallbacks
    {
        public override Exception CreateMissingMetadataException(Type typeWithMissingMetadata) => throw new NotImplementedException();
        public override Type GetArrayTypeForHandle(RuntimeTypeHandle typeHandle) => RuntimeTypeInfo.GetRuntimeTypeInfo(typeHandle);
        public override Assembly GetAssemblyForHandle(RuntimeTypeHandle typeHandle) => new RuntimeAssemblyInfo(typeHandle);
        public override string GetBetterDiagnosticInfoIfAvailable(RuntimeTypeHandle runtimeTypeHandle) => null;
        public override Type GetByRefTypeForHandle(RuntimeTypeHandle typeHandle) => RuntimeTypeInfo.GetRuntimeTypeInfo(typeHandle);
        public override Type GetConstructedGenericTypeForHandle(RuntimeTypeHandle typeHandle) => RuntimeTypeInfo.GetRuntimeTypeInfo(typeHandle);
        public override MethodInfo GetDelegateMethod(Delegate del) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override Exception GetExceptionForHR(int hr) => throw new NotImplementedException();
        public override Type GetMdArrayTypeForHandle(RuntimeTypeHandle typeHandle, int rank) => RuntimeTypeInfo.GetRuntimeTypeInfo(typeHandle);
        public override MethodBase GetMethodBaseFromStartAddressIfAvailable(IntPtr methodStartAddress) => null;
        public override Type GetNamedTypeForHandle(RuntimeTypeHandle typeHandle, bool isGenericTypeDefinition) => RuntimeTypeInfo.GetRuntimeTypeInfo(typeHandle);
        public override Type GetPointerTypeForHandle(RuntimeTypeHandle typeHandle) => RuntimeTypeInfo.GetRuntimeTypeInfo(typeHandle);
        public override Type GetType(string typeName, Func<AssemblyName, Assembly?> assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError, bool ignoreCase, string defaultAssembly) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override RuntimeTypeHandle GetTypeHandleIfAvailable(Type type) => type.TypeHandle;
        public override bool IsReflectionBlocked(RuntimeTypeHandle typeHandle) => false;
        public override bool SupportsReflection(Type type) => false;
        public override IntPtr TryGetStaticClassConstructionContext(RuntimeTypeHandle runtimeTypeHandle) => throw new NotSupportedException(SR.Reflection_Disabled);
    }
}
