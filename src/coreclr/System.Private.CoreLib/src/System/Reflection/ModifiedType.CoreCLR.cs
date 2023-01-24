// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal partial class ModifiedType
    {
        /// <summary>
        /// Called from FieldInfo, PropertyInfo and ParameterInfo to create a modified type tree.
        /// </summary>
        public static ModifiedType CreateRoot(
            Type unmodifiedType,
            object? signatureProvider,
            int rootSignatureParameterIndex) => Create(
                unmodifiedType,
                signatureProvider,
                rootSignatureParameterIndex,
                nestedSignatureIndex: -1,
                nestedSignatureParameterIndex: -1,
                isRoot: true);

        private Type[] GetCustomModifiers(bool required)
        {
            Type[] modifiers = EmptyTypes;
            Signature? signature = GetSignature();
            if (signature is not null)
            {
                if (IsRoot)
                {
                    // For a root node, which is the original field\parameter\property Type, get the root-level modifiers.
                    modifiers = signature.GetCustomModifiers(RootSignatureParameterIndex, required);
                }
                else if (NestedSignatureParameterIndex >= 0)
                {
                    modifiers = signature.GetCustomModifiers(RootSignatureParameterIndex, required, NestedSignatureIndex, NestedSignatureParameterIndex);
                }
            }

            return modifiers;
        }

        internal Signature? GetSignature()
        {
            return (Signature?)SignatureProvider; // Signature is a CoreClr-specific class.
        }
    }
}
