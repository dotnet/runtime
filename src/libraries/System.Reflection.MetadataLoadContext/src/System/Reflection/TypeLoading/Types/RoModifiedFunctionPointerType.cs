// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.TypeLoading;

namespace System.Reflection
{
    /// <summary>
    /// A function pointer type that is modified and contains modified parameter and return types.
    /// </summary>
    internal sealed class RoModifiedFunctionPointerType : RoModifiedType
    {
        private const string CallingConventionTypePrefix = "System.Runtime.CompilerServices.CallConv";

        private readonly Type[] _callingConventions;
        private readonly RoModifiedType[] _parameterTypes;
        private readonly RoModifiedType _returnType;

        public RoModifiedFunctionPointerType(RoFunctionPointerType functionPointerType) : base(functionPointerType)
        {
            Debug.Assert(functionPointerType.IsFunctionPointer);

            Type[] parameterUnmodifiedTypes = functionPointerType._parameterTypes;
            int count = parameterUnmodifiedTypes.Length;

            RoModifiedType[] parameterTypes = new RoModifiedType[count];
            for (int i = 0; i < count; i++)
            {
                RoModifiedType parameter = Create((RoType)parameterUnmodifiedTypes[i]);
                parameterTypes[i] = parameter;
            }

            _parameterTypes = parameterTypes;
            _returnType = Create((RoType)functionPointerType._returnType);

            Type[] returnTypeOptionalModifiers = _returnType.GetOptionalCustomModifiers();
            _callingConventions = CreateCallingConventions(returnTypeOptionalModifiers, functionPointerType);
        }

        public override Type GetFunctionPointerReturnType() => _returnType;
        public override Type[] GetFunctionPointerParameterTypes() => Helpers.CloneArray(_parameterTypes);
        public override Type[] GetFunctionPointerCallingConventions() => Helpers.CloneArray(_callingConventions);

        private Type CDeclType => Loader.GetCoreType(CoreType.CallConvCdecl);
        private Type StdCallType => Loader.GetCoreType(CoreType.CallConvStdcall);
        private Type ThisCallType => Loader.GetCoreType(CoreType.CallConvThiscall);
        private Type FastCallType => Loader.GetCoreType(CoreType.CallConvFastcall);

        private Type[] CreateCallingConventions(Type[] returnTypeOptionalModifiers, RoFunctionPointerType functionPointerType)
        {
            List<Type> builder = new(returnTypeOptionalModifiers.Length + 1);

            // Normalize the calling conventions by manufacturing a type.
            switch (functionPointerType.CallKind)
            {
                case Metadata.SignatureCallingConvention.CDecl:
                    builder.Add(CDeclType);
                    break;
                case Metadata.SignatureCallingConvention.StdCall:
                    builder.Add(StdCallType);
                    break;
                case Metadata.SignatureCallingConvention.ThisCall:
                    builder.Add(ThisCallType);
                    break;
                case Metadata.SignatureCallingConvention.FastCall:
                    builder.Add(FastCallType);
                    break;
                case Metadata.SignatureCallingConvention.Unmanaged:
                    for (int i = 0; i < returnTypeOptionalModifiers.Length; i++)
                    {
                        Type type = returnTypeOptionalModifiers[i];
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
