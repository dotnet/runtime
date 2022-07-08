// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal partial class RuntimeFunctionPointerParameterInfo
    {
        private readonly FunctionPointerInfo _functionPointer;
        private readonly int _position;
        private Type[]? _optionalModifiers;

        public RuntimeFunctionPointerParameterInfo(FunctionPointerInfo functionPointer, Type parameterType, int position)
        {
            _functionPointer = functionPointer;
            _parameterType = parameterType;
            _position = position; // 0 = return value; 1 = first parameter
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            if (_optionalModifiers == null)
            {
                if (_position == 0)
                {
                    // Return value should be normalized. This will call SetCustomModifiersForReturnType.
                    _functionPointer.ComputeCallingConventions();
                    Debug.Assert(_optionalModifiers != null);
                }
                else
                {
                    Type[]? mods = RuntimeTypeHandle.GetCustomModifiersFromFunctionPointer(_functionPointer.FunctionPointerType, _position, required: false);
                    if (mods == null)
                    {
                        _optionalModifiers = Type.EmptyTypes;
                    }
                }
            }

            Debug.Assert(_optionalModifiers != null);
            return _optionalModifiers;
        }

        internal void SetCustomModifiersForReturnType(Type[] modifiers)
        {
            Debug.Assert(_position == 0);
            _optionalModifiers = modifiers;
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            Type[]? value = RuntimeTypeHandle.GetCustomModifiersFromFunctionPointer(_functionPointer.FunctionPointerType, _position, required: true);
            return value ??= Type.EmptyTypes;
        }
    }
}
