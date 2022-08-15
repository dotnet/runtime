// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class MethodByRefParameterDataFlow
	{
		public static void Main ()
		{
			Type typeWithMethods = _fieldWithMethods;

			TestAssignStaticToAnnotatedRefParameter (ref typeWithMethods);
			TestAssignParameterToAnnotatedRefParameter (ref typeWithMethods, typeof (TestType));

			TestReadFromRefParameter ();
			TestReadFromOutParameter_PassedTwice ();
			TestReadFromRefParameter_MismatchOnOutput ();
			TestReadFromRefParameter_MismatchOnOutput_PassedTwice ();
			TestReadFromRefParameter_MismatchOnInput ();
			TestReadFromRefParameter_MismatchOnInput_PassedTwice ();
			Type nullType1 = null;
			TestPassingRefParameter (ref nullType1);
			Type nullType2 = null;
			TestPassingRefParameter_Mismatch (ref nullType2);
			Type nullType3 = null;
			TestAssigningToRefParameter (nullType3, ref nullType3);
			Type nullType4 = null;
			TestAssigningToRefParameter_Mismatch (nullType4, ref nullType4);
			TestPassingRefsWithImplicitThis ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type _fieldWithMethods = null;

		[ExpectedWarning ("IL2026", "Message for --TestType.Requires--")]

		// https://github.com/dotnet/linker/issues/2158
		// The type.GetMethods call generates a warning because we're not able to correctly track the value of the "this".
		// (there's a ldind.ref insruction here which we currently don't handle and the "this" becomes unknown)
		[ExpectedWarning ("IL2065")]
		static void TestAssignStaticToAnnotatedRefParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] ref Type type)
		{
			type = typeof (TestTypeWithRequires);
			type.GetMethods (); // Should not warn
		}

		// The warning message is REALLY confusing (basically wrong) since it talks about "calling the method with wrong argument"
		// which is definitely not the case here.
		[ExpectedWarning ("IL2067", "typeWithFields")]

		// https://github.com/dotnet/linker/issues/2158
		// The type.GetMethods call generates a warning because we're not able to correctly track the value of the "this".
		// (there's a ldind.ref insruction here which we currently don't handle and the "this" becomes unknown)
		[ExpectedWarning ("IL2065")]
		static void TestAssignParameterToAnnotatedRefParameter (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] ref Type type,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type typeWithFields)
		{
			type = typeWithFields; // Should warn
			type.GetMethods (); // Should not warn
		}

		class TestTypeWithRequires
		{
			[RequiresUnreferencedCode ("Message for --TestType.Requires--")]
			public static void Requires () { }
		}

		static void TestReadFromRefParameter ()
		{
			Type typeWithMethods = null;
			TryGetAnnotatedValue (ref typeWithMethods);
			typeWithMethods.RequiresPublicMethods ();
		}

		static void TestReadFromOutParameter_PassedTwice ()
		{
			Type typeWithMethods = null;
			TryGetAnnotatedValueFromValue (typeWithMethods, ref typeWithMethods);
			typeWithMethods.RequiresPublicMethods ();
		}

		[ExpectedWarning ("IL2067", nameof (TryGetAnnotatedValue), "RequiresPublicFields")]
		static void TestReadFromRefParameter_MismatchOnOutput ()
		{
			Type typeWithMethods = null;
			TryGetAnnotatedValue (ref typeWithMethods);
			typeWithMethods.RequiresPublicFields ();
		}

		[ExpectedWarning ("IL2067", nameof (TryGetAnnotatedValue), "RequiresPublicFields")]
		static void TestReadFromRefParameter_MismatchOnOutput_PassedTwice ()
		{
			Type typeWithMethods = null;
			TryGetAnnotatedValueFromValue (typeWithMethods, ref typeWithMethods);
			typeWithMethods.RequiresPublicFields ();
		}

		[ExpectedWarning ("IL2072", nameof (TryGetAnnotatedValue))]
		// https://github.com/dotnet/linker/issues/2632
		// This second warning should not be generated, the value of typeWithMethods should have PublicMethods
		// after the call with out parameter.
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresPublicMethods), ProducedBy = ProducedBy.Analyzer)]
		static void TestReadFromRefParameter_MismatchOnInput ()
		{
			Type typeWithMethods = GetTypeWithFields ();
			TryGetAnnotatedValue (ref typeWithMethods);
			typeWithMethods.RequiresPublicMethods ();
		}

		[ExpectedWarning ("IL2072", nameof (TryGetAnnotatedValueFromValue))]
		[ExpectedWarning ("IL2072", nameof (TryGetAnnotatedValueFromValue))]
		// https://github.com/dotnet/linker/issues/2632
		// This third warning should not be generated, the value of typeWithMethods should have PublicMethods
		// after the call with ref parameter.
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresPublicMethods), ProducedBy = ProducedBy.Analyzer)]
		static void TestReadFromRefParameter_MismatchOnInput_PassedTwice ()
		{
			Type typeWithMethods = GetTypeWithFields ();
			TryGetAnnotatedValueFromValue (typeWithMethods, ref typeWithMethods);
			typeWithMethods.RequiresPublicMethods ();
		}

		static void TestPassingRefParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] ref Type typeWithMethods)
		{
			TryGetAnnotatedValue (ref typeWithMethods);
		}

		[ExpectedWarning ("IL2067", "typeWithMethods", nameof (TryGetAnnotatedValue))]
		[ExpectedWarning ("IL2067", "typeWithMethods", nameof (TryGetAnnotatedValue))]
		static void TestPassingRefParameter_Mismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] ref Type typeWithMethods)
		{
			TryGetAnnotatedValue (ref typeWithMethods);
		}

		static void TestAssigningToRefParameter (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type inputTypeWithMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] ref Type outputTypeWithMethods)
		{
			outputTypeWithMethods = inputTypeWithMethods;
		}

		[ExpectedWarning ("IL2067", "inputTypeWithFields", "outputTypeWithMethods")]
		static void TestAssigningToRefParameter_Mismatch (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type inputTypeWithFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] ref Type outputTypeWithMethods)
		{
			outputTypeWithMethods = inputTypeWithFields;
		}

		static bool TryGetAnnotatedValue ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] ref Type typeWithMethods)
		{
			typeWithMethods = null;
			return false;
		}

		static bool TryGetAnnotatedValueFromValue (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type inValue,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] ref Type typeWithMethods)
		{
			typeWithMethods = inValue;
			return false;
		}

		static void TestPassingRefsWithImplicitThis ()
		{
			var x = new InheritsFromType ();
			var param1 = typeof (int);
			var param2 = typeof (string);
			x.MethodWithRefAndImplicitThis (ref param1, in param2, out var param3);
			param1.RequiresPublicMethods ();
			param2.RequiresAll ();
			param3.RequiresPublicFields ();
		}

		[return: DynamicallyAccessedMembersAttribute (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type GetTypeWithFields () => null;

		class TestType
		{
		}

		class InheritsFromType : Type
		{
			public void MethodWithRefAndImplicitThis ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] ref Type type1,
				in Type type2,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] out Type type3)
			{
				type3 = typeof (int);
			}
			public override Assembly Assembly => throw new NotImplementedException ();

			public override string AssemblyQualifiedName => throw new NotImplementedException ();

			public override Type BaseType => throw new NotImplementedException ();

			public override string FullName => throw new NotImplementedException ();

			public override Guid GUID => throw new NotImplementedException ();

			public override Module Module => throw new NotImplementedException ();

			public override string Namespace => throw new NotImplementedException ();

			public override Type UnderlyingSystemType => throw new NotImplementedException ();

			public override string Name => throw new NotImplementedException ();

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			public override ConstructorInfo[] GetConstructors (BindingFlags bindingAttr)
			{
				throw new NotImplementedException ();
			}

			public override object[] GetCustomAttributes (bool inherit)
			{
				throw new NotImplementedException ();
			}

			public override object[] GetCustomAttributes (Type attributeType, bool inherit)
			{
				throw new NotImplementedException ();
			}

			public override Type GetElementType ()
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
			public override EventInfo GetEvent (string name, BindingFlags bindingAttr)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
			public override EventInfo[] GetEvents (BindingFlags bindingAttr)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
			public override FieldInfo GetField (string name, BindingFlags bindingAttr)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
			public override FieldInfo[] GetFields (BindingFlags bindingAttr)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)]
			public override Type GetInterface (string name, bool ignoreCase)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)]
			public override Type[] GetInterfaces ()
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
			public override MemberInfo[] GetMembers (BindingFlags bindingAttr)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public override MethodInfo[] GetMethods (BindingFlags bindingAttr)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
			public override Type GetNestedType (string name, BindingFlags bindingAttr)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
			public override Type[] GetNestedTypes (BindingFlags bindingAttr)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicProperties)]
			public override PropertyInfo[] GetProperties (BindingFlags bindingAttr)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			public override object InvokeMember (string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
			{
				throw new NotImplementedException ();
			}

			public override bool IsDefined (Type attributeType, bool inherit)
			{
				throw new NotImplementedException ();
			}

			protected override TypeAttributes GetAttributeFlagsImpl ()
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			protected override ConstructorInfo GetConstructorImpl (BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			protected override MethodInfo GetMethodImpl (string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
			{
				throw new NotImplementedException ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
			protected override PropertyInfo GetPropertyImpl (string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
			{
				throw new NotImplementedException ();
			}

			protected override bool HasElementTypeImpl ()
			{
				throw new NotImplementedException ();
			}

			protected override bool IsArrayImpl ()
			{
				throw new NotImplementedException ();
			}

			protected override bool IsByRefImpl ()
			{
				throw new NotImplementedException ();
			}

			protected override bool IsCOMObjectImpl ()
			{
				throw new NotImplementedException ();
			}

			protected override bool IsPointerImpl ()
			{
				throw new NotImplementedException ();
			}

			protected override bool IsPrimitiveImpl ()
			{
				throw new NotImplementedException ();
			}
		}
	}
}
