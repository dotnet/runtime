// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define LAUNCH_DEBUGGER
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
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
            IncrementalValueProvider<KnownTypeData> compilationData =
                context.CompilationProvider
                    .Select((compilation, _) => new KnownTypeData(compilation));

            IncrementalValuesProvider<BinderInvocationOperation> inputCalls = context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) => node is InvocationExpressionSyntax invocation,
                (context, cancellationToken) => new BinderInvocationOperation(context, cancellationToken));

            IncrementalValueProvider<(KnownTypeData, ImmutableArray<BinderInvocationOperation>)> inputData = compilationData.Combine(inputCalls.Collect());

            context.RegisterSourceOutput(inputData, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        /// <summary>
        /// Generates source code to optimize binding with ConfigurationBinder.
        /// </summary>
        private static void Execute(KnownTypeData typeData, ImmutableArray<BinderInvocationOperation> inputCalls, SourceProductionContext context)
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

            Parser parser = new(context, typeData);
            SourceGenerationSpec? spec = parser.GetSourceGenerationSpec(inputCalls);
            if (spec is not null)
            {
                Emitter emitter = new(context, spec);
                emitter.Emit();
            }
        }

        private sealed record KnownTypeData
        {
            public INamedTypeSymbol SymbolForGenericIList { get; }
            public INamedTypeSymbol SymbolForICollection { get; }
            public INamedTypeSymbol SymbolForIEnumerable { get; }
            public INamedTypeSymbol SymbolForString { get; }

            public INamedTypeSymbol? SymbolForConfigurationKeyNameAttribute { get; }
            public INamedTypeSymbol? SymbolForDictionary { get; }
            public INamedTypeSymbol? SymbolForGenericIDictionary { get; }
            public INamedTypeSymbol? SymbolForHashSet { get; }
            public INamedTypeSymbol? SymbolForIConfiguration { get; }
            public INamedTypeSymbol? SymbolForIConfigurationSection { get; }
            public INamedTypeSymbol? SymbolForIDictionary { get; }
            public INamedTypeSymbol? SymbolForIServiceCollection { get; }
            public INamedTypeSymbol? SymbolForISet { get; }
            public INamedTypeSymbol? SymbolForList { get; }

            public KnownTypeData(Compilation compilation)
            {
                SymbolForIEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                SymbolForConfigurationKeyNameAttribute = compilation.GetBestTypeByMetadataName(TypeFullName.ConfigurationKeyNameAttribute);
                SymbolForIConfiguration = compilation.GetBestTypeByMetadataName(TypeFullName.IConfiguration);
                SymbolForIConfigurationSection = compilation.GetBestTypeByMetadataName(TypeFullName.IConfigurationSection);
                SymbolForIServiceCollection = compilation.GetBestTypeByMetadataName(TypeFullName.IServiceCollection);
                SymbolForString = compilation.GetSpecialType(SpecialType.System_String);

                // Collections
                SymbolForIDictionary = compilation.GetBestTypeByMetadataName(TypeFullName.IDictionary);

                // Use for type equivalency checks for unbounded generics
                SymbolForICollection = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).ConstructUnboundGenericType();
                SymbolForGenericIDictionary = compilation.GetBestTypeByMetadataName(TypeFullName.GenericIDictionary)?.ConstructUnboundGenericType();
                SymbolForGenericIList = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).ConstructUnboundGenericType();
                SymbolForISet = compilation.GetBestTypeByMetadataName(TypeFullName.ISet)?.ConstructUnboundGenericType();

                // Used to construct concrete types at runtime; cannot also be constructed
                SymbolForDictionary = compilation.GetBestTypeByMetadataName(TypeFullName.Dictionary);
                SymbolForHashSet = compilation.GetBestTypeByMetadataName(TypeFullName.HashSet);
                SymbolForList = compilation.GetBestTypeByMetadataName(TypeFullName.List);
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
