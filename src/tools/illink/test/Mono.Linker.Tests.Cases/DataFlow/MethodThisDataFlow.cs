// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	public class MethodThisDataFlow
	{
		public static void Main ()
		{
			PropagateToThis ();
			PropagateToThisWithGetters ();
			PropagateToThisWithSetters ();

			TestAnnotationOnNonTypeMethod ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods), new Type[] { },
			"The return value of method 'Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()' " +
			"with dynamically accessed member kinds 'NonPublicMethods' " +
			"is passed into the implicit 'this' parameter of method 'System.MethodThisDataFlowTypeTest.RequireThisPublicMethods()' " +
			"which requires dynamically accessed member kinds 'PublicMethods'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.RequireThisNonPublicMethods), new Type[] { })]
		static void PropagateToThis ()
		{
			GetWithPublicMethods ().RequireThisPublicMethods ();
			GetWithNonPublicMethods ().RequireThisPublicMethods ();

			GetWithPublicMethods ().RequireThisNonPublicMethods ();
			GetWithNonPublicMethods ().RequireThisNonPublicMethods ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), "get_" + nameof (MethodThisDataFlowTypeTest.PropertyRequireThisPublicMethods), new Type[] { },
			"The return value of method 'Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()' " +
			"with dynamically accessed member kinds 'NonPublicMethods' " +
			"is passed into the implicit 'this' parameter of method 'System.MethodThisDataFlowTypeTest.get_PropertyRequireThisPublicMethods()' " +
			"which requires dynamically accessed member kinds 'PublicMethods'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), "get_" + nameof (MethodThisDataFlowTypeTest.PropertyRequireThisNonPublicMethods), new Type[] { })]
		static void PropagateToThisWithGetters ()
		{
			_ = GetWithPublicMethods ().PropertyRequireThisPublicMethods;
			_ = GetWithNonPublicMethods ().PropertyRequireThisPublicMethods;

			_ = GetWithPublicMethods ().PropertyRequireThisNonPublicMethods;
			_ = GetWithNonPublicMethods ().PropertyRequireThisNonPublicMethods;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), "set_" + nameof (MethodThisDataFlowTypeTest.PropertyRequireThisPublicMethods), new Type[] { typeof (Object) },
			"The return value of method 'Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()' " +
			"with dynamically accessed member kinds 'NonPublicMethods' " +
			"is passed into the implicit 'this' parameter of method 'System.MethodThisDataFlowTypeTest.set_PropertyRequireThisPublicMethods(Object)' " +
			"which requires dynamically accessed member kinds 'PublicMethods'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), "set_" + nameof (MethodThisDataFlowTypeTest.PropertyRequireThisNonPublicMethods), new Type[] { typeof (Object) })]
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
		}

		class NonTypeType
		{
			[LogContains ("warning IL2041: Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.NonTypeType.GetMethod(String): " +
				"The DynamicallyAccessedMembersAttribute is only allowed on method parameters, return value or generic parameters.")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public MethodInfo GetMethod (string name)
			{
				return null;
			}

			[LogContains ("warning IL2041: Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.NonTypeType.StaticMethod(): " +
				"The DynamicallyAccessedMembersAttribute is only allowed on method parameters, return value or generic parameters.")]
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
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (RequireNonPublicMethods), new Type[] { typeof (Type) },
			"The implicit 'this' parameter of method 'System.MethodThisDataFlowTypeTest.RequireThisPublicMethods()' " +
			"with dynamically accessed member kinds 'PublicMethods' " +
			"is passed into the parameter 'type' of method 'System.MethodThisDataFlowTypeTest.RequireNonPublicMethods(Type)' " +
			"which requires dynamically accessed member kinds 'NonPublicMethods'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'NonPublicMethods'.")]
		public void RequireThisPublicMethods ()
		{
			RequirePublicMethods (this);
			RequireNonPublicMethods (this);
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (RequirePublicMethods), new Type[] { typeof (Type) })]
		public void RequireThisNonPublicMethods ()
		{
			RequirePublicMethods (this);
			RequireNonPublicMethods (this);
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
