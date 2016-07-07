// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection.Emit
{
    using System;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Diagnostics.Contracts;

[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class GenericTypeParameterBuilder: TypeInfo
    {
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo){
            if(typeInfo==null) return false;            
            return IsAssignableFrom(typeInfo.AsType());
        }

        #region Private Data Mebers
        internal TypeBuilder m_type;
        #endregion

        #region Constructor
        internal GenericTypeParameterBuilder(TypeBuilder type)
        {
            m_type = type;
        }
        #endregion

        #region Object Overrides
        public override String ToString() 
        { 
            return m_type.Name;
        }
        public override bool Equals(object o) 
        { 
            GenericTypeParameterBuilder g = o as GenericTypeParameterBuilder;
            
            if (g == null)
                return false;

            return object.ReferenceEquals(g.m_type, m_type);
        }
        public override int GetHashCode() { return m_type.GetHashCode(); }
        #endregion

        #region MemberInfo Overrides
        public override Type DeclaringType { get { return m_type.DeclaringType; } }

        public override Type ReflectedType { get { return m_type.ReflectedType; } }

        public override String Name { get { return m_type.Name; } }

        public override Module Module { get { return m_type.Module; } }

        internal int MetadataTokenInternal { get { return m_type.MetadataTokenInternal; } }
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

            string szrank = "";
            if (rank == 1)
            {
                szrank = "*";
            }
            else 
            {
                for(int i = 1; i < rank; i++)
                    szrank += ",";
            }

            string s = String.Format(CultureInfo.InvariantCulture, "[{0}]", szrank); // [,,]
            SymbolType st = SymbolType.FormCompoundType(s, this, 0) as SymbolType;
            return st;
        }

        public override Guid GUID { get { throw new NotSupportedException(); } }

        public override Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters) { throw new NotSupportedException(); }

        public override Assembly Assembly { get { return m_type.Assembly; } }

        public override RuntimeTypeHandle TypeHandle { get { throw new NotSupportedException(); } }

        public override String FullName { get { return null; } }

        public override String Namespace { get { return null; } }

        public override String AssemblyQualifiedName { get { return null; } }

        public override Type BaseType { get { return m_type.BaseType; } } 

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

        protected override TypeAttributes GetAttributeFlagsImpl() { return TypeAttributes.Public; }

        protected override bool IsArrayImpl() { return false; }

        protected override bool IsByRefImpl() { return false; }

        protected override bool IsPointerImpl() { return false; }

        protected override bool IsPrimitiveImpl() { return false; }

        protected override bool IsCOMObjectImpl() { return false; }

        public override Type GetElementType() { throw new NotSupportedException(); }

        protected override bool HasElementTypeImpl() { return false; }

        public override Type UnderlyingSystemType { get { return this; } }

        public override Type[] GetGenericArguments() { throw new InvalidOperationException(); }

        public override bool IsGenericTypeDefinition { get { return false; } }

        public override bool IsGenericType { get { return false; } }

        public override bool IsGenericParameter { get { return true; } }

        public override bool IsConstructedGenericType { get { return false; } }

        public override int GenericParameterPosition { get { return m_type.GenericParameterPosition; } }

        public override bool ContainsGenericParameters { get { return m_type.ContainsGenericParameters; } }

        public override GenericParameterAttributes GenericParameterAttributes { get { return m_type.GenericParameterAttributes; } }

        public override MethodBase DeclaringMethod { get { return m_type.DeclaringMethod; } }

        public override Type GetGenericTypeDefinition() { throw new InvalidOperationException(); }

        public override Type MakeGenericType(params Type[] typeArguments) { throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericTypeDefinition")); }

        protected override bool IsValueTypeImpl() { return false; }

        public override bool IsAssignableFrom(Type c) { throw new NotSupportedException(); }

        [System.Runtime.InteropServices.ComVisible(true)]
        [Pure]
        public override bool IsSubclassOf(Type c) { throw new NotSupportedException(); }
        #endregion

        #region ICustomAttributeProvider Implementation
        public override Object[] GetCustomAttributes(bool inherit) { throw new NotSupportedException(); }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit) { throw new NotSupportedException(); }

        public override bool IsDefined(Type attributeType, bool inherit) { throw new NotSupportedException(); }
        #endregion

        #region Public Members
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {   
            m_type.SetGenParamCustomAttribute(con, binaryAttribute);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            m_type.SetGenParamCustomAttribute(customBuilder);
        }

        public void SetBaseTypeConstraint(Type baseTypeConstraint)
        {
            m_type.CheckContext(baseTypeConstraint);
            m_type.SetParent(baseTypeConstraint);
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public void SetInterfaceConstraints(params Type[] interfaceConstraints)
        {
            m_type.CheckContext(interfaceConstraints);
            m_type.SetInterfaces(interfaceConstraints);
        }

        public void SetGenericParameterAttributes(GenericParameterAttributes genericParameterAttributes)
        {
            m_type.SetGenParamAttributes(genericParameterAttributes);
        }
        #endregion
    }
}

