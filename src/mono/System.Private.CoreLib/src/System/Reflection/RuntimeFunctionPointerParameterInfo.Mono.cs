// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection
{
    internal partial class RuntimeFunctionPointerParameterInfo
    {
        public RuntimeFunctionPointerParameterInfo()
        {
            _parameterType = default!;
        }

        public override Type[] GetOptionalCustomModifiers() => throw new NotSupportedException();
        public override Type[] GetRequiredCustomModifiers() => throw new NotSupportedException();
        internal void SetCustomModifiersForReturnType(Type[] modifiers) => throw new NotSupportedException();
    }
}
