// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker;

namespace ILLink.Shared.TrimAnalysis
{
	internal readonly partial struct DiagnosticContext
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

#pragma warning disable IDE0060, CA1822 // The details provided here are not used by illink, but they are used for example by the analyzer
		public partial void AddDiagnostic (DiagnosticId id, ValueWithDynamicallyAccessedMembers actualValue, ValueWithDynamicallyAccessedMembers expectedAnnotationsValue, params string[] args)
		{
			AddDiagnostic (id, args);
		}
#pragma warning restore IDE0060, CA1822
	}
}
