// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[Flags]
	public enum ProducedBy
	{
		Trimmer = 1,
		Analyzer = 2,
		TrimmerAndAnalyzer = Trimmer | Analyzer
	}
}