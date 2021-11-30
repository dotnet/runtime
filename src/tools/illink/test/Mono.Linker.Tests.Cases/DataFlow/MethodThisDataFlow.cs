// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SandboxDependency ("Dependencies/TestSystemTypeBase.cs")]
	public class MethodThisDataFlow
	{
		public static void Main ()
		{
			new MethodThisDataFlowTypeTest ();

			PropagateToThis ();
			PropagateToThisWithGetters ();
			PropagateToThisWithSetters ();

			TestAnnotationOnNonTypeMethod ();
			TestUnknownThis ();
			TestFromParameterToThis (null);
			TestFromFieldToThis ();
			TestFromThisToOthers ();
			TestFromGenericParameterToThis<MethodThisDataFlow> ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods), new Type[] { },
			messageCode: "IL2075", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()",
				"System.MethodThisDataFlowTypeTest.RequireThisPublicMethods()" })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.RequireThisNonPublicMethods), new Type[] { }, messageCode: "IL2075")]
		static void PropagateToThis ()
		{
			GetWithPublicMethods ().RequireThisPublicMethods ();
			GetWithNonPublicMethods ().RequireThisPublicMethods ();

			GetWithPublicMethods ().RequireThisNonPublicMethods ();
			GetWithNonPublicMethods ().RequireThisNonPublicMethods ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.PropertyRequireThisPublicMethods) + ".get", new Type[] { },
			messageCode: "IL2075", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()",
				"System.MethodThisDataFlowTypeTest.PropertyRequireThisPublicMethods.get" })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.PropertyRequireThisNonPublicMethods) + ".get", new Type[] { }, messageCode: "IL2075")]
		static void PropagateToThisWithGetters ()
		{
			_ = GetWithPublicMethods ().PropertyRequireThisPublicMethods;
			_ = GetWithNonPublicMethods ().PropertyRequireThisPublicMethods;

			_ = GetWithPublicMethods ().PropertyRequireThisNonPublicMethods;
			_ = GetWithNonPublicMethods ().PropertyRequireThisNonPublicMethods;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.PropertyRequireThisPublicMethods) + ".set", new Type[] { typeof (Object) },
			messageCode: "IL2075", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()",
				"System.MethodThisDataFlowTypeTest.PropertyRequireThisPublicMethods.set" })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.PropertyRequireThisNonPublicMethods) + ".set", new Type[] { typeof (Object) }, messageCode: "IL2075")]
		static void PropagateToThisWithSetters ()
		{
			GetWithPublicMethods ().PropertyRequireThisPublicMethods = null;
			GetWithNonPublicMethods ().PropertyRequireThisPublicMethods = null;
			GetWithPublicMethods ().PropertyRequireThisNonPublicMethods = null;
			GetWithNonPublicMethods ().PropertyRequireThisNonPublicMethods = null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static MethodThisDataFlowTypeTest GetWithPublicMethods ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
		static MethodThisDataFlowTypeTest GetWithNonPublicMethods ()
		{
			return null;
		}

		[RecognizedReflectionAccessPattern]
		static void TestAnnotationOnNonTypeMethod ()
		{
			var t = new NonTypeType ();
			t.GetMethod ("foo");
			NonTypeType.StaticMethod ();
		}
		// Analyer doesn't warn about unknown values flowing into annotated locations
		// https://github.com/dotnet/linker/issues/2273
		[ExpectedWarning ("IL2065", nameof (MethodThisDataFlowTypeTest) + "." + nameof (MethodThisDataFlowTypeTest.RequireThisNonPublicMethods), "'this'",
			ProducedBy = ProducedBy.Trimmer)]
		static void TestUnknownThis ()
		{
			var array = new object[1];
			array[0] = array.GetType ();
			MakeArrayValuesUnknown (array);
			((MethodThisDataFlowTypeTest) array[0]).RequireThisNonPublicMethods ();

			static void MakeArrayValuesUnknown (object[] array)
			{
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods), new Type[] { },
			messageCode: "IL2070", message: new string[] { "sourceType", nameof (TestFromParameterToThis), nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods) })]
		static void TestFromParameterToThis (MethodThisDataFlowTypeTest sourceType)
		{
			sourceType.RequireThisPublicMethods ();
		}

		static MethodThisDataFlowTypeTest _typeField;

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods), new Type[] { },
			messageCode: "IL2080", message: new string[] { nameof (_typeField), nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods) })]
		static void TestFromFieldToThis ()
		{
			_typeField.RequireThisPublicMethods ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods), new Type[] { },
			messageCode: "IL2090", message: new string[] {
				"TSource",
				"TestFromGenericParameterToThis<TSource>",
				nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods)
			})]
		static void TestFromGenericParameterToThis<TSource> ()
		{
			((MethodThisDataFlowTypeTest) typeof (TSource)).RequireThisPublicMethods ();
		}

		static void TestFromThisToOthers ()
		{
			GetWithPublicMethods ().PropagateToReturn ();
			GetWithPublicMethods ().PropagateToField ();
			GetWithPublicMethods ().PropagateToThis ();
		}

		class NonTypeType
		{
			// Analyzer doesn't warn about annotations on unsupported types:
			// https://github.com/dotnet/linker/issues/2273
			[ExpectedWarning ("IL2041", ProducedBy = ProducedBy.Trimmer)]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public MethodInfo GetMethod (string name)
			{
				return null;
			}

			// Analyzer doesn't warn about annotations on unsupported types:
			// https://github.com/dotnet/linker/issues/2273
			[ExpectedWarning ("IL2041", ProducedBy = ProducedBy.Trimmer)]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static void StaticMethod ()
			{
			}
		}
	}
}

