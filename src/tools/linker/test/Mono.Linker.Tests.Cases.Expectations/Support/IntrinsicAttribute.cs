// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.CompilerServices
{
	// This attribute is normally implemented in CoreLib as internal, but in order to test
	// linker behavior around it, we need to be able to use it in the tests.
	[AttributeUsage (AttributeTargets.Method)]
	public sealed class IntrinsicAttribute : Attribute
	{
	}
}
