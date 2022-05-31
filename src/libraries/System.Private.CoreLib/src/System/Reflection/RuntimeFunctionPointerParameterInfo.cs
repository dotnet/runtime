// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed partial class RuntimeFunctionPointerParameterInfo : FunctionPointerParameterInfo
    {
        private readonly Type _parameterType;

        public override Type ParameterType => _parameterType;
        public override string ToString() => _parameterType.FullName ?? string.Empty;
    }
}
