// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
		[UnrecognizedReflectionAccessPattern (typeof (ByRefDataflow), nameof (MethodWithRefParameter), new string[] { "Type&" }, messageCode: "IL2077")]
		public static void PassRefToField ()
		{
			MethodWithRefParameter (ref s_typeWithPublicParameterlessConstructor);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (ByRefDataflow), nameof (MethodWithRefParameter), new string[] { "Type&" }, messageCode: "IL2067")]
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
