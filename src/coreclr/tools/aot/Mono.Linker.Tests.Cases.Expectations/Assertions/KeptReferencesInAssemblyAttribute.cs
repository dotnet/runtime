// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class KeptReferencesInAssemblyAttribute : BaseInAssemblyAttribute
	{
		public KeptReferencesInAssemblyAttribute (string assemblyFileName, string[] expectedReferenceAssemblyNames)
		{
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentNullException (nameof (assemblyFileName));

			if (expectedReferenceAssemblyNames == null)
				throw new ArgumentNullException (nameof (expectedReferenceAssemblyNames));
		}
	}
}
