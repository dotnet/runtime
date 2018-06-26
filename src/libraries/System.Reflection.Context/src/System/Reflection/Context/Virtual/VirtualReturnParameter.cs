// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Context.Virtual
{
    // Represents the 'return' parameter for a method
    internal class VirtualReturnParameter : VirtualParameter
    {
        public VirtualReturnParameter(MethodInfo method)
            : base(method, method.ReturnType, name:null,  position: -1)
        {
        }
    }
}
