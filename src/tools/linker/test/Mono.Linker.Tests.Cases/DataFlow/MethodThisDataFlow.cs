// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
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
		}

		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), nameof (TypeTest.RequireThisMethods), new Type [] { },
			"The return value of method 'System.TypeTest Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow::GetWithPublicMethods()' " +
			"with dynamically accessed member kinds 'PublicMethods' " +
			"is passed into the implicit 'this' parameter of method 'System.Void System.TypeTest::RequireThisMethods()' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		static void PropagateToThis ()
		{
			GetWithPublicMethods ().RequireThisPublicMethods ();
			GetWithMethods ().RequireThisPublicMethods ();

			GetWithPublicMethods ().RequireThisMethods ();
			GetWithMethods ().RequireThisMethods ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), "get_" + nameof (TypeTest.PropertyRequireThisMethods), new Type [] { },
 			"The return value of method 'System.TypeTest Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow::GetWithPublicMethods()' " +
 			"with dynamically accessed member kinds 'PublicMethods' " +
 			"is passed into the implicit 'this' parameter of method 'System.Object System.TypeTest::get_PropertyRequireThisMethods()' " +
 			"which requires dynamically accessed member kinds `Methods`. " +
 			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		static void PropagateToThisWithGetters ()
		{
			_ = GetWithPublicMethods ().PropertyRequireThisPublicMethods;
			_ = GetWithMethods ().PropertyRequireThisPublicMethods;

			_ = GetWithPublicMethods ().PropertyRequireThisMethods;
			_ = GetWithMethods ().PropertyRequireThisMethods;
		}

		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), "set_" + nameof (TypeTest.PropertyRequireThisMethods), new Type [] { typeof(Object) },
 			"The return value of method 'System.TypeTest Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow::GetWithPublicMethods()' " +
 			"with dynamically accessed member kinds 'PublicMethods' " +
 			"is passed into the implicit 'this' parameter of method 'System.Void System.TypeTest::set_PropertyRequireThisMethods(System.Object)' " +
 			"which requires dynamically accessed member kinds `Methods`. " +
 			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		static void PropagateToThisWithSetters ()
		{
			GetWithPublicMethods ().PropertyRequireThisPublicMethods = null;
			GetWithMethods ().PropertyRequireThisPublicMethods = null;
			GetWithPublicMethods ().PropertyRequireThisMethods = null; // should error.
			GetWithMethods ().PropertyRequireThisMethods = null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicMethods)]
		static TypeTest GetWithPublicMethods ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Methods)]
		static TypeTest GetWithMethods ()
		{
			return null;
		}
	}
}

namespace System
{
	class TypeTest : Type
	{
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicMethods)]
		[UnrecognizedReflectionAccessPattern (typeof (TypeTest), nameof (RequireMethods), new Type [] { typeof (Type) },
			"The implicit 'this' parameter of method 'System.Void System.TypeTest::RequireThisPublicMethods()' " +
			"with dynamically accessed member kinds 'PublicMethods' " +
			"is passed into the parameter 'type' of method 'System.Void System.TypeTest::RequireMethods(System.Type)' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		public void RequireThisPublicMethods ()
		{
			RequirePublicMethods (this);
			RequireMethods (this);
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Methods)]
		public void RequireThisMethods ()
		{
			RequirePublicMethods (this);
			RequireMethods (this);
		}

		public object PropertyRequireThisPublicMethods {
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicMethods)]
			get {
				return null;
			}
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicMethods)]
			set {
				return;
			}
		}

		public object PropertyRequireThisMethods {
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Methods)] // applies to this
			get {
				return null;
			}
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Methods)]
			set {
				return;
			}
		}

		private static void RequirePublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicMethods)]
			Type type)
		{
		}

		private static void RequireMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Methods)]
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

		public override ConstructorInfo [] GetConstructors (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override object [] GetCustomAttributes (bool inherit)
		{
			throw new NotImplementedException ();
		}

		public override object [] GetCustomAttributes (Type attributeType, bool inherit)
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

		public override EventInfo [] GetEvents (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override FieldInfo GetField (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override FieldInfo [] GetFields (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override Type GetInterface (string name, bool ignoreCase)
		{
			throw new NotImplementedException ();
		}

		public override Type [] GetInterfaces ()
		{
			throw new NotImplementedException ();
		}

		public override MemberInfo [] GetMembers (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override MethodInfo [] GetMethods (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override Type GetNestedType (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override Type [] GetNestedTypes (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override PropertyInfo [] GetProperties (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override object InvokeMember (string name, BindingFlags invokeAttr, Binder binder, object target, object [] args, ParameterModifier [] modifiers, CultureInfo culture, string [] namedParameters)
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

		protected override ConstructorInfo GetConstructorImpl (BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type [] types, ParameterModifier [] modifiers)
		{
			throw new NotImplementedException ();
		}

		protected override MethodInfo GetMethodImpl (string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type [] types, ParameterModifier [] modifiers)
		{
			throw new NotImplementedException ();
		}

		protected override PropertyInfo GetPropertyImpl (string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type [] types, ParameterModifier [] modifiers)
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
