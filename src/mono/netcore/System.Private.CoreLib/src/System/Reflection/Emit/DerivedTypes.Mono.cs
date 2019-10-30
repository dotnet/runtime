#nullable disable

//
// System.Reflection.Emit.DerivedTypes.cs
//
// Authors:
// 	Rodrigo Kumpera <rkumpera@novell.com>
//
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;


namespace System.Reflection.Emit
{
	[StructLayout (LayoutKind.Sequential)]
	abstract partial class SymbolType : TypeInfo
	{
		internal Type m_baseType;

		internal SymbolType (Type elementType)
		{
			this.m_baseType = elementType;
		}

		internal abstract String FormatName (string elementName);

		protected override bool IsArrayImpl ()
		{
			return false;
		}

		protected override bool IsByRefImpl ()
		{
			return false;
		}

		protected override bool IsPointerImpl ()
		{
			return false;
		}

		public override Type MakeArrayType ()
		{
			return new ArrayType (this, 0);
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

		public override Type MakePointerType ()
		{
			return new PointerType (this);
		}

		public override string ToString ()
		{
			return FormatName (m_baseType.ToString ());
		}

		public override string AssemblyQualifiedName {
			get {
				string fullName = FormatName (m_baseType.FullName);
				if (fullName == null)
					return null;
				return fullName + ", " + m_baseType.Assembly.FullName;
			}
		}


		public override string FullName {
			get {
				return FormatName (m_baseType.FullName);
			}
		}

		public override string Name {
			get {
				return FormatName (m_baseType.Name);
			}
		}

		public override Type UnderlyingSystemType {
			get {
				return this;
			}
		}

		internal override bool IsUserType {
			get {
				return m_baseType.IsUserType;
			}
		}

		// Called from the runtime to return the corresponding finished Type object
		internal override Type RuntimeResolve () {
			return InternalResolve ();
		}

        public override Guid GUID
        {
            get { throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType")); }
        }

        public override Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target,
            Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Module Module
        {
            get
            {
                Type baseType;

                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType) baseType).m_baseType);

                return baseType.Module;
            }
        }
        public override Assembly Assembly
        {
            get
            {
                Type baseType;

                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType) baseType).m_baseType);

                return baseType.Assembly;
            }
        }

        public override RuntimeTypeHandle TypeHandle
        {
             get { throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType")); }
        }

        public override String Namespace
        {
            get { return m_baseType.Namespace; }
        }

        public override Type BaseType
        {
            get { return typeof(System.Array); }
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr,Binder binder,
                CallingConventions callConvention, Type[] types,ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        protected override MethodInfo GetMethodImpl(String name,BindingFlags bindingAttr,Binder binder,
                CallingConventions callConvention, Type[] types,ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Type GetInterface(String name,bool ignoreCase)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Type[] GetInterfaces()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override EventInfo GetEvent(String name,BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override EventInfo[] GetEvents()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        protected override PropertyInfo GetPropertyImpl(String name, BindingFlags bindingAttr, Binder binder,
                Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Type GetNestedType(String name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override MemberInfo[] GetMember(String name,  MemberTypes type, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            // Return the attribute flags of the base type?
            Type baseType;
            for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType);
            return baseType.Attributes;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        protected override bool IsValueTypeImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        public override bool IsConstructedGenericType
        {
            get
            {
                return false;
            }
        }

        public override Type GetElementType()
        {
            return m_baseType;
        }

        protected override bool HasElementTypeImpl()
        {
            return m_baseType != null;
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override bool IsDefined (Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }
	}

	[StructLayout (LayoutKind.Sequential)]
	internal class ArrayType : SymbolType
	{
		int rank;

		internal ArrayType (Type elementType, int rank) : base (elementType)
		{
			this.rank = rank;
		}

		internal int GetEffectiveRank ()
		{
			return rank;
		}

		internal override Type InternalResolve ()
		{
			Type et = m_baseType.InternalResolve ();
			if (rank == 0)
				return et.MakeArrayType ();
			return et.MakeArrayType (rank);
		}

		internal override Type RuntimeResolve ()
		{
			Type et = m_baseType.RuntimeResolve ();
			if (rank == 0)
				return et.MakeArrayType ();
			return et.MakeArrayType (rank);
		}

		protected override bool IsArrayImpl ()
		{
			return true;
		}

        public override bool IsSZArray {
			get {
				return rank == 0;
			}
		}

		public override int GetArrayRank ()
		{
			return (rank == 0) ? 1 : rank;
		}

		internal override String FormatName (string elementName)
		{
			if (elementName == null)
				return null;
			StringBuilder sb = new StringBuilder (elementName);
			sb.Append ("[");
			for (int i = 1; i < rank; ++i)
				sb.Append (",");
			if (rank == 1)
				sb.Append ("*");
			sb.Append ("]");
			return sb.ToString ();
		}
	}

	[StructLayout (LayoutKind.Sequential)]
	internal class ByRefType : SymbolType
	{
		internal ByRefType (Type elementType) : base (elementType)
		{
		}

		internal override Type InternalResolve ()
		{
			return m_baseType.InternalResolve ().MakeByRefType ();
		}

		protected override bool IsByRefImpl ()
		{
			return true;
		}

		internal override String FormatName (string elementName)
		{
			if (elementName == null)
				return null;
			return elementName + "&";
		}

		public override Type MakeArrayType ()
		{
			throw new ArgumentException ("Cannot create an array type of a byref type");
		}

		public override Type MakeArrayType (int rank)
		{
			throw new ArgumentException ("Cannot create an array type of a byref type");
		}

		public override Type MakeByRefType ()
		{
			throw new ArgumentException ("Cannot create a byref type of an already byref type");
		}

		public override Type MakePointerType ()
		{
			throw new ArgumentException ("Cannot create a pointer type of a byref type");
		}
	}

	[StructLayout (LayoutKind.Sequential)]
	internal class PointerType : SymbolType
	{
		internal PointerType (Type elementType) : base (elementType)
		{
		}

		internal override Type InternalResolve ()
		{
			return m_baseType.InternalResolve ().MakePointerType ();
		}

		protected override bool IsPointerImpl ()
		{
			return true;
		}

		internal override String FormatName (string elementName)
		{
			if (elementName == null)
				return null;
			return elementName + "*";
		}
	}

}
#endif
