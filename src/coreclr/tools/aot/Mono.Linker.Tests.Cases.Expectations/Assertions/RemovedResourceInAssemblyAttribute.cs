// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// <summary>
	/// Verifies that an embedded resource was removed from an assembly
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class RemovedResourceInAssemblyAttribute : BaseInAssemblyAttribute
	{
		public RemovedResourceInAssemblyAttribute (string assemblyFileName, string resourceName)
		{
			ArgumentException.ThrowIfNullOrEmpty (assemblyFileName);
			ArgumentException.ThrowIfNullOrEmpty (resourceName);
		}
	}
}
