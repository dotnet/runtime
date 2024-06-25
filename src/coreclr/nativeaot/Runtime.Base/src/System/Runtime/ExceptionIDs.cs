// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
#if NATIVEAOT
    public
#else
    internal
#endif
    enum ExceptionIDs
    {
        OutOfMemory = 1,
        DivideByZero = 2,
        Overflow = 3,
        NullReference = 4,
        AccessViolation = 5,
        DataMisaligned = 6
    }
}
