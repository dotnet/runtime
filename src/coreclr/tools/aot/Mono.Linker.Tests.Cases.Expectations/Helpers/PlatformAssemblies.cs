// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Tests.Cases.Expectations.Helpers
{
	public static class PlatformAssemblies
	{
#if NETCOREAPP
		public const string CoreLib = "System.Private.CoreLib.dll";
#else
		public const string CoreLib = "mscorlib.dll";
#endif
	}
}
