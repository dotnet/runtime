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
        public static class StepNames
        {
            public const string LoggerMessageTransform = nameof(LoggerMessageTransform);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<(LoggerClassSpec? LoggerClassSpec, ImmutableArray<Diagnostic> Diagnostics, bool HasStringCreate)> loggerClasses = context.SyntaxProvider
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

                        if (loggerMessageAttribute == null || loggerSymbol == null || logLevelSymbol == null)
                        {
                            // Required types aren't available
                            return default;
                        }

                        if (exceptionSymbol == null)
                        {
                            var diagnostics = ImmutableArray.Create(Diagnostic.Create(DiagnosticDescriptors.MissingRequiredType, null, new object?[] { "System.Exception" }));
                            return (null, diagnostics, false);
                        }

                        if (enumerableSymbol == null || stringSymbol == null)
                        {
                            // Required types aren't available
                            return default;
                        }

                        // Parse the logger class immediately to extract value-based data
                        var parser = new Parser(
                            loggerMessageAttribute,
                            loggerSymbol,
                            logLevelSymbol,
                            exceptionSymbol,
                            enumerableSymbol,
                            stringSymbol,
                            null, // Don't report diagnostics in transform; they're collected and reported in Execute
                            cancellationToken);

                        IReadOnlyList<LoggerClass> logClasses = parser.GetLogClasses(new[] { classDeclaration }, semanticModel);

                        // Convert to immutable spec for incremental caching
                        LoggerClassSpec? loggerClassSpec = logClasses.Count > 0 ? logClasses[0].ToSpec() : null;

                        return (loggerClassSpec, parser.Diagnostics.ToImmutableArray(), hasStringCreate);
                    })
#if ROSLYN4_4_OR_GREATER
                .WithTrackingName(StepNames.LoggerMessageTransform)
