// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private static void RegisterGenericParameterNames(
        EntityRegistry.EntityBase owner,
        NamedElementList<EntityRegistry.GenericParameterEntity> parameters,
        ImmutableArray<GenericParameterDeclarationValue> declarations)
    {
        for (int i = 0; i < declarations.Length; i++)
        {
            GenericParameterDeclarationValue declaration = declarations[i];
            EntityRegistry.GenericParameterEntity parameter =
                EntityRegistry.CreateGenericParameter(declaration.Attributes, declaration.Name);
            parameter.Owner = owner;
            parameter.Index = i;
            parameters.Add(parameter);
        }
    }
    private void MaterializeGenericParameterConstraints(
        NamedElementList<EntityRegistry.GenericParameterEntity> parameters,
        List<EntityRegistry.GenericParameterConstraintEntity> constraints,
        ImmutableArray<GenericParameterDeclarationValue> declarations)
    {
        Debug.Assert(parameters.Count >= declarations.Length);
        int count = System.Math.Min(parameters.Count, declarations.Length);
        for (int i = 0; i < count; i++)
        {
            EntityRegistry.GenericParameterEntity parameter = parameters[i];
            foreach (TypeSpecificationValue constraintType in declarations[i].Constraints)
            {
                EntityRegistry.GenericParameterConstraintEntity constraint =
                    EntityRegistry.CreateGenericConstraint(ResolveTypeSpecification(constraintType));
                constraint.Owner = parameter;
                parameter.Constraints.Add(constraint);
                constraints.Add(constraint);
            }
        }
    }

}
