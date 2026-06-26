// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.DataGenerator;

/// <summary>
/// Source generator for cdac <see cref="IData{T}"/> classes. Emits the
/// boilerplate <c>IData&lt;T&gt;.Create</c> factory, managed-type
/// <c>TypeHandle</c> accessors, and static-field accessors from
/// declarative attributes.
/// </summary>
/// <remarks>
/// The trigger attributes (<c>[CdacType]</c>, <c>[Field]</c>, etc.) live in
/// the <c>Microsoft.Diagnostics.DataContractReader</c> root namespace inside
/// the <c>Microsoft.Diagnostics.DataContractReader.Abstractions</c> assembly.
/// They are ordinary committed source so cross-assembly references resolve
/// to a single type identity, matching the convention used by
/// <c>LibraryImportGenerator</c> and <c>JsonSerializerGenerator</c>.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class CdacGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Emit the LayoutSet and TypeNameResolver helpers as internal source ONLY
        // when the consuming assembly doesn't already see them (via project reference
        // + InternalsVisibleTo). This avoids CS0436 type-conflict errors in assemblies
        // that inherit the helpers from a referenced assembly (e.g. the Tests project
        // sees Contracts' copy via [InternalsVisibleTo] and shouldn't emit its own).
        // Each helper is gated independently to handle version-skew scenarios where
        // one helper is present but the other is not.
        IncrementalValueProvider<(bool EmitLayoutSet, bool EmitTypeNameResolver, bool EmitGeneratedTypeCacheContract)> shouldEmitHelpers = context.CompilationProvider
            .Select(static (compilation, _) => (
                EmitLayoutSet: !IsTypeAccessible(compilation, LayoutSetSource.FullyQualifiedName),
                EmitTypeNameResolver: !IsTypeAccessible(compilation, TypeNameResolverSource.FullyQualifiedName),
                EmitGeneratedTypeCacheContract: !IsTypeAccessible(compilation, GeneratedTypeCacheContractSource.FullyQualifiedName)));

        context.RegisterSourceOutput(shouldEmitHelpers, static (ctx, flags) =>
        {
            if (flags.EmitLayoutSet)
            {
                ctx.AddSource(
                    LayoutSetSource.HintName,
                    SourceText.From(LayoutSetSource.Source, Encoding.UTF8));
            }

            if (flags.EmitTypeNameResolver)
            {
                ctx.AddSource(
                    TypeNameResolverSource.HintName,
                    SourceText.From(TypeNameResolverSource.Source, Encoding.UTF8));
            }

            if (flags.EmitGeneratedTypeCacheContract)
            {
                ctx.AddSource(
                    GeneratedTypeCacheContractSource.HintName,
                    SourceText.From(GeneratedTypeCacheContractSource.Source, Encoding.UTF8));
            }
        });

        IncrementalValuesProvider<CdacTypeModel> models = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Parser.CdacTypeAttributeFqn,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => Parser.Parse(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(models, static (spc, model) =>
            spc.AddSource(Emitter.HintNameFor(model), SourceText.From(Emitter.Emit(model), Encoding.UTF8)));
    }

    /// <summary>
    /// Returns true if the consuming compilation can already see a usable
    /// <paramref name="metadataName"/> (declared here, or in a referenced assembly
    /// and accessible).
    /// </summary>
    private static bool IsTypeAccessible(Compilation compilation, string metadataName)
    {
        foreach (INamedTypeSymbol type in compilation.GetTypesByMetadataName(metadataName))
        {
            if (compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
            {
                return true;
            }
        }

        return false;
    }
}
