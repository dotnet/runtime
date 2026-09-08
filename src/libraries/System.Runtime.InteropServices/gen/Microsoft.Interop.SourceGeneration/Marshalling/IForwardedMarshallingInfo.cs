// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Interop
{
    internal interface IForwardedMarshallingInfo
    {
        bool TryCreateAttribute([NotNullWhen(true)] out string? attribute);
    }
}
