// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


#if INCLUDE_FORWARDERS
[assembly: System.Runtime.CompilerServices.TypeForwardedTo (typeof (Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary))]
#endif

#if INCLUDE_REFERENCE_IMPL
namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

public class ForwardedNestedTypeLibrary
{
	public class NestedOne
	{
		public class NestedTwo
		{
			public class NestedThree
			{
			}
		}
	}
}
#endif
