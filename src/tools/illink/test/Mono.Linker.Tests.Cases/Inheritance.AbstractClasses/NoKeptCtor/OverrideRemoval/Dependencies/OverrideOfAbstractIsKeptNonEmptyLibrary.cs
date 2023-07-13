// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval.Dependencies
{
	public class OverrideOfAbstractIsKeptNonEmpty_UnusedType
	{
	}

	public abstract class OverrideOfAbstractIsKeptNonEmpty_BaseType
	{
		public abstract void Method ();
	}
}
