// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Runtime.General;

using NativeFormatTypeSignature = Internal.Metadata.NativeFormat.TypeSignature;

namespace System.Reflection
{
    internal partial class ModifiedType
    {
        internal readonly struct TypeSignature
        {
            internal readonly NativeFormatTypeSignature _signature;
            internal readonly TypeContext _typeContext;

            internal TypeSignature(NativeFormatTypeSignature signature, TypeContext typeContext)
                => (_signature, _typeContext) = (signature, typeContext);
        }

        internal Type GetTypeParameter(Type unmodifiedType, int index) =>
            Create(unmodifiedType, new TypeSignature(
                _typeSignature._signature.Reader is not null ? _typeSignature._signature.GetTypeParameter(index) : default,
                _typeSignature._typeContext));

        internal SignatureCallingConvention GetCallingConventionFromFunctionPointer() =>
            _typeSignature._signature.Reader is not null ?
                (SignatureCallingConvention)_typeSignature._signature.GetCallingConventionFromFunctionPointer() : default;

        internal static Type Create(Type unmodifiedType, NativeFormatTypeSignature signature, TypeContext typeContext) =>
            Create(unmodifiedType, new TypeSignature(signature, typeContext));

        internal static Type Create(Type unmodifiedType) =>
            Create(unmodifiedType, default(TypeSignature));

        private Type[] GetCustomModifiers(bool isOptional) =>
            _typeSignature._signature.Reader is not null ?
                _typeSignature._signature.GetCustomModifiers(_typeSignature._typeContext, isOptional) : [];
    }
}
