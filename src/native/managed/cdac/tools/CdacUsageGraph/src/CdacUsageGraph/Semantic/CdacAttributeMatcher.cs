// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CdacUsageGraph.Model;

namespace CdacUsageGraph.Semantic;

/// <summary>Matches and reads cDAC attributes resolved from one compilation.</summary>
internal sealed class CdacAttributeMatcher
{
    private readonly INamedTypeSymbol? _cdacType;
    private readonly INamedTypeSymbol? _dataDescriptorDependency;
    private readonly INamedTypeSymbol? _usesDataDescriptorTypeSize;
    private readonly INamedTypeSymbol? _staticReference;

    public CdacAttributeMatcher(CSharpCompilation compilation)
    {
        _cdacType = compilation.GetTypeByMetadataName(
            CdacSymbols.CdacTypeAttributeMetadataName);
        _dataDescriptorDependency = compilation.GetTypeByMetadataName(
            CdacSymbols.DataDescriptorDependencyAttributeMetadataName);
        _usesDataDescriptorTypeSize = compilation.GetTypeByMetadataName(
            CdacSymbols.UsesDataDescriptorTypeSizeAttributeMetadataName);
        _staticReference = compilation.GetTypeByMetadataName(
            CdacSymbols.StaticReferenceAttributeMetadataName);
    }

    public string[] GetNames(INamedTypeSymbol symbol)
    {
        AttributeData? attribute = symbol.GetAttributes().FirstOrDefault(IsCdacType);
        if (attribute is not { ConstructorArguments.Length: > 0 })
            return [symbol.Name];

        List<string> names = attribute.ConstructorArguments[0].Values
            .Where(value => value.Value is string)
            .Select(value => (string)value.Value!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (!names.Contains(symbol.Name, StringComparer.Ordinal))
            names.Add(symbol.Name);
        return names.ToArray();
    }

    public bool TryGetDescriptorDependencies(
        ISymbol member,
        out DataDescriptorDependencies dependencies)
    {
        AttributeData[] attributes = GetAttributes(member).ToArray();
        DataDescriptorFieldDependency[] fields = attributes
            .Where(IsDataDescriptorDependency)
            .Select(attribute => attribute.ConstructorArguments)
            .Where(arguments =>
                arguments.Length is 2 or 3 &&
                arguments[0].Value is string &&
                arguments[1].Value is string &&
                (arguments.Length == 2 || arguments[2].Value is null or string))
            .Select(arguments => new DataDescriptorFieldDependency(
                (string)arguments[0].Value!,
                (string)arguments[1].Value!,
                arguments.Length == 3 ? arguments[2].Value as string : null))
            .Distinct()
            .ToArray();
        string?[] typeSizeTypeNames = attributes
            .Where(IsUsesDataDescriptorTypeSize)
            .Select(attribute => attribute.ConstructorArguments)
            .Where(arguments =>
                arguments.Length == 0 ||
                arguments is [{ Value: null or string }])
            .Select(arguments =>
                arguments.Length == 0 ? null : arguments[0].Value as string)
            .Distinct()
            .ToArray();
        if (fields.Length == 0 && typeSizeTypeNames.Length == 0)
        {
            dependencies = null!;
            return false;
        }

        dependencies = new DataDescriptorDependencies(fields, typeSizeTypeNames);
        return true;
    }

    public bool TryGetStaticReferenceFieldName(ISymbol member, out string fieldName)
    {
        foreach (AttributeData attribute in GetAttributes(member))
        {
            if (IsStaticReference(attribute) &&
                attribute.ConstructorArguments is [{ Value: string value }])
            {
                fieldName = value;
                return true;
            }
        }

        fieldName = null!;
        return false;
    }

    private bool IsCdacType(AttributeData attribute) =>
        Matches(attribute, _cdacType);

    private bool IsDataDescriptorDependency(AttributeData attribute) =>
        Matches(attribute, _dataDescriptorDependency);

    private bool IsUsesDataDescriptorTypeSize(AttributeData attribute) =>
        Matches(attribute, _usesDataDescriptorTypeSize);

    private bool IsStaticReference(AttributeData attribute) =>
        Matches(attribute, _staticReference);

    private static bool Matches(AttributeData attribute, INamedTypeSymbol? attributeType) =>
        attribute.AttributeClass is not null &&
        attributeType is not null &&
        SymbolEqualityComparer.Default.Equals(
            attribute.AttributeClass.OriginalDefinition,
            attributeType.OriginalDefinition);

    private static IEnumerable<AttributeData> GetAttributes(ISymbol member)
    {
        HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default);
        foreach (ISymbol symbol in GetRelatedSymbols(member))
        {
            foreach (ISymbol part in GetParts(symbol))
            {
                if (!visited.Add(part))
                    continue;

                foreach (AttributeData attribute in part.GetAttributes())
                    yield return attribute;
            }
        }
    }

    private static IEnumerable<ISymbol> GetRelatedSymbols(ISymbol member)
    {
        yield return member;
        yield return member.OriginalDefinition;

        if (member is IMethodSymbol { AssociatedSymbol: { } associated })
        {
            yield return associated;
            yield return associated.OriginalDefinition;
        }
    }

    private static IEnumerable<ISymbol> GetParts(ISymbol member)
    {
        yield return member;
        switch (member)
        {
            case IMethodSymbol method:
                if (method.PartialDefinitionPart is { } definition)
                    yield return definition;
                if (method.PartialImplementationPart is { } implementation)
                    yield return implementation;
                break;
            case IPropertySymbol property:
                if (property.PartialDefinitionPart is { } propertyDefinition)
                    yield return propertyDefinition;
                if (property.PartialImplementationPart is { } propertyImplementation)
                    yield return propertyImplementation;
                break;
        }
    }
}
