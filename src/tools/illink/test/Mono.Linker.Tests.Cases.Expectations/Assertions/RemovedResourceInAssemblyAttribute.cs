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
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentNullException (nameof (assemblyFileName));

			if (string.IsNullOrEmpty (resourceName))
				throw new ArgumentNullException (nameof (resourceName));
		}
	}
}
