// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
				GetDiagnosticCategory (diagnosticId),
				DiagnosticSeverity.Warning,
				true);
		}

		public static DiagnosticDescriptor GetDiagnosticDescriptor (DiagnosticId diagnosticId, DiagnosticString diagnosticString)
			=> new DiagnosticDescriptor (diagnosticId.AsString (),
				diagnosticString.GetTitle (),
				diagnosticString.GetMessage (),
				GetDiagnosticCategory (diagnosticId),
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
					diagnosticCategory ?? GetDiagnosticCategory (diagnosticId),
					diagnosticSeverity,
					isEnabledByDefault,
					helpLinkUri);
			}

			return new DiagnosticDescriptor (diagnosticId.AsString (),
				lrsTitle!,
				lrsMessage!,
				diagnosticCategory ?? GetDiagnosticCategory (diagnosticId),
				diagnosticSeverity,
				isEnabledByDefault,
				helpLinkUri);
		}

		static string GetDiagnosticCategory (DiagnosticId diagnosticId)
		{
			switch ((int) diagnosticId) {
			case > 2000 and < 3000:
				return DiagnosticCategory.Trimming;

			case >= 3000 and < 3050:
				return DiagnosticCategory.SingleFile;

			case >= 3050 and <= 6000:
				return DiagnosticCategory.AOT;

			default:
				break;
			}

			throw new ArgumentException ($"The provided diagnostic id '{diagnosticId}' does not fall into the range of supported warning codes 2001 to 6000 (inclusive).");
		}
	}
}
