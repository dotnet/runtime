// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	readonly partial struct DiagnosticContext
	{
		public List<Diagnostic> Diagnostics { get; } = new ();

		readonly Location? Location { get; init; }

		public DiagnosticContext (Location location)
		{
			Location = location;
		}

		public static DiagnosticContext CreateDisabled () => new () { Location = null };

		public partial void AddDiagnostic (DiagnosticId id, params string[] args)
		{
			if (Location == null)
				return;

			Diagnostics.Add (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, args));
		}
	}
}
