// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<LoggerClassInfo?> loggerClassInfos = context.SyntaxProvider
                .ForAttributeWithMetadataName(
#if !ROSLYN4_4_OR_GREATER
                    context,
#endif
                    Parser.LoggerMessageAttribute,
                    (node, _) => node is MethodDeclarationSyntax,
                    (context, cancellationToken) =>
                    {
                        var classDeclaration = context.TargetNode.Parent as ClassDeclarationSyntax;
                        if (classDeclaration == null)
                        {
                            return null;
                        }

                        SemanticModel semanticModel = context.SemanticModel;
                        Compilation compilation = semanticModel.Compilation;

                        // Get well-known symbols
                        INamedTypeSymbol? loggerMessageAttribute = compilation.GetBestTypeByMetadataName(Parser.LoggerMessageAttribute);
                        INamedTypeSymbol? loggerSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
                        INamedTypeSymbol? logLevelSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.LogLevel");
                        INamedTypeSymbol? exceptionSymbol = compilation.GetBestTypeByMetadataName("System.Exception");
                        INamedTypeSymbol? enumerableSymbol = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                        INamedTypeSymbol? stringSymbol = compilation.GetSpecialType(SpecialType.System_String);

                        // Check if String.Create exists
                        bool hasStringCreate = stringSymbol?.GetMembers("Create").OfType<IMethodSymbol>()
                            .Any(m => m.IsStatic &&
                                      m.Parameters.Length == 2 &&
                                      m.Parameters[0].Type.Name == "IFormatProvider" &&
                                      m.Parameters[1].RefKind == RefKind.Ref) ?? false;

                        if (loggerMessageAttribute == null || loggerSymbol == null || logLevelSymbol == null ||
                            exceptionSymbol == null || enumerableSymbol == null || stringSymbol == null)
                        {
                            // Required types aren't available
                            return null;
                        }

                        return new LoggerClassInfo(
                            classDeclaration,
                            semanticModel,
                            loggerMessageAttribute,
                            loggerSymbol,
                            logLevelSymbol,
                            exceptionSymbol,
                            enumerableSymbol,
                            stringSymbol,
                            hasStringCreate);
                    })
                .Where(static info => info != null);

            IncrementalValueProvider<ImmutableArray<LoggerClassInfo?>> collectedInfos = loggerClassInfos.Collect();

            context.RegisterSourceOutput(collectedInfos, static (spc, infos) => Execute(infos, spc));
        }

        private static void Execute(ImmutableArray<LoggerClassInfo?> infos, SourceProductionContext context)
        {
            if (infos.IsDefaultOrEmpty)
            {
                // nothing to do yet
                return;
            }

            // Group by semantic model to reuse it
            var groupedBySemanticModel = infos
                .Where(i => i != null)
                .Cast<LoggerClassInfo>()
                .GroupBy(i => i.SemanticModel);

            var allLogClasses = new List<LoggerClass>();
            bool hasStringCreate = false;

            foreach (var group in groupedBySemanticModel)
            {
                var distinctClasses = group.Select(i => i.ClassDeclaration).Distinct().ToImmutableHashSet();

                // All infos in the group have the same symbols, so we can use the first one
                var firstInfo = group.First();
                hasStringCreate = firstInfo.HasStringCreate;

                var p = new Parser(
                    firstInfo.LoggerMessageAttribute,
                    firstInfo.LoggerSymbol,
                    firstInfo.LogLevelSymbol,
                    firstInfo.ExceptionSymbol,
                    firstInfo.EnumerableSymbol,
                    firstInfo.StringSymbol,
                    context.ReportDiagnostic,
                    context.CancellationToken);

                IReadOnlyList<LoggerClass> logClasses = p.GetLogClasses(distinctClasses, firstInfo.SemanticModel);
                allLogClasses.AddRange(logClasses);
            }

            if (allLogClasses.Count > 0)
            {
                var e = new Emitter(hasStringCreate);
                string result = e.Emit(allLogClasses, context.CancellationToken);

                context.AddSource("LoggerMessage.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }

        private sealed class LoggerClassInfo
        {
            public LoggerClassInfo(
                ClassDeclarationSyntax classDeclaration,
                SemanticModel semanticModel,
                INamedTypeSymbol loggerMessageAttribute,
                INamedTypeSymbol loggerSymbol,
                INamedTypeSymbol logLevelSymbol,
                INamedTypeSymbol exceptionSymbol,
                INamedTypeSymbol enumerableSymbol,
                INamedTypeSymbol stringSymbol,
                bool hasStringCreate)
            {
                ClassDeclaration = classDeclaration;
                SemanticModel = semanticModel;
                LoggerMessageAttribute = loggerMessageAttribute;
                LoggerSymbol = loggerSymbol;
                LogLevelSymbol = logLevelSymbol;
                ExceptionSymbol = exceptionSymbol;
                EnumerableSymbol = enumerableSymbol;
                StringSymbol = stringSymbol;
                HasStringCreate = hasStringCreate;
            }

            public ClassDeclarationSyntax ClassDeclaration { get; }
            public SemanticModel SemanticModel { get; }
            public INamedTypeSymbol LoggerMessageAttribute { get; }
            public INamedTypeSymbol LoggerSymbol { get; }
            public INamedTypeSymbol LogLevelSymbol { get; }
            public INamedTypeSymbol ExceptionSymbol { get; }
            public INamedTypeSymbol EnumerableSymbol { get; }
            public INamedTypeSymbol StringSymbol { get; }
            public bool HasStringCreate { get; }
        }
    }
}
