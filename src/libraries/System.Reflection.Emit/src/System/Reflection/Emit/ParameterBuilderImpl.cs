﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection.Emit
{
    internal sealed class ParameterBuilderImpl : ParameterBuilder
    {
        private readonly string? _name;
        private readonly int _position;
        private readonly MethodBuilderImpl _methodBuilder;
        private ParameterAttributes _attributes;

        internal List<CustomAttributeWrapper>? _customAttributes;
        internal MarshallingData? _marshallingData;
        internal object? _defaultValue = DBNull.Value;

        public ParameterBuilderImpl(MethodBuilderImpl methodBuilder, int sequence, ParameterAttributes attributes, string? paramName)
        {
            _position = sequence;
            _name = paramName;
            _methodBuilder = methodBuilder;
            _attributes = attributes;
        }

        public override int Attributes => (int)_attributes;

        public override string? Name => _name;

        public override int Position => _position;

        public override void SetConstant(object? defaultValue) => _defaultValue = defaultValue;

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            switch (con.ReflectedType!.FullName)
            {
                case "System.Runtime.InteropServices.InAttribute":
                    _attributes |= ParameterAttributes.In;
                    return;
                case "System.Runtime.InteropServices.OutAttribute":
                    _attributes |= ParameterAttributes.Out;
                    return;
                case "System.Runtime.InteropServices.OptionalAttribute":
                    _attributes |= ParameterAttributes.Optional;
                    return;
                case "System.Runtime.InteropServices.MarshalAsAttribute":
                    _attributes |= ParameterAttributes.HasFieldMarshal;
                    _marshallingData = MarshallingData.CreateMarshallingData(con, binaryAttribute, isField: false);
                    return;
                case "System.Runtime.InteropServices.DefaultParameterValueAttribute":
                    // MS.NET doesn't handle this attribute but we handle it for consistency TODO: not sure if we need to handle this
                    CustomAttributeInfo caInfo = CustomAttributeInfo.DecodeCustomAttribute(con, binaryAttribute);
                    SetConstant(caInfo._ctorArgs[0]);
                    return;
            }

            _customAttributes ??= new List<CustomAttributeWrapper>();
            _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
        }
    }
}
