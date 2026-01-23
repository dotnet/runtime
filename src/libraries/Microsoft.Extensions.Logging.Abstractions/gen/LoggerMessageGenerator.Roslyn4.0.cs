// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
#if !ROSLYN4_4_OR_GREATER
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
#endif
using Microsoft.CodeAnalysis.Text;

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]

namespace Microsoft.Extensions.Logging.Generators
{
    [Generator]
    public partial class LoggerMessageGenerator : IIncrementalGenerator
    {
        // SyntaxKind.ExtensionDeclaration = 9079 (added in Roslyn for C# 14)
        private const int ExtensionDeclarationKind = 9079;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Filter to MethodDeclarationSyntax and get the parent TypeDeclarationSyntax
            IncrementalValuesProvider<(TypeDeclarationSyntax? TypeDecl, Location MethodLocation)> typeDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
#if !ROSLYN4_4_OR_GREATER
                    context,
#endif
                    Parser.LoggerMessageAttribute,
                    (node, _) => node is MethodDeclarationSyntax,
                    (context, _) => (context.TargetNode.Parent as TypeDeclarationSyntax, context.TargetNode.GetLocation()))
                .Where(static m => m.Item1 is not null);

            // Separate extension blocks (to emit diagnostics) from class declarations (to process normally)
            // Check for extension blocks by raw kind since ExtensionDeclarationSyntax may not be available
            IncrementalValuesProvider<Location> extensionBlocks = typeDeclarations
                .Where(static m => (int)m.TypeDecl!.Kind() == ExtensionDeclarationKind)
                .Select(static (m, _) => m.MethodLocation);

            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = typeDeclarations
                .Where(static m => m.TypeDecl is ClassDeclarationSyntax)
                .Select(static (m, _) => (ClassDeclarationSyntax)m.TypeDecl!);

            // Report diagnostics for extension blocks
            context.RegisterSourceOutput(extensionBlocks, static (spc, location) =>
            {
                spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MethodInsideExtensionBlockNotSupported, location));
            });

            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
                context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                // nothing to do yet
                return;
            }

            ImmutableHashSet<ClassDeclarationSyntax> distinctClasses = classes.ToImmutableHashSet();

            var p = new Parser(compilation, context.ReportDiagnostic, context.CancellationToken);
            IReadOnlyList<LoggerClass> logClasses = p.GetLogClasses(distinctClasses);
            if (logClasses.Count > 0)
            {
                var e = new Emitter(compilation);
                string result = e.Emit(logClasses, context.CancellationToken);

                context.AddSource("LoggerMessage.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }
    }
}
