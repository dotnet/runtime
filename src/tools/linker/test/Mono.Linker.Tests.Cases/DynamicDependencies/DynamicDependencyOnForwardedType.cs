// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileBefore ("FacadeAssembly.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" })]
	[SetupCompileAfter ("ImplementationLibrary.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("FacadeAssembly.dll", new[] { "Dependencies/FacadeAssembly.cs" }, new[] { "ImplementationLibrary.dll" })]
	[KeptAssembly ("FacadeAssembly.dll")]
	[LogDoesNotContain ("IL2036")]
	public class DynamicDependencyOnForwardedType
	{
		[DynamicDependency (".ctor", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.ImplementationLibrary", "FacadeAssembly")]
		[DynamicDependency (".ctor", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.ImplementationLibraryGenericType`2", "FacadeAssembly")]
		[DynamicDependency (".ctor", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.ImplementationLibrary.NestedType", "FacadeAssembly")]
		static void Main ()
		{
		}
	}
}
