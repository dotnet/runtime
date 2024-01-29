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
}
