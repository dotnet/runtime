// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    public GrammarResult.Literal<EntityRegistry.EntityBase> VisitMethodRef(CILParser.MethodRefContext context)
        => new(MaterializeMethodReference(GetMethodReferenceValue(context.Value)));

    public GrammarResult.Literal<EntityRegistry.TypeEntity> VisitTypeSpec(CILParser.TypeSpecContext context)
        => new(ResolveTypeSpecification(GetTypeSpecificationValue(context.Value)));
}
