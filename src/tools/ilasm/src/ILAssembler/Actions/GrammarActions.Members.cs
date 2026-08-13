// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    public GrammarResult VisitClassDecl(CILParser.ClassDeclContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);
}
