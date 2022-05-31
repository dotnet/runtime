// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal partial class ModifiedType
    {
        private Signature? _signature;

        /// <summary>
        /// Called from FieldInfo, PropertyInfo and ParameterInfo to create a modified type tree.
        /// </summary>
        public static ModifiedType Create(Type unmodifiedType, Type[] requiredModifiers, Type[] optionalModifiers, Signature? signature, int rootSignatureParameterIndex)
        {
            ModifiedType modifiedType;

            if (unmodifiedType.IsFunctionPointer)
            {
                modifiedType = new ModifiedFunctionPointerType(unmodifiedType, requiredModifiers, optionalModifiers, rootSignatureParameterIndex);
            }
            else if (unmodifiedType.HasElementType)
            {
                modifiedType = new ModifiedContainerType(unmodifiedType, requiredModifiers, optionalModifiers, rootSignatureParameterIndex);
            }
            else if (unmodifiedType.IsGenericType)
            {
                modifiedType = new ModifiedGenericType(unmodifiedType, requiredModifiers, optionalModifiers, rootSignatureParameterIndex);
            }
            else
            {
                modifiedType = new ModifiedStandaloneType(unmodifiedType, requiredModifiers, optionalModifiers, rootSignatureParameterIndex);
            }

            modifiedType._signature = signature;
            return modifiedType;
        }

        public Signature? GetSignature() => Root._signature;

        private Type[] GetCustomModifiers(bool required)
        {
            if (_nestedSignatureParameterIndex >= 0)
            {
                Signature? signature = GetSignature();
                if (signature != null)
                {
                    return signature.GetCustomModifiers(RootSignatureParameterIndex, required, _nestedSignatureIndex, _nestedSignatureParameterIndex) ?? EmptyTypes;
                }
            }

            return EmptyTypes;
        }
    }
}