#endif
                ;

            // Pipeline 1: Source generation only.
            // Uses Select to extract just the model; the Select operator deduplicates by
            // comparing model equality, so source generation only re-fires on structural changes.
            context.RegisterSourceOutput(
                loggerClasses.Select(static (t, _) => (t.LoggerClassSpec, t.HasStringCreate)).Collect(),
                static (spc, items) => EmitSource(items, spc));

            // Pipeline 2: Diagnostics only.
            // Diagnostics use raw SourceLocation instances that are pragma-suppressible.
            // This pipeline re-fires whenever diagnostics change (e.g. positional shifts)
            // without triggering expensive source regeneration.
            // See https://github.com/dotnet/runtime/issues/92509 for context.
            context.RegisterSourceOutput(loggerClasses.Collect(), EmitDiagnostics);
        }

        private static void EmitDiagnostics(SourceProductionContext context, ImmutableArray<(LoggerClassSpec? LoggerClassSpec, ImmutableArray<Diagnostic> Diagnostics, bool HasStringCreate)> items)
        {
            // Use HashSet to deduplicate — each attributed method triggers parsing of entire class,
            // producing duplicate diagnostics.
            var reportedDiagnostics = new HashSet<(string Id, TextSpan? Span, string? FilePath)>();
            foreach (var item in items)
            {
                foreach (Diagnostic diagnostic in item.Diagnostics)
                {
                    if (reportedDiagnostics.Add((diagnostic.Id, diagnostic.Location?.SourceSpan, diagnostic.Location?.SourceTree?.FilePath)))
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static void EmitSource(ImmutableArray<(LoggerClassSpec? LoggerClassSpec, bool HasStringCreate)> items, SourceProductionContext context)
        {
            if (items.IsDefaultOrEmpty)
            {
                return;
            }

            bool hasStringCreate = false;
            var allLogClasses = new Dictionary<string, LoggerClass>(); // Deduplicate by class key

            foreach (var item in items)
            {
                if (item.LoggerClassSpec != null)
                {
                    hasStringCreate |= item.HasStringCreate;

                    // Build unique key including parent class chain to handle nested classes
                    string classKey = BuildClassKey(item.LoggerClassSpec);

                    // Each attributed method in a partial class file produces the same LoggerClassSpec with all methods in that file.
                    // However, different partial class files produce different LoggerClassSpecs with different methods. Merge them.
                    if (!allLogClasses.TryGetValue(classKey, out LoggerClass? existingClass))
                    {
                        allLogClasses[classKey] = FromSpec(item.LoggerClassSpec);
                    }
                    else
                    {
                        var newClass = FromSpec(item.LoggerClassSpec);

                        var existingMethodKeys = new HashSet<(string Name, int EventId)>();
                        foreach (var method in existingClass.Methods)
                        {
                            existingMethodKeys.Add((method.Name, method.EventId));
                        }

                        foreach (var method in newClass.Methods)
                        {
                            if (existingMethodKeys.Add((method.Name, method.EventId)))
                            {
                                existingClass.Methods.Add(method);
                            }
                        }
                    }
                }
            }

            if (allLogClasses.Count > 0)
            {
                var e = new Emitter(hasStringCreate);
                var orderedLoggerClasses = allLogClasses
                    .OrderBy(static kvp => kvp.Key, System.StringComparer.Ordinal)
                    .Select(static kvp => kvp.Value)
                    .ToList();
                string result = e.Emit(orderedLoggerClasses, context.CancellationToken);

                context.AddSource("LoggerMessage.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }

        private static string BuildClassKey(LoggerClassSpec classSpec)
        {
            // Build key with full namespace and parent class chain to handle nested classes
            var parts = new List<string>();
            var current = classSpec;
            while (current != null)
            {
                parts.Add(current.Name);
                current = current.ParentClass;
            }
            parts.Reverse();
            return classSpec.Namespace + "." + string.Join(".", parts);
        }

        private static LoggerClass FromSpec(LoggerClassSpec spec)
        {
            var lc = new LoggerClass
            {
                Keyword = spec.Keyword,
                Namespace = spec.Namespace,
                Name = spec.Name,
                ParentClass = spec.ParentClass != null ? FromSpec(spec.ParentClass) : null
            };

            foreach (var methodSpec in spec.Methods)
            {
                var lm = new LoggerMethod
                {
                    Name = methodSpec.Name,
                    UniqueName = methodSpec.UniqueName,
                    Message = methodSpec.Message,
                    Level = methodSpec.Level,
                    EventId = methodSpec.EventId,
                    EventName = methodSpec.EventName,
                    IsExtensionMethod = methodSpec.IsExtensionMethod,
                    Modifiers = methodSpec.Modifiers,
                    LoggerField = methodSpec.LoggerField,
                    SkipEnabledCheck = methodSpec.SkipEnabledCheck
                };

                foreach (var paramSpec in methodSpec.AllParameters)
                {
                    lm.AllParameters.Add(new LoggerParameter
                    {
                        Name = paramSpec.Name,
                        Type = paramSpec.Type,
                        CodeName = paramSpec.CodeName,
                        Qualifier = paramSpec.Qualifier,
                        IsLogger = paramSpec.IsLogger,
                        IsException = paramSpec.IsException,
                        IsLogLevel = paramSpec.IsLogLevel,
                        IsEnumerable = paramSpec.IsEnumerable,
                    });
                }

                foreach (var paramSpec in methodSpec.TemplateParameters)
                {
                    lm.TemplateParameters.Add(new LoggerParameter
                    {
                        Name = paramSpec.Name,
                        Type = paramSpec.Type,
                        CodeName = paramSpec.CodeName,
                        Qualifier = paramSpec.Qualifier,
                        IsLogger = paramSpec.IsLogger,
                        IsException = paramSpec.IsException,
                        IsLogLevel = paramSpec.IsLogLevel,
                        IsEnumerable = paramSpec.IsEnumerable,
                    });
                }

                foreach (var kvp in methodSpec.TemplateMap)
                {
                    lm.TemplateMap[kvp.Key] = kvp.Value;
                }

                foreach (var template in methodSpec.TemplateList)
                {
                    lm.TemplateList.Add(template);
                }

                foreach (var typeParamSpec in methodSpec.TypeParameters)
                {
                    lm.TypeParameters.Add(new LoggerMethodTypeParameter
                    {
                        Name = typeParamSpec.Name,
                        Constraints = typeParamSpec.Constraints
                    });
                }

                lc.Methods.Add(lm);
            }

            return lc;
        }
    }
}
