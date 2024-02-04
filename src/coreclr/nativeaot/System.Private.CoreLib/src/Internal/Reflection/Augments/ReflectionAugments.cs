// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//  Internal.Reflection.Augments
//  -------------------------------------------------
//  Why does this exist?:
//    Also, IntrospectionServices.GetTypeInfo() and Assembly.Load()
//    are defined in System.Reflection but need a way to "call into"
//    Reflection.Core.dll to do the real work.
//
//    This contract adds the additional entrypoints needed to System.Reflection.
//
//  Implemented by:
//    System.Reflection.dll on RH (may use ILMerging instead)
//
//  Consumed by:
//    Reflection.Core.dll

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;

using Internal.Runtime;

using EETypeElementType = Internal.Runtime.EETypeElementType;

namespace Internal.Reflection.Augments
{
    public static class ReflectionAugments
    {
        //
        // One time start up initialization - called by Reflection.Core.dll to provide System.Reflection with a way to call back
        // into Reflection.Core.dll.
        //
        public static void Initialize(ReflectionCoreCallbacks reflectionCoreCallbacks)
        {
            Debug.Assert(s_reflectionCoreCallbacks == null);
            s_reflectionCoreCallbacks = reflectionCoreCallbacks;
        }

        internal static unsafe TypeCode GetRuntimeTypeCode(RuntimeType type)
        {
            Debug.Assert(type != null);

            MethodTable* eeType = type.ToMethodTableMayBeNull();
            if (eeType == null)
            {
                // Type exists in metadata only. Aside from the enums, there is no chance a type with a TypeCode would not have an MethodTable,
                // so if it's not an enum, return the default.
                if (!type.IsActualEnum)
                    return TypeCode.Object;
                Type underlyingType = Enum.GetUnderlyingType(type);
                eeType = underlyingType.TypeHandle.ToMethodTable();
            }

            // Note: Type.GetTypeCode() is expected to return the underlying type's TypeCode for enums. EETypePtr.CorElementType does the same,
            // so this one switch handles both cases.
            EETypeElementType rhType = eeType->ElementType;
            switch (rhType)
            {
                case EETypeElementType.Boolean: return TypeCode.Boolean;
                case EETypeElementType.Char: return TypeCode.Char;
                case EETypeElementType.SByte: return TypeCode.SByte;
                case EETypeElementType.Byte: return TypeCode.Byte;
                case EETypeElementType.Int16: return TypeCode.Int16;
                case EETypeElementType.UInt16: return TypeCode.UInt16;
                case EETypeElementType.Int32: return TypeCode.Int32;
                case EETypeElementType.UInt32: return TypeCode.UInt32;
                case EETypeElementType.Int64: return TypeCode.Int64;
                case EETypeElementType.UInt64: return TypeCode.UInt64;
                case EETypeElementType.Single: return TypeCode.Single;
                case EETypeElementType.Double: return TypeCode.Double;
                default:
                    break;
            }

            if (type == typeof(string))
                return TypeCode.String;

            if (type == typeof(DateTime))
                return TypeCode.DateTime;

            if (type == typeof(decimal))
                return TypeCode.Decimal;

            if (type == typeof(DBNull))
                return TypeCode.DBNull;

            return TypeCode.Object;
        }

        public static TypeLoadException CreateTypeLoadException(string message, string typeName)
        {
            return new TypeLoadException(message, typeName);
        }

        internal static ReflectionCoreCallbacks ReflectionCoreCallbacks
        {
            get
            {
                ReflectionCoreCallbacks callbacks = s_reflectionCoreCallbacks;
                Debug.Assert(callbacks != null);
                return callbacks;
            }
        }

        internal static bool IsInitialized
        {
            get
            {
                return s_reflectionCoreCallbacks != null;
            }
        }

        private static ReflectionCoreCallbacks s_reflectionCoreCallbacks;
    }

    //
    // This class is implemented by Internal.Reflection.Core.dll and provides the actual implementation
    // of Type.GetTypeInfo() and Assembly.Load().
    //
    public abstract class ReflectionCoreCallbacks
    {
        public abstract Assembly Load(AssemblyName refName, bool throwOnFileNotFound);
        public abstract Assembly Load(ReadOnlySpan<byte> rawAssembly, ReadOnlySpan<byte> pdbSymbolStore);
        public abstract Assembly Load(string assemblyPath);

        public abstract MethodBase GetMethodFromHandle(RuntimeMethodHandle runtimeMethodHandle);
        public abstract MethodBase GetMethodFromHandle(RuntimeMethodHandle runtimeMethodHandle, RuntimeTypeHandle declaringTypeHandle);
        public abstract FieldInfo GetFieldFromHandle(RuntimeFieldHandle runtimeFieldHandle);
        public abstract FieldInfo GetFieldFromHandle(RuntimeFieldHandle runtimeFieldHandle, RuntimeTypeHandle declaringTypeHandle);

        public abstract EventInfo GetImplicitlyOverriddenBaseClassEvent(EventInfo e);
        public abstract MethodInfo GetImplicitlyOverriddenBaseClassMethod(MethodInfo m);
        public abstract PropertyInfo GetImplicitlyOverriddenBaseClassProperty(PropertyInfo p);

        public abstract object ActivatorCreateInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type, bool nonPublic);
        public abstract object ActivatorCreateInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes);

        // V2 api: Creates open or closed delegates to static or instance methods - relaxed signature checking allowed.
        public abstract Delegate CreateDelegate(Type type, object? firstArgument, MethodInfo method, bool throwOnBindFailure);

        // V1 api: Creates open delegates to static or instance methods - relaxed signature checking allowed.
        public abstract Delegate CreateDelegate(Type type, MethodInfo method, bool throwOnBindFailure);

        // V1 api: Creates closed delegates to instance methods only, relaxed signature checking disallowed.
        [RequiresUnreferencedCode("The target method might be removed")]
        public abstract Delegate CreateDelegate(Type type, object target, string method, bool ignoreCase, bool throwOnBindFailure);

        // V1 api: Creates open delegates to static methods only, relaxed signature checking disallowed.
        public abstract Delegate CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method, bool ignoreCase, bool throwOnBindFailure);

        public abstract IntPtr GetFunctionPointer(RuntimeMethodHandle runtimeMethodHandle, RuntimeTypeHandle declaringTypeHandle);

        public abstract void MakeTypedReference(object target, FieldInfo[] flds, out Type type, out int offset);

        public abstract Assembly[] GetLoadedAssemblies();

        public abstract EnumInfo GetEnumInfo(Type type, Func<Type, string[], object[], bool, EnumInfo> create);

        public abstract DynamicInvokeInfo GetDelegateDynamicInvokeInfo(Type type);

        public abstract MethodInfo GetDelegateMethod(Delegate del);

        public abstract MethodBase GetMethodBaseFromStartAddressIfAvailable(IntPtr methodStartAddress);

        public abstract Assembly GetAssemblyForHandle(RuntimeTypeHandle typeHandle);

        public abstract void RunClassConstructor(RuntimeTypeHandle typeHandle);
    }
}
