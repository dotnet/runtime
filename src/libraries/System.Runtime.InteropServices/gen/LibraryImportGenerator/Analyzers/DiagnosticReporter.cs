// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop.Analyzers
{
    public struct DiagnosticReporter
    {
        private readonly Action<DiagnosticDescriptor, ImmutableDictionary<string, string>, object[]> _diagnosticFactory;

        public DiagnosticReporter(Action<DiagnosticDescriptor, ImmutableDictionary<string, string>, object[]> createAndReportDiagnostic)
        {
            _diagnosticFactory = createAndReportDiagnostic;
        }

        public static DiagnosticReporter CreateForLocation(Location location, Action<Diagnostic> reportDiagnostic) => new((descriptor, properties, args) => reportDiagnostic(location.CreateDiagnostic(descriptor, properties, args)));

        public void CreateAndReportDiagnostic(DiagnosticDescriptor descriptor, params object[] messageArgs) => _diagnosticFactory(descriptor, ImmutableDictionary<string, string>.Empty, messageArgs);

        public void CreateAndReportDiagnostic(DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> properties, params object[] messageArgs) => _diagnosticFactory(descriptor, properties, messageArgs);
    }
}
