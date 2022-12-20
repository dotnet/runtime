// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime.CompilerServices
{
    [System.Runtime.CompilerServices.ReflectionBlocked]
    public struct GenericMethodDescriptor
    {
        public readonly IntPtr MethodFunctionPointer;
        public readonly IntPtr InstantiationArgument;

        public GenericMethodDescriptor(IntPtr methodFunctionPointer, IntPtr instantiationArgument)
            => (MethodFunctionPointer, InstantiationArgument) = (methodFunctionPointer, instantiationArgument);
    }
}
