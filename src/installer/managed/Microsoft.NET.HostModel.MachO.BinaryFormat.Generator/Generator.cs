// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BinaryFormat
{
    [Generator]
    public partial class Generator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((pi) => pi.AddSource("BinaryFormat.Attribute.cs", AttributeSource.TrimStart()));
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            MySyntaxReceiver syntaxReceiver = (MySyntaxReceiver)context.SyntaxContextReceiver;

            foreach (var userType in syntaxReceiver.TypesToAugment)
            {
                GenerateReaderWriter(context, userType, context.Compilation.GetSemanticModel(userType.SyntaxTree));
            }
        }

        private sealed class MySyntaxReceiver : ISyntaxContextReceiver
        {
            public List<TypeDeclarationSyntax> TypesToAugment { get; private set; } = new();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is TypeDeclarationSyntax tds &&
                    context.SemanticModel.GetDeclaredSymbol(tds) is INamedTypeSymbol symbol &&
                    symbol.GetAttributes().Any(a => a.AttributeClass.Name == "GenerateReaderWriterAttribute"))
                {
                    TypesToAugment.Add(tds);
                }
            }
        }
    }
}
