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

    internal sealed class MethodOnTypeBuilderInstantiation : MethodInfo
    {
        #region Private Static Members
        internal static MethodInfo GetMethod(MethodInfo method, TypeBuilderInstantiation type)
        {
            return new MethodOnTypeBuilderInstantiation(method, type);
        }
        #endregion

        #region Private Data Mebers
        internal MethodInfo m_method;
        private TypeBuilderInstantiation m_type;
        #endregion

        #region Constructor
        internal MethodOnTypeBuilderInstantiation(MethodInfo method, TypeBuilderInstantiation type)
        {
            Contract.Assert(method is MethodBuilder || method is RuntimeMethodInfo);

            m_method = method;
            m_type = type;
        }
        #endregion
        
        internal override Type[] GetParameterTypes()
        {
            return m_method.GetParameterTypes();
        }

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return m_method.MemberType;  } }
        public override String Name { get { return m_method.Name; } }
        public override Type DeclaringType { get { return m_type; } }
        public override Type ReflectedType { get { return m_type; } }
        public override Object[] GetCustomAttributes(bool inherit) { return m_method.GetCustomAttributes(inherit); } 
        public override Object[] GetCustomAttributes(Type attributeType, bool inherit) { return m_method.GetCustomAttributes(attributeType, inherit); }
        public override bool IsDefined(Type attributeType, bool inherit) { return m_method.IsDefined(attributeType, inherit); }
        internal int MetadataTokenInternal
        {
            get
            {
                MethodBuilder mb = m_method as MethodBuilder;

                if (mb != null)
                    return mb.MetadataTokenInternal;
                else
                {
                    Contract.Assert(m_method is RuntimeMethodInfo);
                    return m_method.MetadataToken;
                }
            }
        }
        public override Module Module { get { return m_method.Module; } }              
        public new Type GetType() { return base.GetType(); }
        #endregion

        #region MethodBase Members
        [Pure]
        public override ParameterInfo[] GetParameters() { return m_method.GetParameters(); }
        public override MethodImplAttributes GetMethodImplementationFlags() { return m_method.GetMethodImplementationFlags(); }
        public override RuntimeMethodHandle MethodHandle { get { return m_method.MethodHandle; } }
        public override MethodAttributes Attributes { get { return m_method.Attributes; } }
        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
        public override CallingConventions CallingConvention { get { return m_method.CallingConvention; } }
        public override Type [] GetGenericArguments() { return m_method.GetGenericArguments(); }
        public override MethodInfo GetGenericMethodDefinition() { return m_method; }
        public override bool IsGenericMethodDefinition { get { return m_method.IsGenericMethodDefinition; } }
        public override bool ContainsGenericParameters { get { return m_method.ContainsGenericParameters; } }
        public override MethodInfo MakeGenericMethod(params Type[] typeArgs)
        {
            if (!IsGenericMethodDefinition)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericMethodDefinition"));
            Contract.EndContractBlock();

            return MethodBuilderInstantiation.MakeGenericMethod(this, typeArgs);
        }

        public override bool IsGenericMethod { get { return m_method.IsGenericMethod; } }
      
        #endregion

        #region Public Abstract\Virtual Members
        public override Type ReturnType { get { return m_method.ReturnType; } }
        public override ParameterInfo ReturnParameter { get { throw new NotSupportedException(); } }
        public override ICustomAttributeProvider ReturnTypeCustomAttributes { get { throw new NotSupportedException(); } }
        public override MethodInfo GetBaseDefinition() { throw new NotSupportedException(); }
        #endregion    
    }

    internal sealed class ConstructorOnTypeBuilderInstantiation : ConstructorInfo
    {
        #region Private Static Members
        internal static ConstructorInfo GetConstructor(ConstructorInfo Constructor, TypeBuilderInstantiation type)
        {
            return new ConstructorOnTypeBuilderInstantiation(Constructor, type);
        }
        #endregion

        #region Private Data Mebers
        internal ConstructorInfo m_ctor;
        private TypeBuilderInstantiation m_type;
        #endregion

        #region Constructor
        internal ConstructorOnTypeBuilderInstantiation(ConstructorInfo constructor, TypeBuilderInstantiation type)
        {
            Contract.Assert(constructor is ConstructorBuilder || constructor is RuntimeConstructorInfo);

            m_ctor = constructor;
            m_type = type;
        }
        #endregion
        
        internal override Type[] GetParameterTypes()
        {
            return m_ctor.GetParameterTypes();
        }

        internal override Type GetReturnType()
        {
            return DeclaringType;
        }

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return m_ctor.MemberType; } }
        public override String Name { get { return m_ctor.Name; } }
        public override Type DeclaringType { get { return m_type; } }
        public override Type ReflectedType { get { return m_type; } }
        public override Object[] GetCustomAttributes(bool inherit) { return m_ctor.GetCustomAttributes(inherit); } 
        public override Object[] GetCustomAttributes(Type attributeType, bool inherit) { return m_ctor.GetCustomAttributes(attributeType, inherit); }
        public override bool IsDefined(Type attributeType, bool inherit) { return m_ctor.IsDefined(attributeType, inherit); }
        internal int MetadataTokenInternal
        {
            get
            {
                ConstructorBuilder cb = m_ctor as ConstructorBuilder;

                if (cb != null)
                    return cb.MetadataTokenInternal;
                else
                {
                    Contract.Assert(m_ctor is RuntimeConstructorInfo);
                    return m_ctor.MetadataToken;
                }
            }
        }
        public override Module Module { get { return m_ctor.Module; } }              
        public new Type GetType() { return base.GetType(); }
        #endregion

        #region MethodBase Members
        [Pure]
        public override ParameterInfo[] GetParameters() { return m_ctor.GetParameters(); }
        public override MethodImplAttributes GetMethodImplementationFlags() { return m_ctor.GetMethodImplementationFlags(); }
        public override RuntimeMethodHandle MethodHandle { get { return m_ctor.MethodHandle; } }
        public override MethodAttributes Attributes { get { return m_ctor.Attributes; } }
        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
        public override CallingConventions CallingConvention { get { return m_ctor.CallingConvention; } }
        public override Type[] GetGenericArguments() { return m_ctor.GetGenericArguments(); }
        public override bool IsGenericMethodDefinition { get { return false; } }
        public override bool ContainsGenericParameters { get { return false; } }

        public override bool IsGenericMethod { get { return false; } }
        #endregion

        #region ConstructorInfo Members
        public override Object Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
        #endregion
    }

    internal sealed class FieldOnTypeBuilderInstantiation : FieldInfo
    {
        #region Private Static Members
        internal static FieldInfo GetField(FieldInfo Field, TypeBuilderInstantiation type)
        {
            FieldInfo m = null;

            // This ifdef was introduced when non-generic collections were pulled from
            // silverlight. See code:Dictionary#DictionaryVersusHashtableThreadSafety
            // for information about this change.
            //
            // There is a pre-existing race condition in this code with the side effect
            // that the second thread's value clobbers the first in the hashtable. This is 
            // an acceptable race condition since we make no guarantees that this will return the
            // same object.
            //
            // We're not entirely sure if this cache helps any specific scenarios, so 
            // long-term, one could investigate whether it's needed. In any case, this
            // method isn't expected to be on any critical paths for performance.
            if (type.m_hashtable.Contains(Field)) {
                m = type.m_hashtable[Field] as FieldInfo;
            }
            else {
                m = new FieldOnTypeBuilderInstantiation(Field, type);
                type.m_hashtable[Field] = m;
            }

            return m;
        }
        #endregion

        #region Private Data Members
        private FieldInfo m_field;
        private TypeBuilderInstantiation m_type;
        #endregion

        #region Constructor
        internal FieldOnTypeBuilderInstantiation(FieldInfo field, TypeBuilderInstantiation type)
        {
            Contract.Assert(field is FieldBuilder || field is RuntimeFieldInfo);

            m_field = field;
            m_type = type;
        }
        #endregion

        internal FieldInfo FieldInfo { get { return m_field; } }

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return System.Reflection.MemberTypes.Field; } }          
        public override String Name { get { return m_field.Name; } }
        public override Type DeclaringType { get { return m_type; } }
        public override Type ReflectedType { get { return m_type; } }
        public override Object[] GetCustomAttributes(bool inherit) { return m_field.GetCustomAttributes(inherit); } 
        public override Object[] GetCustomAttributes(Type attributeType, bool inherit) { return m_field.GetCustomAttributes(attributeType, inherit); }
        public override bool IsDefined(Type attributeType, bool inherit) { return m_field.IsDefined(attributeType, inherit); }
        internal int MetadataTokenInternal
        {
            get
            {
                FieldBuilder fb = m_field as FieldBuilder;

                if (fb != null)
                    return fb.MetadataTokenInternal;
                else
                {
                    Contract.Assert(m_field is RuntimeFieldInfo);
                    return m_field.MetadataToken;
                }
            }
        }
        public override Module Module { get { return m_field.Module; } }              
        public new Type GetType() { return base.GetType(); }
        #endregion

        #region Public Abstract\Virtual Members
        public override Type[] GetRequiredCustomModifiers() { return m_field.GetRequiredCustomModifiers(); }
        public override Type[] GetOptionalCustomModifiers() { return m_field.GetOptionalCustomModifiers(); }
        public override void SetValueDirect(TypedReference obj, Object value)
        {
            throw new NotImplementedException();
        }
        public override Object GetValueDirect(TypedReference obj)
        {
            throw new NotImplementedException();
        }    
        public override RuntimeFieldHandle FieldHandle 
        {
            get { throw new NotImplementedException(); }
        }    
        public override Type FieldType { get { throw new NotImplementedException(); } }
        public override Object GetValue(Object obj) { throw new InvalidOperationException(); }
        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) { throw new InvalidOperationException(); }
        public override FieldAttributes Attributes { get { return m_field.Attributes;  } }
        #endregion
    }
}








































