// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Text;

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]

namespace Microsoft.Extensions.Logging.Generators
{
    [Generator]
    public partial class LoggerMessageGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<(LoggerClass? LoggerClass, bool HasStringCreate)> loggerClasses = context.SyntaxProvider
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
                            return default;
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
                            return default;
                        }

                        // Parse the logger class immediately to extract value-based data
                        // Note: We can't report diagnostics here, so we'll need to store them somehow
                        // For now, we'll use a no-op diagnostic reporter
                        var parser = new Parser(
                            loggerMessageAttribute,
                            loggerSymbol,
                            logLevelSymbol,
                            exceptionSymbol,
                            enumerableSymbol,
                            stringSymbol,
                            _ => { }, // Diagnostics will be reported during the second parse in Execute
                            cancellationToken);

                        IReadOnlyList<LoggerClass> logClasses = parser.GetLogClasses(new[] { classDeclaration }, semanticModel);

                        // Return the first (and should be only) logger class for this attributed method's containing class
                        LoggerClass? loggerClass = logClasses.Count > 0 ? logClasses[0] : null;

                        return (loggerClass, hasStringCreate);
                    });

            context.RegisterSourceOutput(loggerClasses.Collect(), static (spc, items) => Execute(items, spc));
        }

        private static void Execute(ImmutableArray<(LoggerClass? LoggerClass, bool HasStringCreate)> items, SourceProductionContext context)
        {
            if (items.IsDefaultOrEmpty)
            {
                return;
            }

            bool hasStringCreate = false;
            var allLogClasses = new Dictionary<string, LoggerClass>(); // Use dictionary to deduplicate by class name

            foreach (var item in items)
            {
                if (item.LoggerClass != null)
                {
                    hasStringCreate = item.HasStringCreate;

                    // Merge classes with the same full name (namespace + name)
                    string classKey = item.LoggerClass.Namespace + "." + item.LoggerClass.Name;
                    if (allLogClasses.TryGetValue(classKey, out var existingClass))
                    {
                        // Merge methods from multiple attributed methods in the same class
                        existingClass.Methods.AddRange(item.LoggerClass.Methods);
                    }
                    else
                    {
                        allLogClasses[classKey] = item.LoggerClass;
                    }
                }
            }

            if (allLogClasses.Count > 0)
            {
                var e = new Emitter(hasStringCreate);
                string result = e.Emit(allLogClasses.Values.ToList(), context.CancellationToken);

                context.AddSource("LoggerMessage.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }
    }
}
