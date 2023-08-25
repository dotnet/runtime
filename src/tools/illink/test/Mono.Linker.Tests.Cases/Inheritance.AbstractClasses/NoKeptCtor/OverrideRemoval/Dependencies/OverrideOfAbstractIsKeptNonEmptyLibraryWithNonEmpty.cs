// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval.Dependencies
{
	public class OverrideOfAbstractIsKeptNonEmptyLibraryWithNonEmpty :
		OverrideOfAbstractIsKeptNonEmpty_BaseType
	{
		Dependencies.OverrideOfAbstractIsKeptNonEmpty_UnusedType _field;

		public override void Method ()
		{
			_field = null;
		}
	}
}
