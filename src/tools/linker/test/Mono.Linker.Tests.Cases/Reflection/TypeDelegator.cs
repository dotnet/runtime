// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class TypeDelegator
	{
		public static void Main ()
		{
			_ = new System.Reflection.TypeDelegator (typeof (TypeUsedWithDelegator)).GetMethod ("Method");
		}

		[Kept]
		static class TypeUsedWithDelegator
		{
			[Kept]
			public static void Method () { }

			public static void UnrelatedMethod () { }
		}
	}
}
