// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection.Emit 
{
    using System.Runtime.InteropServices;
    using System;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;
    
    [HostProtection(MayLeakOnAbort = true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_FieldBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class FieldBuilder : FieldInfo, _FieldBuilder
    {
        #region Private Data Members
        private int m_fieldTok;
        private FieldToken m_tkField;
        private TypeBuilder m_typeBuilder;
        private String m_fieldName;
        private FieldAttributes m_Attributes;
        private Type m_fieldType;
        #endregion

        #region Constructor
        [System.Security.SecurityCritical]  // auto-generated
        internal FieldBuilder(TypeBuilder typeBuilder, String fieldName, Type type, 
            Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
        {
            if (fieldName == null)
                throw new ArgumentNullException("fieldName");

            if (fieldName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "fieldName");

            if (fieldName[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalName"), "fieldName");

            if (type == null)
                throw new ArgumentNullException("type");

            if (type == typeof(void))
                throw new ArgumentException(Environment.GetResourceString("Argument_BadFieldType"));
            Contract.EndContractBlock();

            m_fieldName = fieldName;
            m_typeBuilder = typeBuilder;
            m_fieldType = type;
            m_Attributes = attributes & ~FieldAttributes.ReservedMask;
            
            SignatureHelper sigHelp = SignatureHelper.GetFieldSigHelper(m_typeBuilder.Module);
            sigHelp.AddArgument(type, requiredCustomModifiers, optionalCustomModifiers);

            int sigLength;
            byte[] signature = sigHelp.InternalGetSignature(out sigLength);
            
            m_fieldTok = TypeBuilder.DefineField(m_typeBuilder.GetModuleBuilder().GetNativeHandle(),
                typeBuilder.TypeToken.Token, fieldName, signature, sigLength, m_Attributes);

            m_tkField = new FieldToken(m_fieldTok, type);
        }

        #endregion

        #region Internal Members
        [System.Security.SecurityCritical]  // auto-generated
        internal void SetData(byte[] data, int size)
        {
            ModuleBuilder.SetFieldRVAContent(m_typeBuilder.GetModuleBuilder().GetNativeHandle(), m_tkField.Token, data, size);
        }

        internal TypeBuilder GetTypeBuilder() { return m_typeBuilder; }
        #endregion

        #region MemberInfo Overrides
        internal int MetadataTokenInternal
        {
            get { return m_fieldTok; }
        }
        
        public override Module Module
        {
            get { return m_typeBuilder.Module; }
        }

        public override String Name 
        {
            get {return m_fieldName; }
        }

        public override Type DeclaringType 
        {
            get 
            {
                if (m_typeBuilder.m_isHiddenGlobalType == true)
                    return null;

                return m_typeBuilder;
            }
        }
        
        public override Type ReflectedType 
        {
            get 
            {
                if (m_typeBuilder.m_isHiddenGlobalType == true)
                    return null;

                return m_typeBuilder;
            }
        }

        #endregion

        #region FieldInfo Overrides
        public override Type FieldType 
        {
            get { return m_fieldType; }
        }

        public override Object GetValue(Object obj)
        { 
            // NOTE!!  If this is implemented, make sure that this throws 
            // a NotSupportedException for Save-only dynamic assemblies.
            // Otherwise, it could cause the .cctor to be executed.

            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override void SetValue(Object obj,Object val,BindingFlags invokeAttr,Binder binder,CultureInfo culture)
        { 
            // NOTE!!  If this is implemented, make sure that this throws 
            // a NotSupportedException for Save-only dynamic assemblies.
            // Otherwise, it could cause the .cctor to be executed.

            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override RuntimeFieldHandle FieldHandle 
        {
            get { throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule")); }
        }

        public override FieldAttributes Attributes 
        {
            get { return m_Attributes; }
        }

        #endregion

        #region ICustomAttributeProvider Implementation
        public override Object[] GetCustomAttributes(bool inherit)
        {
             
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }
            
        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
             
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
             
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        #endregion

        #region Public Members
        public FieldToken GetToken() 
        {
            return m_tkField;
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        public void SetOffset(int iOffset) 
        {
            m_typeBuilder.ThrowIfCreated();     
   
            TypeBuilder.SetFieldLayoutOffset(m_typeBuilder.GetModuleBuilder().GetNativeHandle(), GetToken().Token, iOffset);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("An alternate API is available: Emit the MarshalAs custom attribute instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetMarshal(UnmanagedMarshal unmanagedMarshal)
        {
            if (unmanagedMarshal == null)
                throw new ArgumentNullException("unmanagedMarshal");
            Contract.EndContractBlock();

            m_typeBuilder.ThrowIfCreated();

            byte[] ubMarshal = unmanagedMarshal.InternalGetBytes();

            TypeBuilder.SetFieldMarshal(m_typeBuilder.GetModuleBuilder().GetNativeHandle(), GetToken().Token, ubMarshal, ubMarshal.Length);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetConstant(Object defaultValue) 
        {
            m_typeBuilder.ThrowIfCreated();  
      
            TypeBuilder.SetConstantValue(m_typeBuilder.GetModuleBuilder(), GetToken().Token, m_fieldType, defaultValue);
        }
        

#if FEATURE_CORECLR
[System.Security.SecurityCritical] // auto-generated
#else
[System.Security.SecuritySafeCritical]
#endif
[System.Runtime.InteropServices.ComVisible(true)]
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");

            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            Contract.EndContractBlock();

            ModuleBuilder module = m_typeBuilder.Module as ModuleBuilder;

            m_typeBuilder.ThrowIfCreated();

            TypeBuilder.DefineCustomAttribute(module,
                m_tkField.Token, module.GetConstructorToken(con).Token, binaryAttribute, false, false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException("customBuilder");
            Contract.EndContractBlock();

            m_typeBuilder.ThrowIfCreated();

            ModuleBuilder module = m_typeBuilder.Module as ModuleBuilder;

            customBuilder.CreateCustomAttribute(module, m_tkField.Token);
        }

        #endregion

#if !FEATURE_CORECLR
        void _FieldBuilder.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _FieldBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _FieldBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        void _FieldBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
