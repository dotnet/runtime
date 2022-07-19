// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed record UseSiteAttributeData(int IndirectionDepth, CountInfo CountInfo, AttributeData AttributeData);

    public delegate MarshallingInfo GetMarshallingInfoCallback(ITypeSymbol type, UseSiteAttributeProvider useSiteAttributes, int indirectionDepth);

    public interface IUseSiteAttributeParser
    {
        bool CanParseAttributeType(INamedTypeSymbol attributeType);

        UseSiteAttributeData ParseAttribute(AttributeData attributeData, IElementInfoProvider elementInfoProvider, GetMarshallingInfoCallback marshallingInfoCallback);
    }

    public interface IMarshallingInfoAttributeParser
    {
        bool CanParseAttributeType(INamedTypeSymbol attributeType);

        MarshallingInfo? ParseAttribute(AttributeData attributeData, ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback);
    }

    public interface ITypeBasedMarshallingInfoProvider
    {
        bool CanProvideMarshallingInfoForType(ITypeSymbol type);
        MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback);
    }

    public interface IElementInfoProvider
    {
        string FindNameForParamIndex(int paramIndex);
        bool TryGetInfoForElementName(AttributeData attrData, string elementName, GetMarshallingInfoCallback marshallingInfoCallback, IElementInfoProvider rootProvider, out TypePositionInfo info);
        bool TryGetInfoForParamIndex(AttributeData attrData, int paramIndex, GetMarshallingInfoCallback marshallingInfoCallback, IElementInfoProvider rootProvider, out TypePositionInfo info);
    }

    public static class ElementInfoProvider
    {
        public static bool TryGetInfoForElementName(this IElementInfoProvider provider, AttributeData attrData, string elementName, GetMarshallingInfoCallback marshallingInfoCallback, out TypePositionInfo info)
        {
            return provider.TryGetInfoForElementName(attrData, elementName, marshallingInfoCallback, provider, out info);
        }
        public static bool TryGetInfoForParamIndex(this IElementInfoProvider provider, AttributeData attrData, int paramIndex, GetMarshallingInfoCallback marshallingInfoCallback, out TypePositionInfo info)
        {
            return provider.TryGetInfoForParamIndex(attrData, paramIndex, marshallingInfoCallback, provider, out info);
        }
    }

    public sealed class MarshallingInfoParser
    {
        private readonly IGeneratorDiagnostics _diagnostics;
        private readonly IElementInfoProvider _elementInfoProvider;
        private readonly ImmutableArray<IUseSiteAttributeParser> _useSiteMarshallingAttributeParsers;
        private readonly ImmutableArray<IMarshallingInfoAttributeParser> _marshallingAttributeParsers;
        private readonly ImmutableArray<ITypeBasedMarshallingInfoProvider> _typeBasedMarshallingInfoProviders;

        public MarshallingInfoParser(
            IGeneratorDiagnostics diagnostics,
            IElementInfoProvider elementInfoProvider,
            ImmutableArray<IUseSiteAttributeParser> useSiteMarshallingAttributeParsers,
            ImmutableArray<IMarshallingInfoAttributeParser> marshallingAttributeParsers,
            ImmutableArray<ITypeBasedMarshallingInfoProvider> typeBasedMarshallingInfoProviders)
        {
            _diagnostics = diagnostics;
            _elementInfoProvider = new CycleDetectingElementInfoProvider(elementInfoProvider, diagnostics);
            _useSiteMarshallingAttributeParsers = useSiteMarshallingAttributeParsers;
            _marshallingAttributeParsers = marshallingAttributeParsers;
            _typeBasedMarshallingInfoProviders = typeBasedMarshallingInfoProviders;
        }

        public MarshallingInfo ParseMarshallingInfo(
            ITypeSymbol managedType,
            IEnumerable<AttributeData> useSiteAttributes)
        {
            UseSiteAttributeProvider useSiteAttributeProvider = new UseSiteAttributeProvider(_useSiteMarshallingAttributeParsers, useSiteAttributes, _elementInfoProvider, _diagnostics, GetMarshallingInfo);

            MarshallingInfo info = GetMarshallingInfo(
                managedType,
                useSiteAttributeProvider,
                indirectionDepth: 0);

            useSiteAttributeProvider.OnAttributeUsageFinished();
            return info;
        }

        private MarshallingInfo GetMarshallingInfo(
            ITypeSymbol type,
            UseSiteAttributeProvider useSiteAttributes,
            int indirectionDepth)
        {
            if (useSiteAttributes.TryGetUseSiteAttributeInfo(indirectionDepth, out UseSiteAttributeData useSiteAttribute))
            {
                if (GetMarshallingInfoForAttribute(useSiteAttribute.AttributeData, type, indirectionDepth, useSiteAttributes, GetMarshallingInfo) is MarshallingInfo marshallingInfo)
                {
                    return marshallingInfo;
                }
            }

            // If we aren't overriding the marshalling at usage time,
            // then fall back to the information on the element type itself.
            foreach (AttributeData typeAttribute in type.GetAttributes())
            {
                if (GetMarshallingInfoForAttribute(typeAttribute, type, indirectionDepth, useSiteAttributes, GetMarshallingInfo) is MarshallingInfo marshallingInfo)
                {
                    return marshallingInfo;
                }
            }

            // If the type doesn't have custom attributes that dictate marshalling,
            // then consider the type itself.

            return GetMarshallingInfoForType(type, indirectionDepth, useSiteAttributes, GetMarshallingInfo) ?? NoMarshallingInfo.Instance;
        }

        private MarshallingInfo? GetMarshallingInfoForAttribute(AttributeData attribute, ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            foreach (var parser in _marshallingAttributeParsers)
            {
                // Automatically ignore invalid attributes.
                // The compiler will already error on them.
                if (attribute.AttributeConstructor is not null && parser.CanParseAttributeType(attribute.AttributeClass))
                {
                    return parser.ParseAttribute(attribute, type, indirectionDepth, useSiteAttributes, marshallingInfoCallback);
                }
            }
            return null;
        }

        private MarshallingInfo? GetMarshallingInfoForType(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            foreach (var parser in _typeBasedMarshallingInfoProviders)
            {
                if (parser.CanProvideMarshallingInfoForType(type))
                {
                    return parser.GetMarshallingInfo(type, indirectionDepth, useSiteAttributes, marshallingInfoCallback);
                }
            }
            return null;
        }
    }

    internal sealed class CycleDetectingElementInfoProvider : IElementInfoProvider
    {
        private ImmutableHashSet<string> _activeInspectingElements = ImmutableHashSet<string>.Empty;
        private readonly IElementInfoProvider _innerProvider;
        private readonly IGeneratorDiagnostics _diagnostics;

        public CycleDetectingElementInfoProvider(IElementInfoProvider innerProvider, IGeneratorDiagnostics diagnostics)
        {
            _innerProvider = innerProvider;
            _diagnostics = diagnostics;
        }

        public string FindNameForParamIndex(int paramIndex) => _innerProvider.FindNameForParamIndex(paramIndex);
        public bool TryGetInfoForElementName(AttributeData attrData, string elementName, GetMarshallingInfoCallback marshallingInfoCallback, IElementInfoProvider rootProvider, [NotNullWhen(true)] out TypePositionInfo? info)
        {
            ImmutableHashSet<string> inspectedElements = _activeInspectingElements;
            if (inspectedElements.Contains(elementName))
            {
                throw new CyclicalElementInfoException(inspectedElements, elementName);
            }
            try
            {
                _activeInspectingElements = inspectedElements.Add(elementName);
                return _innerProvider.TryGetInfoForElementName(attrData, elementName, marshallingInfoCallback, rootProvider, out info);
            }
            // Specifically catch the exception when we're trying to inspect the element that started the cycle.
            // This ensures that we've unwound the whole cycle so when we return, there will be no cycles in the count info.
            catch (CyclicalElementInfoException ex) when (ex.StartOfCycle == elementName)
            {
                _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.CyclicalCountInfo), elementName);
                // Create a dummy value for the invalid marshalling. We're already in an error state, so try to not report extraneous diagnostics.
                info = new TypePositionInfo(SpecialTypeInfo.Void, NoMarshallingInfo.Instance);
                return true;
            }
            finally
            {
                _activeInspectingElements = inspectedElements;
            }
        }

        public bool TryGetInfoForParamIndex(AttributeData attrData, int paramIndex, GetMarshallingInfoCallback marshallingInfoCallback, IElementInfoProvider rootProvider, [NotNullWhen(true)] out TypePositionInfo? info)
        {
            ImmutableHashSet<string> inspectedElements = _activeInspectingElements;
            string paramName = _innerProvider.FindNameForParamIndex(paramIndex);
            if (paramName is not null && inspectedElements.Contains(paramName))
            {
                throw new CyclicalElementInfoException(inspectedElements, paramName);
            }

            try
            {
                _activeInspectingElements = inspectedElements.Add(paramName);
                return _innerProvider.TryGetInfoForParamIndex(attrData, paramIndex, marshallingInfoCallback, rootProvider, out info);
            }
            // Specifically catch the exception when we're trying to inspect the element that started the cycle.
            // This ensures that we've unwound the whole cycle so when we return, there will be no cycles in the count info.
            catch (CyclicalElementInfoException ex) when (ex.StartOfCycle == paramName)
            {
                _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.CyclicalCountInfo), paramName);
                // Create a dummy value for the invalid marshalling. We're already in an error state, so try to not report extraneous diagnostics.
                info = new TypePositionInfo(SpecialTypeInfo.Void, NoMarshallingInfo.Instance);
                return true;
            }
            finally
            {
                _activeInspectingElements = inspectedElements;
            }
        }

        private sealed class CyclicalElementInfoException : Exception
        {
            public CyclicalElementInfoException(ImmutableHashSet<string> elementsInCycle, string startOfCycle)
            {
                ElementsInCycle = elementsInCycle;
                StartOfCycle = startOfCycle;
            }

            public ImmutableHashSet<string> ElementsInCycle { get; }

            public string StartOfCycle { get; }
        }
    }
}
