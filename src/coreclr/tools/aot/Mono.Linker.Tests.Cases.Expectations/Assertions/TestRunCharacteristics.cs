// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
