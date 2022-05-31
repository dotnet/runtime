// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection
{
    internal partial class RuntimeFunctionPointerParameterInfo
    {
        private readonly int _position;
        private readonly Signature _signature;
        private List<Type>? _optionalModifiers;

        public RuntimeFunctionPointerParameterInfo(Type parameterType, int position, Signature signature)
        {
            _parameterType = parameterType;
            _position = position;
            _signature = signature;
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return _optionalModifiers == null ?
                _signature.GetCustomModifiers(_position + 1, required: false) :
                _optionalModifiers.ToArray();
        }

        // Expose the List in order to add calling conventions later.
        internal List<Type> GetOptionalCustomModifiersList()
        {
            _optionalModifiers ??= new List<Type>(_signature.GetCustomModifiers(_position + 1, required: false));
            return _optionalModifiers;
        }

        public override Type[] GetRequiredCustomModifiers() => _signature.GetCustomModifiers(_position + 1, required: true);
    }
}
