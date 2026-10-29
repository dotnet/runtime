// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.Interop;

/// <summary>Collects diagnostics produced while resolving marshalling generators.</summary>
public interface IGeneratorDiagnosticsBag
{
    /// <summary>Reports a generator diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    void ReportGeneratorDiagnostic(GeneratorDiagnostic diagnostic);

    /// <summary>Reports generator diagnostics in their original order.</summary>
    /// <param name="diagnostics">The diagnostics to report.</param>
    void ReportGeneratorDiagnostics(ImmutableArray<GeneratorDiagnostic> diagnostics);
}
