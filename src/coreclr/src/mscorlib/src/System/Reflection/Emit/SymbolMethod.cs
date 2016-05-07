// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection.Emit 
{
    using System.Runtime.InteropServices;
    using System;
    using System.Reflection;
    using System.Diagnostics.Contracts;
    using CultureInfo = System.Globalization.CultureInfo;
    
    internal sealed class SymbolMethod : MethodInfo
    {
        #region Private Data Members
        private ModuleBuilder m_module;
        private Type m_containingType;
        private String m_name;
        private CallingConventions m_callingConvention;
        private Type m_returnType;
        private MethodToken m_mdMethod;
        private Type[] m_parameterTypes;
        private SignatureHelper m_signature;
        #endregion

        #region Constructor
        [System.Security.SecurityCritical]  // auto-generated
        internal SymbolMethod(ModuleBuilder mod, MethodToken token, Type arrayClass, String methodName, 
            CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            // This is a kind of MethodInfo to represent methods for array type of unbaked type

            // Another way to look at this class is as a glorified MethodToken wrapper. At the time of this comment
            // this class is only constructed inside ModuleBuilder.GetArrayMethod and the only interesting thing 
            // passed into it is this MethodToken. The MethodToken was forged using a TypeSpec for an Array type and
            // the name of the method on Array. 
            // As none of the methods on Array have CustomModifiers their is no need to pass those around in here.
            m_mdMethod = token;

            // The ParameterTypes are also a bit interesting in that they may be unbaked TypeBuilders.
            m_returnType = returnType;
            if (parameterTypes != null)
            {
                m_parameterTypes = new Type[parameterTypes.Length];
                Array.Copy(parameterTypes, 0, m_parameterTypes, 0, parameterTypes.Length);
            }
            else
            {
                m_parameterTypes = EmptyArray<Type>.Value; 
            }
   
            m_module = mod;
            m_containingType = arrayClass;
            m_name = methodName;
            m_callingConvention = callingConvention;

            m_signature = SignatureHelper.GetMethodSigHelper(
                mod, callingConvention, returnType, null, null, parameterTypes, null, null);
        }
        #endregion

        #region Internal Members
        internal override Type[] GetParameterTypes()
        {
            return m_parameterTypes;
        }

        internal MethodToken GetToken(ModuleBuilder mod)
        {
            return mod.GetArrayMethodToken(m_containingType, m_name, m_callingConvention, m_returnType, m_parameterTypes);
        }
        
        #endregion

        #region MemberInfo Overrides
        public override Module Module 
        { 
            get { return m_module; } 
        } 

        public override Type ReflectedType
        {
            get { return m_containingType as Type; }
        }

        public override String Name 
        {
            get { return m_name; }
        }

        public override Type DeclaringType 
        {
            get {return m_containingType;}
        }
        #endregion

        #region MethodBase Overrides
        [Pure]
        public override ParameterInfo[] GetParameters()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
        }
            
        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
        }
            
        public override MethodAttributes Attributes 
        {
            get { throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod")); }
        }
            
        public override CallingConventions CallingConvention 
        {
            get { return m_callingConvention; }
        }

        public override RuntimeMethodHandle MethodHandle 
        {
            get { throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod")); }
        }

        #endregion

        #region MethodInfo Overrides
        public override Type ReturnType
        {
            get
            {
                return m_returnType;
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes 
        {
            get { return null; } 
        }
                              
        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
        }
            
        public override MethodInfo GetBaseDefinition()
        {
            return this;
        }
        #endregion

        #region ICustomAttributeProvider Implementation
        public override Object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
        }
            
        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
        }
        
        #endregion

        #region Public Members
        public Module GetModule()
        {
            return m_module;
        }

        public MethodToken GetToken()
        {
            return m_mdMethod;
        }

        #endregion
    }
}
