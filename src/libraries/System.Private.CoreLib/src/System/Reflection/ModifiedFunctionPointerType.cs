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
        private Type[]? _callingConventions;

        /// <summary>
        /// Create a root node.
        /// </summary>
        public ModifiedFunctionPointerType(
            Type functionPointerType,
            object rootFieldParameterOrProperty,
            int rootSignatureParameterIndex)
            : base(functionPointerType, rootFieldParameterOrProperty, rootSignatureParameterIndex)
        {
            Debug.Assert(functionPointerType.IsFunctionPointer);
            _returnType = Create(functionPointerType.GetFunctionPointerReturnType(), this, nestedSignatureIndex: 0, nestedSignatureParameterIndex: 0);
            _parameterTypes = CreateParameters(functionPointerType.GetFunctionPointerParameterTypes(), this, nestedSignatureIndex: 0);
        }

        /// <summary>
        /// Create a child node.
        /// </summary>
        public ModifiedFunctionPointerType(
            Type functionPointerType,
            ModifiedType root,
            int nestedSignatureIndex,
            int nestedSignatureParameterIndex)
            : base(functionPointerType, root, nestedSignatureIndex, nestedSignatureParameterIndex)
        {
            Debug.Assert(functionPointerType.IsFunctionPointer);
            _returnType = Create(functionPointerType.GetFunctionPointerReturnType(), root, nestedSignatureIndex + 1, nestedSignatureParameterIndex: 0);
            _parameterTypes = CreateParameters(functionPointerType.GetFunctionPointerParameterTypes(), root, nestedSignatureIndex + 1);
        }

        public override Type GetFunctionPointerReturnType() => _returnType;
        public override Type[] GetFunctionPointerParameterTypes() => CloneArray<Type>(_parameterTypes);

        public override Type[] GetFunctionPointerCallingConventions()
        {
            _callingConventions ??= CreateCallingConventions();
            return CloneArray(_callingConventions);
        }

        private static ModifiedType[] CreateParameters(Type[] parameterTypes, ModifiedType root, int nestedSignatureIndex)
        {
            int count = parameterTypes.Length;
            ModifiedType[] modifiedTypes = new ModifiedType[count];
            for (int i = 0; i < count; i++)
            {
                modifiedTypes[i] = Create(parameterTypes[i], root, nestedSignatureIndex, nestedSignatureParameterIndex: i + 1);
            }

            return modifiedTypes;
        }

        private Type[] CreateCallingConventions()
        {
            Type[] returnTypeOptionalModifiers = GetFunctionPointerReturnType().GetOptionalCustomModifiers();

            ArrayBuilder<Type> builder = default;

            bool foundCallingConvention = false;

            for (int i = 0; i < returnTypeOptionalModifiers.Length; i++)
            {
                Type type = returnTypeOptionalModifiers[i];
                if (type.FullName!.StartsWith(CallingConventionTypePrefix, StringComparison.Ordinal))
                {
                    builder.Add(type);

                    if (type == typeof(CallConvCdecl) ||
                        type == typeof(CallConvFastcall) ||
                        type == typeof(CallConvStdcall) ||
                        type == typeof(CallConvThiscall))
                    {
                        foundCallingConvention = true;
                    }
                }
            }

            if (!foundCallingConvention)
            {
                // Normalize the calling conventions by manufacturing a type.
                switch (GetCallingConvention())
                {
                    case MdSigCallingConvention.C:
                        builder.Add(typeof(CallConvCdecl));
                        break;
                    case MdSigCallingConvention.StdCall:
                        builder.Add(typeof(CallConvStdcall));
                        break;
                    case MdSigCallingConvention.ThisCall:
                        builder.Add(typeof(CallConvThiscall));
                        break;
                    case MdSigCallingConvention.FastCall:
                        builder.Add(typeof(CallConvFastcall));
                        break;
                }
            }

            return builder.Count == 0 ? EmptyTypes : builder.ToArray();
        }
    }
}
