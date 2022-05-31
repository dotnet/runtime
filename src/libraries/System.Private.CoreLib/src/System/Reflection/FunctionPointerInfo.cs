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
                Type[] customModifiers = _returnInfo.GetOptionalCustomModifiers();
                List<Type>? list = null;
                bool foundCallingConvention = false;

                for (int i = 0; i < customModifiers.Length; i++)
                {
                    Type type = customModifiers[i];
                    if (type.FullName!.StartsWith(CallingConventionTypePrefix))
                    {
                        list ??= new List<Type>();
                        list.Add(type);

                        if (type == typeof(CallConvCdecl) ||
                            type == typeof(CallConvFastcall) ||
                            type == typeof(CallConvStdcall) ||
                            type == typeof(CallConvThiscall))
                        {
                            foundCallingConvention = true;
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
                        case MdSigCallingConvention.FastCall:
                            callConv = typeof(CallConvFastcall);
                            break;
                        case MdSigCallingConvention.StdCall:
                            callConv = typeof(CallConvStdcall);
                            break;
                        case MdSigCallingConvention.ThisCall:
                            callConv = typeof(CallConvThiscall);
                            break;
                    }

                    if (callConv != null)
                    {
                        list ??= new List<Type>();
                        list.Add(callConv);
                        _returnInfo.GetOptionalCustomModifiersList().Add(callConv);
                    }
                }

                _callingConventions = list == null ? Type.EmptyTypes : list.ToArray();
                Debug.Assert(_callingConventions != null);
            }

            return CloneArray(_callingConventions);
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
