// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS1035 // IIncrementalGenerator isn't available for the target configuration

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]

namespace Microsoft.Extensions.Logging.Generators
{
    [Generator]
    public partial class LoggerMessageGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(SyntaxContextReceiver.Create);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not SyntaxContextReceiver receiver || receiver.ClassDeclarations.Count == 0)
            {
                // nothing to do yet
                return;
            }

            var p = new Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
            IReadOnlyList<LoggerClass> logClasses = p.GetLogClasses(receiver.ClassDeclarations);
            if (logClasses.Count > 0)
            {
                var e = new Emitter();
                string result = e.Emit(logClasses, context.CancellationToken);

                context.AddSource("LoggerMessage.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }

        private sealed class SyntaxContextReceiver : ISyntaxContextReceiver
        {
            internal static SyntaxContextReceiver Create()
            {
                return new SyntaxContextReceiver();
            }

            public HashSet<ClassDeclarationSyntax> ClassDeclarations { get; } = new();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (IsSyntaxTargetForGeneration(context.Node))
                {
                    ClassDeclarationSyntax classSyntax = GetSemanticTargetForGeneration(context);
                    if (classSyntax != null)
                    {
                        ClassDeclarations.Add(classSyntax);
                    }
                }
            }

            private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
                node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0;

            private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
            {
                var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

                foreach (AttributeListSyntax attributeListSyntax in methodDeclarationSyntax.AttributeLists)
                {
                    foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                    {
                        IMethodSymbol attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
                        if (attributeSymbol == null)
                        {
                            continue;
                        }

                        INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                        string fullName = attributeContainingTypeSymbol.ToDisplayString();

                        if (fullName == Parser.LoggerMessageAttribute)
                        {
                            return methodDeclarationSyntax.Parent as ClassDeclarationSyntax;
                        }
                    }
                }

                return null;
            }
        }
    }
}
