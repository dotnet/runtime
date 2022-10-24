// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Linker;

namespace ILLink.Shared.TrimAnalysis
{
    public readonly partial struct DiagnosticContext
    {
        public readonly MessageOrigin Origin;
        public readonly bool DiagnosticsEnabled;
        readonly LinkContext _context;

        public DiagnosticContext(in MessageOrigin origin, bool diagnosticsEnabled, LinkContext context)
            => (Origin, DiagnosticsEnabled, _context) = (origin, diagnosticsEnabled, context);

        public partial void AddDiagnostic(DiagnosticId id, params string[] args)
        {
            if (DiagnosticsEnabled)
                _context.LogWarning(Origin, id, args);
        }

#pragma warning disable IDE0060, CA1822
        public partial void AddDiagnostic(DiagnosticId id, ValueWithDynamicallyAccessedMembers actualValue, ValueWithDynamicallyAccessedMembers expectedAnnotationsValue, params string[] args)
        {
            AddDiagnostic(id, args);
        }
#pragma warning restore IDE0060, CA1822
    }
}
