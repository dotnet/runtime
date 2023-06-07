// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal partial class ModifiedType
    {
        internal struct TypeSignature
        {
            internal Signature? _signature;
            internal int _offset;
        }

        internal Type GetTypeParameter(Type unmodifiedType, int index) =>
            Create(unmodifiedType, new TypeSignature()
            {
                _signature = _typeSignature._signature,
                _offset = _typeSignature._signature?.GetTypeParameterOffset(_typeSignature._offset, index) ?? 0
            });

        internal SignatureCallingConvention GetCallingConventionFromFunctionPointer() =>
            _typeSignature._signature?.GetCallingConventionFromFunctionPointerAtOffset(_typeSignature._offset) ?? default;

        internal static Type Create(Type unmodifiedType, Signature? signature, int parameterIndex = 0) =>
            Create(unmodifiedType, new TypeSignature()
            {
                _signature = signature,
                _offset = signature?.GetParameterOffset(parameterIndex) ?? 0
            });

        private Type[] GetCustomModifiers(bool required) =>
             (_typeSignature._signature != null) ?
                _typeSignature._signature.GetCustomModifiersAtOffset(_typeSignature._offset, required) : EmptyTypes;
    }
}
