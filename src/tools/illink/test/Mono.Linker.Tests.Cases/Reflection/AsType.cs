// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
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
