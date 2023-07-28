// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
	public class KeptDelegateCacheFieldAttribute : KeptAttribute
	{
		public KeptDelegateCacheFieldAttribute (string classIndex, string fieldName)
		{
			if (string.IsNullOrEmpty (classIndex))
				throw new ArgumentNullException (nameof (classIndex));
			if (string.IsNullOrEmpty (fieldName))
				throw new ArgumentNullException (nameof (fieldName));
		}
	}
}
