// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static class DiagnosticDescriptors
	{
		public static DiagnosticDescriptor GetDiagnosticDescriptor (DiagnosticId diagnosticId)
		{
			var diagnosticString = new DiagnosticString (diagnosticId);
			return new DiagnosticDescriptor (diagnosticId.AsString (),
				diagnosticString.GetTitleFormat (),
				diagnosticString.GetMessageFormat (),
				diagnosticId.GetDiagnosticCategory (),
				DiagnosticSeverity.Warning,
				true);
		}

		public static DiagnosticDescriptor GetDiagnosticDescriptor (DiagnosticId diagnosticId, DiagnosticString diagnosticString)
			=> new DiagnosticDescriptor (diagnosticId.AsString (),
				diagnosticString.GetTitle (),
				diagnosticString.GetMessage (),
				diagnosticId.GetDiagnosticCategory (),
				DiagnosticSeverity.Warning,
				true);

		public static DiagnosticDescriptor GetDiagnosticDescriptor (DiagnosticId diagnosticId,
			LocalizableResourceString? lrsTitle = null,
			LocalizableResourceString? lrsMessage = null,
			string? diagnosticCategory = null,
			DiagnosticSeverity diagnosticSeverity = DiagnosticSeverity.Warning,
			bool isEnabledByDefault = true,
			string? helpLinkUri = null)
		{
			if (lrsTitle == null || lrsMessage == null) {
				var diagnosticString = new DiagnosticString (diagnosticId);
				return new DiagnosticDescriptor (diagnosticId.AsString (),
					diagnosticString.GetTitleFormat (),
					diagnosticString.GetMessageFormat (),
					diagnosticCategory ?? diagnosticId.GetDiagnosticCategory (),
					diagnosticSeverity,
					isEnabledByDefault,
					helpLinkUri);
			}

			return new DiagnosticDescriptor (diagnosticId.AsString (),
				lrsTitle!,
				lrsMessage!,
				diagnosticCategory ?? diagnosticId.GetDiagnosticCategory (),
				diagnosticSeverity,
				isEnabledByDefault,
				helpLinkUri);
		}
	}
}
