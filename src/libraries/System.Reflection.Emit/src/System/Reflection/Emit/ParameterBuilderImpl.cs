// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection.Emit
{
    internal sealed class ParameterBuilderImpl : ParameterBuilder
    {
        private readonly string? _name;
        private readonly int _position;
        private ParameterAttributes _attributes;
        private readonly MethodBuilderImpl _methodBuilder;

        internal List<CustomAttributeWrapper> _customAttributes = new();

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

        public override void SetConstant(object? defaultValue) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            if (!IsPseudoCustomAttribute(con, binaryAttribute))
            {
                _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
            }
        }

        private bool IsPseudoCustomAttribute(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            string? attrname = con.ReflectedType!.FullName;
            switch (attrname)
            {
                case "System.Runtime.InteropServices.InAttribute":
                    _attributes |= ParameterAttributes.In;
                    break;
                case "System.Runtime.InteropServices.OutAttribute":
                    _attributes |= ParameterAttributes.Out;
                    break;
                case "System.Runtime.InteropServices.OptionalAttribute":
                    _attributes |= ParameterAttributes.Optional;
                    break;
                case "System.Runtime.InteropServices.MarshalAsAttribute":
                    _attributes |= ParameterAttributes.HasFieldMarshal;
                    break;
                case "System.Runtime.InteropServices.DefaultParameterValueAttribute":
                    // MS.NET doesn't handle this attribute but we handle it for consistency TODO: not sure if we need to handle this
                    CustomAttributeInfo cinfo = CustomAttributeInfo.DecodeCustomAttribute(con, binaryAttribute);
                    SetConstant(cinfo._ctorArgs[0]);
                    break;
                default: return false;
            }
            return true;
        }
    }
}
