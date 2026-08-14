// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
#pragma warning disable CA1822 // Structural rules are driven by parser actions.
    public GrammarResult VisitAlignment(CILParser.AlignmentContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitCorflags(CILParser.CorflagsContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitImagebase(CILParser.ImagebaseContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitModuleHead(CILParser.ModuleHeadContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitMscorlib(CILParser.MscorlibContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitStackreserve(CILParser.StackreserveContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitSubsystem(CILParser.SubsystemContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitTypelist(CILParser.TypelistContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);
#pragma warning restore CA1822
}
