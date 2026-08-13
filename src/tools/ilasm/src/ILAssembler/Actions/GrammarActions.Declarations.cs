// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
#pragma warning disable CA1822 // Structural rules are driven by parser actions.
    public GrammarResult VisitDecl(CILParser.DeclContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitDecls(CILParser.DeclsContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitAssemblyRefBlock(CILParser.AssemblyRefBlockContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitExptypeBlock(CILParser.ExptypeBlockContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitManifestResBlock(CILParser.ManifestResBlockContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

#pragma warning restore CA1822
}
