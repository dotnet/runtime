// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection.Emit
{
    internal sealed class RuntimeParameterBuilder : ParameterBuilder
    {
        // Set the default value of the parameter
        public override void SetConstant(object? defaultValue)
        {
            RuntimeTypeBuilder.SetConstantValue(
                _methodBuilder.GetModuleBuilder(),
                _token,
                _position == 0 ? _methodBuilder.ReturnType : _methodBuilder.m_parameterTypes![_position - 1],
                defaultValue);
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            RuntimeTypeBuilder.DefineCustomAttribute(
                _methodBuilder.GetModuleBuilder(),
                _token,
                ((RuntimeModuleBuilder)_methodBuilder.GetModule()).GetMethodMetadataToken(con),
                binaryAttribute);
        }

        internal RuntimeParameterBuilder(
            RuntimeMethodBuilder methodBuilder,
            int sequence,
            ParameterAttributes attributes,
            string? paramName)            // can be NULL string
        {
            _position = sequence;
            _name = paramName;
            _methodBuilder = methodBuilder;
            _attributes = attributes;
            RuntimeModuleBuilder module = _methodBuilder.GetModuleBuilder();
            _token = RuntimeTypeBuilder.SetParamInfo(
                        new QCallModule(ref module),
                        _methodBuilder.MetadataToken,
                        sequence,
                        attributes,
                        paramName);
        }

        internal int GetToken()
        {
            return _token;
        }

        public override string? Name => _name;

        public override int Position => _position;

        public override int Attributes => (int)_attributes;

        private readonly string? _name;
        private readonly int _position;
        private readonly ParameterAttributes _attributes;
        private readonly RuntimeMethodBuilder _methodBuilder;
        private readonly int _token;
    }
}
