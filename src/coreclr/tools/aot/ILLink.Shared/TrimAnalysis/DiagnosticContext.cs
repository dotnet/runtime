// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	public readonly partial struct DiagnosticContext
	{
		/// <summary>
		/// The diagnostic context may be entirely disabled or some kinds of warnings may be suppressed.
		/// The suppressions are determined based on the <paramref name="id"/>.
		/// Typically the suppressions will be based on diagnostic category <see cref="DiagnosticCategory"/>:
		///  - Trimmer warnings (suppressed by RequiresUnreferencedCodeAttribute)
		///  - AOT warnings (suppressed by RequiresDynamicCodeAttribute)
		///  - Single-file warnings (suppressed by RequiresAssemblyFilesAttribute)
		/// Note that not all categories are used/supported by all tools, for example the ILLink only handles trimmer warnings and ignores the rest.
		/// </summary>
		/// <param name="id">The diagnostic ID, this will be used to determine the category of diagnostic (trimmer, AOT, single-file)</param>
		/// <param name="args">The arguments for diagnostic message.</param>
		public partial void AddDiagnostic (DiagnosticId id, params string[] args);
	}
}
