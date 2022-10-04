// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker;

namespace ILLink.Shared.TrimAnalysis
{
	public readonly partial struct DiagnosticContext
	{
		public readonly MessageOrigin Origin;
		public readonly bool DiagnosticsEnabled;
		readonly LinkContext _context;

		public DiagnosticContext (in MessageOrigin origin, bool diagnosticsEnabled, LinkContext context)
			=> (Origin, DiagnosticsEnabled, _context) = (origin, diagnosticsEnabled, context);

		public partial void AddDiagnostic (DiagnosticId id, params string[] args)
		{
			if (DiagnosticsEnabled)
				_context.LogWarning (Origin, id, args);
		}
	}
}
