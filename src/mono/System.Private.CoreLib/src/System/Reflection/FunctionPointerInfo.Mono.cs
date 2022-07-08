// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal partial class FunctionPointerInfo
    {
        internal FunctionPointerInfo()
        {
            _returnInfo = default!;
            _parameterInfos = default!;
        }

        private Type[]? GetCustomModifiersFromFunctionPointer(int position, bool required) => throw new NotSupportedException();
        private unsafe MdSigCallingConvention CallingConvention => throw new NotSupportedException();
        private RuntimeType FunctionPointerType => throw new NotSupportedException();
    }
}
