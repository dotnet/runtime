// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ANCILLARY_INTEROP
namespace System.Runtime.InteropServices.Marshalling
#else
namespace Microsoft.Interop
#endif
{
    public enum MarshalDirection
    {
        ManagedToUnmanaged = 0,
        UnmanagedToManaged = 1,
        Bidirectional = 2
    }
}
