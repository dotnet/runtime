// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    internal void BeginNamespace(CILParser.NameSpaceHeadContext context, string? namespaceName)
    {
        if (_namespaceHeaderFrames.Count == 0 ||
            !ReferenceEquals(_namespaceHeaderFrames.Peek().Owner, context) ||
            _namespaceHeaderFrames.Peek().InitialSyntaxErrorCount != _syntaxErrorCount ||
            context.exception is not null ||
            namespaceName is null)
        {
            return;
        }

        string? outerNamespace = _currentNamespace.PeekOrDefault();
        _currentNamespace.Push(
            string.IsNullOrEmpty(outerNamespace)
                ? namespaceName
                : $"{outerNamespace}.{namespaceName}");
        _namespaceOwners.Push(context.Parent);
    }

    internal void BeginType(CILParser.ClassHeadContext context, object? value)
    {
        ClassHeaderValue header = GetClassHeaderValue(value);
        if (!header.IsValid)
        {
            return;
        }

        EntityRegistry.TypeDefinitionEntity typeDefinition = MaterializeClassHeader(context, header);
        _currentTypeDefinition.Push(typeDefinition);
        _typeOwners.Push(context.Parent);
    }

    private EntityRegistry.TypeDefinitionEntity MaterializeClassHeader(
        CILParser.ClassHeadContext context,
        ClassHeaderValue header)
    {
        (string typeNamespace, string typeName) = GetTypeDefinitionName(header.FullName);
        bool isNewType = false;
        EntityRegistry.TypeDefinitionEntity typeDefinition =
            _entityRegistry.GetOrCreateTypeDefinition(
                _currentTypeDefinition.PeekOrDefault(),
                typeNamespace,
                typeName,
                newTypeDefinition =>
                {
                    isNewType = true;
                    InitializeTypeDefinition(context, header, newTypeDefinition);
                });

        if (!isNewType)
        {
            MergeTypeDefinition(header, typeDefinition);
        }

        return typeDefinition;
    }

    private (string Namespace, string Name) GetTypeDefinitionName(string fullName)
    {
        int lastDot = fullName.LastIndexOf('.');
        if (lastDot == 0)
        {
            lastDot = -1;
        }

        string typeNamespace;
        if (_currentTypeDefinition.Count != 0)
        {
            typeNamespace = lastDot == -1 ? string.Empty : fullName.Substring(0, lastDot);
        }
        else if (lastDot == -1)
        {
            typeNamespace = _currentNamespace.PeekOrDefault() ?? string.Empty;
        }
        else
        {
            typeNamespace =
                $"{_currentNamespace.PeekOrDefault()}{fullName.Substring(0, lastDot)}";
        }

        return (
            typeNamespace,
            lastDot == -1 ? fullName : fullName.Substring(lastDot + 1));
    }

    private void InitializeTypeDefinition(
        CILParser.ClassHeadContext context,
        ClassHeaderValue header,
        EntityRegistry.TypeDefinitionEntity typeDefinition)
    {
        EntityRegistry.WellKnownBaseType? fallbackBase =
            _options.NoAutoInherit ? null : EntityRegistry.WellKnownBaseType.System_Object;
        bool requireSealed = false;
        TypeAttributes attributes = 0;
        foreach (ClassAttributeValue classAttribute in header.Attributes)
        {
            if (classAttribute.FallbackBase is not null)
            {
                fallbackBase = classAttribute.FallbackBase;
            }

            AttributeValue<TypeAttributes> attribute = classAttribute.Attribute;
            if (!attribute.ShouldAppend)
            {
                attributes = attribute.Value;
                requireSealed = classAttribute.RequireSealed;
                continue;
            }

            requireSealed |= classAttribute.RequireSealed;
            if (attribute.Value == TypeAttributes.RTSpecialName)
            {
                continue;
            }

            attributes = ApplyAttribute(attributes, attribute);
        }

        typeDefinition.Attributes = attributes;
        RegisterGenericParameterNames(
            typeDefinition,
            typeDefinition.GenericParameters,
            header.GenericParameters);

        _currentTypeDefinition.Push(typeDefinition);
        try
        {
            MaterializeGenericParameterConstraints(
                typeDefinition.GenericParameters,
                typeDefinition.GenericParameterConstraints,
                header.GenericParameters);
            if (header.BaseType is not null)
            {
                typeDefinition.BaseType = ResolveTypeSpecification(header.BaseType);
            }

            AddInterfaceImplementations(typeDefinition, header.Interfaces);
        }
        finally
        {
            _currentTypeDefinition.Pop();
        }

        if (typeDefinition.Attributes.HasFlag(TypeAttributes.Interface))
        {
            fallbackBase = null;
        }

        typeDefinition.BaseType ??= _entityRegistry.ResolveImplicitBaseType(fallbackBase);
        if (!typeDefinition.Attributes.HasFlag(TypeAttributes.Sealed) &&
            (requireSealed || _entityRegistry.SystemValueTypeType.Equals(typeDefinition.BaseType)))
        {
            IToken location = header.NameToken ?? context.Start;
            _diagnostics.Add(
                new Diagnostic(
                    DiagnosticIds.UnsealedValueType,
                    DiagnosticSeverity.Error,
                    string.Format(DiagnosticMessageTemplates.UnsealedValueType, typeDefinition.Name),
                    Location.From(location, _documents)));
            typeDefinition.Attributes |= TypeAttributes.Sealed;
        }
    }

    private void MergeTypeDefinition(
        ClassHeaderValue header,
        EntityRegistry.TypeDefinitionEntity typeDefinition)
    {
        TypeAttributes attributes = typeDefinition.Attributes;
        foreach (ClassAttributeValue classAttribute in header.Attributes)
        {
            AttributeValue<TypeAttributes> attribute = classAttribute.Attribute;
            if (!attribute.ShouldAppend)
            {
                attributes = attribute.Value;
            }
            else if ((attribute.Value & TypeAttributes.Interface) != 0)
            {
                attributes |= TypeAttributes.Interface | TypeAttributes.Abstract;
            }
            else
            {
                attributes |= attribute.Value;
            }
        }
        typeDefinition.Attributes = attributes;

        bool materializeConstraints = typeDefinition.GenericParameters.Count == 0;
        if (materializeConstraints)
        {
            RegisterGenericParameterNames(
                typeDefinition,
                typeDefinition.GenericParameters,
                header.GenericParameters);
        }

        _currentTypeDefinition.Push(typeDefinition);
        try
        {
            if (materializeConstraints)
            {
                MaterializeGenericParameterConstraints(
                    typeDefinition.GenericParameters,
                    typeDefinition.GenericParameterConstraints,
                    header.GenericParameters);
            }

            if (header.BaseType is not null)
            {
                EntityRegistry.TypeEntity baseType = ResolveTypeSpecification(header.BaseType);
                typeDefinition.BaseType ??= baseType;
            }

            AddInterfaceImplementations(typeDefinition, header.Interfaces);
        }
        finally
        {
            _currentTypeDefinition.Pop();
        }
    }

    private void AddInterfaceImplementations(
        EntityRegistry.TypeDefinitionEntity typeDefinition,
        ImmutableArray<TypeSpecificationValue> interfaces)
    {
        foreach (TypeSpecificationValue interfaceType in interfaces)
        {
            typeDefinition.InterfaceImplementations.Add(
                EntityRegistry.CreateUnrecordedInterfaceImplementation(
                    typeDefinition,
                    ResolveTypeSpecification(interfaceType)));
        }
    }
}
