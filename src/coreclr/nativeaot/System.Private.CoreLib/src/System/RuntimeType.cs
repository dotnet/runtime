// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System
{
    // Runtime implemented Type
    public sealed class RuntimeType : TypeInfo, ICloneable
    {
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
                    if (!Enum.ValueTypeMatchesEnumType(this, value))
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

        internal bool IsActualEnum
            => TryGetEEType(out EETypePtr eeType) && eeType.IsEnum;

        object ICloneable.Clone()
            => this;

        public override bool IsSecurityCritical => true;
        public override bool IsSecuritySafeCritical => false;
        public override bool IsSecurityTransparent => false;

        protected override bool IsArrayImpl() => throw new NotImplementedException();
        protected override bool IsByRefImpl() => throw new NotImplementedException();
        protected override bool IsPointerImpl() => throw new NotImplementedException();
        protected override bool HasElementTypeImpl() => throw new NotImplementedException();
        public override Type? GetElementType() => throw new NotImplementedException();
        protected override TypeAttributes GetAttributeFlagsImpl() => throw new NotImplementedException();
        protected override bool IsCOMObjectImpl() => throw new NotImplementedException();
        protected override bool IsPrimitiveImpl() => throw new NotImplementedException();
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => throw new NotImplementedException();
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => throw new NotImplementedException();
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw new NotImplementedException();
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr) => throw new NotImplementedException();
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => throw new NotImplementedException();
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotImplementedException();
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw new NotImplementedException();
        public override Type? GetNestedType(string name, BindingFlags bindingAttr) => throw new NotImplementedException();
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw new NotImplementedException();
        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw new NotImplementedException();
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) => throw new NotImplementedException();
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase) => throw new NotImplementedException();
        public override Type[] GetInterfaces() => throw new NotImplementedException();
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

        public override string? Namespace => throw new NotImplementedException();

        public override string? AssemblyQualifiedName => throw new NotImplementedException();

        public override string? FullName => throw new NotImplementedException();

        public override Assembly Assembly => throw new NotImplementedException();

        public override Module Module => throw new NotImplementedException();

        public override Type UnderlyingSystemType => throw new NotImplementedException();

        public override Guid GUID => throw new NotImplementedException();

        public override Type? BaseType => throw new NotImplementedException();

        public override string Name => throw new NotImplementedException();
    }
}
