// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
	// This attribute is normally implemented in CoreLib as internal, but in order to test
	// linker behavior around it, we need to be able to use it in the tests.
	[AttributeUsage (AttributeTargets.Method)]
	public sealed class IntrinsicAttribute : Attribute
	{
	}
}
