﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Reflection.Emit
{
    internal sealed class FieldBuilderImpl : FieldBuilder
    {
        private readonly TypeBuilderImpl _typeBuilder;
        private readonly string _fieldName;
        private readonly Type _fieldType;
        private FieldAttributes _attributes;

        internal MarshallingData? _marshallingData;
        internal int _offset;
        internal List<CustomAttributeWrapper>? _customAttributes;
        internal object? _defaultValue = DBNull.Value;

        internal FieldBuilderImpl(TypeBuilderImpl typeBuilder, string fieldName, Type type, FieldAttributes attributes)
        {
            _fieldName = fieldName;
            _typeBuilder = typeBuilder;
            _fieldType = type;
            _attributes = attributes & ~FieldAttributes.ReservedMask;
            _offset = -1;
        }

        protected override void SetConstantCore(object? defaultValue)
        {
            if (defaultValue == null)
            {
                // nullable value types can hold null value.
                if (_fieldType.IsValueType && !(_fieldType.IsGenericType && _fieldType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    throw new ArgumentException(SR.Argument_ConstantNull);
            }
            else
            {
                Type type = defaultValue.GetType();
                Type destType = _fieldType;

                // We should allow setting a constant value on a ByRef parameter
                if (destType.IsByRef)
                    destType = destType.GetElementType()!;

                // Convert nullable types to their underlying type.
                destType = Nullable.GetUnderlyingType(destType) ?? destType;

                if (destType.IsEnum)
                {
                    Type underlyingType;
                    if (destType is EnumBuilderImpl enumBldr)
                    {
                        underlyingType = enumBldr.GetEnumUnderlyingType();

                        if (type != enumBldr._typeBuilder.UnderlyingSystemType && type != underlyingType)
                            throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
                    }
                    else if (destType is TypeBuilderImpl typeBldr)
                    {
                        underlyingType = typeBldr.UnderlyingSystemType;

                        if (underlyingType == null || (type != typeBldr.UnderlyingSystemType && type != underlyingType))
                            throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
                    }
                    else
                    {
                        underlyingType = Enum.GetUnderlyingType(destType);

                        if (type != destType && type != underlyingType)
                            throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
                    }
                }
                else
                {
                    if (!destType.IsAssignableFrom(type))
                        throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
                }

                _defaultValue = defaultValue;
            }
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            // Handle pseudo custom attributes
            switch (con.ReflectedType!.FullName)
            {
                case "System.Runtime.InteropServices.FieldOffsetAttribute":
                    Debug.Assert(binaryAttribute.Length >= 6);
                    _offset = BinaryPrimitives.ReadInt32LittleEndian(binaryAttribute.Slice(2));
                    return;
                case "System.NonSerializedAttribute":
#pragma warning disable SYSLIB0050 // 'FieldAttributes.NotSerialized' is obsolete: 'Formatter-based serialization is obsolete and should not be used'.
                    _attributes |= FieldAttributes.NotSerialized;
#pragma warning restore SYSLIB0050
                    return;
                case "System.Runtime.CompilerServices.SpecialNameAttribute":
                    _attributes |= FieldAttributes.SpecialName;
                    return;
                case "System.Runtime.InteropServices.MarshalAsAttribute":
                    _attributes |= FieldAttributes.HasFieldMarshal;
                    _marshallingData = MarshallingData.CreateMarshallingData(con, binaryAttribute, isField : true);
                    return;
            }

            _customAttributes ??= new List<CustomAttributeWrapper>();
            _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
        }

        protected override void SetOffsetCore(int iOffset)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(iOffset);

            _offset = iOffset;
        }

        #region MemberInfo Overrides

        public override int MetadataToken => throw new NotImplementedException();

        public override Module Module => _typeBuilder.Module;

        public override string Name => _fieldName;

        public override Type? DeclaringType => _typeBuilder;

        public override Type? ReflectedType => _typeBuilder;

        #endregion

        #region FieldInfo Overrides
        public override Type FieldType => _fieldType;

        public override object? GetValue(object? obj) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override void SetValue(object? obj, object? val, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
            => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override RuntimeFieldHandle FieldHandle => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override FieldAttributes Attributes => _attributes;

        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        #endregion
    }
}
