// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
	public class KeptDelegateCacheFieldAttribute : KeptAttribute
	{
		public KeptDelegateCacheFieldAttribute (string uniquePartOfName)
		{
			if (string.IsNullOrEmpty (uniquePartOfName))
				throw new ArgumentNullException (nameof (uniquePartOfName));
		}
	}
}
