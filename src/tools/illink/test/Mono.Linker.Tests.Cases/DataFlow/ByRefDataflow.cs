// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SetupCompileArgument ("/langversion:7.3")]
	[Kept]
	[ExpectedNoWarnings]
	class ByRefDataflow
	{
		public static void Main ()
		{
			{
				Type t = typeof (ClassPassedToMethodTakingTypeByRef);
				MethodWithRefParameter (ref t);
			}

			{
				Type t1 = typeof (ClassMaybePassedToMethodTakingTypeByRef);
				Type t2 = typeof (OtherClassMaybePassedToMethodTakingTypeByRef);
				ref Type t = ref t1;
				if (string.Empty.Length == 0)
					t = ref t2;
				MethodWithRefParameter (ref t);
			}

			PassRefToField ();
			PassRefToParameter (null);
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		static Type s_typeWithPublicParameterlessConstructor;

		[Kept]
		// Trimmer and analyzer use different formats for ref parameters: https://github.com/dotnet/linker/issues/2406
		[ExpectedWarning ("IL2077", nameof (ByRefDataflow) + "." + nameof (MethodWithRefParameter) + "(Type&)", ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning ("IL2077", nameof (ByRefDataflow) + "." + nameof (MethodWithRefParameter) + "(ref Type)", ProducedBy = ProducedBy.Analyzer)]
		public static void PassRefToField ()
		{
			MethodWithRefParameter (ref s_typeWithPublicParameterlessConstructor);
		}

		[Kept]
		// Trimmer and analyzer use different formats for ref parameters: https://github.com/dotnet/linker/issues/2406
		[ExpectedWarning ("IL2067", nameof (ByRefDataflow) + "." + nameof (MethodWithRefParameter) + "(Type&)", ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning ("IL2067", nameof (ByRefDataflow) + "." + nameof (MethodWithRefParameter) + "(ref Type)", ProducedBy = ProducedBy.Analyzer)]
		public static void PassRefToParameter (Type parameter)
		{
			MethodWithRefParameter (ref parameter);
		}

		[Kept]
		public static void MethodWithRefParameter (
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			ref Type type)
		{
			type = typeof (ClassReturnedAsRefFromMethodTakingTypeByRef);
		}

		[Kept]
		class ClassPassedToMethodTakingTypeByRef
		{
			[Kept]
			public static void KeptMethod () { }
			internal static void RemovedMethod () { }
		}

		[Kept]
		class ClassReturnedAsRefFromMethodTakingTypeByRef
		{
			[Kept]
			public static void KeptMethod () { }
			internal static void RemovedMethod () { }
		}

		[Kept]
		class ClassMaybePassedToMethodTakingTypeByRef
		{
			[Kept]
			public static void KeptMethod () { }
			internal static void RemovedMethod () { }
		}

		[Kept]
		class OtherClassMaybePassedToMethodTakingTypeByRef
		{
			[Kept]
			public static void KeptMethod () { }
			internal static void RemovedMethod () { }
		}
	}
}
