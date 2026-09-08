// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Interop;

public static class SyntaxKindExtensions
{
    public static ContainingDeclarationKind GetDeclarationKind(this SyntaxKind syntaxKind) => syntaxKind switch
    {
        SyntaxKind.ClassDeclaration => ContainingDeclarationKind.Class,
        SyntaxKind.StructDeclaration => ContainingDeclarationKind.Struct,
        SyntaxKind.InterfaceDeclaration => ContainingDeclarationKind.Interface,
        SyntaxKind.RecordDeclaration => ContainingDeclarationKind.Record,
        SyntaxKind.RecordStructDeclaration => ContainingDeclarationKind.RecordStruct,
        _ => throw new UnreachableException(),
    };
}
