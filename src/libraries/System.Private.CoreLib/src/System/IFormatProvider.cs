// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>
    /// Provides a mechanism for retrieving an object to control formatting.
    /// </summary>
    public interface IFormatProvider
    {
        // Interface does not need to be marked with the serializable attribute
        object? GetFormat(Type? formatType);
    }
}
