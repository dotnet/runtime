// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    internal sealed class RoFunctionPointerParameterInfo : FunctionPointerParameterInfo
    {
        private readonly Type _parameterType;
        private List<Type>? _optionalModifiers;
        private List<Type>? _requiredModifiers;

        public RoFunctionPointerParameterInfo(Type parameterType)
        {
            _parameterType = parameterType;

            RoFunctionPointerDelegator? typeWithModifiers = parameterType as RoFunctionPointerDelegator;
            if (typeWithModifiers != null)
            {
                _optionalModifiers = typeWithModifiers._optionalModifiers;
                _requiredModifiers = typeWithModifiers._requiredModifiers;
            }
        }

        public override Type ParameterType => _parameterType;

        public override Type[] GetOptionalCustomModifiers()
        {
            return _optionalModifiers == null ? Type.EmptyTypes : _optionalModifiers.ToArray();
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return _requiredModifiers == null ? Type.EmptyTypes : _requiredModifiers.ToArray();
        }

        internal List<Type> GetOptionalCustomModifiersList()
        {
            _optionalModifiers ??= new List<Type>(GetOptionalCustomModifiers());
            return _optionalModifiers;
        }

        public override string ToString() => _parameterType.FullName!;
    }
}
