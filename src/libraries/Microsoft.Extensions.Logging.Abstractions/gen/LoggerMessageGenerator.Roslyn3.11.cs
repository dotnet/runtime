// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
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

            Compilation compilation = context.Compilation;

            // Get well-known symbols
            INamedTypeSymbol? loggerMessageAttribute = compilation.GetBestTypeByMetadataName(Parser.LoggerMessageAttribute);
            INamedTypeSymbol? loggerSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
            INamedTypeSymbol? logLevelSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.LogLevel");
            INamedTypeSymbol? exceptionSymbol = compilation.GetBestTypeByMetadataName("System.Exception");
            INamedTypeSymbol? enumerableSymbol = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
            INamedTypeSymbol? stringSymbol = compilation.GetSpecialType(SpecialType.System_String);

            if (loggerMessageAttribute == null || loggerSymbol == null || logLevelSymbol == null)
            {
                // Required types aren't available
                return;
            }

            if (exceptionSymbol == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MissingRequiredType, null, "System.Exception"));
                return;
            }

            if (enumerableSymbol == null || stringSymbol == null)
            {
                // Required types aren't available
                return;
            }

            // Check if String.Create exists
            bool hasStringCreate = stringSymbol.GetMembers("Create").OfType<IMethodSymbol>()
                .Any(m => m.IsStatic &&
                          m.Parameters.Length == 2 &&
                          m.Parameters[0].Type.Name == "IFormatProvider" &&
                          m.Parameters[1].RefKind == RefKind.Ref);

            // Get a semantic model to pass to Parser (used to access compilation)
            var firstClass = receiver.ClassDeclarations.FirstOrDefault();
            if (firstClass == null)
            {
                return;
            }
            SemanticModel semanticModel = compilation.GetSemanticModel(firstClass.SyntaxTree);

            var p = new Parser(
                loggerMessageAttribute,
                loggerSymbol,
                logLevelSymbol,
                exceptionSymbol,
                enumerableSymbol,
                stringSymbol,
                context.ReportDiagnostic,
                context.CancellationToken);

            IReadOnlyList<LoggerClass> logClasses = p.GetLogClasses(receiver.ClassDeclarations, semanticModel);
            if (logClasses.Count > 0)
            {
                var e = new Emitter(hasStringCreate);
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
