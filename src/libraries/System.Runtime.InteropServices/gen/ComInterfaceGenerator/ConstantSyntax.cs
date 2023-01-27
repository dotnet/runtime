// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    // Fully qualified type name syntaxes for commonly used types
    internal static class ConstantSyntax
    {
        public struct TypeSyntax
        {
            public static QualifiedNameSyntax QualifiedName { get; }
        }
    }
}
