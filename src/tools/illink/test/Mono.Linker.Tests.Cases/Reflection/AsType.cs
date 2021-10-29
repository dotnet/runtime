// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	class AsType
	{
		public static void Main ()
		{
			_ = typeof (TypeUsedWithAsType).GetTypeInfo ().AsType ().GetMethod (nameof (TypeUsedWithAsType.Method));
		}

		[Kept]
		static class TypeUsedWithAsType
		{
			[Kept]
			public static void Method () { }

			public static void OtherMethod () { }
		}
	}
}
