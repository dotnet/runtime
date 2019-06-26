#nullable disable

//
// System.Reflection.Emit.GenericTypeParameterBuilder
//
// Martin Baulig (martin@ximian.com)
//
// (C) 2004 Novell, Inc.
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if MONO_FEATURE_SRE
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Reflection.Emit
{
	[ComVisible (true)]
	[StructLayout (LayoutKind.Sequential)]
	public sealed class GenericTypeParameterBuilder : 
		TypeInfo
	{
	#region Sync with reflection.h
		private TypeBuilder tbuilder;
		private MethodBuilder mbuilder;
		private string name;
		private int index;
		private Type base_type;
#pragma warning disable 414
		private Type[] iface_constraints;
		private CustomAttributeBuilder[] cattrs;
		private GenericParameterAttributes attrs;
#pragma warning restore
	#endregion

		public void SetBaseTypeConstraint (Type baseTypeConstraint)
		{
			this.base_type = baseTypeConstraint ?? typeof (object);
		}

		[ComVisible (true)]
		public void SetInterfaceConstraints (params Type[] interfaceConstraints)
		{
			this.iface_constraints = interfaceConstraints;
		}

		public void SetGenericParameterAttributes (GenericParameterAttributes genericParameterAttributes)
		{
			this.attrs = genericParameterAttributes;
		}

		internal GenericTypeParameterBuilder (TypeBuilder tbuilder,
						      MethodBuilder mbuilder,
						      string name, int index)
		{
			this.tbuilder = tbuilder;
			this.mbuilder = mbuilder;
			this.name = name;
			this.index = index;
		}

		internal override Type InternalResolve ()
		{
			if (mbuilder != null)
				return MethodBase.GetMethodFromHandle (mbuilder.MethodHandleInternal, mbuilder.TypeBuilder.InternalResolve ().TypeHandle).GetGenericArguments () [index];
			return tbuilder.InternalResolve ().GetGenericArguments () [index];
		}

		internal override Type RuntimeResolve ()
		{
			if (mbuilder != null)
				return MethodBase.GetMethodFromHandle (mbuilder.MethodHandleInternal, mbuilder.TypeBuilder.RuntimeResolve ().TypeHandle).GetGenericArguments () [index];
			return tbuilder.RuntimeResolve ().GetGenericArguments () [index];
		}

		[ComVisible (true)]
		public override bool IsSubclassOf (Type c)
		{
			throw not_supported ();
		}

		protected override TypeAttributes GetAttributeFlagsImpl ()
		{
			return TypeAttributes.Public;
		}

		protected override ConstructorInfo GetConstructorImpl (BindingFlags bindingAttr,
								       Binder binder,
								       CallingConventions callConvention,
								       Type[] types,
								       ParameterModifier[] modifiers)
		{
			throw not_supported ();
		}

		[ComVisible (true)]
		public override ConstructorInfo[] GetConstructors (BindingFlags bindingAttr)
		{
			throw not_supported ();
		}

		public override EventInfo GetEvent (string name, BindingFlags bindingAttr)
		{
			throw not_supported ();
		}

		public override EventInfo[] GetEvents ()
		{
			throw not_supported ();
		}

		public override EventInfo[] GetEvents (BindingFlags bindingAttr)
		{
			throw not_supported ();
		}

		public override FieldInfo GetField (string name, BindingFlags bindingAttr)
		{
			throw not_supported ();
		}

		public override FieldInfo[] GetFields (BindingFlags bindingAttr)
		{
			throw not_supported ();
		}
		
		public override Type GetInterface (string name, bool ignoreCase)
		{
			throw not_supported ();
		}

		public override Type[] GetInterfaces ()
		{
			throw not_supported ();
		}
		
		public override MemberInfo[] GetMembers (BindingFlags bindingAttr)
		{
			throw not_supported ();
		}

		public override MemberInfo[] GetMember (string name, MemberTypes type, BindingFlags bindingAttr)
		{
			throw not_supported ();
		}

		public override MethodInfo [] GetMethods (BindingFlags bindingAttr)
		{
			throw not_supported ();
		}

		protected override MethodInfo GetMethodImpl (string name, BindingFlags bindingAttr,
							     Binder binder,
							     CallingConventions callConvention,
							     Type[] types, ParameterModifier[] modifiers)
		{
			throw not_supported ();
		}

		public override Type GetNestedType (string name, BindingFlags bindingAttr)
		{
			throw not_supported ();
		}

		public override Type[] GetNestedTypes (BindingFlags bindingAttr)
		{
			throw not_supported ();
		}

		public override PropertyInfo [] GetProperties (BindingFlags bindingAttr)
		{
			throw not_supported ();
		}

		protected override PropertyInfo GetPropertyImpl (string name, BindingFlags bindingAttr,
								 Binder binder, Type returnType,
								 Type[] types,
								 ParameterModifier[] modifiers)
		{
			throw not_supported ();
		}

		protected override bool HasElementTypeImpl ()
		{
			return false;
		}

		public override bool IsAssignableFrom (Type c)
		{
			throw not_supported ();
		}

		public override bool IsAssignableFrom (TypeInfo typeInfo)
		{
			if (typeInfo == null)
				return false;

			return IsAssignableFrom (typeInfo.AsType ());
		}

		public override bool IsInstanceOfType (object o)
		{
			throw not_supported ();
		}

		protected override bool IsArrayImpl ()
		{
			return false;
		}

		protected override bool IsByRefImpl ()
		{
			return false;
		}

		protected override bool IsCOMObjectImpl ()
		{
			return false;
		}

		protected override bool IsPointerImpl ()
		{
			return false;
		}

		protected override bool IsPrimitiveImpl ()
		{
			return false;
		}

		protected override bool IsValueTypeImpl ()
		{
			return base_type != null ? base_type.IsValueType : false;
		}

        public override bool IsSZArray {
			get {
				return false;
			}
		}

		public override object InvokeMember (string name, BindingFlags invokeAttr,
						     Binder binder, object target, object[] args,
						     ParameterModifier[] modifiers,
						     CultureInfo culture, string[] namedParameters)
		{
			throw not_supported ();
		}

		public override Type GetElementType ()
		{
			throw not_supported ();
		}

		public override Type UnderlyingSystemType {
			get {
				return this;
			}
		}

		public override Assembly Assembly {
			get { return tbuilder.Assembly; }
		}

		public override string AssemblyQualifiedName {
			get { return null; }
		}

		public override Type BaseType {
			get { return base_type; }
		}

		public override string FullName {
			get { return null; }
		}

		public override Guid GUID {
			get { throw not_supported (); }
		}

		public override bool IsDefined (Type attributeType, bool inherit)
		{
			throw not_supported ();
		}

		public override object[] GetCustomAttributes (bool inherit)
		{
			throw not_supported ();
		}

		public override object[] GetCustomAttributes (Type attributeType, bool inherit)
		{
			throw not_supported ();
		}

		[ComVisible (true)]
		public override InterfaceMapping GetInterfaceMap (Type interfaceType)
		{
			throw not_supported ();
		}

		public override string Name {
			get { return name; }
		}

		public override string Namespace {
			get { return null; }
		}

		public override Module Module {
			get { return tbuilder.Module; }
		}

		public override Type DeclaringType {
			get { return mbuilder != null ? mbuilder.DeclaringType : tbuilder; }
		}

		public override Type ReflectedType {
			get { return DeclaringType; }
		}

		public override RuntimeTypeHandle TypeHandle {
			get { throw not_supported (); }
		}

		public override Type[] GetGenericArguments ()
		{
			throw new InvalidOperationException ();
		}

		public override Type GetGenericTypeDefinition ()
		{
			throw new InvalidOperationException ();
		}

		public override bool ContainsGenericParameters {
			get { return true; }
		}

		public override bool IsGenericParameter {
			get { return true; }
		}

		public override bool IsGenericType {
			get { return false; }
		}

		public override bool IsGenericTypeDefinition {
			get { return false; }
		}

		public override GenericParameterAttributes GenericParameterAttributes {
			get {
				return attrs;
			}
		}

		public override int GenericParameterPosition {
			get { return index; }
		}

		public override Type[] GetGenericParameterConstraints ()
		{
			throw new InvalidOperationException ();
		}

		public override MethodBase DeclaringMethod {
			get { return mbuilder; }
		}

		public void SetCustomAttribute (CustomAttributeBuilder customBuilder)
		{
			if (customBuilder == null)
				throw new ArgumentNullException ("customBuilder");

			if (cattrs != null) {
				CustomAttributeBuilder[] new_array = new CustomAttributeBuilder [cattrs.Length + 1];
				cattrs.CopyTo (new_array, 0);
				new_array [cattrs.Length] = customBuilder;
				cattrs = new_array;
			} else {
				cattrs = new CustomAttributeBuilder [1];
				cattrs [0] = customBuilder;
			}
		}

		// FIXME: "unverified implementation"
		public void SetCustomAttribute (ConstructorInfo con, byte [] binaryAttribute)
		{
			SetCustomAttribute (new CustomAttributeBuilder (con, binaryAttribute));
		}

		private Exception not_supported ()
		{
			return new NotSupportedException ();
		}

		public override string ToString ()
		{
			return name;
		}

		// FIXME:
		public override bool Equals (object o)
		{
			return base.Equals (o);
		}

		// FIXME:
		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}

		public override Type MakeArrayType ()
		{
			return  new ArrayType (this, 0);
		}

		public override Type MakeArrayType (int rank)
		{
			if (rank < 1)
				throw new IndexOutOfRangeException ();
			return new ArrayType (this, rank);
		}

		public override Type MakeByRefType ()
		{
			return new ByRefType (this);
		}

		public override Type MakeGenericType (params Type[] typeArguments)
		{
			throw new InvalidOperationException (Environment.GetResourceString ("Arg_NotGenericTypeDefinition"));
		}

		public override Type MakePointerType ()
		{
			return new PointerType (this);
		}

		internal override bool IsUserType {
			get {
				return false;
			}
		}
	}
}
#endif
