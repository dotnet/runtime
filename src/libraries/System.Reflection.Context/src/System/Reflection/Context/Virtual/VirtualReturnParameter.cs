// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Context.Virtual
{
    // Represents the 'return' parameter for a method
    internal sealed class VirtualReturnParameter : VirtualParameter
    {
        public VirtualReturnParameter(MethodInfo method)
            : base(method, method.ReturnType, name: null, position: -1)
        {
        }
    }
}