namespace System
{
	class MethodThisDataFlowTypeTest : TestSystemTypeBase
	{
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		[ExpectedWarning ("IL2082", nameof (MethodThisDataFlowTypeTest) + "." + nameof (RequireNonPublicMethods) + "(Type)",
			"'type' argument ", "in call to 'System.MethodThisDataFlowTypeTest.RequireNonPublicMethods(Type)'",
			"implicit 'this' argument of method 'System.MethodThisDataFlowTypeTest.RequireThisPublicMethods()'")]
		public void RequireThisPublicMethods ()
		{
			RequirePublicMethods (this);
			RequireNonPublicMethods (this);
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
		[ExpectedWarning ("IL2082", nameof (MethodThisDataFlowTypeTest) + "." + nameof (RequirePublicMethods) + "(Type)")]
		public void RequireThisNonPublicMethods ()
		{
			RequirePublicMethods (this);
			RequireNonPublicMethods (this);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (PropagateToReturn), new Type[] { }, returnType: typeof (Type),
			messageCode: "IL2083", message: new string[] {
				nameof(PropagateToReturn),
				nameof(PropagateToReturn)
			})]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		public Type PropagateToReturn ()
		{
			return this;
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		Type _requiresPublicConstructors;

		[ExpectedWarning ("IL2084", nameof (MethodThisDataFlowTypeTest) + "." + nameof (_requiresPublicConstructors),
			nameof (PropagateToField))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		public void PropagateToField ()
		{
			_requiresPublicConstructors = this;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (RequireThisNonPublicMethods), new Type[] { },
			messageCode: "IL2085", message: new string[] {
				nameof (PropagateToThis),
				nameof (RequireThisNonPublicMethods)
			})]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		public void PropagateToThis ()
		{
			this.RequireThisNonPublicMethods ();
		}

		public object PropertyRequireThisPublicMethods {
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			get {
				return null;
			}
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			set {
				return;
			}
		}

		public object PropertyRequireThisNonPublicMethods {
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			get {
				return null;
			}
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			set {
				return;
			}
		}

		private static void RequirePublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			Type type)
		{
		}

		private static void RequireNonPublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
			Type type)
		{
		}
	}
}
