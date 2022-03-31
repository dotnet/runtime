// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.FunctionPointers
{
	[SetupCompileArgument ("/unsafe")]
	unsafe class CanCompileMethodWithFunctionPointerParameter
	{
		public static void Main ()
		{
			new CanCompileMethodWithFunctionPointerParameter.B ().Method (null);
		}

		[KeptMember (".ctor()")]
		class B
		{
			public void Unused (delegate* unmanaged<void> fnptr)
			{
			}

			[Kept]
			public void Method (delegate* unmanaged<void> fnptr)
			{
			}
		}
	}
}
