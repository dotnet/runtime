// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the ExpectedWarning attributes.
	[SkipKeptItemsValidation]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/TypeInfoCalls.il" })]

	[LogContains ("IL2070: Library.TypeInfoCalls.TestGetConstructors(TypeInfo)")]
	[LogContains ("IL2070: Library.TypeInfoCalls.TestGetMethods(TypeInfo)")]
	[LogContains ("IL2070: Library.TypeInfoCalls.TestGetFields(TypeInfo)")]
	[LogContains ("IL2070: Library.TypeInfoCalls.TestGetProperties(TypeInfo)")]
	[LogContains ("IL2070: Library.TypeInfoCalls.TestGetEvents(TypeInfo)")]
	[LogContains ("IL2070: Library.TypeInfoCalls.TestGetNestedTypes(TypeInfo)")]
	[LogContains ("IL2070: Library.TypeInfoCalls.TestGetField(TypeInfo)")]
	[LogContains ("IL2070: Library.TypeInfoCalls.TestGetProperty(TypeInfo)")]
	[LogContains ("IL2070: Library.TypeInfoCalls.TestGetEvent(TypeInfo)")]
	public class TypeInfoIntrinsics
	{
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			Library.TypeInfoCalls.TestGetConstructors(typeof(string).GetTypeInfo());
			Library.TypeInfoCalls.TestGetMethods(typeof(string).GetTypeInfo());
			Library.TypeInfoCalls.TestGetFields(typeof(string).GetTypeInfo());
			Library.TypeInfoCalls.TestGetProperties(typeof(string).GetTypeInfo());
			Library.TypeInfoCalls.TestGetEvents(typeof(string).GetTypeInfo());
			Library.TypeInfoCalls.TestGetNestedTypes(typeof(string).GetTypeInfo());
			Library.TypeInfoCalls.TestGetField(typeof(string).GetTypeInfo());
			Library.TypeInfoCalls.TestGetProperty(typeof(string).GetTypeInfo());
			Library.TypeInfoCalls.TestGetEvent(typeof(string).GetTypeInfo());
#endif
		}
	}
}
