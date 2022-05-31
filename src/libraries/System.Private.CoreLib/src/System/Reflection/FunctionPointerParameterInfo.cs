// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    public abstract class FunctionPointerParameterInfo
    {
        public abstract Type ParameterType { get; }
        public abstract Type[] GetOptionalCustomModifiers();
        public abstract Type[] GetRequiredCustomModifiers();
        public override string? ToString() => ParameterType.ToString();
    }
}
