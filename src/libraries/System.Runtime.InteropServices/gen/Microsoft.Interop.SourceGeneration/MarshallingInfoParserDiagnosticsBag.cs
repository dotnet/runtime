// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Resources;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public static class DiagnosticExtensions
    {
        public static Diagnostic CreateDiagnostic(
            this ISymbol symbol,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return symbol.Locations.CreateDiagnostic(descriptor, args);
        }

        public static Diagnostic CreateDiagnostic(
            this ISymbol symbol,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            return symbol.Locations.CreateDiagnostic(descriptor, properties, args);
        }

        public static Diagnostic CreateDiagnostic(
            this AttributeData attributeData,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            SyntaxReference? syntaxReference = attributeData.ApplicationSyntaxReference;
            Location location = syntaxReference is not null
                ? syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                : Location.None;

            return location.CreateDiagnostic(descriptor, args);
        }

        public static Diagnostic CreateDiagnostic(
            this AttributeData attributeData,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            SyntaxReference? syntaxReference = attributeData.ApplicationSyntaxReference;
            Location location = syntaxReference is not null
                ? syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                : Location.None;

            return location.CreateDiagnostic(descriptor, properties, args);
        }

        public static Diagnostic CreateDiagnostic(
            this ImmutableArray<Location> locations,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return CreateDiagnostic(locations, descriptor, properties: null, args);
        }

        public static Diagnostic CreateDiagnostic(
            this ImmutableArray<Location> locations,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            Location firstLocation = null;
            List<Location> additionalLocations = null;
            foreach (Location location in locations)
            {
                if (location.IsInSource)
                {
                    if (firstLocation is null)
                    {
                        firstLocation = location;
                    }
                    else
                    {
                        (additionalLocations ??= new()).Add(location);
                    }
                }
            }

            return firstLocation is null ?
                Diagnostic.Create(descriptor, Location.None, properties: properties, args) :
                Diagnostic.Create(descriptor, firstLocation, additionalLocations: additionalLocations, properties: properties, messageArgs: args);
        }

        public static Diagnostic CreateDiagnostic(
            this Location location,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return Diagnostic.Create(
                descriptor,
                location: location.IsInSource ? location : Location.None,
                messageArgs: args);
        }

        public static Diagnostic CreateDiagnostic(
            this Location location,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            return Diagnostic.Create(
                descriptor,
                location: location.IsInSource ? location : Location.None,
                properties: properties,
                messageArgs: args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this ISymbol symbol,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return symbol.Locations.CreateDiagnosticInfo(descriptor, args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this ISymbol symbol,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            return symbol.Locations.CreateDiagnosticInfo(descriptor, properties, args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this AttributeData attributeData,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            SyntaxReference? syntaxReference = attributeData.ApplicationSyntaxReference;
            Location location = syntaxReference is not null
                ? syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                : Location.None;

            return location.CreateDiagnosticInfo(descriptor, args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this AttributeData attributeData,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            SyntaxReference? syntaxReference = attributeData.ApplicationSyntaxReference;
            Location location = syntaxReference is not null
                ? syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                : Location.None;

            return location.CreateDiagnosticInfo(descriptor, properties, args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this ImmutableArray<Location> locations,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return CreateDiagnosticInfo(locations, descriptor, properties: null, args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this ImmutableArray<Location> locations,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            Location firstLocation = null;
            List<Location> additionalLocations = null;
            foreach (Location location in locations)
            {
                if (location.IsInSource)
                {
                    if (firstLocation is null)
                    {
                        firstLocation = location;
                    }
                    else
                    {
                        (additionalLocations ??= new()).Add(location);
                    }
                }
            }

            return firstLocation is null ?
                DiagnosticInfo.Create(descriptor, Location.None, properties: properties, args) :
                DiagnosticInfo.Create(descriptor, firstLocation, additionalLocations: additionalLocations, properties: properties, messageArgs: args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this Location location,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return DiagnosticInfo.Create(
                descriptor,
                location: location.IsInSource ? location : Location.None,
                messageArgs: args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this Location location,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            return DiagnosticInfo.Create(
                descriptor,
                location: location.IsInSource ? location : Location.None,
                properties: properties,
                messageArgs: args);
        }
    }


    public class MarshallingInfoParserDiagnosticsBag
    {
        private readonly IDiagnosticDescriptorProvider _descriptorProvider;
        private readonly GeneratorDiagnosticBag _diagnostics;
        private readonly ResourceManager _resourceManager;
        private readonly Type _resourceSource;

        public MarshallingInfoParserDiagnosticsBag(IDiagnosticDescriptorProvider descriptorProvider, GeneratorDiagnosticBag diagnostics, ResourceManager resourceManager, Type resourceSource)
        {
            _descriptorProvider = descriptorProvider;
            _diagnostics = diagnostics;
            _resourceManager = resourceManager;
            _resourceSource = resourceSource;
        }

        /// <summary>
        /// Report diagnostic for configuration that is not supported by the DLL import source generator
        /// </summary>
        /// <param name="attributeData">Attribute specifying the unsupported configuration</param>
        /// <param name="configurationName">Name of the configuration</param>
        /// <param name="unsupportedValue">[Optiona] Unsupported configuration value</param>
        public void ReportConfigurationNotSupported(
            AttributeData attributeData,
            string configurationName,
            string? unsupportedValue)
        {
            _diagnostics.ReportConfigurationNotSupported(_descriptorProvider, attributeData, configurationName, unsupportedValue);
        }

        public void ReportInvalidMarshallingAttributeInfo(
            AttributeData attributeData,
            string reasonResourceName,
            params string[] reasonArgs)
        {
            _diagnostics.ReportInvalidMarshallingAttributeInfo(_descriptorProvider, attributeData, _resourceManager, _resourceSource, reasonResourceName, reasonArgs);
        }
    }

    public class GeneratorDiagnosticBag
    {
        private readonly List<DiagnosticInfo> _diagnostics = new List<DiagnosticInfo>();
        public IEnumerable<DiagnosticInfo> Diagnostics => _diagnostics;

        public void ReportDiagnostic(DiagnosticInfo diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        public void ReportConfigurationNotSupported(IDiagnosticDescriptorProvider descriptorProvider, AttributeData attributeData, string configurationName, string? unsupportedValue)
        {
            if (unsupportedValue is null)
            {
                ReportDiagnostic(attributeData.CreateDiagnosticInfo(descriptorProvider.GetConfigurationNotSupportedDescriptor(withValue: false), configurationName));
            }
            else
            {
                ReportDiagnostic(attributeData.CreateDiagnosticInfo(descriptorProvider.GetConfigurationNotSupportedDescriptor(withValue: true), unsupportedValue, configurationName));
            }
        }

        public void ReportInvalidMarshallingAttributeInfo(IDiagnosticDescriptorProvider descriptorProvider, AttributeData attributeData, ResourceManager resourceManager, Type resourceSource, string reasonResourceName, params string[] reasonArgs)
        {
            ReportDiagnostic(attributeData.CreateDiagnosticInfo(descriptorProvider.InvalidMarshallingAttributeInfo, new LocalizableResourceString(reasonResourceName, resourceManager, resourceSource, reasonArgs)));
        }

        public void ReportGeneratorDiagnostic(IDiagnosticDescriptorProvider descriptorProvider, ISignatureDiagnosticLocations locations, GeneratorDiagnostic diagnostic)
        {
            _diagnostics.Add(locations.CreateDiagnosticInfo(descriptorProvider, diagnostic));
        }

        public void ReportConfigurationNotSupported(IDiagnosticDescriptorProvider descriptorProvider, AttributeData attributeData, string configurationName)
        {
            ReportDiagnostic(attributeData.CreateDiagnosticInfo(descriptorProvider.GetConfigurationNotSupportedDescriptor(withValue: false), configurationName));
        }
    }

    public static class IGeneratorDiagnosticsExtensions
    {
        public static void ReportConfigurationNotSupported(this MarshallingInfoParserDiagnosticsBag diagnostics, AttributeData attributeData, string configurationName)
            => diagnostics.ReportConfigurationNotSupported(attributeData, configurationName, null);
    }

    public class GeneratorDiagnosticProperties
    {
        public const string AddDisableRuntimeMarshallingAttribute = nameof(AddDisableRuntimeMarshallingAttribute);
    }
}
