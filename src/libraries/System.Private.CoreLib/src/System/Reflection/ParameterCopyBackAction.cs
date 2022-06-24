// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    /// <summary>
    /// Determines how an invoke parameter needs to be copied back to the caller's object[] parameters.
    /// </summary>
    internal enum ParameterCopyBackAction : byte
    {
        None = 0,
        Copy = 1,
        CopyNullable = 2
    }
}
