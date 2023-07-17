// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SetupCompileArgument ("/langversion:7.3")]
	[SetupCompileArgument ("/unsafe")]
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

			PointerDereference.Test ();
			MultipleOutRefsToField.Test ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		static Type s_typeWithPublicParameterlessConstructor;

		[Kept]
		// Trimmer and analyzer use different formats for ref parameters: https://github.com/dotnet/linker/issues/2406
		[ExpectedWarning ("IL2077", nameof (ByRefDataflow) + "." + nameof (MethodWithRefParameter) + "(Type&)", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2077", nameof (ByRefDataflow) + "." + nameof (MethodWithRefParameter) + "(ref Type)", ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2069", nameof (s_typeWithPublicParameterlessConstructor), "parameter 'type'", nameof (MethodWithRefParameter), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		// MethodWithRefParameter (ref x)
		[ExpectedWarning ("IL2077", nameof (ByRefDataflow) + "." + nameof (MethodWithRefParameter) + "(Type&)", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2077", nameof (ByRefDataflow) + "." + nameof (MethodWithRefParameter) + "(ref Type)", ProducedBy = Tool.Analyzer)]
		public static void PassRefToField ()
		{
			MethodWithRefParameter (ref s_typeWithPublicParameterlessConstructor);
			var x = s_typeWithPublicParameterlessConstructor;
			MethodWithRefParameter (ref x);
		}

		[Kept]
		// Trimmer and analyzer use different formats for ref parameters: https://github.com/dotnet/linker/issues/2406
		[ExpectedWarning ("IL2067", nameof (ByRefDataflow) + "." + nameof (MethodWithRefParameter) + "(Type&)", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2067", nameof (ByRefDataflow) + "." + nameof (MethodWithRefParameter) + "(ref Type)", ProducedBy = Tool.Analyzer)]
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

		[Kept]
		unsafe class PointerDereference
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("")]
			static unsafe void IntPtrDeref ()
			{
				*_ptr = GetDangerous ();
			}

			[Kept]
			static IntPtr* _ptr;

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("")]
			static IntPtr GetDangerous () { return IntPtr.Zero; }

			[Kept]
			[ExpectedWarning ("IL2070")]
			static unsafe void LocalStackAllocDeref (Type t)
			{
				// Code pattern from CoreLib which caused problems in AOT port
				// so making sure we handle this correctly (that is without failing)
				int buffSize = 256;
				byte* stackSpace = stackalloc byte[buffSize];
				byte* buffer = stackSpace;

				byte* toFree = buffer;
				buffer = null;

				// IL2070 - this is to make sure that DataFlow ran on the method's body
				t.GetProperties ();
			}

			[Kept]
			[ExpectedWarning ("IL2026")]
			public static void Test ()
			{
				IntPtrDeref ();
				LocalStackAllocDeref (null);
			}
		}

		[Kept]
		class MultipleOutRefsToField
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			static Type _publicMethodsField;

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			static Type _publicPropertiesField;

			[Kept]
			static void TwoOutRefs (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				out Type publicMethods,
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
				out Type publicProperties)
			{
				publicMethods = null;
				publicProperties = null;
			}

			[Kept]
			// https://github.com/dotnet/runtime/issues/85464
			[ExpectedWarning ("IL2069", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2069", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			public static void Test ()
			{
				TwoOutRefs (out _publicMethodsField, out _publicPropertiesField);
			}
		}
	}
}
