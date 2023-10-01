// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Runtime.TypeInfos;
using System.Runtime.CompilerServices;
using Internal.Reflection.Core.Execution;
using Internal.Runtime;
using Internal.Runtime.Augments;

namespace System
{
    // Runtime implemented Type
    public sealed unsafe class RuntimeType : TypeInfo, ICloneable
    {
        private MethodTable* _pUnderlyingEEType;
        private RuntimeTypeInfo? _runtimeTypeInfo;

        internal RuntimeType(MethodTable* pUnderlyingEEType)
        {
            _pUnderlyingEEType = pUnderlyingEEType;
        }

        internal EETypePtr ToEETypePtr() => new EETypePtr(_pUnderlyingEEType);

        internal RuntimeTypeInfo GetRuntimeTypeInfo() => _runtimeTypeInfo ?? CreateRuntimeTypeInfo();

        private RuntimeTypeInfo CreateRuntimeTypeInfo()
        {
            EETypePtr eeType = ToEETypePtr();

            RuntimeTypeHandle runtimeTypeHandle = new RuntimeTypeHandle(eeType);

            RuntimeTypeInfo runtimeTypeInfo;

            if (eeType.IsDefType)
            {
                if (eeType.IsGeneric)
                {
                    runtimeTypeInfo = ExecutionDomain.GetConstructedGenericTypeForHandle(runtimeTypeHandle);
                }
                else
                {
                    runtimeTypeInfo = ReflectionCoreExecution.ExecutionDomain.GetNamedTypeForHandle(runtimeTypeHandle);
                }
            }
            else if (eeType.IsArray)
            {
                if (!eeType.IsSzArray)
                    runtimeTypeInfo = ExecutionDomain.GetMdArrayTypeForHandle(runtimeTypeHandle, eeType.ArrayRank);
                else
                    runtimeTypeInfo = ExecutionDomain.GetArrayTypeForHandle(runtimeTypeHandle);
            }
            else if (eeType.IsPointer)
            {
                runtimeTypeInfo = ExecutionDomain.GetPointerTypeForHandle(runtimeTypeHandle);
            }
            else if (eeType.IsFunctionPointer)
            {
                runtimeTypeInfo = ExecutionDomain.GetFunctionPointerTypeForHandle(runtimeTypeHandle);
            }
            else if (eeType.IsByRef)
            {
                runtimeTypeInfo = ExecutionDomain.GetByRefTypeForHandle(runtimeTypeHandle);
            }
            else
            {
                Debug.Fail("Invalid RuntimeTypeHandle");
                throw new ArgumentException(SR.Arg_InvalidHandle);
            }

            return (_runtimeTypeInfo = runtimeTypeInfo);
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
                    if (value.GetEETypePtr() != this.ToEETypePtr())
                        throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, value.GetType(), this));
                }
                else
                {
                    Type underlyingType = Enum.InternalGetUnderlyingType(this);
                    if (!(underlyingType.TypeHandle.ToEETypePtr() == value.GetEETypePtr()))
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

        object ICloneable.Clone()
            => this;

        public override bool IsSecurityCritical => true;
        public override bool IsSecuritySafeCritical => false;
        public override bool IsSecurityTransparent => false;

        public override Type UnderlyingSystemType => this;

        public override RuntimeTypeHandle TypeHandle
            => new RuntimeTypeHandle(_pUnderlyingEEType);

        internal new unsafe bool IsInterface
            => _pUnderlyingEEType->IsInterface;

        protected override bool IsValueTypeImpl()
            => _pUnderlyingEEType->IsValueType;

        internal bool IsActualValueType
            => _pUnderlyingEEType->IsValueType;

        public override unsafe bool IsEnum
            => new EETypePtr(_pUnderlyingEEType).IsEnum;

        internal unsafe bool IsActualEnum
            => new EETypePtr(_pUnderlyingEEType).IsEnum;

        protected override unsafe bool IsArrayImpl()
            => _pUnderlyingEEType->IsArray;

        protected override unsafe bool IsByRefImpl()
            => _pUnderlyingEEType->IsByRef;

        protected override unsafe bool IsPointerImpl()
            => _pUnderlyingEEType->IsPointer;

        protected override unsafe bool HasElementTypeImpl()
            => _pUnderlyingEEType->IsParameterizedType;

        public override Type? GetElementType()
            => _pUnderlyingEEType->IsParameterizedType ? GetTypeFromMethodTable(_pUnderlyingEEType->RelatedParameterType) : null;

        public override Type? BaseType => throw new NotImplementedException();

        protected override TypeAttributes GetAttributeFlagsImpl() => throw new NotImplementedException();
        protected override bool IsCOMObjectImpl() => false;
        protected override bool IsPrimitiveImpl() => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string name, BindingFlags bindingAttr) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase) => throw new NotImplementedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces() => throw new NotImplementedException();
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

        public override string? Namespace => GetRuntimeTypeInfo().Namespace;

        public override string? AssemblyQualifiedName => GetRuntimeTypeInfo().AssemblyQualifiedName;

        public override string? FullName => GetRuntimeTypeInfo().FullName;

        public override Assembly Assembly => GetRuntimeTypeInfo().Assembly;

        public override Module Module => GetRuntimeTypeInfo().Module;

        public override Guid GUID => GetRuntimeTypeInfo().GUID;

        public override string Name => GetRuntimeTypeInfo().Name;
    }
}
