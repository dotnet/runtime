// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal partial class ModifiedType
    {
        private Signature? _signature; // CoreClr-specific class.

        /// <summary>
        /// Called from FieldInfo, PropertyInfo and ParameterInfo to create a modified type tree.
        /// </summary>
        public static ModifiedType Create(
            Type unmodifiedType,
            object rootFieldParameterOrProperty,
            Signature? signature,
            int rootSignatureParameterIndex)
        {
            Debug.Assert(
                rootFieldParameterOrProperty is FieldInfo ||
                rootFieldParameterOrProperty is ParameterInfo ||
                rootFieldParameterOrProperty is PropertyInfo);

            ModifiedType modifiedType;

            if (unmodifiedType.IsFunctionPointer)
            {
                modifiedType = new ModifiedFunctionPointerType(unmodifiedType, rootFieldParameterOrProperty, rootSignatureParameterIndex);
            }
            else if (unmodifiedType.HasElementType)
            {
                modifiedType = new ModifiedContainerType(unmodifiedType, rootFieldParameterOrProperty, rootSignatureParameterIndex);
            }
            else if (unmodifiedType.IsGenericType)
            {
                modifiedType = new ModifiedGenericType(unmodifiedType, rootFieldParameterOrProperty, rootSignatureParameterIndex);
            }
            else
            {
                modifiedType = new ModifiedStandaloneType(unmodifiedType, rootFieldParameterOrProperty, rootSignatureParameterIndex);
            }

            modifiedType._signature = signature;
            return modifiedType;
        }

        public Signature? GetSignature() => Root._signature;

        private Type[] GetCustomModifiersFromSignature(bool required)
        {
            Debug.Assert(_nestedSignatureParameterIndex >= 0);

            Signature? signature = GetSignature();
            if (signature != null)
            {
                return signature.GetCustomModifiers(RootSignatureParameterIndex, required, _nestedSignatureIndex, _nestedSignatureParameterIndex) ?? EmptyTypes;
            }

            return EmptyTypes;
        }
    }
}
