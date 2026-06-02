// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Interop;

public static class SyntaxKindExtensions
{
    public static string GetDeclarationKeyword(this SyntaxKind syntaxKind) => syntaxKind switch
    {
        SyntaxKind.ClassDeclaration => "class",
        SyntaxKind.StructDeclaration => "struct",
        SyntaxKind.InterfaceDeclaration => "interface",
        SyntaxKind.RecordDeclaration => "record",
        SyntaxKind.RecordStructDeclaration => "record struct",
        _ => throw new UnreachableException(),
    };
}
