// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace ILAssembler;

#pragma warning disable CA1822 // Visitor wrappers intentionally preserve the existing instance API.
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

    GrammarResult ICILVisitor<GrammarResult>.VisitTyBound(CILParser.TyBoundContext context)
        => VisitTyBound(context);

    public GrammarResult.Sequence<EntityRegistry.GenericParameterConstraintEntity> VisitTyBound(
        CILParser.TyBoundContext? context)
    {
        if (context?.Value is not ImmutableArray<TypeSpecificationValue> constraintTypes)
        {
            return new([]);
        }

        ImmutableArray<EntityRegistry.GenericParameterConstraintEntity>.Builder constraints =
            ImmutableArray.CreateBuilder<EntityRegistry.GenericParameterConstraintEntity>(
                constraintTypes.Length);
        foreach (TypeSpecificationValue constraintType in constraintTypes)
        {
            constraints.Add(
                EntityRegistry.CreateGenericConstraint(ResolveTypeSpecification(constraintType)));
        }

        return new(constraints.MoveToImmutable());
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitTypar(CILParser.TyparContext context)
        => VisitTypar(context);

    public GrammarResult.Literal<EntityRegistry.GenericParameterEntity> VisitTypar(
        CILParser.TyparContext context)
    {
        GenericParameterDeclarationValue declaration =
            GetGenericParameterDeclaration(context.Value);
        EntityRegistry.GenericParameterEntity parameter =
            EntityRegistry.CreateGenericParameter(declaration.Attributes, declaration.Name);
        foreach (TypeSpecificationValue constraintType in declaration.Constraints)
        {
            parameter.Constraints.Add(
                EntityRegistry.CreateGenericConstraint(ResolveTypeSpecification(constraintType)));
        }

        return new(parameter);
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitTyparAttrib(CILParser.TyparAttribContext context)
        => VisitTyparAttrib(context);

    public static GrammarResult.Flag<GenericParameterAttributes> VisitTyparAttrib(
        CILParser.TyparAttribContext context)
    {
        AttributeValue<GenericParameterAttributes> attribute =
            GetAttributeValue<GenericParameterAttributes>(context.Value);
        return new(attribute.Value, attribute.ShouldAppend, attribute.GroupMask);
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitTyparAttribs(CILParser.TyparAttribsContext context)
        => VisitTyparAttribs(context);

    public static GrammarResult.Literal<GenericParameterAttributes> VisitTyparAttribs(
        CILParser.TyparAttribsContext context)
        => new(GetAttributeValue<GenericParameterAttributes>(context.Value).Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitTypars(CILParser.TyparsContext context)
        => VisitTypars(context);

    public GrammarResult.Sequence<EntityRegistry.GenericParameterEntity> VisitTypars(
        CILParser.TyparsContext context)
    {
        ImmutableArray<GenericParameterDeclarationValue> declarations =
            GetGenericParameterDeclarations(context.Value);
        ImmutableArray<EntityRegistry.GenericParameterEntity>.Builder parameters =
            ImmutableArray.CreateBuilder<EntityRegistry.GenericParameterEntity>(declarations.Length);
        foreach (GenericParameterDeclarationValue declaration in declarations)
        {
            EntityRegistry.GenericParameterEntity parameter =
                EntityRegistry.CreateGenericParameter(declaration.Attributes, declaration.Name);
            foreach (TypeSpecificationValue constraintType in declaration.Constraints)
            {
                parameter.Constraints.Add(
                    EntityRegistry.CreateGenericConstraint(ResolveTypeSpecification(constraintType)));
            }
            parameters.Add(parameter);
        }

        return new(parameters.MoveToImmutable());
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitTyparsClause(CILParser.TyparsClauseContext context)
        => VisitTyparsClause(context);

    public GrammarResult.Sequence<EntityRegistry.GenericParameterEntity> VisitTyparsClause(
        CILParser.TyparsClauseContext context)
    {
        ImmutableArray<GenericParameterDeclarationValue> declarations =
            GetGenericParameterDeclarations(context.Value);
        ImmutableArray<EntityRegistry.GenericParameterEntity>.Builder parameters =
            ImmutableArray.CreateBuilder<EntityRegistry.GenericParameterEntity>(declarations.Length);
        foreach (GenericParameterDeclarationValue declaration in declarations)
        {
            parameters.Add(
                EntityRegistry.CreateGenericParameter(declaration.Attributes, declaration.Name));
        }

        return new(parameters.MoveToImmutable());
    }
}
