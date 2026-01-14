// Licensed to the .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCompileBefore("TypeMapEntryAssemblyLib.dll", new[] { "Dependencies/TypeMapEntryAssemblyLib.cs" })]
	[SetupLinkerArgument("--typemap-entry-assembly", "TypeMapEntryAssemblyLib")]

	// The TypeMapEntryAssemblyGroup is defined in TypeMapEntryAssemblyLib, so its attributes should be kept
	[KeptTypeInAssembly("TypeMapEntryAssemblyLib.dll", typeof(TypeMapEntryAssemblyGroup))]
	[KeptTypeInAssembly("TypeMapEntryAssemblyLib.dll", typeof(EntryType1))]
	[KeptTypeInAssembly("TypeMapEntryAssemblyLib.dll", typeof(EntryType2))]
	[KeptTypeInAssembly("TypeMapEntryAssemblyLib.dll", typeof(EntrySource1))]
	[KeptTypeInAssembly("TypeMapEntryAssemblyLib.dll", typeof(EntrySource2))]
	[KeptTypeInAssembly("TypeMapEntryAssemblyLib.dll", typeof(EntryProxy1))]
	[KeptTypeInAssembly("TypeMapEntryAssemblyLib.dll", typeof(EntryProxy2))]

	[KeptAttributeInAssembly("TypeMapEntryAssemblyLib.dll", typeof(TypeMapAttribute<TypeMapEntryAssemblyGroup>))]
	[KeptAttributeInAssembly("TypeMapEntryAssemblyLib.dll", typeof(TypeMapAssociationAttribute<TypeMapEntryAssemblyGroup>))]

	public class TypeMapEntryAssembly
	{
		public static void Main()
		{
			// Access the type map to ensure it gets used
			var externalMap = TypeMapping.GetOrCreateExternalTypeMapping<TypeMapEntryAssemblyGroup>();
			var proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<TypeMapEntryAssemblyGroup>();

			Console.WriteLine(externalMap);
			Console.WriteLine(proxyMap);
			_ = new EntrySource1();
			_ = new EntrySource2();
		}
	}
}
