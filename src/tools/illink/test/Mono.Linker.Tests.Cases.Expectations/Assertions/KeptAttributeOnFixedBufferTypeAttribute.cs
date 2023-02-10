// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
	public class KeptAttributeOnFixedBufferTypeAttribute : KeptAttribute
	{
		public KeptAttributeOnFixedBufferTypeAttribute (string attributeName)
		{
			if (string.IsNullOrEmpty (attributeName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (attributeName));
		}

		public KeptAttributeOnFixedBufferTypeAttribute (Type type)
		{
			ArgumentNullException.ThrowIfNull (type);
		}
	}
}