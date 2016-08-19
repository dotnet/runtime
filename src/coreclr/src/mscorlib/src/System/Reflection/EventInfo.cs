// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Runtime.ConstrainedExecution;
    using System.Security.Permissions;
    using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

    [Serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_EventInfo))]
#pragma warning disable 618
    [PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]
#pragma warning restore 618
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class EventInfo : MemberInfo, _EventInfo
    {
        #region Constructor
        protected EventInfo() { }
        #endregion

        public static bool operator ==(EventInfo left, EventInfo right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimeEventInfo || right is RuntimeEventInfo)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(EventInfo left, EventInfo right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return MemberTypes.Event; } }
        #endregion

        #region Public Abstract\Virtual Members
        public virtual MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public abstract MethodInfo GetAddMethod(bool nonPublic);

        public abstract MethodInfo GetRemoveMethod(bool nonPublic);

        public abstract MethodInfo GetRaiseMethod(bool nonPublic);

        public abstract EventAttributes Attributes { get; }
        #endregion

        #region Public Members
        public virtual MethodInfo AddMethod
        {
            get
            {
                return GetAddMethod(true);
            }
        }

        public virtual MethodInfo RemoveMethod
        {
            get
            {
                return GetRemoveMethod(true);
            }
        }

        public virtual MethodInfo RaiseMethod
        {
            get
            {
                return GetRaiseMethod(true);
            }
        }

        public MethodInfo[] GetOtherMethods() { return GetOtherMethods(false); }

        public MethodInfo GetAddMethod() { return GetAddMethod(false); }
   
        public MethodInfo GetRemoveMethod() { return GetRemoveMethod(false); }

        public MethodInfo GetRaiseMethod() { return GetRaiseMethod(false); }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public virtual void AddEventHandler(Object target, Delegate handler)
        {
            MethodInfo addMethod = GetAddMethod();

            if (addMethod == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoPublicAddMethod"));

#if FEATURE_COMINTEROP
            if (addMethod.ReturnType == typeof(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotSupportedOnWinRTEvent"));

            // Must be a normal non-WinRT event
            Contract.Assert(addMethod.ReturnType == typeof(void));
#endif // FEATURE_COMINTEROP

            addMethod.Invoke(target, new object[] { handler });
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public virtual void RemoveEventHandler(Object target, Delegate handler)
        {
            MethodInfo removeMethod = GetRemoveMethod();

            if (removeMethod == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoPublicRemoveMethod"));

#if FEATURE_COMINTEROP
            ParameterInfo[] parameters = removeMethod.GetParametersNoCopy();
            Contract.Assert(parameters != null && parameters.Length == 1);

            if (parameters[0].ParameterType == typeof(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotSupportedOnWinRTEvent"));

            // Must be a normal non-WinRT event
            Contract.Assert(parameters[0].ParameterType.BaseType == typeof(MulticastDelegate));
#endif // FEATURE_COMINTEROP

            removeMethod.Invoke(target, new object[] { handler });
        }

        public virtual Type EventHandlerType 
        {
            get 
            {
                MethodInfo m = GetAddMethod(true);

                ParameterInfo[] p = m.GetParametersNoCopy();

                Type del = typeof(Delegate);

                for (int i = 0; i < p.Length; i++)
                {
                    Type c = p[i].ParameterType;

                    if (c.IsSubclassOf(del))
                        return c;
                }
                return null;
            }
        }
        public bool IsSpecialName 
        {
            get 
            {
                return(Attributes & EventAttributes.SpecialName) != 0;
            }
        }

        public virtual bool IsMulticast 
        {
            get 
            {
                Type cl = EventHandlerType;
                Type mc = typeof(MulticastDelegate);
                return mc.IsAssignableFrom(cl);
            }
        }
        #endregion

#if !FEATURE_CORECLR
        Type _EventInfo.GetType()
        {
            return base.GetType();
        }

        void _EventInfo.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _EventInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _EventInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        // If you implement this method, make sure to include _EventInfo.Invoke in VM\DangerousAPIs.h and 
        // include _EventInfo in SystemDomain::IsReflectionInvocationMethod in AppDomain.cpp.
        void _EventInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif
    }

    [Serializable]
    internal unsafe sealed class RuntimeEventInfo : EventInfo, ISerializable
    {
        #region Private Data Members
        private int m_token;
        private EventAttributes m_flags;
        private string m_name;
        [System.Security.SecurityCritical]
        private void* m_utf8name;
        private RuntimeTypeCache m_reflectedTypeCache;
        private RuntimeMethodInfo m_addMethod;
        private RuntimeMethodInfo m_removeMethod;
        private RuntimeMethodInfo m_raiseMethod;
        private MethodInfo[] m_otherMethod;        
        private RuntimeType m_declaringType;
        private BindingFlags m_bindingFlags;
        #endregion

        #region Constructor
        internal RuntimeEventInfo()
        {
            // Used for dummy head node during population
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal RuntimeEventInfo(int tkEvent, RuntimeType declaredType, RuntimeTypeCache reflectedTypeCache, out bool isPrivate)
        {
            Contract.Requires(declaredType != null);
            Contract.Requires(reflectedTypeCache != null);
            Contract.Assert(!reflectedTypeCache.IsGlobal);

            MetadataImport scope = declaredType.GetRuntimeModule().MetadataImport;

            m_token = tkEvent;
            m_reflectedTypeCache = reflectedTypeCache;        
            m_declaringType = declaredType;
            

            RuntimeType reflectedType = reflectedTypeCache.GetRuntimeType();

            scope.GetEventProps(tkEvent, out m_utf8name, out m_flags);

            RuntimeMethodInfo dummy;
            Associates.AssignAssociates(scope, tkEvent, declaredType, reflectedType, 
                out m_addMethod, out m_removeMethod, out m_raiseMethod, 
                out dummy, out dummy, out m_otherMethod, out isPrivate, out m_bindingFlags);
        }
        #endregion

        #region Internal Members
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal override bool CacheEquals(object o)
        {
            RuntimeEventInfo m = o as RuntimeEventInfo;

            if ((object)m == null)
                return false;

            return m.m_token == m_token &&
                RuntimeTypeHandle.GetModule(m_declaringType).Equals(
                    RuntimeTypeHandle.GetModule(m.m_declaringType));
        }

        internal BindingFlags BindingFlags { get { return m_bindingFlags; } }
        #endregion

        #region Object Overrides
        public override String ToString() 
        {
            if (m_addMethod == null || m_addMethod.GetParametersNoCopy().Length == 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoPublicAddMethod"));

            return m_addMethod.GetParametersNoCopy()[0].ParameterType.FormatTypeName() + " " + Name;
        }
        #endregion

        #region ICustomAttributeProvider
        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null) 
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"attributeType");

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null) 
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"attributeType");

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return MemberTypes.Event; } }
        public override String Name 
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get 
            {
                if (m_name == null)
                    m_name = new Utf8String(m_utf8name).ToString();
                
                return m_name; 
            } 
        }
        public override Type DeclaringType { get { return m_declaringType; } }
        public override Type ReflectedType
        {
            get
            {
                return ReflectedTypeInternal;
            }
        }

        private RuntimeType ReflectedTypeInternal
        {
            get
            {
                return m_reflectedTypeCache.GetRuntimeType();
            }
        }

        public override int MetadataToken { get { return m_token; } }
        public override Module Module { get { return GetRuntimeModule(); } }
        internal RuntimeModule GetRuntimeModule() { return m_declaringType.GetRuntimeModule(); }
        #endregion

        #region ISerializable
        [System.Security.SecurityCritical]  // auto-generated_required
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            MemberInfoSerializationHolder.GetSerializationInfo(
                info,
                Name,
                ReflectedTypeInternal,
                null,
                MemberTypes.Event);
        }
        #endregion

        #region EventInfo Overrides
        public override MethodInfo[] GetOtherMethods(bool nonPublic) 
        {
            List<MethodInfo> ret = new List<MethodInfo>();

            if ((object)m_otherMethod == null)
                return new MethodInfo[0];

            for(int i = 0; i < m_otherMethod.Length; i ++)
            {
                if (Associates.IncludeAccessor((MethodInfo)m_otherMethod[i], nonPublic))
                    ret.Add(m_otherMethod[i]);
            }
            
            return ret.ToArray();
        }

        public override MethodInfo GetAddMethod(bool nonPublic)
        {
            if (!Associates.IncludeAccessor(m_addMethod, nonPublic))
                return null;

            return m_addMethod;
        }

        public override MethodInfo GetRemoveMethod(bool nonPublic)
        {
            if (!Associates.IncludeAccessor(m_removeMethod, nonPublic))
                return null;

            return m_removeMethod;
        }

        public override MethodInfo GetRaiseMethod(bool nonPublic)
        {
            if (!Associates.IncludeAccessor(m_raiseMethod, nonPublic))
                return null;

            return m_raiseMethod;
        }

        public override EventAttributes Attributes 
        {
            get
            {
                return m_flags;
            }
        }
        #endregion    
    }

}
