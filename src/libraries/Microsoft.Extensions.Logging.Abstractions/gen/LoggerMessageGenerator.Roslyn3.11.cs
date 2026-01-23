// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS1035 // IIncrementalGenerator isn't available for the target configuration

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]

namespace Microsoft.Extensions.Logging.Generators
{
    [Generator]
    public partial class LoggerMessageGenerator : ISourceGenerator
    {
        // SyntaxKind.ExtensionDeclaration = 9079 (added in Roslyn for C# 14)
        private const int ExtensionDeclarationKind = 9079;

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(SyntaxContextReceiver.Create);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not SyntaxContextReceiver receiver)
            {
                return;
            }

            // Report diagnostics for methods inside extension blocks
            foreach (Location location in receiver.ExtensionBlockMethodLocations)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MethodInsideExtensionBlockNotSupported, location));
            }

            if (receiver.ClassDeclarations.Count == 0)
            {
                // nothing to do yet
                return;
            }

            var p = new Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
            IReadOnlyList<LoggerClass> logClasses = p.GetLogClasses(receiver.ClassDeclarations);
            if (logClasses.Count > 0)
            {
                var e = new Emitter(context.Compilation);
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

            public List<Location> ExtensionBlockMethodLocations { get; } = new();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (IsSyntaxTargetForGeneration(context.Node))
                {
                    (ClassDeclarationSyntax? classSyntax, Location? extensionBlockLocation) = GetSemanticTargetForGeneration(context);
                    if (classSyntax is not null)
                    {
                        ClassDeclarations.Add(classSyntax);
                    }
                    else if (extensionBlockLocation is not null)
                    {
                        ExtensionBlockMethodLocations.Add(extensionBlockLocation);
                    }
                }
            }

            private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
                node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0;

            private static (ClassDeclarationSyntax? ClassDecl, Location? ExtensionBlockLocation) GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
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
                            // Check if the parent is an extension block by kind
                            if (methodDeclarationSyntax.Parent is TypeDeclarationSyntax parent &&
                                (int)parent.Kind() == ExtensionDeclarationKind)
                            {
                                return (null, methodDeclarationSyntax.GetLocation());
                            }

                            return (methodDeclarationSyntax.Parent as ClassDeclarationSyntax, null);
                        }
                    }
                }

                return (null, null);
            }
        }
    }
}
