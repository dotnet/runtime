// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define LAUNCH_DEBUGGER
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize binding with ConfigurationBinder.
    /// </summary>
    [Generator]
    public sealed partial class ConfigurationBindingSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<CompilationData?> compilationData =
                context.CompilationProvider
                    .Select((compilation, _) => compilation.Options is CSharpCompilationOptions options
                        ? new CompilationData((CSharpCompilation)compilation)
                        : null);

            IncrementalValuesProvider<BinderInvocationOperation> inputCalls = context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) => node is InvocationExpressionSyntax invocation,
                (context, cancellationToken) => new BinderInvocationOperation(context, cancellationToken));

            IncrementalValueProvider<(CompilationData?, ImmutableArray<BinderInvocationOperation>)> inputData = compilationData.Combine(inputCalls.Collect());

            context.RegisterSourceOutput(inputData, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        /// <summary>
        /// Generates source code to optimize binding with ConfigurationBinder.
        /// </summary>
        private static void Execute(CompilationData compilationData, ImmutableArray<BinderInvocationOperation> inputCalls, SourceProductionContext context)
        {
#if LAUNCH_DEBUGGER
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
#endif
            if (inputCalls.IsDefaultOrEmpty)
            {
                return;
            }

            if (compilationData?.LanguageVersionIsSupported != true)
            {
                context.ReportDiagnostic(Diagnostic.Create(LanguageVersionNotSupported, location: null));
                return;
            }

            Parser parser = new(context, compilationData.TypeSymbols!);
            SourceGenerationSpec? spec = parser.GetSourceGenerationSpec(inputCalls);
            if (spec is not null)
            {
                Emitter emitter = new(context, spec);
                emitter.Emit();
            }
        }

        private sealed record CompilationData
        {
            public bool LanguageVersionIsSupported { get; }
            public KnownTypeSymbols? TypeSymbols { get; }

            public CompilationData(CSharpCompilation compilation)
            {
                LanguageVersionIsSupported = compilation.LanguageVersion >= LanguageVersion.CSharp11;
                if (LanguageVersionIsSupported)
                {
                    TypeSymbols = new KnownTypeSymbols(compilation);
                }
            }
        }

        private sealed record KnownTypeSymbols
        {
            public INamedTypeSymbol GenericIList { get; }
            public INamedTypeSymbol ICollection { get; }
            public INamedTypeSymbol IEnumerable { get; }
            public INamedTypeSymbol String { get; }

            public INamedTypeSymbol? CultureInfo { get; }
            public INamedTypeSymbol? DateOnly { get; }
            public INamedTypeSymbol? DateTimeOffset { get; }
            public INamedTypeSymbol? Guid { get; }
            public INamedTypeSymbol? Half { get; }
            public INamedTypeSymbol? Int128 { get; }
            public INamedTypeSymbol? TimeOnly { get; }
            public INamedTypeSymbol? TimeSpan { get; }
            public INamedTypeSymbol? UInt128 { get; }
            public INamedTypeSymbol? Uri { get; }
            public INamedTypeSymbol? Version { get; }

            public INamedTypeSymbol? ConfigurationKeyNameAttribute { get; }
            public INamedTypeSymbol? Dictionary { get; }
            public INamedTypeSymbol? GenericIDictionary { get; }
            public INamedTypeSymbol? HashSet { get; }
            public INamedTypeSymbol? IConfiguration { get; }
            public INamedTypeSymbol? IConfigurationSection { get; }
            public INamedTypeSymbol? IDictionary { get; }
            public INamedTypeSymbol? IServiceCollection { get; }
            public INamedTypeSymbol? ISet { get; }
            public INamedTypeSymbol? List { get; }

            public KnownTypeSymbols(CSharpCompilation compilation)
            {
                // Primitives (needed because they are Microsoft.CodeAnalysis.SpecialType.None)
                CultureInfo = compilation.GetBestTypeByMetadataName(TypeFullName.CultureInfo);
                DateOnly = compilation.GetBestTypeByMetadataName(TypeFullName.DateOnly);
                DateTimeOffset = compilation.GetBestTypeByMetadataName(TypeFullName.DateTimeOffset);
                Guid = compilation.GetBestTypeByMetadataName(TypeFullName.Guid);
                Half = compilation.GetBestTypeByMetadataName(TypeFullName.Half);
                Int128 = compilation.GetBestTypeByMetadataName(TypeFullName.Int128);
                TimeOnly = compilation.GetBestTypeByMetadataName(TypeFullName.TimeOnly);
                TimeSpan = compilation.GetBestTypeByMetadataName(TypeFullName.TimeSpan);
                UInt128 = compilation.GetBestTypeByMetadataName(TypeFullName.UInt128);
                Uri = compilation.GetBestTypeByMetadataName(TypeFullName.Uri);
                Version = compilation.GetBestTypeByMetadataName(TypeFullName.Version);

                // Used to verify input configuation binding API calls.
                ConfigurationKeyNameAttribute = compilation.GetBestTypeByMetadataName(TypeFullName.ConfigurationKeyNameAttribute);
                IConfiguration = compilation.GetBestTypeByMetadataName(TypeFullName.IConfiguration);
                IConfigurationSection = compilation.GetBestTypeByMetadataName(TypeFullName.IConfigurationSection);
                IServiceCollection = compilation.GetBestTypeByMetadataName(TypeFullName.IServiceCollection);

                // Collections.
                IEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                IDictionary = compilation.GetBestTypeByMetadataName(TypeFullName.IDictionary);

                // Used for type equivalency checks for unbounded generics.
                ICollection = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).ConstructUnboundGenericType();
                GenericIDictionary = compilation.GetBestTypeByMetadataName(TypeFullName.GenericIDictionary)?.ConstructUnboundGenericType();
                GenericIList = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).ConstructUnboundGenericType();
                ISet = compilation.GetBestTypeByMetadataName(TypeFullName.ISet)?.ConstructUnboundGenericType();

                // Used to construct concrete types at runtime; cannot also be constructed.
                Dictionary = compilation.GetBestTypeByMetadataName(TypeFullName.Dictionary);
                HashSet = compilation.GetBestTypeByMetadataName(TypeFullName.HashSet);
                List = compilation.GetBestTypeByMetadataName(TypeFullName.List);
            }
        }

        private enum BinderMethodKind
        {
            None = 0,
            Configure = 1,
            Get = 2,
            Bind = 3,
        }

        private readonly record struct BinderInvocationOperation()
        {
            public IInvocationOperation? InvocationOperation { get; }
            public BinderMethodKind BinderMethodKind { get; }
            public Location? Location { get; }

            public BinderInvocationOperation(GeneratorSyntaxContext context, CancellationToken cancellationToken) : this()
            {
                if (context.Node is not InvocationExpressionSyntax syntax ||
                    context.SemanticModel.GetOperation(syntax, cancellationToken) is not IInvocationOperation operation)
                {
                    return;
                }

                InvocationOperation = operation;
                Location = syntax.GetLocation();

                if (IsGetCall(syntax))
                {
                    BinderMethodKind = BinderMethodKind.Get;
                }
                else if (IsConfigureCall(syntax))
                {
                    BinderMethodKind = BinderMethodKind.Configure;
                }
                else if (IsBindCall(syntax))
                {
                    BinderMethodKind = BinderMethodKind.Bind;
                }
            }

            private static bool IsBindCall(InvocationExpressionSyntax invocation) =>
                invocation is
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name: IdentifierNameSyntax
                        {
                            Identifier.ValueText: "Bind"
                        }
                    },
                    ArgumentList.Arguments.Count: 1
                };

            private static bool IsConfigureCall(InvocationExpressionSyntax invocation) =>
                invocation is
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name: GenericNameSyntax
                        {
                            Identifier.ValueText: "Configure"
                        }
                    },
                    ArgumentList.Arguments.Count: 1
                };

            private static bool IsGetCall(InvocationExpressionSyntax invocation) =>
                invocation is
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name: GenericNameSyntax
                        {
                            Identifier.ValueText: "Get"
                        }
                    },
                    ArgumentList.Arguments.Count: 0
                };
        }
    }
}
