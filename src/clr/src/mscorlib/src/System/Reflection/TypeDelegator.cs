// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// TypeDelegator
// 
// This class wraps a Type object and delegates all methods to that Type.

namespace System.Reflection {

    using System;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;
    using CultureInfo = System.Globalization.CultureInfo;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class TypeDelegator : TypeInfo
    {
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo){
            if(typeInfo==null) return false;            
            return IsAssignableFrom(typeInfo.AsType());
        }

        protected Type typeImpl;
        
        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        protected TypeDelegator() {}
        
        public TypeDelegator(Type delegatingType) {
            if (delegatingType == null)
                throw new ArgumentNullException("delegatingType");
            Contract.EndContractBlock();
                
            typeImpl = delegatingType;
        }
        
        public override Guid GUID {
            get {return typeImpl.GUID;}
        }

        public override int MetadataToken { get { return typeImpl.MetadataToken; } }
        
        public override Object InvokeMember(String name,BindingFlags invokeAttr,Binder binder,Object target,
            Object[] args,ParameterModifier[] modifiers,CultureInfo culture,String[] namedParameters)
        {
            return typeImpl.InvokeMember(name,invokeAttr,binder,target,args,modifiers,culture,namedParameters);
        }
        
        public override Module Module {
                get {return typeImpl.Module;}
        }
        
        public override Assembly Assembly {
                get {return typeImpl.Assembly;}
        }

        public override RuntimeTypeHandle TypeHandle {
                get{return typeImpl.TypeHandle;}
        }
        
        public override String Name {
            get{return typeImpl.Name;}
        }
        
        public override String FullName {
            get{return typeImpl.FullName;}
        }
        
        public override String Namespace {
            get{return typeImpl.Namespace;}
        }
        
        public override String AssemblyQualifiedName {
            get { 
                return typeImpl.AssemblyQualifiedName;
            }
        }
            
        public override Type BaseType {
            get{return typeImpl.BaseType;}
        }
        
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr,Binder binder,
                CallingConventions callConvention, Type[] types,ParameterModifier[] modifiers)
        {
            return typeImpl.GetConstructor(bindingAttr,binder,callConvention,types,modifiers);
        }
        
[System.Runtime.InteropServices.ComVisible(true)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return typeImpl.GetConstructors(bindingAttr);
        }
        
        protected override MethodInfo GetMethodImpl(String name,BindingFlags bindingAttr,Binder binder,
                CallingConventions callConvention, Type[] types,ParameterModifier[] modifiers)
        {
            // This is interesting there are two paths into the impl.  One that validates
            //  type as non-null and one where type may be null.
            if (types == null)
                return typeImpl.GetMethod(name,bindingAttr);
            else
                return typeImpl.GetMethod(name,bindingAttr,binder,callConvention,types,modifiers);
        }
        
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return typeImpl.GetMethods(bindingAttr);
        }
        
        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            return typeImpl.GetField(name,bindingAttr);
        }
        
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return typeImpl.GetFields(bindingAttr);
        }
        
        public override Type GetInterface(String name, bool ignoreCase)
        {
            return typeImpl.GetInterface(name,ignoreCase);
        }
        
        public override Type[] GetInterfaces()
        {
            return typeImpl.GetInterfaces();
        }
        
        public override EventInfo GetEvent(String name,BindingFlags bindingAttr)
        {
            return typeImpl.GetEvent(name,bindingAttr);
        }
        
        public override EventInfo[] GetEvents()
        {
            return typeImpl.GetEvents();
        }
        
        protected override PropertyInfo GetPropertyImpl(String name,BindingFlags bindingAttr,Binder binder,
                        Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            if (returnType == null && types == null)
                return typeImpl.GetProperty(name,bindingAttr);
            else
                return typeImpl.GetProperty(name,bindingAttr,binder,returnType,types,modifiers);
        }
        
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return typeImpl.GetProperties(bindingAttr);
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return typeImpl.GetEvents(bindingAttr);
        }
        
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return typeImpl.GetNestedTypes(bindingAttr);
        }

        public override Type GetNestedType(String name, BindingFlags bindingAttr)
        {
            return typeImpl.GetNestedType(name,bindingAttr);
        }

        public override MemberInfo[] GetMember(String name,  MemberTypes type, BindingFlags bindingAttr)
        {
            return typeImpl.GetMember(name,type,bindingAttr);
        }
        
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return typeImpl.GetMembers(bindingAttr);
        }
        
        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return typeImpl.Attributes;
        }
        
        protected override bool IsArrayImpl()
        {
            return typeImpl.IsArray;
        }
        
        protected override bool IsPrimitiveImpl()
        {
            return typeImpl.IsPrimitive;
        }

        protected override bool IsByRefImpl()
        {
            return typeImpl.IsByRef;
        }

        protected override bool IsPointerImpl()
        {
            return typeImpl.IsPointer;
        }
        
        protected override bool IsValueTypeImpl() 
        {
            return typeImpl.IsValueType;
        }
        
        protected override bool IsCOMObjectImpl()
        {
            return typeImpl.IsCOMObject;
        }

        public override bool IsConstructedGenericType
        {
            get
            {
                return typeImpl.IsConstructedGenericType;
            }
        }

        public override Type GetElementType()
        {
            return typeImpl.GetElementType();
        }

        protected override bool HasElementTypeImpl()
        {
            return typeImpl.HasElementType;
        }
        
        public override Type UnderlyingSystemType 
        {
            get {return typeImpl.UnderlyingSystemType;}
        }
        
        // ICustomAttributeProvider
        public override Object[] GetCustomAttributes(bool inherit)
        {
            return typeImpl.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return typeImpl.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return typeImpl.IsDefined(attributeType, inherit);
        }

[System.Runtime.InteropServices.ComVisible(true)]
        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            return typeImpl.GetInterfaceMap(interfaceType);
        }
    }
}
