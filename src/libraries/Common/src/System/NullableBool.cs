// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>
    /// Used instead of bool? for thread-safe state because <see cref="Nullable{Boolean}"/> is not guaranteed to be read or written atomically.
    /// </summary>
    internal enum NullableBool : sbyte
    {
        Undefined = 0,
        False = -1,
        True = 1,
    }
}
