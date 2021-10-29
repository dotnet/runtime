// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
