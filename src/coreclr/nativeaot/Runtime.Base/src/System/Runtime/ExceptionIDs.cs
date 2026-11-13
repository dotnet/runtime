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
        ArrayTypeMismatch = 3,
        DivideByZero = 4,
        IndexOutOfRange = 5,
        InvalidCast = 6,
        Overflow = 7,
        NullReference = 8,
#if !NATIVEAOT || !TARGET_WASM
        AccessViolation = 9,
        DataMisaligned = 10,
#endif
        EntrypointNotFound = 11,
        AmbiguousImplementation = 12,
#if !NATIVEAOT || !TARGET_WASM
        IllegalInstruction = 13,
        PrivilegedInstruction = 14,
        InPageError = 15,
#endif
    }
}
