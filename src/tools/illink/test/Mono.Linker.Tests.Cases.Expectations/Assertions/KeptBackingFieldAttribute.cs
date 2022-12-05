// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false, Inherited = false)]
	public sealed class KeptBackingFieldAttribute : KeptAttribute
	{
	}
}
