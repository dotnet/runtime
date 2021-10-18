// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// <summary>
	/// Used to specify which tool produces a warning. This can either be the trimmer, a specific analyzer, or both.
	/// Currently we have all existing diagnostic analyzers listed in here so that we can leave out some expected warnings
	/// when testing analyzers which do not produce them.
	/// </summary>
	[Flags]
	public enum ProducedBy
	{
		Trimmer = 1,
		RequiresAssemblyFileAnalyzer = 2,
		RequiresUnreferencedCodeAnalyzer = 4,
		COMAnalyzer = 8,
		Analyzer = RequiresAssemblyFileAnalyzer | RequiresUnreferencedCodeAnalyzer | COMAnalyzer,
		TrimmerAndAnalyzer = Trimmer | Analyzer
	}
}