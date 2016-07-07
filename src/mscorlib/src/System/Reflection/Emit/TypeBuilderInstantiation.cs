// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection.Emit
{
    using System;
    using System.Reflection;
    using System.Collections;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    internal sealed class TypeBuilderInstantiation : TypeInfo
    {
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo){
            if(typeInfo==null) return false;            
            return IsAssignableFrom(typeInfo.AsType());
        }

        #region Static Members
        internal static Type MakeGenericType(Type type, Type[] typeArguments)
        {
            Contract.Requires(type != null, "this is only called from RuntimeType.MakeGenericType and TypeBuilder.MakeGenericType so 'type' cannot be null");

            if (!type.IsGenericTypeDefinition)
                throw new InvalidOperationException();

            if (typeArguments == null)
                throw new ArgumentNullException("typeArguments");
            Contract.EndContractBlock();

            foreach (Type t in typeArguments)
            {
                if (t == null)
                    throw new ArgumentNullException("typeArguments");                    
            }
            
            return new TypeBuilderInstantiation(type, typeArguments);
        }

        #endregion

        #region Private Data Mebers
        private Type m_type;
        private Type[] m_inst;
        private string m_strFullQualName;
        internal Hashtable m_hashtable = new Hashtable();

        #endregion

        #region Constructor
        private TypeBuilderInstantiation(Type type, Type[] inst)
        {
            m_type = type;
            m_inst = inst;
            m_hashtable = new Hashtable();
        }
        #endregion

        #region Object Overrides
        public override String ToString()
        {
            return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
        }
        #endregion

        #region MemberInfo Overrides
        public override Type DeclaringType { get { return m_type.DeclaringType; } }

        public override Type ReflectedType { get { return m_type.ReflectedType; } }

        public override String Name { get { return m_type.Name; } }

        public override Module Module { get { return m_type.Module; } }
        #endregion

        #region Type Overrides
        public override Type MakePointerType() 
        { 
            return SymbolType.FormCompoundType("*", this, 0); 
        }
        public override Type MakeByRefType() 
        {
            return SymbolType.FormCompoundType("&", this, 0);
        }
        public override Type MakeArrayType() 
        {
            return SymbolType.FormCompoundType("[]", this, 0);
        }
        public override Type MakeArrayType(int rank) 
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();
            Contract.EndContractBlock();

            string comma = "";
            for(int i = 1; i < rank; i++)
                comma += ",";

            string s = String.Format(CultureInfo.InvariantCulture, "[{0}]", comma);
            return SymbolType.FormCompoundType(s, this, 0);
        }
        public override Guid GUID { get { throw new NotSupportedException(); } }
        public override Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters) { throw new NotSupportedException(); }
        public override Assembly Assembly { get { return m_type.Assembly; } }
        public override RuntimeTypeHandle TypeHandle { get { throw new NotSupportedException(); } }
        public override String FullName 
        { 
            get 
            { 
                if (m_strFullQualName == null)
                    m_strFullQualName = TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName); 
                return m_strFullQualName;
            } 
        }
        public override String Namespace { get { return m_type.Namespace; } }
        public override String AssemblyQualifiedName { get { return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName); } }
        private Type Substitute(Type[] substitutes)
        {
            Type[] inst = GetGenericArguments();
            Type[] instSubstituted = new Type[inst.Length];

            for (int i = 0; i < instSubstituted.Length; i++)
            {
                Type t = inst[i];
                
                if (t is TypeBuilderInstantiation)
                {
                    instSubstituted[i] = (t as TypeBuilderInstantiation).Substitute(substitutes);
                }
                else if (t is GenericTypeParameterBuilder)
                {
                    // Substitute
                    instSubstituted[i] = substitutes[t.GenericParameterPosition];
                }
                else
                {
                    instSubstituted[i] = t;
                }
            }

            return GetGenericTypeDefinition().MakeGenericType(instSubstituted);
        }
        public override Type BaseType
        {
            // B<A,B,C>
            // D<T,S> : B<S,List<T>,char>
            
            // D<string,int> : B<int,List<string>,char>
            // D<S,T> : B<T,List<S>,char>        
            // D<S,string> : B<string,List<S>,char>        
            get
            {
                Type typeBldrBase = m_type.BaseType;

                if (typeBldrBase == null)
                    return null;

                TypeBuilderInstantiation typeBldrBaseAs = typeBldrBase as TypeBuilderInstantiation;
                
                if (typeBldrBaseAs == null)
                    return typeBldrBase;

                return typeBldrBaseAs.Substitute(GetGenericArguments());
            }
        }
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) { throw new NotSupportedException(); }
        
        [System.Runtime.InteropServices.ComVisible(true)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) { throw new NotSupportedException(); }
        protected override MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) { throw new NotSupportedException(); }
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) { throw new NotSupportedException(); }
        public override FieldInfo GetField(String name, BindingFlags bindingAttr) { throw new NotSupportedException(); }
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) { throw new NotSupportedException(); }
        public override Type GetInterface(String name, bool ignoreCase) { throw new NotSupportedException(); }
        public override Type[] GetInterfaces() { throw new NotSupportedException(); }
        public override EventInfo GetEvent(String name, BindingFlags bindingAttr) { throw new NotSupportedException(); }
        public override EventInfo[] GetEvents() { throw new NotSupportedException(); }
        protected override PropertyInfo GetPropertyImpl(String name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) { throw new NotSupportedException(); }
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) { throw new NotSupportedException(); }
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) { throw new NotSupportedException(); }
        public override Type GetNestedType(String name, BindingFlags bindingAttr) { throw new NotSupportedException(); }
        public override MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [System.Runtime.InteropServices.ComVisible(true)]
        public override InterfaceMapping GetInterfaceMap(Type interfaceType) { throw new NotSupportedException(); }
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) { throw new NotSupportedException(); }
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) { throw new NotSupportedException(); }
        protected override TypeAttributes GetAttributeFlagsImpl() { return m_type.Attributes; }
        protected override bool IsArrayImpl() { return false; }
        protected override bool IsByRefImpl() { return false; }
        protected override bool IsPointerImpl() { return false; }
        protected override bool IsPrimitiveImpl() { return false; }
        protected override bool IsCOMObjectImpl() { return false; }
        public override Type GetElementType() { throw new NotSupportedException(); }
        protected override bool HasElementTypeImpl() { return false; }
        public override Type UnderlyingSystemType { get { return this; } }
        public override Type[] GetGenericArguments() { return m_inst; }
        public override bool IsGenericTypeDefinition { get { return false; } }
        public override bool IsGenericType { get { return true; } }
        public override bool IsConstructedGenericType { get { return true; } }
        public override bool IsGenericParameter { get { return false; } }
        public override int GenericParameterPosition { get { throw new InvalidOperationException(); } }
        protected override bool IsValueTypeImpl() { return m_type.IsValueType; }
        public override bool ContainsGenericParameters
        {
            get
            {
                for (int i = 0; i < m_inst.Length; i++)
                {
                    if (m_inst[i].ContainsGenericParameters)
                        return true;
                }

                return false;
            }
        }
        public override MethodBase DeclaringMethod { get { return null; } }
        public override Type GetGenericTypeDefinition() { return m_type; }
        public override Type MakeGenericType(params Type[] inst) { throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericTypeDefinition")); }
        public override bool IsAssignableFrom(Type c) { throw new NotSupportedException(); }

        [System.Runtime.InteropServices.ComVisible(true)]
        [Pure]
        public override bool IsSubclassOf(Type c)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region ICustomAttributeProvider Implementation
        public override Object[] GetCustomAttributes(bool inherit) { throw new NotSupportedException(); }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit) { throw new NotSupportedException(); }

        public override bool IsDefined(Type attributeType, bool inherit) { throw new NotSupportedException(); }
        #endregion
    }
}



































