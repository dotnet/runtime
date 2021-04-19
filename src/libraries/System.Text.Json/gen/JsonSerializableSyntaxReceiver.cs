// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace System.Text.Json.SourceGeneration
{
    internal sealed class JsonSerializableSyntaxReceiver : ISyntaxReceiver
    {
        public List<CompilationUnitSyntax> CompilationUnits { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is CompilationUnitSyntax compilationUnit)
            {
                CompilationUnits.Add(compilationUnit);
            }
        }
    }
}
