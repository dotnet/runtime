// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class FunctionPointerInfo
    {
        private const string CallingConventionTypePrefix = "System.Runtime.CompilerServices.CallConv";
        private readonly RuntimeFunctionPointerParameterInfo _returnInfo;
        private readonly RuntimeFunctionPointerParameterInfo[] _parameterInfos;
        private Type[]? _callingConventions;

        public FunctionPointerParameterInfo ReturnParameter => _returnInfo;
        public FunctionPointerParameterInfo[] ParameterInfos => CloneArray(_parameterInfos);

        public Type[] GetCallingConventions()
        {
            if (_callingConventions == null)
            {
                ComputeCallingConventions();
                Debug.Assert(_callingConventions != null);
            }

            return CloneArray(_callingConventions);
        }

        internal void ComputeCallingConventions()
        {
           if (_callingConventions == null)
            {
                ArrayBuilder<Type> ccBuilder = default;
                ArrayBuilder<Type> allBuilder = default;

                Type[]? modifiers = GetCustomModifiersFromFunctionPointer(position: 0, required: false);
                bool foundCallingConvention = false;

                if (modifiers != null)
                {
                    for (int i = 0; i < modifiers.Length; i++)
                    {
                        Type type = modifiers[i];
                        allBuilder.Add(type);
                        if (type.FullName!.StartsWith(CallingConventionTypePrefix, StringComparison.Ordinal))
                        {
                            ccBuilder.Add(type);

                            if (type == typeof(CallConvCdecl) ||
                                type == typeof(CallConvFastcall) ||
                                type == typeof(CallConvStdcall) ||
                                type == typeof(CallConvThiscall))
                            {
                                foundCallingConvention = true;
                            }
                        }
                    }
                }

                // Normalize the calling conventions.
                if (!foundCallingConvention)
                {
                    Type? callConv = null;

                    switch (CallingConvention)
                    {
                        case MdSigCallingConvention.C:
                            callConv = typeof(CallConvCdecl);
                            break;
                        case MdSigCallingConvention.StdCall:
                            callConv = typeof(CallConvStdcall);
                            break;
                        case MdSigCallingConvention.ThisCall:
                            callConv = typeof(CallConvThiscall);
                            break;
                        case MdSigCallingConvention.FastCall:
                            callConv = typeof(CallConvFastcall);
                            break;
                    }

                    if (callConv != null)
                    {
                        allBuilder.Add(callConv);
                        ccBuilder.Add(callConv);
                    }
                }

                _returnInfo.SetCustomModifiersForReturnType(allBuilder.Count == 0 ? Type.EmptyTypes : allBuilder.ToArray());
                _callingConventions = ccBuilder.Count == 0 ? Type.EmptyTypes : ccBuilder.ToArray();
            }
        }

        private static T[] CloneArray<T>(T[] original)
        {
            if (original.Length == 0)
            {
                return original;
            }

            T[] copy = new T[original.Length];
            Array.Copy(sourceArray: original, sourceIndex: 0, destinationArray: copy, destinationIndex: 0, length: original.Length);
            return copy;
        }
    }
}
