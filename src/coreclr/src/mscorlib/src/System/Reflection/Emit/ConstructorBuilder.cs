// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection.Emit 
{    
    using System;
    using System.Reflection;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Collections.Generic;
    using System.Diagnostics.SymbolStore;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;
    
    [HostProtection(MayLeakOnAbort = true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_ConstructorBuilder))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ConstructorBuilder : ConstructorInfo, _ConstructorBuilder
    { 
        private readonly MethodBuilder m_methodBuilder;
        internal bool m_isDefaultConstructor;

        #region Constructor

        private ConstructorBuilder()
        {
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal ConstructorBuilder(String name, MethodAttributes attributes, CallingConventions callingConvention,
            Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers, ModuleBuilder mod, TypeBuilder type)
        {
            int sigLength;
            byte[] sigBytes;
            MethodToken token;

            m_methodBuilder = new MethodBuilder(name, attributes, callingConvention, null, null, null, 
                parameterTypes, requiredCustomModifiers, optionalCustomModifiers, mod, type, false);

            type.m_listMethods.Add(m_methodBuilder);
            
            sigBytes = m_methodBuilder.GetMethodSignature().InternalGetSignature(out sigLength);
    
            token = m_methodBuilder.GetToken();
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal ConstructorBuilder(String name, MethodAttributes attributes, CallingConventions callingConvention,
            Type[] parameterTypes, ModuleBuilder mod, TypeBuilder type) : 
            this(name, attributes, callingConvention, parameterTypes, null, null, mod, type)
        {
        }

        #endregion

        #region Internal
        internal override Type[] GetParameterTypes()
        {
            return m_methodBuilder.GetParameterTypes();
        }

        private TypeBuilder GetTypeBuilder()
        {
            return m_methodBuilder.GetTypeBuilder();
        }

        internal ModuleBuilder GetModuleBuilder()
        {
            return GetTypeBuilder().GetModuleBuilder();
        }
        #endregion

        #region Object Overrides
        public override String ToString()
        {
            return m_methodBuilder.ToString();
        }
        
        #endregion

        #region MemberInfo Overrides
        internal int MetadataTokenInternal
        {
            get { return m_methodBuilder.MetadataTokenInternal; }
        }
        
        public override Module Module
        {
            get { return m_methodBuilder.Module; }
        }
        
        public override Type ReflectedType
        {
            get { return m_methodBuilder.ReflectedType; }
        }

        public override Type DeclaringType
        {
            get { return m_methodBuilder.DeclaringType; }
        }
    
        public override String Name 
        {
            get { return m_methodBuilder.Name; }
        }

        #endregion

        #region MethodBase Overrides
        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture) 
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule")); 
        }

        [Pure]
        public override ParameterInfo[] GetParameters()
        {
            ConstructorInfo rci = GetTypeBuilder().GetConstructor(m_methodBuilder.m_parameterTypes);
            return rci.GetParameters();
        }
                    
        public override MethodAttributes Attributes
        {
            get { return m_methodBuilder.Attributes; }
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return m_methodBuilder.GetMethodImplementationFlags();
        }
        
        public override RuntimeMethodHandle MethodHandle 
        {
            get { return m_methodBuilder.MethodHandle; }
        }
        
        #endregion

        #region ConstructorInfo Overrides
        public override Object Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule")); 
        }
    
        #endregion

        #region ICustomAttributeProvider Implementation
        public override Object[] GetCustomAttributes(bool inherit)
        {
            return m_methodBuilder.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_methodBuilder.GetCustomAttributes(attributeType, inherit);
        }
        
        public override bool IsDefined (Type attributeType, bool inherit)
        {
            return m_methodBuilder.IsDefined(attributeType, inherit);
        }

        #endregion

        #region Public Members
        public MethodToken GetToken()
        {
            return m_methodBuilder.GetToken();
        }
    
        public ParameterBuilder DefineParameter(int iSequence, ParameterAttributes attributes, String strParamName)
        {
            // Theoretically we shouldn't allow iSequence to be 0 because in reflection ctors don't have 
            // return parameters. But we'll allow it for backward compatibility with V2. The attributes 
            // defined on the return parameters won't be very useful but won't do much harm either.

            // MD will assert if we try to set the reserved bits explicitly
            attributes = attributes & ~ParameterAttributes.ReservedMask;
            return m_methodBuilder.DefineParameter(iSequence, attributes, strParamName);
        }
    
        public void SetSymCustomAttribute(String name, byte[] data)
        {
            m_methodBuilder.SetSymCustomAttribute(name, data);
        }
    
        public ILGenerator GetILGenerator() 
        {
            if (m_isDefaultConstructor)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DefaultConstructorILGen"));

            return m_methodBuilder.GetILGenerator();
        }
        
        public ILGenerator GetILGenerator(int streamSize)
        {
            if (m_isDefaultConstructor)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DefaultConstructorILGen"));

            return m_methodBuilder.GetILGenerator(streamSize);
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public void SetMethodBody(byte[] il, int maxStack, byte[] localSignature, IEnumerable<ExceptionHandler> exceptionHandlers, IEnumerable<int> tokenFixups)
        {
            if (m_isDefaultConstructor)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DefaultConstructorDefineBody"));
            }

            m_methodBuilder.SetMethodBody(il, maxStack, localSignature, exceptionHandlers, tokenFixups);
        }

#if FEATURE_CAS_POLICY
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void AddDeclarativeSecurity(SecurityAction action, PermissionSet pset)
        {
            if (pset == null)
                throw new ArgumentNullException("pset");

#pragma warning disable 618
            if (!Enum.IsDefined(typeof(SecurityAction), action) ||
                action == SecurityAction.RequestMinimum ||
                action == SecurityAction.RequestOptional ||
                action == SecurityAction.RequestRefuse)
            {
                throw new ArgumentOutOfRangeException("action");
            }
#pragma warning restore 618
            Contract.EndContractBlock();

            // Cannot add declarative security after type is created.
            if (m_methodBuilder.IsTypeCreated())
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TypeHasBeenCreated"));
    
            // Translate permission set into serialized format (use standard binary serialization).
            byte[] blob = pset.EncodeXml();
    
            // Write the blob into the metadata.
            TypeBuilder.AddDeclarativeSecurity(GetModuleBuilder().GetNativeHandle(), GetToken().Token, action, blob, blob.Length);
        }
#endif // FEATURE_CAS_POLICY

        public override CallingConventions CallingConvention 
        { 
            get 
            { 
                if (DeclaringType.IsGenericType)
                    return CallingConventions.HasThis;
                    
                return CallingConventions.Standard; 
            } 
        }
    
        public Module GetModule()
        {
            return m_methodBuilder.GetModule();
        }
    
    
        [Obsolete("This property has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")] //It always returns null.
        public Type ReturnType
        {
            get { return GetReturnType(); }
        }
        
        // This always returns null. Is that what we want?
        internal override Type GetReturnType() 
        {
            return m_methodBuilder.ReturnType;
        }
                                
        public String Signature 
        {
            get { return m_methodBuilder.Signature; }
        }
    
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        [System.Runtime.InteropServices.ComVisible(true)]
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            m_methodBuilder.SetCustomAttribute(con, binaryAttribute);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            m_methodBuilder.SetCustomAttribute(customBuilder);
        }

        public void SetImplementationFlags(MethodImplAttributes attributes) 
        {
            m_methodBuilder.SetImplementationFlags(attributes);
        }
                
        public bool InitLocals 
        {
            get { return m_methodBuilder.InitLocals; }
            set { m_methodBuilder.InitLocals = value; }
        }

        #endregion

#if !FEATURE_CORECLR
        void _ConstructorBuilder.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _ConstructorBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _ConstructorBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        void _ConstructorBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif
    }
}

