// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class ModifiedFunctionPointerType : ModifiedType
    {
        private const string CallingConventionTypePrefix = "System.Runtime.CompilerServices.CallConv";

        private readonly ModifiedType[] _parameterTypes;
        private readonly ModifiedType _returnType;

        public ModifiedFunctionPointerType(
            Type functionPointerType,
            object? signatureProvider,
            int rootSignatureParameterIndex,
            ref int nestedSignatureIndex,
            int nestedSignatureParameterIndex,
            bool isRoot)
            : base(
                  functionPointerType,
                  signatureProvider,
                  rootSignatureParameterIndex,
                  ++nestedSignatureIndex,
                  nestedSignatureParameterIndex,
                  isRoot)
        {
            Debug.Assert(functionPointerType.IsFunctionPointer);
            _returnType = Create(
                functionPointerType.GetFunctionPointerReturnType(),
                signatureProvider,
                rootSignatureParameterIndex,
                ref nestedSignatureIndex,
                nestedSignatureParameterIndex: 0);

            Type[] parameters = functionPointerType.GetFunctionPointerParameterTypes();
            int count = parameters.Length;
            ModifiedType[] modifiedTypes = new ModifiedType[count];
            for (int i = 0; i < count; i++)
            {
                modifiedTypes[i] = Create(
                    parameters[i],
                    signatureProvider,
                    rootSignatureParameterIndex,
                    ref nestedSignatureIndex,
                    nestedSignatureParameterIndex: i + 1);
            }

            _parameterTypes = modifiedTypes;
        }

        public override Type GetFunctionPointerReturnType() => _returnType;
        public override Type[] GetFunctionPointerParameterTypes() => CloneArray<Type>(_parameterTypes);

        public override Type[] GetFunctionPointerCallingConventions()
        {
            ArrayBuilder<Type> builder = default;

            // Normalize the calling conventions by manufacturing a type.
            switch (GetCallingConvention())
            {
                case SignatureCallingConvention.Cdecl:
                    builder.Add(typeof(CallConvCdecl));
                    break;
                case SignatureCallingConvention.StdCall:
                    builder.Add(typeof(CallConvStdcall));
                    break;
                case SignatureCallingConvention.ThisCall:
                    builder.Add(typeof(CallConvThiscall));
                    break;
                case SignatureCallingConvention.FastCall:
                    builder.Add(typeof(CallConvFastcall));
                    break;
                case SignatureCallingConvention.Unmanaged:
                    // For the above cases, there will be no other custom calling convention modifiers.
                    foreach (Type type in GetFunctionPointerReturnType().GetOptionalCustomModifiers())
                    {
                        if (type.FullName!.StartsWith(CallingConventionTypePrefix, StringComparison.Ordinal))
                        {
                            builder.Add(type);
                        }
                    }
                    break;
            }

            return builder.Count == 0 ? EmptyTypes : builder.ToArray();
        }
    }
}
