// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Event, Inherited = false, AllowMultiple = false)]
	public class KeptEventAddMethodAttribute : KeptAttribute
	{
	}
}
