// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace ILAssembler;

public enum DiagnosticSeverity
{
    Error,
    Warning,
    Info,
    Hidden
}

public record Diagnostic(string Id, DiagnosticSeverity Severity, string Message, Location Location);
