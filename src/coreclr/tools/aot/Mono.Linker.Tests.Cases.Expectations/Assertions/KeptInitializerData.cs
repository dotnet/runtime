// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class KeptInitializerData : KeptAttribute
	{

		public KeptInitializerData ()
		{
		}

		public KeptInitializerData (int occurrenceIndexInBody)
		{
			if (occurrenceIndexInBody < 0)
				throw new ArgumentOutOfRangeException (nameof (occurrenceIndexInBody));
		}
	}
}
