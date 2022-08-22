// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
		Analyzer = 2,
		NativeAot = 4,
		TrimmerAnalyzerAndNativeAot = Trimmer | Analyzer | NativeAot
	}
}
