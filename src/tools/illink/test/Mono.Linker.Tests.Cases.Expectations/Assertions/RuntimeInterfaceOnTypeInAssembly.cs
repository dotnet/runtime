// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class RuntimeInterfaceOnTypeInAssembly : BaseTypeMapInfoAttribute
	{
		public RuntimeInterfaceOnTypeInAssembly (string dllName, string type, string interfaceType, params string[] implementationChains)
		{
		}
	}
}
