// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	public class ApplyTypeAnnotations
	{
		public static void Main ()
		{
			TestFromTypeOf ();
			TestFromTypeGetTypeOverConstant ();
			TestFromStringContantWithAnnotation ();
			TestFromStringConstantWithGeneric ();
			TestFromStringConstantWithGenericAndAssemblyQualified ();
			TestFromStringConstantWithGenericAndAssemblyQualifiedInvalidAssembly ();
			TestFromStringConstantWithGenericAndAssemblyQualifiedNonExistingAssembly ();
		}

		[Kept]
		static void TestFromTypeOf ()
		{
			RequireCombination (typeof (FromTypeOfTestType));
		}

		[Kept]
		class FromTypeOfTestType
		{
			[Kept]
			public FromTypeOfTestType () { }
			public FromTypeOfTestType (int i) { }

			[Kept]
			public void PublicMethod () { }
			private void PrivateMethod () { }

			[Kept]
			public bool _publicField;
			private bool _privateField;

			[Kept]
			[KeptBackingField]
			public bool PublicProperty { [Kept] get; [Kept] set; }
			private bool PrivateProperty { get; set; }
		}

		[Kept]
		static void TestFromTypeGetTypeOverConstant ()
		{
			RequireCombination (Type.GetType ("Mono.Linker.Tests.Cases.DataFlow.ApplyTypeAnnotations+FromTypeGetTypeOverConstantTestType"));
		}

		[Kept]
		class FromTypeGetTypeOverConstantTestType
		{
			[Kept]
			public FromTypeGetTypeOverConstantTestType () { }
			public FromTypeGetTypeOverConstantTestType (int i) { }

			[Kept]
			public void PublicMethod () { }
			private void PrivateMethod () { }

			[Kept]
			public bool _publicField;
			private bool _privateField;

			[Kept]
			[KeptBackingField]
			public bool PublicProperty { [Kept] get; [Kept] set; }
			private bool PrivateProperty { get; set; }
		}

		[Kept]
		static void TestFromStringContantWithAnnotation ()
		{
			RequireCombinationOnString ("Mono.Linker.Tests.Cases.DataFlow.ApplyTypeAnnotations+FromStringConstantWithAnnotationTestType");
		}

		[Kept]
		class FromStringConstantWithAnnotationTestType
		{
			[Kept]
			public FromStringConstantWithAnnotationTestType () { }
			public FromStringConstantWithAnnotationTestType (int i) { }

			[Kept]
			public void PublicMethod () { }
			private void PrivateMethod () { }

			[Kept]
			public bool _publicField;
			private bool _privateField;

			[Kept]
			[KeptBackingField]
			public bool PublicProperty { [Kept] get; [Kept] set; }
			private bool PrivateProperty { get; set; }
		}

		[Kept]
		private static void RequireCombination (
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers(
				DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
				DynamicallyAccessedMemberTypes.PublicFields |
				DynamicallyAccessedMemberTypes.PublicMethods |
				DynamicallyAccessedMemberTypes.PublicProperties)]
			Type type)
		{
		}

		[Kept]
		private static void RequireCombinationOnString (
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers(
				DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
				DynamicallyAccessedMemberTypes.PublicFields |
				DynamicallyAccessedMemberTypes.PublicMethods |
				DynamicallyAccessedMemberTypes.PublicProperties)]
			string typeName)
		{
		}

		// Issue: https://github.com/mono/linker/issues/1537
		//[Kept]
		//[KeptMember (".ctor()")]
		class FromStringConstantWithGenericInner
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class FromStringConstantWithGeneric<T>
		{
			[Kept]
			public T GetValue () { return default (T); }
		}

		[Kept]
		static void TestFromStringConstantWithGeneric ()
		{
			RequireCombinationOnString ("Mono.Linker.Tests.Cases.DataFlow.ApplyTypeAnnotations+FromStringConstantWithGeneric`1[[Mono.Linker.Tests.Cases.DataFlow.ApplyTypeAnnotations+FromStringConstantWithGenericInner]]");
		}

		[Kept]
		[KeptMember (".ctor()")]
		class FromStringConstantWithGenericAndAssemblyQualified<T>
		{
			[Kept]
			public T GetValue () { return default (T); }
		}

		[Kept]
		// This is a workaround for the inability to lazy load assemblies. The type name resolver will not load new assemblies
		// and since the KeptAttribute is otherwise not referenced by the test anywhere (the test-validation attributes are removed before processing normally)
		// it would not resolve from name - since its assembly is not loaded.
		// Adding DynamicDependency solves this problem as it is basically the only attribute which has the ability to load new assemblies.
		[DynamicDependency (DynamicallyAccessedMemberTypes.None, typeof (KeptAttribute))]
		static void TestFromStringConstantWithGenericAndAssemblyQualified ()
		{
			RequireCombinationOnString ("Mono.Linker.Tests.Cases.DataFlow.ApplyTypeAnnotations+FromStringConstantWithGenericAndAssemblyQualified`1[[Mono.Linker.Tests.Cases.Expectations.Assertions.KeptAttribute,Mono.Linker.Tests.Cases.Expectations]]");
		}

		class InvalidAssemblyNameType
		{
		}

		[Kept]
		static void TestFromStringConstantWithGenericAndAssemblyQualifiedInvalidAssembly ()
		{
			RequireCombinationOnString ("Mono.Linker.Tests.Cases.DataFlow.ApplyTypeAnnotations+InvalidAssemblyNameType,Invalid/Assembly/Name");
		}

		class NonExistingAssemblyType
		{
		}

		[Kept]
		static void TestFromStringConstantWithGenericAndAssemblyQualifiedNonExistingAssembly ()
		{
			RequireCombinationOnString ("Mono.Linker.Tests.Cases.DataFlow.ApplyTypeAnnotations+InvalidAssemblyNameType,NonExistingAssembly");
		}
	}
}
