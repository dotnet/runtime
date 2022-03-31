// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.FunctionPointers
{
	[SetupCompileArgument ("/unsafe")]
	unsafe class CanCompileInterfaceWithFunctionPointerParameter
	{
		public static void Main ()
		{
			new CanCompileInterfaceWithFunctionPointerParameter.B ().Method (null);
		}

		[KeptMember (".ctor()")]
		class B : I
		{
			public void Unused (delegate* unmanaged<void> fnptr)
			{
			}

			[Kept]
			public void Method (delegate* unmanaged<void> fnptr)
			{
			}
		}

		interface I
		{
			void Unused (delegate* unmanaged<void> fnptr);

			void Method (delegate* unmanaged<void> fnptr);
		}
	}
}
