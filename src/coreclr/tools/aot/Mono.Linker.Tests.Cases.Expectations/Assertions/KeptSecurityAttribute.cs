// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class KeptSecurityAttribute : KeptAttribute
	{
		public KeptSecurityAttribute (string attributeName)
		{
			if (string.IsNullOrEmpty (attributeName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (attributeName));
		}

		public KeptSecurityAttribute (Type type)
		{
			if (type == null)
				throw new ArgumentNullException (nameof (type));
		}
	}
}
