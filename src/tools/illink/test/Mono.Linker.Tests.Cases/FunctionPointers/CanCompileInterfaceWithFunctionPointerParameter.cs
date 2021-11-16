// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
