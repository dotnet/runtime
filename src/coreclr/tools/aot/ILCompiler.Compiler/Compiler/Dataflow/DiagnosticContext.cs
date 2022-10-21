// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler;
using ILCompiler.Logging;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    public readonly partial struct DiagnosticContext
    {
        public readonly MessageOrigin Origin;
        private readonly bool _diagnosticsEnabled;
        private readonly bool _suppressTrimmerDiagnostics;
        private readonly bool _suppressAotDiagnostics;
        private readonly bool _suppressSingleFileDiagnostics;
        private readonly Logger _logger;

        public DiagnosticContext(in MessageOrigin origin, bool diagnosticsEnabled, Logger logger)
        {
            Origin = origin;
            _diagnosticsEnabled = diagnosticsEnabled;
            _suppressTrimmerDiagnostics = false;
            _suppressAotDiagnostics = false;
            _suppressSingleFileDiagnostics = false;
            _logger = logger;
        }

        public DiagnosticContext(in MessageOrigin origin, bool suppressTrimmerDiagnostics, bool suppressAotDiagnostics, bool suppressSingleFileDiagnostics, Logger logger)
        {
            Origin = origin;
            _diagnosticsEnabled = true;
            _suppressTrimmerDiagnostics = suppressTrimmerDiagnostics;
            _suppressAotDiagnostics = suppressAotDiagnostics;
            _suppressSingleFileDiagnostics = suppressSingleFileDiagnostics;
            _logger = logger;
        }

        public partial void AddDiagnostic(DiagnosticId id, params string[] args)
        {
            if (!_diagnosticsEnabled)
                return;

            string category = id.GetDiagnosticCategory();
            if (_suppressTrimmerDiagnostics && category == DiagnosticCategory.Trimming)
                return;
            if (_suppressAotDiagnostics && category == DiagnosticCategory.AOT)
                return;
            if (_suppressSingleFileDiagnostics && category == DiagnosticCategory.SingleFile)
                return;

            _logger.LogWarning(Origin, id, args);
        }
    }
}
