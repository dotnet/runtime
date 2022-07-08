// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal partial class FunctionPointerInfo
    {
        private readonly RuntimeType _type;

        internal FunctionPointerInfo(RuntimeType type)
        {
            _type = type;
            Type[] arguments = RuntimeTypeHandle.GetArgumentTypesFromFunctionPointer(type);
            Debug.Assert(arguments.Length >= 1);

            _returnInfo = new RuntimeFunctionPointerParameterInfo(this, arguments[0], 0);
            int count = arguments.Length;
            if (count == 1)
            {
                _parameterInfos = Array.Empty<RuntimeFunctionPointerParameterInfo>();
            }
            else
            {
                RuntimeFunctionPointerParameterInfo[] parameterInfos = new RuntimeFunctionPointerParameterInfo[count - 1];
                for (int i = 0; i < count - 1; i++)
                {
                    parameterInfos[i] = new RuntimeFunctionPointerParameterInfo(this, arguments[i + 1], i + 1);
                }
                _parameterInfos = parameterInfos;
            }
        }

        internal RuntimeType FunctionPointerType => _type;
        internal unsafe MdSigCallingConvention CallingConvention => (MdSigCallingConvention)RuntimeTypeHandle.GetRawCallingConventionsFromFunctionPointer(_type);
        internal Type[]? GetCustomModifiersFromFunctionPointer(int position, bool required) =>
            RuntimeTypeHandle.GetCustomModifiersFromFunctionPointer(_type, 0, required: false);
    }
}
