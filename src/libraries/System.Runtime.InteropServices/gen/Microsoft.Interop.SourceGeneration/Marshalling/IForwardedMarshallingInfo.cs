// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Interop
{
    internal interface IForwardedMarshallingInfo
    {
        /// <summary>Creates attribute text without enclosing brackets; the caller supplies <c>[</c> and <c>]</c>.</summary>
        bool TryCreateAttribute([NotNullWhen(true)] out string? attribute);
    }
}
