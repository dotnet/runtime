// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
{
#if METHOD
	public class DynamicDependencyFromAttributeXmlOnNonReferencedAssemblyLibrary_Method
#else
	public class DynamicDependencyFromAttributeXmlOnNonReferencedAssemblyLibrary_Field
#endif
	{
		public static void Method ()
		{
		}
	}
}
