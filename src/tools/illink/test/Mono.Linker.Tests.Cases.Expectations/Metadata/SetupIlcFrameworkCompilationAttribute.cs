// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	/// <summary>
	/// In ILC/AOT tests by default only compile the test itself (and its compiled dependencies) through the ILC compiler
	/// this means that by default none of the framework assemblies are compiled.
	/// Adding this attribute modifies the runner to compile all framework assemblies as well.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupIlcWholeProgramAnalysisAttribute : BaseMetadataAttribute
	{
	}
}
