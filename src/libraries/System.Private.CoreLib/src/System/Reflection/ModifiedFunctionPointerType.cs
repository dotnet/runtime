// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Reflection
{
    internal sealed partial class ModifiedFunctionPointerType : ModifiedType
    {
        private const string CallingConventionTypePrefix = "System.Runtime.CompilerServices.CallConv";

        private Type[]? _parameterTypes;
        private Type? _returnType;

        internal ModifiedFunctionPointerType(Type unmodifiedType, TypeSignature typeSignature)
            : base(unmodifiedType, typeSignature)
        {
            Debug.Assert(unmodifiedType.IsFunctionPointer);
        }

        public override Type GetFunctionPointerReturnType()
        {
            return _returnType ?? Initialize();

            Type Initialize()
            {
                Interlocked.CompareExchange(ref _returnType, GetTypeParameter(UnmodifiedType.GetFunctionPointerReturnType(), 0), null);
                return _returnType!;
            }
        }

        public override Type[] GetFunctionPointerParameterTypes()
        {
            return (Type[])(_parameterTypes ?? Initialize()).Clone();

            Type[] Initialize()
            {
                Type[] parameterTypes = UnmodifiedType.GetFunctionPointerParameterTypes();
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    parameterTypes[i] = GetTypeParameter(parameterTypes[i], i + 1);
                }
                Interlocked.CompareExchange(ref _parameterTypes, parameterTypes, null);
                return _parameterTypes!;
            }
        }

        public override Type[] GetFunctionPointerCallingConventions()
        {
            ArrayBuilder<Type> builder = default;

            // Normalize the calling conventions by manufacturing a type.
            switch (GetCallingConventionFromFunctionPointer())
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
