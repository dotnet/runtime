// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
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
            CodeAnalysis.Location location = syntaxReference is not null
                ? syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                : CodeAnalysis.Location.None;

            return location.CreateDiagnostic(descriptor, args);
        }

        public static Diagnostic CreateDiagnostic(
            this AttributeData attributeData,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            SyntaxReference? syntaxReference = attributeData.ApplicationSyntaxReference;
            CodeAnalysis.Location location = syntaxReference is not null
                ? syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                : CodeAnalysis.Location.None;

            return location.CreateDiagnostic(descriptor, properties, args);
        }

        public static Diagnostic CreateDiagnostic(
            this ImmutableArray<CodeAnalysis.Location> locations,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return CreateDiagnostic(locations, descriptor, properties: null, args);
        }

        public static Diagnostic CreateDiagnostic(
            this ImmutableArray<CodeAnalysis.Location> locations,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            CodeAnalysis.Location firstLocation = null;
            List<CodeAnalysis.Location> additionalLocations = null;
            foreach (CodeAnalysis.Location location in locations)
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
                Diagnostic.Create(descriptor, CodeAnalysis.Location.None, properties: properties, args) :
                Diagnostic.Create(descriptor, firstLocation, additionalLocations: additionalLocations, properties: properties, messageArgs: args);
        }

        public static Diagnostic CreateDiagnostic(
            this CodeAnalysis.Location location,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return Diagnostic.Create(
                descriptor,
                location: location.IsInSource ? location : CodeAnalysis.Location.None,
                messageArgs: args);
        }

        public static Diagnostic CreateDiagnostic(
            this CodeAnalysis.Location location,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            return Diagnostic.Create(
                descriptor,
                location: location.IsInSource ? location : CodeAnalysis.Location.None,
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
            CodeAnalysis.Location location = syntaxReference is not null
                ? syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                : CodeAnalysis.Location.None;

            return location.CreateDiagnosticInfo(descriptor, args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this AttributeData attributeData,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            SyntaxReference? syntaxReference = attributeData.ApplicationSyntaxReference;
            CodeAnalysis.Location location = syntaxReference is not null
                ? syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                : CodeAnalysis.Location.None;

            return location.CreateDiagnosticInfo(descriptor, properties, args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this ImmutableArray<CodeAnalysis.Location> locations,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return CreateDiagnosticInfo(locations, descriptor, properties: null, args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this ImmutableArray<CodeAnalysis.Location> locations,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            CodeAnalysis.Location firstLocation = null;
            List<CodeAnalysis.Location> additionalLocations = null;
            foreach (CodeAnalysis.Location location in locations)
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
                DiagnosticInfo.Create(descriptor, CodeAnalysis.Location.None, properties: properties, args) :
                DiagnosticInfo.Create(descriptor, firstLocation, additionalLocations: additionalLocations, properties: properties, messageArgs: args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this CodeAnalysis.Location location,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return DiagnosticInfo.Create(
                descriptor,
                location: location.IsInSource ? location : CodeAnalysis.Location.None,
                messageArgs: args);
        }

        public static DiagnosticInfo CreateDiagnosticInfo(
            this CodeAnalysis.Location location,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            return DiagnosticInfo.Create(
                descriptor,
                location: location.IsInSource ? location : CodeAnalysis.Location.None,
                properties: properties,
                messageArgs: args);
        }
    }


    public interface IGeneratorDiagnostics
    {
        /// <summary>
        /// Report diagnostic for configuration that is not supported by the DLL import source generator
        /// </summary>
        /// <param name="attributeData">Attribute specifying the unsupported configuration</param>
        /// <param name="configurationName">Name of the configuration</param>
        /// <param name="unsupportedValue">[Optiona] Unsupported configuration value</param>
        void ReportConfigurationNotSupported(
            AttributeData attributeData,
            string configurationName,
            string? unsupportedValue);

        void ReportInvalidMarshallingAttributeInfo(
            AttributeData attributeData,
            string reasonResourceName,
            params string[] reasonArgs);
    }

    public static class IGeneratorDiagnosticsExtensions
    {
        public static void ReportConfigurationNotSupported(this IGeneratorDiagnostics diagnostics, AttributeData attributeData, string configurationName)
            => diagnostics.ReportConfigurationNotSupported(attributeData, configurationName, null);
    }

    public class GeneratorDiagnosticProperties
    {
        public const string AddDisableRuntimeMarshallingAttribute = nameof(AddDisableRuntimeMarshallingAttribute);
    }
}
