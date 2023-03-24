// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
