// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
				true,
				customTags: WellKnownDiagnosticTags.NotConfigurable);
		}

		public static DiagnosticDescriptor GetDiagnosticDescriptor (DiagnosticId diagnosticId, DiagnosticString diagnosticString)
			=> new DiagnosticDescriptor (diagnosticId.AsString (),
				diagnosticString.GetTitle (),
				diagnosticString.GetMessage (),
				GetDiagnosticCategory (diagnosticId),
				DiagnosticSeverity.Warning,
				true,
				customTags: WellKnownDiagnosticTags.NotConfigurable);

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
					helpLinkUri,
					customTags: WellKnownDiagnosticTags.NotConfigurable);
			}

			return new DiagnosticDescriptor (diagnosticId.AsString (),
				lrsTitle!,
				lrsMessage!,
				diagnosticCategory ?? GetDiagnosticCategory (diagnosticId),
				diagnosticSeverity,
				isEnabledByDefault,
				helpLinkUri,
				customTags: WellKnownDiagnosticTags.NotConfigurable);
		}

		static string GetDiagnosticCategory (DiagnosticId diagnosticId)
		{
			switch ((int) diagnosticId) {
			case > 2000 and < 3000:
				return DiagnosticCategory.Trimming;

			case >= 3000 and <= 6000:
				return DiagnosticCategory.SingleFile;

			default:
				break;
			}

			throw new ArgumentException ($"The provided diagnostic id '{diagnosticId}' does not fall into the range of supported warning codes 2001 to 6000 (inclusive).");
		}
	}
}
