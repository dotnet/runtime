// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop;

/// <summary>Converts marshalling diagnostics to compiler diagnostics.</summary>
public static class GeneratorDiagnosticExtensions
{
    /// <summary>Combines a generator diagnostic with compiler-specific reporting information.</summary>
    /// <param name="diagnostic">The generator diagnostic.</param>
    /// <param name="descriptor">The compiler diagnostic descriptor.</param>
    /// <param name="location">The source location.</param>
    /// <param name="elementName">The signature element name.</param>
    /// <returns>The compiler diagnostic information.</returns>
    public static DiagnosticInfo ToDiagnosticInfo(this GeneratorDiagnostic diagnostic, DiagnosticDescriptor descriptor, Location location, string elementName)
    {
        return diagnostic switch
        {
            GeneratorDiagnostic.NotSupported notSupported => DiagnosticInfo.Create(
                descriptor,
                location,
                notSupported.DiagnosticProperties,
                notSupported.NotSupportedDetails ?? notSupported.TypePositionInfo.ManagedType.DiagnosticFormattedName,
                elementName),
            GeneratorDiagnostic.NotRecommended notRecommended => DiagnosticInfo.Create(descriptor, location, notRecommended.Details),
            _ => throw new UnreachableException()
        };
    }
}
