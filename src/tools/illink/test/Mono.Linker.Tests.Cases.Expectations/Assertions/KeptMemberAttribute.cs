// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
	public sealed class KeptMemberAttribute : KeptAttribute
	{

		public KeptMemberAttribute (string name)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (name));
		}
	}
}