// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// <summary>
	/// Verifies that name of the member is removed
	/// </summary>
	[AttributeUsage (AttributeTargets.All, AllowMultiple = false, Inherited = false)]
	public class RemovedNameValueAttribute : BaseExpectedLinkedBehaviorAttribute
	{
	}
}
