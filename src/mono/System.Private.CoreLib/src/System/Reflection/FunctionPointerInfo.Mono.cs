// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection
{
    internal partial class FunctionPointerInfo
    {
        internal FunctionPointerInfo()
        {
            _returnInfo = default!;
            _parameterInfos = default!;
        }

        internal List<Type> GetOptionalCustomModifiersList() => throw new NotSupportedException();
        private unsafe MdSigCallingConvention CallingConvention => throw new NotSupportedException();
    }
}
