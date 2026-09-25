// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if CORECLR

namespace System.Runtime.InteropServices
{
    internal enum ErrorLocation
    {
        ReturnValue = 0,
        LastParameter = 1,
        HiddenReturnValue = 2,
        HiddenLastParameter = 3,
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ErrorHandlerAttribute : Attribute
    {
        public ErrorHandlerAttribute(Type marshallerType, ErrorLocation errorLocation)
        {
            _ = marshallerType;
            _ = errorLocation;
        }
    }
}

#endif
