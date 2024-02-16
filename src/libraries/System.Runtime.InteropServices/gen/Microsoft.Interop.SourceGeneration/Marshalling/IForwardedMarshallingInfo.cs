// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    internal interface IForwardedMarshallingInfo
    {
        bool TryCreateAttributeSyntax([NotNullWhen(true)] out AttributeSyntax? attribute);
    }
}
