// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	internal static class AnalyzerOptionsExtensions
	{
		public static string? GetMSBuildPropertyValue (
			this AnalyzerOptions options,
			string optionName,
			Compilation compilation)
		{
			// MSBuild property values should be set at compilation level, and cannot have different values per-tree.
			// So, we default to first syntax tree.
			var tree = compilation.SyntaxTrees.FirstOrDefault ();
			if (tree is null) {
				return null;
			}

			return options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue (
					$"build_property.{optionName}", out var value)
				? value
				: null;
		}

		public static bool IsMSBuildPropertyValueTrue (
			this AnalyzerOptions options,
			string propertyName,
			Compilation compilation)
		{
			var propertyValue = GetMSBuildPropertyValue (options, propertyName, compilation);
			if (!string.Equals (propertyValue?.Trim (), "true", System.StringComparison.OrdinalIgnoreCase))
				return false;

			return true;
		}
	}
}
