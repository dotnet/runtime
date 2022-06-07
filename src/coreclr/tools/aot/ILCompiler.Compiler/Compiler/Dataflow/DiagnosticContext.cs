// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler;
using ILCompiler.Logging;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    readonly partial struct DiagnosticContext
    {
        public readonly MessageOrigin Origin;
        public readonly bool DiagnosticsEnabled;
        readonly Logger _logger;

        public DiagnosticContext(in MessageOrigin origin, bool diagnosticsEnabled, Logger logger)
            => (Origin, DiagnosticsEnabled, _logger) = (origin, diagnosticsEnabled, logger);

        public partial void AddDiagnostic(DiagnosticId id, params string[] args)
        {
            if (DiagnosticsEnabled)
                _logger.LogWarning(Origin, id, args);
        }
    }
}
