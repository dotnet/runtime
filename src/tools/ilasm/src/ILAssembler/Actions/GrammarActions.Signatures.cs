// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    internal EntityRegistry.EntityBase MaterializeMethodReference(CILParser.MethodRefContext context)
        => MaterializeMethodReference(GetMethodReferenceValue(context.Value));

    internal EntityRegistry.TypeEntity ResolveTypeSpecification(CILParser.TypeSpecContext context)
        => ResolveTypeSpecification(GetTypeSpecificationValue(context.Value));
}
