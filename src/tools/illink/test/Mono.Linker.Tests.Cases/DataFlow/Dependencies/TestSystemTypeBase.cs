// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System
{
	public class TestSystemTypeBase : Type
	{
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

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
		public override Type GetNestedType (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
		public override Type[] GetNestedTypes (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
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
