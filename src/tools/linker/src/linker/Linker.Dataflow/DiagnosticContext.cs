// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Linker;

namespace ILLink.Shared.TrimAnalysis
{
	readonly partial struct DiagnosticContext
	{
		public readonly MessageOrigin Origin;
		public readonly bool DiagnosticsEnabled;
		readonly LinkContext _context;

		public DiagnosticContext (in MessageOrigin origin, bool diagnosticsEnabled, LinkContext context)
			=> (Origin, DiagnosticsEnabled, _context) = (origin, diagnosticsEnabled, context);

		public partial void ReportDiagnostic (DiagnosticId id, params string[] args)
		{
			if (DiagnosticsEnabled)
				_context.LogWarning (new DiagnosticString (id).GetMessage (args), (int) id, Origin, MessageSubCategory.TrimAnalysis);
		}
	}
}
