// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class HasRuntimeInterfaceAttribute : Attribute
	{
		public HasRuntimeInterfaceAttribute (Type interfaceType, params Type[] implementationChains)
		{
		}

		public HasRuntimeInterfaceAttribute (string interfaceType, params string[] implementationChains)
		{
		}
	}
}
