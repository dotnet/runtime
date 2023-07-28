// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ANCILLARY_INTEROP
namespace System.Runtime.InteropServices.Marshalling
#else
namespace Microsoft.Interop
#endif
{
    public enum ExceptionMarshalling
    {
        // Use a custom marshaller to implement marshalling an exception to a return value
        Custom = 0,
        // Provide some COM-style defaults
        Com = 1
    }
}
