// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	internal static class AnalyzerOptionsExtensions
	{
		public static string? GetMSBuildPropertyValue (
			this AnalyzerOptions options,
			string optionName)
		{
			return options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue (
					$"build_property.{optionName}", out var value)
				? value
				: null;
		}

		public static bool IsMSBuildPropertyValueTrue (
			this AnalyzerOptions options,
			string propertyName)
		{
			var propertyValue = GetMSBuildPropertyValue (options, propertyName);
			if (!string.Equals (propertyValue?.Trim (), "true", System.StringComparison.OrdinalIgnoreCase))
				return false;

			return true;
		}
	}
}
