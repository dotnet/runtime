// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            this ImmutableArray<Location> locations,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            IEnumerable<Location> locationsInSource = locations.Where(l => l.IsInSource);
            if (!locationsInSource.Any())
                return Diagnostic.Create(descriptor, Location.None, args);

            return Diagnostic.Create(
                descriptor,
                location: locationsInSource.First(),
                additionalLocations: locationsInSource.Skip(1),
                messageArgs: args);
        }

        public static Diagnostic CreateDiagnostic(
            this ImmutableArray<Location> locations,
            DiagnosticDescriptor descriptor,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            IEnumerable<Location> locationsInSource = locations.Where(l => l.IsInSource);
            if (!locationsInSource.Any())
                return Diagnostic.Create(descriptor, Location.None, args);

            return Diagnostic.Create(
                descriptor,
                location: locationsInSource.First(),
                additionalLocations: locationsInSource.Skip(1),
                properties: properties,
                messageArgs: args);
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
    }


    public interface IGeneratorDiagnostics
    {
        /// <summary>
        /// Report diagnostic for marshalling of a parameter/return that is not supported
        /// </summary>
        /// <param name="method">Method with the parameter/return</param>
        /// <param name="info">Type info for the parameter/return</param>
        /// <param name="notSupportedDetails">[Optional] Specific reason for lack of support</param>
        void ReportMarshallingNotSupported(
            MethodDeclarationSyntax method,
            TypePositionInfo info,
            string? notSupportedDetails);

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
}
