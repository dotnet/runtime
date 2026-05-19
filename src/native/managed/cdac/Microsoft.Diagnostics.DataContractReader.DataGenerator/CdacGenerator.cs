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
/// <c>Microsoft.Diagnostics.DataContractReader.Abstractions</c> under
/// namespace <c>Microsoft.Diagnostics.DataContractReader.Generated</c>. They
/// are ordinary committed source so cross-assembly references resolve to a
/// single type identity, matching the convention used by
/// <c>LibraryImportGenerator</c> and <c>JsonSerializerGenerator</c>.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class CdacGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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
}
