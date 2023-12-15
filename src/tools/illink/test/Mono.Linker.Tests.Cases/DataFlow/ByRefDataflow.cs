// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
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
			MultipleRefCaptures.Test ();
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

		[Kept]
		class MultipleRefCaptures
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			static Type _publicMethodsField;

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			static Type _publicPropertiesField;

			static Type Prop { get; set; }

			[Kept]
			[ExpectedWarning ("IL2074", nameof (_publicMethodsField), nameof (GetUnknownType))]
			[ExpectedWarning ("IL2074", nameof (_publicPropertiesField), nameof (GetUnknownType))]
			static void TestFieldAssignment (bool b = true)
			{
				(b ? ref _publicMethodsField : ref _publicPropertiesField) = GetUnknownType ();
			}

			[Kept]
			[ExpectedWarning ("IL2072", nameof (publicMethodsParameter), nameof (GetUnknownType))]
			[ExpectedWarning ("IL2072", nameof (publicPropertiesParameter), nameof (GetUnknownType))]
			static void TestParameterAssignment (
				bool b = true,
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type publicMethodsParameter = null,
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
				Type publicPropertiesParameter = null)
			{
				(b ? ref publicMethodsParameter : ref publicPropertiesParameter) = GetUnknownType ();
			}

			[Kept]
			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void TestLocalAssignment (bool b = true)
			{
				var local1 = GetUnknownType ();
				var local2 = GetTypeWithPublicFields ();
				(b ? ref local1 : ref local2) = GetTypeWithPublicConstructors ();
				local1.RequiresAll ();
				local2.RequiresAll ();
			}

			[Kept]
			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll), ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), nameof (DataFlowTypeExtensions.RequiresAll), ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll), ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll), ProducedBy = Tool.Analyzer)]
			// ILLink/ILCompiler produce different warning code: https://github.com/dotnet/linker/issues/2737
			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void TestArrayElementReferenceAssignment (bool b = true)
			{
				var arr1 = new Type[] { GetUnknownType () };
				var arr2 = new Type[] { GetTypeWithPublicConstructors () };
				(b ? ref arr1[0] : ref arr2[0]) = GetTypeWithPublicFields ();
				arr1[0].RequiresAll ();
				arr2[0].RequiresAll ();
			}

			[Kept]
			[ExpectedWarning ("IL2074", nameof (_publicMethodsField), nameof (GetUnknownType))]
			[ExpectedWarning ("IL2074", nameof (_publicPropertiesField), nameof (GetUnknownType))]
			static void TestNullCoalescingAssignment (bool b = true)
			{
				(b ? ref _publicMethodsField : ref _publicPropertiesField) ??= GetUnknownType ();
			}

			[Kept]
			[ExpectedWarning ("IL2074", nameof (_publicMethodsField), nameof (GetUnknownType))]
			[ExpectedWarning ("IL2074", nameof (_publicMethodsField), nameof (GetTypeWithPublicConstructors))]
			[ExpectedWarning ("IL2074", nameof (_publicPropertiesField), nameof (GetUnknownType))]
			[ExpectedWarning ("IL2074", nameof (_publicPropertiesField), nameof (GetTypeWithPublicConstructors))]
			static void TestNullCoalescingAssignmentComplex (bool b = true)
			{
				(b ? ref _publicMethodsField : ref _publicPropertiesField) ??= GetUnknownType () ?? GetTypeWithPublicConstructors ();
			}

			[Kept]
			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DynamicallyAccessedMemberTypes.PublicConstructors), nameof (type))]
			[ExpectedWarning ("IL2074", nameof (_publicMethodsField), nameof (GetUnknownType))]
			[ExpectedWarning ("IL2074", nameof (_publicPropertiesField), nameof (GetUnknownType))]
			static void TestDataFlowOnRightHandOfAssignment (
				bool b = true,
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type type = null)
			{
				(b ? ref _publicMethodsField : ref _publicPropertiesField) = (type = GetUnknownType ());
			}

			[Kept]
			[ExpectedWarning ("IL2074", nameof (_publicMethodsField), nameof (GetUnknownType))]
			[ExpectedWarning ("IL2074", nameof (_publicPropertiesField), nameof (GetUnknownType))]
			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void TestReturnValue (bool b = true)
			{
				var value = (b ? ref _publicMethodsField : ref _publicPropertiesField) = GetUnknownType ();
				value.RequiresAll ();
			}

			[Kept]
			public static void Test ()
			{
				TestFieldAssignment ();
				TestParameterAssignment ();
				TestLocalAssignment ();
				TestArrayElementReferenceAssignment ();
				TestNullCoalescingAssignment ();
				TestNullCoalescingAssignmentComplex ();
				TestDataFlowOnRightHandOfAssignment ();
				TestReturnValue ();
			}
		}

		[Kept]
		static Type GetUnknownType () => null;

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		static Type GetTypeWithPublicConstructors () => null;

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type GetTypeWithPublicFields () => null;
	}
}
