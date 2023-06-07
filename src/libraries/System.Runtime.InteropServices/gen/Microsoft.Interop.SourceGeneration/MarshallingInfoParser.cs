// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Information from a marshalling attribute that can only be provided if the attribute is at the usage site.
    /// </summary>
    /// <param name="IndirectionDepth">The indirection depth that the info applies to.</param>
    /// <param name="CountInfo">Any collection count information provided.</param>
    /// <param name="AttributeData">The original attribute data.</param>
    public sealed record UseSiteAttributeData(int IndirectionDepth, CountInfo CountInfo, AttributeData AttributeData);

    /// <summary>
    /// A callback to get the marshalling info for a given type at the provided indirection depth with the provided attributes at its usage site.
    /// </summary>
    /// <param name="type">The managed type to get marshalling info for</param>
    /// <param name="useSiteAttributes">The attributes at the use site</param>
    /// <param name="indirectionDepth">The target indirection level</param>
    /// <returns>Marshalling info for provided information.</returns>
    public delegate MarshallingInfo GetMarshallingInfoCallback(ITypeSymbol type, UseSiteAttributeProvider useSiteAttributes, int indirectionDepth);

    /// <summary>
    /// A parser for an attribute used at the marshalling site, such as a parameter or return value attribute.
    /// </summary>
    public interface IUseSiteAttributeParser
    {
        /// <summary>
        /// Whether or not the parser can parse an attribute of the provided type.
        /// </summary>
        /// <param name="attributeType">The attribute type</param>
        /// <returns><c>true</c> if the parser can parse an attribute of the provided type; otherwise <c>false</c></returns>
        bool CanParseAttributeType(INamedTypeSymbol attributeType);

        /// <summary>
        /// Parse the use-site information out of the provided attribute.
        /// </summary>
        /// <param name="attributeData">The attribute data to parse.</param>
        /// <param name="elementInfoProvider">The provider for information about other elements. This is used to retrieve information about other parameters that might be referenced by any count information.</param>
        /// <param name="marshallingInfoCallback">A callback to provide to the <paramref name="elementInfoProvider"/> when retrieving additional information.</param>
        /// <returns>The information about the attribute at the use site.</returns>
        UseSiteAttributeData ParseAttribute(AttributeData attributeData, IElementInfoProvider elementInfoProvider, GetMarshallingInfoCallback marshallingInfoCallback);
    }

    /// <summary>
    /// A parser for an attribute that provides information about which marshaller to use.
    /// </summary>
    public interface IMarshallingInfoAttributeParser
    {
        /// <summary>
        /// Whether or not the parser can parse an attribute of the provided type.
        /// </summary>
        /// <param name="attributeType">The attribute type</param>
        /// <returns><c>true</c> if the parser can parse an attribute of the provided type; otherwise <c>false</c></returns>
        bool CanParseAttributeType(INamedTypeSymbol attributeType);

        /// <summary>
        /// Parse the attribute into marshalling information
        /// </summary>
        /// <param name="attributeData">The attribute to parse</param>
        /// <param name="type">The managed type</param>
        /// <param name="indirectionDepth">The current indirection depth</param>
        /// <param name="useSiteAttributes">Attributes provided at the usage site, such as for count information</param>
        /// <param name="marshallingInfoCallback">A callback to get marshalling info for nested elements, in the case of a collection of collections.</param>
        /// <returns>Marshalling information parsed from the attribute, or <c>null</c> if no information could be parsed from the attribute</returns>
        MarshallingInfo? ParseAttribute(AttributeData attributeData, ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback);
    }

    /// <summary>
    /// A provider of marshalling info based only on the managed type and any previously parsed use-site attribute information
    /// </summary>
    public interface ITypeBasedMarshallingInfoProvider
    {
        /// <summary>
        /// Whether or not the provider can provide marshalling info for the given managed type.
        /// </summary>
        /// <param name="type">The managed type</param>
        /// <returns><c>true</c> if the provider can provide info for the provided type; otherwise <c>false</c></returns>
        bool CanProvideMarshallingInfoForType(ITypeSymbol type);
        /// <summary>
        /// Get marshalling info for the provided type at the given indirection level.
        /// </summary>
        /// <param name="type">The managed type</param>
        /// <param name="indirectionDepth">The current indirection depth</param>
        /// <param name="useSiteAttributes">Attributes provided at the usage site, such as for count information</param>
        /// <param name="marshallingInfoCallback">A callback to get marshalling info for nested elements, in the case of a collection of collections.</param>
        /// <returns>Marshalling information for the provided type</returns>
        MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback);
    }

    /// <summary>
    /// A provider of TypePositionInfo instances for other elements in the current signature, such as parameters or return values.
    /// </summary>
    public interface IElementInfoProvider
    {
        /// <summary>
        /// Get the name for a given index.
        /// </summary>
        /// <param name="paramIndex">The index</param>
        /// <returns>The name associated with the provided index, or <see cref="string.Empty"/> if the index does not correspond to an element.</returns>
        string FindNameForParamIndex(int paramIndex);
        /// <summary>
        /// Get a <see cref="TypePositionInfo"/> instance for the given element name.
        /// </summary>
        /// <param name="attrData">The attribute to report diagnostics on.</param>
        /// <param name="elementName">The element name to retrieve a <see cref="TypePositionInfo"/> instance for.</param>
        /// <param name="marshallingInfoCallback">A callback to retrieve marshalling info to put into the <see cref="TypePositionInfo"/>.</param>
        /// <param name="rootProvider">The <see cref="IElementInfoProvider"/> to pass to the <paramref name="marshallingInfoCallback"/>.</param>
        /// <param name="info">The produced info.</param>
        /// <returns><c>true</c> if a <see cref="TypePositionInfo"/> instance could be created for the type; otherwise <c>false</c></returns>
        bool TryGetInfoForElementName(AttributeData attrData, string elementName, GetMarshallingInfoCallback marshallingInfoCallback, IElementInfoProvider rootProvider, out TypePositionInfo info);
        /// <summary>
        /// Get a <see cref="TypePositionInfo"/> instance for the given element index.
        /// </summary>
        /// <param name="attrData">The attribute to report diagnostics on.</param>
        /// <param name="paramIndex">The element index to retrieve a <see cref="TypePositionInfo"/> instance for.</param>
        /// <param name="marshallingInfoCallback">A callback to retrieve marshalling info to put into the <see cref="TypePositionInfo"/>.</param>
        /// <param name="rootProvider">The <see cref="IElementInfoProvider"/> to pass to the <paramref name="marshallingInfoCallback"/>.</param>
        /// <param name="info">The produced info.</param>
        /// <returns><c>true</c> if a <see cref="TypePositionInfo"/> instance could be created for the type; otherwise <c>false</c></returns>
        bool TryGetInfoForParamIndex(AttributeData attrData, int paramIndex, GetMarshallingInfoCallback marshallingInfoCallback, IElementInfoProvider rootProvider, out TypePositionInfo info);
    }

    /// <summary>
    /// Convenience extension methods for <see cref="IElementInfoProvider"/>.
    /// </summary>
    public static class ElementInfoProviderExtensions
    {
        /// <summary>
        /// Get a <see cref="TypePositionInfo"/> instance for the given element name.
        /// </summary>
        /// <param name="attrData">The attribute to report diagnostics on.</param>
        /// <param name="elementName">The element name to retrieve a <see cref="TypePositionInfo"/> instance for.</param>
        /// <param name="marshallingInfoCallback">A callback to retrieve marshalling info to put into the <see cref="TypePositionInfo"/>.</param>
        /// <param name="info">The produced info.</param>
        /// <returns><c>true</c> if a <see cref="TypePositionInfo"/> instance could be created for the type; otherwise <c>false</c></returns>
        public static bool TryGetInfoForElementName(this IElementInfoProvider provider, AttributeData attrData, string elementName, GetMarshallingInfoCallback marshallingInfoCallback, out TypePositionInfo info)
        {
            return provider.TryGetInfoForElementName(attrData, elementName, marshallingInfoCallback, provider, out info);
        }

        /// <summary>
        /// Get a <see cref="TypePositionInfo"/> instance for the given element index.
        /// </summary>
        /// <param name="attrData">The attribute to report diagnostics on.</param>
        /// <param name="paramIndex">The element index to retrieve a <see cref="TypePositionInfo"/> instance for.</param>
        /// <param name="marshallingInfoCallback">A callback to retrieve marshalling info to put into the <see cref="TypePositionInfo"/>.</param>
        /// <param name="info">The produced info.</param>
        /// <returns><c>true</c> if a <see cref="TypePositionInfo"/> instance could be created for the type; otherwise <c>false</c></returns>
        public static bool TryGetInfoForParamIndex(this IElementInfoProvider provider, AttributeData attrData, int paramIndex, GetMarshallingInfoCallback marshallingInfoCallback, out TypePositionInfo info)
        {
            return provider.TryGetInfoForParamIndex(attrData, paramIndex, marshallingInfoCallback, provider, out info);
        }
    }

    /// <summary>
    /// Overall parser for all marshalling info.
    /// </summary>
    /// <remarks>
    /// This type combines the provided parsers to enable parsing marshalling information from a single call.
    /// For a given managed type and use site attributes, it will parse marshalling information in the following order:
    /// <list type="number">
    /// <item>Parse attributes provided at the usage site for use-site-only information.</item>
    /// <item>Parse attributes provided at the usage site for marshalling information.</item>
    /// <item>If no marshalling information has been found yet, parse attributes provided at the definition of the managed type.</item>
    /// <item>If no marshalling information has been found yet, generate marshalling information for the managed type itself.</item>
    /// </list>
    /// </remarks>
    public sealed class MarshallingInfoParser
    {
        private readonly IGeneratorDiagnostics _diagnostics;
        private readonly IElementInfoProvider _elementInfoProvider;
        private readonly ImmutableArray<IUseSiteAttributeParser> _useSiteMarshallingAttributeParsers;
        private readonly ImmutableArray<IMarshallingInfoAttributeParser> _marshallingAttributeParsers;
        private readonly ImmutableArray<ITypeBasedMarshallingInfoProvider> _typeBasedMarshallingInfoProviders;

        /// <summary>
        /// Construct a new <see cref="MarshallingInfoParser"/>.
        /// </summary>
        /// <param name="diagnostics">The diagnostics sink to report all diagnostics to.</param>
        /// <param name="elementInfoProvider">An <see cref="IElementInfoProvider"/> to retrieve information about other elements than the current element when parsing.</param>
        /// <param name="useSiteMarshallingAttributeParsers">Parsers for retrieving use-site-only information from attributes.</param>
        /// <param name="marshallingAttributeParsers">Parsers for retrieving marshalling information from attributes and the managed type.</param>
        /// <param name="typeBasedMarshallingInfoProviders">Parsers for retrieving marshalling information from the managed type only.</param>
        public MarshallingInfoParser(
            IGeneratorDiagnostics diagnostics,
            IElementInfoProvider elementInfoProvider,
            ImmutableArray<IUseSiteAttributeParser> useSiteMarshallingAttributeParsers,
            ImmutableArray<IMarshallingInfoAttributeParser> marshallingAttributeParsers,
            ImmutableArray<ITypeBasedMarshallingInfoProvider> typeBasedMarshallingInfoProviders)
        {
            _diagnostics = diagnostics;
            // Always support cycle detection. Otherwise we can get stack-overflows, which does not provide a good dev experience for any customer scenario.
            _elementInfoProvider = new CycleDetectingElementInfoProvider(elementInfoProvider, diagnostics);
            _useSiteMarshallingAttributeParsers = useSiteMarshallingAttributeParsers;
            _marshallingAttributeParsers = marshallingAttributeParsers;
            _typeBasedMarshallingInfoProviders = typeBasedMarshallingInfoProviders;
        }

        /// <summary>
        /// Parse the marshalling info for the provided managed type and attributes at the usage site.
        /// </summary>
        /// <param name="managedType">The managed type</param>
        /// <param name="useSiteAttributes">All attributes specified at the usage site</param>
        /// <returns>The parsed marshalling information</returns>
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

    /// <summary>
    /// Wraps another <see cref="IElementInfoProvider"/> with support to detect infinite cycles in marshalling info (i.e. count information that refers to other elements that refer to the original element).
    /// </summary>
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
