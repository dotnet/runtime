// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
