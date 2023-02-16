// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection.Emit
{
    public class ParameterBuilder
    {
        // Set the default value of the parameter
        public virtual void SetConstant(object? defaultValue)
        {
            RuntimeTypeBuilder.SetConstantValue(
                _methodBuilder.GetModuleBuilder(),
                _token,
                _position == 0 ? _methodBuilder.ReturnType : _methodBuilder.m_parameterTypes![_position - 1],
                defaultValue);
        }

        // Use this function if client decides to form the custom attribute blob themselves
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            RuntimeTypeBuilder.DefineCustomAttribute(
                _methodBuilder.GetModuleBuilder(),
                _token,
                ((RuntimeModuleBuilder)_methodBuilder.GetModule()).GetMethodMetadataToken(con),
                binaryAttribute);
        }

        // Use this function if client wishes to build CustomAttribute using CustomAttributeBuilder
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            customBuilder.CreateCustomAttribute((RuntimeModuleBuilder)(_methodBuilder.GetModule()), _token);
        }

        internal ParameterBuilder(
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

        public virtual string? Name => _name;

        public virtual int Position => _position;

        public virtual int Attributes => (int)_attributes;

        public bool IsIn => (_attributes & ParameterAttributes.In) != 0;

        public bool IsOut => (_attributes & ParameterAttributes.Out) != 0;

        public bool IsOptional => (_attributes & ParameterAttributes.Optional) != 0;

        private readonly string? _name;
        private readonly int _position;
        private readonly ParameterAttributes _attributes;
        private RuntimeMethodBuilder _methodBuilder;
        private int _token;
    }
}
