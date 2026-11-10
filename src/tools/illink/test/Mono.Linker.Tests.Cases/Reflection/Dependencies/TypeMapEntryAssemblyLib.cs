// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

[assembly: TypeMap<TypeMapEntryAssemblyGroup>("entry_type1", typeof(EntryType1))]
[assembly: TypeMap<TypeMapEntryAssemblyGroup>("entry_type2", typeof(EntryType2))]

[assembly: TypeMapAssociation<TypeMapEntryAssemblyGroup>(typeof(EntrySource1), typeof(EntryProxy1))]
[assembly: TypeMapAssociation<TypeMapEntryAssemblyGroup>(typeof(EntrySource2), typeof(EntryProxy2))]

namespace Mono.Linker.Tests.Cases.Reflection.Dependencies
{
	public class TypeMapEntryAssemblyGroup { }

	public class EntryType1 { }
	public class EntryType2 { }

	public class EntrySource1 { }
	public class EntrySource2 { }

	public class EntryProxy1 { }
	public class EntryProxy2 { }
}
