// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;


namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class KeptMemberInAssemblyAttribute : BaseInAssemblyAttribute
	{
		public KeptMemberInAssemblyAttribute (string assemblyFileName, Type type, params string[] memberNames)
		{
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentNullException (nameof (assemblyFileName));
			ArgumentNullException.ThrowIfNull (type);
			ArgumentNullException.ThrowIfNull (memberNames);
		}

		public KeptMemberInAssemblyAttribute (string assemblyFileName, string typeName, params string[] memberNames)
		{
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentNullException (nameof (assemblyFileName));
			ArgumentNullException.ThrowIfNull (typeName);
			ArgumentNullException.ThrowIfNull (memberNames);
		}

		public string ExpectationAssemblyName { get; set; }
	}
}
