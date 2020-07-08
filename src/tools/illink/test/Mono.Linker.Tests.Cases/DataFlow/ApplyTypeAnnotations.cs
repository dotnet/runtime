// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	public class ApplyTypeAnnotations
	{
		public static void Main ()
		{
			TestFromTypeOf ();
			TestFromTypeGetTypeOverConstant ();
			TestFromStringContantWithAnnotation ();
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
	}
}
