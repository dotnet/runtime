// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
	public class ExpectedLocalsSequenceAttribute : BaseInAssemblyAttribute
	{
		public ExpectedLocalsSequenceAttribute (string[] types)
		{
			if (types == null)
				throw new ArgumentNullException (nameof (types));
		}

		public ExpectedLocalsSequenceAttribute (Type[] types)
		{
			if (types == null)
				throw new ArgumentNullException (nameof (types));
		}
	}
}