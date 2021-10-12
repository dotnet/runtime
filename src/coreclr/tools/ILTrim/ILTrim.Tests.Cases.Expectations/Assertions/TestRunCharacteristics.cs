// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[Flags]
	public enum TestRunCharacteristics
	{
		TargetingNetFramework = 1,
		TargetingNetCore = 2,
		SupportsDefaultInterfaceMethods = 8,
		SupportsStaticInterfaceMethods = 16,
		TestFrameworkSupportsMcs = 32
	}
}