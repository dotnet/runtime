// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal sealed partial class ModifiedGenericType : ModifiedType
    {
        private readonly ModifiedType[] _argumentTypes;

        public ModifiedGenericType(
            Type genericType,
            object? signatureProvider,
            int rootSignatureParameterIndex,
            ref int nestedSignatureIndex,
            int nestedSignatureParameterIndex,
            bool isRoot)
            : base(
                  genericType,
                  signatureProvider,
                  rootSignatureParameterIndex,
                  ++nestedSignatureIndex,
                  nestedSignatureParameterIndex,
                  isRoot)
        {
            Debug.Assert(genericType.IsGenericType);

            Type[] genericArguments = genericType.GetGenericArguments();
            int count = genericArguments.Length;
            ModifiedType[] modifiedTypes = new ModifiedType[count];
            for (int i = 0; i < count; i++)
            {
                modifiedTypes[i] = Create(
                    genericArguments[i],
                    signatureProvider,
                    rootSignatureParameterIndex,
                    ref nestedSignatureIndex,
                    // Since generic signatures don't have a return type, we use +1 here.
                    nestedSignatureParameterIndex: i + 1);
            }

            _argumentTypes = modifiedTypes;
        }

        public override Type[] GetGenericArguments() => CloneArray<Type>(_argumentTypes);
    }
}
