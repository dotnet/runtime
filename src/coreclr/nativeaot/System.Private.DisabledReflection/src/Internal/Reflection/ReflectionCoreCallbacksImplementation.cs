// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

using Internal.Reflection.Augments;
using Internal.Runtime.Augments;

namespace Internal.Reflection
{
    internal class ReflectionCoreCallbacksImplementation : ReflectionCoreCallbacks
    {
        public override EnumInfo GetEnumInfo(Type type, Func<Type, string[], object[], bool, EnumInfo> create) =>
            create(
                RuntimeAugments.GetEnumUnderlyingType(type.TypeHandle),
                Array.Empty<string>(),
                Array.Empty<object>(),
                false);

        public override DynamicInvokeInfo GetDelegateDynamicInvokeInfo(Type type)
            => throw new NotSupportedException(SR.Reflection_Disabled);
        public override MethodInfo GetDelegateMethod(Delegate del)
            => throw new NotSupportedException(SR.Reflection_Disabled);
        public override object ActivatorCreateInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type, bool nonPublic) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override object ActivatorCreateInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override Delegate CreateDelegate(Type type, object firstArgument, MethodInfo method, bool throwOnBindFailure) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override Delegate CreateDelegate(Type type, MethodInfo method, bool throwOnBindFailure) => throw new NotSupportedException(SR.Reflection_Disabled);
        [RequiresUnreferencedCode("The target method might be removed")]
        public override Delegate CreateDelegate(Type type, object target, string method, bool ignoreCase, bool throwOnBindFailure) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override Delegate CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method, bool ignoreCase, bool throwOnBindFailure) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override FieldInfo GetFieldFromHandle(RuntimeFieldHandle runtimeFieldHandle) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override FieldInfo GetFieldFromHandle(RuntimeFieldHandle runtimeFieldHandle, RuntimeTypeHandle declaringTypeHandle) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override IntPtr GetFunctionPointer(RuntimeMethodHandle runtimeMethodHandle, RuntimeTypeHandle declaringTypeHandle) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override EventInfo GetImplicitlyOverriddenBaseClassEvent(EventInfo e) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override MethodInfo GetImplicitlyOverriddenBaseClassMethod(MethodInfo m) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override PropertyInfo GetImplicitlyOverriddenBaseClassProperty(PropertyInfo p) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override Assembly[] GetLoadedAssemblies() => throw new NotSupportedException(SR.Reflection_Disabled);
        public override MethodBase GetMethodFromHandle(RuntimeMethodHandle runtimeMethodHandle) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override MethodBase GetMethodFromHandle(RuntimeMethodHandle runtimeMethodHandle, RuntimeTypeHandle declaringTypeHandle) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override Assembly Load(AssemblyName refName, bool throwOnFileNotFound) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override Assembly Load(ReadOnlySpan<byte> rawAssembly, ReadOnlySpan<byte> pdbSymbolStore) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override Assembly Load(string assemblyPath) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override void MakeTypedReference(object target, FieldInfo[] flds, out Type type, out int offset) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override Assembly GetAssemblyForHandle(RuntimeTypeHandle typeHandle) => new RuntimeAssemblyInfo(typeHandle);
        public override void RunClassConstructor(RuntimeTypeHandle typeHandle) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override MethodBase GetMethodBaseFromStartAddressIfAvailable(IntPtr methodStartAddress) => null;
    }
}
