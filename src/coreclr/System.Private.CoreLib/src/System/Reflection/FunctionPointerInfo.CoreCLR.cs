// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal partial class FunctionPointerInfo
    {
        private readonly Signature _signature;
        private readonly Type _type;

        internal FunctionPointerInfo(Type type, Signature signature)
        {
            Debug.Assert(signature.m_csig > 0);

            _type = type;
            _signature = signature;
            _returnInfo = new RuntimeFunctionPointerParameterInfo(signature.ReturnType.AsType(), -1, signature);

            RuntimeType[] arguments = signature.Arguments;
            int count = arguments.Length;
            if (count == 0)
            {
                _parameterInfos = Array.Empty<RuntimeFunctionPointerParameterInfo>();
            }
            else
            {
                RuntimeFunctionPointerParameterInfo[] parameterInfos = new RuntimeFunctionPointerParameterInfo[count];
                for (int i = 0; i < count; i++)
                {
                    parameterInfos[i] = new RuntimeFunctionPointerParameterInfo(arguments[i].AsType(), i - 1, signature);
                }
                _parameterInfos = parameterInfos;
            }
        }

        private unsafe MdSigCallingConvention CallingConvention => (MdSigCallingConvention)((byte*)_signature.m_sig)[0] & MdSigCallingConvention.CallConvMask;
    }
}
