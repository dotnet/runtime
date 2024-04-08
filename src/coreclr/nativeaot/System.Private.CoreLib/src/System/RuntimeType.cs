// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Runtime.TypeInfos;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Reflection.Augments;
using Internal.Reflection.Core.Execution;
using Internal.Runtime;

using Debug = System.Diagnostics.Debug;

namespace System
{
    internal sealed unsafe class RuntimeType : TypeInfo, ICloneable
    {
        private MethodTable* _pUnderlyingEEType;
        private IntPtr _runtimeTypeInfoHandle;

        internal RuntimeType(MethodTable* pEEType)
        {
            _pUnderlyingEEType = pEEType;
        }

        internal RuntimeType(RuntimeTypeInfo runtimeTypeInfo)
        {
            // This needs to be a strong handle to prevent the type from being collected and re-created that would end up leaking the handle.
            _runtimeTypeInfoHandle = RuntimeImports.RhHandleAlloc(runtimeTypeInfo, GCHandleType.Normal);
        }

        internal void DangerousSetUnderlyingEEType(MethodTable* pEEType)
        {
            Debug.Assert(_pUnderlyingEEType == null);
            _pUnderlyingEEType = pEEType;
        }

        internal void Free()
        {
            RuntimeImports.RhHandleFree(_runtimeTypeInfoHandle);
        }

        private static bool IsReflectionDisabled => false;

        private static bool DoNotThrowForNames => AppContext.TryGetSwitch("Switch.System.Reflection.Disabled.DoNotThrowForNames", out bool doNotThrow) && doNotThrow;
        private static bool DoNotThrowForAssembly => AppContext.TryGetSwitch("Switch.System.Reflection.Disabled.DoNotThrowForAssembly", out bool doNotThrow) && doNotThrow;
        private static bool DoNotThrowForAttributes => AppContext.TryGetSwitch("Switch.System.Reflection.Disabled.DoNotThrowForAttributes", out bool doNotThrow) && doNotThrow;

        internal MethodTable* ToMethodTableMayBeNull() => _pUnderlyingEEType;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RuntimeTypeInfo GetRuntimeTypeInfo()
        {
            IntPtr handle = _runtimeTypeInfoHandle;
            if (handle != default)
            {
                object? runtimeTypeInfo = RuntimeImports.RhHandleGet(handle);
                if (runtimeTypeInfo != null)
                {
                    return Unsafe.As<RuntimeTypeInfo>(runtimeTypeInfo);
                }
            }
            return InitializeRuntimeTypeInfoHandle();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private RuntimeTypeInfo InitializeRuntimeTypeInfoHandle()
        {
            if (IsReflectionDisabled)
                throw new NotSupportedException(SR.Reflection_Disabled);

            RuntimeTypeInfo runtimeTypeInfo = ExecutionDomain.GetRuntimeTypeInfo(_pUnderlyingEEType);

            // We assume that the RuntimeTypeInfo unifiers pick a winner when multiple threads
            // race to create RuntimeTypeInfo.

            IntPtr handle = _runtimeTypeInfoHandle;
            if (handle == default)
            {
                IntPtr tempHandle = RuntimeImports.RhHandleAlloc(runtimeTypeInfo, GCHandleType.Weak);
                if (Interlocked.CompareExchange(ref _runtimeTypeInfoHandle, tempHandle, default) != default)
                    RuntimeImports.RhHandleFree(tempHandle);
            }
            else
            {
                RuntimeImports.RhHandleSet(handle, runtimeTypeInfo);
            }

            return runtimeTypeInfo;
        }

        public override string? GetEnumName(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            ulong rawValue;
            if (!Enum.TryGetUnboxedValueOfEnumOrInteger(value, out rawValue))
                throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, nameof(value));

            // For desktop compatibility, do not bounce an incoming integer that's the wrong size.
            // Do a value-preserving cast of both it and the enum values and do a 64-bit compare.

            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            return Enum.GetName(this, rawValue);
        }

        public override string[] GetEnumNames()
        {
            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            string[] ret = Enum.GetNamesNoCopy(this);

            // Make a copy since we can't hand out the same array since users can modify them
            return new ReadOnlySpan<string>(ret).ToArray();
        }

        public override Type GetEnumUnderlyingType()
        {
            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            return Enum.InternalGetUnderlyingType(this);
        }

