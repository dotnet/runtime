// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// A base class for attributes that make assertions about a particular member.
	// The test infrastructure is expected to check the assertion on the member to which
	// the attribute is applied.
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Delegate, AllowMultiple = true)]
	public abstract class BaseMemberAssertionAttribute : Attribute
	{
	}
}