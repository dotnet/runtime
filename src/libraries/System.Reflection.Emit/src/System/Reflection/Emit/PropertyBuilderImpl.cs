// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Metadata;

namespace System.Reflection.Emit
{
    internal sealed class PropertyBuilderImpl : PropertyBuilder
    {
        private readonly string _name;
        private readonly CallingConventions _callingConvention;
        private readonly Type _propertyType;
        private readonly Type[]? _parameterTypes;
        private readonly TypeBuilderImpl _containingType;
        private PropertyAttributes _attributes;
        private MethodInfo? _getMethod;
        private MethodInfo? _setMethod;
        internal HashSet<MethodInfo>? _otherMethods;
        internal readonly Type[]? _returnTypeRequiredCustomModifiers;
        internal readonly Type[]? _returnTypeOptionalCustomModifiers;
        internal readonly Type[][]? _parameterTypeRequiredCustomModifiers;
        internal readonly Type[][]? _parameterTypeOptionalCustomModifiers;
        internal PropertyDefinitionHandle _handle;
        internal List<CustomAttributeWrapper>? _customAttributes;
        internal object? _defaultValue = DBNull.Value;

        internal PropertyBuilderImpl(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers, TypeBuilderImpl containingType)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            _name = name;
            _attributes = attributes;
            _callingConvention = callingConvention;
            _propertyType = returnType;
            _parameterTypes = parameterTypes;
            _containingType = containingType;
            _returnTypeRequiredCustomModifiers = returnTypeRequiredCustomModifiers;
            _returnTypeOptionalCustomModifiers = returnTypeOptionalCustomModifiers;
            _parameterTypeRequiredCustomModifiers = parameterTypeRequiredCustomModifiers;
            _parameterTypeOptionalCustomModifiers = parameterTypeOptionalCustomModifiers;
        }

        internal Type[]? ParameterTypes => _parameterTypes;
        internal CallingConventions CallingConventions => _callingConvention;

        protected override void AddOtherMethodCore(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            _containingType.ThrowIfCreated();

            _otherMethods ??= new HashSet<MethodInfo>();
            _otherMethods.Add(mdBuilder);
        }

        protected override void SetConstantCore(object? defaultValue)
        {
            _containingType.ThrowIfCreated();
            FieldBuilderImpl.ValidateDefaultValueType(defaultValue, _propertyType);
            _defaultValue = defaultValue;
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            _containingType.ThrowIfCreated();

            if (con.ReflectedType!.FullName == "System.Runtime.CompilerServices.SpecialNameAttribute")
            {
                _attributes |= PropertyAttributes.SpecialName;
                return;
            }

            _customAttributes ??= new List<CustomAttributeWrapper>();
            _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
        }

        protected override void SetGetMethodCore(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            _containingType.ThrowIfCreated();

            _getMethod = mdBuilder;
        }

        protected override void SetSetMethodCore(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            _containingType.ThrowIfCreated();

            _setMethod = mdBuilder;
        }

        public override Module Module => _containingType.Module;

        public override Type PropertyType => _propertyType;

        public override PropertyAttributes Attributes => _attributes;

        public override bool CanRead => _getMethod != null ? true : false;

        public override bool CanWrite => _setMethod != null ? true : false;

        public override string Name => _name;

        public override Type? DeclaringType => _containingType;

        public override Type? ReflectedType => _containingType;

        public override MethodInfo? GetGetMethod(bool nonPublic)
        {
            if (nonPublic || _getMethod == null)
            {
                return _getMethod;
            }

            if ((_getMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
            {
                return _getMethod;
            }

            return null;
        }

        public override MethodInfo? GetSetMethod(bool nonPublic)
        {
            if (nonPublic || _setMethod == null)
            {
                return _setMethod;
            }

            if ((_setMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
            {
                return _setMethod;
            }

            return null;
        }

        public override object? GetConstantValue() => _defaultValue == DBNull.Value ? null : _defaultValue;

        public override object GetValue(object? obj, object?[]? index) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override object GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture) =>
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override void SetValue(object? obj, object? value, object?[]? index) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture) =>
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override MethodInfo[] GetAccessors(bool nonPublic) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override ParameterInfo[] GetIndexParameters() => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
    }
}
