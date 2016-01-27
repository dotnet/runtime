// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Eventbuilder is for client to define eevnts for a class
**
** 
===========================================================*/
namespace System.Reflection.Emit {
    
    using System;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;
    
    // 
    // A EventBuilder is always associated with a TypeBuilder.  The TypeBuilder.DefineEvent
    // method will return a new EventBuilder to a client.
    // 
    [HostProtection(MayLeakOnAbort = true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_EventBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class EventBuilder : _EventBuilder
    { 
    
        // Make a private constructor so these cannot be constructed externally.
        private EventBuilder() {}
        
        // Constructs a EventBuilder.  
        //
        internal EventBuilder(
            ModuleBuilder    mod,                    // the module containing this EventBuilder        
            String            name,                    // Event name
            EventAttributes attr,                    // event attribute such as Public, Private, and Protected defined above
            //int            eventType,                // event type
            TypeBuilder     type,                    // containing type
            EventToken        evToken)
        {           
            m_name = name;
            m_module = mod;
            m_attributes = attr;
            m_evToken = evToken;
            m_type = type;
        }
    
        // Return the Token for this event within the TypeBuilder that the
        // event is defined within.
        public EventToken GetEventToken()
        {
            return m_evToken;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
        {
            if (mdBuilder == null)
            {
                throw new ArgumentNullException("mdBuilder");
            }
            Contract.EndContractBlock();

            m_type.ThrowIfCreated();
            TypeBuilder.DefineMethodSemantics(
                m_module.GetNativeHandle(),
                m_evToken.Token,
                semantics,
                mdBuilder.GetToken().Token);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetAddOnMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.AddOn);
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetRemoveOnMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.RemoveOn);
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetRaiseMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Fire);
        }
       
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void AddOtherMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
        }
    
        // Use this function if client decides to form the custom attribute blob themselves

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
            m_type.ThrowIfCreated();

            TypeBuilder.DefineCustomAttribute(
                m_module,
                m_evToken.Token,
                m_module.GetConstructorToken(con).Token,
                binaryAttribute,
                false, false);
        }

        // Use this function if client wishes to build CustomAttribute using CustomAttributeBuilder
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException("customBuilder");
            }
            Contract.EndContractBlock();
            m_type.ThrowIfCreated();
            customBuilder.CreateCustomAttribute(m_module, m_evToken.Token);
        }

#if !FEATURE_CORECLR
        void _EventBuilder.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _EventBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _EventBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        void _EventBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif


        // These are package private so that TypeBuilder can access them.
        private String              m_name;         // The name of the event
        private EventToken          m_evToken;      // The token of this event
        private ModuleBuilder       m_module;
        private EventAttributes     m_attributes;
        private TypeBuilder         m_type;       
    }




}