        public override bool IsEnumDefined(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            if (value is string valueAsString)
            {
                EnumInfo enumInfo = Enum.GetEnumInfo(this);
                foreach (string name in enumInfo.Names)
                {
                    if (valueAsString == name)
                        return true;
                }
                return false;
            }
            else
            {
                ulong rawValue;
                if (!Enum.TryGetUnboxedValueOfEnumOrInteger(value, out rawValue))
                {
                    if (Type.IsIntegerType(value.GetType()))
                        throw new ArgumentException(SR.Format(SR.Arg_EnumUnderlyingTypeAndObjectMustBeSameType, value.GetType(), Enum.InternalGetUnderlyingType(this)));
                    else
                        throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
                }

                if (value is Enum)
                {
                    if (value.GetMethodTable() != this.ToMethodTableMayBeNull())
                        throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, value.GetType(), this));
                }
                else
                {
                    Type underlyingType = Enum.InternalGetUnderlyingType(this);
                    if (!(underlyingType.TypeHandle.ToMethodTable() == value.GetMethodTable()))
                        throw new ArgumentException(SR.Format(SR.Arg_EnumUnderlyingTypeAndObjectMustBeSameType, value.GetType(), underlyingType));
                }

                return Enum.GetName(this, rawValue) != null;
            }
        }

        [RequiresDynamicCode("It might not be possible to create an array of the enum type at runtime. Use the GetValues<TEnum> overload instead.")]
        public override Array GetEnumValues()
        {
            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            Array values = Enum.GetValuesAsUnderlyingTypeNoCopy(this);
            int count = values.Length;

            // Without universal shared generics, chances are slim that we'll have the appropriate
            // array type available. Offer an escape hatch that avoids a missing metadata exception
            // at the cost of a small appcompat risk.
            Array result = AppContext.TryGetSwitch("Switch.System.Enum.RelaxedGetValues", out bool isRelaxed) && isRelaxed ?
                Array.CreateInstance(Enum.InternalGetUnderlyingType(this), count) :
                Array.CreateInstance(this, count);

            Array.Copy(values, result, values.Length);
            return result;
        }

        public override Array GetEnumValuesAsUnderlyingType()
        {
            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            return Enum.GetValuesAsUnderlyingType(this);
        }

        public override int GetHashCode()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
                return ((nuint)pEEType).GetHashCode();
            return RuntimeHelpers.GetHashCode(this);
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return new RuntimeTypeHandle(pEEType);
                return GetRuntimeTypeInfo().TypeHandle;
            }
        }

        internal new unsafe bool IsInterface
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsInterface;
                return GetRuntimeTypeInfo().IsInterface;
            }
        }

        protected override bool IsValueTypeImpl()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
                return pEEType->IsValueType;
            return GetRuntimeTypeInfo().IsValueType;
        }

        internal bool IsActualValueType
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsValueType;
                return GetRuntimeTypeInfo().IsActualValueType;
            }
        }

        public override unsafe bool IsEnum
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsEnum;
                return GetRuntimeTypeInfo().IsEnum;
            }
        }

        internal unsafe bool IsActualEnum
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsEnum;
                return GetRuntimeTypeInfo().IsActualEnum;
            }
        }

        protected override unsafe bool IsArrayImpl()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
                return pEEType->IsArray;
            return GetRuntimeTypeInfo().IsArray;
        }

        protected override unsafe bool IsByRefImpl()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
                return pEEType->IsByRef;
            return GetRuntimeTypeInfo().IsByRef;
        }

        protected override unsafe bool IsPointerImpl()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
                return pEEType->IsPointer;
            return GetRuntimeTypeInfo().IsPointer;
        }

        protected override unsafe bool HasElementTypeImpl()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
                return pEEType->IsParameterizedType;
            return GetRuntimeTypeInfo().HasElementType;
        }

        public override Type? GetElementType()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
                return pEEType->IsParameterizedType ? GetTypeFromMethodTable(pEEType->RelatedParameterType) : null;
            return GetRuntimeTypeInfo().GetElementType();
        }

        public override int GetArrayRank()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
                return pEEType->IsArray ? pEEType->ArrayRank : throw new ArgumentException(SR.Argument_HasToBeArrayClass);
            return GetRuntimeTypeInfo().GetArrayRank();
        }

        public override Type? BaseType
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                {
                    if (pEEType->IsCanonical)
                    {
                        MethodTable* pBaseType = pEEType->NonArrayBaseType;
                        return (pBaseType != null) ? GetTypeFromMethodTable(pBaseType) : null;
                    }

                    if (pEEType->IsArray)
                    {
                        return typeof(Array);
                    }

                    if (!pEEType->IsGenericTypeDefinition)
                    {
                        return null;
                    }
                }

                return GetRuntimeTypeInfo().BaseType;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null && !pEEType->IsGenericTypeDefinition)
            {
                int count = pEEType->NumInterfaces;
                if (count == 0)
                    return EmptyTypes;

                Type[] result = new Type[count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = GetTypeFromMethodTable(pEEType->InterfaceMap[i]);
                }
                return result;
            }

            return GetRuntimeTypeInfo().GetInterfaces();
        }

        public override bool IsTypeDefinition
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return (pEEType->IsCanonical && !pEEType->IsGeneric) || pEEType->IsGenericTypeDefinition;
                return GetRuntimeTypeInfo().IsTypeDefinition;
            }
        }

        public override bool IsGenericType
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsGeneric || pEEType->IsGenericTypeDefinition;
                return GetRuntimeTypeInfo().IsGenericType;
            }
        }

        public override bool IsGenericTypeDefinition
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsGenericTypeDefinition;
                return GetRuntimeTypeInfo().IsGenericTypeDefinition;
            }
        }

        public override bool IsConstructedGenericType
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsGeneric;
                return GetRuntimeTypeInfo().IsConstructedGenericType;
            }
        }

        public override Type GetGenericTypeDefinition()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
            {
                return pEEType->IsGeneric ? GetTypeFromMethodTable(pEEType->GenericDefinition) :
                    pEEType->IsGenericTypeDefinition ? this :
                        throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
            }
            return GetRuntimeTypeInfo().GetGenericTypeDefinition();
        }

        public override Type[] GenericTypeArguments
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                {
                    if (!pEEType->IsGeneric)
                        return EmptyTypes;

                    MethodTableList genericArguments = pEEType->GenericArguments;

                    Type[] result = new Type[pEEType->GenericArity];
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = GetTypeFromMethodTable(genericArguments[i]);
                    }
                    return result;
                }

                return GetRuntimeTypeInfo().GenericTypeArguments;
            }
        }

        public override Type[] GetGenericArguments()
        {
            if (IsConstructedGenericType)
                return GenericTypeArguments;
            if (IsGenericTypeDefinition)
                return GenericTypeParameters;
            return EmptyTypes;
        }

        public override bool IsGenericParameter
        {
            get
            {
                if (_pUnderlyingEEType != null)
                    return false;
                return GetRuntimeTypeInfo().IsGenericParameter;
            }
        }

        public override bool IsGenericTypeParameter
        {
            get
            {
                if (_pUnderlyingEEType != null)
                    return false;
                return GetRuntimeTypeInfo().IsGenericTypeParameter;
            }
        }

        public override bool IsGenericMethodParameter
        {
            get
            {
                if (_pUnderlyingEEType != null)
                    return false;
                return GetRuntimeTypeInfo().IsGenericMethodParameter;
            }
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsGenericTypeDefinition;
                return GetRuntimeTypeInfo().ContainsGenericParameters;
            }
        }

        protected override bool IsPrimitiveImpl()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
                return pEEType->IsActualPrimitive;
            return GetRuntimeTypeInfo().IsPrimitive;
        }

        public override bool IsSZArray
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->ElementType == EETypeElementType.SzArray;
                return GetRuntimeTypeInfo().IsSZArray;
            }
        }

        public override bool IsVariableBoundArray
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->ElementType == EETypeElementType.Array;
                return GetRuntimeTypeInfo().IsVariableBoundArray;
            }
        }

        public override bool IsByRefLike
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsByRefLike;
                return GetRuntimeTypeInfo().IsByRefLike;
            }
        }

        public override bool IsFunctionPointer
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsFunctionPointer;
                return GetRuntimeTypeInfo().IsFunctionPointer;
            }
        }

        public override bool IsUnmanagedFunctionPointer
        {
            get
            {
                MethodTable* pEEType = _pUnderlyingEEType;
                if (pEEType != null)
                    return pEEType->IsFunctionPointer && pEEType->IsUnmanagedFunctionPointer;
                return GetRuntimeTypeInfo().IsUnmanagedFunctionPointer;
            }
        }

        public override Type[] GetFunctionPointerParameterTypes()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
            {
                if (!pEEType->IsFunctionPointer)
                    throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);

                uint count = pEEType->NumFunctionPointerParameters;
                if (count == 0)
                    return EmptyTypes;

                MethodTableList parameterTypes = pEEType->FunctionPointerParameters;

                Type[] result = new Type[count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = GetTypeFromMethodTable(parameterTypes[i]);
                }
                return result;
            }

            return GetRuntimeTypeInfo().GetFunctionPointerParameterTypes();
        }

        public override Type GetFunctionPointerReturnType()
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType != null)
            {
                if (!pEEType->IsFunctionPointer)
                    throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);

                return GetTypeFromMethodTable(pEEType->FunctionPointerReturnType);
            }

            return GetRuntimeTypeInfo().GetFunctionPointerReturnType();
        }

        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c)
        {
            if (c == null)
                return false;

            if (object.ReferenceEquals(c, this))
                return true;

            if (c.UnderlyingSystemType is not RuntimeType fromRuntimeType)
                return false;  // Desktop compat: If typeInfo is null, or implemented by a different Reflection implementation, return "false."

            if (fromRuntimeType._pUnderlyingEEType != null && _pUnderlyingEEType != null)
            {
                // If both types have type handles, let MRT handle this. It's not dependent on metadata.
                if (RuntimeImports.AreTypesAssignable(fromRuntimeType._pUnderlyingEEType, _pUnderlyingEEType))
                    return true;

                // Runtime IsAssignableFrom does not handle casts from generic type definitions: always returns false. For those, we fall through to the
                // managed implementation. For everyone else, return "false".
                //
                // Runtime IsAssignableFrom does not handle pointer -> UIntPtr cast.
                if (!fromRuntimeType._pUnderlyingEEType->IsGenericTypeDefinition || fromRuntimeType._pUnderlyingEEType->IsPointer)
                    return false;
            }

            // If we got here, the types are open, or reduced away, or otherwise lacking in type handles. Perform the IsAssignability check using metadata.
            return GetRuntimeTypeInfo().IsAssignableFrom(fromRuntimeType);
        }

        public override bool IsInstanceOfType([NotNullWhen(true)] object? o)
        {
            MethodTable* pEEType = _pUnderlyingEEType;
            if (pEEType == null || pEEType->IsGenericTypeDefinition)
                return false;
            if (pEEType->IsNullable)
                pEEType = pEEType->NullableType;
            return RuntimeImports.IsInstanceOf(pEEType, o) != null;
        }

        //
        // Methods that do not directly depend on _pUnderlyingEEType
        //

        public override string ToString()
        {
            if (IsReflectionDisabled)
                return "0x" + ((nuint)_pUnderlyingEEType).ToString("x");

            return GetRuntimeTypeInfo().ToString();
        }

        public override bool Equals(object? obj) => ReferenceEquals(obj, this);

        object ICloneable.Clone() => this;

        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
            => typeInfo != null && IsAssignableFrom(typeInfo.AsType());

        public override bool IsSecurityCritical => true;
        public override bool IsSecuritySafeCritical => false;
        public override bool IsSecurityTransparent => false;

        public override Type UnderlyingSystemType => this;

        public override bool IsCollectible => false;

        public override MemberTypes MemberType => GetRuntimeTypeInfo().MemberType;

        public override int MetadataToken => GetRuntimeTypeInfo().MetadataToken;

        public override Type? DeclaringType => GetRuntimeTypeInfo().DeclaringType;
        public override Type? ReflectedType => DeclaringType;

        public override MethodBase? DeclaringMethod => GetRuntimeTypeInfo().DeclaringMethod;

        public override StructLayoutAttribute StructLayoutAttribute => GetRuntimeTypeInfo().StructLayoutAttribute;

        protected override bool IsCOMObjectImpl() => false;

        protected override TypeCode GetTypeCodeImpl() => ReflectionAugments.GetRuntimeTypeCode(this);

        protected override TypeAttributes GetAttributeFlagsImpl() => GetRuntimeTypeInfo().Attributes;

        public override Type[] GenericTypeParameters
            => GetRuntimeTypeInfo().GenericTypeParameters;

        public override int GenericParameterPosition
            => GetRuntimeTypeInfo().GenericParameterPosition;
        public override GenericParameterAttributes GenericParameterAttributes
            => GetRuntimeTypeInfo().GenericParameterAttributes;
        public override Type[] GetGenericParameterConstraints()
            => GetRuntimeTypeInfo().GetGenericParameterConstraints();

        public override Type[] GetFunctionPointerCallingConventions()
        {
            if (!IsFunctionPointer)
                throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);

            // Requires a modified type to return the modifiers.
            return EmptyTypes;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
            => GetRuntimeTypeInfo().GetConstructorImpl(bindingAttr, binder, callConvention, types, modifiers);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetConstructors(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetEvent(name, bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetEvents(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetField(name, bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetFields(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
            => GetRuntimeTypeInfo().GetMethodImpl(name, RuntimeTypeInfo.GenericParameterCountAny, bindingAttr, binder, callConvention, types, modifiers);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
            => GetRuntimeTypeInfo().GetMethodImpl(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetMethods(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string name, BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetNestedType(name, bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetNestedTypes(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
            => GetRuntimeTypeInfo().GetPropertyImpl(name, bindingAttr, binder, returnType, types, modifiers);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetProperties(bindingAttr);

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetMember(name, bindingAttr);

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetMember(name, type, bindingAttr);

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetMembers(bindingAttr);

        public override MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member)
            => GetRuntimeTypeInfo().GetMemberWithSameMetadataDefinitionAs(member);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
            => GetRuntimeTypeInfo().InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase)
            => GetRuntimeTypeInfo().GetInterface(name, ignoreCase);

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
            => GetRuntimeTypeInfo().GetInterfaceMap(interfaceType);

        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.PublicEvents
            | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        public override MemberInfo[] GetDefaultMembers()
            => GetRuntimeTypeInfo().GetDefaultMembers();

        public override bool IsDefined(Type attributeType, bool inherit)
            => GetRuntimeTypeInfo().IsDefined(attributeType, inherit);

        public override object[] GetCustomAttributes(bool inherit)
        {
            if (IsReflectionDisabled && DoNotThrowForAttributes)
                return Array.Empty<object>();

            return GetRuntimeTypeInfo().GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (IsReflectionDisabled && DoNotThrowForAttributes)
                return Array.Empty<object>();

            return GetRuntimeTypeInfo().GetCustomAttributes(attributeType, inherit);
        }

        public override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                if (IsReflectionDisabled && DoNotThrowForAttributes)
                    return Array.Empty<CustomAttributeData>();

                return GetRuntimeTypeInfo().CustomAttributes;
            }
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            if (IsReflectionDisabled && DoNotThrowForAttributes)
                return Array.Empty<CustomAttributeData>();

            return GetRuntimeTypeInfo().GetCustomAttributesData();
        }

        public override string Name
        {
            get
            {
                if (IsReflectionDisabled && DoNotThrowForNames)
                    return ToString();

                return GetRuntimeTypeInfo().Name;
            }
        }

        public override string? Namespace
        {
            get
            {
                if (IsReflectionDisabled && DoNotThrowForNames)
                    return null;

                return GetRuntimeTypeInfo().Namespace;
            }
        }

        public override string? AssemblyQualifiedName => GetRuntimeTypeInfo().AssemblyQualifiedName;

        public override string? FullName => GetRuntimeTypeInfo().FullName;

        public override Assembly Assembly
        {
            get
            {
                if (IsReflectionDisabled && DoNotThrowForAssembly)
                    return Assembly.GetExecutingAssembly();

                return GetRuntimeTypeInfo().Assembly;
            }
        }

        public override Module Module => GetRuntimeTypeInfo().Module;

        public override Guid GUID => GetRuntimeTypeInfo().GUID;

        public override bool HasSameMetadataDefinitionAs(MemberInfo other) => GetRuntimeTypeInfo().HasSameMetadataDefinitionAs(other);

        public override Type MakePointerType()
            => GetRuntimeTypeInfo().MakePointerType();

        public override Type MakeByRefType()
            => GetRuntimeTypeInfo().MakeByRefType();

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType()
            => GetRuntimeTypeInfo().MakeArrayType();

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType(int rank)
            => GetRuntimeTypeInfo().MakeArrayType(rank);

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] instantiation)
            => GetRuntimeTypeInfo().MakeGenericType(instantiation);
    }
}
