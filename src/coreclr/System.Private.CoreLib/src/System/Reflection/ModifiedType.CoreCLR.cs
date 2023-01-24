// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal partial class ModifiedType
    {
        private Signature? _signature; // Signature is a CoreClr-specific class.

        /// <summary>
        /// Called from FieldInfo, PropertyInfo and ParameterInfo to create a modified type tree.
        /// </summary>
        public static ModifiedType Create(Type unmodifiedType, int rootSignatureParameterIndex, Signature? signature)
        {
            ModifiedType modifiedType = Create(unmodifiedType, rootSignatureParameterIndex);
            modifiedType._signature = signature;
            return modifiedType;
        }

        public Signature? GetSignature() => Root._signature;

        private Type[] GetCustomModifiersFromSignature(bool required)
        {
            Debug.Assert(_nestedSignatureParameterIndex >= 0);

            Type[] modifiers;
            Signature? signature = GetSignature();
            if (signature is not null)
            {
                if (ReferenceEquals(this, Root))
                {
                    // For a root node, which is a Type (not a parameter), ask for the root-level modifiers.
                    modifiers = signature.GetCustomModifiers(RootSignatureParameterIndex, required);
                }
                else
                {
                    modifiers = signature.GetCustomModifiers(RootSignatureParameterIndex, required, _nestedSignatureIndex, _nestedSignatureParameterIndex);
                }
            }
            else
            {
                modifiers = EmptyTypes;
            }

            return modifiers;
        }
    }
}
