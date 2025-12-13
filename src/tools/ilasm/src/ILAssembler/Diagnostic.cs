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

internal static class DiagnosticIds
{
    public const string LiteralOutOfRange = "ILA0001";
    public const string UnsealedValueType = "ILA0002";
}

internal static class DiagnosticMessageTemplates
{
    public const string LiteralOutOfRange = "The value '{0}' is out of range";
    public const string UnsealedValueType = "The value type '{0}' is unsealed; implicitly sealed.";
}
