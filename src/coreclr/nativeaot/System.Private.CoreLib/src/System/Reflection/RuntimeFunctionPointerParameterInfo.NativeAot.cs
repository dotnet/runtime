// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal partial class RuntimeFunctionPointerParameterInfo
    {
        public override Type[] GetOptionalCustomModifiers() => throw new NotSupportedException();
        public override Type[] GetRequiredCustomModifiers() => throw new NotSupportedException();
    }
}
