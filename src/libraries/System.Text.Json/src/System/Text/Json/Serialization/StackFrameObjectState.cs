// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    /// <summary>
    /// The current state of an object or collection that supports continuation.
    /// The values are typically compared with the less-than operator so the ordering is important.
    /// </summary>
    internal enum StackFrameObjectState : byte
    {
        None = 0,

        StartToken,
        ReadMetadata,
        ConstructorArguments,
        CreatedObject,
        ReadElements,
        EndToken,
        EndTokenValidation,
    }
}
