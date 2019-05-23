// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Emit
{
    public class ParameterBuilder
    {
        // Set the default value of the parameter
        public virtual void SetConstant(object? defaultValue)
        {
            TypeBuilder.SetConstantValue(
                _methodBuilder.GetModuleBuilder(),
                _token.Token,
                _position == 0 ? _methodBuilder.ReturnType! : _methodBuilder.m_parameterTypes![_position - 1],
                defaultValue);
        }

        // Use this function if client decides to form the custom attribute blob themselves
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
            {
                throw new ArgumentNullException(nameof(con));
            }
            if (binaryAttribute == null)
            {
                throw new ArgumentNullException(nameof(binaryAttribute));
            }

            TypeBuilder.DefineCustomAttribute(
                _methodBuilder.GetModuleBuilder(),
                _token.Token,
                ((ModuleBuilder)_methodBuilder.GetModule()).GetConstructorToken(con).Token,
                binaryAttribute,
                false, false);
        }

        // Use this function if client wishes to build CustomAttribute using CustomAttributeBuilder
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException(nameof(customBuilder));
            }
            customBuilder.CreateCustomAttribute((ModuleBuilder)(_methodBuilder.GetModule()), _token.Token);
        }

        internal ParameterBuilder(
            MethodBuilder methodBuilder,
            int sequence,
            ParameterAttributes attributes,
            string? paramName)            // can be NULL string
        {
            _position = sequence;
            _name = paramName;
            _methodBuilder = methodBuilder;
            _attributes = attributes;
            ModuleBuilder module = _methodBuilder.GetModuleBuilder();
            _token = new ParameterToken(TypeBuilder.SetParamInfo(
                        JitHelpers.GetQCallModuleOnStack(ref module),
                        _methodBuilder.GetToken().Token,
                        sequence,
                        attributes,
                        paramName));
        }

        public virtual ParameterToken GetToken()
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
        private MethodBuilder _methodBuilder;
        private ParameterToken _token;
    }
}
