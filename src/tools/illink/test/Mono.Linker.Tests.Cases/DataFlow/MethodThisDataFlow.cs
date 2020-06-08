// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;

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

		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), nameof (TypeTest.RequireThisPublicMethods), new Type[] { },
			"The return value of method 'System.TypeTest Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow::GetWithNonPublicMethods()' " +
			"with dynamically accessed member kinds 'NonPublicMethods' " +
			"is passed into the implicit 'this' parameter of method 'System.Void System.TypeTest::RequireThisPublicMethods()' " +
			"which requires dynamically accessed member kinds 'PublicMethods'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), nameof (TypeTest.RequireThisNonPublicMethods), new Type[] { })]
		static void PropagateToThis ()
		{
			GetWithPublicMethods ().RequireThisPublicMethods ();
			GetWithNonPublicMethods ().RequireThisPublicMethods ();

			GetWithPublicMethods ().RequireThisNonPublicMethods ();
			GetWithNonPublicMethods ().RequireThisNonPublicMethods ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), "get_" + nameof (TypeTest.PropertyRequireThisPublicMethods), new Type[] { },
			"The return value of method 'System.TypeTest Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow::GetWithNonPublicMethods()' " +
			"with dynamically accessed member kinds 'NonPublicMethods' " +
			"is passed into the implicit 'this' parameter of method 'System.Object System.TypeTest::get_PropertyRequireThisPublicMethods()' " +
			"which requires dynamically accessed member kinds 'PublicMethods'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), "get_" + nameof (TypeTest.PropertyRequireThisNonPublicMethods), new Type[] { })]
		static void PropagateToThisWithGetters ()
		{
			_ = GetWithPublicMethods ().PropertyRequireThisPublicMethods;
			_ = GetWithNonPublicMethods ().PropertyRequireThisPublicMethods;

			_ = GetWithPublicMethods ().PropertyRequireThisNonPublicMethods;
			_ = GetWithNonPublicMethods ().PropertyRequireThisNonPublicMethods;
		}

		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), "set_" + nameof (TypeTest.PropertyRequireThisPublicMethods), new Type[] { typeof (Object) },
			"The return value of method 'System.TypeTest Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow::GetWithNonPublicMethods()' " +
			"with dynamically accessed member kinds 'NonPublicMethods' " +
			"is passed into the implicit 'this' parameter of method 'System.Void System.TypeTest::set_PropertyRequireThisPublicMethods(System.Object)' " +
			"which requires dynamically accessed member kinds 'PublicMethods'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), "set_" + nameof (TypeTest.PropertyRequireThisNonPublicMethods), new Type[] { typeof (Object) })]
		static void PropagateToThisWithSetters ()
		{
			GetWithPublicMethods ().PropertyRequireThisPublicMethods = null;
			GetWithNonPublicMethods ().PropertyRequireThisPublicMethods = null;
			GetWithPublicMethods ().PropertyRequireThisNonPublicMethods = null;
			GetWithNonPublicMethods ().PropertyRequireThisNonPublicMethods = null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static TypeTest GetWithPublicMethods ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
		static TypeTest GetWithNonPublicMethods ()
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
			[LogContains ("warning IL2041: DynamicallyAccessedMembersAttribute is specified " +
				"on method 'System.Reflection.MethodInfo Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow/NonTypeType::GetMethod(System.String)'. " +
				"The DynamicallyAccessedMembersAttribute is only allowed on method parameters, return value or generic parameters.")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public MethodInfo GetMethod (string name)
			{
				return null;
			}

			[LogContains ("warning IL2041: DynamicallyAccessedMembersAttribute is specified " +
				"on method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow/NonTypeType::StaticMethod()'. " +
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
	class TypeTest : Type
	{
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), nameof (RequireNonPublicMethods), new Type[] { typeof (Type) },
			"The implicit 'this' parameter of method 'System.Void System.TypeTest::RequireThisPublicMethods()' " +
			"with dynamically accessed member kinds 'PublicMethods' " +
			"is passed into the parameter 'type' of method 'System.Void System.TypeTest::RequireNonPublicMethods(System.Type)' " +
			"which requires dynamically accessed member kinds 'NonPublicMethods'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'NonPublicMethods'.")]
		public void RequireThisPublicMethods ()
		{
			RequirePublicMethods (this);
			RequireNonPublicMethods (this);
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), nameof (RequirePublicMethods), new Type[] { typeof (Type) })]
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

		#region Required memebers for Type
		public override Assembly Assembly => throw new NotImplementedException ();

		public override string AssemblyQualifiedName => throw new NotImplementedException ();

		public override Type BaseType => throw new NotImplementedException ();

		public override string FullName => throw new NotImplementedException ();

		public override Guid GUID => throw new NotImplementedException ();

		public override Module Module => throw new NotImplementedException ();

		public override string Namespace => throw new NotImplementedException ();

		public override Type UnderlyingSystemType => throw new NotImplementedException ();

		public override string Name => throw new NotImplementedException ();

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

		public override EventInfo GetEvent (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override EventInfo[] GetEvents (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override FieldInfo GetField (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override FieldInfo[] GetFields (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override Type GetInterface (string name, bool ignoreCase)
		{
			throw new NotImplementedException ();
		}

		public override Type[] GetInterfaces ()
		{
			throw new NotImplementedException ();
		}

		public override MemberInfo[] GetMembers (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override MethodInfo[] GetMethods (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override Type GetNestedType (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override Type[] GetNestedTypes (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override PropertyInfo[] GetProperties (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

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

		protected override ConstructorInfo GetConstructorImpl (BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

		protected override MethodInfo GetMethodImpl (string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

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
		#endregion
	}
}
